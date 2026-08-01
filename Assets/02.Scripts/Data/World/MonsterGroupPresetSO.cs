using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Actor;

namespace UPlayGround.Data.World
{
    /// <summary>
    /// 그룹 프리셋의 멤버 1종. 위치는 그룹 앵커 기준 로컬 오프셋으로만 저장한다.
    /// 월드 좌표를 저장하면 프리셋이 특정 지형에 종속되므로 저장하지 않으며,
    /// 높이(Y)는 배치 시점에 멤버별 레이캐스트로 새로 결정한다.
    /// </summary>
    [Serializable]
    public sealed class MonsterGroupPresetMember
    {
        [Tooltip("배치 소스. 우선 사용된다.")]
        public ActorDefinitionSO definition;

        [Tooltip("ActorDatabase에 없는 배치물용 폴백. definition이 비었을 때만 사용된다.")]
        public GameObject directPrefab;

        [Tooltip("그룹 앵커 기준 로컬 오프셋. XZ만 사용하며 Y는 배치 시 지면 스냅으로 대체된다.")]
        public Vector3 localOffset;

        [Tooltip("앵커 forward 기준 상대 yaw(도).")]
        public float localYaw;

        public Vector3 scale = Vector3.one;

        [Tooltip("2 이상이면 jitterRadius 범위 안에 산개해 배치한다.")]
        [Min(1)] public int count = 1;

        [Tooltip("count가 2 이상일 때 오프셋 지점 주변으로 흩뿌릴 반경(m).")]
        [Min(0f)] public float jitterRadius;

        public bool initiallyActive = true;

        public string DisplayName
        {
            get
            {
                if (definition != null)
                    return string.IsNullOrEmpty(definition.displayName) ? definition.name : definition.displayName;

                return directPrefab != null ? directPrefab.name : "(비어 있음)";
            }
        }

        public GameObject ResolvePrefab()
        {
            if (definition != null && definition.prefab != null)
                return definition.prefab;

            return directPrefab;
        }

        public bool IsValid => ResolvePrefab() != null;
    }

    /// <summary>
    /// 몬스터 조우(encounter) 하나를 통째로 저작·재사용하기 위한 프리셋.
    /// 씬 클릭 1회로 MonsterGroupController 앵커와 멤버 전원을 생성한다.
    /// </summary>
    [CreateAssetMenu(fileName = "MonsterGroupPreset", menuName = "UPlayGround/World/Monster Group Preset")]
    public sealed class MonsterGroupPresetSO : ScriptableObject
    {
        [Tooltip("프리셋 고유 키. Bake 레코드에 기록되어 배치물의 유래를 추적한다.")]
        [SerializeField] private string _presetId;

        [SerializeField] private string _displayName;

        [Tooltip("배치 도구 좌측 목록의 그룹 헤더로 쓰인다. 예: 숲 순찰, 보스 호위")]
        [SerializeField] private string _category = "기본";

        [TextArea(2, 4)]
        [SerializeField] private string _description;

        [Tooltip("씬 뷰 프리뷰 앵커 링의 반경(m).")]
        [Min(0.5f)] [SerializeField] private float _anchorRadiusHint = 6f;

        [Tooltip("배치 시 MonsterGroupController 설정 스냅샷을 적용할지 여부.")]
        [SerializeField] private bool _applyGroupSettings = true;

        /// <remarks>
        /// MonsterGroupController의 직렬화 필드를 EditorJsonUtility로 통째로 캡처한 값.
        /// 필드를 하나씩 미러링하면 컨트롤러가 바뀔 때마다 드리프트가 생기므로 스냅샷 방식을 쓴다.
        /// 단, 씬 오브젝트 참조(예: _visibilityCamera)는 이 방식으로 보존되지 않는다.
        /// </remarks>
        [HideInInspector]
        [SerializeField] private string _groupSettingsJson;

        [SerializeField] private List<MonsterGroupPresetMember> _members = new();

        [Tooltip("프리셋이 저장될 때마다 증가한다. 배치된 그룹의 적용 리비전과 비교해 갱신 여부를 판단한다.")]
        [SerializeField] private int _revision = 1;

        public string PresetId => _presetId;
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public string Category => string.IsNullOrEmpty(_category) ? "기본" : _category;
        public string Description => _description;
        public float AnchorRadiusHint => _anchorRadiusHint;
        public bool ApplyGroupSettings => _applyGroupSettings;
        public string GroupSettingsJson => _groupSettingsJson;
        public IReadOnlyList<MonsterGroupPresetMember> Members => _members;
        public int Revision => _revision;

        /// <summary>배치 시 실제로 생성될 인스턴스 총 개수.</summary>
        public int TotalInstanceCount
        {
            get
            {
                int total = 0;
                for (int i = 0; i < _members.Count; i++)
                {
                    var member = _members[i];
                    if (member != null && member.IsValid)
                        total += Mathf.Max(1, member.count);
                }

                return total;
            }
        }

#if UNITY_EDITOR
        /// <summary>에디터 캡처 전용. 프리셋 내용을 통째로 교체하고 리비전을 올린다.</summary>
        public void EditorSetContent(
            string presetId,
            string groupSettingsJson,
            IEnumerable<MonsterGroupPresetMember> members)
        {
            if (!string.IsNullOrEmpty(presetId))
                _presetId = presetId;

            _groupSettingsJson = groupSettingsJson;
            _members = new List<MonsterGroupPresetMember>(members);
            _revision++;
        }

        public void EditorEnsurePresetId()
        {
            if (string.IsNullOrEmpty(_presetId))
                _presetId = Guid.NewGuid().ToString("N");
        }
#endif
    }
}
