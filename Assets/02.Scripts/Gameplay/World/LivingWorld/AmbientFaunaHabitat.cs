using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Manager;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.World;

namespace UPlayGround.Gameplay.World.LivingWorld
{
    /// <summary>GPU 군집의 월드 입력·전환 중 용량·공격 자극을 관리하며 개체 상태는 읽지 않는다.</summary>
    [DisallowMultipleComponent]
    public sealed class AmbientFaunaHabitat : MonoBehaviour
    {
        public const int InfluenceCapacity = 8;
        private static readonly List<AmbientFaunaHabitat> Hosts = new();
        [SerializeField] private EnvironmentController _environment;
        [SerializeField] private Transform _observerOverride;
        [SerializeField, Range(0, 24)] private float _previewHour = 12;
        [SerializeField, Range(0, 1)] private float _previewRain, _previewSnow;
        [SerializeField, Min(1)] private int _maxCapacity = 4096, _maxGroups = 32;
        [SerializeField, Min(0)] private float _attackRadius = 5, _attackStrength = 3, _attackDuration = 0.8f;
        [SerializeField, Min(1)] private float _teleportDistance = 20;
        private readonly List<AmbientFaunaGroup> _groups = new();
        private readonly Dictionary<AmbientFaunaGroup, int> _reservations = new();
        private readonly Vector4[] _influences = new Vector4[InfluenceCapacity];
        private readonly Vector4[] _strengths = new Vector4[InfluenceCapacity];
        private readonly int[] _sequences = new int[InfluenceCapacity];
        private PlayerActor _player;
        private PlayerCombat _combat;
        private Vector3 _previousObserver;
        private bool _hadObserver;
        private int _attackSequence;
        private float _hour, _rain, _snow;
        public Vector3 ObserverVelocity { get; private set; }
        public int AllocatedCapacity { get; private set; }
        public int AllocatedGroups => _reservations.Count;
        public int DroppedStimuli { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetHosts() => Hosts.Clear();

        /// <summary>준비된 그룹을 가장 가까운 서식지에 중복 없이 등록한다.</summary>
        public bool TryRegister(AmbientFaunaGroup group)
        {
            if (group == null || !group.IsReady || !group.isActiveAndEnabled
                || group.GetComponentInParent<AmbientFaunaHabitat>(true) != this) return false;
            if (!_groups.Contains(group)) _groups.Add(group);
            if (!Hosts.Contains(this)) Hosts.Add(this);
            if (!AmbientFaunaRendererFeature.Groups.Contains(group)) AmbientFaunaRendererFeature.Groups.Add(group);
            group.SetSuspended(!isActiveAndEnabled);
            return true;
        }

        /// <summary>비활성 그룹과 남은 용량 예약을 해제한다.</summary>
        public void Unregister(AmbientFaunaGroup group)
        {
            ReleaseReservation(group);
            _groups.Remove(group);
            AmbientFaunaRendererFeature.Groups.Remove(group);
            if (_groups.Count == 0) Hosts.Remove(this);
        }

        /// <summary>퇴장 중 그룹까지 합산해 GPU 할당 전에 예산을 예약한다.</summary>
        public bool TryReserve(AmbientFaunaGroup group, int capacity)
        {
            if (!isActiveAndEnabled || group == null || !_groups.Contains(group) || capacity < 1) return false;
            if (_reservations.TryGetValue(group, out int current)) return current == capacity;
            ReadGlobalBudget(out int allocated, out int groups, out int maxCapacity, out int maxGroups);
            if (groups >= maxGroups || capacity > maxCapacity - allocated) return false;
            _reservations.Add(group, capacity);
            AllocatedCapacity += capacity;
            return true;
        }

        private static void ReadGlobalBudget(out int allocated, out int groups, out int maxCapacity, out int maxGroups)
        {
            allocated = groups = 0;
            maxCapacity = maxGroups = int.MaxValue;
            for (int i = 0; i < Hosts.Count; i++)
            {
                var host = Hosts[i];
                if (host == null || !host.isActiveAndEnabled || host._groups.Count == 0) continue;
                allocated += host.AllocatedCapacity;
                groups += host.AllocatedGroups;
                // 중첩 월드/셀의 품질 설정이 다르면 더 낮은 한도를 공동 적용한다.
                maxCapacity = Mathf.Min(maxCapacity, Mathf.Max(0, host._maxCapacity));
                maxGroups = Mathf.Min(maxGroups, Mathf.Max(0, host._maxGroups));
            }
        }

        private static void EnforceGlobalBudget()
        {
            ReadGlobalBudget(out int allocated, out int groups, out int maxCapacity, out int maxGroups);
            var registered = AmbientFaunaRendererFeature.Groups;
            for (int i = registered.Count - 1; i >= 0 && (allocated > maxCapacity || groups > maxGroups); i--)
            {
                var group = registered[i];
                if (group == null || group.AllocatedCapacity == 0) continue;
                group.ReleaseForBudget();
                ReadGlobalBudget(out allocated, out groups, out maxCapacity, out maxGroups);
            }
        }

        /// <summary>실제 버퍼 해제와 같은 시점에 예약을 반환한다.</summary>
        public void ReleaseReservation(AmbientFaunaGroup group)
        {
            if (group == null || !_reservations.Remove(group, out int capacity)) return;
            AllocatedCapacity -= capacity;
        }

        /// <summary>현재 월드 시각과 강수로 공유 종 프로필의 표시 밀도를 계산한다.</summary>
        public float EvaluateDensity(AmbientFaunaProfileSO profile) => profile.EvaluateDensity(_hour, _rain, _snow);

        /// <summary>시간·날씨 서비스가 없는 독립 검증 씬의 입력을 설정한다.</summary>
        public void SetPreviewEnvironment(float hour, float rain, float snow)
        {
            if (!AmbientFaunaProfileSO.IsFinite(hour) || !AmbientFaunaProfileSO.IsFinite(rain)
                || !AmbientFaunaProfileSO.IsFinite(snow)) return;
            _previewHour = Mathf.Repeat(hour, 24);
            _previewRain = Mathf.Clamp01(rain);
            _previewSnow = Mathf.Clamp01(snow);
        }

        /// <summary>자극을 고정 슬롯에 저장하고 살아 있는 동일 sequence의 중복을 거절한다.</summary>
        public bool EmitStimulus(Vector3 position, float radius, float strength, float seconds, int sequence)
        {
            if (!AmbientFaunaProfileSO.IsFinite(position.x) || !AmbientFaunaProfileSO.IsFinite(position.y)
                || !AmbientFaunaProfileSO.IsFinite(position.z) || !AmbientFaunaProfileSO.IsFinite(radius) || radius <= 0
                || !AmbientFaunaProfileSO.IsFinite(strength) || strength <= 0
                || !AmbientFaunaProfileSO.IsFinite(seconds) || seconds <= 0) return false;
            int slot = -1;
            for (int i = 0; i < InfluenceCapacity; i++)
            {
                if (_strengths[i].y > 0 && _sequences[i] == sequence) return false;
                if (slot < 0 && _strengths[i].y <= 0) slot = i;
            }
            if (slot < 0) { DroppedStimuli++; return false; }
            _influences[slot] = new Vector4(position.x, position.y, position.z, radius);
            _strengths[slot] = new Vector4(strength, seconds, seconds, 0);
            _sequences[slot] = sequence;
            return true;
        }

        /// <summary>개체 수와 무관한 상한 8개의 자극만 그룹 로컬 좌표로 복사한다.</summary>
        public int CopyInfluences(Transform group, Vector4[] positions, Vector4[] strengths)
        {
            int count = 0;
            for (int i = 0; i < InfluenceCapacity; i++)
            {
                if (_strengths[i].y <= 0) continue;
                Vector4 influence = _influences[i];
                Vector3 local = group.InverseTransformPoint(new Vector3(influence.x, influence.y, influence.z));
                positions[count] = new Vector4(local.x, local.y, local.z, influence.w);
                strengths[count] = new Vector4(_strengths[i].x * _strengths[i].y / _strengths[i].z, 0, 0, 0);
                count++;
            }
            return count;
        }

        private void OnEnable()
        {
            if (_groups.Count > 0 && !Hosts.Contains(this)) Hosts.Add(this);
            _hadObserver = false;
            for (int i = 0; i < _groups.Count; i++) _groups[i].SetSuspended(false);
        }

        private void OnDisable()
        {
            BindCombat(null);
            for (int i = 0; i < _groups.Count; i++) _groups[i].SetSuspended(true);
            System.Array.Clear(_strengths, 0, _strengths.Length);
            _hadObserver = false;
            ObserverVelocity = Vector3.zero;
            Hosts.Remove(this);
        }

        private void Update()
        {
            EnforceGlobalBudget();
            Services.TryGet<IActorQueryService>(out var actors);
            BindCombat(actors?.Player as PlayerActor);
            Transform player = _observerOverride != null ? _observerOverride : actors?.PlayerTransform;
            Vector3 observer = player != null ? player.position : default;
            float delta = Time.deltaTime;
            Vector3 displacement = observer - _previousObserver;
            ObserverVelocity = _hadObserver && player != null && delta > 0
                && displacement.sqrMagnitude < _teleportDistance * _teleportDistance ? displacement / delta : Vector3.zero;
            _previousObserver = observer;
            _hadObserver = player != null;
            _hour = Services.TryGet<IGameTimeService>(out var time) ? time.MinuteOfDay / 60 : _previewHour;
            _rain = _previewRain;
            _snow = _previewSnow;
            Vector3 wind = default;
            if (_environment != null && _environment.isActiveAndEnabled && _environment.IsReady)
            {
                Vector2 sample = _environment.Current.Wind;
                wind = new Vector3(sample.x, 0, sample.y);
                _rain = _environment.RainIntensity;
                _snow = _environment.SnowIntensity;
            }
            for (int i = 0; i < InfluenceCapacity; i++) _strengths[i].y = Mathf.Max(0, _strengths[i].y - delta);
            for (int i = 0; i < _groups.Count; i++) _groups[i].ApplyWorldInputs(delta, observer, player != null, wind);
        }

        private void BindCombat(PlayerActor player)
        {
            PlayerCombat combat = player != null ? player.GetCombat() : null;
            if (combat == _combat && player == _player) return;
            if (_combat != null) _combat.OnAttackStarted -= OnPlayerAttack;
            _player = player;
            _combat = combat;
            if (_combat != null) _combat.OnAttackStarted += OnPlayerAttack;
        }

        private void OnPlayerAttack(AttackData attack)
        {
            if (_player != null) EmitStimulus(_player.transform.position, _attackRadius, _attackStrength, _attackDuration, ++_attackSequence);
        }
    }
}
