using System;
using System.Linq;
using EmbodiedPhysicsLab.Core;
using EmbodiedPhysicsLab.Core.Acceleration;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace EmbodiedPhysicsLab.Editor
{
    /// <summary>Scoped end-to-end Play Mode acceptance check; no scenes are saved.</summary>
    [InitializeOnLoad]
    public static class AccelerationPlayVerification
    {
        private const string Active="AccelerationV1.PlayCheck";
        static AccelerationPlayVerification(){if(SessionState.GetBool(Active,false))Attach();}
        private static void Attach(){EditorApplication.update-=Tick;EditorApplication.update+=Tick;}
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Lobby.unity",OpenSceneMode.Single);
            SessionState.SetBool(Active,true);SessionState.SetInt(Active+"Phase",0);SessionState.SetInt(Active+"Frames",0);SessionState.SetFloat(Active+"Start",(float)EditorApplication.timeSinceStartup);Attach();EditorApplication.EnterPlaymode();
        }
        private static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
        private static void Tick()
        {
            if(!SessionState.GetBool(Active,false))return;
            try
            {
                if(EditorApplication.timeSinceStartup-SessionState.GetFloat(Active+"Start",0)>120)throw new Exception("Play verification timeout.");
                if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
                int frames=SessionState.GetInt(Active+"Frames",0)+1;SessionState.SetInt(Active+"Frames",frames);if(frames<25)return;
                int phase=SessionState.GetInt(Active+"Phase",0);var c=Object.FindFirstObjectByType<AccelerationExperimentController>();
                if(phase==0)
                {
                    var door=Object.FindObjectsByType<SceneDoorInteractable>(FindObjectsSortMode.None).First(d=>d.DestinationScene=="AccelerationLab");
                    SessionState.SetInt(Active+"Phase",1);SessionState.SetInt(Active+"Frames",0);
                    Aim(door.transform.position+Vector3.up*1.4f,new Vector3(0,1.63f,25.5f));Debug.Log("PLAY_CHECK Lobby -> AccelerationLab via IInteractable");
                }
                else if(phase==1&&SceneManager.GetActiveScene().name=="AccelerationLab"&&c!=null)
                {
                    var player=GameObject.Find("DesktopPlayer");Require(player.GetComponentInChildren<Animator>().isHuman,"Humanoid missing in laboratory.");
                    var cc=player.GetComponent<CharacterController>();float z=player.transform.position.z;cc.Move(Vector3.forward);Require(player.transform.position.z>z+.9f,"Player approach path blocked.");cc.Move(Vector3.down*.3f);Require(player.transform.position.y>-.1f,"Ground collision failed.");
                    Click(AccelerationCommand.CycleAngle);Click(AccelerationCommand.CycleFriction);Click(AccelerationCommand.AddCartWeight);Click(AccelerationCommand.AddCartWeight);Click(AccelerationCommand.RemoveCartWeight);Click(AccelerationCommand.AddBucketWeight);Click(AccelerationCommand.AddBucketWeight);Click(AccelerationCommand.RemoveBucketWeight);
                    Require(c.State.CartWeights==1&&c.State.BucketWeights==1&&c.State.Angle==2&&c.State.Friction==.02,"Physical parameter controls failed.");
                    var visual=c.GetComponent<AccelerationExperimentVisual>();Require(visual.Cart.Find("CartWeights").Cast<Transform>().Count(t=>t.gameObject.activeSelf)==1,"Cart weight model missing.");Require(visual.Bucket.Find("BucketWeights").Cast<Transform>().Count(t=>t.gameObject.activeSelf)==1,"Bucket weight model missing.");
                    SessionState.SetFloat(Active+"BucketY",visual.Bucket.position.y);Click(AccelerationCommand.Start);Require(c.State.Phase==AccelerationPhase.Running,"Start interaction failed.");Require(!c.ChangeCartWeights(1),"Run-time parameter lock failed.");
                    SessionState.SetFloat(Active+"FirstA",(float)c.State.Acceleration);SessionState.SetInt(Active+"Phase",2);Debug.Log("PLAY_CHECK physical A/B/C controls, visible weights, Start and lock passed");
                }
                else if(phase==2&&c!=null&&c.State.Phase==AccelerationPhase.Completed)
                {
                    var visual=c.GetComponent<AccelerationExperimentVisual>();AccelerationLabBuilder.AssertClose(visual.Cart.localPosition.x-c.Settings.cartStartDistance,c.State.Position,1e-5);AccelerationLabBuilder.AssertClose(SessionState.GetFloat(Active+"BucketY",0)-visual.Bucket.position.y,c.State.Position,1e-5);
                    Require(c.Recorder.Samples.Count>10,"50 Hz samples missing.");for(int i=0;i<c.Recorder.Samples.Count;i++)AccelerationLabBuilder.AssertClose(c.Recorder.Samples[i].Time,i*.02);
                    Require(Object.FindFirstObjectByType<AccelerationDataDisplay>().GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("Completed")),"Live final readout missing.");
                    SessionState.SetFloat(Active+"FirstT",(float)c.State.Time);Click(AccelerationCommand.Record);Require(c.Recorder.SavedResults.Count==1,"Result recording failed.");Click(AccelerationCommand.Reset);Require(c.State.Phase==AccelerationPhase.Ready&&c.State.Position==0&&c.Recorder.Samples.Count==0,"Reset incomplete.");
                    Click(AccelerationCommand.CycleAngle);Click(AccelerationCommand.CycleAngle);Click(AccelerationCommand.CycleFriction);Click(AccelerationCommand.CycleFriction);Click(AccelerationCommand.Start);Require(c.State.Phase==AccelerationPhase.Ready&&c.State.Force<=0,"Insufficient-force rejection failed.");
                    Click(AccelerationCommand.Reset);Click(AccelerationCommand.AddBucketWeight);Click(AccelerationCommand.Start);Require(c.State.Acceleration>SessionState.GetFloat(Active+"FirstA",0),"Parameter changes did not change motion.");SessionState.SetInt(Active+"Phase",3);Debug.Log("PLAY_CHECK automatic finish, rope-linked visuals, live results, 50Hz, record, Reset, F<=0 rejection passed");
                }
                else if(phase==3&&c!=null&&c.State.Phase==AccelerationPhase.Completed)
                {
                    Require(c.State.Time<SessionState.GetFloat(Active+"FirstT",0),"Second run did not react to different parameters.");Click(AccelerationCommand.Reset);Require(c.State.Time==0&&c.State.Velocity==0&&c.State.Position==0&&c.Recorder.Samples.Count==0,"Second Reset failed.");
                    var exit=Object.FindObjectsByType<SceneDoorInteractable>(FindObjectsSortMode.None).First(d=>d.DestinationScene=="Lobby");SessionState.SetInt(Active+"Phase",4);SessionState.SetInt(Active+"Frames",0);Aim(exit.transform.position+Vector3.up*1.4f,new Vector3(0,1.63f,1.6f));
                }
                else if(phase==4&&SceneManager.GetActiveScene().name=="Lobby")
                {
                    Require(GameObject.Find("DesktopPlayer")!=null,"Return to Lobby player failed.");Debug.Log("ACCELERATION_PLAY_ACCEPTANCE_PASS: Lobby entry, physical controls, two different runs, auto-finish, records, Reset, return to Lobby.");SessionState.SetBool(Active,false);EditorApplication.Exit(0);
                }
            }
            catch(Exception e){Debug.LogException(e);SessionState.SetBool(Active,false);EditorApplication.Exit(1);}
        }
        private static void Click(AccelerationCommand command)
        {
            var action=Object.FindObjectsByType<AccelerationInteractable>(FindObjectsSortMode.None).First(a=>a.name=="Control_"+command);
            var target=action.transform.position;Aim(target,new Vector3(target.x,1.63f,target.z-1.15f));
        }
        private static void Aim(Vector3 target,Vector3 origin)
        {
            var player=GameObject.Find("DesktopPlayer");var camera=player.GetComponentInChildren<Camera>();player.transform.rotation=Quaternion.identity;player.transform.position=origin-camera.transform.localPosition;
            camera.transform.rotation=Quaternion.LookRotation(target-origin);Physics.SyncTransforms();Require(player.GetComponent<PlayerInteractor>().TryInteract(),"Player ray did not reach target at "+target);
        }
    }
}
