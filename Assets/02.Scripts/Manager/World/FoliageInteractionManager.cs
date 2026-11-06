using UnityEngine;
using UPlayGround.Data.Config;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 플레이어 주변 액터 중 가까운 최대 8명의 위치와 이동 속도를 풀 셰이더 전역 값으로 전달해
    /// 풀이 밟히면 눕는 연출을 만든다. 게임플레이 판정에는 관여하지 않는다.
    /// 후보 선정은 일정 간격으로만 하고, 매 프레임에는 선정된 슬롯의 위치·강도만 갱신한다.
    /// </summary>
    public sealed class FoliageInteractionManager : BaseManager<FoliageInteractionManager>, IManager, IUpdatableManager
    {
        private const ActorType AffectingActorTypes = ActorType.Player | ActorType.Monster | ActorType.NPC;

        private static readonly int ActorsId = Shader.PropertyToID("_UPG_FoliageActors");
        private static readonly int ActorParamsId = Shader.PropertyToID("_UPG_FoliageActorParams");
        private static readonly int ActorCountId = Shader.PropertyToID("_UPG_FoliageActorCount");
        private static readonly int BendId = Shader.PropertyToID("_UPG_FoliageBend");

        private struct Slot
        {
            public Transform Target;
            public Vector3 LastPosition;
            public float Strength;
            public float StrengthVelocity;
        }

        private readonly Slot[] _slots = new Slot[FoliageInteractionSettingsSO.MaxActorCount];
        private readonly Slot[] _nextSlots = new Slot[FoliageInteractionSettingsSO.MaxActorCount];
        private readonly float[] _candidateSqrDistances = new float[FoliageInteractionSettingsSO.MaxActorCount];
        private readonly Vector4[] _actorPositions = new Vector4[FoliageInteractionSettingsSO.MaxActorCount];
        private readonly Vector4[] _actorParams = new Vector4[FoliageInteractionSettingsSO.MaxActorCount];

        private FoliageInteractionSettingsSO _settings;
        private int _slotCount;
        private float _nextRefreshTime;

        public void Init()
        {
            _settings = Resources.Load<FoliageInteractionSettingsSO>(FoliageInteractionSettingsSO.ResourcePath);
            if (_settings == null)
            {
                // 에셋이 없어도 기본값으로 동작해야 신규 프로젝트 설정 누락이 풀 연출을 끄지 않는다.
                _settings = ScriptableObject.CreateInstance<FoliageInteractionSettingsSO>();
                _settings.hideFlags = HideFlags.DontSave;
            }
        }

        public void AfterInit() { }
        public void OnFixedUpdate() { }
        public void OnLateUpdate() { }

        public void Dispose()
        {
            ClearSlots();
            PublishToShaders();
        }

        public void OnSceneChanged(string sceneType)
        {
            ClearSlots();
            PublishToShaders();
        }

        public void OnUpdate()
        {
            if (_settings == null)
                return;

            Transform player = Svc.ActorQuery?.PlayerTransform;
            if (player == null)
            {
                ClearSlots();
                PublishToShaders();
                return;
            }

            if (Time.unscaledTime >= _nextRefreshTime)
            {
                _nextRefreshTime = Time.unscaledTime + _settings.candidateRefreshInterval;
                RefreshCandidates(player.position);
            }

            UpdateSlots(Time.deltaTime);
            PublishToShaders();
        }

        private void RefreshCandidates(Vector3 center)
        {
            int count = 0;
            float rangeSqr = _settings.candidateRange * _settings.candidateRange;

            foreach (IWorldActor actor in Svc.ActorQuery.AllActors)
            {
                if (actor == null || !actor.IsAlive || (actor.ActorType & AffectingActorTypes) == 0)
                    continue;

                Transform actorTransform = actor.Transform;
                if (actorTransform == null || !actorTransform.gameObject.activeInHierarchy)
                    continue;

                float sqrDistance = (actorTransform.position - center).sqrMagnitude;
                if (sqrDistance > rangeSqr)
                    continue;

                count = InsertByDistance(actorTransform, sqrDistance, count);
            }

            for (int i = 0; i < count; i++)
                _slots[i] = _nextSlots[i];
            _slotCount = count;
        }

        /// <summary>가까운 순 고정 길이 목록에 삽입하고 새 길이를 반환한다. 이전 슬롯의 속도 상태는 같은 Transform이면 이어받는다.</summary>
        private int InsertByDistance(Transform target, float sqrDistance, int count)
        {
            int max = _nextSlots.Length;
            int index = count;
            while (index > 0 && _candidateSqrDistances[index - 1] > sqrDistance)
                index--;
            if (index >= max)
                return count;

            int last = Mathf.Min(count, max - 1);
            for (int i = last; i > index; i--)
            {
                _nextSlots[i] = _nextSlots[i - 1];
                _candidateSqrDistances[i] = _candidateSqrDistances[i - 1];
            }

            _nextSlots[index] = CreateSlotCarryingState(target);
            _candidateSqrDistances[index] = sqrDistance;
            return Mathf.Min(count + 1, max);
        }

        private Slot CreateSlotCarryingState(Transform target)
        {
            for (int i = 0; i < _slotCount; i++)
            {
                if (_slots[i].Target == target)
                    return _slots[i];
            }

            return new Slot { Target = target, LastPosition = target.position };
        }

        private void UpdateSlots(float deltaTime)
        {
            // 히트스톱 등으로 시간이 멈춘 프레임은 속도를 0으로 오판하지 않도록 건너뛴다.
            if (deltaTime <= 0.0001f)
                return;

            int writeIndex = 0;
            for (int i = 0; i < _slotCount; i++)
            {
                Slot slot = _slots[i];
                if (slot.Target == null || !slot.Target.gameObject.activeInHierarchy)
                    continue;

                Vector3 position = slot.Target.position;
                Vector3 delta = position - slot.LastPosition;
                delta.y = 0f;
                float speed = delta.magnitude / deltaTime;
                float targetStrength = Mathf.Clamp01(_settings.strengthBySpeed.Evaluate(speed));

                slot.Strength = Mathf.SmoothDamp(
                    slot.Strength, targetStrength, ref slot.StrengthVelocity, _settings.strengthSmoothTime, Mathf.Infinity, deltaTime);
                slot.LastPosition = position;
                _slots[writeIndex++] = slot;
            }
            _slotCount = writeIndex;
        }

        private void PublishToShaders()
        {
            float radius = _settings != null ? _settings.influenceRadius : 0f;
            for (int i = 0; i < _slotCount; i++)
            {
                Vector3 position = _slots[i].LastPosition;
                _actorPositions[i] = new Vector4(position.x, position.y, position.z, radius);
                _actorParams[i] = new Vector4(_slots[i].Strength, 0f, 0f, 0f);
            }

            Shader.SetGlobalVectorArray(ActorsId, _actorPositions);
            Shader.SetGlobalVectorArray(ActorParamsId, _actorParams);
            Shader.SetGlobalFloat(ActorCountId, _slotCount);
            if (_settings != null)
                Shader.SetGlobalVector(BendId, new Vector4(_settings.maxBendDistance, _settings.bendDropRatio, 0f, 0f));
        }

        private void ClearSlots()
        {
            _slotCount = 0;
        }
    }
}
