using System;
using System.IO;
using System.Linq;
using EmbodiedPhysicsLab.Core;
using EmbodiedPhysicsLab.Core.Transformer;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmbodiedPhysicsLab.Editor
{
    public static class TransformerPhysicalInteractionBuilder
    {
        private static Material steel, copper, dark, primaryColor, secondaryColor, green;
        private static TMP_FontAsset font;
        [MenuItem("Embodied Physics Lab/Upgrade TransformerLab Physical Interaction")]
        public static void Upgrade()
        {
            var scene = EditorSceneManager.OpenScene(TransformerLabBuilder.Lab, OpenSceneMode.Single);
            ApplyToOpenScene();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save physical TransformerLab.");
            AssetDatabase.SaveAssets();
            Debug.Log("TRANSFORMER_PHYSICAL_UPGRADE_COMPLETE");
        }
        public static void UpgradeAndCapture() { Upgrade(); Capture(); }
        public static void Capture()
        {
            EditorSceneManager.OpenScene(TransformerLabBuilder.Lab, OpenSceneMode.Single);
            foreach (var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)) text.ForceMeshUpdate(true, true);
            Canvas.ForceUpdateCanvases();
            foreach (var renderer in GameObject.Find("DesktopPlayer").GetComponentsInChildren<Renderer>()) renderer.enabled = false;
            var camera = Camera.main; camera.fieldOfView = 74;
            var position = new Vector3(-.5f, 2.4f, .25f);
            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(new Vector3(-.3f, 1.65f, 5.4f) - position));
            var target = new RenderTexture(1600, 1000, 24);
            var pixels = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); pixels.Apply();
            var output = Environment.GetEnvironmentVariable("PHYSICSLAB_PREVIEW_PATH");
            if (string.IsNullOrEmpty(output)) output = Path.Combine(Application.temporaryCachePath, "TransformerPhysical.png");
            File.WriteAllBytes(output, pixels.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = null; target.Release();
            Object.DestroyImmediate(pixels); Object.DestroyImmediate(target);
            EditorSceneManager.OpenScene(TransformerLabBuilder.Lab, OpenSceneMode.Single);
        }

        public static void ApplyToOpenScene()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != TransformerLabBuilder.Lab)
                throw new InvalidOperationException("Only TransformerLab may be upgraded.");
            var controller = Object.FindFirstObjectByType<TransformerExperimentController>();
            var visual = controller.GetComponent<TransformerExperimentVisual>();
            var previous = controller.GetComponent<TransformerEquipmentInteraction>();
            if (previous != null) Object.DestroyImmediate(previous);
            Remove("PhysicalTransformerEquipment");
            controller.Configure(controller.Settings);
            foreach (var action in Object.FindObjectsByType<TransformerInteractable>(FindObjectsSortMode.None))
                if (action.Command != TransformerCommand.Record && action.Command != TransformerCommand.Reset) Object.DestroyImmediate(action.gameObject);
            Remove("ExperimentControl"); Remove("PrimaryControl_A");
            var apparatus = GameObject.Find("CoreAndCoils").transform;
            Clear(apparatus);
            var areaB = GameObject.Find("TurnsControl_B").transform;
            var areaC = GameObject.Find("MeasurementStation_C").transform;
            foreach (var canvas in areaB.GetComponentsInChildren<Canvas>()) Object.DestroyImmediate(canvas.gameObject);
            foreach (var canvas in areaC.GetComponentsInChildren<Canvas>())
                if (canvas.transform.parent == areaC) Object.DestroyImmediate(canvas.gameObject);
            var source = GameObject.Find("ACSource").transform;
            foreach (var canvas in source.GetComponentsInChildren<Canvas>()) Object.DestroyImmediate(canvas.gameObject);
            var oldSwitch = source.Find("PowerSwitch"); if (oldSwitch != null) Object.DestroyImmediate(oldSwitch.gameObject);
            var oldInstrument = areaC.Find("MeasurementBenchInstrument"); if (oldInstrument != null) Object.DestroyImmediate(oldInstrument.gameObject);
            Remove("PrimaryWiring"); Remove("SecondaryWiring");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/NotoSansSC SDF.asset");
            steel = Mat("CoreSteel"); copper = Mat("CopperWinding"); dark = Mat("DataScreen"); primaryColor = Mat("ControlBlue"); secondaryColor = Mat("SourceOrange"); green = Mat("PowerGreen");
            steel.EnableKeyword("_EMISSION"); steel.SetColor("_EmissionColor", Color.black); EditorUtility.SetDirty(steel);
            var root = Group("PhysicalTransformerEquipment");
            var core = Group("UShapedIronCore", apparatus);
            Part(core, "LaminatedLeftLimb", new Vector3(-.42f, 1.535f, 0), new Vector3(.10f, .67f, .16f), steel);
            Part(core, "LaminatedRightLimb", new Vector3(.42f, 1.535f, 0), new Vector3(.10f, .67f, .16f), steel);
            Part(core, "LowerYoke", new Vector3(0, 1.15f, 0), new Vector3(1.12f, .10f, .20f), steel);
            Part(core, "InsulatingCoreBase", new Vector3(0, 1.075f, 0), new Vector3(1.28f, .05f, .44f), dark);
            var primarySocket = Socket(apparatus, "PrimaryCoilMount", new Vector3(-.42f, 1.50f, 0), "Transformer.Primary", .28f, false);
            var secondarySocket = Socket(apparatus, "SecondaryCoilMount", new Vector3(.42f, 1.50f, 0), "Transformer.Secondary", .28f, false);
            foreach (var socket in new[] { primarySocket, secondarySocket })
            {
                Part(socket.transform, "ModuleSeat", new Vector3(0, -.255f, 0), new Vector3(.35f, .045f, .35f), dark);
                Part(socket.transform, "ContactBlock", new Vector3(0, -.25f, -.20f), new Vector3(.19f, .05f, .065f), copper);
            }
            var settings = controller.Settings;
            var primary = new SnapObject[settings.TurnsCount]; var secondary = new SnapObject[settings.TurnsCount];
            var primaryHomes = new SnapSocket[primary.Length]; var secondaryHomes = new SnapSocket[secondary.Length];
            Part(root, "SecondaryCoilRackRiser", new Vector3(0, 1.13f, 3.55f), new Vector3(2.25f, .24f, .44f), dark, true);
            for (int i = 0; i < settings.TurnsCount; i++)
            {
                float x = (i - (settings.TurnsCount - 1) * .5f) * .56f;
                primaryHomes[i] = Socket(root, "PrimaryStorage_" + settings.TurnsAt(i), new Vector3(x, 1.27f, 3.02f), "Transformer.Primary", .22f, true);
                secondaryHomes[i] = Socket(root, "SecondaryStorage_" + settings.TurnsAt(i), new Vector3(x, 1.49f, 3.55f), "Transformer.Secondary", .22f, true);
                primary[i] = Coil(root, "Primary", i, settings.TurnsAt(i), primaryHomes[i].transform.position, primaryColor);
                secondary[i] = Coil(root, "Secondary", i, settings.TurnsAt(i), secondaryHomes[i].transform.position, secondaryColor);
            }
            var yokeSocket = Socket(apparatus, "UpperYokeMount", new Vector3(0, 1.93f, 0), "Transformer.Yoke", .32f, false);
            var yokeHome = Socket(root, "UpperYokeStorage", new Vector3(1.30f, 1.08f, 3.25f), "Transformer.Yoke", .25f, true);
            yokeHome.transform.rotation = Quaternion.Euler(0, 90, 0);
            yokeHome.transform.Find("StorageMarker").localPosition = Vector3.down * .061f;
            yokeHome.transform.Find("StorageMarker").localScale = new Vector3(1.15f, .008f, .25f);
            var yokeRoot = Group("RemovableUpperYoke", root); yokeRoot.position = yokeSocket.transform.position;
            Part(yokeRoot, "LaminatedUpperYoke", Vector3.zero, new Vector3(1.12f, .12f, .20f), steel);
            Part(yokeRoot, "HandleLeftPost", new Vector3(-.18f, .12f, 0), new Vector3(.04f, .13f, .055f), dark);
            Part(yokeRoot, "HandleRightPost", new Vector3(.18f, .12f, 0), new Vector3(.04f, .13f, .055f), dark);
            Part(yokeRoot, "CarryHandle", new Vector3(0, .20f, 0), new Vector3(.40f, .04f, .055f), dark);
            var cap = Grabbable(yokeRoot, "Transformer.Yoke", "握住提手，移开 / 合上上铁轭", "请先断电，再移开上铁轭", new Vector3(1.12f, .29f, .24f), Vector3.up * .07f);
            Sign(root, "B · 线圈换装台   |   断电 → 移开上铁轭 → 换线圈 → 合上铁轭", new Vector3(0, .87f, 2.67f), 2.65f, .22f, 18);
            Sign(root, "原线圈\nN1", new Vector3(-1.20f, 1.43f, 3.10f), .32f, .27f, 17);
            Sign(root, "副线圈\nN2", new Vector3(-1.20f, 1.43f, 3.64f), .32f, .27f, 17);
            Sign(root, "上铁轭\n暂存位", new Vector3(1.20f, 1.44f, 3.82f), .34f, .27f, 15);
            var assemblyText = Sign(root, "", new Vector3(0, 2.37f, 5.55f), 2.15f, .28f, 20);
            foreach (float x in new[] { -.90f, .90f }) Part(root, "AssemblyReadoutStand", new Vector3(x, 1.70f, 5.58f), new Vector3(.025f, 1.32f, .025f), steel);

            var sourcePanel = Group("A_SourcePhysicalControls", root);
            var voltageRoot = Group("VoltageKnob", sourcePanel); voltageRoot.position = new Vector3(-4.42f, 1.29f, 3.91f);
            var body = RoundPart(voltageRoot, "KnobBody", new Vector3(.22f, .055f, .22f), primaryColor); body.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Part(voltageRoot, "Pointer", new Vector3(0, .075f, -.065f), new Vector3(.015f, .07f, .018f), green);
            var knob = voltageRoot.gameObject.AddComponent<RotaryKnob>(); Set(knob, "minimumAngle", 0f); Set(knob, "maximumAngle", 270f);
            Set(knob, "interactionPrompt", "握住调压旋钮，转动调节交流输入电压");
            var leverRoot = Group("ACPowerLever", sourcePanel); leverRoot.position = new Vector3(-3.93f, 1.28f, 3.90f); leverRoot.localRotation = Quaternion.Euler(-25, 0, 0);
            Part(leverRoot, "LeverStem", new Vector3(0, .09f, 0), new Vector3(.035f, .18f, .035f), steel);
            Part(leverRoot, "InsulatedGrip", new Vector3(0, .19f, 0), new Vector3(.12f, .065f, .07f), primaryColor);
            var leverCollider = leverRoot.gameObject.AddComponent<BoxCollider>(); leverCollider.size = new Vector3(.14f, .28f, .12f); leverCollider.center = Vector3.up * .10f;
            var lever = leverRoot.gameObject.AddComponent<LeverSwitch>(); Set(lever, "stateCount", 2); Set(lever, "minimumAngle", 0f); Set(lever, "maximumAngle", 50f);
            Set(lever, "interactionPrompt", "握住电源拨杆，拨到 0 断电 / 1 通电");
            Sign(sourcePanel, "A · 低压交流电源", new Vector3(-4.15f, 1.71f, 3.92f), 1.00f, .14f, 21);
            var selectedVoltage = Sign(sourcePanel, "", new Vector3(-4.42f, 1.54f, 3.92f), .42f, .18f, 23);
            Label(sourcePanel, $"{settings.inputVoltageOptions.Min():F0}—{settings.inputVoltageOptions.Max():F0} V", new Vector3(-4.42f, 1.11f, 3.89f), .42f, .1f, 15);
            Label(sourcePanel, "0 / 1", new Vector3(-3.91f, 1.10f, 3.89f), .32f, .1f, 17);
            var indicator = source.Find("PowerIndicator"); if (indicator != null) indicator.GetComponent<Renderer>().sharedMaterial = green;
            var meterDisplay = Object.FindFirstObjectByType<TransformerMeterDisplay>();
            var meterData = new SerializedObject(meterDisplay);
            var inputText = meterData.FindProperty("inputText").objectReferenceValue as TMP_Text;
            var outputText = meterData.FindProperty("outputText").objectReferenceValue as TMP_Text;
            var inputMeter = GameObject.Find("InputVoltmeter").transform; var outputMeter = GameObject.Find("OutputVoltmeter").transform;
            MoveMeter(inputMeter, new Vector3(-1.65f, 1.60f, 5.95f), inputText, root);
            MoveMeter(outputMeter, new Vector3(4.15f, 1.75f, 5.1f), outputText, root);
            meterDisplay.Configure(controller, inputText, outputText, selectedVoltage);
            var note = areaC.Find("MeasurementBenchInstrument"); if (note != null) Object.DestroyImmediate(note.gameObject);
            Sign(root, "C · 输出测量与数据记录\n换一组线圈后，再通电读取 U2", new Vector3(4.15f, 2.55f, 5.4f), 1.8f, .30f, 20);
            ReplaceRecordButtons(controller);
            Cables(root);
            visual.UsePhysicalEquipment(core.GetComponentsInChildren<Renderer>().Concat(yokeRoot.GetComponentsInChildren<Renderer>()).ToArray());
            var bridge = controller.gameObject.AddComponent<TransformerEquipmentInteraction>();
            bridge.Configure(controller, visual, primary, secondary, primaryHomes, secondaryHomes, primarySocket, secondarySocket, cap, yokeSocket, yokeHome, knob, lever, assemblyText);
            foreach (var item in primary.Concat(secondary).Append(cap)) { var rb = item.GetComponent<Rigidbody>(); rb.isKinematic = false; rb.useGravity = true; }
            var player = GameObject.Find("DesktopPlayer"); var grab = player.GetComponent<DesktopGrabInteractor>(); if (grab == null) grab = player.AddComponent<DesktopGrabInteractor>();
            var grabData = new SerializedObject(grab); grabData.FindProperty("operationOffset").vector3Value = new Vector3(.25f, -.23f, .95f); grabData.ApplyModifiedPropertiesWithoutUndo();
            LayoutDisplay(root, controller);
            foreach (var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)) text.text = text.text.Replace((char)0x2081, '1').Replace((char)0x2082, '2');
            var sun = GameObject.Find("Directional Light").GetComponent<Light>(); sun.intensity = 1.15f;
            Debug.Log("TRANSFORMER_PHYSICAL_COMPONENTS_CONNECTED");
        }

        private static SnapObject Coil(Transform parent, string role, int option, int turns, Vector3 position, Material color)
        {
            var root = Group(role + "Coil_" + turns, parent); root.position = position;
            foreach (float x in new[] { -.12f, .12f }) Part(root, "BobbinSide", new Vector3(x, 0, 0), new Vector3(.025f, .43f, .25f), color);
            foreach (float y in new[] { -.225f, .225f })
            {
                Part(root, "BobbinFlangeFront", new Vector3(0, y, -.13f), new Vector3(.34f, .035f, .06f), color);
                Part(root, "BobbinFlangeBack", new Vector3(0, y, .13f), new Vector3(.34f, .035f, .06f), color);
                Part(root, "BobbinFlangeLeft", new Vector3(-.14f, y, 0), new Vector3(.06f, .035f, .28f), color);
                Part(root, "BobbinFlangeRight", new Vector3(.14f, y, 0), new Vector3(.06f, .035f, .28f), color);
            }
            int count = 4 + option * 2;
            for (int i = 0; i < count; i++)
            {
                float y = Mathf.Lerp(-.17f, .17f, (float)i / Mathf.Max(1, count - 1));
                Part(root, "CopperFront", new Vector3(0, y, -.145f), new Vector3(.31f, .019f, .023f), copper);
                Part(root, "CopperBack", new Vector3(0, y, .145f), new Vector3(.31f, .019f, .023f), copper);
                Part(root, "CopperLeft", new Vector3(-.145f, y, 0), new Vector3(.023f, .019f, .31f), copper);
                Part(root, "CopperRight", new Vector3(.145f, y, 0), new Vector3(.023f, .019f, .31f), copper);
            }
            Sign(root, (role == "Primary" ? "N1" : "N2") + $"\n{turns} 匝", new Vector3(0, 0, -.174f), .28f, .17f, 14);
            return Grabbable(root, "Transformer." + role, $"拿取 {turns} 匝{(role == "Primary" ? "原" : "副")}线圈，放入对应铁芯托架", "请先断电并移开上铁轭，再取出线圈", new Vector3(.34f, .48f, .34f), Vector3.zero);
        }
        private static SnapObject Grabbable(Transform root, string key, string prompt, string locked, Vector3 bounds, Vector3 center)
        {
            var collider = root.gameObject.AddComponent<BoxCollider>(); collider.size = bounds; collider.center = center;
            var body = root.gameObject.AddComponent<Rigidbody>(); body.mass = .3f; body.interpolation = RigidbodyInterpolation.Interpolate; body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            var item = root.gameObject.AddComponent<SnapObject>(); Set(item, "snapKey", key); Set(item, "interactionPrompt", prompt); Set(item, "lockedPrompt", locked); return item;
        }
        private static SnapSocket Socket(Transform parent, string name, Vector3 position, string key, float radius, bool marker)
        {
            var root = Group(name, parent); root.localPosition = position;
            if (marker) Part(root, "StorageMarker", new Vector3(0, -.24f, 0), new Vector3(.36f, .008f, .36f), dark);
            var socket = root.gameObject.AddComponent<SnapSocket>(); Set(socket, "acceptedKey", key); Set(socket, "snapDistance", radius); return socket;
        }
        private static void MoveMeter(Transform meter, Vector3 position, TMP_Text numeric, Transform parent)
        {
            meter.position = position;
            meter.Find("MeterHousing").localScale = new Vector3(.52f, .055f, .52f);
            meter.Find("MeterFace").localScale = new Vector3(.46f, .008f, .46f);
            var needle = meter.Find("NeedlePivot/Needle"); needle.localPosition = Vector3.up * .10f; needle.localScale = new Vector3(.014f, .20f, .014f);
            foreach (var canvas in meter.GetComponentsInChildren<Canvas>())
            {
                var text = canvas.GetComponentInChildren<TMP_Text>(); bool value = text == numeric;
                canvas.transform.localPosition = new Vector3(0, value ? -.36f : text.text.Contains("MAX") ? -.15f : .40f, -.11f);
                canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(250, value ? 70 : 50); text.fontSize = value ? 28 : text.text.Contains("MAX") ? 13 : 22;
            }
            float top = position.y - .20f;
            Part(parent, "VoltmeterSupport", new Vector3(position.x, (1.05f + top) / 2, position.z + .03f), new Vector3(.035f, top - 1.05f, .035f), steel);
            Part(parent, "VoltmeterBase", new Vector3(position.x, 1.07f, position.z + .03f), new Vector3(.36f, .03f, .24f), dark);
        }
        private static void ReplaceRecordButtons(TransformerExperimentController controller)
        {
            foreach (var action in Object.FindObjectsByType<TransformerInteractable>(FindObjectsSortMode.None))
            {
                var position = action.transform.position; var command = action.Command; Object.DestroyImmediate(action.gameObject);
                var root = Group("Control_" + command); root.position = position;
                root.gameObject.AddComponent<TransformerInteractable>().Configure(controller, command);
                Part(root, "PhysicalSwitchPlate", Vector3.zero, new Vector3(.32f, .32f, .035f), dark);
                Part(root, "Pushbutton", new Vector3(0, .05f, -.05f), new Vector3(.13f, .13f, .05f), command == TransformerCommand.Record ? green : Mat("ResetRed"), true);
                Label(root, command == TransformerCommand.Record ? "记录" : "Reset", new Vector3(0, -.10f, -.03f), .29f, .09f, 19);
            }
        }
        private static void Cables(Transform root)
        {
            Cable(root, "PrimaryFixedLead", Mat("PrimaryCable"), new[] { new Vector3(-3.72f, 1.13f, 3.94f), new Vector3(-2.6f, 1.08f, 4.6f), new Vector3(-.42f, 1.25f, 5.05f) });
            Cable(root, "PrimaryReturnLead", Mat("PrimaryCable"), new[] { new Vector3(-.55f, 1.25f, 5.05f), new Vector3(-1.65f, 1.12f, 5.7f), new Vector3(-3.72f, 1.13f, 4.1f) });
            Cable(root, "SecondaryFixedLead", Mat("SecondaryCable"), new[] { new Vector3(.42f, 1.25f, 5.05f), new Vector3(2.25f, 1.08f, 5.5f), new Vector3(2.75f, .85f, 5.5f), new Vector3(3.4f, 1.08f, 5.5f), new Vector3(4.05f, 1.28f, 5.1f) });
            Cable(root, "SecondaryReturnLead", Mat("SecondaryCable"), new[] { new Vector3(.55f, 1.25f, 5.05f), new Vector3(2.25f, 1.08f, 5.65f), new Vector3(2.75f, .85f, 5.65f), new Vector3(3.4f, 1.08f, 5.65f), new Vector3(4.25f, 1.28f, 5.1f) });
        }
        private static void Cable(Transform parent, string name, Material mat, Vector3[] points)
        {
            var line = Group(name, parent).gameObject.AddComponent<LineRenderer>(); line.sharedMaterial = mat; line.startWidth = line.endWidth = .012f; line.useWorldSpace = true; line.positionCount = points.Length; line.SetPositions(points);
        }
        private static void LayoutDisplay(Transform root, TransformerExperimentController controller)
        {
            var display = Object.FindFirstObjectByType<TransformerDataDisplay>();
            var data = new SerializedObject(display);
            TextLayout(data.FindProperty("valuesText").objectReferenceValue as TMP_Text, new Vector3(-2.65f, 3.18f, 10.47f), 4.8f, 1.30f, 38);
            TextLayout(data.FindProperty("tableText").objectReferenceValue as TMP_Text, new Vector3(1.55f, 3.1f, 10.47f), 3.6f, 1.35f, 22);
            TextLayout(data.FindProperty("statusText").objectReferenceValue as TMP_Text, new Vector3(-2.65f, 2.13f, 10.47f), 4.8f, .32f, 22);
            Label(root, "电磁学实验室 · 理想变压器匝数与电压关系", new Vector3(-1.0f, 4.02f, 10.47f), 7.0f, .30f, 34);
            display.Refresh(controller.State);
        }
        private static void TextLayout(TMP_Text text, Vector3 local, float width, float height, int size)
        {
            var canvas = text.transform.parent.GetComponent<RectTransform>(); canvas.localPosition = local;
            canvas.sizeDelta = new Vector2(width * 250, height * 250); text.fontSize = size;
        }
        private static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Transformer/" + name + ".mat");
        private static Transform Group(string name, Transform parent = null) { var root = new GameObject(name).transform; if (parent != null) root.SetParent(parent, false); return root; }
        private static GameObject Part(Transform parent, string name, Vector3 local, Vector3 size, Material material, bool collider = false)
        {
            var item = GameObject.CreatePrimitive(PrimitiveType.Cube); item.name = name; item.transform.SetParent(parent, false); item.transform.localPosition = local; item.transform.localScale = size; item.GetComponent<Renderer>().sharedMaterial = material; if (!collider) Object.DestroyImmediate(item.GetComponent<Collider>()); return item;
        }
        private static GameObject RoundPart(Transform parent, string name, Vector3 size, Material material)
        {
            var item = GameObject.CreatePrimitive(PrimitiveType.Cylinder); item.name = name; item.transform.SetParent(parent, false); item.transform.localScale = size; item.GetComponent<Renderer>().sharedMaterial = material; return item;
        }
        private static TMP_Text Sign(Transform parent, string text, Vector3 position, float width, float height, int size)
        {
            Part(parent, "InstrumentPlacard", position + Vector3.forward * .02f, new Vector3(width, height, .025f), dark);
            return Label(parent, text, position, width * .96f, height * .95f, size);
        }
        private static TMP_Text Label(Transform parent, string content, Vector3 local, float width, float height, int size)
        {
            var root = new GameObject("InstrumentLabel", typeof(RectTransform), typeof(Canvas)); root.transform.SetParent(parent, false); root.transform.localPosition = local; root.transform.localScale = Vector3.one * .004f;
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(width * 250, height * 250); root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var child = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)); child.transform.SetParent(root.transform, false);
            var rect = child.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = child.GetComponent<TextMeshProUGUI>(); text.font = font; text.text = content; text.fontSize = size; text.color = Color.white; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false; return text;
        }
        private static void Set(Object target, string key, float value) { var data = new SerializedObject(target); data.FindProperty(key).floatValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Set(Object target, string key, int value) { var data = new SerializedObject(target); data.FindProperty(key).intValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Set(Object target, string key, string value) { var data = new SerializedObject(target); data.FindProperty(key).stringValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Clear(Transform root) { for (int i = root.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.GetChild(i).gameObject); }
        private static void Remove(string name) { var item = GameObject.Find(name); if (item != null) Object.DestroyImmediate(item); }
    }
}
