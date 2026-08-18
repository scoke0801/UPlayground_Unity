#if UNITY_EDITOR
using System.Collections.Generic;
using UPlayGround.Data.World;
using UPlayGround.Manager.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UPlayGround.Tool.Editor.Map
{
    /// <summary>
    /// Bake된 PlacementData의 제거 액션.
    /// Bake는 기존 레코드를 절대 지우지 않는 병합이라, 씬에서 사라진 배치의 레코드를 걷어내는 경로가 여기에만 있다.
    /// </summary>
    public static partial class WorldPlacementBakeUtility
    {
        /// <summary>레코드 하나를 데이터에서 제거한다. 실제로 제거했으면 true.</summary>
        public static bool RemoveRecord(WorldPlacementDataSO placementData, WorldPlacementRecord record)
        {
            if (placementData == null || record == null)
                return false;

            if (!EditorUtility.DisplayDialog(
                    "Bake 레코드 제거",
                    $"'{GetRecordDisplayName(record)}' 레코드를 '{placementData.name}'에서 제거합니다.\n" +
                    "씬에 복원해 둔 오브젝트는 지워지지 않습니다.\n계속하시겠습니까?",
                    "제거",
                    "취소"))
                return false;

            var remaining = new List<WorldPlacementRecord>(placementData.Records.Count);
            bool isRemoved = false;
            foreach (var existing in placementData.Records)
            {
                // 같은 placementGuid는 같은 배치이므로, 참조가 달라도 같은 레코드로 본다.
                if (!isRemoved && IsSameRecord(existing, record))
                {
                    isRemoved = true;
                    continue;
                }

                remaining.Add(existing);
            }

            if (!isRemoved)
                return false;

            ApplyRecords(placementData, remaining, "Remove Placement Record");
            return true;
        }

        /// <summary>데이터의 레코드를 모두 비운다. 에셋과 씬 로더 참조는 그대로 두므로 재Bake로 다시 채울 수 있다.</summary>
        public static bool ClearRecords(WorldPlacementDataSO placementData)
        {
            if (placementData == null)
                return false;

            int count = placementData.Records.Count;
            if (count == 0)
            {
                EditorUtility.DisplayDialog("Bake 데이터 비우기", $"'{placementData.name}'에 제거할 레코드가 없습니다.", "확인");
                return false;
            }

            if (!EditorUtility.DisplayDialog(
                    "Bake 데이터 비우기",
                    $"'{placementData.name}'의 레코드 {count}개를 모두 제거합니다.\n" +
                    "에셋과 씬 로더 연결은 유지되므로 다시 Bake하면 채워집니다.\n계속하시겠습니까?",
                    "비우기",
                    "취소"))
                return false;

            ApplyRecords(placementData, new List<WorldPlacementRecord>(), "Clear Placement Records");
            return true;
        }

        /// <summary>
        /// PlacementData 에셋 자체를 삭제한다. 열려 있는 씬의 로더 참조를 먼저 끊어
        /// 씬에 Missing 참조가 남지 않게 한다.
        /// </summary>
        public static bool DeletePlacementDataAsset(WorldPlacementDataSO placementData)
        {
            if (placementData == null)
                return false;

            string assetPath = AssetDatabase.GetAssetPath(placementData);
            if (string.IsNullOrEmpty(assetPath))
            {
                EditorUtility.DisplayDialog("Bake 데이터 삭제", "에셋 경로를 찾지 못해 삭제할 수 없습니다.", "확인");
                return false;
            }

            string assetName = placementData.name;
            if (!EditorUtility.DisplayDialog(
                    "Bake 데이터 삭제",
                    $"'{assetName}' 에셋을 삭제합니다 (레코드 {placementData.Records.Count}개).\n{assetPath}\n\n" +
                    "열려 있는 씬의 RuntimePlacementLoader 참조는 함께 해제하지만, " +
                    "열지 않은 씬이 이 데이터를 참조 중이면 Missing 참조가 됩니다.\n계속하시겠습니까?",
                    "삭제",
                    "취소"))
                return false;

            int detached = DetachLoadersReferencing(placementData);
            if (!AssetDatabase.DeleteAsset(assetPath))
            {
                EditorUtility.DisplayDialog("Bake 데이터 삭제", $"에셋 삭제에 실패했습니다.\n{assetPath}", "확인");
                return false;
            }

            AssetDatabase.Refresh();
            string detachMessage = detached > 0 ? $"\n열린 씬의 로더 참조 {detached}개를 해제했습니다 (씬 저장 필요)." : "";
            EditorUtility.DisplayDialog("Bake 데이터 삭제", $"'{assetName}' 에셋을 삭제했습니다.{detachMessage}", "확인");
            return true;
        }

        /// <summary>같은 배치인지 판정한다. placementGuid가 있으면 그것이 1순위 키다.</summary>
        private static bool IsSameRecord(WorldPlacementRecord left, WorldPlacementRecord right)
        {
            if (left == null || right == null)
                return false;

            if (ReferenceEquals(left, right))
                return true;

            return !string.IsNullOrEmpty(left.placementGuid) && left.placementGuid == right.placementGuid;
        }

        private static void ApplyRecords(
            WorldPlacementDataSO placementData,
            List<WorldPlacementRecord> records,
            string undoName)
        {
            Undo.RecordObject(placementData, undoName);
            placementData.SetRecords(records);
            EditorUtility.SetDirty(placementData);
            AssetDatabase.SaveAssets();
        }

        /// <summary>열려 있는 모든 씬에서 이 데이터를 참조하는 로더의 참조를 해제하고, 해제한 개수를 돌려준다.</summary>
        private static int DetachLoadersReferencing(WorldPlacementDataSO placementData)
        {
            int detached = 0;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;

                bool isSceneChanged = false;
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var loader in root.GetComponentsInChildren<RuntimePlacementLoader>(true))
                    {
                        if (loader == null || loader.PlacementData != placementData)
                            continue;

                        Undo.RecordObject(loader, "Detach Placement Data");
                        loader.EditorSetPlacementData(null);
                        EditorUtility.SetDirty(loader);
                        detached++;
                        isSceneChanged = true;
                    }
                }

                if (isSceneChanged)
                    EditorSceneManager.MarkSceneDirty(scene);
            }

            return detached;
        }
    }
}
#endif
