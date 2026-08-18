using UnityEngine;

namespace UPlayGround.Dialogue
{
    /// <summary>
    /// 대화 연출용 임시 화자(대역)의 배치와 등장·소멸 연출 수치.
    /// 에셋이 없어도 기본값으로 동작하므로, 연출 조정이 필요할 때만 에셋을 만들어 Addressable로 등록한다.
    /// </summary>
    [CreateAssetMenu(
        menuName = "UPlayGround/대화/Stage Settings",
        fileName = "DialogueStageSettings")]
    public class DialogueStageSettingsSO : ScriptableObject
    {
        public const string AddressableKey = "DialogueStageSettings";

        [Header("임시 화자 스폰")]
        [Tooltip("월드에 없는 화자를 대화 동안만 세워둡니다. 끄면 카메라만 플레이어를 대역으로 씁니다.")]
        [SerializeField] private bool _spawnMissingSpeakers = true;

        [Tooltip("한 대화에서 세울 수 있는 임시 화자 수 상한.")]
        [Min(1)] [SerializeField] private int _maxStandInCount = 3;

        [Tooltip("플레이어 정면으로부터의 거리(m).")]
        [Min(0.5f)] [SerializeField] private float _spawnDistance = 3f;

        [Tooltip("여러 명을 세울 때 좌우로 벌리는 간격(m).")]
        [Min(0f)] [SerializeField] private float _lateralSpacing = 1.1f;

        [Tooltip("플레이어 발높이와 이보다 차이 나는 지면은 배치 후보에서 제외합니다.")]
        [Min(0f)] [SerializeField] private float _maxHeightDelta = 2f;

        [Header("등장·소멸 연출")]
        [Tooltip("등장 디졸브 시간(초). 0이면 즉시 나타납니다.")]
        [Min(0f)] [SerializeField] private float _revealDuration = 0.45f;

        [Tooltip("소멸 디졸브 시간(초). 0이면 즉시 사라집니다.")]
        [Min(0f)] [SerializeField] private float _dissolveDuration = 0.6f;

        public bool SpawnMissingSpeakers => _spawnMissingSpeakers;
        public int MaxStandInCount => Mathf.Max(1, _maxStandInCount);
        public float SpawnDistance => Mathf.Max(0.5f, _spawnDistance);
        public float LateralSpacing => Mathf.Max(0f, _lateralSpacing);
        public float MaxHeightDelta => Mathf.Max(0f, _maxHeightDelta);
        public float RevealDuration => Mathf.Max(0f, _revealDuration);
        public float DissolveDuration => Mathf.Max(0f, _dissolveDuration);
    }
}
