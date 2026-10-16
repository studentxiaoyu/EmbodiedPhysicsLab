using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using EmbodiedPhysicsLab.Core;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EmbodiedPhysicsLab.Editor
{
    public static class LobbySocialBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Lobby.unity";
        private const string Source = "Assets/Samples/Meta XR Movement SDK/201.0.0/Body Tracking Samples/Body/Models/Realistic.fbx";
        private const string AvatarFolder = "Assets/_Project/Avatars/MovementRealistic";
        private const string AvatarPrefab = "Assets/_Project/Prefabs/Players/DesktopHumanoidAvatar.prefab";
        private const string MaterialFolder = "Assets/_Project/Materials/Lobby";
        private static TMP_FontAsset font;
        private static Material plaster, floor, wood, fabric, cushion, metal, glass, carpet, foliage, paper, screen, accent;
        private static Transform exterior, architecture, lounge, furniture, reception, identity, corridor, doors;

        public static void BuildAndCapture()
        {
            Build();
            Capture();
        }

        [MenuItem("Embodied Physics Lab/Build Social Lobby and Humanoid")]
        public static void Build()
        {
            MigrateAvatarAssets();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var player = GameObject.Find("DesktopPlayer");
            var camera = GameObject.FindWithTag("MainCamera")?.GetComponent<Camera>();
            if (player == null || camera == null) throw new IOException("Existing DesktopPlayer/Main Camera are required.");
            foreach (var root in scene.GetRootGameObjects())
                if (root != player && root.GetComponent<UnityEngine.EventSystems.EventSystem>() == null && root.name != "Interaction HUD")
                    Object.DestroyImmediate(root);
            var oldBody = player.transform.Find("Body Visual (no head)");
            if (oldBody != null) Object.DestroyImmediate(oldBody.gameObject);
            var oldAvatar = player.transform.Find("HumanoidAvatar");
            if (oldAvatar != null) Object.DestroyImmediate(oldAvatar.gameObject);
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/NotoSansSC SDF.asset");
            if (font == null) throw new IOException("Chinese TMP font is missing.");
            MakeMaterials();
            exterior = Group("ExteriorFoyer");
            architecture = Group("LobbyArchitecture");
            lounge = Group("SocialLounge");
            furniture = Group("TablesAndChairs", lounge);
            reception = Group("ReceptionAndInformation");
            identity = Group("CollegeIdentityPlaceholders");
            corridor = Group("ExperimentCorridor");
            doors = Group("LaboratoryDoors", corridor);
            BuildExterior();
            BuildHall();
            BuildLounge();
            BuildIdentityAndInformation();
            BuildCorridor();
            InstallAvatar(player, camera);
            AddLighting();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Lobby save failed.");
            AssetDatabase.SaveAssets();
            Validate();
            Debug.Log("SOCIAL_LOBBY_BUILD_COMPLETE: existing Lobby modified; lounge, separate corridor, three doors, full Humanoid installed.");
        }

        private static void MigrateAvatarAssets()
        {
            foreach (var sub in new[]{"Models", "Materials", "Textures"}) Directory.CreateDirectory(AvatarFolder + "/" + sub);
            Directory.CreateDirectory("Assets/_Project/Prefabs/Players");
            AssetDatabase.Refresh();
            var dependencies = AssetDatabase.GetDependencies(Source, true);
            var copies = new Dictionary<string, string>();
            foreach (var path in dependencies)
            {
                if (!path.StartsWith("Assets/Samples/")) continue;
                var extension = Path.GetExtension(path).ToLowerInvariant();
                string sub = extension == ".fbx" ? "Models" : extension == ".mat" ? "Materials" : extension == ".png" ? "Textures" : null;
                if (sub == null) continue;
                var target = AvatarFolder + "/" + sub + "/" + Path.GetFileName(path);
                if (!File.Exists(target) && !AssetDatabase.CopyAsset(path, target)) throw new IOException("Copy failed: " + path);
                copies[path] = target;
            }
            foreach (var copy in copies.Where(c => c.Key.EndsWith(".mat")))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(copy.Value);
                foreach (var property in material.GetTexturePropertyNames())
                {
                    var texture = material.GetTexture(property);
                    if (texture != null && copies.TryGetValue(AssetDatabase.GetAssetPath(texture), out var target))
                        material.SetTexture(property, AssetDatabase.LoadAssetAtPath<Texture>(target));
                }
                EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();
            var importer = (ModelImporter)AssetImporter.GetAtPath(copies[Source]);
            foreach (var mapping in importer.GetExternalObjectMap().ToArray())
                if (mapping.Value != null && copies.TryGetValue(AssetDatabase.GetAssetPath(mapping.Value), out var target))
                    importer.AddRemap(mapping.Key, AssetDatabase.LoadAssetAtPath<Material>(target));
            importer.SaveAndReimport();
            Debug.Log("AVATAR_MIGRATION: " + copies.Count + " direct assets copied; package shaders reused without changes.");
        }

        private static void InstallAvatar(GameObject player, Camera camera)
        {
            var avatar = new GameObject("HumanoidAvatar");
            var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(AvatarFolder + "/Models/Realistic.fbx"), avatar.transform);
            model.name = "MovementRealistic";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            var animator = model.GetComponentInChildren<Animator>();
            if (animator == null || !animator.isHuman || !animator.avatar.isValid) throw new InvalidOperationException("Valid Humanoid Avatar required.");
            animator.runtimeAnimatorController = null;
            animator.enabled = false; // A saved desktop rest pose; tracking can take over this same skeleton later.
            PoseArm(animator, true);
            PoseArm(animator, false);
            var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var renderer in renderers) renderer.updateWhenOffscreen = true;
            var heads = renderers.Where(r => r.name.StartsWith("head_") || r.name.StartsWith("mouth_") || r.name.StartsWith("cornea")).Cast<Renderer>().ToArray();
            if (heads.Length != 4) throw new InvalidOperationException("Unexpected head mesh layout.");
            avatar.AddComponent<FirstPersonAvatarVisibility>().Configure(heads);
            PrefabUtility.SaveAsPrefabAsset(avatar, AvatarPrefab);
            Object.DestroyImmediate(avatar);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(AvatarPrefab));
            instance.transform.SetParent(player.transform, false);
            instance.name = "HumanoidAvatar";
            player.transform.SetPositionAndRotation(new Vector3(0f, 0.04f, -4.4f), Quaternion.identity);
            var rig = instance.GetComponentInChildren<Animator>();
            var eye = (rig.GetBoneTransform(HumanBodyBones.LeftEye).position + rig.GetBoneTransform(HumanBodyBones.RightEye).position) * 0.5f;
            camera.transform.localPosition = player.transform.InverseTransformPoint(eye);
            camera.transform.localRotation = Quaternion.identity;
            camera.nearClipPlane = 0.04f;
            camera.fieldOfView = 72f;
            var controller = player.GetComponent<CharacterController>();
            controller.height = 1.75f;
            controller.center = new Vector3(0f, 0.875f, 0f);
            controller.radius = 0.30f;
            foreach (var extra in player.GetComponentsInChildren<Collider>(true))
                if (extra != controller)
                {
                    Debug.Log("REMOVED_PLAYER_SELF_COLLIDER " + extra.gameObject.name + "/" + extra.GetType().Name);
                    Object.DestroyImmediate(extra);
                }
            player.GetComponent<DesktopFirstPersonController>().Configure(controller, camera.transform);
        }

        private static void PoseArm(Animator animator, bool left)
        {
            var upper = animator.GetBoneTransform(left ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm);
            var lower = animator.GetBoneTransform(left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
            var hand = animator.GetBoneTransform(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
            var finger = animator.GetBoneTransform(left ? HumanBodyBones.LeftMiddleProximal : HumanBodyBones.RightMiddleProximal);
            var side = left ? -1f : 1f;
            upper.rotation = Quaternion.FromToRotation(lower.position - upper.position, new Vector3(side * 0.07f, -0.06f, 0.22f)) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, new Vector3(-side * 0.02f, 0.02f, 0.24f)) * lower.rotation;
            if (finger != null)
            {
                hand.rotation = Quaternion.FromToRotation(finger.position - hand.position, Vector3.forward) * hand.rotation;
                var index = animator.GetBoneTransform(left ? HumanBodyBones.LeftIndexProximal : HumanBodyBones.RightIndexProximal);
                var little = animator.GetBoneTransform(left ? HumanBodyBones.LeftLittleProximal : HumanBodyBones.RightLittleProximal);
                if (index != null && little != null)
                {
                    var across = Vector3.ProjectOnPlane(index.position - little.position, Vector3.forward);
                    var roll = Vector3.SignedAngle(across, left ? Vector3.right : Vector3.left, Vector3.forward);
                    hand.rotation = Quaternion.AngleAxis(roll, Vector3.forward) * hand.rotation;
                }
            }
        }

        private static void MakeMaterials()
        {
            Directory.CreateDirectory(MaterialFolder);
            AssetDatabase.Refresh();
            plaster = Mat("WarmPlaster", new Color(.82f,.81f,.77f));
            floor = Mat("StoneFloor", new Color(.52f,.53f,.50f));
            wood = Mat("Oak", new Color(.38f,.25f,.15f));
            fabric = Mat("SofaTeal", new Color(.19f,.35f,.38f));
            cushion = Mat("CushionLinen", new Color(.63f,.59f,.50f));
            metal = Mat("DarkMetal", new Color(.16f,.19f,.19f));
            glass = Mat("FrostedGlass", new Color(.54f,.69f,.72f));
            carpet = Mat("LoungeRug", new Color(.44f,.41f,.35f));
            foliage = Mat("PlantLeaves", new Color(.13f,.32f,.15f));
            paper = Mat("NoticePaper", new Color(.93f,.90f,.81f));
            screen = Mat("InformationScreen", new Color(.055f,.105f,.12f));
            accent = Mat("CollegeAccent", new Color(.13f,.34f,.42f));
        }

        private static Material Mat(string name, Color color)
        {
            var path = MaterialFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            material.color = color;
            material.SetFloat("_Smoothness", .25f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void BuildExterior()
        {
            Box(exterior,"FoyerFloor",new Vector3(0,-.1f,-3),new Vector3(7,.2f,6),floor);
            Box(exterior,"CoveredPortico",new Vector3(0,3.65f,-2.55f),new Vector3(7,.18f,5.1f),plaster);
            foreach(var x in new[]{-3.15f,3.15f}) Cylinder(exterior,"PorticoColumn",new Vector3(x,1.8f,-4.6f),new Vector3(.3f,1.8f,.3f),plaster);
            var gate=Group("ExperimentCenterMainEntrance",exterior);
            Box(gate,"OpenDoorLeft",new Vector3(-1.75f,1.4f,-.8f),new Vector3(.1f,2.8f,1.6f),wood);
            Box(gate,"OpenDoorRight",new Vector3(1.75f,1.4f,-.8f),new Vector3(.1f,2.8f,1.6f),wood);
            Box(gate,"EntranceSignBacking",new Vector3(0,3.14f,-.21f),new Vector3(4.7f,.52f,.12f),accent);
            Label(gate,"虚拟物理实验中心",new Vector3(0,3.14f,-.29f),4.5f,.44f,64,Quaternion.identity,Color.white);
            Label(exterior,"欢迎进入 · 公共大厅 → 实验区",new Vector3(0,2.8f,-.29f),3.6f,.25f,34,Quaternion.identity,accent.color);
            Plant(exterior,new Vector3(-2.6f,0,-.8f),.65f);
            Plant(exterior,new Vector3(2.6f,0,-.8f),.65f);
        }

        private static void BuildHall()
        {
            Box(architecture,"LobbyFloor",new Vector3(0,-.1f,8),new Vector3(18,.2f,16),floor);
            foreach(var x in new[]{-5.35f,5.35f})
            {
                Box(architecture,"FrontWallSection",new Vector3(x,2.1f,0),new Vector3(7.3f,4.2f,.22f),plaster);
                Box(architecture,"RearWallSection",new Vector3(x,2.1f,16),new Vector3(7.3f,4.2f,.22f),plaster);
            }
            Box(architecture,"MainEntranceLintel",new Vector3(0,3.62f,0),new Vector3(3.4f,1.16f,.22f),plaster);
            Box(architecture,"CorridorEntranceLintel",new Vector3(0,3.62f,16),new Vector3(3.4f,1.16f,.22f),plaster);
            foreach(var x in new[]{-9f,9f})
            {
                Box(architecture,"SideWall",new Vector3(x,2.1f,8),new Vector3(.22f,4.2f,16),plaster);
                Box(architecture,"Skirting",new Vector3(x*.985f,.1f,8),new Vector3(.06f,.2f,15.8f),wood);
            }
            var ceiling=Box(architecture,"LobbyCeiling",new Vector3(0,4.3f,8),new Vector3(18,.2f,16),plaster);
            ceiling.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            Box(architecture,"CorridorWayfindingPanel",new Vector3(0,3.25f,15.82f),new Vector3(3.35f,.55f,.12f),accent);
            Label(architecture,"实验区  ↑",new Vector3(0,3.25f,15.73f),3.2f,.45f,57,Quaternion.identity,Color.white);
            Label(architecture,"交谊大厅 · 欢迎交流与休息",new Vector3(0,3.8f,15.75f),7,.45f,52,Quaternion.identity,accent.color);
            // Wall panels imply daylight without adding third-party art or obstructing the circulation route.
            foreach(var z in new[]{3.4f,11.6f})
            {
                Box(architecture,"WindowFrame",new Vector3(-8.86f,2.2f,z),new Vector3(.08f,1.9f,2.65f),wood);
                Box(architecture,"FrostedDaylightPanel",new Vector3(-8.80f,2.2f,z),new Vector3(.02f,1.64f,2.38f),glass,false);
            }
        }

        private static void BuildLounge()
        {
            Box(lounge,"SeatingRug",new Vector3(-5.1f,.008f,7.6f),new Vector3(4.6f,.015f,5.3f),carpet,false);
            Sofa(new Vector3(-6.7f,0,7.6f),90);
            Sofa(new Vector3(-3.5f,0,7.6f),-90);
            Table(lounge,"CoffeeTable",new Vector3(-5.1f,0,7.6f),new Vector2(1.25f,2.35f),.46f);
            Box(lounge,"Books",new Vector3(-5.1f,.52f,7.4f),new Vector3(.24f,.09f,.32f),paper,false);
            Cylinder(lounge,"SmallVase",new Vector3(-5.1f,.60f,8.0f),new Vector3(.12f,.13f,.12f),glass,false);
            foreach(var z in new[]{6.2f,10.4f})
            {
                Table(furniture,"DiscussionTable",new Vector3(5.5f,0,z),new Vector2(2.25f,1.1f),.74f);
                Chair(furniture,new Vector3(4.9f,0,z-1),0);
                Chair(furniture,new Vector3(6.1f,0,z-1),0);
                Chair(furniture,new Vector3(4.9f,0,z+1),180);
                Chair(furniture,new Vector3(6.1f,0,z+1),180);
            }
            Plant(lounge,new Vector3(-7.8f,0,13.9f),1);
            Plant(lounge,new Vector3(7.9f,0,14.5f),1);
            Plant(lounge,new Vector3(7.8f,0,4.0f),.75f);
        }

        private static void Sofa(Vector3 position,float yaw)
        {
            var group=Group("LoungeSofa",lounge); group.position=position; group.rotation=Quaternion.Euler(0,yaw,0);
            LocalBox(group,"Base",new Vector3(0,.24f,0),new Vector3(2.5f,.30f,.9f),wood);
            LocalBox(group,"UpholsteredBack",new Vector3(0,.72f,-.37f),new Vector3(2.55f,.75f,.24f),fabric);
            foreach(var x in new[]{-.82f,0,.82f})
            {
                LocalBox(group,"SeatCushion",new Vector3(x,.47f,.05f),new Vector3(.79f,.20f,.77f),fabric);
                LocalBox(group,"BackCushion",new Vector3(x,.76f,-.19f),new Vector3(.77f,.48f,.2f),cushion);
            }
            foreach(var x in new[]{-1.32f,1.32f}) LocalBox(group,"Armrest",new Vector3(x,.56f,0),new Vector3(.20f,.53f,.98f),fabric);
        }

        private static void Table(Transform parent,string name,Vector3 position,Vector2 size,float height)
        {
            var group=Group(name,parent); group.position=position;
            LocalBox(group,"OakTop",new Vector3(0,height,0),new Vector3(size.x,.08f,size.y),wood);
            foreach(var x in new[]{-.4f,.4f}) foreach(var z in new[]{-.38f,.38f})
                LocalBox(group,"Leg",new Vector3(size.x*x,height/2,size.y*z),new Vector3(.06f,height,.06f),metal);
        }

        private static void Chair(Transform parent,Vector3 position,float yaw)
        {
            var group=Group("LoungeChair",parent); group.position=position; group.rotation=Quaternion.Euler(0,yaw,0);
            LocalBox(group,"Seat",new Vector3(0,.45f,0),new Vector3(.5f,.09f,.48f),cushion);
            LocalBox(group,"Back",new Vector3(0,.72f,-.21f),new Vector3(.5f,.5f,.08f),wood);
            foreach(var x in new[]{-.19f,.19f}) foreach(var z in new[]{-.18f,.18f}) LocalBox(group,"Leg",new Vector3(x,.22f,z),new Vector3(.05f,.44f,.05f),metal);
        }

        private static void Plant(Transform parent,Vector3 position,float scale)
        {
            var group=Group("PottedPlant",parent); group.position=position;
            Cylinder(group,"Planter",position+Vector3.up*.25f*scale,new Vector3(.42f,.25f,.42f)*scale,wood);
            Cylinder(group,"Stem",position+Vector3.up*.7f*scale,new Vector3(.06f,.45f,.06f)*scale,wood,false);
            for(int i=0;i<7;i++)
            {
                var a=i*2.4f; var leaf=Primitive(group,PrimitiveType.Sphere,"LeafCluster",position+new Vector3(Mathf.Cos(a)*.22f,.9f+(i%3)*.14f,Mathf.Sin(a)*.22f)*scale,new Vector3(.35f,.45f,.22f)*scale,foliage,false);
                leaf.transform.rotation=Quaternion.Euler(10,i*55,25);
            }
        }

        private static void BuildIdentityAndInformation()
        {
            var desk=Group("InformationDesk",reception);
            Box(desk,"OakCounter",new Vector3(5.2f,.95f,2.25f),new Vector3(3.2f,.13f,1.05f),wood);
            Box(desk,"DeskFront",new Vector3(5.2f,.46f,2.25f),new Vector3(3.1f,.87f,.86f),plaster);
            Box(desk,"CounterDisplay",new Vector3(5.7f,1.2f,2.25f),new Vector3(.65f,.38f,.05f),screen,false);
            Label(desk,"信息服务",new Vector3(5.2f,.60f,1.80f),2.3f,.34f,48,Quaternion.identity,accent.color);
            var logo=Group("LogoPlaceholder",identity);
            Box(logo,"LogoReservePanel",new Vector3(-5.4f,2.3f,.18f),new Vector3(3.0f,1.55f,.12f),paper);
            Label(logo,"学院 Logo 预留区域",new Vector3(-5.4f,2.3f,.27f),2.8f,.9f,46,Quaternion.Euler(0,180,0),accent.color);
            var college=Group("CollegeNamePlaceholder",identity);
            Box(college,"CollegeReservePanel",new Vector3(8.83f,2.65f,12.4f),new Vector3(.12f,1.65f,3.45f),paper);
            Label(college,"北京师范大学\n人工智能学院\n学院名称 / 正式标识预留",new Vector3(8.73f,2.65f,12.4f),3.2f,1.5f,47,Quaternion.Euler(0,90,0),accent.color);
            var mascot=Group("MascotPlaceholder",identity);
            Box(mascot,"DisplayPlinth",new Vector3(-5.8f,.48f,12.85f),new Vector3(1.4f,.96f,1.25f),plaster);
            Box(mascot,"ReservedDisplayTop",new Vector3(-5.8f,1f,12.85f),new Vector3(1.5f,.08f,1.35f),wood);
            Label(mascot,"吉祥物展示位\n正式素材预留",new Vector3(-5.8f,.64f,12.17f),1.3f,.5f,30,Quaternion.identity,accent.color);
            var intro=Group("ProjectIntroductionScreen",reception);
            Box(intro,"ScreenFrame",new Vector3(8.80f,2.1f,7.7f),new Vector3(.15f,1.9f,3.1f),wood);
            Box(intro,"Screen",new Vector3(8.69f,2.1f,7.7f),new Vector3(.04f,1.72f,2.95f),screen,false);
            Label(intro,"多人具身化协同虚拟物理实验室\n\n大学虚拟实验中心 · 交流与学习\n当前：单人桌面开发测试\n经大厅后方走廊前往各实验室",new Vector3(8.65f,2.1f,7.7f),2.75f,1.5f,34,Quaternion.Euler(0,90,0),Color.white);
            var board=Group("NoticeBoard",reception);
            Box(board,"CorkFrame",new Vector3(-5.8f,2.1f,15.79f),new Vector3(3.35f,1.65f,.16f),wood);
            Box(board,"NoticePaper",new Vector3(-5.8f,2.1f,15.68f),new Vector3(3.12f,1.42f,.04f),paper,false);
            Label(board,"实验中心公告\n\n大厅为公共休息与交流区域\n请沿实验区走廊按门牌进入\n操作提示：WASD 移动 · 左键交互",new Vector3(-5.8f,2.1f,15.64f),2.9f,1.28f,33,Quaternion.identity,accent.color);
        }

        private static void BuildCorridor()
        {
            Box(corridor,"CorridorFloor",new Vector3(0,-.1f,24.5f),new Vector3(4.6f,.2f,17),floor);
            var ceiling=Box(corridor,"CorridorCeiling",new Vector3(0,3.6f,24.5f),new Vector3(4.6f,.2f,17),plaster);
            ceiling.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            foreach(var x in new[]{-2.3f,2.3f})
            {
                Box(corridor,"CorridorSideFront",new Vector3(x,1.75f,20),new Vector3(.2f,3.5f,8),plaster);
                Box(corridor,"CorridorSideRear",new Vector3(x,1.75f,30),new Vector3(.2f,3.5f,6),plaster);
                Box(corridor,"SideDoorLintel",new Vector3(x,3.1f,25.5f),new Vector3(.2f,.8f,3),plaster);
            }
            Box(corridor,"EndWallLeft",new Vector3(-1.8f,1.75f,33),new Vector3(1f,3.5f,.2f),plaster);
            Box(corridor,"EndWallRight",new Vector3(1.8f,1.75f,33),new Vector3(1f,3.5f,.2f),plaster);
            Box(corridor,"EndDoorLintel",new Vector3(0,3.1f,33),new Vector3(2.6f,.8f,.2f),plaster);
            Door("MechanicsDoor","力学实验室","探究加速度与力、质量的关系","AccelerationLab",new Vector3(-2.25f,0,25.5f),-90,new Color(.18f,.36f,.42f));
            Door("ThermalDoor","热学实验室","探究气体等温变化规律","GasLab",new Vector3(2.25f,0,25.5f),90,new Color(.52f,.34f,.18f));
            Door("ElectromagnetismDoor","电磁学实验室","探究变压器原、副线圈匝数与电压的关系","TransformerLab",new Vector3(0,0,32.92f),0,new Color(.35f,.33f,.48f));
            Label(corridor,"实验区\n力学 ←   热学 →   电磁学 ↑",new Vector3(0,3.0f,21.0f),3.4f,.6f,37,Quaternion.identity,accent.color);
        }

        private static void Door(string name,string title,string subtitle,string scene,Vector3 position,float yaw,Color color)
        {
            var root=Group(name,doors); root.position=position; root.rotation=Quaternion.Euler(0,yaw,0);
            var nav=root.gameObject.AddComponent<SceneNavigator>(); nav.TargetScene=scene;
            var interactive=root.gameObject.AddComponent<SceneDoorInteractable>(); interactive.DestinationScene=scene; interactive.DestinationName=title;
            LocalBox(root,"DoorPanel",new Vector3(0,1.28f,0),new Vector3(2.5f,2.56f,.15f),wood);
            foreach(var x in new[]{-1.34f,1.34f}) LocalBox(root,"DoorFrame",new Vector3(x,1.35f,0),new Vector3(.15f,2.7f,.24f),metal);
            LocalBox(root,"FrameTop",new Vector3(0,2.66f,0),new Vector3(2.82f,.16f,.24f),metal);
            LocalBox(root,"FrostedWindow",new Vector3(0,1.62f,-.095f),new Vector3(1.92f,1.15f,.04f),glass,false);
            var sign=Mat(name+"Sign",color);
            LocalBox(root,"NamePlate",new Vector3(0,2.97f,-.08f),new Vector3(2.9f,.45f,.10f),sign,false);
            var heading=Label(root,title,root.TransformPoint(new Vector3(0,2.97f,-.145f)),2.7f,.36f,48,root.rotation,Color.white);
            var sub=Label(root,subtitle+"\n走近 · 看向门 · 左键进入",root.TransformPoint(new Vector3(0,1.65f,-.128f)),2.0f,.85f,30,root.rotation,metal.color);
            LocalBox(root,"DoorHandle",new Vector3(.88f,1.05f,-.14f),new Vector3(.07f,.32f,.09f),metal,false);
        }

        private static void AddLighting()
        {
            var sun = new GameObject("Daylight").AddComponent<Light>();
            sun.type=LightType.Directional; sun.intensity=.9f; sun.color=new Color(1,.96f,.89f);
            sun.transform.rotation=Quaternion.Euler(45,-28,0); sun.shadows=LightShadows.Soft;
            RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(.65f,.65f,.62f);
            foreach(var x in new[]{-4.8f,4.8f}) foreach(var z in new[]{4f,11.7f}) CeilingLamp(architecture,new Vector3(x,4.1f,z),3.8f);
            foreach(var z in new[]{18.5f,24f,30.5f}) CeilingLamp(corridor,new Vector3(0,3.42f,z),2.5f);
        }

        private static void CeilingLamp(Transform parent,Vector3 position,float height)
        {
            Box(parent,"CeilingLightFixture",position,new Vector3(1.25f,.07f,.42f),paper,false);
            var lamp=new GameObject("WarmCeilingLight"); lamp.transform.SetParent(parent,false); lamp.transform.position=position-Vector3.up*.2f;
            var light=lamp.AddComponent<Light>(); light.type=LightType.Point; light.color=new Color(1,.96f,.88f); light.intensity=2.2f; light.range=8f;
        }

        private static Transform Group(string name,Transform parent=null)
        {
            var group=new GameObject(name).transform; if(parent!=null)group.SetParent(parent,false); return group;
        }
        private static GameObject Box(Transform parent,string name,Vector3 position,Vector3 scale,Material material,bool collision=true) => Primitive(parent,PrimitiveType.Cube,name,position,scale,material,collision);
        private static GameObject Cylinder(Transform parent,string name,Vector3 position,Vector3 scale,Material material,bool collision=true) => Primitive(parent,PrimitiveType.Cylinder,name,position,scale,material,collision);
        private static GameObject Primitive(Transform parent,PrimitiveType type,string name,Vector3 position,Vector3 scale,Material material,bool collision)
        {
            var part=GameObject.CreatePrimitive(type);part.name=name;part.transform.SetParent(parent,true);part.transform.position=position;part.transform.localScale=scale;part.GetComponent<Renderer>().sharedMaterial=material;
            if(!collision)Object.DestroyImmediate(part.GetComponent<Collider>());return part;
        }
        private static GameObject LocalBox(Transform parent,string name,Vector3 position,Vector3 scale,Material material,bool collision=true)
        {
            var part=GameObject.CreatePrimitive(PrimitiveType.Cube);part.name=name;part.transform.SetParent(parent,false);part.transform.localPosition=position;part.transform.localScale=scale;part.GetComponent<Renderer>().sharedMaterial=material;
            if(!collision)Object.DestroyImmediate(part.GetComponent<Collider>());return part;
        }

        private static GameObject Label(Transform parent,string value,Vector3 position,float width,float height,int fontSize,Quaternion rotation,Color color)
        {
            var root=new GameObject("Sign | "+value.Split('\n')[0],typeof(RectTransform),typeof(Canvas));root.transform.SetParent(parent,true);root.transform.position=position;root.transform.rotation=rotation;root.transform.localScale=Vector3.one*.004f;
            var rect=root.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(width*250,height*250);
            root.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            var child=new GameObject("Text",typeof(RectTransform),typeof(TextMeshProUGUI));child.transform.SetParent(root.transform,false);
            var r=child.GetComponent<RectTransform>();r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=Vector2.zero;r.offsetMax=Vector2.zero;
            var text=child.GetComponent<TextMeshProUGUI>();text.font=font;text.text=value;text.fontSize=fontSize;text.color=color;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;
            return root;
        }

        public static void Validate()
        {
            if (SceneManager.GetActiveScene().path != ScenePath) EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            var all=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            foreach(var name in new[]{"LogoPlaceholder","MascotPlaceholder","CollegeNamePlaceholder","SocialLounge","ExperimentCorridor"})
                if(!all.Any(t=>t.name==name))throw new InvalidOperationException("Missing: "+name);
            if(all.Any(t=>t.name=="Preview Exhibits"||t.name=="Body Visual (no head)"))throw new InvalidOperationException("Old exhibits/body remain.");
            var entrances=Object.FindObjectsByType<SceneDoorInteractable>(FindObjectsSortMode.None);
            if(entrances.Length!=3)throw new InvalidOperationException("Expected exactly three experiment doors.");
            foreach(var door in entrances)
            {
                var navigator=door.GetComponent<SceneNavigator>();
                if(navigator==null||navigator.TargetScene!=door.DestinationScene)throw new InvalidOperationException("Door navigation mismatch.");
                if(!EditorBuildSettings.scenes.Any(s=>s.enabled&&Path.GetFileNameWithoutExtension(s.path)==door.DestinationScene))throw new InvalidOperationException("Scene not in build settings.");
                Debug.Log("VALIDATED_DOOR "+door.name+" -> "+navigator.TargetScene);
            }
            var player=GameObject.Find("DesktopPlayer");var animator=player.GetComponentInChildren<Animator>();
            if (AssetDatabase.GetDependencies(AvatarPrefab, true).Any(p => p.StartsWith("Assets/Samples/")))
                throw new InvalidOperationException("The migrated Avatar still depends on a Sample asset.");
            if(animator==null||!animator.isHuman||!animator.avatar.isValid)throw new InvalidOperationException("Invalid player rig.");
            if(player.GetComponentsInChildren<SkinnedMeshRenderer>().Length!=6)throw new InvalidOperationException("Expected full six-mesh avatar.");
            if(player.GetComponentsInChildren<Collider>().Length!=1)throw new InvalidOperationException("Avatar must not add self-colliders.");
            var camera=player.GetComponentInChildren<Camera>();
            var eyes=(animator.GetBoneTransform(HumanBodyBones.LeftEye).position+animator.GetBoneTransform(HumanBodyBones.RightEye).position)*.5f;
            if(Vector3.Distance(camera.transform.position,eyes)>.005f)throw new InvalidOperationException("Camera is not at the eyes.");
            Physics.SyncTransforms();
            foreach (var door in entrances)
            {
                var origin = door.name == "ElectromagnetismDoor" ? new Vector3(0, 1.65f, 30.5f) : new Vector3(0, 1.65f, 25.5f);
                var target = door.transform.position + Vector3.up * 1.65f;
                if (!Physics.Raycast(origin, (target-origin).normalized, out var hit, 3.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide)
                    || hit.collider.GetComponentInParent<SceneDoorInteractable>() != door)
                    throw new InvalidOperationException("Door ray is blocked: " + door.name);
            }
            // Check the human-sized main route at eye and chest heights, including both openings.
            foreach(var height in new[]{.85f,1.6f})
                if(Physics.SphereCast(new Vector3(0,height,-4.4f),.30f,Vector3.forward,out var obstruction,34.8f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
                    throw new InvalidOperationException("Blocked main circulation: "+obstruction.collider.name);
            Debug.Log("VALIDATED_ROUTE exterior -> hall -> corridor; valid Humanoid, camera at eyes, no primitive body or exhibit models.");
        }

        public static void Capture()
        {
            EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            foreach(var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))text.ForceMeshUpdate(true,true);
            Canvas.ForceUpdateCanvases();
            var player=GameObject.Find("DesktopPlayer");var camera=player.GetComponentInChildren<Camera>();
            Directory.CreateDirectory("Logs/LobbySocialPreview");
            Shot(camera,new Vector3(0,1.65f,-4.4f),Quaternion.identity,"01-Exterior");
            Shot(camera,new Vector3(0,1.65f,2),Quaternion.Euler(0,-25,0),"02-SocialHall");
            Shot(camera,new Vector3(7,3.1f,1.8f),Quaternion.LookRotation(new Vector3(-4,1.3f,9)-new Vector3(7,3.1f,1.8f)),"03-LoungeOverview");
            Shot(camera,new Vector3(0,1.65f,22.1f),Quaternion.identity,"04-Corridor");
            player.transform.position=new Vector3(0,0,4);
            var rig=player.GetComponentInChildren<Animator>();var eye=(rig.GetBoneTransform(HumanBodyBones.LeftEye).position+rig.GetBoneTransform(HumanBodyBones.RightEye).position)*.5f;
            Shot(camera,eye,Quaternion.identity,"05-FirstPersonHands");
            Shot(camera,eye+Vector3.forward*.15f,Quaternion.Euler(80,0,0),"06-FirstPersonBody");
            // Preview transforms are temporary. Restore the saved scene rather than leaving an editor-only camera pose.
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("LOBBY_PREVIEWS_CAPTURED");
        }

        private static void Shot(Camera camera,Vector3 position,Quaternion rotation,string name)
        {
            camera.transform.SetPositionAndRotation(position,rotation);
            var rt=new RenderTexture(1280,720,24);camera.targetTexture=rt;camera.Render();
            var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
            File.WriteAllBytes("Logs/LobbySocialPreview/"+name+".png",image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=old;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);
        }
    }
}
