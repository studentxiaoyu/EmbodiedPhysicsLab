using System;
using EmbodiedPhysicsLab.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmbodiedPhysicsLab.Editor
{
    /// <summary>Isolated unsaved Play Mode verification; no project scene or experiment is edited.</summary>
    [InitializeOnLoad]
    public static class ManipulationFrameworkVerification
    {
        private const string RunningKey = "ManipulationFrameworkVerification.Running";
        static ManipulationFrameworkVerification()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        [MenuItem("Embodied Physics Lab/Verify Desktop Manipulation Framework")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before verification.");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetBool(RunningKey, true);
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(RunningKey, false)) return;
            EditorApplication.delayCall += Verify;
        }

        private static void Verify()
        {
            int code = 0;
            try
            {
                VerifyGrabAndSnap();
                VerifyConstraints();
                VerifyConnections();
                Debug.Log("MANIPULATION_VERIFIED: existing interaction ray, grab ownership, smooth offset following, release/body restore, proximity snap, socket occupancy/disable cleanup, slider and knob constraints/events, 2/3-state lever, symmetric port connections/type checks/cleanup.");
            }
            catch (Exception error) { Debug.LogException(error); code = 1; }
            SessionState.SetBool(RunningKey, false);
            EditorApplication.ExitPlaymode();
            int result = code;
            EditorApplication.delayCall += () => { if (Application.isBatchMode) EditorApplication.Exit(result); };
        }

        private static void VerifyGrabAndSnap()
        {
            var actor = new GameObject("ManipulationTestPlayer");
            var adapter = actor.AddComponent<DesktopGrabInteractor>();
            var ray = actor.AddComponent<PlayerInteractor>();
            var camera = new GameObject("ManipulationTestCamera").AddComponent<Camera>();
            camera.transform.SetParent(actor.transform);
            camera.transform.position = Vector3.up * 1.6f;
            ray.Configure(camera, null);
            var item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.transform.position = new Vector3(0, 1.6f, 2);
            item.transform.localScale = Vector3.one * 0.2f;
            var body = item.AddComponent<Rigidbody>();
            var snap = item.AddComponent<SnapObject>();
            int grabbed = 0, released = 0;
            snap.Grabbed.AddListener(() => grabbed++);
            snap.Released.AddListener(() => released++);
            Physics.SyncTransforms();
            var start = item.transform.position;
            Require(ray.TryInteract() && snap.IsGrabbed && body.isKinematic, "Existing ray did not grab the item.");
            Require(Vector3.Distance(start, item.transform.position) < 0.00001f, "Grab teleported the item.");
            Require(!snap.TryBeginGrab(new GameObject("OtherActor").transform, new Pose()), "Concurrent grab ownership was accepted.");
            var target = new Vector3(0.25f, 1.4f, 1.25f);
            snap.UpdateGrab(new Pose(target, Quaternion.identity), 0.02f);
            Require(Vector3.Distance(item.transform.position, target) < Vector3.Distance(start, target), "Held item did not approach its offset pose.");
            Require(Vector3.Distance(item.transform.position, target) > 0.001f, "Smooth following snapped instantly.");
            Require(adapter.Release() && !snap.IsGrabbed && !body.isKinematic && body.useGravity, "Release failed to restore Rigidbody settings.");
            Require(grabbed == 1 && released == 1 && !adapter.Release(), "Grab/release events or repeated release were incorrect.");

            var socket = new GameObject("SnapSocketTest").AddComponent<SnapSocket>();
            socket.transform.SetPositionAndRotation(item.transform.position + Vector3.right * 0.05f, Quaternion.Euler(0, 40, 0));
            Require(snap.TrySnapNearby() && socket.Occupant == snap && snap.Socket == socket, "Nearby item did not snap with mutual occupancy.");
            Require(Vector3.Distance(item.transform.position, socket.SnapPoint.position) < 0.0001f && Quaternion.Angle(item.transform.rotation, socket.SnapPoint.rotation) < 0.001f, "Snap pose was not exact.");
            var second = new GameObject("OtherSnapObject").AddComponent<SnapObject>();
            second.transform.position = socket.transform.position;
            Require(!socket.TrySnap(second), "An occupied socket accepted another item.");
            Require(adapter.TryGrab(snap, camera.transform) && socket.Occupant == null && snap.Socket == null, "Grabbing did not detach the socket.");
            adapter.Release();
            Require(snap.Socket == socket, "Release near socket did not resnap.");
            socket.enabled = false;
            Require(snap.Socket == null && !body.isKinematic && body.useGravity, "Disabling socket left stale occupancy or Rigidbody state.");
            snap.enabled = false;
            second.enabled = false;
        }

        private static void VerifyConstraints()
        {
            var actor = new GameObject("ConstraintActor").transform;
            var frame = new GameObject("RotatedControlFrame").transform;
            frame.position = new Vector3(10, 1, 0);
            frame.rotation = Quaternion.Euler(10, 35, 0);
            var slider = Child<AxisSlider>(frame, "AxisSliderTest");
            int sliderEvents = 0;
            slider.ValueChanged.AddListener(_ => sliderEvents++);
            slider.SetNormalizedValue(0);
            var origin = slider.transform.position;
            var grab = new Pose(origin + Vector3.forward, Quaternion.identity);
            slider.TryBeginGrab(actor, grab);
            slider.UpdateGrab(new Pose(grab.position + frame.TransformVector(new Vector3(5, 2, 3)), Quaternion.identity), 0.02f);
            Require(Mathf.Approximately(slider.NormalizedValue, 1) && Vector3.Distance(frame.InverseTransformPoint(slider.transform.position), Vector3.right * 0.25f) < 0.0001f, "Slider did not enforce its local axis/end stop.");
            Require(sliderEvents == 1, "Slider value event was not emitted once.");
            slider.SetNormalizedValue(-5);
            Require(slider.NormalizedValue == 0, "Slider lower bound failed.");
            slider.EndGrab();

            var knob = Child<RotaryKnob>(frame, "RotaryKnobTest");
            knob.transform.localPosition = Vector3.up;
            var pivot = knob.transform.position;
            knob.TryBeginGrab(actor, new Pose(pivot + frame.right, Quaternion.identity));
            knob.UpdateGrab(new Pose(pivot + frame.up, Quaternion.identity), 0.02f);
            Require(Mathf.Abs(knob.Angle - 90) < 0.001f, "Knob did not track single-axis rotation.");
            knob.SetAngle(1000);
            Require(knob.Angle == 135 && Quaternion.Angle(knob.transform.rotation, frame.rotation * Quaternion.AngleAxis(135, Vector3.forward)) < 0.1f, "Knob angle bound or rotation axis failed.");
            knob.EndGrab();

            var lever = Child<LeverSwitch>(frame, "LeverSwitchTest");
            lever.transform.localPosition = Vector3.up * 2;
            pivot = lever.transform.position;
            int leverEvents = 0;
            lever.StateChanged.AddListener(_ => leverEvents++);
            lever.TryBeginGrab(actor, new Pose(pivot + frame.up, Quaternion.identity));
            var direction = Quaternion.AngleAxis(70, frame.right) * frame.up;
            lever.UpdateGrab(new Pose(pivot + direction, Quaternion.identity), 0.02f);
            Require(lever.State == 2 && leverEvents == 1, "Three-position lever did not reach its upper detent.");
            lever.EndGrab();
            var serialized = new SerializedObject(lever);
            serialized.FindProperty("stateCount").intValue = 2;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            lever.SetState(9);
            Require(lever.StateCount == 2 && lever.State == 1, "Two-position lever accepted an invalid detent.");
            lever.SetState(-2);
            Require(lever.State == 0, "Lever lower detent clamp failed.");
        }

        private static void VerifyConnections()
        {
            var a = new GameObject("PortA").AddComponent<ConnectionSocket>();
            var b = new GameObject("PortB").AddComponent<ConnectionSocket>();
            var c = new GameObject("PortC").AddComponent<ConnectionSocket>();
            Require(!a.TryConnect(a) && a.TryConnect(b), "Port self-rejection or connection failed.");
            Require(a.ConnectedTo == b && b.ConnectedTo == a && !c.TryConnect(b), "Port connection was not exclusive and symmetric.");
            b.enabled = false;
            Require(a.ConnectedTo == null && b.ConnectedTo == null, "Disabled port left stale peer state.");
            var serialized = new SerializedObject(c);
            serialized.FindProperty("connectionType").stringValue = "OtherType";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Require(!a.TryConnect(c), "Incompatible port types were accepted.");
        }

        private static T Child<T>(Transform parent, string name) where T : Component
        {
            var item = new GameObject(name);
            item.transform.SetParent(parent, false);
            return item.AddComponent<T>();
        }
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
