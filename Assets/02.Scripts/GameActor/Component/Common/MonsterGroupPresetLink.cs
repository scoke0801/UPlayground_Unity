using UnityEngine;

namespace UPlayGround.Components
{
    /// <summary>
    /// 그룹 프리셋으로 배치된 MonsterGroupController에 남기는 저작 정보.
    /// 런타임 로직은 없으며, 프리셋이 갱신됐을 때 배치 인스턴스를 찾아내기 위한 역참조 용도다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MonsterGroupPresetLink : MonoBehaviour
    {
        [SerializeField, Tooltip("유래한 MonsterGroupPresetSO의 PresetId.")]
        private string _presetId;

        [SerializeField, Tooltip("이 그룹에 적용된 시점의 프리셋 리비전. 프리셋 쪽이 더 크면 갱신 대상이다.")]
        private int _appliedRevision;

        public string PresetId => _presetId;
        public int AppliedRevision => _appliedRevision;

#if UNITY_EDITOR
        public void EditorSetLink(string presetId, int appliedRevision)
        {
            _presetId = presetId;
            _appliedRevision = appliedRevision;
        }
#endif
    }
}
