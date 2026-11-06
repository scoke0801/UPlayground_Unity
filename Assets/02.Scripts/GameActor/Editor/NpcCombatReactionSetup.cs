using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    /// <summary>
    /// 비전투 NPC에 전투 반응(NpcCombatReaction + BehaviorTreeRunner)을 연결하는 데이터 연결 도구.
    /// 몇 번 실행해도 같은 결과가 되도록 이미 붙은 컴포넌트는 값만 다시 맞춘다.
    /// </summary>
    public static class NpcCombatReactionSetup
    {
        public const string DefaultProfilePath = "Assets/10.Datas/Actor/NpcCombatReaction_Default.asset";
        public const string TreePath = "Assets/10.Datas/AI/BehaviorTree/BT_NpcCombatReaction.asset";

        private const float TreeTickInterval = 0.25f;

        /// <summary>월드 주민·도시 NPC 프리팹. 서사 배치 NPC와 외형 도구용 프리팹은 제외한다.</summary>
        private static readonly string[] TargetFolders =
        {
            "Assets/03.Prefabs/Characters/WorldResidents",
            "Assets/03.Prefabs/Characters/WorldCities",
        };

        /// <summary>대상 프리팹 전체에 반응을 연결한다. 일회성 적용이라 툴 런처에는 올리지 않는다.</summary>
        [UPlayGround.EditorTools.UPlaygroundTool("NpcCombatReaction.ApplyToWorldNpcPrefabs")]
        public static void ApplyToWorldNpcPrefabs()
        {
            var profile = AssetDatabase.LoadAssetAtPath<NpcCombatReactionProfileSO>(DefaultProfilePath);
            var tree = AssetDatabase.LoadAssetAtPath<BehaviorTreeAsset>(TreePath);
            if (profile == null || tree == null)
            {
                Debug.LogError($"[NpcCombatReaction] 프로파일 또는 BT 에셋이 없습니다. profile={profile != null}, tree={tree != null}");
                return;
            }

            var applied = new List<string>();
            var skipped = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", TargetFolders))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                (TryApply(path, profile, tree) ? applied : skipped).Add(path);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[NpcCombatReaction] 적용 {applied.Count}개 / 제외 {skipped.Count}개\n제외: {string.Join("\n", skipped)}");
        }

        /// <summary>프리팹 하나에 반응을 연결한다. NPC가 아니거나 전투 가능 NPC면 false.</summary>
        public static bool TryApply(string prefabPath, NpcCombatReactionProfileSO profile, BehaviorTreeAsset tree)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var actor = root.GetComponent<NpcActor>();
                if (actor == null || actor.HasActorType(ActorType.Combat)) return false;

                Wire(root, profile, tree);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>씬·프리팹 어느 쪽 루트든 컴포넌트와 참조를 맞춘다. P09 NPC 템플릿도 이 경로를 쓴다.</summary>
        public static void Wire(GameObject root, NpcCombatReactionProfileSO profile, BehaviorTreeAsset tree)
        {
            var reaction = root.GetComponent<NpcCombatReaction>();
            if (reaction == null) reaction = root.AddComponent<NpcCombatReaction>();
            SetReference(reaction, "_profile", profile);

            var runner = root.GetComponent<BehaviorTreeRunner>();
            if (runner == null) runner = root.AddComponent<BehaviorTreeRunner>();
            SetReference(runner, "_treeAsset", tree);

            var runnerObject = new SerializedObject(runner);
            runnerObject.FindProperty("_tickInterval").floatValue = TreeTickInterval;
            // 적 전용 타겟 거리 감쇠는 NPC에 의미가 없다.
            runnerObject.FindProperty("_useDistanceLod").boolValue = false;
            runnerObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetReference(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(fieldName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
