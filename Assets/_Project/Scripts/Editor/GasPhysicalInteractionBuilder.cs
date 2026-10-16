using System;
using System.IO;
using System.Linq;
using EmbodiedPhysicsLab.Core;
using EmbodiedPhysicsLab.Core.Gas;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmbodiedPhysicsLab.Editor
{
    public static class GasPhysicalInteractionBuilder
    {
        private static TMP_FontAsset font;
        private static Material steel, dark, blue, green, red, hot, cold, wood;
        [MenuItem("Embodied Physics Lab/Repair GasLab Volume Scale")]
        public static void RepairVolumeScale()
        {
            var scene=EditorSceneManager.OpenScene(GasLabBuilder.Lab,OpenSceneMode.Single);
            var controller=Object.FindFirstObjectByType<GasExperimentController>();
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/NotoSansSC SDF.asset");
            steel=Mat("InstrumentSteel");dark=Mat("DisplayBlack");green=Mat("HoldGreen");
            BuildVolumeScale(GameObject.Find("GasCylinder").transform,controller.GetComponent<GasEquipmentInteraction>().PistonSlider.transform,controller.Settings);
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save repaired GasLab scale.");
            Debug.Log("GAS_SCALE_REPAIR_COMPLETE");
        }
        public static void RepairCaptureAndVerify(){RepairVolumeScale();Capture();GasPhysicalVerification.Run();}
        private static void BuildVolumeScale(Transform cylinder,Transform grip,GasExperimentSettings settings)
        {
            Remove("PistonVolumeScale");
            var old=grip.Find("VolumeReadPointer");if(old!=null)Object.DestroyImmediate(old.gameObject);
            var scale=Group("PistonVolumeScale",cylinder);
            float minV=(float)settings.VolumeM3(0),maxV=(float)settings.VolumeM3(settings.VolumeCount-1);
            float lowY=Mathf.Lerp(settings.pistonMinY,settings.pistonMaxY,minV/maxV);
            // Tick ends and moving pointer share one plane; read the pointer, not the grip edge.
            const float planeZ=-.66f;
            Part(scale,"ScaleBacking",new Vector3(.92f,(lowY+settings.pistonMaxY)*.5f,planeZ+.035f),new Vector3(.65f,settings.pistonMaxY-lowY+.16f,.035f),dark);
            Part(scale,"GraduatedRail",new Vector3(.63f,(lowY+settings.pistonMaxY)*.5f,planeZ),new Vector3(.012f,settings.pistonMaxY-lowY+.05f,.012f),steel);
            var tickMaterial=Mat("LabWall");
            for(int i=0;i<settings.VolumeCount;i++)
            {
                float y=Mathf.Lerp(settings.pistonMinY,settings.pistonMaxY,(float)(settings.VolumeM3(i)/settings.VolumeM3(settings.VolumeCount-1)));
                Part(scale,"VolumeTick_"+i,new Vector3(.70f,y,planeZ),new Vector3(.16f,.009f,.012f),tickMaterial);
                var text=Label(scale,$"{settings.VolumeM3(i)*1000:F1} L",new Vector3(1.0f,y,planeZ-.008f),.38f,.09f,17);
                text.gameObject.name="VolumeLabel_"+i;
            }
            var pointer=Group("VolumeReadPointer",grip);
            // A thin bridge connects the actual piston height to a pointer ending at the ticks.
            Part(pointer,"IndicatorLink",new Vector3(.34f,0,planeZ),new Vector3(.42f,.012f,.018f),green);
            Part(pointer,"PointerTip",new Vector3(.58f,0,planeZ),new Vector3(.06f,.018f,.018f),green);
            Sign(scale,"以绿色指针读数",new Vector3(.92f,lowY-.13f,planeZ-.008f),.85f,.10f,15);
            // The previous sensor housing covered the 1.5—2.0 L marks.
            var sensor=cylinder.Find("PressureSensor");if(sensor!=null)sensor.localPosition=new Vector3(1.42f,.58f,.10f);
        }
        [MenuItem("Embodied Physics Lab/Upgrade GasLab Physical Interaction")]
        public static void Upgrade()
        {
            var scene = EditorSceneManager.OpenScene(GasLabBuilder.Lab, OpenSceneMode.Single);
            ApplyToOpenScene(); EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save physical GasLab.");
            AssetDatabase.SaveAssets(); Debug.Log("GAS_PHYSICAL_UPGRADE_COMPLETE");
        }
        public static void UpgradeAndCapture() { Upgrade(); Capture(); }
        public static void UpgradeCaptureAndVerify() { Upgrade(); Capture(); GasPhysicalVerification.Run(); }
        public static void Capture()
        {
            EditorSceneManager.OpenScene(GasLabBuilder.Lab, OpenSceneMode.Single);
            foreach (var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)) text.ForceMeshUpdate(true, true);
            Canvas.ForceUpdateCanvases();
            foreach (var renderer in GameObject.Find("DesktopPlayer").GetComponentsInChildren<Renderer>()) renderer.enabled = false;
            var camera = Camera.main; camera.fieldOfView = 72;
            var position = new Vector3(0, 2.35f, .65f);
            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(new Vector3(0, 1.75f, 5.4f) - position));
            var target = new RenderTexture(1600, 1000, 24); var pixels = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0,0,1600,1000),0,0); pixels.Apply();
            var output = Environment.GetEnvironmentVariable("PHYSICSLAB_PREVIEW_PATH");
            if (string.IsNullOrEmpty(output)) output = Path.Combine(Application.temporaryCachePath,"GasPhysical.png");
            File.WriteAllBytes(output,pixels.EncodeToPNG()); camera.targetTexture = null; RenderTexture.active = null;
            target.Release(); Object.DestroyImmediate(pixels); Object.DestroyImmediate(target);
            EditorSceneManager.OpenScene(GasLabBuilder.Lab, OpenSceneMode.Single);
        }
        public static void ApplyToOpenScene()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != GasLabBuilder.Lab) throw new InvalidOperationException("Only GasLab may be upgraded.");
            var controller = Object.FindFirstObjectByType<GasExperimentController>();
            var previous = controller.GetComponent<GasEquipmentInteraction>(); if (previous != null) Object.DestroyImmediate(previous);
            Remove("PhysicalGasEquipment"); Remove("VolumeControl_A"); Remove("TemperatureControl_B"); Remove("ExperimentControl");
            foreach (var action in Object.FindObjectsByType<GasInteractable>(FindObjectsSortMode.None))
                if (action.Command != GasCommand.Record && action.Command != GasCommand.Reset) Object.DestroyImmediate(action.gameObject);
            controller.Configure(controller.Settings);
            var visual = controller.GetComponent<GasExperimentVisual>(); visual.Render(controller.State);
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/NotoSansSC SDF.asset");
            steel=Mat("InstrumentSteel");dark=Mat("DisplayBlack");blue=Mat("ControlBlue");green=Mat("HoldGreen");
            red=Mat("ResetRed");hot=Mat("HeatOrange");cold=Mat("CoolCyan");wood=Mat("BenchWood");
            var root = Group("PhysicalGasEquipment");
            var cylinder = GameObject.Find("GasCylinder").transform;
            // Keep the complete apparatus above the existing bench, including the base.
            float tableTop = GameObject.Find("GasExperiment/ExperimentalApparatus/BenchTop").GetComponent<Renderer>().bounds.max.y;
            cylinder.position = new Vector3(cylinder.position.x, tableTop - controller.Settings.cylinderBottomY + .07f, cylinder.position.z);
            cylinder.Find("CylinderBase").localScale = new Vector3(1.06f,.045f,1.06f);
            cylinder.Find("UpperRim").localScale = new Vector3(1.03f,.025f,1.03f);
            visual.Piston.localScale = new Vector3(.88f,.035f,.88f);
            foreach (var canvas in cylinder.GetComponentsInChildren<Canvas>()) Object.DestroyImmediate(canvas.gameObject);
            var piston = visual.Piston;
            var oldSlider = piston.GetComponent<AxisSlider>(); if(oldSlider!=null)Object.DestroyImmediate(oldSlider);
            var oldHandle = piston.Find("PhysicalPistonHandle"); if(oldHandle!=null)Object.DestroyImmediate(oldHandle.gameObject);
            // Parent with unit scale so the generic axis constraint operates in metres.
            var grip = Group("PistonGrip_A", root); grip.position = piston.position;
            Part(grip,"PullRod",new Vector3(0,.025f,-.30f),new Vector3(.045f,.045f,.60f),steel);
            Part(grip,"InsulatedCrossHandle",new Vector3(0,.025f,-.64f),new Vector3(.62f,.075f,.10f),blue);
            foreach(float x in new[]{-.28f,.28f})Part(grip,"EndCap",new Vector3(x,.025f,-.64f),new Vector3(.075f,.10f,.12f),dark);
            var hit=grip.gameObject.AddComponent<BoxCollider>();hit.center=new Vector3(0,.025f,-.64f);hit.size=new Vector3(.70f,.17f,.20f);
            var slider=grip.gameObject.AddComponent<AxisSlider>();Set(slider,"localAxis",Vector3.up);
            float lowY = Mathf.Lerp(controller.Settings.pistonMinY,controller.Settings.pistonMaxY,(float)(controller.Settings.VolumeM3(0)/controller.Settings.VolumeM3(controller.Settings.VolumeCount-1)));
            Set(slider,"minimumTravel",0f);Set(slider,"maximumTravel",controller.Settings.pistonMaxY-lowY);
            Set(slider,"interactionPrompt","握住活塞拉柄，上下拖动改变体积；再次左键松开");
            var volumeText=Sign(root,"",new Vector3(-1.30f,1.77f,5.20f),.85f,.30f,22);
            Sign(root,"A · 活塞体积调节",new Vector3(-1.30f,2.17f,5.20f),1.35f,.20f,22);
            Sign(root,"左键握住拉柄 → 上下拖动 → 左键松开\n每个刻度对应一个体积档位",new Vector3(0,.89f,4.53f),2.6f,.25f,19);
            BuildVolumeScale(cylinder,grip,controller.Settings);
            // A three-position mechanical selector uses the same grab interface as the piston.
            var b=Group("TemperatureControl_B",root);Bench(b,new Vector3(-3.7f,0,5.2f),2.25f,1.25f,.96f);
            Part(b,"ThermostatHousing",new Vector3(-3.7f,1.22f,5.15f),new Vector3(1.42f,.43f,.56f),steel);
            Part(b,"ThermostatFace",new Vector3(-3.7f,1.24f,4.855f),new Vector3(1.36f,.37f,.025f),dark);
            var selector=Group("ThermalModeLever",b);selector.position=new Vector3(-3.7f,1.26f,4.79f);
            Part(selector,"LeverStem",new Vector3(0,.12f,0),new Vector3(.035f,.24f,.045f),steel);
            Part(selector,"SelectorGrip",new Vector3(0,.255f,-.025f),new Vector3(.11f,.085f,.09f),blue);
            var leverHit=selector.gameObject.AddComponent<BoxCollider>();leverHit.center=Vector3.up*.15f;leverHit.size=new Vector3(.16f,.32f,.15f);
            var lever=selector.gameObject.AddComponent<LeverSwitch>();Set(lever,"localAxis",Vector3.forward);Set(lever,"stateCount",3);Set(lever,"minimumAngle",-40f);Set(lever,"maximumAngle",40f);Set(lever,"initialState",1);
            Set(lever,"interactionPrompt","握住温控拨杆：左侧加热 / 中间恒温 / 右侧冷却；再次左键松开");
            // Positive rotation about forward tilts the up-pointing lever towards the left.
            Sign(b,"B · 温度控制",new Vector3(-3.7f,2.03f,5.2f),1.8f,.24f,26);
            Label(b,"加热     恒温     冷却",new Vector3(-3.7f,1.73f,4.77f),1.36f,.14f,22);
            var modeText=Sign(b,"",new Vector3(-3.7f,1.05f,4.77f),1.35f,.32f,19);
            var indicator=Part(b,"ThermalStatusLamp",new Vector3(-4.25f,1.38f,4.82f),new Vector3(.10f,.10f,.025f),green).GetComponent<Renderer>();
            for(int i=0;i<6;i++)Part(b,"HeatSinkFin",new Vector3(-4.13f+i*.17f,1.53f,5.25f),new Vector3(.045f,.14f,.38f),i%2==0?hot:cold);
            Sign(b,"拨到恒温，等待温度回稳后再记录",new Vector3(-3.7f,.86f,4.54f),2.0f,.16f,18);
            Cable(root,"ThermalSupplyHose",new[]{new Vector3(-3.05f,1.15f,5.25f),new Vector3(-2.5f,1.15f,5.25f),new Vector3(-2.3f,.65f,5.25f),new Vector3(-.75f,.65f,5.45f),new Vector3(-.50f,1.14f,5.45f)},hot);
            Cable(root,"TemperatureProbeCable",new[]{new Vector3(-3.05f,1.1f,5.5f),new Vector3(-2.35f,1.08f,5.65f),new Vector3(-.6f,1.08f,5.65f),new Vector3(-.4f,1.45f,5.45f)},cold);
            var c=GameObject.Find("ObservationStation_C").transform;
            foreach(var canvas in c.GetComponentsInChildren<Canvas>())if(canvas.transform.parent==c)Object.DestroyImmediate(canvas.gameObject);
            Sign(c,"C · 等温观察与记录",new Vector3(4.15f,2.06f,5.20f),2.2f,.24f,25);
            Label(c,"温度稳定后记录不同体积的数据\n记录至少 4 组，比较 pV 并查看 P-V 曲线",new Vector3(4.15f,1.76f,5.20f),2.2f,.29f,19);
            foreach(var action in Object.FindObjectsByType<GasInteractable>(FindObjectsSortMode.None))
            {
                var cmd=action.Command;var pos=action.transform.position;Object.DestroyImmediate(action.gameObject);
                var button=Group("Control_"+cmd,c);button.position=pos;
                button.gameObject.AddComponent<GasInteractable>().Configure(controller,cmd);
                Part(button,"PhysicalSwitchPlate",Vector3.zero,new Vector3(.40f,.34f,.04f),dark);
                Part(button,"Pushbutton",new Vector3(0,.04f,-.06f),new Vector3(.15f,.15f,.055f),cmd==GasCommand.Record?green:red,true);
                Label(button,cmd==GasCommand.Record?"记录数据":"重置",new Vector3(0,-.105f,-.09f),.34f,.09f,16);
            }
            var bridge=controller.gameObject.AddComponent<GasEquipmentInteraction>();bridge.Configure(controller,slider,lever,volumeText,modeText,indicator);
            var player=GameObject.Find("DesktopPlayer");var grab=player.GetComponent<DesktopGrabInteractor>();if(grab==null)grab=player.AddComponent<DesktopGrabInteractor>();Set(grab,"operationOffset",new Vector3(.25f,-.23f,.95f));
            player.transform.SetPositionAndRotation(new Vector3(0,.04f,1.25f),Quaternion.identity);
            GameObject.Find("Directional Light").GetComponent<Light>().intensity=1.15f;
            Debug.Log("GAS_PHYSICAL_COMPONENTS_CONNECTED");
        }
        private static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Gas/"+name+".mat");
        private static Transform Group(string name,Transform parent=null){var t=new GameObject(name).transform;if(parent!=null)t.SetParent(parent,false);return t;}
        private static GameObject Part(Transform p,string name,Vector3 pos,Vector3 size,Material m,bool collider=false)
        {var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;if(!collider)Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
        private static TMP_Text Label(Transform p,string value,Vector3 pos,float width,float height,int size)
        {
            var root=new GameObject("Label | "+value.Split('\n')[0],typeof(RectTransform),typeof(Canvas));root.transform.SetParent(p,false);root.transform.localPosition=pos;root.transform.localScale=Vector3.one*.004f;root.GetComponent<RectTransform>().sizeDelta=new Vector2(width*250,height*250);root.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            var child=new GameObject("Text",typeof(RectTransform),typeof(TextMeshProUGUI));child.transform.SetParent(root.transform,false);var r=child.GetComponent<RectTransform>();r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;var txt=child.GetComponent<TextMeshProUGUI>();txt.font=font;txt.text=value;txt.fontSize=size;txt.alignment=TextAlignmentOptions.Center;txt.color=Color.white;txt.raycastTarget=false;return txt;
        }
        private static TMP_Text Sign(Transform p,string value,Vector3 pos,float width,float height,int size){Part(p,"InstrumentSign",pos+Vector3.forward*.022f,new Vector3(width+.07f,height+.045f,.035f),dark);return Label(p,value,pos,width,height,size);}
        private static void Bench(Transform p,Vector3 pos,float w,float d,float h){Part(p,"BenchTop",pos+Vector3.up*h,new Vector3(w,.12f,d),wood,true);foreach(float x in new[]{-.43f,.43f})foreach(float z in new[]{-.4f,.4f})Part(p,"BenchLeg",pos+new Vector3(w*x,h*.5f,d*z),new Vector3(.07f,h,.07f),steel,true);}
        private static void Cable(Transform p,string name,Vector3[] points,Material m){var g=Group(name,p);var line=g.gameObject.AddComponent<LineRenderer>();line.sharedMaterial=m;line.startWidth=line.endWidth=.025f;line.useWorldSpace=false;line.positionCount=points.Length;line.SetPositions(points);}
        private static void Set(Object o,string key,float v){var s=new SerializedObject(o);s.FindProperty(key).floatValue=v;s.ApplyModifiedPropertiesWithoutUndo();}
        private static void Set(Object o,string key,int v){var s=new SerializedObject(o);s.FindProperty(key).intValue=v;s.ApplyModifiedPropertiesWithoutUndo();}
        private static void Set(Object o,string key,string v){var s=new SerializedObject(o);s.FindProperty(key).stringValue=v;s.ApplyModifiedPropertiesWithoutUndo();}
        private static void Set(Object o,string key,Vector3 v){var s=new SerializedObject(o);s.FindProperty(key).vector3Value=v;s.ApplyModifiedPropertiesWithoutUndo();}
        private static void Remove(string name){var g=GameObject.Find(name);if(g!=null)Object.DestroyImmediate(g);}
    }
}
