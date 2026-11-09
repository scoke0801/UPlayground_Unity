using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Manager;
using UPlayGround.CameraSystem;
using UPlayGround.Data.Combat;

namespace UPlayGround.Gameplay.World.LivingWorld
{
    public sealed partial class WildlifeCreature : IDamageable, ICombatAffiliationView, ILockOnTarget
    {
        private float _health, _deathSeconds, _hitThreatSeconds;
        private bool _hasHealth, _isDead;
        private Vector3 _hitThreatPosition;
        private float _deathGroundHeight;
        private Collider _huntCollider;
        [SerializeField, Tooltip("이동 충돌체. 피격 트리거와 별도로 몸의 실제 크기를 지정합니다.")]
        private Collider _bodyCollider;

        public int CombatantRuntimeId => GetInstanceID();
        public string CombatFactionId => CombatFactionRules.WorldHostileId;
        public CombatCreditOwner CombatCreditOwner => CombatCreditOwner.World;
        public bool IsCombatAvailable => CanTakeDamage();
        public Transform Transform => transform;
        public bool CanLockOn => CanTakeDamage();
        public float LockOnPriority => 0;
        private Collider FocusCollider => _bodyCollider != null ? _bodyCollider : _huntCollider;
        public float BoundsSize => FocusCollider != null ? FocusCollider.bounds.size.magnitude : 1;
        public Vector3 FocusPosition => FocusCollider != null ? FocusCollider.bounds.center : transform.position;
        public Vector3 UIAnchorPosition => FocusPosition;

        private void Awake() => _huntCollider = GetComponent<Collider>();

        /// <summary>기존 공격 피해 계산과 처치 보상 경로를 환경 생물에 연결한다.</summary>
        public CombatResult ReceiveHit(in HitRequest request)
        {
            if (!CanTakeDamage()) return default;
            if (request.Attacker != null && !CombatRelationUtility.CanDamage(request.Attacker, this, request.TargetPolicy))
                return default;
            var hit = HitContext.Create(request, null);
            // 방어·브레이크가 없는 생물도 공격력과 치명타는 공용 계산을 따른다.
            var damage = DamageResolver.ResolveMonsterDamage(null, hit, null);
            if (damage.FinalDamage <= 0) return default;
            float applied = Mathf.Min(GetCurrentHealth(), damage.FinalDamage);
            _health -= applied;
            if (_health <= 0) Die();
            else
            {
                Initialize();
                _hitThreatPosition = request.Attacker != null
                    ? request.Attacker.transform.position : transform.position - request.AttackDirection;
                _hitThreatSeconds = _calmTime = _profile.CalmSeconds;
                _activity = Activity.Fleeing;
                _idleTime = 0;
                if (_isInitialized) ChooseEscapeDestination(_hitThreatPosition);
            }
            var result = CombatResult.Build(hit, DefenseResult.None, damage, default,
                ResourceChangeSet.FromDamage(applied));
            if (Services.TryGet<ISoundService>(out _)) CombatFeedbackDispatcher.PlayDamageImpact(result);
            return result;
        }

        /// <summary>사냥이 허용된 살아 있는 생물인지 반환한다.</summary>
        public bool IsAlive() => !_isDead && _profile != null && _profile.MaxHealth > 0;

        /// <summary>비활성 개체와 장식 생물은 피격 대상에서 제외한다.</summary>
        public bool CanTakeDamage() => isActiveAndEnabled && IsAlive();

        /// <summary>피격 지점 계산에 사용할 루트를 반환한다.</summary>
        public Transform GetTransform() => transform;

        /// <summary>환경 생물에는 별도 락온 표시를 띄우지 않는다.</summary>
        public void LockOn() { }

        /// <summary>환경 생물의 락온 표시 해제 계약을 제공한다.</summary>
        public void UnLockOn() { }

        /// <summary>현재 체력 비율을 반환한다.</summary>
        public float GetHealthPercent() => IsAlive() ? GetCurrentHealth() / _profile.MaxHealth : 0;

        /// <summary>프로필이 연결된 뒤 최초 접근에서 체력을 초기화한다.</summary>
        public float GetCurrentHealth()
        {
            if (!_hasHealth && _profile != null)
            {
                _health = _profile.MaxHealth;
                _hasHealth = true;
            }
            return _health;
        }

        /// <summary>살아 있는 생물의 체력을 최대치 안에서 회복한다.</summary>
        public void ApplyHealingEffect(float healAmount)
        {
            if (IsAlive()) _health = Mathf.Min(_profile.MaxHealth, GetCurrentHealth() + Mathf.Max(0, healAmount));
        }

        private void Die()
        {
            // 다단 공격과 중복 콜백이 같은 보상을 다시 지급하지 못하도록 먼저 사망을 확정한다.
            _isDead = true;
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh) _agent.ResetPath();
            if (_agent != null) _agent.enabled = false;
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
            _deathSeconds = Mathf.Max(_profile.DeathRemainsSeconds,
                _profile.Death != null ? _profile.Death.length : 0);
            _deathGroundHeight = transform.position.y;
            if (_profile.IsFlying && Physics.Raycast(transform.position + Vector3.up, Vector3.down,
                out var ground, _profile.LandingProbeDistance, _profile.LandingMask, QueryTriggerInteraction.Ignore))
                _deathGroundHeight = ground.point.y + _profile.GroundClearance;
            if (_animation != null && _animation.Animator != null)
            {
                SetSimulation(true);
                if (_profile.Death != null)
                {
                    var state = _animation.Play(_profile.Death, _profile.CrossFade);
                    state.Speed = 1;
                    state.Events(this).OnEnd = () => state.IsPlaying = false;
                }
                else _animation.enabled = false;
            }
            if (_profile.DropTable != null
                && Services.TryGet<IItemService>(out var items)
                && Services.TryGet<IActorObjectService>(out var objects))
                objects.GrantAndPresentItems(items.GetDropItemList(_profile.DropTable), transform.position);
        }

        private void UpdateDeath(float deltaTime)
        {
            if (deltaTime <= 0) return;
            _deathSeconds -= deltaTime;
            if (_profile.Death == null)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.Euler(0, transform.eulerAngles.y, 90), _profile.TurnSpeed * deltaTime);
                var position = transform.position;
                position.y = Mathf.MoveTowards(position.y, _deathGroundHeight, _profile.FleeSpeed * deltaTime);
                transform.position = position;
            }
            if (_deathSeconds <= 0) gameObject.SetActive(false);
        }
    }
}
