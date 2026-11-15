using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Pet;

namespace UPlayGround.EditorTools
{
    [CustomEditor(typeof(PetBondProgressionSO))]
    public sealed class PetBondProgressionEditor : UnityEditor.Editor
    {
        private float _supportsPerHour = 6f;
        private float _encountersPerHour = 4f;
        private float _objectivesPerHour = 2f;
        private float _questsPerHour = 1f;
        private float _activeMinutesPerHour = 40f;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("유대 도달 예상치", EditorStyles.boldLabel);
            _supportsPerHour = Mathf.Max(0f, EditorGUILayout.FloatField("시간당 전투 도움", _supportsPerHour));
            _encountersPerHour = Mathf.Max(0f, EditorGUILayout.FloatField("시간당 교전 종료", _encountersPerHour));
            _objectivesPerHour = Mathf.Max(0f, EditorGUILayout.FloatField("시간당 목표 완료", _objectivesPerHour));
            _questsPerHour = Mathf.Max(0f, EditorGUILayout.FloatField("시간당 퀘스트 완료", _questsPerHour));
            _activeMinutesPerHour = Mathf.Clamp(EditorGUILayout.FloatField("시간당 생활 활동(분)", _activeMinutesPerHour), 0f, 60f);
            var data = (PetBondProgressionSO)target;
            float raw = _supportsPerHour * data.supportPoints * data.sweepWeight + _encountersPerHour * data.encounterPoints
                + _objectivesPerHour * data.objectivePoints + _questsPerHour * data.questPoints
                + _activeMinutesPerHour * 60f / Mathf.Max(1f, data.lifestyleSecondsPerPoint);
            EditorGUILayout.HelpBox(raw > 0f
                ? $"입력 가정의 감쇠 전 적립: 시간당 {raw:0.#}. 2단계 {data.secondSlotPoints / raw:0.#}시간, 3단계 {data.signaturePoints / raw:0.#}시간. 실제 시간은 동행 비율·원천 한도·감쇠에 따라 더 길어집니다. 목표 시간을 확정하는 수치가 아닙니다."
                : "활동 가정을 입력하면 감쇠 전 예상치를 표시합니다.", MessageType.Info);
        }
    }
}
