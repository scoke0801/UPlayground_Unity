using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.Item;
using UPlayGround.Data.ReactiveEnvironment;
using UPlayGround.Gameplay.World.ReactiveEnvironment;

namespace UPlayGround.EditorTools
{
    /// <summary>정규 씬을 수정하지 않고 첫 단면의 명시적 회색 상자 샘플을 만든다.</summary>
    public static class ReactiveEnvironmentSampleAuthoring
    {
        public const string PrefabPath = "Assets/03.Prefabs/Environment/ReactiveEnvironmentCrateSample.prefab";
        public const string DefinitionPath = "Assets/10.Datas/Environment/ReactiveCrateSample.asset";
        private const string MaterialPath = "Assets/11.Material/ReactiveEnvironmentSample.mat";

        [UPlaygroundTool("Environment/CreateReactionSample")]
        public static void CreateSample()
        {
            if (AssetDatabase.LoadMainAssetAtPath(PrefabPath) != null
                || AssetDatabase.LoadMainAssetAtPath(DefinitionPath) != null
                || AssetDatabase.LoadMainAssetAtPath(MaterialPath) != null)
                throw new InvalidOperationException("기존 환경 샘플을 덮어쓰지 않습니다.");
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var wood = AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/10.Datas/Item/Firewood.asset");
            if (shader == null || wood == null) throw new InvalidOperationException("샘플 셰이더 또는 기존 장작 데이터가 없습니다.");
            EnsureFolder("Assets/03.Prefabs/Environment");
            EnsureFolder("Assets/10.Datas/Environment");
            var material = new Material(shader) { color = new Color(0.55f, 0.34f, 0.16f) };
            AssetDatabase.CreateAsset(material, MaterialPath);
            var definition = ScriptableObject.CreateInstance<ReactiveEnvironmentDefinitionSO>();
            definition.definitionId = "environment.sample.crate";
            definition.impactDrops.Add(new ItemDropList { itemData = wood, rate = 100, minimumDropCount = 1, maximumDropCount = 1 });
            // 숯의 소비처가 정의되지 않았으므로 샘플의 화재 파괴는 자원을 소진한다.
            AssetDatabase.CreateAsset(definition, DefinitionPath);
            var scene = EditorSceneManager.NewPreviewScene();
            var root = new GameObject("ReactiveEnvironmentCrateSample");
            root.SetActive(false);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            try
            {
                var identity = root.AddComponent<SceneEntityId>();
                var intact = GameObject.CreatePrimitive(PrimitiveType.Cube);
                intact.name = "Intact"; intact.transform.SetParent(root.transform, false);
                intact.transform.localPosition = Vector3.up * 0.5f;
                intact.GetComponent<Renderer>().sharedMaterial = material;
                intact.layer = LayerMask.NameToLayer("InteractableObject");
                var remnant = new GameObject("Remnant"); remnant.transform.SetParent(root.transform, false);
                for (int i = 0; i < 3; i++)
                {
                    var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    board.name = "RemnantBoard"; board.transform.SetParent(remnant.transform, false);
                    board.transform.localScale = new Vector3(0.9f, 0.08f, 0.22f);
                    board.transform.localPosition = new Vector3(0, 0.04f + i * 0.09f, (i - 1) * 0.18f);
                    board.transform.localRotation = Quaternion.Euler(0, i * 25, 0);
                    board.GetComponent<Renderer>().sharedMaterial = material;
                    board.layer = LayerMask.NameToLayer("InteractableObject");
                }
                remnant.SetActive(false);
                var sample = new GameObject("RainSurfaceSample"); sample.transform.SetParent(root.transform, false);
                sample.transform.localPosition = Vector3.up * 1.01f;
                var component = root.AddComponent<ReactiveEnvironmentObject>();
                var serialized = new SerializedObject(component);
                serialized.FindProperty("_definition").objectReferenceValue = definition;
                serialized.FindProperty("_intact").objectReferenceValue = intact;
                serialized.FindProperty("_remnant").objectReferenceValue = remnant;
                serialized.FindProperty("_rainSamples").arraySize = 1;
                serialized.FindProperty("_rainSamples").GetArrayElementAtIndex(0).objectReferenceValue = sample.transform;
                var renderers = root.GetComponentsInChildren<Renderer>(true);
                serialized.FindProperty("_surfaces").arraySize = renderers.Length;
                for (int i = 0; i < renderers.Length; i++)
                    serialized.FindProperty("_surfaces").GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var id = new SerializedObject(identity);
                id.FindProperty("_guid").stringValue = "";
                id.FindProperty("_ownerKey").stringValue = "";
                id.ApplyModifiedPropertiesWithoutUndo();
                root.SetActive(true);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                // 임시 씬에서 OnValidate가 부여한 ID를 프리팹 원본에 남기지 않는다.
                var prefabId = new SerializedObject(prefab.GetComponent<SceneEntityId>());
                prefabId.FindProperty("_guid").stringValue = "";
                prefabId.FindProperty("_ownerKey").stringValue = "";
                prefabId.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int split = path.LastIndexOf('/');
            string parent = path.Substring(0, split);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(split + 1));
        }
    }
}
