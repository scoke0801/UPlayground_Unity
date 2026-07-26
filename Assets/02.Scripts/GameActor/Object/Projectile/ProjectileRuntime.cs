using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Projectile;
using UPlayGround.Debugging;

namespace UPlayGround
{
    /// <summary>
    /// Definition의 이동 전략과 동작 모듈을 실행하는 단일 풀링 런타임.
    /// 개별 Update를 갖지 않고 ProjectileManager가 Tick한다.
    /// </summary>
    public sealed class ProjectileRuntime : MonoBehaviour, IDebugGizmoProvider
    {
        private const int HitBufferSize = 32;
        private static readonly RaycastHit[] SweepHits = new RaycastHit[HitBufferSize];
        private static readonly Collider[] OverlapHits = new Collider[HitBufferSize];

        private readonly HashSet<IDamageable> _hitTargets = new();
        private readonly Dictionary<IDamageable, float> _areaCooldowns = new();
        private readonly List<IDamageable> _cooldownKeys = new(32);
        private readonly List<TrailRenderer> _trails = new();
        private readonly List<ParticleSystem> _particles = new();
        private readonly List<AudioSource> _audioSources = new();
        private readonly List<Renderer> _modelRenderers = new();

        private ProjectileDefinitionSO _definition;
        private ProjectileSpawnRequest _request;
        private AttackData _attackData;
        private GameActor _owner;
        private Action<ProjectileRuntime, bool> _return;
        private Action<ProjectileSpawnRequest, AttackData> _spawnChild;
        private Vector3 _direction;
        private Vector3 _previousPosition;
        private Vector3 _startPosition;
        private Vector3 _logicalPosition;
        private Vector3 _barrelOffset;
        private Vector3 _initialScale;
        private Quaternion _initialRotation;
        private float _elapsed;
        private float _speed;
        private float _arcDuration;
        private float _orbitAngle;
        private float _areaRadius;
        private float _currentDamageScale;
        private int _pierceCount;
        private int _bounceCount;
        private bool _active;
        private bool _hitscanCommitted;
        private bool _attached;
        private bool _returning;
        private bool _movementInterrupted;
        private bool _pendingExpired;
        private bool _impactFeedbackApplied;
        private bool _areaAppliedOnce;
        private float _returnDelay;

        public ProjectileDefinitionSO Definition => _definition;
        public bool IsActive => _active;
        public float SpawnTime { get; private set; }
        public GameActor Owner => _owner;
        public int HitTargetCount => _hitTargets.Count;
        public bool IsReset => !_active && _definition == null && _owner == null && _attackData == null;

        public DebugGizmoCategory Category => DebugGizmoCategory.Projectile;
        public DebugGizmoContentType ContentType => DebugGizmoContentType.Projectile;
        public UnityEngine.Object OwnerObject => gameObject;
        UnityEngine.Object IDebugGizmoProvider.Owner => gameObject;
        public bool IsAvailable => _active;

