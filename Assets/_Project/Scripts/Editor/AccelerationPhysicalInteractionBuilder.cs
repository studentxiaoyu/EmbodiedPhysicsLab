using System;
using System.IO;
using System.Linq;
using EmbodiedPhysicsLab.Core;
using EmbodiedPhysicsLab.Core.Acceleration;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmbodiedPhysicsLab.Editor
{
    /// <summary>Upgrades the current apparatus in place; also called by the original lab builder.</summary>
    public static class AccelerationPhysicalInteractionBuilder
    {
        private static TMP_FontAsset font;
        private static Material steel, blue, dark, green;
        [MenuItem("Embodied Physics Lab/Upgrade AccelerationLab Physical Interaction")]
        public static void Upgrade()
        {
            var scene = EditorSceneManager.OpenScene(AccelerationLabBuilder.Lab, OpenSceneMode.Single);
            ApplyToOpenScene();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Could not save upgraded AccelerationLab.");
            AssetDatabase.SaveAssets();
            AccelerationLabBuilder.Verify();
            Debug.Log("ACCELERATION_PHYSICAL_UPGRADE_COMPLETE");
        }

        public static void UpgradeAndCapture()
        {
            Upgrade();
            EditorSceneManager.OpenScene(AccelerationLabBuilder.Lab, OpenSceneMode.Single);
            foreach (var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)) text.ForceMeshUpdate(true, true);
            Canvas.ForceUpdateCanvases();
            var camera = Camera.main;
            foreach (var renderer in GameObject.Find("DesktopPlayer").GetComponentsInChildren<Renderer>()) renderer.enabled = false;
            camera.fieldOfView = 72f;
            camera.transform.SetPositionAndRotation(new Vector3(-.35f, 2.15f, .65f), Quaternion.LookRotation(new Vector3(-.25f, 1.35f, 4.8f) - new Vector3(-.35f, 2.15f, .65f)));
            var texture = new RenderTexture(1600, 1000, 24);
            var pixels = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
            camera.targetTexture = texture; camera.Render(); RenderTexture.active = texture;
            pixels.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); pixels.Apply();
            var output = Environment.GetEnvironmentVariable("PHYSICSLAB_PREVIEW_PATH");
            if (string.IsNullOrEmpty(output)) output = Path.Combine(Application.temporaryCachePath, "AccelerationPhysical.png");
            File.WriteAllBytes(output, pixels.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = null; texture.Release();
            Object.DestroyImmediate(texture); Object.DestroyImmediate(pixels);
            EditorSceneManager.OpenScene(AccelerationLabBuilder.Lab, OpenSceneMode.Single);
        }

        public static void ApplyToOpenScene()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != AccelerationLabBuilder.Lab)
                throw new InvalidOperationException("Only AccelerationLab may be upgraded.");
            var controller = Object.FindFirstObjectByType<AccelerationExperimentController>();
            var visual = controller.GetComponent<AccelerationExperimentVisual>();
            var old = controller.GetComponent<AccelerationEquipmentInteraction>();
            if (old != null) Object.DestroyImmediate(old);
            var oldRoot = GameObject.Find("PhysicalEquipment");
            if (oldRoot != null) Object.DestroyImmediate(oldRoot);
            visual.UsePhysicalWeights();
            controller.Configure(controller.Settings, controller.Recorder);
            visual.Refresh(controller.State);
            foreach (var action in Object.FindObjectsByType<AccelerationInteractable>(FindObjectsSortMode.None))
            {
                if (action.Command == AccelerationCommand.Start || action.Command == AccelerationCommand.Reset || action.Command == AccelerationCommand.Record) continue;
                if (action.name == "Cart") Object.DestroyImmediate(action);
                else Object.DestroyImmediate(action.gameObject);
            }
            var cartRoot = visual.Cart.Find("CartWeights");
            var bucketRoot = visual.Bucket.Find("BucketWeights");
            Clear(cartRoot); Clear(bucketRoot);
            var areaA = GameObject.Find("A_EnvironmentControl").transform;
            var areaB = GameObject.Find("B_CartMassControl").transform;
            var areaC = GameObject.Find("C_BucketMassControl").transform;
            foreach (var area in new[] { areaA, areaB, areaC })
                foreach (var canvas in area.GetComponentsInChildren<Canvas>()) Object.DestroyImmediate(canvas.gameObject);
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/NotoSansSC SDF.asset");
            steel = Material("Steel"); blue = Material("ControlBlue"); dark = Material("DataScreen"); green = Material("StartGreen");
            var root = Group("PhysicalEquipment");
            var settings = controller.Settings;
            Sign(root, $"B · 小车砝码 {settings.cartWeightMass * 1000:F0} g\n拿起并放入绿色承重槽", new Vector3(-1.7f, 1.18f, 3.62f), 1.05f, .26f, 17);
            Sign(root, $"C · 沙桶配重 {settings.bucketWeightMass * 1000:F0} g\n拿起并从桶口放入", new Vector3(2.8f, 1.29f, 4.96f), 1.05f, .26f, 17);
            var cartHomes = Homes(root, "Cart", settings.maxCartWeights, new Vector3(-1.7f, .99f, 3.35f), "Acceleration.Cart");
            var bucketHomes = Homes(root, "Bucket", settings.maxBucketWeights, new Vector3(2.8f, 1.07f, 4.68f), "Acceleration.Bucket");
            var cartItems = Items(root, "Cart", cartHomes, settings.cartWeightMass, false);
            var bucketItems = Items(root, "Bucket", bucketHomes, settings.bucketWeightMass, true);
            var cartSlots = new SnapSocket[settings.maxCartWeights];
            for (int i = 0; i < cartSlots.Length; i++)
                cartSlots[i] = Socket(cartRoot, "CartLoadSlot_" + i, new Vector3((i % 3 - 1) * .13f, -.015f, (i / 3 - .5f) * .14f), "Acceleration.Cart", .22f);
            var bucketSlots = new SnapSocket[settings.maxBucketWeights];
            for (int i = 0; i < bucketSlots.Length; i++)
                bucketSlots[i] = Socket(bucketRoot, "BucketLoadSlot_" + i, Vector3.up * (.035f * i), "Acceleration.Bucket", .22f, false);

            var angleControl = Group("AngleSliderControl", root);
            angleControl.localPosition = new Vector3(-3.3f, 1.22f, 4.04f);
            Part(angleControl, "SliderFaceplate", Vector3.zero, new Vector3(.92f, .38f, .055f), dark);
            Part(angleControl, "GuideRail", new Vector3(0, 0, -.05f), new Vector3(.76f, .022f, .022f), steel, PrimitiveType.Cube, false);
            var handle = Group("AngleSliderHandle", angleControl);
            handle.localPosition = new Vector3(-.35f, 0, -.09f);
            Part(handle, "Grip", Vector3.zero, new Vector3(.085f, .19f, .09f), blue);
            var slider = handle.gameObject.AddComponent<AxisSlider>();
            Set(slider, "maximumTravel", .7f); Set(slider, "maximumValue", (float)settings.anglesDegrees.Max());
            Set(slider, "minimumValue", (float)settings.anglesDegrees.Min()); Set(slider, "interactionPrompt", "握住调角手柄，左右拖动调节倾角");
            for (int i = 0; i <= 4; i++)
            {
                float x = -.35f + .175f * i;
                Part(angleControl, "AngleScaleTick", new Vector3(x, -.115f, -.04f), new Vector3(.006f, .035f, .01f), steel, PrimitiveType.Cube, false);
            }
            Label(angleControl, $"{settings.anglesDegrees.Min():F0}°             {settings.anglesDegrees.Max():F0}°", new Vector3(0, -.16f, -.04f), .8f, .08f, 15);
            var angleText = Sign(root, "", new Vector3(-3.3f, 1.53f, 4.12f), .92f, .24f, 17);
            var knobBase = Group("FrictionKnobControl", root);
            knobBase.localPosition = new Vector3(-2.57f, 1.20f, 4.04f);
            Part(knobBase, "KnobFaceplate", Vector3.zero, new Vector3(.38f, .38f, .06f), dark);
            var grip = Group("FrictionKnobGrip", knobBase); grip.localPosition = new Vector3(0, 0, -.10f);
            var knobBody = Part(grip, "RotatingGrip", Vector3.zero, new Vector3(.20f, .055f, .20f), blue, PrimitiveType.Cylinder);
            knobBody.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Part(grip, "Pointer", new Vector3(0, .067f, -.062f), new Vector3(.018f, .065f, .018f), green, PrimitiveType.Cube, false);
            var knob = grip.gameObject.AddComponent<RotaryKnob>();
            Set(knob, "minimumAngle", 0f); Set(knob, "maximumAngle", 270f);
            Set(knob, "interactionPrompt", "握住摩擦旋钮，绕中心转动调节");
            var frictionText = Sign(root, "", new Vector3(-2.57f, 1.53f, 4.15f), .48f, .24f, 13);
            Sign(root, "A · 轨道调节\n左键握住手柄，移动视角拖动\n再次左键松开；砝码放稳后开始", new Vector3(-3.10f, 1.90f, 4.55f), 1.25f, .43f, 18);
            var experimentControls = GameObject.Find("ExperimentControl").transform;
            foreach (var canvas in experimentControls.GetComponentsInChildren<Canvas>())
                if (canvas.transform.parent == experimentControls) Object.DestroyImmediate(canvas.gameObject);
            foreach (var action in experimentControls.GetComponentsInChildren<AccelerationInteractable>())
            {
                var button = action.transform.Find("ButtonBody");
                if (button != null) { button.localScale = new Vector3(.14f, .14f, .055f); button.localPosition = new Vector3(0, .055f, -.02f); }
                var plate = action.transform.Find("PhysicalSwitchPlate");
                if (plate != null) Object.DestroyImmediate(plate.gameObject);
                Part(action.transform, "PhysicalSwitchPlate", new Vector3(0, 0, .06f), new Vector3(.31f, .32f, .035f), dark, PrimitiveType.Cube, false);
                foreach (var canvas in action.GetComponentsInChildren<Canvas>())
                {
                    canvas.transform.localPosition = new Vector3(0, -.09f, -.04f);
                    canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(72, 25);
                    var text = canvas.GetComponentInChildren<TMP_Text>(); text.fontSize = 18;
                }
            }
            var equipment = controller.gameObject.AddComponent<AccelerationEquipmentInteraction>();
            equipment.Configure(controller, cartItems, bucketItems, cartHomes, bucketHomes, cartSlots, bucketSlots, slider, knob, angleText, frictionText);
            // Persist the free-body defaults; runtime socket attachment freezes these bodies before simulation.
            foreach (var item in cartItems.Concat(bucketItems))
            {
                var body = item.GetComponent<Rigidbody>(); body.isKinematic = false; body.useGravity = true;
            }
            var player = GameObject.Find("DesktopPlayer");
            var grab = player.GetComponent<DesktopGrabInteractor>();
            if (grab == null) grab = player.AddComponent<DesktopGrabInteractor>();
            var data = new SerializedObject(grab);
            data.FindProperty("operationOffset").vector3Value = new Vector3(.25f, -.27f, .85f);
            data.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("ACCELERATION_PHYSICAL_COMPONENTS_CONNECTED");
        }

        private static SnapSocket[] Homes(Transform root, string prefix, int count, Vector3 center, string key)
        {
            var tray = Group(prefix + "WeightTray", root); tray.position = center;
            Part(tray, "RecessedTray", new Vector3(0, -.012f, 0), new Vector3(1.18f, .025f, .28f), dark);
            var homes = new SnapSocket[count];
            for (int i = 0; i < count; i++) homes[i] = Socket(tray, prefix + "Home_" + i, new Vector3((i - (count - 1) * .5f) * .17f, 0, 0), key, .13f);
            return homes;
        }
        private static SnapObject[] Items(Transform parent, string prefix, SnapSocket[] homes, double mass, bool small)
        {
            var items = new SnapObject[homes.Length];
            for (int i = 0; i < items.Length; i++)
            {
                var root = Group(prefix + "Weight_" + (i + 1), parent);
                root.position = homes[i].transform.position;
                float half = small ? .0125f : .025f;
                Part(root, "MetalMass", new Vector3(0, half, 0), new Vector3(.12f, half, .12f), steel, PrimitiveType.Cylinder);
                Part(root, "GripPin", new Vector3(0, half * 2 + .012f, 0), new Vector3(.035f, .012f, .035f), steel, PrimitiveType.Cylinder);
                var body = root.gameObject.AddComponent<Rigidbody>(); body.mass = (float)mass;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                items[i] = root.gameObject.AddComponent<SnapObject>();
                Set(items[i], "snapKey", "Acceleration." + prefix);
                Set(items[i], "interactionPrompt", $"拿取 {mass * 1000:F0} g {(small ? "沙桶配重" : "小车砝码")}，移近承重槽后放下");
                Label(root, $"{mass * 1000:F0} g", new Vector3(0, half * 2 + .06f, -.065f), .15f, .06f, 12);
            }
            return items;
        }
        private static SnapSocket Socket(Transform parent, string name, Vector3 local, string key, float radius, bool marker = true)
        {
            var root = Group(name, parent); root.localPosition = local;
            if (marker) Part(root, "PlacementMarker", new Vector3(0, -.003f, 0), new Vector3(.14f, .003f, .14f), green, PrimitiveType.Cylinder, false);
            var socket = root.gameObject.AddComponent<SnapSocket>();
            Set(socket, "acceptedKey", key); Set(socket, "snapDistance", radius);
            return socket;
        }
        private static Material Material(string name) => AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Acceleration/" + name + ".mat");
        private static Transform Group(string name, Transform parent = null)
        {
            var result = new GameObject(name).transform;
            if (parent != null) result.SetParent(parent, false);
            return result;
        }
        private static GameObject Part(Transform parent, string name, Vector3 local, Vector3 size, Material material, PrimitiveType shape = PrimitiveType.Cube, bool collider = true)
        {
            var item = GameObject.CreatePrimitive(shape); item.name = name; item.transform.SetParent(parent, false);
            item.transform.localPosition = local; item.transform.localScale = size;
            item.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) Object.DestroyImmediate(item.GetComponent<Collider>());
            return item;
        }
        private static TMP_Text Label(Transform parent, string content, Vector3 local, float width, float height, int size)
        {
            var root = new GameObject("InstrumentLabel", typeof(RectTransform), typeof(Canvas)); root.transform.SetParent(parent, false);
            root.transform.localPosition = local; root.transform.localScale = Vector3.one * .004f;
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(width * 250, height * 250);
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var child = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)); child.transform.SetParent(root.transform, false);
            var rect = child.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = child.GetComponent<TextMeshProUGUI>(); text.font = font; text.fontSize = size; text.text = content;
            text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false; text.color = Color.white;
            return text;
        }
        private static TMP_Text Sign(Transform parent, string content, Vector3 local, float width, float height, int size)
        {
            Part(parent, "InstrumentPlacard", local + Vector3.forward * .025f, new Vector3(width, height, .035f), dark, PrimitiveType.Cube, false);
            return Label(parent, content, local, width * .96f, height * .93f, size);
        }
        private static void Set(Object item, string property, float value) { var data = new SerializedObject(item); data.FindProperty(property).floatValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Set(Object item, string property, string value) { var data = new SerializedObject(item); data.FindProperty(property).stringValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Clear(Transform root) { for (int i = root.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.GetChild(i).gameObject); }
    }
}
