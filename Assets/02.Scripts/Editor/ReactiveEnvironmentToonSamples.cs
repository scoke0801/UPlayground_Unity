using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UPlayGround.Components;
using UPlayGround.Core.ReactiveEnvironment;
using UPlayGround.Data.Item;
using UPlayGround.Data.ReactiveEnvironment;
using UPlayGround.Gameplay.World.ReactiveEnvironment;

namespace UPlayGround.EditorTools
{
    /// <summary>실제 반응형 재질과 파괴 레시피가 연결된 반복 검증용 샘플을 저작한다.</summary>
    public static class ReactiveEnvironmentToonSamples
    {
        private const string DataFolder = "Assets/10.Datas/Environment";
        private const string PrefabFolder = "Assets/03.Prefabs/Environment";
        private const string MaterialFolder = "Assets/11.Material";
        private static Material _bark, _leaves, _interior, _stone;

        [UPlaygroundTool("Environment/CreateToonSamples")]
        public static void CreateSamples()
        {
            EnsureFolder(DataFolder); EnsureFolder(PrefabFolder);
            Shader shader = Shader.Find("UPlayGround/Environment/Reactive Toon");
            if (shader == null || !shader.isSupported) throw new InvalidOperationException("반응형 툰 셰이더가 준비되지 않았습니다.");
            _bark = Material("Bark",new Color(0.5f,0.28f,0.12f),0);
            _leaves = Material("Foliage",new Color(0.3f,0.52f,0.14f),1);
            _interior = Material("Interior",new Color(0.76f,0.59f,0.34f),2);
            _stone = Material("Stone",new Color(0.5f,0.53f,0.57f),0);
            UpgradeCrate();
            CreateTree(); CreateRock(); CreateRoof(); CreateGrass();
            UpdateTreeHeatSamples();
            RegisterRoofCameraRule();
            AssetDatabase.SaveAssets();
        }

        private static void UpdateTreeHeatSamples()
        {
            string path=PrefabFolder+"/ReactiveEnvironmentTreeSample.prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var standing=root.transform.Find("Intact/HeatCenter");
                if(standing==null) {standing=Child("HeatCenter",root.transform.Find("Intact")).transform; standing.localPosition=Vector3.up*1.5f;}
                var fallen=root.transform.Find("Remnant/FallPivot/HeatCenter");
                if(fallen==null) {fallen=Child("HeatCenter",root.transform.Find("Remnant/FallPivot")).transform; fallen.localPosition=Vector3.up*1.5f;}
                SetArray(root.GetComponent<ReactiveEnvironmentObject>(),"_heatSamples",new[] {standing,fallen});
                var prefab=PrefabUtility.SaveAsPrefabAsset(root,path);
                Set(prefab.GetComponent<SceneEntityId>(),"_guid",""); Set(prefab.GetComponent<SceneEntityId>(),"_ownerKey","");
            }
            finally {PrefabUtility.UnloadPrefabContents(root);}
        }

