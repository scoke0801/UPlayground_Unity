using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Editor
{
    [CustomEditor(typeof(SpecialBreakAttackAsset))]
    public class SpecialBreakAttackAssetEditor : UnityEditor.Editor
    {
        SerializedProperty _ownerType;
        SerializedProperty _animKey;
        SerializedProperty _motionSet;
        SerializedProperty _duration;
        SerializedProperty _fallbackHitTime;
        SerializedProperty _cameraProfile;
        SerializedProperty _searchRange;
        SerializedProperty _searchAngle;
        SerializedProperty _startDistance;
        SerializedProperty _maxSlideSpeed;
        SerializedProperty _slideDuration;
        SerializedProperty _damageByMaxHpRate;
        SerializedProperty _fixedDamage;
        SerializedProperty _hitStopDuration;
        SerializedProperty _startVfxKey;
        SerializedProperty _hitVfxKey;
        SerializedProperty _finishVfxKey;

        void OnEnable()
        {
            _ownerType = serializedObject.FindProperty("ownerType");
            _animKey = serializedObject.FindProperty("animKey");
            _motionSet = serializedObject.FindProperty("motionSet");
            _duration = serializedObject.FindProperty("duration");
            _fallbackHitTime = serializedObject.FindProperty("fallbackHitTime");
            _cameraProfile = serializedObject.FindProperty("cameraProfile");
            _searchRange = serializedObject.FindProperty("searchRange");
            _searchAngle = serializedObject.FindProperty("searchAngle");
            _startDistance = serializedObject.FindProperty("startDistance");
            _maxSlideSpeed = serializedObject.FindProperty("maxSlideSpeed");
            _slideDuration = serializedObject.FindProperty("slideDuration");
            _damageByMaxHpRate = serializedObject.FindProperty("damageByMaxHpRate");
            _fixedDamage = serializedObject.FindProperty("fixedDamage");
            _hitStopDuration = serializedObject.FindProperty("hitStopDuration");
            _startVfxKey = serializedObject.FindProperty("startVfxKey");
            _hitVfxKey = serializedObject.FindProperty("hitVfxKey");
            _finishVfxKey = serializedObject.FindProperty("finishVfxKey");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawSection("대상 / 모션", new Color(0.35f, 0.65f, 1f));
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(_ownerType, new GUIContent("소유 캐릭터"));
                EditorGUILayout.PropertyField(_animKey, new GUIContent("재생 AnimKey"));
                EditorGUILayout.PropertyField(_motionSet, new GUIContent("전용 MotionSet"));
                EditorGUILayout.PropertyField(_duration, new GUIContent("상태 지속시간"));
                EditorGUILayout.PropertyField(_fallbackHitTime, new GUIContent("폴백 타격 시간"));
            }

            DrawMotionWarnings();

            DrawSection("탐색 / 위치 보정", new Color(0.25f, 0.85f, 0.65f));
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(_searchRange, new GUIContent("탐색 범위"));
                EditorGUILayout.PropertyField(_searchAngle, new GUIContent("탐색 각도"));
                EditorGUILayout.PropertyField(_startDistance, new GUIContent("시작 거리"));
                EditorGUILayout.PropertyField(_maxSlideSpeed, new GUIContent("최대 슬라이드 속도"));
                EditorGUILayout.PropertyField(_slideDuration, new GUIContent("슬라이드 시간"));
            }

            DrawSection("피해 / 피드백", new Color(1f, 0.45f, 0.25f));
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(_damageByMaxHpRate, new GUIContent("최대 HP 피해율"));
                EditorGUILayout.PropertyField(_fixedDamage, new GUIContent("고정 피해"));
                EditorGUILayout.PropertyField(_hitStopDuration, new GUIContent("히트스톱 시간"));
            }

            DrawSection("카메라 / VFX", new Color(0.75f, 0.35f, 1f));
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(_cameraProfile, new GUIContent("카메라 프로필"));
                EditorGUILayout.PropertyField(_startVfxKey, new GUIContent("시작 VFX 키"));
                EditorGUILayout.PropertyField(_hitVfxKey, new GUIContent("타격 VFX 키"));
                EditorGUILayout.PropertyField(_finishVfxKey, new GUIContent("종료 VFX 키"));
            }

            EditorGUILayout.HelpBox(
                "VFX 키는 데이터 필드만 준비되어 있습니다. 실제 재생은 MotionEvent 또는 후속 런타임 연결이 필요합니다.",
                MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }

        void DrawMotionWarnings()
        {
            string animKeyName = _animKey.enumValueIndex >= 0 && _animKey.enumValueIndex < _animKey.enumNames.Length
                ? _animKey.enumNames[_animKey.enumValueIndex]
                : string.Empty;
            if (animKeyName == nameof(AnimKey.None))
                EditorGUILayout.HelpBox("AnimKey가 None이면 FinishAttack, Attack_1 순으로 폴백합니다.", MessageType.Info);

            if (_fallbackHitTime.floatValue > _duration.floatValue)
                EditorGUILayout.HelpBox("폴백 타격 시간이 상태 지속시간보다 깁니다. MotionEvent가 없으면 피해가 적용되지 않을 수 있습니다.", MessageType.Warning);

            if (_motionSet.objectReferenceValue == null)
                EditorGUILayout.HelpBox("전용 MotionSet은 선택 사항입니다. 실제 재생은 PlayerActorAnimator의 AnimKey MotionSet 보유 여부를 따릅니다.", MessageType.None);
        }

        static void DrawSection(string title, Color accent)
        {
            Rect rect = GUILayoutUtility.GetRect(0, 24f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(accent.r, accent.g, accent.b, 0.18f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), accent);
            EditorGUI.LabelField(new Rect(rect.x + 8f, rect.y, rect.width - 16f, rect.height), title, EditorStyles.boldLabel);
        }
    }
}
