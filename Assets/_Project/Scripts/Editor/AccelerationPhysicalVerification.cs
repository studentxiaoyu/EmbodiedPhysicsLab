using System;
using System.Linq;
using EmbodiedPhysicsLab.Core;
using EmbodiedPhysicsLab.Core.Acceleration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmbodiedPhysicsLab.Editor
{
    [InitializeOnLoad]
    public static class AccelerationPhysicalVerification
    {
        private const string RunningKey = "AccelerationPhysicalVerification.Running";
        private static AccelerationExperimentController controller;
        private static AccelerationEquipmentInteraction equipment;
        private static DesktopGrabInteractor grab;
        private static Camera camera;
        private static GameObject player;
        private static int motionStartFrame;
        static AccelerationPhysicalVerification() { EditorApplication.playModeStateChanged += OnMode; }
        public static void Run()
        {
            EditorSceneManager.OpenScene(AccelerationLabBuilder.Lab, OpenSceneMode.Single);
            SessionState.SetBool(RunningKey, true);
            EditorApplication.EnterPlaymode();
        }
        private static void OnMode(PlayModeStateChange mode)
        {
            if (mode == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(RunningKey, false))
                EditorApplication.delayCall += VerifyPreparation;
        }

        private static void VerifyPreparation()
        {
            try
            {
                controller = Object.FindFirstObjectByType<AccelerationExperimentController>();
                equipment = controller.GetComponent<AccelerationEquipmentInteraction>();
                player = GameObject.Find("DesktopPlayer"); camera = player.GetComponentInChildren<Camera>();
                grab = player.GetComponent<DesktopGrabInteractor>();
                controller.ResetExperiment();
                Require(equipment != null && grab != null, "Physical apparatus or grab adapter missing.");
                Require(!Object.FindObjectsByType<AccelerationInteractable>(FindObjectsSortMode.None).Any(x => x.Command < AccelerationCommand.Start), "Legacy mass/angle buttons remain in the scene.");
                var cartWeight = equipment.CartItems[0];
                var origin = cartWeight.transform.position;
                Grab(cartWeight.transform, Vector3.up * .025f);
                Require(cartWeight.IsGrabbed && Vector3.Distance(origin, cartWeight.transform.position) < .0001f, "Physical pickup failed or teleported the mass.");
                Require(!controller.StartExperiment(), "Start accepted a mass still being handled.");
                cartWeight.UpdateGrab(new Pose(new Vector3(0, 2.2f, 2.3f), Quaternion.identity), .5f);
                grab.Release();
                Require(!cartWeight.GetComponent<Rigidbody>().isKinematic && cartWeight.GetComponent<Rigidbody>().useGravity, "Free release did not restore physical falling.");
                Grab(cartWeight.transform, Vector3.up * .025f);
                Place(cartWeight, equipment.CartSlots[0]);
                Require(controller.State.CartWeights == 1 && equipment.CartSlots[0].Occupant == cartWeight, "Placing the real mass did not update M.");
                Near(controller.State.CartMass, controller.Settings.cartBaseMass + controller.Settings.cartWeightMass);
                Grab(cartWeight.transform, Vector3.up * .025f);
                Require(controller.State.CartWeights == 0, "Lifting the actual mass did not reduce M.");
                Place(cartWeight, equipment.CartHomes[0]);
                Grab(cartWeight.transform, Vector3.up * .025f); Place(cartWeight, equipment.CartSlots[0]);
                var bucketWeight = equipment.BucketItems[0];
                Grab(bucketWeight.transform, Vector3.up * .02f); Place(bucketWeight, equipment.BucketSlots[equipment.BucketSlots.Count - 1]);
                Require(controller.State.BucketWeights == 1 && equipment.BucketSlots[0].Occupant == bucketWeight, "Bucket mass did not settle to the lowest stack level.");
                Near(controller.State.BucketMass, controller.Settings.bucketBaseMass + controller.Settings.bucketWeightMass);
                DragAngle(.3f);
                Near(controller.State.Angle, 1.2, 1e-5);
                Near(controller.GetComponent<AccelerationExperimentVisual>().Track.localEulerAngles.z, 1.2, 1e-4);
                Grab(equipment.FrictionKnob.transform, Vector3.zero);
                var pose = OperationPose();
                var knob = equipment.FrictionKnob;
                var axis = knob.transform.parent.TransformDirection(Vector3.forward);
                var vector = pose.position - knob.transform.position;
                var turned = Quaternion.AngleAxis(54f, axis) * vector;
                knob.UpdateGrab(new Pose(knob.transform.position + turned, pose.rotation), .02f);
                Near(controller.State.Friction, .01, 1e-5);
                Require(!controller.StartExperiment(), "Start accepted an unfinished handle operation.");
                grab.Release();
                var expected = AccelerationPhysicsModel.Evaluate(controller.State.CartMass, controller.State.BucketMass, controller.State.Angle, controller.State.Friction, controller.Settings.gravity);
                Near(controller.State.Acceleration, expected.Acceleration);
                Require(controller.StartExperiment(), "Prepared apparatus did not start.");
                Require(!cartWeight.TryBeginGrab(player.transform, OperationPose()), "Mass remained grabbable during run.");
                Require(!controller.SetAngleDegrees(3), "Inclination changed during run.");
                controller.Advance(.15);
                motionStartFrame = Time.frameCount;
                EditorApplication.update += WaitForMotion;
            }
            catch (Exception error) { Fail(error); }
        }
        private static void WaitForMotion()
        {
            if (Time.frameCount < motionStartFrame + 2) return;
            EditorApplication.update -= WaitForMotion;
            try
            {
                var cartWeight = equipment.CartItems[0];
                Require(Vector3.Distance(cartWeight.transform.position, cartWeight.Socket.SnapPoint.position) < .003f, "Installed physical mass did not travel with the cart.");
                controller.Advance(100);
                Require(controller.State.Phase == AccelerationPhase.Completed && controller.RecordResult(), "First run did not complete/record.");
                controller.ResetExperiment();
                Require(controller.State.Phase == AccelerationPhase.Ready && controller.State.CartWeights == controller.Settings.defaultCartWeights && controller.Recorder.Samples.Count == 0, "Reset did not clear state and samples.");
                Require(equipment.CartItems[0].Socket == equipment.CartHomes[0] && equipment.BucketItems[0].Socket == equipment.BucketHomes[0], "Reset did not return real weights to the trays.");
                Near(equipment.AngleSlider.NormalizedValue, 0); Near(equipment.FrictionKnob.Angle, 0);
                var bucket = equipment.BucketItems[1]; Grab(bucket.transform, Vector3.up * .02f); Place(bucket, equipment.BucketSlots[0]);
                DragAngle(.5f); Near(controller.State.Angle, 2, 1e-5);
                Require(controller.StartExperiment(), "Second run required a scene reload.");
                controller.Advance(100);
                Require(controller.State.Phase == AccelerationPhase.Completed, "Second run did not complete.");
                controller.ResetExperiment();
                Debug.Log("ACCELERATION_PHYSICAL_VERIFIED: ray pickup, same-object placement/removal, M/m from occupancy, bucket settling, continuous slider/knob input, unchanged analytical model, start guard, running locks, carried masses, record, reset to trays and two complete rounds.");
                Finish(0);
            }
            catch (Exception error) { Fail(error); }
        }
        private static void DragAngle(float normalizedDelta)
        {
            var slider = equipment.AngleSlider;
            Grab(slider.transform, Vector3.zero);
            var pose = OperationPose();
            slider.UpdateGrab(new Pose(pose.position + slider.transform.parent.TransformVector(Vector3.right * (.7f * normalizedDelta)), pose.rotation), .02f);
            grab.Release();
        }
        private static Pose OperationPose() => new Pose(camera.transform.TransformPoint(new Vector3(.25f, -.27f, .85f)), camera.transform.rotation);
        private static void Grab(Transform target, Vector3 offset)
        {
            player.transform.rotation = Quaternion.identity;
            player.transform.position = new Vector3(target.position.x, .04f, target.position.z - 1.25f);
            camera.transform.rotation = Quaternion.LookRotation(target.position + offset - camera.transform.position);
            Physics.SyncTransforms();
            Require(player.GetComponent<PlayerInteractor>().TryInteract(), "Interaction ray missed " + target.name);
            Require(grab.HeldObject != null, "Interaction did not grab " + target.name);
        }
        private static void Place(SnapObject item, SnapSocket socket)
        {
            for (int i = 0; i < 30; i++) item.UpdateGrab(new Pose(socket.SnapPoint.position, socket.SnapPoint.rotation), .05f);
            Require(grab.Release() && item.Socket != null, "Release did not place the mass into a socket.");
        }
        private static void Near(double value, double expected, double tolerance = 1e-8) => Require(Math.Abs(value - expected) <= tolerance, $"Expected {expected}, got {value}.");
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private static void Fail(Exception error) { Debug.LogException(error); Finish(1); }
        private static void Finish(int result)
        {
            SessionState.SetBool(RunningKey, false);
            EditorApplication.update -= WaitForMotion;
            EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += () => EditorApplication.Exit(result);
        }
    }
}