        private static void UpgradeCrate()
        {
            var definition = AssetDatabase.LoadAssetAtPath<ReactiveEnvironmentDefinitionSO>(ReactiveEnvironmentSampleAuthoring.DefinitionPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/ReactiveEnvironmentSample.mat");
            if (definition == null || material == null) throw new InvalidOperationException("기존 상자 단면 샘플이 없습니다.");
            Color color = material.GetColor("_BaseColor");
            material.shader = Shader.Find("UPlayGround/Environment/Reactive Toon");
            material.SetColor("_BaseColor",color);
            definition.surfaceBackend = EnvironmentSurfaceBackend.ReactiveToon;
            definition.spreadRadius = 3; definition.spreadHeatPerSecond = 2;
            definition.fireBlockerLayers = LayerMask.GetMask("Default","Ground","InteractableObject");
            EditorUtility.SetDirty(definition); EditorUtility.SetDirty(material);
        }

        private static void CreateTree()
        {
            Create("Tree", sample =>
            {
                sample.definition.destruction = Recipe("Tree",EnvironmentDestructionMode.DirectionalFall,false,true);
                sample.definition.impactDrops.Add(Drop("Assets/10.Datas/Item/Firewood.asset"));
                var lod0 = Child("LOD0",sample.intact.transform);
                var lod1 = Child("LOD1",sample.intact.transform);
                Part("Trunk",PrimitiveType.Cylinder,lod0.transform,new Vector3(0,1.5f,0),new Vector3(0.45f,1.5f,0.45f),_bark);
                Part("Crown",PrimitiveType.Sphere,lod0.transform,new Vector3(0,3.4f,0),new Vector3(2,2,2),_leaves,false);
                Part("CrownSide",PrimitiveType.Sphere,lod0.transform,new Vector3(0.7f,3,0.1f),new Vector3(1.5f,1.5f,1.5f),_leaves,false);
                Part("Branch",PrimitiveType.Cylinder,lod0.transform,new Vector3(0.5f,2.3f,0),new Vector3(0.15f,0.7f,0.15f),_bark,false).transform.localRotation = Quaternion.Euler(0,0,-45);
                Part("Trunk",PrimitiveType.Cylinder,lod1.transform,new Vector3(0,1.5f,0),new Vector3(0.45f,1.5f,0.45f),_bark,false);
                Part("Crown",PrimitiveType.Sphere,lod1.transform,new Vector3(0,3.3f,0),new Vector3(2.2f,2.2f,2.2f),_leaves,false);
                var group = sample.intact.AddComponent<LODGroup>();
                group.fadeMode = LODFadeMode.CrossFade;
                group.SetLODs(new[] { new LOD(0.35f,lod0.GetComponentsInChildren<Renderer>()) { fadeTransitionWidth = 0.1f },
                    new LOD(0.02f,lod1.GetComponentsInChildren<Renderer>()) }); group.RecalculateBounds();
                Part("Stump",PrimitiveType.Cylinder,sample.remnant.transform,new Vector3(0,0.12f,0),new Vector3(0.5f,0.12f,0.5f),_bark);
                Part("CutCap",PrimitiveType.Cylinder,sample.remnant.transform,new Vector3(0,0.245f,0),new Vector3(0.44f,0.01f,0.44f),_interior,false);
                var pivot = Child("FallPivot",sample.remnant.transform); pivot.transform.localPosition = Vector3.up * 0.25f;
                Part("Log",PrimitiveType.Cylinder,pivot.transform,new Vector3(0,1.4f,0),new Vector3(0.43f,1.4f,0.43f),_bark);
                Part("Crown",PrimitiveType.Sphere,pivot.transform,new Vector3(0,3,0),new Vector3(2,2,2),_leaves,false);
                var fallback = Child("ShortCollapse",sample.remnant.transform);
                for (int i=0;i<3;i++) Part("SplitLog",PrimitiveType.Cylinder,fallback.transform,new Vector3((i-1)*0.3f,0.3f,0),new Vector3(0.25f,0.4f,0.25f),_bark);
                var destruction = sample.root.AddComponent<ReactiveEnvironmentDestruction>();
                Set(destruction,"_fallPivot",pivot.transform); Set(destruction,"_blockedFallback",fallback);
                SetArray(destruction,"_standingObstacles",new[] { Obstacle(sample.intact,new Vector3(0,1.5f,0),new Vector3(0.6f,3,0.6f)) });
                SetArray(destruction,"_remnantObstacles",new[] { Obstacle(pivot,new Vector3(0,1.4f,0),new Vector3(0.6f,2.8f,0.6f)) });
                SamplePoint(sample,sample.intact.transform,new Vector3(0,3.7f,0));
                SamplePoint(sample,pivot.transform,new Vector3(0,1.5f,0));
            });
        }

        private static void CreateRock()
        {
            Create("Rock", sample =>
            {
                sample.definition.reaction.canIgnite = false; sample.definition.reaction.initialFuel = 0;
                sample.definition.reaction.slashMultiplier = 0; sample.definition.reaction.bluntMultiplier = 2;
                sample.definition.reaction.scorchPerHeat = 0.1f;
                sample.definition.spreadRadius = sample.definition.spreadHeatPerSecond = 0;
                sample.definition.destruction = Recipe("Rock",EnvironmentDestructionMode.Swap,false,true);
                sample.definition.impactDrops.Add(Drop("Assets/10.Datas/Item/Item_Stone.asset"));
                Part("Rock",PrimitiveType.Sphere,sample.intact.transform,Vector3.up*0.7f,new Vector3(1.7f,1.5f,1.4f),_stone);
                for (int i=0;i<4;i++) Part("FracturePiece",PrimitiveType.Sphere,sample.remnant.transform,
                    new Vector3(Mathf.Cos(i*1.57f)*0.65f,0.25f,Mathf.Sin(i*1.57f)*0.65f),Vector3.one*0.5f,_stone);
                var destruction = sample.root.AddComponent<ReactiveEnvironmentDestruction>();
                SetArray(destruction,"_standingObstacles",new[] { Obstacle(sample.intact,Vector3.up*0.7f,new Vector3(1.7f,1.5f,1.4f)) });
                SamplePoint(sample,sample.intact.transform,Vector3.up*1.5f);
                SamplePoint(sample,sample.remnant.transform,Vector3.up*0.6f);
            });
        }

        private static void CreateRoof()
        {
            Create("Roof", sample =>
            {
                sample.definition.destruction = Recipe("Roof",EnvironmentDestructionMode.Swap,true,false);
                var roof = Part("RoofPlate",PrimitiveType.Cube,sample.intact.transform,Vector3.up*3,new Vector3(4,0.25f,3),_bark);
                // 카메라/강수 차폐의 기존 Ground 레이어 계약을 따른다.
                roof.layer = LayerMask.NameToLayer("Ground");
                var proxy = Child("AttackProxy",sample.intact.transform).AddComponent<BoxCollider>();
                proxy.gameObject.layer = LayerMask.NameToLayer("InteractableObject");
                proxy.isTrigger = true; proxy.center = Vector3.up*3; proxy.size = new Vector3(4,0.3f,3);
                var volume = Child("ShelterVolume",sample.intact.transform).AddComponent<BoxCollider>();
                volume.isTrigger = true; volume.center = Vector3.up*1.5f; volume.size = new Vector3(3.8f,2.8f,2.8f);
                var destruction = sample.root.AddComponent<ReactiveEnvironmentDestruction>();
                SetArray(destruction,"_shelterColliders",new Collider[] { roof.GetComponent<Collider>(),volume });
                for (int i=0;i<3;i++) Part("RoofDebris",PrimitiveType.Cube,sample.remnant.transform,
                    new Vector3(i-1,0.15f,0),new Vector3(0.8f,0.2f,0.7f),_bark);
                SamplePoint(sample,sample.intact.transform,new Vector3(0,3.15f,0));
                SamplePoint(sample,sample.remnant.transform,Vector3.up*0.4f);
            });
        }

        private static void CreateGrass()
        {
            Create("Grass", sample =>
            {
                sample.definition.reaction.canBreak = false; sample.definition.reaction.initialFuel = 6;
                sample.definition.reaction.burnRate = 1.5f; sample.definition.reaction.damagePerFuel = 0;
                Mesh leaf = LeafMesh();
                for (int i=0;i<7;i++)
                {
                    var go=Child("GrassCard",sample.intact.transform);
                    go.transform.localPosition = new Vector3(Mathf.Cos(i)*0.6f,0,Mathf.Sin(i)*0.6f);
                    go.transform.localRotation = Quaternion.Euler(0,i*51,0);
                    go.AddComponent<MeshFilter>().sharedMesh=leaf; go.AddComponent<MeshRenderer>().sharedMaterial=_leaves;
                }
                var hitbox=sample.intact.AddComponent<BoxCollider>(); hitbox.isTrigger=true;
                hitbox.center=Vector3.up*0.5f; hitbox.size=new Vector3(2,1,2);
                sample.intact.layer=LayerMask.NameToLayer("InteractableObject");
                SamplePoint(sample,sample.intact.transform,Vector3.up*0.8f);
            });
        }

        private sealed class Sample
        {
            public GameObject root,intact,remnant;
            public ReactiveEnvironmentDefinitionSO definition;
            public readonly List<Transform> rain = new();
        }
        private static void Create(string type, Action<Sample> build)
        {
            string name="ReactiveEnvironment"+type+"Sample";
            if (AssetDatabase.LoadMainAssetAtPath(PrefabFolder+"/"+name+".prefab") != null) return;
            string definitionPath=DataFolder+"/Reactive"+type+"Sample.asset";
            if (AssetDatabase.LoadMainAssetAtPath(definitionPath) != null)
                throw new InvalidOperationException("기존 정의를 덮어쓰지 않습니다: "+definitionPath);
            var scene=EditorSceneManager.NewPreviewScene();
            var sample=new Sample {root=new GameObject(name)}; sample.root.SetActive(false);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(sample.root,scene);
            try
            {
                sample.intact=Child("Intact",sample.root.transform); sample.remnant=Child("Remnant",sample.root.transform);
                sample.definition=ScriptableObject.CreateInstance<ReactiveEnvironmentDefinitionSO>();
                sample.definition.definitionId="environment.sample."+type.ToLowerInvariant();
                sample.definition.surfaceBackend=EnvironmentSurfaceBackend.ReactiveToon;
                sample.definition.spreadRadius=3; sample.definition.spreadHeatPerSecond=2;
                sample.definition.fireBlockerLayers=LayerMask.GetMask("Default","Ground","InteractableObject");
                build(sample);
                AssetDatabase.CreateAsset(sample.definition,definitionPath);
                var component=sample.root.AddComponent<ReactiveEnvironmentObject>();
                Set(component,"_definition",sample.definition); Set(component,"_intact",sample.intact); Set(component,"_remnant",sample.remnant);
                SetArray(component,"_rainSamples",sample.rain.ToArray());
                SetArray(component,"_surfaces",sample.root.GetComponentsInChildren<Renderer>(true));
                sample.remnant.SetActive(false); sample.root.SetActive(true);
                var prefab=PrefabUtility.SaveAsPrefabAsset(sample.root,PrefabFolder+"/"+name+".prefab");
                Set(prefab.GetComponent<SceneEntityId>(),"_guid",""); Set(prefab.GetComponent<SceneEntityId>(),"_ownerKey","");
                EditorUtility.SetDirty(prefab);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        private static Material Material(string name, Color color, int role)
        {
            string path=MaterialFolder+"/ReactiveEnvironment_"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path); if (material!=null) return material;
            material=new Material(Shader.Find("UPlayGround/Environment/Reactive Toon"));
            material.SetColor("_BaseColor",color); material.SetFloat("_SurfaceRole",role);
            if (role==1) {material.SetFloat("_Cull",0); material.SetFloat("_AlphaClip",1); material.SetFloat("_WindAmplitude",0.12f); material.SetFloat("_AllowFuelCoverage",1);}
            AssetDatabase.CreateAsset(material,path); return material;
        }
        private static EnvironmentDestructionRecipeSO Recipe(string name, EnvironmentDestructionMode mode, bool shelter, bool navigation)
        {
            string path=DataFolder+"/Destruction_"+name+".asset";
            var recipe=AssetDatabase.LoadAssetAtPath<EnvironmentDestructionRecipeSO>(path); if (recipe!=null) return recipe;
            recipe=ScriptableObject.CreateInstance<EnvironmentDestructionRecipeSO>(); recipe.mode=mode;
            recipe.changesShelter=shelter; recipe.changesNavigation=navigation;
            recipe.trunkLength=3.2f; recipe.trunkRadius=0.24f;
            recipe.fallBlockerLayers=LayerMask.GetMask("Default","Ground","Player","Enemy","Npc","InteractableObject");
            AssetDatabase.CreateAsset(recipe,path); return recipe;
        }
        private static ItemDropList Drop(string path)
        {
            var item=AssetDatabase.LoadAssetAtPath<ItemSO>(path);
            if (item==null) throw new InvalidOperationException("기존 자원 데이터가 없습니다: "+path);
            return new ItemDropList {itemData=item,rate=100,minimumDropCount=1,maximumDropCount=1};
        }
        private static GameObject Child(string name, Transform parent) {var go=new GameObject(name); go.transform.SetParent(parent,false); return go;}
        private static GameObject Part(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material, bool collider=true)
        {
            var go=GameObject.CreatePrimitive(type); go.name=name; go.transform.SetParent(parent,false);
            go.transform.localPosition=position; go.transform.localScale=scale;
            go.layer=LayerMask.NameToLayer("InteractableObject"); go.GetComponent<Renderer>().sharedMaterial=material;
            if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
        }
        private static void SamplePoint(Sample sample, Transform parent, Vector3 position)
        {var point=Child("RainSurfaceSample",parent); point.transform.localPosition=position; sample.rain.Add(point.transform);}
        private static NavMeshObstacle Obstacle(GameObject go,Vector3 center,Vector3 size)
        {var obstacle=go.AddComponent<NavMeshObstacle>(); obstacle.shape=NavMeshObstacleShape.Box; obstacle.center=center; obstacle.size=size; obstacle.carving=true; return obstacle;}
        private static Mesh LeafMesh()
        {
            const string folder="Assets/05.Models/ReactiveEnvironmentSample"; EnsureFolder(folder);
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(folder+"/Leaf.asset"); if (mesh!=null) return mesh;
            mesh=new Mesh {name="Reactive leaf sample"};
            mesh.vertices=new[] {Vector3.zero,new Vector3(-0.3f,0.6f,0),Vector3.up,new Vector3(0.3f,0.6f,0)};
            mesh.uv=new[] {new Vector2(0.5f,0),new Vector2(0,0.6f),new Vector2(0.5f,1),new Vector2(1,0.6f)};
            mesh.triangles=new[] {0,1,2,0,2,3}; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,folder+"/Leaf.asset"); return mesh;
        }
        private static void Set(UnityEngine.Object target,string name,UnityEngine.Object value)
        {var so=new SerializedObject(target); so.FindProperty(name).objectReferenceValue=value; so.ApplyModifiedPropertiesWithoutUndo();}
        private static void Set(UnityEngine.Object target,string name,string value)
        {var so=new SerializedObject(target); so.FindProperty(name).stringValue=value; so.ApplyModifiedPropertiesWithoutUndo();}
        private static void SetArray<T>(UnityEngine.Object target,string name,T[] values) where T:UnityEngine.Object
        {var so=new SerializedObject(target); var property=so.FindProperty(name); property.arraySize=values.Length; for(int i=0;i<values.Length;i++) property.GetArrayElementAtIndex(i).objectReferenceValue=values[i]; so.ApplyModifiedPropertiesWithoutUndo();}
        private static void EnsureFolder(string path)
        {if (AssetDatabase.IsValidFolder(path)) return; int split=path.LastIndexOf('/'); EnsureFolder(path.Substring(0,split)); AssetDatabase.CreateFolder(path.Substring(0,split),path.Substring(split+1));}
        private static void RegisterRoofCameraRule()
        {
            var rules=AssetDatabase.LoadAssetAtPath<UPlayGround.Data.CameraOccluderNameRulesSO>("Assets/10.Datas/Camera/CameraOccluderNameRules.asset");
            if(rules==null) throw new InvalidOperationException("카메라 지붕 규칙이 없습니다.");
            if(rules.TryMatch("ReactiveEnvironmentRoofSample",out _)) return;
            var list=new List<UPlayGround.Data.CameraOccluderNameRulesSO.Rule>(rules.Rules);
            list.Add(new UPlayGround.Data.CameraOccluderNameRulesSO.Rule {namePattern="ReactiveEnvironmentRoofSample",coversInterior=true,
                note="반응형 지붕 샘플의 실제 판 콜라이더. 파괴 시 원형과 함께 비활성화.",mode=UPlayGround.CameraSystem.OccluderMode.Auto});
            rules.SetRules(list); EditorUtility.SetDirty(rules);
        }
    }
}
