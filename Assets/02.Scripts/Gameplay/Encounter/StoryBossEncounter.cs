using System;
using System.Collections;
using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.InputDefine;
using UPlayGround.CameraSystem;

namespace UPlayGround.Gameplay.Encounter
{
    /// <summary>치명 피해로 확인한 전투 패배를 사망·보상과 분리하고 준비 사실을 보존한다.</summary>
    public sealed class StoryBossEncounter : MonoBehaviour, IMonsterFatalDamagePolicy
    {
        [SerializeField] private string _actorId;
        [SerializeField] private string[] _requiredFlags = Array.Empty<string>();
        [SerializeField] private CharacterActorType[] _requiredCharacters = Array.Empty<CharacterActorType>();
        [SerializeField] private string _acceptedFlag;
        [SerializeField] private string _formShownFlag;
        [SerializeField] private string _victoryFlag;
        [SerializeField] private string _attacksClearedFlag;
        [SerializeField] private string _absorptionStoppedFlag;
        [SerializeField] private Transform _bossStart;
        [SerializeField] private Transform _playerStart;
        [SerializeField] private BoxCollider _arena;
        [SerializeField] private GameObject[] _absorbingVisuals = Array.Empty<GameObject>();
        [SerializeField] private GameObject[] _stoppedVisuals = Array.Empty<GameObject>();
        [Min(0.1f), SerializeField] private float _firstAttackDelay = 1.5f;
        [Min(0.05f), SerializeField] private float _checkInterval = 0.1f;
        private IGlobalFlagService _flags;
        private MonsterActor _boss;
        private IDisposable _hold;
        private IDisposable _fatalPolicy;
        private Coroutine _loop;
        private bool _isFighting;
        private bool _needsReset;
        private bool _returnPlayerToStart;
        private bool _restoreRequested;
        private float _readySince = -1f;

        public MonsterActor Boss => _boss;
        public bool IsFighting => _isFighting;

        private void OnEnable() => _loop = StartCoroutine(Run());

        /// <summary>지정 동료와 각각의 준비 사실·수용 완료를 함께 확인한다.</summary>
        public bool CanBeginBattle()
        {
            if (_flags == null || !_flags.GetFlag(_acceptedFlag) || _flags.GetFlag(_victoryFlag)
                || !Services.TryGet<IPartyService>(out var party)) return false;
            foreach (string key in _requiredFlags) if (!_flags.GetFlag(key)) return false;
            foreach (var character in _requiredCharacters) if (!party.IsCharacterUnlocked(character)) return false;
            return true;
        }

        private IEnumerator Run()
        {
            while (!Services.TryGet(out _flags)) yield return null;
            _flags.OnFlagsReloaded += RequestRestore;
            _restoreRequested = true;
            var wait = new WaitForSecondsRealtime(_checkInterval);
            while (isActiveAndEnabled)
            {
                TickEncounter();
                yield return wait;
            }
        }

        private void TickEncounter()
        {
            var player = Svc.ActorQuery?.Player as PlayerActor;
            if (player == null) return;
            if (_restoreRequested)
            {
                _restoreRequested = false;
                StopBattle();
                _needsReset = true;
                _returnPlayerToStart = !_flags.GetFlag(_victoryFlag) && IsInside(player.transform.position);
                if (!_flags.GetFlag(_acceptedFlag) && _boss != null)
                {
                    _fatalPolicy?.Dispose(); _fatalPolicy = null;
                    Destroy(_boss.gameObject); _boss = null;
                    _hold?.Dispose(); _hold = null;
                }
                if (_flags.GetFlag(_acceptedFlag) && _returnPlayerToStart)
                    player.PlaceAtPose(_playerStart.position, _playerStart.rotation);
            }
            if (_flags.GetFlag(_victoryFlag))
            {
                if (_boss == null && IsInside(player.transform.position)) TrySpawn();
                StopBattle();
                ApplyStoppedPresentation();
                return;
            }
            foreach (var visual in _absorbingVisuals) if (visual != null) visual.SetActive(true);
            foreach (var visual in _stoppedVisuals) if (visual != null) visual.SetActive(false);
            if (!CanBeginBattle()) { StopBattle(); return; }
            if (!player.IsAlive())
            {
                if (_isFighting) { _needsReset = true; _returnPlayerToStart = true; }
                StopBattle();
                return;
            }
            if (_needsReset)
            {
                StopBattle();
                if (_boss != null)
                {
                    _hold?.Dispose(); _hold = null;
                    _boss.RestoreEncounterCombatState();
                    _boss.PlaceAtEncounterAnchor(_bossStart);
                    HoldBattle();
                }
                if (_returnPlayerToStart) player.PlaceAtPose(_playerStart.position, _playerStart.rotation);
                _returnPlayerToStart = false;
                _needsReset = false;
            }
            if (!IsInside(player.transform.position))
            {
                if (_isFighting) _needsReset = true;
                StopBattle();
                return;
            }
            if (_boss != null && !IsInside(_boss.transform.position))
            {
                _needsReset = true;
                StopBattle();
                return;
            }
            if (!HasPlayerControl()) { StopBattle(); return; }
            if (_boss == null && !TrySpawn()) return;
            if (_isFighting) return;
            if (_readySince < 0f) _readySince = Time.unscaledTime;
            if (Time.unscaledTime - _readySince < _firstAttackDelay) return;
            _hold?.Dispose(); _hold = null;
            _isFighting = true;
        }

