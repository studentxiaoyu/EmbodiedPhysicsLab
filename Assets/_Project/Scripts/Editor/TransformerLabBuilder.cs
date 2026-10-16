using System;
using System.IO;
using System.Linq;
using EmbodiedPhysicsLab.Core;
using EmbodiedPhysicsLab.Core.Transformer;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace EmbodiedPhysicsLab.Editor
{
    public static class TransformerLabBuilder
    {
        public const string Lab="Assets/_Project/Scenes/TransformerLab.unity";
        private const string PlayerPath="Assets/_Project/Prefabs/Players/DesktopPlayer.prefab",HudPath="Assets/_Project/Prefabs/Players/DesktopInteractionHUD.prefab",SettingsPath="Assets/_Project/Experiments/Transformer/TransformerSettings.asset";
        private static TMP_FontAsset font; private static Material wall,floor,wood,steel,copper,coilBase,screen,blue,green,red,orange,cyan,dots,cableBlack,cableBlue; private static TransformerExperimentSettings settings; private static TransformerExperimentController controller;
        [MenuItem("Embodied Physics Lab/Build TransformerLab v1")]
        public static void Build()
        {
            Directory.CreateDirectory("Assets/_Project/Experiments/Transformer");Directory.CreateDirectory("Assets/_Project/Materials/Transformer");AssetDatabase.Refresh();font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/NotoSansSC SDF.asset");if(font==null)throw new IOException("Chinese TMP font missing.");
            EnsurePlayerPrefabs();var scene=EditorSceneManager.OpenScene(Lab,OpenSceneMode.Single);foreach(var root in scene.GetRootGameObjects())Object.DestroyImmediate(root);
            settings=AssetDatabase.LoadAssetAtPath<TransformerExperimentSettings>(SettingsPath);if(settings==null){settings=ScriptableObject.CreateInstance<TransformerExperimentSettings>();AssetDatabase.CreateAsset(settings,SettingsPath);}MakeMaterials();Room();
            var experiment=new GameObject("TransformerExperiment");controller=experiment.AddComponent<TransformerExperimentController>();controller.Configure(settings);BuildApparatus(experiment.transform);BuildStations();BuildDisplay();BuildControls();BuildPlayer();
            new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));var sun=new GameObject("Directional Light").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1f;sun.transform.rotation=Quaternion.Euler(40,-25,0);sun.shadows=LightShadows.Soft;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.65f,.68f,.70f);
            TransformerPhysicalInteractionBuilder.ApplyToOpenScene();
            EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("TransformerLab scene save failed.");AssetDatabase.SaveAssets();Verify();Debug.Log("TRANSFORMER_BUILD_COMPLETE");
        }
        private static void EnsurePlayerPrefabs()
        {
            if(File.Exists(PlayerPath)&&File.Exists(HudPath))return;EditorSceneManager.OpenScene("Assets/_Project/Scenes/Lobby.unity",OpenSceneMode.Single);var src=GameObject.Find("DesktopPlayer");var hudsrc=GameObject.Find("Interaction HUD");if(src==null||hudsrc==null)throw new IOException("Shared player and HUD missing in Lobby.");
            var p=Object.Instantiate(src);p.name="DesktopPlayer";var camera=p.GetComponentInChildren<Camera>();p.GetComponent<PlayerInteractor>().Configure(camera,null,3.5f);PrefabUtility.SaveAsPrefabAsset(p,PlayerPath);Object.DestroyImmediate(p);var h=Object.Instantiate(hudsrc);h.name="DesktopInteractionHUD";PrefabUtility.SaveAsPrefabAsset(h,HudPath);Object.DestroyImmediate(h);
        }
        private static Material Mat(string name,Color color,bool emissive=false)
        {
            string path="Assets/_Project/Materials/Transformer/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;m.SetFloat("_Smoothness",.32f);if(emissive){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*.15f);}EditorUtility.SetDirty(m);return m;
        }
        private static void MakeMaterials()
        {wall=Mat("LabWall",new Color(.78f,.82f,.82f));floor=Mat("LabFloor",new Color(.42f,.48f,.5f));wood=Mat("BenchWood",new Color(.48f,.31f,.17f));steel=Mat("CoreSteel",new Color(.25f,.30f,.34f));copper=Mat("CopperWinding",new Color(.72f,.27f,.08f),true);coilBase=Mat("CoilBobbin",new Color(.12f,.16f,.19f));screen=Mat("DataScreen",new Color(.02f,.06f,.08f));blue=Mat("ControlBlue",new Color(.10f,.30f,.48f));green=Mat("PowerGreen",new Color(.08f,.48f,.25f),true);red=Mat("ResetRed",new Color(.54f,.15f,.13f));orange=Mat("SourceOrange",new Color(.75f,.28f,.06f));cyan=Mat("VoltageCyan",new Color(.08f,.47f,.62f));dots=Mat("GraphPoint",new Color(.12f,.85f,.57f),true);cableBlack=Mat("PrimaryCable",new Color(.08f,.08f,.09f));cableBlue=Mat("SecondaryCable",new Color(.08f,.28f,.68f));}
        private static Transform Group(string name,Transform parent=null){var t=new GameObject(name).transform;if(parent!=null)t.SetParent(parent,false);return t;}
        private static GameObject Part(Transform parent,string name,Vector3 pos,Vector3 scale,Material mat,PrimitiveType type=PrimitiveType.Cube,bool collider=true)
        {var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=mat;if(!collider)Object.DestroyImmediate(go.GetComponent<Collider>());return go;}
        private static TMP_Text Label(Transform parent,string value,Vector3 pos,float width,float height,int size,Quaternion? rotation=null)
        {var root=new GameObject("Label | "+value.Split('\n')[0],typeof(RectTransform),typeof(Canvas));root.transform.SetParent(parent,false);root.transform.localPosition=pos;root.transform.localRotation=rotation??Quaternion.identity;root.transform.localScale=Vector3.one*.004f;root.GetComponent<RectTransform>().sizeDelta=new Vector2(width*250,height*250);root.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;var child=new GameObject("Text",typeof(RectTransform),typeof(TextMeshProUGUI));child.transform.SetParent(root.transform,false);var rt=child.GetComponent<RectTransform>();rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;var text=child.GetComponent<TextMeshProUGUI>();text.font=font;text.text=value;text.fontSize=size;text.alignment=TextAlignmentOptions.Center;text.color=Color.white;text.raycastTarget=false;return text;}
        private static LineRenderer Line(Transform parent,string name,Material mat,float width,bool world=true){var go=new GameObject(name);go.transform.SetParent(parent,false);var l=go.AddComponent<LineRenderer>();l.sharedMaterial=mat;l.startWidth=l.endWidth=width;l.useWorldSpace=world;l.positionCount=0;return l;}
        private static void Room()
        {
            var r=Group("LaboratoryRoom");Part(r,"Floor",new Vector3(0,-.1f,5.5f),new Vector3(14,.2f,11),floor);Part(r,"Ceiling",new Vector3(0,4.5f,5.5f),new Vector3(14,.2f,11),wall);foreach(float x in new[]{-7f,7f})Part(r,"SideWall",new Vector3(x,2.2f,5.5f),new Vector3(.2f,4.4f,11),wall);Part(r,"BackWall",new Vector3(0,2.2f,11),new Vector3(14,4.4f,.2f),wall);foreach(float x in new[]{-4.1f,4.1f})Part(r,"FrontWall",new Vector3(x,2.2f,0),new Vector3(5.8f,4.4f,.2f),wall);Part(r,"FrontLintel",new Vector3(0,3.6f,0),new Vector3(2.4f,1.6f,.2f),wall);
            Label(r,"电磁学实验室 · 变压器匝数与电压关系",new Vector3(0,3.85f,10.84f),7,.65f,55);for(int i=0;i<4;i++){var lamp=Part(r,"CeilingFixture",new Vector3(i%2==0?-4:4,4.34f,3+i/2*5),new Vector3(1.3f,.06f,.4f),wall,PrimitiveType.Cube,false);var light=lamp.AddComponent<Light>();light.type=LightType.Point;light.range=7;light.intensity=1.4f;}
            var exit=Group("ExitToLobby");exit.localPosition=new Vector3(0,0,.12f);Part(exit,"DoorPanel",new Vector3(0,1.35f,0),new Vector3(2.1f,2.7f,.14f),wood);var door=exit.gameObject.AddComponent<SceneDoorInteractable>();door.DestinationName="返回实验大厅";door.DestinationScene="Lobby";exit.gameObject.AddComponent<SceneNavigator>().TargetScene="Lobby";Label(exit,"返回大厅",new Vector3(0,2.1f,-.09f),1.8f,.4f,40);
        }
        private static void Bench(Transform parent,Vector3 pos,float w,float d,float h)
        {Part(parent,"BenchTop",pos+Vector3.up*h,new Vector3(w,.12f,d),wood);foreach(float x in new[]{-.45f,.45f})foreach(float z in new[]{-.4f,.4f})Part(parent,"BenchLeg",pos+new Vector3(w*x,h*.5f,d*z),new Vector3(.08f,h,.08f),steel);}
        private static void Wire(Transform parent,string name,Vector3[] pts,Material material){var l=Line(parent,name,material,.025f);l.positionCount=pts.Length;l.SetPositions(pts);}
        private static Transform[] MakeWinding(Transform parent,string name,Vector3 center,int levelCount)
        {
            var coil=Group(name,parent);coil.localPosition=center;Part(coil,"Bobbin",Vector3.zero,new Vector3(.38f,1.05f,.38f),coilBase,PrimitiveType.Cylinder,false);
            var levels=new Transform[levelCount];for(int i=0;i<levelCount;i++){var turn=Group("CoilTurn_"+(i+1),coil);turn.localPosition=new Vector3(0,-.42f+i*.094f,0);Part(turn,"FrontWinding",new Vector3(0,0,-.22f),new Vector3(.48f,.038f,.045f),copper,PrimitiveType.Cube,false);Part(turn,"BackWinding",new Vector3(0,0,.22f),new Vector3(.48f,.038f,.045f),copper,PrimitiveType.Cube,false);Part(turn,"LeftWinding",new Vector3(-.22f,0,0),new Vector3(.045f,.038f,.48f),copper,PrimitiveType.Cube,false);Part(turn,"RightWinding",new Vector3(.22f,0,0),new Vector3(.045f,.038f,.48f),copper,PrimitiveType.Cube,false);levels[i]=turn;}
            return levels;
        }
        private static void BenchArea(Transform p,Vector3 pos,float w,float d,float h){Bench(p,pos,w,d,h);}
        private static void BuildApparatus(Transform parent)
        {
            var rig=Group("TransformerApparatus",parent);Bench(rig,new Vector3(0,0,5.5f),4.8f,2.15f,1.0f);var apparatus=Group("CoreAndCoils",rig);apparatus.localPosition=new Vector3(0,0,5.25f);
            var core=Group("IronCore",apparatus);Part(core,"CoreLeftLeg",new Vector3(-.58f,1f,0),new Vector3(.18f,1.48f,.24f),steel);Part(core,"CoreRightLeg",new Vector3(.58f,1f,0),new Vector3(.18f,1.48f,.24f),steel);Part(core,"CoreTopYoke",new Vector3(0,1.73f,0),new Vector3(1.34f,.18f,.24f),steel);Part(core,"CoreBottomYoke",new Vector3(0,.27f,0),new Vector3(1.34f,.18f,.24f),steel);Part(core,"CenterLimb",new Vector3(0,1f,0),new Vector3(.14f,1.32f,.2f),steel);
            var coreRenderers=core.GetComponentsInChildren<Renderer>();var pri=MakeWinding(apparatus,"PrimaryCoil",new Vector3(-.58f,1f,-.02f),10);var sec=MakeWinding(apparatus,"SecondaryCoil",new Vector3(.58f,1f,-.02f),10);
            Label(apparatus,"原线圈 N1",new Vector3(-1.45f,1.98f,-.15f),1.25f,.32f,30);Label(apparatus,"副线圈 N2",new Vector3(1.45f,1.98f,-.15f),1.25f,.32f,30);Label(apparatus,"叠片铁芯",new Vector3(0,2.02f,.20f),1.15f,.25f,25);
            var inputMeter=BuildMeter(rig,"InputVoltmeter",new Vector3(-2.0f,1.5f,7.05f),"输入电压表");var outputMeter=BuildMeter(rig,"OutputVoltmeter",new Vector3(2.0f,1.5f,7.05f),"输出电压表");
            var ac=Group("ACSource",rig);BenchArea(ac,new Vector3(-4.15f,0,4.3f),1.8f,1.15f,.95f);Part(ac,"ACSourceBody",new Vector3(-4.15f,1.42f,4.3f),new Vector3(1.12f,.72f,.67f),steel);Label(ac,"交流电源",new Vector3(-4.15f,1.94f,3.92f),1.5f,.3f,30);
            Label(ac,"U1设定",new Vector3(-4.15f,1.52f,3.94f),.8f,.25f,25);var setText=Label(ac,"",new Vector3(-4.15f,1.26f,3.94f),.8f,.24f,27);
            var indicator=Part(ac,"PowerIndicator",new Vector3(-3.69f,1.54f,3.94f),new Vector3(.12f,.12f,.06f),red,PrimitiveType.Sphere,false).GetComponent<Renderer>();var lever=Part(ac,"PowerSwitch",new Vector3(-4.55f,1.29f,4.08f),new Vector3(.12f,.32f,.12f),red);lever.transform.localRotation=Quaternion.Euler(0,0,25);
            Wire(rig,"PrimaryWiring",new[]{new Vector3(-3.6f,1.3f,4.3f),new Vector3(-2.5f,1.3f,5.1f),new Vector3(-.75f,1.0f,5.23f)},cableBlack);Wire(rig,"SecondaryWiring",new[]{new Vector3(.75f,1.0f,5.23f),new Vector3(2.45f,1.3f,5.1f),new Vector3(3.35f,1.3f,6.0f)},cableBlue);
            var visual=parent.gameObject.AddComponent<TransformerExperimentVisual>();visual.Configure(controller,lever.transform,inputMeter.Needle,outputMeter.Needle,pri.SelectMany(x=>x.GetComponentsInChildren<Renderer>()).ToArray(),sec.SelectMany(x=>x.GetComponentsInChildren<Renderer>()).ToArray(),coreRenderers,indicator,pri,sec,inputMeter.Text,outputMeter.Text);
            // Output meter is updated through its display text by the shared readout display.
            var meterDisplay=rig.gameObject.AddComponent<TransformerMeterDisplay>();meterDisplay.Configure(controller,inputMeter.Text,outputMeter.Text,setText);
        }
        private struct MeterRefs{public Transform Needle;public TMP_Text Text;}
        private static MeterRefs BuildMeter(Transform parent,string name,Vector3 pos,string title)
        {
            var m=Group(name,parent);m.localPosition=pos;var dial=Part(m,"MeterHousing",Vector3.zero,new Vector3(.72f,.72f,.12f),steel,PrimitiveType.Cylinder,false);dial.transform.localRotation=Quaternion.Euler(90,0,0);var face=Part(m,"MeterFace",new Vector3(0,0,-.075f),new Vector3(.63f,.63f,.035f),screen,PrimitiveType.Cylinder,false);face.transform.localRotation=Quaternion.Euler(90,0,0);
            var needle=Group("NeedlePivot",m);needle.localPosition=new Vector3(0,0,-.11f);Part(needle,"Needle",new Vector3(0,.17f,0),new Vector3(.018f,.34f,.018f),red,PrimitiveType.Cube,false);
            Label(m,title,new Vector3(0,.55f,-.11f),1.3f,.25f,25);var text=Label(m,"",new Vector3(0,-.45f,-.11f),1.1f,.32f,34);Label(m,"0         V         MAX",new Vector3(0,-.23f,-.11f),1.2f,.18f,18);return new MeterRefs{Needle=needle,Text=text};
        }
        private static GameObject Button(Transform parent,TransformerCommand command,Vector3 pos,string text,Material mat)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="Control_"+command;go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=new Vector3(.62f,.27f,.16f);go.GetComponent<Renderer>().sharedMaterial=mat;go.AddComponent<TransformerInteractable>().Configure(controller,command);Label(go.transform,text,new Vector3(0,0,-.085f),.58f,.22f,24);return go;}
        private static void BuildStations()
        {
            var a=Group("PrimaryControl_A");Bench(a,new Vector3(-4.2f,0,4.35f),2.1f,1.15f,.95f);Label(a,"A 区 · 交流电源 / U1",new Vector3(-4.2f,1.72f,4.55f),2.5f,.38f,31);
            Button(a,TransformerCommand.CycleInputVoltage,new Vector3(-4.57f,1.22f,3.88f),"调节 U1",blue);Button(a,TransformerCommand.TogglePower,new Vector3(-3.83f,1.22f,3.88f),"电源开关",green);Label(a,"6 / 9 / 12 V",new Vector3(-4.2f,1.55f,3.84f),1.4f,.24f,23);
            var b=Group("TurnsControl_B");Bench(b,new Vector3(0,0,3.25f),2.8f,1.1f,.95f);Label(b,"B 区 · 线圈匝数",new Vector3(0,1.70f,3.45f),2.35f,.38f,32);
            Button(b,TransformerCommand.CyclePrimaryTurns,new Vector3(-.55f,1.22f,2.78f),"调节 N1",orange);Button(b,TransformerCommand.CycleSecondaryTurns,new Vector3(.55f,1.22f,2.78f),"调节 N2",cyan);Label(b,"100 / 200 / 300 / 400 匝",new Vector3(0,1.54f,2.74f),2.5f,.24f,23);
            var c=Group("MeasurementStation_C");Bench(c,new Vector3(4.15f,0,5.1f),2.0f,1.25f,.95f);Label(c,"C 区 · 测量 / 记录",new Vector3(4.15f,1.72f,5.32f),2.35f,.38f,32);Button(c,TransformerCommand.Record,new Vector3(3.78f,1.23f,4.50f),"Record",green);Button(c,TransformerCommand.Reset,new Vector3(4.52f,1.23f,4.50f),"Reset",red);
            Part(c,"MeasurementBenchInstrument",new Vector3(4.15f,1.15f,5.1f),new Vector3(.48f,.4f,.32f),steel);Label(c,"读取 U2/U1 与 N2/N1",new Vector3(4.15f,1.55f,4.43f),2.2f,.25f,22);
        }
        private static void BuildDisplay()
        {
            var root=Group("DataDisplay");Part(root,"DisplayFrame",new Vector3(0,3.08f,10.62f),new Vector3(10.6f,2.45f,.16f),steel);Part(root,"DisplayScreen",new Vector3(0,3.08f,10.51f),new Vector3(10.4f,2.28f,.04f),screen,PrimitiveType.Cube,false);
            var values=Label(root,"",new Vector3(-3.35f,3.53f,10.47f),5.0f,1.25f,26);var table=Label(root,"",new Vector3(.82f,3.18f,10.47f),3.25f,1.75f,18);var status=Label(root,"",new Vector3(-3.3f,2.48f,10.47f),5.1f,.33f,22);
            Label(root,"匝数比—电压比\n横轴 N2/N1   纵轴 U2/U1",new Vector3(4.4f,4.0f,10.47f),1.8f,.42f,20);var graph=Group("RatioGraph",root);graph.localPosition=new Vector3(4.4f,3.12f,10.46f);
            Part(graph,"HorizontalAxis",new Vector3(0,-.69f,0),new Vector3(1.4f,.016f,.012f),steel,PrimitiveType.Cube,false);Part(graph,"VerticalAxis",new Vector3(-.69f,0,0),new Vector3(.016f,1.4f,.012f),steel,PrimitiveType.Cube,false);
            var ideal=Line(graph,"Ideal y=x",dots,.012f,false);ideal.positionCount=2;ideal.SetPosition(0,new Vector3(-.68f,-.68f,-.01f));ideal.SetPosition(1,new Vector3(.68f,.68f,-.01f));var line=Line(graph,"MeasuredRatioCurve",green,.025f,false);var markers=Group("MeasuredPoints",graph);
            root.gameObject.AddComponent<TransformerDataDisplay>().Configure(controller,values,table,status,line,markers,dots);
        }
        private static void BuildControls(){var root=Group("ExperimentControl");Button(root,TransformerCommand.Record,new Vector3(-.48f,1.13f,7.95f),"Record",green);Button(root,TransformerCommand.Reset,new Vector3(.48f,1.13f,7.95f),"Reset",red);Label(root,"接通电源后测量并记录不同匝数组合",new Vector3(0,1.55f,8.2f),3.6f,.32f,27);}
        private static void BuildPlayer()
        {var player=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath));player.name="DesktopPlayer";player.transform.SetPositionAndRotation(new Vector3(0,.04f,1.2f),Quaternion.identity);var hud=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HudPath));hud.name="Interaction HUD";var prompt=hud.GetComponentsInChildren<TMP_Text>(true).First(t=>t.name=="Interaction Prompt");player.GetComponent<PlayerInteractor>().Configure(player.GetComponentInChildren<Camera>(),prompt,3.5f);}
        public static void Verify()
        {
            if(SceneManager.GetActiveScene().path!=Lab)EditorSceneManager.OpenScene(Lab,OpenSceneMode.Single);var c=Object.FindFirstObjectByType<TransformerExperimentController>();var visual=c.GetComponent<TransformerExperimentVisual>();c.Configure(c.Settings);
            if(TransformerPhysicsModel.SecondaryVoltage(12,200,400,false)!=0)throw new Exception("Power-off output must be zero.");double u2=TransformerPhysicsModel.SecondaryVoltage(12,200,400,true);if(Math.Abs(u2-24)>1e-10)throw new Exception("Ideal transformer voltage calculation failed.");double tr=TransformerPhysicsModel.TurnsRatio(200,400),vr=TransformerPhysicsModel.VoltageRatio(12,u2);if(Math.Abs(tr-vr)>1e-10)throw new Exception("Turns and voltage ratios diverged.");
            if(c.Record())throw new Exception("Recording while source off must fail.");c.TogglePower();c.CycleInputVoltage();if(c.State.SelectedInputVoltage!=12||Math.Abs(c.State.OutputVoltage-12)>1e-10)throw new Exception("Input voltage selector failed.");
            if(c.State.OutputVoltage<=0)throw new Exception("Energized output voltage missing.");c.CycleSecondaryTurns();c.CycleSecondaryTurns();if(c.State.SecondaryTurns!=400)throw new Exception("Turn selector did not cycle correctly.");if(!c.Record())throw new Exception("Valid first data record failed.");
            c.CycleSecondaryTurns();if(!c.Record())throw new Exception("Second turns combination failed.");c.CyclePrimaryTurns();if(!c.Record())throw new Exception("Third turns combination failed.");c.CycleSecondaryTurns();if(!c.Record())throw new Exception("Fourth turns combination failed.");
            foreach(var point in c.Recorder.Points)if(point.Error>1e-10)throw new Exception("Ideal ratio error not near zero.");var display=Object.FindFirstObjectByType<TransformerDataDisplay>();display.Refresh(c.State);if(c.Recorder.Points.Count<c.Settings.minimumDataGroups)throw new Exception("Minimum data group count not reached.");
            c.ResetExperiment();if(c.State.PowerOn||c.State.OutputVoltage!=0||c.Recorder.Points.Count!=0||c.State.PrimaryTurns!=c.Settings.TurnsAt(c.Settings.defaultPrimaryTurnsIndex))throw new Exception("Reset incomplete.");visual.Render(c.State);
            Physics.SyncTransforms();foreach(var control in Object.FindObjectsByType<TransformerInteractable>(FindObjectsSortMode.None)){var target=control.transform.position;var origin=target+new Vector3(0,.1f,-1.35f);if(!Physics.Raycast(origin,(target-origin).normalized,out var hit,3.5f)||hit.collider.GetComponentInParent<TransformerInteractable>()!=control)throw new Exception("Interaction ray blocked: "+control.name);}
            if(GameObject.Find("DesktopPlayer")?.GetComponentInChildren<Animator>()?.isHuman!=true)throw new Exception("Shared Humanoid missing.");Debug.Log("TRANSFORMER_VERIFIED: ideal equation, off-state zero, input/turn controls, four distinct combinations, ratio error, table/graph, reset and interaction rays.");
        }
    }
}

