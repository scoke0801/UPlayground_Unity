using UnityEditor;
using UnityEngine;
using UPlayGround.Component;

namespace UPlayGround.Editor
{
    /// <summary>
    /// WeaponIKController 보조 인스펙터.
    /// 기본 필드 위에, 플레이 중 IK 상태(발화/그립보유/오프셋락/적용 weight/그립거리)를
    /// 실시간 읽기 패널로 노출해 콘솔 경고 없이 Phase 0 검증(설계서 §12.2)을 가능하게 한다.
    /// (자동 그립 주입 버튼은 Phase 1 자동배선에서 폐기될 한시 기능이라 의도적으로 제외)
    /// </summary>
    [CustomEditor(typeof(WeaponIKController))]
    public sealed class WeaponIKControllerEditor : UnityEditor.Editor
    {
        // 플레이 중 상태 패널이 매 프레임 갱신되도록.
        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (!Application.isPlaying)
                return;

            var ctrl = (WeaponIKController)target;

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("런타임 상태 (Play 중)", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle("IK 발화됨 (OnAnimatorIK)", ctrl.EditorIkEverProcessed);
                EditorGUILayout.Toggle("그립 보유", ctrl.EditorHasGrip);
                EditorGUILayout.Toggle("오프셋 락됨", ctrl.EditorOffsetLocked);
                EditorGUILayout.Slider("적용 weight", ctrl.EditorCurrentWeight, 0f, 1f);
                EditorGUILayout.FloatField("그립 거리 (m)", ctrl.DebugGripDistance);
            }

            EditorGUILayout.HelpBox(
                "그립 거리 절댓값으로 정확도를 판정하지 마세요(손목-그립 피벗 오프셋 때문에 0이 안 될 수 있음). " +
                "올바른 판정: 주손이 움직이는 모션 중 씬뷰 빨강선이 길어지지 않는가. (설계서 §12.2)",
                MessageType.Info);
        }
    }
}
