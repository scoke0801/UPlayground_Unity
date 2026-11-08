using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UPlayGround.EditorTools;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class LakeVillageWorldPolishTool
    {
        private const string PropReadabilityPath = "Assets/10.Datas/World/OpenWorld/LakeVillagePropReadability.json";

        [Serializable]
        private sealed class PropReadabilitySettings
        {
            public string scenePath;
            public PropReadabilityEntry[] entries;
        }

        [Serializable]
        private sealed class PropReadabilityEntry
        {
            public string path;
            public bool active;
            public float[] position;
            public float[] scale;
            public float[] euler;
        }

        /// <summary>확정된 진입로 바위와 문서 소품 배치만 재적용하고 씬 저장은 사용자에게 맡긴다.</summary>
        [UPlaygroundTool("OpenWorld/호수 마을 소품 가독성 적용")]
        public static void ApplyPropReadabilityOnly()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Edit Mode에서 실행하세요.");

            var settings = JsonUtility.FromJson<PropReadabilitySettings>(File.ReadAllText(PropReadabilityPath));
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (settings == null || settings.entries == null || scene.path != settings.scenePath)
                throw new InvalidOperationException("소품 설정 또는 활성 씬이 올바르지 않습니다.");

            var targets = new Transform[settings.entries.Length];
            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < settings.entries.Length; i++)
            {
                var entry = settings.entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.path))
                    throw new InvalidOperationException("소품 경로가 비어 있습니다.");
                ReadPropVector(entry.position);
                ReadPropVector(entry.euler);
                Vector3 scale = ReadPropVector(entry.scale);
                if (scale.x <= 0f || scale.y <= 0f || scale.z <= 0f)
                    throw new InvalidOperationException("소품 배율은 양수여야 합니다: " + entry.path);
                foreach (var root in roots)
                {
                    string prefix = root.name + "/";
                    if (entry.path.StartsWith(prefix, StringComparison.Ordinal))
                        targets[i] = root.transform.Find(entry.path.Substring(prefix.Length));
                }
                if (targets[i] == null)
                    throw new InvalidOperationException("소품을 찾지 못했습니다: " + entry.path);
            }

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("호수 마을 소품 가독성 적용");
            try
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    var target = targets[i];
                    var entry = settings.entries[i];
                    Undo.RecordObjects(new UnityEngine.Object[] { target, target.gameObject }, "호수 소품 배치");
                    target.localPosition = ReadPropVector(entry.position);
                    target.localEulerAngles = ReadPropVector(entry.euler);
                    target.localScale = ReadPropVector(entry.scale);
                    target.gameObject.SetActive(entry.active);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(target.gameObject);
                }
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(group);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                throw;
            }
        }

        private static Vector3 ReadPropVector(float[] values)
        {
            if (values == null || values.Length != 3)
                throw new InvalidOperationException("소품 좌표는 세 성분이 필요합니다.");
            foreach (float value in values)
                if (float.IsNaN(value) || float.IsInfinity(value))
                    throw new InvalidOperationException("소품 좌표가 유한하지 않습니다.");
            return new Vector3(values[0], values[1], values[2]);
        }
    }
}
