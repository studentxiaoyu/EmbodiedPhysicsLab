using System;
using System.IO;
using System.Linq;
using EmbodiedPhysicsLab.Core;
using EmbodiedPhysicsLab.Core.Acceleration;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace EmbodiedPhysicsLab.Editor
{
    public static class AccelerationLabBuilder
    {
        public const string Lab="Assets/_Project/Scenes/AccelerationLab.unity";
        private const string PlayerPath="Assets/_Project/Prefabs/Players/DesktopPlayer.prefab";
        private const string HudPath="Assets/_Project/Prefabs/Players/DesktopInteractionHUD.prefab";
        private const string SettingsPath="Assets/_Project/Experiments/Acceleration/AccelerationSettings.asset";
        private static TMP_FontAsset font;
        private static Material wall,floor,wood,steel,cartMat,bucketMat,screen,green,red,blue,ropeMat;
        private static AccelerationExperimentController controller;
        private static AccelerationExperimentSettings settings;
        [MenuItem("Embodied Physics Lab/Build AccelerationLab v1")]
        public static void Build()
        {
            Directory.CreateDirectory("Assets/_Project/Experiments/Acceleration");Directory.CreateDirectory("Assets/_Project/Materials/Acceleration");AssetDatabase.Refresh();
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/NotoSansSC SDF.asset");if(font==null)throw new IOException("Chinese TMP font missing.");
            MakePlayerPrefabs();
            var scene=EditorSceneManager.OpenScene(Lab,OpenSceneMode.Single);
            foreach(var root in scene.GetRootGameObjects())Object.DestroyImmediate(root);
            settings=AssetDatabase.LoadAssetAtPath<AccelerationExperimentSettings>(SettingsPath);
            if(settings==null){settings=ScriptableObject.CreateInstance<AccelerationExperimentSettings>();AssetDatabase.CreateAsset(settings,SettingsPath);}
            MakeMaterials();Room();
            var experiment=new GameObject("AccelerationExperiment");var recorder=experiment.AddComponent<AccelerationDataRecorder>();controller=experiment.AddComponent<AccelerationExperimentController>();controller.Configure(settings,recorder);
            Rig(experiment.transform);Controls();Display();
            var player=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath));player.name="DesktopPlayer";player.transform.position=new Vector3(0,.04f,1.3f);
            var hud=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HudPath));hud.name="Interaction HUD";
            var prompt=hud.GetComponentsInChildren<TMP_Text>(true).First(t=>t.name=="Interaction Prompt");player.GetComponent<PlayerInteractor>().Configure(player.GetComponentInChildren<Camera>(),prompt,3.5f);
            var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            var sun=new GameObject("Directional Light").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=.95f;sun.transform.rotation=Quaternion.Euler(40,-20,0);sun.shadows=LightShadows.Soft;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.62f,.65f,.67f);
            AccelerationPhysicalInteractionBuilder.ApplyToOpenScene();
            EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("AccelerationLab save failed.");AssetDatabase.SaveAssets();
            Verify();Debug.Log("ACCELERATION_BUILD_COMPLETE");
        }
        public static void BuildAndCapture(){Build();Capture();}
        private static void MakePlayerPrefabs()
        {
            if(File.Exists(PlayerPath)&&File.Exists(HudPath))return;
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Lobby.unity",OpenSceneMode.Single);
            var source=GameObject.Find("DesktopPlayer");var sourceHud=GameObject.Find("Interaction HUD");if(source==null||sourceHud==null)throw new IOException("Lobby player/HUD missing.");
            var copy=Object.Instantiate(source);copy.name="DesktopPlayer";copy.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            var camera=copy.GetComponentInChildren<Camera>();copy.GetComponent<PlayerInteractor>().Configure(camera,null,3.5f);
            copy.GetComponent<DesktopFirstPersonController>().Configure(copy.GetComponent<CharacterController>(),camera.transform);
            PrefabUtility.SaveAsPrefabAsset(copy,PlayerPath);Object.DestroyImmediate(copy);
            var hud=Object.Instantiate(sourceHud);hud.name="DesktopInteractionHUD";PrefabUtility.SaveAsPrefabAsset(hud,HudPath);Object.DestroyImmediate(hud);
        }
        private static Material Mat(string name,Color color,bool unlit=false)
        {
            string path="Assets/_Project/Materials/Acceleration/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;m.SetFloat("_Smoothness",.25f);EditorUtility.SetDirty(m);return m;
        }
        private static void MakeMaterials()
        {
            wall=Mat("Plaster",new Color(.79f,.82f,.81f));floor=Mat("Floor",new Color(.44f,.49f,.51f));wood=Mat("BoardWood",new Color(.55f,.36f,.18f));steel=Mat("Steel",new Color(.32f,.37f,.40f));
            cartMat=Mat("CartBlue",new Color(.12f,.31f,.48f));bucketMat=Mat("SandBucket",new Color(.54f,.27f,.14f));screen=Mat("DataScreen",new Color(.03f,.07f,.10f));green=Mat("StartGreen",new Color(.08f,.42f,.23f));red=Mat("ResetRed",new Color(.53f,.13f,.12f));blue=Mat("ControlBlue",new Color(.11f,.32f,.51f));ropeMat=Mat("Rope",new Color(.07f,.07f,.06f),true);
        }
        private static Transform Group(string name,Transform parent=null){var t=new GameObject(name).transform;if(parent!=null)t.SetParent(parent,false);return t;}
        private static GameObject Part(Transform parent,string name,Vector3 position,Vector3 size,Material material,PrimitiveType type=PrimitiveType.Cube,bool collide=true)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;if(!collide)Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        private static TMP_Text Label(Transform parent,string value,Vector3 position,float width,float height,int size,Quaternion? rotation=null)
        {
            var go=new GameObject("Label | "+value.Split('\n')[0],typeof(RectTransform),typeof(Canvas));go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localRotation=rotation??Quaternion.identity;go.transform.localScale=Vector3.one*.004f;
            go.GetComponent<RectTransform>().sizeDelta=new Vector2(width*250,height*250);go.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            var child=new GameObject("Text",typeof(RectTransform),typeof(TextMeshProUGUI));child.transform.SetParent(go.transform,false);var r=child.GetComponent<RectTransform>();r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
            var text=child.GetComponent<TextMeshProUGUI>();text.font=font;text.text=value;text.fontSize=size;text.alignment=TextAlignmentOptions.Center;text.color=Color.white;text.raycastTarget=false;return text;
        }
        private static void Room()
        {
            var room=Group("LaboratoryRoom");Part(room,"Floor",new Vector3(0,-.1f,5.5f),new Vector3(14,.2f,11),floor);
            foreach(var x in new[]{-7f,7f})Part(room,"SideWall",new Vector3(x,2.2f,5.5f),new Vector3(.2f,4.4f,11),wall);
            Part(room,"BackWall",new Vector3(0,2.2f,11),new Vector3(14,4.4f,.2f),wall);
            foreach(var x in new[]{-4.15f,4.15f})Part(room,"FrontWall",new Vector3(x,2.2f,0),new Vector3(5.7f,4.4f,.2f),wall);
            Part(room,"DoorLintel",new Vector3(0,3.6f,0),new Vector3(2.6f,1.6f,.2f),wall);
            var ceiling=Part(room,"Ceiling",new Vector3(0,4.5f,5.5f),new Vector3(14,.2f,11),wall);ceiling.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            foreach(var x in new[]{-4f,4f})foreach(var z in new[]{3f,8f}){Part(room,"CeilingFixture",new Vector3(x,4.35f,z),new Vector3(1.3f,.07f,.45f),wall,PrimitiveType.Cube,false);var light=Group("CeilingLight",room);light.position=new Vector3(x,4.1f,z);var l=light.gameObject.AddComponent<Light>();l.type=LightType.Point;l.range=8;l.intensity=2;}
            var exit=Group("ExitToLobby");exit.position=new Vector3(0,0,.12f);exit.rotation=Quaternion.Euler(0,180,0);Part(exit,"DoorPanel",new Vector3(0,1.4f,0),new Vector3(2.4f,2.8f,.14f),wood);
            var door=exit.gameObject.AddComponent<SceneDoorInteractable>();door.DestinationName="实验大厅";door.DestinationScene="Lobby";exit.GetComponent<SceneNavigator>().TargetScene="Lobby";Label(exit,"返回实验大厅",new Vector3(0,1.8f,-.10f),2.2f,.5f,50);
            Label(room,"力学实验室\n探究加速度与力、质量的关系",new Vector3(0,3.75f,10.84f),7f,.8f,64);
            for(int i=0;i<3;i++)Part(room,"EquipmentCabinet",new Vector3(-5.6f,.9f,7.2f+i*1.0f),new Vector3(1.3f,1.8f,.8f),wood);
            Label(room,"理想模型说明\n轻绳 · 理想滑轮 · 常数动摩擦\n初速为零，沙桶下降牵引小车上坡\nF = mg − Mg sinθ − μMg cosθ\na = F / (M + m)",new Vector3(-4.8f,2.65f,10.84f),3.4f,1.65f,33);
        }
        private static void Table(Transform root,Vector3 pos,Vector2 size,float height)
        {
            Part(root,"TableTop",pos+Vector3.up*height,new Vector3(size.x,.12f,size.y),wood);
            foreach(var x in new[]{-.42f,.42f})foreach(var z in new[]{-.38f,.38f})Part(root,"TableLeg",pos+new Vector3(size.x*x,height/2,size.y*z),new Vector3(.07f,height,.07f),steel);
        }
        private static LineRenderer Line(Transform parent,string name,Material material,float width)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);var line=go.AddComponent<LineRenderer>();line.sharedMaterial=material;line.startWidth=line.endWidth=width;line.useWorldSpace=true;return line;
        }
        private static void Rig(Transform parent)
        {
            var rig=Group("ExperimentalApparatus",parent);var bench=Group("ExperimentTable",rig);Table(bench,new Vector3(-.2f,0,5.0f),new Vector2(3.5f,1),1.04f);
            var track=Group("LongBoard_Track",rig);track.localPosition=new Vector3(-settings.pulleyDistance/2,settings.trackPivotHeight,5.0f);
            Part(track,"LongWoodenBoard",new Vector3(settings.pulleyDistance/2,-.045f,0),new Vector3(settings.pulleyDistance,.12f,.62f),wood);
            foreach(var z in new[]{-.20f,.20f})Part(track,"StraightRail",new Vector3(settings.pulleyDistance/2,.04f,z),new Vector3(settings.pulleyDistance,.05f,.035f),steel,PrimitiveType.Cube,false);
            var cart=Group("Cart",track);Part(cart,"CartChassis",new Vector3(0,.01f,0),new Vector3(.44f,.14f,.34f),cartMat);cart.gameObject.AddComponent<AccelerationInteractable>().Configure(controller,AccelerationCommand.RemoveCartWeight);
            foreach(var x in new[]{-.14f,.14f})foreach(var z in new[]{-.19f,.19f}){var w=Part(cart,"CartWheel",new Vector3(x,-.06f,z),new Vector3(.115f,.025f,.115f),steel,PrimitiveType.Cylinder,false);w.transform.localRotation=Quaternion.Euler(90,0,0);}
            var cw=Group("CartWeights",cart);cw.localPosition=new Vector3(0,.115f,0);
            var pulley=Group("Pulley",track);pulley.localPosition=new Vector3(settings.pulleyDistance,settings.pulleyHeight,0);var wheel=Part(pulley,"PulleyWheel",Vector3.zero,new Vector3(.20f,.035f,.20f),steel,PrimitiveType.Cylinder,false);wheel.transform.localRotation=Quaternion.Euler(90,0,0);
            Part(track,"PulleyBracket",new Vector3(settings.pulleyDistance,.06f,.12f),new Vector3(.08f,.38f,.055f),steel);
            var bucket=Group("HangingBucket",rig);Part(bucket,"BucketBottom",new Vector3(0,-.12f,0),new Vector3(.30f,.025f,.30f),bucketMat,PrimitiveType.Cylinder,false);
            for(int i=0;i<12;i++){float a=i*30*Mathf.Deg2Rad;var panel=Part(bucket,"BucketSide",new Vector3(Mathf.Cos(a)*.14f,0,Mathf.Sin(a)*.14f),new Vector3(.079f,.24f,.025f),bucketMat,PrimitiveType.Cube,false);panel.transform.localRotation=Quaternion.Euler(0,90-i*30,0);}
            var handle=Line(bucket,"BucketHandle",steel,.012f);handle.useWorldSpace=false;handle.positionCount=9;for(int i=0;i<9;i++){float a=i*Mathf.PI/8;handle.SetPosition(i,new Vector3(Mathf.Cos(a)*.12f,.11f+Mathf.Sin(a)*.16f,0));}
            var bw=Group("BucketWeights",bucket);bw.localPosition=new Vector3(0,-.08f,0);
            var rope=Line(rig,"Rope",ropeMat,.007f);
            var visual=parent.gameObject.AddComponent<AccelerationExperimentVisual>();visual.Configure(controller,track,cart,pulley,bucket,cw,bw,rope,steel);
        }
        private static GameObject Control(Transform parent,AccelerationCommand command,Vector3 pos,string text,Material material)
        {
            var root=Group("Control_"+command,parent);root.localPosition=pos;
            Part(root,"ButtonBody",Vector3.zero,new Vector3(.45f,.24f,.13f),material);root.gameObject.AddComponent<AccelerationInteractable>().Configure(controller,command);
            Label(root,text,new Vector3(0,0,-.071f),.41f,.19f,22);return root.gameObject;
        }
        private static void Controls()
        {
            var a=Group("A_EnvironmentControl");Table(a,new Vector3(-3.1f,0,4.4f),new Vector2(1.25f,.75f),1f);
            Label(a,"A · 环境控制\n倾角 / 摩擦条件",new Vector3(-3.1f,1.65f,4.65f),1.7f,.55f,33);
            Control(a,AccelerationCommand.CycleAngle,new Vector3(-3.4f,1.15f,4.06f),"倾角循环",blue);Control(a,AccelerationCommand.CycleFriction,new Vector3(-2.8f,1.15f,4.06f),"摩擦循环",blue);
            var b=Group("B_CartMassControl");Table(b,new Vector3(-1.7f,0,3.35f),new Vector2(1.3f,.7f),.92f);Label(b,"B · 小车质量\n每枚 "+settings.cartWeightMass.ToString("F2")+" kg",new Vector3(-1.7f,1.6f,3.55f),1.6f,.5f,32);
            Control(b,AccelerationCommand.AddCartWeight,new Vector3(-2.0f,1.10f,3.05f),"添加砝码",green);Control(b,AccelerationCommand.RemoveCartWeight,new Vector3(-1.4f,1.10f,3.05f),"移除砝码",red);
            for(int i=0;i<6;i++){var token=Part(b,"AvailableCartWeight",new Vector3(-2.15f+i*.17f,1.02f,3.43f),new Vector3(.12f,.035f,.12f),steel,PrimitiveType.Cylinder);token.AddComponent<AccelerationInteractable>().Configure(controller,AccelerationCommand.AddCartWeight);}
            var c=Group("C_BucketMassControl");Table(c,new Vector3(2.8f,0,4.7f),new Vector2(1.3f,.9f),1f);Label(c,"C · 沙桶质量\n每枚 "+settings.bucketWeightMass.ToString("F2")+" kg",new Vector3(2.8f,1.65f,4.95f),1.7f,.55f,32);
            Control(c,AccelerationCommand.AddBucketWeight,new Vector3(2.5f,1.16f,4.28f),"添加配重",green);Control(c,AccelerationCommand.RemoveBucketWeight,new Vector3(3.1f,1.16f,4.28f),"移除配重",red);
            for(int i=0;i<6;i++){var token=Part(c,"AvailableBucketWeight",new Vector3(2.35f+i*.17f,1.08f,4.85f),new Vector3(.12f,.025f,.12f),steel,PrimitiveType.Cylinder);token.AddComponent<AccelerationInteractable>().Configure(controller,AccelerationCommand.AddBucketWeight);}
            var controls=Group("ExperimentControl");Control(controls,AccelerationCommand.Start,new Vector3(-.55f,1.05f,4.44f),"开始",green);Control(controls,AccelerationCommand.Reset,new Vector3(.05f,1.05f,4.44f),"Reset",red);Control(controls,AccelerationCommand.Record,new Vector3(.65f,1.05f,4.44f),"记录结果",blue);
            Label(controls,"完成后：记录结果 → Reset → 更换参数",new Vector3(0,1.45f,4.9f),2.6f,.35f,28);
        }
        private static void Display()
        {
            var root=Group("DataDisplay");Part(root,"DisplayFrame",new Vector3(0,2.55f,6.1f),new Vector3(3.9f,1.85f,.12f),steel);Part(root,"ScreenPanel",new Vector3(0,2.55f,6.01f),new Vector3(3.76f,1.72f,.04f),screen,PrimitiveType.Cube,false);
            var text=Label(root,"",new Vector3(-.56f,2.55f,5.97f),2.55f,1.60f,27);text.enableAutoSizing=true;text.fontSizeMax=27;text.fontSizeMin=18;
            var graph=Group("VelocityTimePlot",root);graph.localPosition=new Vector3(.65f,2.06f,5.96f);var line=Line(graph,"v-t samples",Mat("GraphCyan",Color.cyan,true),.008f);line.useWorldSpace=false;
            Part(graph,"TimeAxis",new Vector3(.475f,0,.015f),new Vector3(.95f,.008f,.008f),steel,PrimitiveType.Cube,false);Part(graph,"VelocityAxis",new Vector3(0,.19f,.015f),new Vector3(.008f,.38f,.008f),steel,PrimitiveType.Cube,false);
            Label(root,"v-t 曲线\n横轴 t / s\n纵轴 v / (m/s)\n50 Hz 位移采样",new Vector3(1.11f,2.91f,5.96f),1.1f,.70f,24);
            root.gameObject.AddComponent<AccelerationDataDisplay>().Configure(controller,text,line);
        }
        public static void Verify()
        {
            if(SceneManager.GetActiveScene().path!=Lab)EditorSceneManager.OpenScene(Lab,OpenSceneMode.Single);
            var c=Object.FindFirstObjectByType<AccelerationExperimentController>();var v=c.GetComponent<AccelerationExperimentVisual>();c.Configure(c.Settings,c.Recorder);
            var baseline=AccelerationPhysicsModel.Evaluate(.5,.05,0,0,9.81);AssertClose(baseline.Force,.4905);AssertClose(baseline.Acceleration,.8918181818181818);
            var slow=AccelerationPhysicsModel.Evaluate(.6,.05,0,0,9.81);if(slow.Acceleration>=baseline.Acceleration)throw new Exception("Mass effect missing.");
            var fast=AccelerationPhysicsModel.Evaluate(.5,.1,0,0,9.81);if(fast.Acceleration<=baseline.Acceleration)throw new Exception("Bucket mass effect missing.");
            c.CycleAngle();c.CycleAngle();c.CycleFriction();c.CycleFriction();if(c.StartExperiment())throw new Exception("Insufficient-force experiment started.");
            c.ResetExperiment();c.ChangeCartWeights(1);c.ChangeBucketWeights(1);if(!c.StartExperiment())throw new Exception("Valid experiment rejected.");
            var initial=c.State;if(c.ChangeCartWeights(1)||c.CycleAngle())throw new Exception("Parameters changed during run.");
            c.Advance(.13);AssertClose(c.State.Position,.5*initial.Acceleration*.13*.13);v.Refresh(c.State);
            AssertClose(v.Cart.localPosition.x-c.Settings.cartStartDistance,c.State.Position,1e-5);
            c.Advance(100);if(c.State.Phase!=AccelerationPhase.Completed)throw new Exception("Auto finish failed.");AssertClose(c.State.Position,c.State.TravelLimit);
            var expected=c.Recorder.Samples.ToArray();for(int i=0;i<expected.Length;i++){AssertClose(expected[i].Time,i*.02);AssertClose(expected[i].Position,.5*initial.Acceleration*expected[i].Time*expected[i].Time);}
            if(!c.RecordResult()||c.RecordResult())throw new Exception("Result recording/duplicate handling failed.");
            c.ResetExperiment();if(c.State.Time!=0||c.State.Position!=0||c.State.Velocity!=0||c.Recorder.Samples.Count!=0)throw new Exception("Reset failed.");
            c.ChangeCartWeights(1);c.ChangeBucketWeights(1);c.StartExperiment();while(c.State.Phase==AccelerationPhase.Running)c.Advance(.007);
            if(c.Recorder.Samples.Count!=expected.Length)throw new Exception("Frame-dependent sampling.");for(int i=0;i<expected.Length;i++)AssertClose(c.Recorder.Samples[i].Position,expected[i].Position);
            c.ResetExperiment();v.Refresh(c.State);var player=GameObject.Find("DesktopPlayer");if(player.GetComponentInChildren<Animator>()?.isHuman!=true)throw new Exception("Shared Humanoid missing.");
            Physics.SyncTransforms();foreach(var action in Object.FindObjectsByType<AccelerationInteractable>(FindObjectsSortMode.None).Where(a=>a.name.StartsWith("Control_")))
            {
                var target=action.transform.position;var origin=new Vector3(target.x,1.63f,target.z-1.15f);
                if(!Physics.Raycast(origin,(target-origin).normalized,out var hit,3.5f)||hit.collider.GetComponentInParent<AccelerationInteractable>()!=action)throw new Exception("Control ray blocked: "+action.name);
            }
            Debug.Log("ACCELERATION_VERIFIED: forces, mass effects, F<=0 block, run locks, analytical motion, clamp, 50Hz frame independence, record, reset, Humanoid, physical control rays.");
        }
        public static void AssertClose(double value,double expected,double tolerance=1e-8){if(Math.Abs(value-expected)>tolerance)throw new Exception($"Expected {expected}, got {value}");}
        public static void Capture()
        {
            EditorSceneManager.OpenScene(Lab,OpenSceneMode.Single);var c=Object.FindFirstObjectByType<AccelerationExperimentController>();c.Configure(c.Settings,c.Recorder);var visual=c.GetComponent<AccelerationExperimentVisual>();
            c.ChangeCartWeights(2);c.ChangeBucketWeights(1);visual.Refresh(c.State);Object.FindFirstObjectByType<AccelerationDataDisplay>().Refresh(c.State);foreach(var t in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))t.ForceMeshUpdate(true,true);Canvas.ForceUpdateCanvases();
            var camera=Camera.main;camera.transform.SetPositionAndRotation(new Vector3(0,2.2f,2.1f),Quaternion.LookRotation(new Vector3(0,1.9f,5)-new Vector3(0,2.2f,2.1f)));
            var rt=new RenderTexture(1400,900,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1400,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1400,900),0,0);image.Apply();File.WriteAllBytes("Logs/AccelerationLab-v1.png",image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);EditorSceneManager.OpenScene(Lab,OpenSceneMode.Single);Debug.Log("ACCELERATION_CAPTURE_COMPLETE");
        }
    }
}
