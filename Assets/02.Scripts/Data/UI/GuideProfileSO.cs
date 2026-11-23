using UnityEngine;

namespace UPlayGround.Data.UI
{
    /// <summary>길안내 연출의 종류. 새 연출을 추가하면 컨트롤러의 생성 분기에 한 줄을 더한다.</summary>
    public enum GuideEffectKind
    {
        Butterfly = 0,
        Wind = 1,
    }

    /// <summary>
    /// 퀘스트 길안내 연출의 종류와 수치를 담는 단일 소스.
    /// 연출 교체는 이 에셋의 <see cref="effectKind"/>와 <see cref="agentPrefab"/>을 바꾸는 것으로 끝나야 한다.
    /// </summary>
    [CreateAssetMenu(menuName = "UPlayGround/Guide/Profile", fileName = "GuideProfile")]
    public sealed class GuideProfileSO : ScriptableObject
    {
        [Header("연출")]
        [Tooltip("사용할 연출 종류.")]
        public GuideEffectKind effectKind = GuideEffectKind.Butterfly;

        [Tooltip("연출 개체 프리팹. 나비는 한 마리, 바람은 경로를 따라 흐르는 VFX 한 벌.")]
        public GameObject agentPrefab;

        [Tooltip("길을 찾아 연출을 실제로 시작할 때 재생할 효과음 키. 비우면 재생하지 않는다.")]
        public string startSfxKey;

        [Header("입력·조건")]
        [Min(0f), Tooltip("연출이 끝난 뒤 다음 입력을 받기까지 기다리는 시간(초).")]
        public float cooldown = 1.5f;

        [Min(0f), Tooltip("전투 중에는 길안내·퀘스트 표시 입력을 받지 않는다. 플레이어의 전투 상태에 더해, 플레이어를 노리는(어그로 중인) 몬스터가 이 거리(m) 안에 있어도 전투 중으로 본다.")]
        public float combatThreatRadius = 40f;

        [Header("경로")]
        [Min(0.1f), Tooltip("플레이어·목표를 NavMesh 위로 옮길 때 찾는 최대 거리(m).")]
        public float navMeshSampleDistance = 3f;

        [Range(1, 12), Tooltip("경로 모서리 사이를 곡선으로 채우는 분할 수.")]
        public int smoothSubdivisions = 4;

        [Min(1f), Tooltip("재생 중 플레이어가 경로에서 이만큼 벗어나면 경로를 다시 계산한다(m).")]
        public float repathDistance = 8f;

        [Header("나비 군집(플레이어 앞에서 기다리는 실제 나비)")]
        [Range(1, 24), Tooltip("agentPrefab으로 만드는 나비 수. 가까이서 실제 나비로 보이는 역할이며, 길 전체는 아래 행렬이 보여 준다.")]
        public int agentCount = 8;

        [Min(1f), Tooltip("연출 전체 길이(초). 마지막 fadeOutDuration 동안 사라진다.")]
        public float duration = 5f;

        [Min(0f)] public float fadeOutDuration = 0.8f;

        [Min(0f), Tooltip("카메라 앞에서 나타나 경로에 합류하기까지 걸리는 시간(초).")]
        public float gatherDuration = 0.7f;

        [Min(0f), Tooltip("카메라 전방 몇 m 지점에 나타나는가.")]
        public float spawnForwardDistance = 2.5f;

        [Min(0f), Tooltip("나타나는 지점 주변 흩어짐 반경(m).")]
        public float spawnRadius = 1.2f;

        [Min(0f), Tooltip("플레이어 발밑 기준 나타나는 높이(m).")]
        public float spawnHeight = 1.4f;

        [Min(0f), Tooltip("재생 시작 시 플레이어보다 앞서는 거리(m).")]
        public float leadDistanceStart = 4f;

        [Min(0f), Tooltip("재생 끝 무렵 플레이어보다 앞서는 거리(m). 가만히 서 있어도 이만큼은 길을 보여 준다.")]
        public float leadDistanceEnd = 22f;

        [Min(0f), Tooltip("개체마다 앞서는 거리가 이 범위 안에서 흩어진다(m).")]
        public float leadSpread = 2.5f;

        [Min(1f), Tooltip("카메라에서 이 거리보다 멀어지면 개체를 거리에 비례해 키워 멀리서도 같은 크기로 보이게 한다(m).")]
        public float distanceScaleReference = 8f;

        [Min(1f), Tooltip("거리 보정으로 키울 수 있는 최대 배율. 1이면 보정하지 않는다.")]
        public float distanceScaleMax = 2.6f;

        [Min(0.1f), Tooltip("경로를 따라 날 수 있는 최대 속도(m/s).")]
        public float maxSpeed = 11f;

        [Min(0.1f), Tooltip("목표 위치와의 거리 차이를 속도로 바꾸는 배율. 클수록 빨리 따라붙는다.")]
        public float catchUpGain = 2.2f;

        [Min(0f), Tooltip("경로 위 지면에서 날아다니는 평균 높이(m).")]
        public float flightHeight = 1.5f;

        [Min(0f), Tooltip("좌우·상하 팔랑임 진폭(m).")]
        public float flutterAmplitude = 0.45f;

        [Min(0.1f), Tooltip("팔랑임 주파수(Hz).")]
        public float flutterFrequency = 1.8f;

        [Min(0f), Tooltip("경로를 따라 흩어지는 옆 폭(m).")]
        public float lateralSpread = 0.9f;

        [Min(1f), Tooltip("경로 끝에서 이 거리 안으로 들어오면 목표 위를 선회한다(m).")]
        public float arriveRadius = 4f;

