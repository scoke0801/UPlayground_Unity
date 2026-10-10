using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.Components;
using UPlayGround.EditorTools;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class ScenarioMapAuthoring
    {
        private const string BaselineDirectory = "output/ScenarioGameplay/P00";

        [Serializable] private sealed class BaselineScene
        {
            public string scene;
            public List<BaselineObject> objects = new();
            public List<string> errors = new();
        }

        [Serializable] private sealed class BaselineObject
        {
            public string path;
            public string globalObjectId;
            public string sceneEntityId;
            public string prefab;
            public Vector3 position;
            public List<string> components = new();
            public List<string> bindings = new();
        }

        /// <summary>프리팹을 펼친 실제 시나리오 배치와 참조를 저장 없이 기록한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/시나리오 P00 기준선 조사")]
        public static void InspectScenarioBaseline()
        {
            RequireEditMode();
            for (int index = 0; index < SceneManager.sceneCount; index++)
                if (SceneManager.GetSceneAt(index).isDirty)
                    throw new InvalidOperationException("미저장 씬이 있어 P00 조사를 중단합니다.");

            Directory.CreateDirectory(BaselineDirectory);
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                InspectBaselineScene(ReadLayout().scenePath, "ScenarioScene.runtime.json");
                InspectBaselineScene(SourceScenePath, "OriginalScene.runtime.json");
            }
            finally
            {
                if (previous.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(previous);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static void InspectBaselineScene(string path, string reportName)
        {
            Scene scene = EditorSceneManager.OpenScene(path);
            var report = new BaselineScene { scene = path };
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                    InspectBaselineObject(node, report);
            File.WriteAllText(Path.Combine(BaselineDirectory, reportName), JsonUtility.ToJson(report, true));
            Debug.Log($"[ScenarioBaseline] {path}: {report.objects.Count}개 배치, 오류 {report.errors.Count}개. 씬 저장 안 함.");
        }

        private static void InspectBaselineObject(Transform node, BaselineScene report)
        {
            MonoBehaviour[] behaviours = node.GetComponents<MonoBehaviour>();
            if (behaviours.Length == 0 && node.parent != null) return;
            var entry = new BaselineObject
            {
                path = GetTransformPath(node),
                globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(node.gameObject).ToString(),
                sceneEntityId = node.GetComponent<SceneEntityId>()?.Guid,
                prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(node.gameObject),
                position = node.position
            };
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null)
                {
                    report.errors.Add(entry.path + ": Missing Script");
                    continue;
                }
                entry.components.Add(behaviour.GetType().FullName);
                if (UnityEditor.SerializationUtility.HasManagedReferencesWithMissingTypes(behaviour))
                    report.errors.Add(entry.path + ": managed reference 타입 누락");
                var serialized = new SerializedObject(behaviour);
                SerializedProperty property = serialized.GetIterator();
                while (property.Next(true))
                {
                    string value = null;
                    if (property.propertyType == SerializedPropertyType.String)
                        value = property.stringValue;
                    else if (property.propertyType == SerializedPropertyType.Vector3)
                        value = property.vector3Value.ToString("R");
                    else if (property.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        UnityEngine.Object target = property.objectReferenceValue;
                        if (target != null)
                            value = target is Component component ? GetTransformPath(component.transform)
                                : target is GameObject gameObject ? GetTransformPath(gameObject.transform)
                                : AssetDatabase.GetAssetPath(target);
                        else if (property.objectReferenceInstanceIDValue != 0)
                            report.errors.Add(entry.path + ": 참조 누락 " + property.propertyPath);
                    }
                    if (!string.IsNullOrEmpty(value))
                        entry.bindings.Add(behaviour.GetType().Name + "." + property.propertyPath + "=" + value);
                }
            }
            report.objects.Add(entry);
        }
    }
}
