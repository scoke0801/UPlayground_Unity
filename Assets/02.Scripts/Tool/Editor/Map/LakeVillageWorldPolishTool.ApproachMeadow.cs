using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UPlayGround.EditorTools;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class LakeVillageWorldPolishTool
    {
        private const string MeadowSettingsPath = "Assets/10.Datas/World/OpenWorld/LakeVillageApproachMeadow.json";
        private const string MeadowParentPath = "생활과 주민/LakeAbundanceArt";
        private const string MeadowRootName = "ApproachMeadow";

        [Serializable]
        private sealed class MeadowSettings { public MeadowEntry[] entries; }

        [Serializable]
        private sealed class MeadowEntry
        {
            public string name;
            public string prefab;
            public float[] position;
            public float[] euler;
            public float[] scale;
            public bool disableColliders;
        }

        /// <summary>호수 진입로 가장자리 식생 군집을 재적용하며 TerrainData와 씬 저장은 건드리지 않는다.</summary>
        [UPlaygroundTool("OpenWorld/호수 진입로 식생 군집 적용")]
        public static void ApplyApproachMeadowOnly()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var parent = GameObject.Find(MeadowParentPath);
            var terrain = Terrain.activeTerrain;
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath || parent == null || terrain == null)
                throw new InvalidOperationException("호수 마을 Edit Mode와 Terrain이 필요합니다.");
            var settings = JsonUtility.FromJson<MeadowSettings>(File.ReadAllText(MeadowSettingsPath));
            if (settings?.entries == null)
                throw new InvalidOperationException("식생 군집 설정이 없습니다.");
            var prefabs = new GameObject[settings.entries.Length];
            var names = new HashSet<string>();
            for (int i = 0; i < settings.entries.Length; i++)
            {
                var entry = settings.entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.name) || entry.name.Contains("/") || !names.Add(entry.name))
                    throw new InvalidOperationException("식생 이름이 비어 있거나 중복입니다.");
                ReadPropVector(entry.position);
                ReadPropVector(entry.euler);
                var scale = ReadPropVector(entry.scale);
                if (scale.x <= 0f || scale.y <= 0f || scale.z <= 0f)
                    throw new InvalidOperationException("식생 배율이 올바르지 않습니다.");
                prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(entry.prefab);
                if (prefabs[i] == null)
                    throw new InvalidOperationException("식생 프리팹 누락: " + entry.prefab);
            }
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("호수 진입로 식생 군집 적용");
            try
            {
                var root = parent.transform.Find(MeadowRootName);
                if (root == null)
                {
                    var created = new GameObject(MeadowRootName);
                    Undo.RegisterCreatedObjectUndo(created, "식생 군집 루트");
                    created.transform.SetParent(parent.transform, false);
                    root = created.transform;
                }
                for (int i = 0; i < settings.entries.Length; i++)
                    ApplyMeadowEntry(root, terrain, settings.entries[i], prefabs[i]);
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(group);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                throw;
            }
        }

        private static void ApplyMeadowEntry(Transform root, Terrain terrain, MeadowEntry entry, GameObject prefab)
        {
            var target = root.Find(entry.name);
            if (target == null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
                Undo.RegisterCreatedObjectUndo(instance, "진입로 식생");
                instance.name = entry.name;
                target = instance.transform;
            }
            Undo.RecordObject(target, "식생 위치");
            var position = ReadPropVector(entry.position);
            position.y = terrain.SampleHeight(position) + terrain.transform.position.y;
            target.position = position;
            target.rotation = Quaternion.Euler(ReadPropVector(entry.euler));
            target.localScale = ReadPropVector(entry.scale);
            foreach (var collider in target.GetComponentsInChildren<Collider>(true))
            {
                if (!entry.disableColliders) continue;
                Undo.RecordObject(collider, "장식 식생 통과");
                collider.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
            }
            // 원본 관목에 포함된 개별 입자 효과는 군집마다 복제하지 않는다.
            foreach (var particles in target.GetComponentsInChildren<ParticleSystem>(true))
            {
                Undo.RecordObject(particles.gameObject, "식생 입자 효과 제외");
                particles.gameObject.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(particles.gameObject);
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
    }
}