        [Min(0.1f), Tooltip("도착 선회 반경(m).")]
        public float arriveOrbitRadius = 1.6f;

        [Min(0.1f), Tooltip("도착 선회 각속도(rad/s).")]
        public float arriveOrbitSpeed = 1.6f;

        [Min(0.01f), Tooltip("위치를 목표로 부드럽게 따라가는 정도. 클수록 즉각 따라간다.")]
        public float followSharpness = 7f;

        [Header("나비 행렬(경로를 따라 흐르는 무리)")]
        [Tooltip("행렬 나비 본체를 그리는 ParticleSystem 프리팹(빌보드 나비 텍스처). 비우면 행렬은 보이지 않는다.")]
        public GameObject streamPrefab;

        [Tooltip("행렬 나비가 지나간 자리에 남기는 빛가루 ParticleSystem 프리팹. 비우면 잔광을 남기지 않는다.")]
        public GameObject afterglowPrefab;

        [Range(0, 128), Tooltip("경로를 따라 흐르는 행렬 나비 수. 위의 나비 군집(agentCount)과 별개다.")]
        public int streamCount = 64;

        [Min(1f), Tooltip("플레이어 위치에서 경로 앞쪽으로 행렬이 이어지는 길이(m). 끝에 닿은 나비는 플레이어 앞에서 다시 날아오른다.")]
        public float streamLength = 36f;

        [Tooltip("행렬이 시작되는 지점. 플레이어가 경로에서 도달한 거리 기준 앞(+)·뒤(-) 거리(m).")]
        public float streamNearOffset = 1f;

        [Min(0.1f), Tooltip("행렬 나비가 경로를 따라 나는 평균 속도(m/s). 빠를수록 흐름 방향이 또렷하다.")]
        public float streamSpeed = 7f;

        [Range(0f, 0.9f), Tooltip("행렬 나비 속도의 개체별 흩어짐 비율. 클수록 행렬이 고르게 퍼진다.")]
        public float streamSpeedVariance = 0.3f;

        [Min(0f), Tooltip("재생 시작 후 행렬 나비가 모두 날아오르기까지 걸리는 시간(초). 짧으면 한 무리로 몰려가고, 길면 고르게 이어진다.")]
        public float streamReleaseDuration = 3f;

        [Min(0f), Tooltip("행렬 끝에서 나비가 흐려지며 사라지는 구간 길이(m).")]
        public float streamEdgeFadeDistance = 6f;

        [Min(0f), Tooltip("행렬 나비가 경로 옆으로 흩어지는 폭(m).")]
        public float streamLateralSpread = 0.45f;

        [Min(0f), Tooltip("행렬 나비가 비행 높이(flightHeight) 위아래로 흩어지는 폭(m).")]
        public float streamHeightSpread = 0.3f;

        [Min(0f), Tooltip("행렬 나비의 좌우·상하 팔랑임 진폭(m). 주파수는 flutterFrequency를 쓴다. 크면 행렬이 뭉쳐 길의 선이 흐려진다.")]
        public float streamFlutterAmplitude = 0.2f;

        [Min(0.01f), Tooltip("가까이 있을 때 행렬 나비 한 마리의 날개 폭(m).")]
        public float streamSize = 0.55f;

        [Range(0f, 0.1f), Tooltip("멀리 있어도 행렬 나비가 화면 높이의 이 비율보다 작아지지 않게 키운다. 0.022면 1080p에서 약 24픽셀.")]
        public float streamMinScreenFraction = 0.022f;

        [Min(0.1f), Tooltip("행렬 나비 날갯짓 주파수(Hz).")]
        public float streamFlapFrequency = 6f;

        [Tooltip("행렬 나비 색. 나비마다 이 그라디언트에서 무작위로 고른다. 알파는 최대 불투명도다.")]
        public Gradient streamColors = new()
        {
            colorKeys = new[]
            {
                new GradientColorKey(new Color(1f, 1f, 0.92f), 0f),
                new GradientColorKey(new Color(1f, 0.82f, 0.45f), 1f),
            },
            alphaKeys = new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f),
            },
        };

        [Min(0f), Tooltip("행렬 나비 한 마리가 1초에 떨어뜨리는 빛가루 수.")]
        public float afterglowRate = 5f;

        [Min(0.1f), Tooltip("빛가루가 남아 있는 시간(초). 길수록 지나간 길이 오래 보인다.")]
        public float afterglowLifetime = 2.4f;

        [Min(0f), Tooltip("빛가루 크기. 행렬 나비 날개 폭(streamSize)에 대한 비율.")]
        public float afterglowSizeRatio = 0.5f;

        [Tooltip("빛가루가 떠오르는 속도(m/s).")]
        public float afterglowRiseSpeed = 0.15f;

        [Header("경로 띠(바람결)")]
        [Tooltip("경로 위에 흐르는 띠 프리팹. LineRenderer 하나와 흐름 텍스처 재질을 가진다. 비우면 띠를 그리지 않는다.")]
        public GameObject pathRibbonPrefab;

        [Min(1f), Tooltip("플레이어 앞쪽으로 띠를 그리는 길이(m).")]
        public float ribbonLength = 30f;

        [Min(0f), Tooltip("경로 표면에서 띠를 띄우는 높이(m).")]
        public float ribbonHeight = 0.3f;

        [Tooltip("띠 무늬가 경로를 따라 흐르는 속도. 음수면 반대 방향.")]
        public float ribbonScrollSpeed = 1.4f;
    }
}