        private bool HasPlayerControl() => Svc.Dialogue?.IsDialogueActive == false
            && Services.TryGet<IInputService>(out var input)
            && input.CurrentLayer == InputLayer.Level_0 && !input.IsPlayerActionInputSuppressed
            && Services.TryGet<ICameraViewService>(out var camera)
            && camera.GetMainCamera() != null && camera.CurrentCameraMode == CameraModeType.InGame
            && (!(camera is CameraManager manager) || manager.IsSceneCameraReadyFor(Svc.ActorQuery.PlayerTransform));

        private bool TrySpawn()
        {
            if (!Services.TryGet<IActorSpawnService>(out var spawn) || !spawn.IsReady) return false;
            _boss = Svc.ActorQuery?.FindActor(_actorId) as MonsterActor;
            if (_boss == null) _boss = spawn.SpawnActor(_actorId, _bossStart.position, _bossStart.rotation) as MonsterActor;
            if (_boss == null) return false;
            _fatalPolicy = _boss.OverrideFatalDamagePolicy(this);
            HoldBattle();
            if (!string.IsNullOrEmpty(_formShownFlag)) _flags.SetFlag(_formShownFlag, true);
            return true;
        }

        private bool IsInside(Vector3 position) => _arena != null && _arena.bounds.Contains(position + Vector3.up);
        private void HoldBattle() { if (_boss != null && _hold == null) _hold = _boss.HoldStoryCombat(); }
        private void StopBattle()
        {
            _isFighting = false;
            _readySince = -1f;
            HoldBattle();
            if (_boss != null && Services.TryGet<IProjectileService>(out var projectiles))
                projectiles.CancelOwnedBy(_boss.gameObject);
        }

        /// <summary>실제 전투의 치명 피해만 승리로 기록하며 일반 사망·드롭 경로에는 들어가지 않는다.</summary>
        public MonsterFatalDamageResolution ResolveFatalDamage(MonsterActor victim, in HitRequest request,
            float requestedDamage, out float appliedDamage)
        {
            appliedDamage = 0f;
            if (victim != _boss || !_isFighting || !CanBeginBattle()
                || requestedDamage < victim.CurrentHealth) return MonsterFatalDamageResolution.Prevented;
            appliedDamage = Mathf.Max(0f, victim.CurrentHealth - 1f);
            StopBattle();
            _flags.SetFlag(_victoryFlag, true);
            ApplyStoppedPresentation();
            return MonsterFatalDamageResolution.Incapacitated;
        }

        private void ApplyStoppedPresentation()
        {
            _flags.SetFlag(_attacksClearedFlag, true);
            foreach (var visual in _absorbingVisuals) if (visual != null) visual.SetActive(false);
            foreach (var visual in _stoppedVisuals) if (visual != null) visual.SetActive(true);
            _flags.SetFlag(_absorptionStoppedFlag, true);
        }

        private void RequestRestore() { StopBattle(); _restoreRequested = true; }
        private void OnDisable()
        {
            if (_loop != null) StopCoroutine(_loop);
            _loop = null;
            StopBattle();
            _fatalPolicy?.Dispose(); _fatalPolicy = null;
            // 소유자의 이탈로 잠시 살아난 공격이 발생하지 않도록 액터부터 제거한다.
            if (_boss != null) Destroy(_boss.gameObject);
            _boss = null;
            _hold?.Dispose(); _hold = null;
            if (_flags != null) _flags.OnFlagsReloaded -= RequestRestore;
            _flags = null;
        }
    }
}
