using System.IO;
using EmbodiedPhysicsLab.Core;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EmbodiedPhysicsLab.Editor
{
    public static class ProjectSceneGenerator
    {
        private const string Root = "Assets/_Project";
        private static Font uiFont;

        [MenuItem("Embodied Physics Lab/Generate First Scenes")]
        public static void GenerateFirstScenes()
        {
            EnsureFolders();
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            CreateScene("Lobby", true, "多人具身化虚拟物理实验室", new[]
            {
                ("加速度与力、质量的关系", "AccelerationLab"),
                ("气体等温变化规律", "GasLab"),
                ("变压器匝数与电压关系", "TransformerLab")
            });

            CreateScene("AccelerationLab", false, "加速度与力、质量的关系", null);
            CreateScene("GasLab", false, "气体等温变化规律", null);
            CreateScene("TransformerLab", false, "变压器匝数与电压关系", null);

            AssetDatabase.Refresh();
            EditorBuildSettings.scenes = new[]
            {
                BuildScene("Lobby"),
                BuildScene("AccelerationLab"),
                BuildScene("GasLab"),
                BuildScene("TransformerLab")
            };
            AssetDatabase.SaveAssets();
            Debug.Log("Generated four first-version lab scenes and updated Build Settings.");
        }

        private static void EnsureFolders()
        {
            foreach (var folder in new[]
            {
                "Scenes", "Scripts", "Scripts/Core", "Scripts/Editor",
                "Prefabs", "Prefabs/Common", "Materials", "UI"
            })
            {
                Directory.CreateDirectory(Path.Combine(Root, folder));
            }
            AssetDatabase.Refresh();
        }

        private static void CreateScene(string name, bool lobby, string title, (string label, string scene)[] entries)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateEnvironment(lobby);
            CreateUi(title, entries);
            var path = $"{Root}/Scenes/{name}.unity";
            if (!EditorSceneManager.SaveScene(scene, path))
            {
                throw new IOException($"Could not save scene at {path}");
            }
        }

        private static void CreateEnvironment(bool lobby)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.position = new Vector3(0f, -0.12f, 0f);
            floor.transform.localScale = new Vector3(16f, 0.24f, 16f);
            floor.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Floor Material", new Color(0.22f, 0.28f, 0.34f));

            if (lobby)
            {
                CreateWall("Back Wall", new Vector3(0f, 2.1f, 7.8f), new Vector3(16f, 4.2f, 0.25f));
                CreateWall("Left Wall", new Vector3(-7.8f, 2.1f, 0f), new Vector3(0.25f, 4.2f, 16f));
                CreateWall("Right Wall", new Vector3(7.8f, 2.1f, 0f), new Vector3(0.25f, 4.2f, 16f));
            }

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(1f, 0.94f, 0.82f);
            lightObject.transform.rotation = Quaternion.Euler(48f, -30f, 0f);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 150f;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.position = new Vector3(0f, lobby ? 6.5f : 5.5f, -12f);
            cameraObject.transform.rotation = Quaternion.Euler(lobby ? 17f : 15f, 0f, 0f);
        }

        private static void CreateWall(string name, Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.position = position;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Wall Material", new Color(0.38f, 0.46f, 0.52f));
        }

        private static Material MakeMaterial(string name, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = name, color = color };
            return material;
        }

        private static void CreateUi(string title, (string label, string scene)[] entries)
        {
            var canvasObject = new GameObject("UI Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var inputModule = eventObject.GetComponent<InputSystemUIInputModule>();
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (actions == null) throw new IOException("Could not load the existing Input System actions asset.");
            var ui = actions.FindActionMap("UI", true);
            inputModule.actionsAsset = actions;
            inputModule.point = InputActionReference.Create(ui.FindAction("Point", true));
            inputModule.leftClick = InputActionReference.Create(ui.FindAction("Click", true));
            inputModule.rightClick = InputActionReference.Create(ui.FindAction("RightClick", true));
            inputModule.middleClick = InputActionReference.Create(ui.FindAction("MiddleClick", true));
            inputModule.scrollWheel = InputActionReference.Create(ui.FindAction("ScrollWheel", true));
            inputModule.move = InputActionReference.Create(ui.FindAction("Navigate", true));
            inputModule.submit = InputActionReference.Create(ui.FindAction("Submit", true));
            inputModule.cancel = InputActionReference.Create(ui.FindAction("Cancel", true));

            var panel = CreateRect("Panel", canvas.transform, new Vector2(900f, entries == null ? 420f : 700f));
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0.055f, 0.09f, 0.13f, 0.88f);

            var titleRect = CreateRect("Title", panel, new Vector2(820f, entries == null ? 130f : 150f));
            titleRect.anchoredPosition = new Vector2(0f, entries == null ? 115f : 245f);
            CreateText(title, titleRect, entries == null ? 48 : 46, Color.white);

            if (entries != null)
            {
                for (var i = 0; i < entries.Length; i++)
                {
                    var buttonRect = CreateRect($"Open {entries[i].scene}", panel, new Vector2(760f, 82f));
                    buttonRect.anchoredPosition = new Vector2(0f, 95f - i * 112f);
                    CreateButton(entries[i].label, buttonRect, entries[i].scene);
                }
            }
            else
            {
                var buttonRect = CreateRect("Return to Lobby", panel, new Vector2(520f, 78f));
                buttonRect.anchoredPosition = new Vector2(0f, -135f);
                CreateButton("返回实验大厅", buttonRect, "Lobby");
            }
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 size)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            return rect;
        }

        private static void CreateText(string value, RectTransform parent, int size, Color color)
        {
            var gameObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(16f, 8f);
            rect.offsetMax = new Vector2(-16f, -8f);
            var text = gameObject.GetComponent<Text>();
            text.font = uiFont;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
        }

        private static void CreateButton(string label, RectTransform rect, string targetScene)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.08f, 0.48f, 0.62f, 1f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = new Color(0.08f, 0.48f, 0.62f, 1f);
            colors.highlightedColor = new Color(0.12f, 0.62f, 0.76f, 1f);
            colors.pressedColor = new Color(0.05f, 0.34f, 0.46f, 1f);
            button.colors = colors;

            var navigator = rect.gameObject.AddComponent<SceneNavigator>();
            navigator.TargetScene = targetScene;
            UnityEventTools.AddPersistentListener(button.onClick, navigator.Navigate);
            CreateText(label, rect, 28, Color.white);
        }

        private static EditorBuildSettingsScene BuildScene(string name)
        {
            var path = $"{Root}/Scenes/{name}.unity";
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
            {
                throw new IOException($"No asset GUID found for {path}");
            }
            return new EditorBuildSettingsScene(path, true);
        }
    }
}
