using System;
using System.Linq;
using EmbodiedPhysicsLab.Core;
using EmbodiedPhysicsLab.Core.Gas;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace EmbodiedPhysicsLab.Editor
{
    [InitializeOnLoad]
    public static class GasPhysicalVerification
    {
        private const string RunningKey="GasPhysicalVerification.Running";
        private static GasExperimentController experiment;
        private static GasEquipmentInteraction equipment;
        private static GasExperimentVisual visual;
        private static GameObject player;
        private static Camera camera;
        private static DesktopGrabInteractor grab;
        private static int initialFrame;
        private static double returnDeadline;
        static GasPhysicalVerification(){EditorApplication.playModeStateChanged+=OnMode;}
        public static void Run(){EditorSceneManager.OpenScene(GasLabBuilder.Lab,OpenSceneMode.Single);SessionState.SetBool(RunningKey,true);EditorApplication.EnterPlaymode();}
        private static void OnMode(PlayModeStateChange mode)
        {
            if(mode==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(RunningKey,false)){initialFrame=Time.frameCount;EditorApplication.update+=WaitForStartup;}
        }
        private static void WaitForStartup(){if(Time.frameCount<initialFrame+2)return;EditorApplication.update-=WaitForStartup;Verify();}
        private static void Verify()
        {
            int result=0;
            try
            {
                experiment=Object.FindFirstObjectByType<GasExperimentController>();equipment=experiment.GetComponent<GasEquipmentInteraction>();visual=experiment.GetComponent<GasExperimentVisual>();
                player=GameObject.Find("DesktopPlayer");camera=player.GetComponentInChildren<Camera>();grab=player.GetComponent<DesktopGrabInteractor>();
                Require(equipment!=null&&grab!=null,"Physical gas apparatus missing.");
                Require(!Object.FindObjectsByType<GasInteractable>(FindObjectsSortMode.None).Any(x=>x.Command!=GasCommand.Record&&x.Command!=GasCommand.Reset),"Legacy gas parameter buttons remain.");
                Require(player.GetComponentInChildren<Animator>()?.isHuman==true,"Shared Humanoid missing.");
                var cc=player.GetComponent<CharacterController>();float z=player.transform.position.z;cc.Move(Vector3.forward*.5f);Require(player.transform.position.z>z+.4f,"Desktop movement failed.");
                VerifyReset();
                var top=GameObject.Find("GasExperiment/ExperimentalApparatus/BenchTop").GetComponent<Renderer>().bounds.max.y;
                Require(GameObject.Find("CylinderBase").GetComponent<Renderer>().bounds.min.y>=top-.001f,"Cylinder base intersects the table.");
                double pressure=experiment.State.PressurePa,temperature=experiment.State.TemperatureK;
                DragPiston(1);Require(experiment.State.PressurePa<pressure&&experiment.State.TemperatureK<temperature,"Piston movement did not affect volume, pressure and temperature.");
                Require(!experiment.Record(),"Unstable gas was recorded.");
                SelectMode(2);double before=experiment.State.TemperatureK;experiment.Advance(.5);Require(experiment.State.TemperatureK>before,"Physical heating lever had no effect.");
                SelectMode(0);before=experiment.State.TemperatureK;experiment.Advance(.5);Require(experiment.State.TemperatureK<before,"Physical cooling lever had no effect.");
                SelectMode(1);Settle();Record();
                DragPiston(2);Settle();Record();
                DragPiston(3);Settle();Record();
                DragPiston(4);Settle();Record();
                Require(experiment.Recorder.DistinctVolumeCount==4&&experiment.State.Phase==GasPhase.EnoughData,"Four distinct piston settings were not recorded.");
                var pv=experiment.Recorder.Points.Select(x=>x.PTimesV).ToArray();Require(pv.Max()-pv.Min()<pv.Average()*.01,"Isothermal pV variation exceeds one percent.");
                var graph=GameObject.Find("PVDataCurve").GetComponent<LineRenderer>();Require(graph.positionCount==4,"P-V curve missing recorded points.");
                Require(!experiment.Record(),"Duplicate volume was recorded twice.");
                // Exercise the reverse stroke and real end stops, not a controller-only setter.
                DragPiston(0);DragBeyondStops(-10);Require(experiment.VolumeIndex==0,"Lower piston stop failed.");
                DragBeyondStops(10);Require(experiment.VolumeIndex==experiment.Settings.VolumeCount-1,"Upper piston stop failed.");
                Grab(equipment.PistonSlider.transform,new Vector3(0,.025f,-.64f));
                experiment.ResetExperiment();Require(grab.HeldObject==null,"Reset left the physical piston held.");VerifyReset();
                Require(graph.positionCount==0,"Reset retained the P-V graph.");
                DragPiston(2);SelectMode(2);experiment.Advance(.3);SelectMode(1);Settle();Record();
                Require(experiment.Recorder.Points.Count==1,"Second round required scene reload.");
                Click(GasCommand.Reset);VerifyReset();
                var exit=Object.FindObjectsByType<SceneDoorInteractable>(FindObjectsSortMode.None).First(x=>x.DestinationScene=="Lobby");
                Require(exit.GetComponent<SceneNavigator>()!=null,"Return door navigator missing.");
                player.transform.rotation=Quaternion.identity;player.transform.position=new Vector3(0,.04f,1.6f);
                camera.transform.rotation=Quaternion.LookRotation(exit.transform.position+Vector3.up*1.4f-camera.transform.position);
                Physics.SyncTransforms();returnDeadline=EditorApplication.timeSinceStartup+30;EditorApplication.update+=WaitForLobby;
                Require(player.GetComponent<PlayerInteractor>().TryInteract(),"Return door interaction ray missed.");
                return;
            }
            catch(Exception e){Debug.LogException(e);result=1;}
            Finish(result);
        }
        private static void WaitForLobby()
        {
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="Lobby")
            {
                EditorApplication.update-=WaitForLobby;
                if(GameObject.Find("DesktopPlayer")==null){Debug.LogError("Lobby player missing after return.");Finish(1);return;}
                Debug.Log("GAS_PHYSICAL_VERIFIED: ray grab of actual piston and 3-position thermal lever, 5 volume detents, both end stops, pressure/temperature response, unchanged PV=nRT, heat/cool/hold, stability gate, 4 distinct records and PV graph, pV within 1%, full reset including held controls, second round, shared Humanoid and actual return to Lobby.");
                Finish(0);
            }
            else if(EditorApplication.timeSinceStartup>returnDeadline){Debug.LogError("Return to Lobby timed out.");Finish(1);}
        }
        private static void Finish(int result)
        {
            EditorApplication.update-=WaitForLobby;
            SessionState.SetBool(RunningKey,false);EditorApplication.ExitPlaymode();int code=result;EditorApplication.delayCall+=()=>EditorApplication.Exit(code);
        }
        private static void VerifyReset()
        {
            Require(experiment.VolumeIndex==0&&experiment.Recorder.Points.Count==0&&experiment.State.Mode==ThermalMode.HoldConstant,"Gas reset state incomplete.");
            Near(experiment.State.TemperatureK,experiment.Settings.defaultTemperature);Near(equipment.PistonSlider.NormalizedValue,0);Require(equipment.ThermalLever.State==1,"Thermal selector did not reset to middle detent.");VerifyGeometry();
        }
        private static void VerifyGeometry()
        {
            float y=Mathf.Lerp(experiment.Settings.pistonMinY,experiment.Settings.pistonMaxY,(float)(experiment.State.VolumeM3/experiment.Settings.VolumeM3(experiment.Settings.VolumeCount-1)));
            Near(visual.Piston.localPosition.y,y,1e-5);Near(equipment.PistonSlider.transform.position.y,visual.Piston.position.y,1e-5);
            var pointer=equipment.PistonSlider.transform.Find("VolumeReadPointer/PointerTip");
            var tick=GameObject.Find("PistonVolumeScale").transform.Find("VolumeTick_"+experiment.VolumeIndex);
            Require(pointer!=null&&tick!=null,"Volume scale pointer or tick missing.");
            Near(pointer.position.y,tick.position.y,1e-5);Near(pointer.position.z,tick.position.z,1e-5);
            var label=GameObject.Find("VolumeLabel_"+experiment.VolumeIndex).transform.parent;
            Near(label.position.y,tick.position.y,1e-5);
            var gas=visual.GasVolume.GetComponent<Renderer>().bounds;
            Require(gas.max.y<=visual.Piston.position.y+.001f&&gas.min.y>=visual.Piston.parent.position.y+experiment.Settings.cylinderBottomY,"Gas volume mesh escapes its piston chamber.");
        }
        private static void Settle(){for(int i=0;i<300&&!experiment.State.IsothermalReady;i++)experiment.Advance(.05);Require(experiment.State.IsothermalReady,"Thermostat did not settle.");}
        private static void Record(){int n=experiment.Recorder.Points.Count;Click(GasCommand.Record);Require(experiment.Recorder.Points.Count==n+1,"Ray record button did not save data.");}
        private static void Click(GasCommand command){var target=Object.FindObjectsByType<GasInteractable>(FindObjectsSortMode.None).First(x=>x.Command==command);Aim(target.transform,Vector3.up*.04f);Require(player.GetComponent<PlayerInteractor>().TryInteract(),"Missed physical "+command+" pushbutton.");}
        private static void DragPiston(int index)
        {
            var slider=equipment.PistonSlider;Grab(slider.transform,new Vector3(0,.025f,-.64f));
            float low=(float)experiment.Settings.VolumeM3(0),high=(float)experiment.Settings.VolumeM3(experiment.Settings.VolumeCount-1);
            float next=Mathf.InverseLerp(low,high,(float)experiment.Settings.VolumeM3(index));
            float lowerY=Mathf.Lerp(experiment.Settings.pistonMinY,experiment.Settings.pistonMaxY,low/high);
            var pose=OperationPose();float dy=(next-slider.NormalizedValue)*(experiment.Settings.pistonMaxY-lowerY);
            slider.UpdateGrab(new Pose(pose.position+Vector3.up*dy,pose.rotation),.02f);grab.Release();
            Require(experiment.VolumeIndex==index,"Physical piston selected wrong volume detent.");VerifyGeometry();
        }
        private static void DragBeyondStops(float metres){var slider=equipment.PistonSlider;Grab(slider.transform,new Vector3(0,.025f,-.64f));var pose=OperationPose();slider.UpdateGrab(new Pose(pose.position+Vector3.up*metres,pose.rotation),.02f);grab.Release();VerifyGeometry();}
        private static void SelectMode(int state)
        {
            var lever=equipment.ThermalLever;if(lever.State==state)return;
            Grab(lever.transform,lever.transform.TransformVector(Vector3.up*.20f));
            var pose=OperationPose();var vector=pose.position-lever.transform.position;float degrees=(state-lever.State)*40;
            lever.UpdateGrab(new Pose(lever.transform.position+Quaternion.AngleAxis(degrees,Vector3.forward)*vector,pose.rotation),.02f);grab.Release();
            Require(lever.State==state,"Physical thermostat selected wrong detent.");
            Require(experiment.State.Mode==(state==0?ThermalMode.Cooling:state==2?ThermalMode.Heating:ThermalMode.HoldConstant),"Lever did not control experiment thermal mode.");
        }
        private static void Grab(Transform target,Vector3 offset){Aim(target,offset);Require(player.GetComponent<PlayerInteractor>().TryInteract()&&ReferenceEquals(grab.HeldObject,target.GetComponent<IGrabbable>()),"Ray did not grab intended apparatus: "+target.name);}
        private static void Aim(Transform target,Vector3 offset){player.transform.rotation=Quaternion.identity;player.transform.position=new Vector3(target.position.x,.04f,target.position.z-1.50f);camera.transform.rotation=Quaternion.LookRotation(target.position+offset-camera.transform.position);Physics.SyncTransforms();}
        private static Pose OperationPose()=>new Pose(camera.transform.TransformPoint(new Vector3(.25f,-.23f,.95f)),camera.transform.rotation);
        private static void Near(double value,double expected,double tolerance=1e-7)=>Require(Math.Abs(value-expected)<tolerance,$"Expected {expected}, got {value}.");
        private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    }
}
