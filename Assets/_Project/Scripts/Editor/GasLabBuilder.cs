using System;
using System.IO;
using System.Linq;
using EmbodiedPhysicsLab.Core;
using EmbodiedPhysicsLab.Core.Gas;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace EmbodiedPhysicsLab.Editor
{
    public static class GasLabBuilder
    {
        public const string Lab="Assets/_Project/Scenes/GasLab.unity";
        private const string PlayerPath="Assets/_Project/Prefabs/Players/DesktopPlayer.prefab",HudPath="Assets/_Project/Prefabs/Players/DesktopInteractionHUD.prefab",SettingsPath="Assets/_Project/Experiments/Gas/GasSettings.asset";
        private static TMP_FontAsset font; private static Material wall,floor,wood,steel,glass,gasColor,screen,blue,green,red,orange,cyan,dot;
        private static GasExperimentSettings settings; private static GasExperimentController controller;
        [MenuItem("Embodied Physics Lab/Build GasLab v1")]
        public static void Build()
        {
            Directory.CreateDirectory("Assets/_Project/Experiments/Gas");Directory.CreateDirectory("Assets/_Project/Materials/Gas");AssetDatabase.Refresh();
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/NotoSansSC SDF.asset");if(font==null)throw new IOException("Chinese TMP font missing.");
            MakePlayerPrefabs();var scene=EditorSceneManager.OpenScene(Lab,OpenSceneMode.Single);foreach(var root in scene.GetRootGameObjects())Object.DestroyImmediate(root);
            settings=AssetDatabase.LoadAssetAtPath<GasExperimentSettings>(SettingsPath);if(settings==null){settings=ScriptableObject.CreateInstance<GasExperimentSettings>();AssetDatabase.CreateAsset(settings,SettingsPath);}MakeMaterials();Room();
            var experiment=new GameObject("GasExperiment");controller=experiment.AddComponent<GasExperimentController>();controller.Configure(settings);
            var visual=BuildApparatus(experiment.transform);BuildStations();BuildDisplay(visual);BuildControls();BuildPlayer();
            new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));var sun=new GameObject("Directional Light").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=.95f;sun.transform.rotation=Quaternion.Euler(40,-25,0);sun.shadows=LightShadows.Soft;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.64f,.68f,.70f);
            GasPhysicalInteractionBuilder.ApplyToOpenScene();
            EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("GasLab save failed.");AssetDatabase.SaveAssets();Verify();Debug.Log("GAS_BUILD_COMPLETE");
        }
        private static void MakePlayerPrefabs()
        {
            if(File.Exists(PlayerPath)&&File.Exists(HudPath))return;
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Lobby.unity",OpenSceneMode.Single);var src=GameObject.Find("DesktopPlayer");var hudsrc=GameObject.Find("Interaction HUD");if(src==null||hudsrc==null)throw new IOException("Shared player/HUD missing in Lobby.");
            var player=Object.Instantiate(src);player.name="DesktopPlayer";player.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);var cam=player.GetComponentInChildren<Camera>();player.GetComponent<PlayerInteractor>().Configure(cam,null,3.5f);player.GetComponent<DesktopFirstPersonController>().Configure(player.GetComponent<CharacterController>(),cam.transform);PrefabUtility.SaveAsPrefabAsset(player,PlayerPath);Object.DestroyImmediate(player);
            var hud=Object.Instantiate(hudsrc);hud.name="DesktopInteractionHUD";PrefabUtility.SaveAsPrefabAsset(hud,HudPath);Object.DestroyImmediate(hud);
        }
        private static Material Mat(string name,Color color,bool transparent=false)
        {
            string path="Assets/_Project/Materials/Gas/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;m.SetFloat("_Smoothness",transparent ? .45f : .22f);
            if(transparent){m.SetFloat("_Surface",1);m.SetFloat("_Blend",0);m.SetOverrideTag("RenderType","Transparent");m.renderQueue=(int)RenderQueue.Transparent;m.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);m.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);m.SetInt("_ZWrite",0);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");}
            EditorUtility.SetDirty(m);return m;
        }
        private static void MakeMaterials()
        {
            wall=Mat("LabWall",new Color(.79f,.83f,.82f));floor=Mat("LabFloor",new Color(.42f,.48f,.49f));wood=Mat("BenchWood",new Color(.48f,.31f,.17f));steel=Mat("InstrumentSteel",new Color(.35f,.42f,.45f));glass=Mat("CylinderGlass",new Color(.5f,.81f,.9f,.20f),true);gasColor=Mat("GasColumn",new Color(.25f,.78f,.92f,.35f),true);screen=Mat("DisplayBlack",new Color(.025f,.055f,.075f));blue=Mat("ControlBlue",new Color(.12f,.32f,.5f));green=Mat("HoldGreen",new Color(.06f,.43f,.29f));red=Mat("ResetRed",new Color(.58f,.18f,.14f));orange=Mat("HeatOrange",new Color(.78f,.28f,.07f));cyan=Mat("CoolCyan",new Color(.08f,.48f,.63f));dot=Mat("GraphPoint",new Color(.1f,.85f,.65f));
        }
        private static Transform Group(string name,Transform parent=null){var t=new GameObject(name).transform;if(parent!=null)t.SetParent(parent,false);return t;}
        private static GameObject Part(Transform parent,string name,Vector3 pos,Vector3 scale,Material mat,PrimitiveType type=PrimitiveType.Cube,bool collider=true)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=mat;if(!collider)Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        private static TMP_Text Label(Transform parent,string value,Vector3 pos,float width,float height,int size,Quaternion? rotation=null)
        {
            var root=new GameObject("Label | "+value.Split('\n')[0],typeof(RectTransform),typeof(Canvas));root.transform.SetParent(parent,false);root.transform.localPosition=pos;root.transform.localRotation=rotation??Quaternion.identity;root.transform.localScale=Vector3.one*.004f;root.GetComponent<RectTransform>().sizeDelta=new Vector2(width*250,height*250);root.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            var child=new GameObject("Text",typeof(RectTransform),typeof(TextMeshProUGUI));child.transform.SetParent(root.transform,false);var r=child.GetComponent<RectTransform>();r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;var txt=child.GetComponent<TextMeshProUGUI>();txt.font=font;txt.text=value;txt.fontSize=size;txt.alignment=TextAlignmentOptions.Center;txt.color=Color.white;txt.raycastTarget=false;return txt;
        }
        private static LineRenderer Line(Transform parent,string name,Material mat,float width,bool local=true){var g=new GameObject(name);g.transform.SetParent(parent,false);var l=g.AddComponent<LineRenderer>();l.sharedMaterial=mat;l.startWidth=l.endWidth=width;l.useWorldSpace=!local;l.positionCount=0;return l;}
        private static void Room()
        {
            var room=Group("LaboratoryRoom");Part(room,"Floor",new Vector3(0,-.1f,5.5f),new Vector3(14,.2f,11),floor);Part(room,"Ceiling",new Vector3(0,4.5f,5.5f),new Vector3(14,.2f,11),wall);
            foreach(float x in new[]{-7f,7f})Part(room,"SideWall",new Vector3(x,2.2f,5.5f),new Vector3(.2f,4.4f,11),wall);Part(room,"BackWall",new Vector3(0,2.2f,11),new Vector3(14,4.4f,.2f),wall);
            foreach(float x in new[]{-4.1f,4.1f})Part(room,"FrontWall",new Vector3(x,2.2f,0),new Vector3(5.8f,4.4f,.2f),wall);Part(room,"FrontLintel",new Vector3(0,3.6f,0),new Vector3(2.4f,1.6f,.2f),wall);
            Label(room,"热学实验室 · 气体等温变化规律",new Vector3(0,3.85f,10.84f),7,.65f,58);
            for(int i=0;i<4;i++){var lamp=Part(room,"CeilingFixture",new Vector3(i%2==0?-4:4,4.34f,3+i/2*5),new Vector3(1.3f,.06f,.4f),wall,PrimitiveType.Cube,false);var light=lamp.AddComponent<Light>();light.type=LightType.Point;light.range=7;light.intensity=1.4f;}
            var exit=Group("ExitToLobby");exit.localPosition=new Vector3(0,0,.12f);Part(exit,"DoorPanel",new Vector3(0,1.35f,0),new Vector3(2.1f,2.7f,.14f),wood);var door=exit.gameObject.AddComponent<SceneDoorInteractable>();door.DestinationName="返回实验大厅";door.DestinationScene="Lobby";exit.gameObject.AddComponent<SceneNavigator>().TargetScene="Lobby";Label(exit,"返回大厅",new Vector3(0,2.1f,-.09f),1.8f,.4f,40);
        }
        private static void Bench(Transform p,Vector3 pos,float w,float d,float h)
        {
            Part(p,"BenchTop",pos+Vector3.up*h,new Vector3(w,.12f,d),wood);foreach(float x in new[]{-.45f,.45f})foreach(float z in new[]{-.4f,.4f})Part(p,"BenchLeg",pos+new Vector3(w*x,h*.5f,d*z),new Vector3(.08f,h,.08f),steel);
        }
        private static GasExperimentVisual BuildApparatus(Transform root)
        {
            var apparatus=Group("ExperimentalApparatus",root);const float benchTopCenterY=1.0f,benchTopThickness=.12f,baseHalfHeight=.07f;Bench(apparatus,new Vector3(0,0,5.6f),4.2f,2f,benchTopCenterY);
            var cylinder=Group("GasCylinder",apparatus);float benchSurfaceY=benchTopCenterY+benchTopThickness*.5f;float cylinderRootY=benchSurfaceY-(settings.cylinderBottomY-baseHalfHeight);cylinder.localPosition=new Vector3(0,cylinderRootY,5.45f);
            Part(cylinder,"CylinderBody",new Vector3(0,(settings.cylinderBottomY+settings.pistonMaxY)*.5f,0),new Vector3(settings.cylinderRadius*2,(settings.pistonMaxY-settings.cylinderBottomY)*.5f,settings.cylinderRadius*2),glass,PrimitiveType.Cylinder,false);
            Part(cylinder,"CylinderBase",new Vector3(0,settings.cylinderBottomY,0),new Vector3(.54f,.07f,.54f),steel,PrimitiveType.Cylinder,false);
            Part(cylinder,"UpperRim",new Vector3(0,settings.pistonMaxY,0),new Vector3(.52f,.035f,.52f),steel,PrimitiveType.Cylinder,false);
            var gas=Part(cylinder,"GasVolume",new Vector3(0,.55f,0),new Vector3(settings.cylinderRadius*1.4f,.3f,settings.cylinderRadius*1.4f),gasColor,PrimitiveType.Cylinder,false);
            var piston=Part(cylinder,"Piston",new Vector3(0,settings.pistonMinY,0),new Vector3(.48f,.065f,.48f),blue,PrimitiveType.Cylinder,false);
            var rod=Part(cylinder,"PistonRod",new Vector3(0,settings.pistonMinY+.1f,0),new Vector3(.07f,.1f,.07f),steel,PrimitiveType.Cylinder,false);
            Part(cylinder,"PistonGuide",new Vector3(0,settings.pistonMaxY+.15f,0),new Vector3(.095f,.18f,.095f),steel,PrimitiveType.Cylinder,false);
            Part(cylinder,"PressureSensor",new Vector3(.72f,1.25f,0),new Vector3(.42f,.38f,.25f),steel);Part(cylinder,"SensorProbe",new Vector3(.52f,.58f,0),new Vector3(.05f,.8f,.05f),steel,PrimitiveType.Cylinder,false);Label(cylinder,"压力传感器",new Vector3(.78f,1.53f,-.15f),.85f,.25f,27);
            Label(cylinder,"透明气缸 · 可调活塞",new Vector3(0,1.88f,-.03f),2.3f,.3f,31);
            var visual=root.gameObject.AddComponent<GasExperimentVisual>();visual.Configure(controller,piston.transform,gas.transform,rod.transform,gas.GetComponent<Renderer>());return visual;
        }
        private static GameObject Button(Transform parent,GasCommand command,Vector3 pos,string title,Material mat)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="Control_"+command;go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=new Vector3(.62f,.28f,.17f);go.GetComponent<Renderer>().sharedMaterial=mat;go.AddComponent<GasInteractable>().Configure(controller,command);Label(go.transform,title,new Vector3(0,0,-.09f),.58f,.22f,25);return go;
        }
        private static void BuildStations()
        {
            var a=Group("VolumeControl_A");Bench(a,new Vector3(-4.15f,0,5.2f),2.2f,1.2f,.94f);Label(a,"A 区 · 活塞 / 体积",new Vector3(-4.15f,1.65f,5.35f),2.4f,.45f,35);
            Button(a,GasCommand.VolumeDown,new Vector3(-4.52f,1.25f,4.67f),"减小 V",blue);Button(a,GasCommand.VolumeUp,new Vector3(-3.78f,1.25f,4.67f),"增大 V",blue);
            Label(a,"档位：1.0 · 1.2 · 1.5 · 1.8 · 2.0 L",new Vector3(-4.15f,1.83f,4.62f),2.5f,.3f,23);
            var b=Group("TemperatureControl_B");Bench(b,new Vector3(0,0,2.9f),2.2f,1.0f,.9f);Label(b,"B 区 · 温度控制",new Vector3(0,1.62f,3.1f),2.2f,.42f,35);
            Button(b,GasCommand.Heat,new Vector3(-.68f,1.22f,2.46f),"加热",orange);Button(b,GasCommand.Cool,new Vector3(0,1.22f,2.46f),"冷却",cyan);Button(b,GasCommand.Hold,new Vector3(.68f,1.22f,2.46f),"恒温",green);
            Part(b,"HeatingCoolingModule",new Vector3(0,1.1f,3.35f),new Vector3(.45f,.36f,.3f),steel,PrimitiveType.Cube,false);for(int i=0;i<4;i++)Part(b,"HeatSinkFin",new Vector3(-.3f+i*.2f,1.37f,3.35f),new Vector3(.06f,.22f,.32f),i%2==0?orange:cyan);
            Label(b,"目标温度 300 K",new Vector3(0,1.68f,2.43f),1.75f,.26f,24);
            var c=Group("ObservationStation_C");Bench(c,new Vector3(4.15f,0,5.2f),2.2f,1.2f,.94f);Label(c,"C 区 · 观察 / 记录",new Vector3(4.15f,1.65f,5.35f),2.4f,.45f,35);
            Button(c,GasCommand.Record,new Vector3(3.78f,1.25f,4.67f),"记录数据",green);Button(c,GasCommand.Reset,new Vector3(4.52f,1.25f,4.67f),"Reset",red);
            Part(c,"ObservationSensor",new Vector3(4.15f,1.16f,5.2f),new Vector3(.42f,.48f,.3f),steel);Part(c,"TempProbe",new Vector3(3.86f,1.3f,5.2f),new Vector3(.04f,.56f,.04f),cyan,PrimitiveType.Cylinder,false);Part(c,"ThermometerBulb",new Vector3(3.86f,1.03f,5.2f),new Vector3(.15f,.15f,.15f),red,PrimitiveType.Sphere,false);
            Label(c,"等温判据：|T − T目标| ≤ 1 K 且稳定",new Vector3(4.15f,1.88f,4.55f),2.5f,.3f,22);
        }
        private static void BuildDisplay(GasExperimentVisual visual)
        {
            var root=Group("DataDisplay");Part(root,"DashboardFrame",new Vector3(0,3.12f,10.62f),new Vector3(10.6f,2.45f,.16f),steel);Part(root,"DashboardScreen",new Vector3(0,3.12f,10.51f),new Vector3(10.4f,2.28f,.04f),screen,PrimitiveType.Cube,false);
            var p=Label(root,"",new Vector3(-4.25f,3.65f,10.47f),1.7f,.9f,35);var t=Label(root,"",new Vector3(-2.15f,3.65f,10.47f),1.7f,.9f,35);var v=Label(root,"",new Vector3(-.05f,3.65f,10.47f),1.7f,.9f,35);
            var info=Label(root,"",new Vector3(-3.0f,2.78f,10.47f),4.7f,.8f,23);var list=Label(root,"",new Vector3(1.95f,3.3f,10.47f),3.9f,1.55f,21);
            Label(root,"P-V 曲线\n横轴 V / L   纵轴 P / kPa",new Vector3(4.53f,4.0f,10.47f),1.65f,.45f,21);
            var graph=Group("PVGraph",root);graph.localPosition=new Vector3(4.5f,3.18f,10.46f);Part(graph,"HorizontalAxis",new Vector3(0,-.47f,0),new Vector3(1.7f,.018f,.015f),steel,PrimitiveType.Cube,false);Part(graph,"VerticalAxis",new Vector3(-.83f,0,0),new Vector3(.018f,.94f,.015f),steel,PrimitiveType.Cube,false);
            var line=Line(graph,"PVDataCurve",dot,.035f);var markers=Group("PVDataPoints",graph);var display=root.gameObject.AddComponent<GasDataDisplay>();display.Configure(controller,p,t,v,info,list,line,markers,dot);
            visual.Render(controller.State);
        }
        private static void BuildControls()
        {
            var controls=Group("ExperimentControl");Button(controls,GasCommand.Record,new Vector3(-.5f,1.14f,7.65f),"Record",green);Button(controls,GasCommand.Reset,new Vector3(.5f,1.14f,7.65f),"Reset",red);Label(controls,"A 体积  ·  B 温控  ·  C 观察与记录",new Vector3(0,1.62f,7.9f),3.8f,.35f,30);
        }
        private static void BuildPlayer()
        {
            var player=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath));player.name="DesktopPlayer";player.transform.SetPositionAndRotation(new Vector3(0,.04f,1.25f),Quaternion.identity);
            var hud=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HudPath));hud.name="Interaction HUD";var prompt=hud.GetComponentsInChildren<TMP_Text>(true).First(t=>t.name=="Interaction Prompt");player.GetComponent<PlayerInteractor>().Configure(player.GetComponentInChildren<Camera>(),prompt,3.5f);
        }
        public static void Verify()
        {
            if(SceneManager.GetActiveScene().path!=Lab)EditorSceneManager.OpenScene(Lab,OpenSceneMode.Single);var c=Object.FindFirstObjectByType<GasExperimentController>();var visual=c.GetComponent<GasExperimentVisual>();c.Configure(c.Settings);
            c.Advance(c.Settings.stableDwellSeconds+.1);var s=c.State;double p0=s.PressurePa,pv0=s.PTimesV;if(!c.Record())throw new Exception("Default stable record failed.");GasLabBuilder.AssertClose(c.Recorder.Points[0].PTimesV,pv0,1e-8);
            c.ChangeVolume(1);if(c.State.PressurePa>=p0||c.State.VolumeM3<=s.VolumeM3)throw new Exception("Volume change failed to affect gas state.");if(c.Record())throw new Exception("Unstable temperature was recorded.");
            c.SetMode(ThermalMode.Heating);double before=c.State.TemperatureK;c.Advance(.5);if(c.State.TemperatureK<=before)throw new Exception("Heating control had no effect.");c.SetMode(ThermalMode.Cooling);before=c.State.TemperatureK;c.Advance(.5);if(c.State.TemperatureK>=before)throw new Exception("Cooling control had no effect.");
            c.SetMode(ThermalMode.HoldConstant);for(int i=0;i<200&&!c.State.IsothermalReady;i++)c.Advance(.05);if(!c.State.IsothermalReady||!c.Record())throw new Exception("Temperature stabilization/record condition failed.");
            while(c.Recorder.DistinctVolumeCount<c.Settings.minimumDistinctVolumes){c.ChangeVolume(1);c.SetMode(ThermalMode.HoldConstant);for(int i=0;i<200&&!c.State.IsothermalReady;i++)c.Advance(.05);if(!c.Record())throw new Exception("PV round recording failed.");}
            var values=c.Recorder.Points.Select(x=>x.PTimesV).ToArray();if(values.Max()-values.Min()>values.Average()*.01)throw new Exception("Isothermal pV did not remain approximately constant.");
            visual.Render(c.State);var disp=Object.FindFirstObjectByType<GasDataDisplay>();disp.Refresh(c.State);c.ResetExperiment();if(c.State.VolumeIndex!=0||c.Recorder.Points.Count!=0||c.State.TemperatureK!=c.Settings.defaultTemperature)throw new Exception("Reset failed.");visual.Render(c.State);
            Physics.SyncTransforms();foreach(var control in Object.FindObjectsByType<GasInteractable>(FindObjectsSortMode.None))
            {
                var target=control.transform.position;var origin=target+new Vector3(0,.1f,-1.4f);if(!Physics.Raycast(origin,(target-origin).normalized,out var hit,3.5f)||hit.collider.GetComponentInParent<GasInteractable>()!=control)throw new Exception("Interactable ray blocked: "+control.name);
            }
            var player=GameObject.Find("DesktopPlayer");if(player==null||player.GetComponentInChildren<Animator>()?.isHuman!=true)throw new Exception("Shared Humanoid missing.");Debug.Log("GAS_VERIFIED: PV=nRT, volume-pressure response, heat/cool/hold dynamics, stability gate, distinct data points, pv constancy, curve, reset, interaction rays, Humanoid.");
        }
        public static void AssertClose(double v,double expected,double tolerance){if(Math.Abs(v-expected)>tolerance)throw new Exception($"Expected {expected}, got {v}");}
    }
    public sealed class GasDataRecorderComponent:MonoBehaviour{}
}