        private void Awake()
        {
            _initialScale = transform.localScale;
            _initialRotation = transform.localRotation;
            GetComponentsInChildren(true, _trails);
            GetComponentsInChildren(true, _particles);
            GetComponentsInChildren(true, _audioSources);
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] is not TrailRenderer
                    && renderers[i] is not ParticleSystemRenderer)
                    _modelRenderers.Add(renderers[i]);
        }

        public void Initialize(
            ProjectileDefinitionSO definition,
            in ProjectileSpawnRequest request,
            AttackData attackData,
            Action<ProjectileRuntime, bool> returnHandler,
            Action<ProjectileSpawnRequest, AttackData> spawnChildHandler)
        {
            _definition = definition;
            _request = request;
            _attackData = attackData ?? new AttackData { damage = request.legacyDamage };
            _owner = request.owner != null ? request.owner.GetComponent<GameActor>() : null;
            _return = returnHandler;
            _spawnChild = spawnChildHandler;
            _direction = request.direction.sqrMagnitude > 0.0001f
                ? request.direction.normalized
                : Vector3.forward;
            _logicalPosition = request.barrelBlendTime > 0f ? request.logicalOrigin : request.origin;
            _barrelOffset = request.origin - _logicalPosition;
            _previousPosition = _logicalPosition;
            _startPosition = _logicalPosition;
            _elapsed = 0f;
            _speed = ResolveInitialSpeed();
            _arcDuration = ResolveArcDuration();
            _orbitAngle = 0f;
            _areaRadius = 0f;
            _currentDamageScale = 1f;
            _pierceCount = 0;
            _bounceCount = 0;
            _hitscanCommitted = false;
            _attached = false;
            _returning = false;
            _impactFeedbackApplied = false;
            _areaAppliedOnce = false;
            _returnDelay = 0f;
            _active = true;
            SpawnTime = Time.unscaledTime;
            _hitTargets.Clear();
            _areaCooldowns.Clear();

            transform.SetParent(null, true);
            transform.SetPositionAndRotation(request.origin, Quaternion.LookRotation(_direction));
            transform.localScale = _initialScale;
            gameObject.SetActive(true);
            AttachStationaryMotionToGround();

            _attackData.attacker = _owner;
            _attackData.isProjectile = true;
            _attackData.isReflectableProjectile =
                definition.GetBehavior<ReflectableProjectileBehavior>() != null;
            if (!string.IsNullOrWhiteSpace(request.hitEffectOverride))
                _attackData.hitParticleName = request.hitEffectOverride;
            else if (!string.IsNullOrWhiteSpace(definition.hitEffectKey))
                _attackData.hitParticleName = definition.hitEffectKey;

            for (int i = 0; i < _trails.Count; i++)
            {
                _trails[i].Clear();
                _trails[i].emitting = true;
            }
            for (int i = 0; i < _particles.Count; i++)
                _particles[i].Play(true);
            for (int i = 0; i < _modelRenderers.Count; i++)
                _modelRenderers[i].enabled = true;
            DebugGizmoBridge.RegisterProvider(this);
        }

        public void Tick(float globalDeltaTime)
        {
            if (_returning)
            {
                _returnDelay -= globalDeltaTime;
                if (_returnDelay <= 0f)
                    CompleteReturn();
                return;
            }
            if (!_active)
                return;

            float deltaTime = ResolveDeltaTime(globalDeltaTime);
            if (deltaTime <= 0f)
                return;

            _elapsed += deltaTime;
            UpdateAreaCooldowns(deltaTime);
            TickAreaBehavior(deltaTime);

            if (!_attached)
                TickMotion(deltaTime);

            float lifetime = _request.durationOverride > 0f
                ? _request.durationOverride
                : _definition.lifetime;
            if (_active && _elapsed >= Mathf.Max(0.01f, lifetime))
                Expire();
        }

        public bool TryReflect(GameActor newOwner, Vector3 direction)
        {
            ReflectableProjectileBehavior reflect =
                _definition?.GetBehavior<ReflectableProjectileBehavior>();
            if (!_active || reflect == null || newOwner == null)
                return false;

            _owner = newOwner;
            _request.owner = newOwner.gameObject;
            _request.targetTransform = null;
            _request.orbitCenter = null;
            LayerMask reflectedHitLayers = newOwner.GetAttackTargetLayerMask();
            if (reflectedHitLayers.value != 0)
                _request.hitLayers = reflectedHitLayers;
            _direction = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : -_direction;
            _speed *= Mathf.Max(0f, reflect.reflectedSpeedMultiplier);
            _attackData.attacker = newOwner;
            _attackData.attackDirection = _direction;
            _attackData.hitTarget = null;
            _hitTargets.Clear();
            _attached = false;
            _movementInterrupted = true;
            transform.SetParent(null, true);
            transform.rotation = Quaternion.LookRotation(_direction);
            return true;
        }

        public void ForceReturn()
        {
            if (_returning)
            {
                _returnDelay = 0f;
                CompleteReturn();
                return;
            }
            ReturnToPool(false, true);
        }

        private float ResolveDeltaTime(float globalDeltaTime)
        {
            if (!_definition.inheritOwnerTimeScale || _owner == null)
                return globalDeltaTime;
            if (_owner is IDamageable damageable && !damageable.IsAlive())
                return globalDeltaTime;
            return globalDeltaTime * _owner.LocalTimeScale;
        }

        private float ResolveInitialSpeed()
        {
            if (_request.speedOverride > 0f)
                return _request.speedOverride;
            return _definition.motion switch
            {
                LinearProjectileMotion linear => linear.speed,
                ArcProjectileMotion arc => arc.speed,
                HomingProjectileMotion homing => homing.speed,
                _ => 0f,
            };
        }

        private float ResolveArcDuration()
        {
            if (_definition.motion is not ArcProjectileMotion arc)
                return 0f;
            if (arc.flightTimeMode == ProjectileArcFlightTimeMode.Fixed)
                return Mathf.Max(0.01f, arc.fixedFlightTime);
            Vector3 target = _request.hasTargetPosition
                ? _request.targetPosition
                : _request.origin + _request.direction * Mathf.Max(1f, _speed);
            return Mathf.Max(0.01f, Vector3.Distance(_request.origin, target) / Mathf.Max(0.01f, _speed));
        }

        private void TickMotion(float deltaTime)
        {
            _previousPosition = _logicalPosition;
            Vector3 nextPosition = _previousPosition;

            switch (_definition.motion)
            {
                case LinearProjectileMotion linear:
                    _speed = Mathf.Min(
                        _speed + linear.acceleration * deltaTime,
                        linear.maxSpeed > 0f ? linear.maxSpeed : float.MaxValue);
                    float ratio = Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, _definition.lifetime));
                    nextPosition += _direction * (_speed * linear.speedCurve.Evaluate(ratio) * deltaTime);
                    break;

                case ArcProjectileMotion arc:
                    Vector3 target = _request.hasTargetPosition
                        ? _request.targetPosition
                        : _startPosition + _direction * (_speed * _arcDuration);
                    float t = Mathf.Clamp01(_elapsed / _arcDuration);
                    float curved = arc.progressCurve.Evaluate(t);
                    nextPosition = Vector3.Lerp(_startPosition, target, curved)
                                   + Vector3.up * (4f * curved * (1f - curved) * arc.arcHeight);
                    break;

                case HomingProjectileMotion homing:
                    if (_elapsed >= homing.activationDelay && _request.targetTransform != null)
                    {
                        Vector3 desired = (_request.targetTransform.position - _logicalPosition).normalized;
                        if (Vector3.Angle(_direction, desired) <= homing.maxTrackAngle)
                        {
                            float strength = homing.strengthCurve.Evaluate(
                                Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, _definition.lifetime)));
                            _direction = Vector3.RotateTowards(
                                _direction,
                                desired,
                                homing.turnRate * Mathf.Deg2Rad * strength * deltaTime,
                                0f).normalized;
                        }
                    }
                    nextPosition += _direction * (_speed * deltaTime);
                    break;

                case OrbitProjectileMotion orbit:
                    Transform center = _request.orbitCenter != null
                        ? _request.orbitCenter
                        : _request.owner != null ? _request.owner.transform : null;
                    if (center != null)
                    {
                        _orbitAngle += orbit.angularSpeed * deltaTime;
                        nextPosition = center.position
                                       + Quaternion.AngleAxis(_orbitAngle, Vector3.up)
                                       * Vector3.forward * orbit.radius;
                    }
                    break;

                case HitscanProjectileMotion hitscan:
                    if (!_hitscanCommitted)
                    {
                        _hitscanCommitted = true;
                        nextPosition = _startPosition + _direction * hitscan.range;
                    }
                    break;
            }

            Vector3 velocity = nextPosition - _previousPosition;
            if (velocity.sqrMagnitude > 0.000001f)
            {
                _direction = velocity.normalized;
                transform.rotation = Quaternion.LookRotation(_direction);
                SweepAndMove(_previousPosition, nextPosition);
            }

            if (_definition.motion is HitscanProjectileMotion scan
                && _elapsed >= Mathf.Max(0.01f, scan.visualDuration))
                Expire();
            else if (_active
                     && !_attached
                     && _definition.motion is ArcProjectileMotion
                     && _elapsed >= _arcDuration)
                Expire();
        }

        private void SweepAndMove(Vector3 from, Vector3 to)
        {
            _movementInterrupted = false;
            float distance = Vector3.Distance(from, to);
            float maxStep = Mathf.Max(0.01f, _definition.collisionRadius * 0.75f);
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / maxStep));
            Vector3 stepStart = from;

            for (int step = 1; step <= steps && _active; step++)
            {
                Vector3 stepEnd = Vector3.Lerp(from, to, step / (float)steps);
                Vector3 move = stepEnd - stepStart;
                float stepDistance = move.magnitude;
                if (stepDistance <= 0.00001f)
                    continue;

                int count = Physics.SphereCastNonAlloc(
                    stepStart,
                    _definition.collisionRadius,
                    move / stepDistance,
                    SweepHits,
                    stepDistance,
                    _request.hitLayers,
                    QueryTriggerInteraction.Collide);
                Array.Sort(SweepHits, 0, count, RaycastHitDistanceComparer.Instance);
                for (int i = 0; i < count && _active; i++)
                {
                    ProcessSweepHit(SweepHits[i]);
                    if (_movementInterrupted)
                        break;
                }

                stepStart = stepEnd;
                if (_movementInterrupted)
                    break;
            }

            if (_active && !_attached && !_movementInterrupted)
            {
                _logicalPosition = to;
                float blend = _request.barrelBlendTime > 0f
                    ? Mathf.Clamp01(_elapsed / _request.barrelBlendTime)
                    : 1f;
                transform.position = to + _barrelOffset * (1f - blend);
            }
        }

        private void ProcessSweepHit(RaycastHit hit)
        {
            if (hit.collider == null || IsOwnerCollider(hit.collider))
                return;

            IDamageable damageable = hit.collider.GetComponent<IDamageable>()
                                     ?? hit.collider.GetComponentInParent<IDamageable>();
            if (damageable == null)
            {
                if (TryBounce(hit))
                    return;
                HandleTerminalHit(hit.point, hit.collider.transform);
                return;
            }
            if (_hitTargets.Contains(damageable) || !CanDeliver(damageable))
                return;

            GameObject hitObject = hit.collider.gameObject;
            AttackData hitAttack = PlayerAttackController.Copy(_attackData);
            hitAttack.damage *= _currentDamageScale;
            hitAttack.poiseDamage *= _currentDamageScale;
            hitAttack.breakDamage *= _currentDamageScale;
            hitAttack.hitTarget = hitObject;
            hitAttack.hitPoint = hit.point;
            hitAttack.attackDirection = _direction;
            hitAttack.attacker = _owner;

            CombatResult result = damageable.ReceiveHit(HitRequest.FromAttackData(hitAttack));
            if (result.DefenseOutcome == DefenseOutcome.Parried
                && TryReflect(result.Victim, -_direction))
                return;

            _hitTargets.Add(damageable);
            ApplyPlayerFeedback(result, hitAttack);
            NotifyHit();

            PierceProjectileBehavior pierce = _definition.GetBehavior<PierceProjectileBehavior>();
            if (pierce != null && _pierceCount < pierce.maxPierce)
            {
                _pierceCount++;
                _currentDamageScale *= pierce.damageMultiplierPerPierce;
                return;
            }

            HandleTerminalHit(hit.point, hit.collider.transform);
        }

        private bool TryBounce(RaycastHit hit)
        {
            BounceProjectileBehavior bounce = _definition.GetBehavior<BounceProjectileBehavior>();
            if (bounce == null || _bounceCount >= bounce.maxBounce)
                return false;
            if ((bounce.surfaceLayers.value & (1 << hit.collider.gameObject.layer)) == 0)
                return false;

            _bounceCount++;
            _direction = Vector3.Reflect(_direction, hit.normal).normalized;
            _speed *= bounce.speedRetention;
            _logicalPosition = hit.point + hit.normal * (_definition.collisionRadius + 0.001f);
            transform.position = _logicalPosition;
            transform.rotation = Quaternion.LookRotation(_direction);
            _movementInterrupted = true;
            return true;
        }

        private void HandleTerminalHit(Vector3 point, Transform target)
        {
            TriggerSplit(ProjectileSplitTrigger.Hit);
            TriggerDetonate(true);
            ShowHitEffect();

            AttachProjectileBehavior attach = _definition.GetBehavior<AttachProjectileBehavior>();
            if (attach != null)
            {
                _attached = true;
                _movementInterrupted = true;
                _logicalPosition = point;
                transform.position = point;
                if (attach.attachToTarget && target != null)
                    transform.SetParent(target, true);
                if (attach.attachedLifetime > 0f)
                    _elapsed = Mathf.Max(0f, _definition.lifetime - attach.attachedLifetime);
                return;
            }

            if (_definition.destroyOnHit)
                ReturnToPool(false);
        }

        private void TickAreaBehavior(float deltaTime)
        {
            AreaTickProjectileBehavior area = _definition.GetBehavior<AreaTickProjectileBehavior>();
            if (area == null
                || _elapsed < area.activationDelay
                || (area.applyOnce && _areaAppliedOnce))
                return;

            _areaRadius = area.expandOverTime
                ? Mathf.Min(area.radius, _areaRadius + area.expansionSpeed * deltaTime)
                : area.radius;
            ApplyAreaDamage(_areaRadius, area.damageFalloff, area.interval);
            _areaAppliedOnce = true;
        }

        private void ApplyAreaDamage(float radius, AnimationCurve falloff, float cooldown)
        {
            if (radius <= 0f)
                return;

            int count = Physics.OverlapSphereNonAlloc(
                transform.position,
                radius,
                OverlapHits,
                _request.hitLayers,
                QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                Collider collider = OverlapHits[i];
                if (collider == null || IsOwnerCollider(collider))
                    continue;
                IDamageable damageable = collider.GetComponent<IDamageable>()
                                         ?? collider.GetComponentInParent<IDamageable>();
                if (damageable == null || _areaCooldowns.ContainsKey(damageable) || !CanDeliver(damageable))
                    continue;

                float normalized = Mathf.Clamp01(
                    Vector3.Distance(transform.position, collider.transform.position) / radius);
                float multiplier = falloff != null ? falloff.Evaluate(normalized) : 1f;
                AttackData areaAttack = PlayerAttackController.Copy(_attackData);
                areaAttack.damage *= _currentDamageScale * multiplier;
                areaAttack.poiseDamage *= _currentDamageScale * multiplier;
                areaAttack.breakDamage *= _currentDamageScale * multiplier;
                areaAttack.hitTarget = collider.gameObject;
                areaAttack.hitPoint = collider.ClosestPoint(transform.position);
                areaAttack.attackDirection =
                    (collider.transform.position - transform.position).normalized;
                CombatResult result = damageable.ReceiveHit(HitRequest.FromAttackData(areaAttack));
                _areaCooldowns[damageable] = Mathf.Max(0.01f, cooldown);
                ApplyPlayerFeedback(result, areaAttack);
                NotifyHit();
            }
        }

        private bool TriggerDetonate(bool onHit)
        {
            DetonateProjectileBehavior detonate =
                _definition.GetBehavior<DetonateProjectileBehavior>();
            if (detonate == null || (onHit ? !detonate.onHit : !detonate.onExpire))
                return false;
            ApplyAreaDamage(detonate.radius, detonate.damageFalloff, float.MaxValue);
            return true;
        }

        private void TriggerSplit(ProjectileSplitTrigger trigger)
        {
            SplitProjectileBehavior split = _definition.GetBehavior<SplitProjectileBehavior>();
            if (split == null || split.childDefinition == null)
                return;
            if (split.trigger != trigger && split.trigger != ProjectileSplitTrigger.HitOrExpire)
                return;
            if (_request.generation >= _definition.maxGeneration)
            {
                Debug.LogWarning($"[ProjectileRuntime] 분열 세대 상한 도달: {_definition.name}", this);
                return;
            }

            int count = Mathf.Max(1, split.count);
            for (int i = 0; i < count; i++)
            {
                float ratio = count == 1 ? 0.5f : i / (float)(count - 1);
                float angle = Mathf.Lerp(-split.spreadAngle * 0.5f, split.spreadAngle * 0.5f, ratio);
                ProjectileSpawnRequest child = _request;
                child.definition = split.childDefinition;
                child.origin = transform.position;
                child.direction = Quaternion.AngleAxis(angle, Vector3.up) * _direction;
                child.damageScale = 1f;
                child.generation = _request.generation + 1;
                child.delay = 0f;
                AttackData childAttack = PlayerAttackController.Copy(_attackData);
                float childScale = _currentDamageScale * split.damageScale;
                childAttack.damage *= childScale;
                childAttack.poiseDamage *= childScale;
                childAttack.breakDamage *= childScale;
                _spawnChild?.Invoke(child, childAttack);
            }
        }

        private void Expire()
        {
            TriggerSplit(ProjectileSplitTrigger.Expire);
            if (TriggerDetonate(false))
                ShowHitEffect();
            ReturnToPool(true);
        }

        private void ReturnToPool(bool expired, bool forceImmediate = false)
        {
            if (_returning)
                return;
            _returning = true;
            _active = false;
            DebugGizmoBridge.UnregisterProvider(this);
            _pendingExpired = expired;
            _returnDelay = 0f;
            if (!forceImmediate && _definition != null && _definition.detachTrailOnReturn)
            {
                for (int i = 0; i < _modelRenderers.Count; i++)
                    _modelRenderers[i].enabled = false;
                for (int i = 0; i < _trails.Count; i++)
                {
                    _trails[i].emitting = false;
                    _returnDelay = Mathf.Max(_returnDelay, _trails[i].time);
                }
            }
            if (_returnDelay <= 0f)
                CompleteReturn();
        }

        private void CompleteReturn()
        {
            Action<ProjectileRuntime, bool> handler = _return;
            bool expired = _pendingExpired;
            _returnDelay = 0f;
            handler?.Invoke(this, expired);
        }

        public void OnReturnedToPool()
        {
            CancelInvoke();
            StopAllCoroutines();
            transform.SetParent(null, true);
            transform.localScale = _initialScale;
            transform.localRotation = _initialRotation;
            for (int i = 0; i < _trails.Count; i++)
            {
                _trails[i].emitting = false;
                _trails[i].Clear();
            }
            for (int i = 0; i < _particles.Count; i++)
                _particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            for (int i = 0; i < _audioSources.Count; i++)
                _audioSources[i].Stop();
            for (int i = 0; i < _modelRenderers.Count; i++)
                _modelRenderers[i].enabled = true;
            _hitTargets.Clear();
            _areaCooldowns.Clear();
            _definition = null;
            _attackData = null;
            _owner = null;
            _return = null;
            _spawnChild = null;
            _active = false;
            _returning = false;
            _impactFeedbackApplied = false;
            gameObject.SetActive(false);
        }

        private bool IsOwnerCollider(Collider collider) =>
            collider.transform == transform
            || collider.transform.IsChildOf(transform)
            || (_owner != null
                && (collider.gameObject == _owner.gameObject
                    || collider.transform.IsChildOf(_owner.transform)));

        private bool CanDeliver(IDamageable damageable)
        {
            if (_owner == null)
                return damageable.CanTakeDamage();
            return _owner.HasActorType(ActorType.Monster)
                ? damageable.IsAlive()
                : damageable.CanTakeDamage();
        }

        private void ApplyPlayerFeedback(CombatResult result, AttackData attack)
        {
            if (_owner is not PlayerActor player)
                return;
            PlayerCombat combat = player.GetCombat();
            if (combat == null)
                return;
            combat.ShowExternalHitFeedback(result);
            if (!_impactFeedbackApplied)
            {
                combat.ApplyExternalAttackImpact(attack);
                _impactFeedbackApplied = true;
            }
            combat.NotifyAttackHit(attack);
        }

        private void AttachStationaryMotionToGround()
        {
            if (_definition.motion is not StationaryProjectileMotion stationary
                || !stationary.attachToGround)
                return;

            Vector3 rayOrigin = _logicalPosition
                                + Vector3.up * Mathf.Max(0f, stationary.groundProbeHeight);
            float distance = Mathf.Max(
                0.01f,
                stationary.groundProbeHeight + stationary.groundProbeDistance);
            if (!Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out RaycastHit hit,
                    distance,
                    stationary.groundLayers,
                    QueryTriggerInteraction.Ignore))
                return;

            _logicalPosition = hit.point;
            _previousPosition = hit.point;
            _startPosition = hit.point;
            transform.SetPositionAndRotation(
                hit.point,
                Quaternion.FromToRotation(Vector3.up, hit.normal));
        }

        private void ShowHitEffect()
        {
            string key = !string.IsNullOrWhiteSpace(_attackData?.hitParticleName)
                ? _attackData.hitParticleName
                : _definition?.hitEffectKey;
            if (!string.IsNullOrWhiteSpace(key))
                Manager.ActorSvc.Objects?.ShowFX(key, transform.position);
        }

        private void NotifyHit() => ProjectileRuntimeTelemetry.Hit?.Invoke(this);

        private void UpdateAreaCooldowns(float deltaTime)
        {
            _cooldownKeys.Clear();
            foreach (IDamageable key in _areaCooldowns.Keys)
                _cooldownKeys.Add(key);
            for (int i = 0; i < _cooldownKeys.Count; i++)
            {
                IDamageable key = _cooldownKeys[i];
                float remaining = _areaCooldowns[key] - deltaTime;
                if (remaining <= 0f)
                    _areaCooldowns.Remove(key);
                else
                    _areaCooldowns[key] = remaining;
            }
        }

        public void CollectSnapshot(DebugGizmoFrameSnapshot snapshot) { }

        public void DrawGizmos(DebugGizmoDrawContext context)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _definition != null ? _definition.collisionRadius : 0.1f);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(_previousPosition, transform.position);
            if (_areaRadius > 0f)
                context.DrawWireDisc(transform.position, _areaRadius, new Color(1f, 0.2f, 0.1f));
            context.DrawLabel(transform.position, _definition != null ? _definition.name : "Projectile");
        }

        private sealed class RaycastHitDistanceComparer : IComparer<RaycastHit>
        {
            public static readonly RaycastHitDistanceComparer Instance = new();
            public int Compare(RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance);
        }
    }

    public static class ProjectileRuntimeTelemetry
    {
        public static Action<ProjectileRuntime> Hit;
    }
}
