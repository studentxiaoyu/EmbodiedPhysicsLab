using System.IO;
using EmbodiedPhysicsLab.Core;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EmbodiedPhysicsLab.Editor
{
    public static class LobbyVisualUpgrade
    {
        private const string LobbyPath = "Assets/_Project/Scenes/Lobby.unity";
        private static Transform architecture;
        private static Transform decor;
        private static Transform portals;
        private static Transform exhibits;
        private static Transform information;
        private static Material floorMat, wallMat, ceilingMat, trimMat, metalMat, darkMat;
        private static Material tealMat, amberMat, violetMat, glowMat;
        private static Font uiFont;
        private static Camera lobbyCamera;
        private static GameObject eventSystemObject;

        [MenuItem("Embodied Physics Lab/Upgrade Lobby Visuals")]
        public static void Upgrade()
        {
            var scene = EditorSceneManager.OpenScene(LobbyPath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            GameObject cameraObject = null;
            GameObject lightObject = null;
            GameObject floorObject = null;
            eventSystemObject = null;

            foreach (var root in roots)
            {
                if (root.name == "Main Camera") cameraObject = root;
                else if (root.name == "Directional Light") lightObject = root;
                else if (root.name == "Floor") floorObject = root;
                else if (root.GetComponent<EventSystem>() != null) eventSystemObject = root;
            }

            if (cameraObject == null || lightObject == null || floorObject == null || eventSystemObject == null)
                throw new IOException("Lobby is missing its camera, light, floor, or EventSystem.");

            foreach (var root in scene.GetRootGameObjects())
            {
                if (root == cameraObject || root == lightObject || root == floorObject || root == eventSystemObject)
                    continue;
                Object.DestroyImmediate(root);
            }

            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lobbyCamera = cameraObject.GetComponent<Camera>();
            if (lobbyCamera == null) lobbyCamera = cameraObject.AddComponent<Camera>();
            lobbyCamera.tag = "MainCamera";
            lobbyCamera.clearFlags = CameraClearFlags.Skybox;
            lobbyCamera.fieldOfView = 62f;
            lobbyCamera.nearClipPlane = 0.1f;
            lobbyCamera.farClipPlane = 160f;
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 4.6f, -6.5f), Quaternion.Euler(9f, 0f, 0f));
            if (cameraObject.GetComponent<AudioListener>() == null) cameraObject.AddComponent<AudioListener>();

            var directional = lightObject.GetComponent<Light>();
            if (directional == null) directional = lightObject.AddComponent<Light>();
            directional.type = LightType.Directional;
            directional.intensity = 1.05f;
            directional.color = new Color(0.82f, 0.89f, 1f);
            lightObject.transform.rotation = Quaternion.Euler(46f, -28f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.31f, 0.37f, 0.44f);

            floorMat = MakeMaterial("Lobby | Graphite floor", new Color(0.12f, 0.17f, 0.22f));
            wallMat = MakeMaterial("Lobby | Cool ivory wall", new Color(0.67f, 0.73f, 0.77f));
            ceilingMat = MakeMaterial("Lobby | Ceiling", new Color(0.16f, 0.21f, 0.26f));
            trimMat = MakeMaterial("Lobby | Brushed brass", new Color(0.69f, 0.50f, 0.22f), true);
            metalMat = MakeMaterial("Exhibit | Satin metal", new Color(0.40f, 0.48f, 0.54f));
            darkMat = MakeMaterial("Exhibit | Dark frame", new Color(0.055f, 0.075f, 0.09f));
            tealMat = MakeMaterial("Lab accent | Mechanics", new Color(0.08f, 0.70f, 0.77f), true);
            amberMat = MakeMaterial("Lab accent | Thermal", new Color(0.98f, 0.47f, 0.12f), true);
            violetMat = MakeMaterial("Lab accent | Electromagnetics", new Color(0.56f, 0.38f, 0.92f), true);
            glowMat = MakeMaterial("Portal | Soft luminous glass", new Color(0.14f, 0.45f, 0.57f), true);

            architecture = Group("Architecture");
            decor = Group("Laboratory Decor");
            portals = Group("Laboratory Entrances");
            exhibits = Group("Preview Exhibits");
            information = Group("Entrance Information");

            ConfigureFloor(floorObject);
            BuildRoom();
            BuildRoomDecoration();
            BuildLabBay(-9f, "力学实验室", "加速度与力、质量的关系",
                "轨道小车与砝码展示｜探索运动、受力和质量之间的联系。",
                "AccelerationLab", tealMat, BuildMechanicsExhibit);
            BuildLabBay(0f, "热学实验室", "气体等温变化规律",
                "气缸与活塞展示｜观察封闭气体状态变化的实验装置。",
                "GasLab", amberMat, BuildThermalExhibit);
            BuildLabBay(9f, "电磁学实验室", "变压器匝数与电压关系",
                "双线圈与铁芯展示｜认识变压器的主要结构。",
                "TransformerLab", violetMat, BuildElectromagnetismExhibit);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException("Could not save the upgraded Lobby scene.");

            AssetDatabase.SaveAssets();
            Debug.Log("Upgraded the existing Lobby scene in place. All three SceneNavigator targets are retained.");
        }

        private static void ConfigureFloor(GameObject floor)
        {
            floor.name = "Lobby Floor";
            floor.transform.SetPositionAndRotation(new Vector3(0f, -0.16f, 4f), Quaternion.identity);
            floor.transform.localScale = new Vector3(30f, 0.32f, 30f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;
        }

        private static void BuildRoom()
        {
            // A complete enclosed room; the camera sits inside near the front entrance.
            Cube(architecture, "Left Perimeter Wall", new Vector3(-15f, 4.5f, 4f), new Vector3(0.32f, 9f, 30f), wallMat);
            Cube(architecture, "Right Perimeter Wall", new Vector3(15f, 4.5f, 4f), new Vector3(0.32f, 9f, 30f), wallMat);
            Cube(architecture, "Front Perimeter Wall", new Vector3(0f, 4.5f, -11f), new Vector3(30f, 9f, 0.32f), wallMat);
            Cube(architecture, "Rear Perimeter Wall", new Vector3(0f, 4.5f, 19f), new Vector3(30f, 9f, 0.32f), wallMat);
            Cube(architecture, "Full Ceiling", new Vector3(0f, 9.08f, 4f), new Vector3(30f, 0.24f, 30f), ceilingMat);

            Cube(architecture, "Front Wall Brass Course", new Vector3(0f, 8.7f, -10.79f), new Vector3(29.7f, 0.10f, 0.06f), trimMat);
            Cube(architecture, "Rear Wall Brass Course", new Vector3(0f, 8.7f, 18.79f), new Vector3(29.7f, 0.10f, 0.06f), trimMat);
            Cube(architecture, "Left Wall Brass Course", new Vector3(-14.79f, 8.7f, 4f), new Vector3(0.06f, 0.10f, 29.7f), trimMat);
            Cube(architecture, "Right Wall Brass Course", new Vector3(14.79f, 8.7f, 4f), new Vector3(0.06f, 0.10f, 29.7f), trimMat);

            var titleBacking = Cube(decor, "Main Title Plaque", new Vector3(0f, 7.45f, 18.69f), new Vector3(22f, 1.55f, 0.20f), darkMat);
            Cube(decor, "Title Plaque Accent", new Vector3(0f, 6.62f, 18.55f), new Vector3(14f, 0.055f, 0.08f), trimMat);
            WorldTitle("多人具身化协同虚拟物理实验室", new Vector3(0f, 7.48f, 18.53f), new Vector2(2100f, 150f), 76);

            // Branded entrance surrounds with recessed luminous door panels.
            foreach (var x in new[] {-9f, 0f, 9f})
            {
                var accent = x < -1f ? tealMat : (x > 1f ? violetMat : amberMat);
                Cube(portals, "Entrance Portal | Left jamb", new Vector3(x - 2.65f, 2.45f, 16.35f), new Vector3(0.38f, 4.9f, 0.62f), metalMat);
                Cube(portals, "Entrance Portal | Right jamb", new Vector3(x + 2.65f, 2.45f, 16.35f), new Vector3(0.38f, 4.9f, 0.62f), metalMat);
                Cube(portals, "Entrance Portal | Header", new Vector3(x, 4.88f, 16.35f), new Vector3(5.65f, 0.40f, 0.62f), metalMat);
                Cube(portals, "Entrance Portal | Threshold", new Vector3(x, 0.10f, 16.05f), new Vector3(5.1f, 0.20f, 1.15f), accent);
                Cube(portals, "Entrance Portal | Recessed panel", new Vector3(x, 2.43f, 17.05f), new Vector3(4.85f, 4.45f, 0.10f), darkMat);
                Cube(portals, "Entrance Portal | Light line", new Vector3(x, 2.43f, 16.93f), new Vector3(0.065f, 3.9f, 0.08f), accent);
                Cube(portals, "Entrance Portal | Sign band", new Vector3(x, 4.52f, 16.89f), new Vector3(3.6f, 0.11f, 0.08f), accent);
                Cube(portals, "Portal approach | Accent runner", new Vector3(x, 0.012f, 12f), new Vector3(3.1f, 0.025f, 6.2f), accent);
                Cube(portals, "Portal approach | Edge left", new Vector3(x - 1.7f, 0.018f, 12f), new Vector3(0.055f, 0.03f, 6.2f), trimMat);
                Cube(portals, "Portal approach | Edge right", new Vector3(x + 1.7f, 0.018f, 12f), new Vector3(0.055f, 0.03f, 6.2f), trimMat);
            }
        }

        private static void BuildRoomDecoration()
        {
            // Ceiling fixtures and soft point lights.
            foreach (var x in new[] {-9f, 0f, 9f})
            foreach (var z in new[] {2f, 10f})
            {
                Cube(decor, "Recessed ceiling light", new Vector3(x, 8.91f, z), new Vector3(4.1f, 0.08f, 1.05f), MakeMaterial("LED diffuser", new Color(0.82f, 0.91f, 1f), true));
                var lightObject = new GameObject("Ceiling area light");
                lightObject.transform.SetParent(decor, true);
                lightObject.transform.position = new Vector3(x, 8.25f, z);
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(0.73f, 0.85f, 1f);
                light.intensity = 2.0f;
                light.range = 13f;
            }

            // Side work surfaces, storage and wall panels frame the central exhibition floor.
            foreach (var side in new[] {-1f, 1f})
            {
                var x = side * 12.7f;
                Cube(decor, "Side laboratory workbench", new Vector3(x, 1.05f, 4f), new Vector3(2.7f, 0.24f, 11f), metalMat);
                Cube(decor, "Workbench base", new Vector3(x, 0.50f, 4f), new Vector3(2.25f, 0.95f, 10.4f), darkMat);
                foreach (var z in new[] {0f, 4f, 8f})
                {
                    Cube(decor, "Storage cabinet", new Vector3(side * 13.65f, 1.0f, z), new Vector3(1.75f, 1.9f, 2.15f), darkMat);
                    Cube(decor, "Cabinet handle", new Vector3(side * 12.74f, 1.0f, z), new Vector3(0.06f, 0.50f, 0.08f), trimMat);
                    Cube(decor, "Wall display frame", new Vector3(side * 14.76f, 4.6f, z + 1.3f), new Vector3(0.08f, 2.7f, 2.9f), metalMat);
                    Cube(decor, "Wall display inset", new Vector3(side * 14.70f, 4.6f, z + 1.3f), new Vector3(0.07f, 2.42f, 2.62f), darkMat);
                }
            }

            Cube(decor, "Reception demonstration table", new Vector3(0f, 0.62f, -0.2f), new Vector3(9.4f, 1.24f, 1.55f), metalMat);
            Cube(decor, "Reception table top", new Vector3(0f, 1.30f, -0.2f), new Vector3(9.8f, 0.14f, 1.8f), trimMat);
            Cube(decor, "Welcome direction board", new Vector3(0f, 2.24f, -0.98f), new Vector3(6.2f, 0.75f, 0.13f), darkMat);
            WorldTitle("力学   •   热学   •   电磁学", new Vector3(0f, 2.26f, -1.06f), new Vector2(1100f, 120f), 58);
        }

        private static void BuildLabBay(float x, string category, string experiment, string description, string target, Material accent, System.Action<float, Material> exhibitBuilder)
        {
            // Raised exhibit island with an adjacent interactive entry card.
            Cube(exhibits, category + " | Display plinth", new Vector3(x, 0.52f, 8.25f), new Vector3(5.8f, 1.04f, 3.2f), darkMat);
            Cube(exhibits, category + " | Display top", new Vector3(x, 1.08f, 8.25f), new Vector3(6.0f, 0.12f, 3.35f), accent);
            Cube(exhibits, category + " | Plinth front", new Vector3(x, 0.66f, 6.62f), new Vector3(5.8f, 0.52f, 0.08f), metalMat);
            exhibitBuilder(x, accent);

            // Individual wayfinding panels sit in each bay instead of covering the screen center.
            var signPost = Cube(information, category + " | Information stand", new Vector3(x, 1.05f, 3.35f), new Vector3(0.14f, 2.1f, 0.14f), metalMat);
            var signBase = Cube(information, category + " | Information base", new Vector3(x, 0.12f, 3.35f), new Vector3(1.55f, 0.24f, 0.72f), accent);
            WorldCard(x, category, experiment, description, target, accent);
        }

        private static void BuildMechanicsExhibit(float x, Material accent)
        {
            var z = 8.2f;
            var y = 1.28f;
            Cube(exhibits, "Mechanics | Rail left", new Vector3(x, y + 0.30f, z - 0.47f), new Vector3(4.65f, 0.11f, 0.10f), metalMat);
            Cube(exhibits, "Mechanics | Rail right", new Vector3(x, y + 0.30f, z + 0.47f), new Vector3(4.65f, 0.11f, 0.10f), metalMat);
            for (var i = -2; i <= 2; i++)
                Cube(exhibits, "Mechanics | Rail sleeper", new Vector3(x + i * 0.9f, y + 0.20f, z), new Vector3(0.12f, 0.12f, 1.25f), trimMat);

            Cube(exhibits, "Mechanics | Cart chassis", new Vector3(x + 0.20f, y + 0.70f, z), new Vector3(1.45f, 0.30f, 1.05f), accent);
            Cube(exhibits, "Mechanics | Cart top", new Vector3(x + 0.20f, y + 0.90f, z), new Vector3(1.18f, 0.12f, 0.82f), metalMat);
            foreach (var dx in new[] {-0.34f, 0.72f})
            foreach (var dz in new[] {-0.49f, 0.49f})
                Cylinder(exhibits, "Mechanics | Cart wheel", new Vector3(x + dx, y + 0.48f, z + dz), new Vector3(0.22f, 0.11f, 0.22f), darkMat, Quaternion.Euler(90f, 0f, 0f));
            Cylinder(exhibits, "Mechanics | Weight one", new Vector3(x - 1.45f, y + 0.55f, z), new Vector3(0.36f, 0.28f, 0.36f), trimMat);
            Cylinder(exhibits, "Mechanics | Weight two", new Vector3(x - 1.45f, y + 0.92f, z), new Vector3(0.28f, 0.18f, 0.28f), accent);
            Cylinder(exhibits, "Mechanics | Weight cap", new Vector3(x - 1.45f, y + 1.18f, z), new Vector3(0.20f, 0.07f, 0.20f), metalMat);
        }

        private static void BuildThermalExhibit(float x, Material accent)
        {
            var z = 8.2f;
            var baseY = 1.28f;
            Cylinder(exhibits, "Thermal | Cylinder base", new Vector3(x, baseY + 0.15f, z), new Vector3(1.1f, 0.30f, 1.1f), metalMat);
            Cylinder(exhibits, "Thermal | Gas cylinder", new Vector3(x, baseY + 1.25f, z), new Vector3(0.82f, 1.55f, 0.82f), accent);
            Cylinder(exhibits, "Thermal | Inner chamber", new Vector3(x, baseY + 1.30f, z), new Vector3(0.57f, 1.30f, 0.57f), MakeMaterial("Thermal | chamber", new Color(0.34f, 0.72f, 0.78f)));
            Cylinder(exhibits, "Thermal | Piston", new Vector3(x, baseY + 2.03f, z), new Vector3(0.96f, 0.14f, 0.96f), trimMat);
            Cube(exhibits, "Thermal | Piston rod", new Vector3(x, baseY + 2.62f, z), new Vector3(0.14f, 1.05f, 0.14f), metalMat);
            Cylinder(exhibits, "Thermal | Handle", new Vector3(x, baseY + 3.16f, z), new Vector3(0.55f, 0.10f, 0.55f), darkMat);
            Cube(exhibits, "Thermal | Scale board", new Vector3(x + 1.25f, baseY + 1.32f, z), new Vector3(0.12f, 1.7f, 0.42f), darkMat);
            for (var i = 0; i < 5; i++)
                Cube(exhibits, "Thermal | Scale marking", new Vector3(x + 1.17f, baseY + 0.75f + i * 0.28f, z - 0.22f), new Vector3(0.15f, 0.035f, 0.08f), trimMat);
        }

        private static void BuildElectromagnetismExhibit(float x, Material accent)
        {
            var z = 8.2f;
            var baseY = 1.28f;
            Cube(exhibits, "Electromagnetism | Iron core bridge", new Vector3(x, baseY + 1.42f, z), new Vector3(3.05f, 0.34f, 0.42f), metalMat);
            foreach (var coilX in new[] {x - 1.05f, x + 1.05f})
            {
                Cylinder(exhibits, "Electromagnetism | Coil bobbin", new Vector3(coilX, baseY + 0.65f, z), new Vector3(0.54f, 0.65f, 0.54f), darkMat);
                for (var i = 0; i < 7; i++)
                    Cylinder(exhibits, "Electromagnetism | Copper winding", new Vector3(coilX, baseY + 0.18f + i * 0.16f, z), new Vector3(0.74f, 0.075f, 0.74f), accent);
                Cylinder(exhibits, "Electromagnetism | Iron core", new Vector3(coilX, baseY + 0.70f, z), new Vector3(0.22f, 0.72f, 0.22f), metalMat);
            }
            Cube(exhibits, "Electromagnetism | Core leg left", new Vector3(x - 1.05f, baseY + 0.18f, z), new Vector3(0.28f, 0.42f, 0.34f), metalMat);
            Cube(exhibits, "Electromagnetism | Core leg right", new Vector3(x + 1.05f, baseY + 0.18f, z), new Vector3(0.28f, 0.42f, 0.34f), metalMat);
            Cube(exhibits, "Electromagnetism | Primary label", new Vector3(x - 1.05f, baseY + 1.45f, z - 0.45f), new Vector3(0.85f, 0.22f, 0.06f), darkMat);
            Cube(exhibits, "Electromagnetism | Secondary label", new Vector3(x + 1.05f, baseY + 1.45f, z - 0.45f), new Vector3(0.85f, 0.22f, 0.06f), darkMat);
        }

        private static void WorldCard(float x, string category, string experiment, string description, string target, Material accent)
        {
            var root = new GameObject(category + " | Interactive information card", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(information, false);
            root.transform.position = new Vector3(x, 2.30f, 3.40f);
            root.transform.rotation = Quaternion.LookRotation(lobbyCamera.transform.position - root.transform.position, Vector3.up);
            root.transform.localScale = Vector3.one * 0.003f;
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(700f, 720f);

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = lobbyCamera;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 12f;
            var bg = UiRect("Card background", rect, new Vector2(700f, 720f), Vector2.zero, root.transform);
            var bgImage = bg.gameObject.AddComponent<Image>();
            bgImage.color = new Color(0.045f, 0.075f, 0.10f, 0.96f);

            var top = UiRect("Card accent", rect, new Vector2(700f, 18f), new Vector2(0f, 351f), root.transform);
            top.gameObject.AddComponent<Image>().color = accent.color;

            var heading = UiRect("Discipline heading", rect, new Vector2(640f, 104f), new Vector2(0f, 275f), root.transform);
            AddText(category, heading, 40, accent.color);
            var name = UiRect("Experiment name", rect, new Vector2(640f, 148f), new Vector2(0f, 145f), root.transform);
            AddText(experiment, name, 43, Color.white);
            var copy = UiRect("Experiment description", rect, new Vector2(610f, 190f), new Vector2(0f, -45f), root.transform);
            AddText(description, copy, 27, new Color(0.78f, 0.85f, 0.88f));

            var buttonRect = UiRect("Enter experiment", rect, new Vector2(560f, 120f), new Vector2(0f, -265f), root.transform);
            var image = buttonRect.gameObject.AddComponent<Image>();
            image.color = accent.color;
            var button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = accent.color;
            colors.highlightedColor = Color.Lerp(accent.color, Color.white, 0.20f);
            colors.pressedColor = Color.Lerp(accent.color, Color.black, 0.20f);
            button.colors = colors;

            var navigator = buttonRect.gameObject.AddComponent<SceneNavigator>();
            navigator.TargetScene = target;
            UnityEventTools.AddPersistentListener(button.onClick, navigator.Navigate);
            var buttonText = UiRect("Button label", buttonRect, new Vector2(540f, 108f), Vector2.zero, buttonRect);
            AddText("进入实验", buttonText, 34, Color.white);
        }

        private static void WorldTitle(string value, Vector3 position, Vector2 size, int fontSize)
        {
            var root = new GameObject("World title | " + value, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(decor, false);
            root.transform.position = position;
            root.transform.rotation = Quaternion.LookRotation(lobbyCamera.transform.position - position, Vector3.up);
            root.transform.localScale = Vector3.one * 0.01f;
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = lobbyCamera;
            root.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;
            var textRect = UiRect("Title text", rect, size, Vector2.zero, root.transform);
            AddText(value, textRect, fontSize, Color.white);
        }

        private static RectTransform UiRect(string name, RectTransform parent, Vector2 size, Vector2 position, Transform owner)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(owner, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private static void AddText(string value, RectTransform rect, int size, Color color)
        {
            var text = rect.gameObject.AddComponent<Text>();
            text.font = uiFont;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
        }

        private static Transform Group(string name)
        {
            var go = new GameObject(name);
            return go.transform;
        }

        private static GameObject Cube(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(position, Quaternion.identity);
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            return go;
        }

        private static GameObject Cylinder(Transform parent, string name, Vector3 position, Vector3 scale, Material material, Quaternion? rotation = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(position, rotation ?? Quaternion.identity);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static Material MakeMaterial(string name, Color color, bool emissive = false)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) throw new IOException("Could not find a usable material shader.");
            var material = new Material(shader) { name = name, color = color };
            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.2f);
            }
            return material;
        }
    }
}
