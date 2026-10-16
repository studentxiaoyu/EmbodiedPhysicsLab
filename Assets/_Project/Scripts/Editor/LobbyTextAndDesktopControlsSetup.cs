using System;
using System.IO;
using EmbodiedPhysicsLab.Core;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EmbodiedPhysicsLab.Editor
{
    public static class LobbyTextAndDesktopControlsSetup
    {
        private const string LobbyPath = "Assets/_Project/Scenes/Lobby.unity";
        private const string SourceFontPath = "Assets/_Project/UI/Fonts/NotoSansSC.otf";
        private const string TmpFontPath = "Assets/_Project/UI/Fonts/NotoSansSC SDF.asset";

        public static void Apply()
        {
            EnsureTmpSettings();
            var scene = EditorSceneManager.OpenScene(LobbyPath, OpenSceneMode.Single);
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (sourceFont == null)
                throw new FileNotFoundException("Could not load the project Noto Sans SC font.", SourceFontPath);

            var tmpFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpFontPath);
            if (tmpFont != null && (tmpFont.atlasTextures == null || tmpFont.atlasTextures.Length == 0 || tmpFont.atlasTextures[0] == null))
            {
                AssetDatabase.DeleteAsset(TmpFontPath);
                tmpFont = null;
            }
            if (tmpFont == null)
            {
                tmpFont = TMP_FontAsset.CreateFontAsset(sourceFont);
                if (tmpFont == null) throw new InvalidOperationException("TMP could not create a dynamic Chinese font asset.");
                tmpFont.name = "NotoSansSC SDF";
                AssetDatabase.CreateAsset(tmpFont, TmpFontPath);
                if (tmpFont.material != null)
                    AssetDatabase.AddObjectToAsset(tmpFont.material, tmpFont);
                foreach (var atlas in tmpFont.atlasTextures)
                    if (atlas != null) AssetDatabase.AddObjectToAsset(atlas, tmpFont);
                tmpFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                tmpFont.isMultiAtlasTexturesEnabled = true;
                EditorUtility.SetDirty(tmpFont);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(TmpFontPath, ImportAssetOptions.ForceUpdate);
                tmpFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpFontPath);
                if (tmpFont == null || tmpFont.atlasTextures == null || tmpFont.atlasTextures.Length == 0 || tmpFont.atlasTextures[0] == null)
                    throw new InvalidOperationException("TMP font atlas did not persist as an asset sub-object.");
            }

            var legacyTexts = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None);
            foreach (var legacy in legacyTexts)
            {
                var go = legacy.gameObject;
                var value = legacy.text;
                var fontSize = legacy.fontSize;
                var color = legacy.color;
                var alignment = ConvertAlignment(legacy.alignment);
                var raycastTarget = legacy.raycastTarget;
                var richText = legacy.supportRichText;
                UnityEngine.Object.DestroyImmediate(legacy);

                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.font = tmpFont;
                tmp.text = value;
                tmp.fontSize = fontSize;
                tmp.color = color;
                tmp.alignment = alignment;
                tmp.raycastTarget = raycastTarget;
                tmp.richText = richText;
            }

            var cameraObject = GameObject.FindWithTag("MainCamera");
            if (cameraObject == null) throw new InvalidOperationException("Lobby Main Camera was not found.");
            var controller = cameraObject.GetComponent<DesktopFirstPersonController>() ?? cameraObject.AddComponent<DesktopFirstPersonController>();
            controller.enabled = true;

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the Lobby scene.");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Lobby text migrated to dynamic Chinese TMP font; converted {legacyTexts.Length} text elements and enabled desktop WASD/mouse controls.");
        }

        private static void EnsureTmpSettings()
        {
            if (TMP_Settings.LoadDefaultSettings() != null) return;

            const string settingsPath = "Assets/Resources/TMP Settings.asset";
            Directory.CreateDirectory("Assets/Resources");
            AssetDatabase.Refresh();
            var settings = ScriptableObject.CreateInstance<TMP_Settings>();
            AssetDatabase.CreateAsset(settings, settingsPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (TMP_Settings.LoadDefaultSettings() == null)
                throw new InvalidOperationException("Could not initialize TextMeshPro settings.");
        }
        private static TextAlignmentOptions ConvertAlignment(TextAnchor alignment)
        {
            switch (alignment)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.MidlineLeft;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.MidlineRight;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Center;
            }
        }
    }
}







