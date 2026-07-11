#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Tool.Editor.Actor
{
    [CustomEditor(typeof(ActorDefinitionSO))]
    public class ActorDefinitionSOEditor : UnityEditor.Editor
    {
        private SerializedProperty _actorId;
        private SerializedProperty _displayName;
        private SerializedProperty _description;
        private SerializedProperty _actorType;
        private SerializedProperty _characterType;
        private SerializedProperty _targetLayerMask;
        private SerializedProperty _prefab;
        private SerializedProperty _statData;
        private SerializedProperty _poiseData;
        private SerializedProperty _monsterProfile;
        private SerializedProperty _breakGaugeData;
        private SerializedProperty _monsterScaling;
        private SerializedProperty _grade;
        private SerializedProperty _level;
        private SerializedProperty _attackData;
        private SerializedProperty _combatDefensePolicy;
        private SerializedProperty _combatReactionPolicy;
        private SerializedProperty _behaviorData;
        private SerializedProperty _npcData;
        private SerializedProperty _dropTable;
        private SerializedProperty _recruitableAs;
        private SerializedProperty _expReward;
        private SerializedProperty _goldReward;

        private void OnEnable()
        {
            _actorId = serializedObject.FindProperty("actorId");
            _displayName = serializedObject.FindProperty("displayName");
            _description = serializedObject.FindProperty("description");
            _actorType = serializedObject.FindProperty("actorType");
            _characterType = serializedObject.FindProperty("characterType");
            _targetLayerMask = serializedObject.FindProperty("targetLayerMask");
            _prefab = serializedObject.FindProperty("prefab");
            _statData = serializedObject.FindProperty("statData");
            _poiseData = serializedObject.FindProperty("poiseData");
            _monsterProfile = serializedObject.FindProperty("monsterProfile");
            _breakGaugeData = serializedObject.FindProperty("breakGaugeData");
            _monsterScaling = serializedObject.FindProperty("monsterScaling");
            _grade = serializedObject.FindProperty("grade");
            _level = serializedObject.FindProperty("level");
            _attackData = serializedObject.FindProperty("attackData");
            _combatDefensePolicy = serializedObject.FindProperty("combatDefensePolicy");
            _combatReactionPolicy = serializedObject.FindProperty("combatReactionPolicy");
            _behaviorData = serializedObject.FindProperty("behaviorData");
            _npcData = serializedObject.FindProperty("npcData");
            _dropTable = serializedObject.FindProperty("dropTable");
            _recruitableAs = serializedObject.FindProperty("recruitableAs");
            _expReward = serializedObject.FindProperty("expReward");
            _goldReward = serializedObject.FindProperty("goldReward");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            ActorType actorType = (ActorType)_actorType.intValue;
            bool isPlayer = actorType.HasFlag(ActorType.Player);
            bool isMonster = actorType.HasFlag(ActorType.Monster);
            bool isNpc = actorType.HasFlag(ActorType.NPC);

            DrawIdentitySection();
            DrawBaseSection();
            DrawPrefabSection();

            if (isMonster)
            {
                DrawStatSection(required: true, playerFallback: false);
                DrawMonsterSection();
                DrawMonsterCombatSection();
                DrawMonsterRewardSection();
            }
            else
            {
                DrawStatSection(required: false, playerFallback: isPlayer);
            }

            if (isNpc)
                DrawNpcSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawIdentitySection()
        {
            DrawHeader("식별");
            EditorGUILayout.PropertyField(_actorId);
            EditorGUILayout.PropertyField(_displayName);
            EditorGUILayout.PropertyField(_description);
        }

        private void DrawBaseSection()
        {
            DrawHeader("Actor 기본 정보");
            EditorGUILayout.PropertyField(_actorType);
            EditorGUILayout.PropertyField(_characterType);
            EditorGUILayout.PropertyField(_targetLayerMask);
        }

        private void DrawPrefabSection()
        {
            DrawHeader("프리팹");
            EditorGUILayout.PropertyField(_prefab);
        }

        private void DrawStatSection(bool required, bool playerFallback)
        {
            DrawHeader(required ? "스탯 데이터" : "스탯 데이터 (선택)");

            if (playerFallback)
            {
                EditorGUILayout.HelpBox(
                    "PlayerActor는 PartyConfigSO의 성장/파티 데이터가 런타임 권위입니다. Stat Data는 PartyManager 없이 단독 실행할 때의 폴백입니다.",
                    MessageType.Info);
            }

            EditorGUILayout.PropertyField(_statData);
            EditorGUILayout.PropertyField(_poiseData);
        }

        private void DrawMonsterSection()
        {
            DrawHeader("몬스터 프로필");
            EditorGUILayout.PropertyField(_monsterProfile);

            DrawHeader("몬스터 데이터 (레거시 호환)");
            EditorGUILayout.PropertyField(_breakGaugeData);
            EditorGUILayout.PropertyField(_monsterScaling);

            DrawHeader("몬스터 메타");
            EditorGUILayout.PropertyField(_grade);
            EditorGUILayout.PropertyField(_level);
        }

        private void DrawMonsterCombatSection()
        {
            DrawHeader("전투/AI 데이터");
            EditorGUILayout.PropertyField(_attackData);
            EditorGUILayout.PropertyField(_combatDefensePolicy);
            EditorGUILayout.PropertyField(_combatReactionPolicy);
            EditorGUILayout.PropertyField(_behaviorData);
        }

        private void DrawMonsterRewardSection()
        {
            DrawHeader("드랍 데이터");
            EditorGUILayout.PropertyField(_dropTable);

            DrawHeader("합류");
            EditorGUILayout.PropertyField(_recruitableAs);

            DrawHeader("성장 보상");
            EditorGUILayout.PropertyField(_expReward);
            EditorGUILayout.PropertyField(_goldReward);
        }

        private void DrawNpcSection()
        {
            DrawHeader("NPC 데이터");
            EditorGUILayout.PropertyField(_npcData);
        }

        private static void DrawHeader(string label)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        }
    }
}
#endif
