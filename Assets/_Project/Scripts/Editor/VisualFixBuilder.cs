using System;
using System.Linq;
using EmbodiedPhysicsLab.Core;
using EmbodiedPhysicsLab.Core.Gas;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace EmbodiedPhysicsLab.Editor
{
    public static class VisualFixBuilder
    {
        private const string LayerName="LocalAvatarHead";
        private const string PlayerPrefab="Assets/_Project/Prefabs/Players/DesktopPlayer.prefab";
        private const string AvatarPrefab="Assets/_Project/Prefabs/Players/DesktopHumanoidAvatar.prefab";
        private const string GasScene="Assets/_Project/Scenes/GasLab.unity";
        [MenuItem("Embodied Physics Lab/Fix Avatar Head and Gas Cylinder Layout")]
        public static void Apply()
        {
            int layer=EnsureLayer(LayerName);
            ConfigurePrefab(PlayerPrefab,layer);
            ConfigurePrefab(AvatarPrefab,layer);
            FixGasScene();
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            Debug.Log("VISUAL_FIX_COMPLETE: local camera excludes only LocalAvatarHead; other cameras retain it. Gas cylinder base rests above bench surface.");
        }
        private static int EnsureLayer(string name)
        {
            var asset=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset").FirstOrDefault();
            if(asset==null)throw new InvalidOperationException("Could not load TagManager settings.");
            var serialized=new SerializedObject(asset);var layers=serialized.FindProperty("layers");int empty=-1;
            for(int i=8;i<layers.arraySize;i++)
            {
                var layer=layers.GetArrayElementAtIndex(i);
                if(layer.stringValue==name)return i;
                if(empty<0&&string.IsNullOrEmpty(layer.stringValue))empty=i;
            }
            if(empty<0)throw new InvalidOperationException("No free user layer is available for the local avatar head.");
            layers.GetArrayElementAtIndex(empty).stringValue=name;serialized.ApplyModifiedProperties();return empty;
        }
        private static void ConfigurePrefab(string path,int layer)
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var components=root.GetComponentsInChildren<FirstPersonAvatarVisibility>(true);
                if(components.Length==0)throw new InvalidOperationException("FirstPersonAvatarVisibility is missing in "+path);
                foreach(var component in components)
                {
                    component.ConfigureHeadLayer(LayerName);
                    if(component.HeadRenderers==null||component.HeadRenderers.Length==0)throw new InvalidOperationException("Head renderer list is empty in "+path);
                    foreach(var renderer in component.HeadRenderers)
                    {
                        if(renderer==null)throw new InvalidOperationException("Missing head renderer reference in "+path);
                        renderer.gameObject.layer=layer;renderer.shadowCastingMode=ShadowCastingMode.On;EditorUtility.SetDirty(renderer);
                    }
                    EditorUtility.SetDirty(component);
                }
                var cameras=root.GetComponentsInChildren<Camera>(true).Where(c=>c.CompareTag("MainCamera")).ToArray();
                foreach(var camera in cameras)camera.cullingMask|=1<<layer;
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        private static void FixGasScene()
        {
            var scene=EditorSceneManager.OpenScene(GasScene,OpenSceneMode.Single);
            var cylinder=GameObject.Find("GasCylinder");if(cylinder==null)throw new InvalidOperationException("GasCylinder root missing in GasLab.");
            var settings=AssetDatabase.LoadAssetAtPath<GasExperimentSettings>("Assets/_Project/Experiments/Gas/GasSettings.asset");if(settings==null)throw new InvalidOperationException("Gas settings asset missing.");
            const float tableTopCenterY=1.0f,tableTopThickness=.12f,baseHalfHeight=.07f;
            float benchSurface=tableTopCenterY+tableTopThickness*.5f;
            float rootY=benchSurface-(settings.cylinderBottomY-baseHalfHeight);
            var position=cylinder.transform.localPosition;cylinder.transform.localPosition=new Vector3(position.x,rootY,position.z);
            float baseLowest=cylinder.transform.position.y+settings.cylinderBottomY-baseHalfHeight;
            if(baseLowest<benchSurface-1e-4f)throw new InvalidOperationException("Gas cylinder base still intersects the benchtop.");
            EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("GasLab save failed.");
        }
    }
}
