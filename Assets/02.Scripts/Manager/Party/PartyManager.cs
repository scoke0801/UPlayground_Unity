using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;
using UPlayGround.Data.Party;
using UPlayGround.Data.Save;
using UPlayGround.Data.Stat;
using UPlayGround.InputDefine;
using UPlayGround.Core.Party;
using UPlayGround.Manager.World;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 파티 캐릭터 교체 시스템 매니저 (단일 PlayerActor + Model 교체 방식).
    ///
    /// 보유(Roster)와 출전(BattleOrder) 분리 — docs/party-formation-system.md 참조.
    /// - Roster: 보유 전체 (상한 없음). 처치 보상으로 추가됨.
    /// - BattleOrder: 출전 슬롯 (최대 maxBattleSize). Swap 입력의 대상.
    /// - ActiveIndex: BattleOrder 기준.
    ///
    /// 교체 규칙
    /// - 쿨다운 중 입력 버퍼에 보관, 쿨다운 해제 즉시 실행
    /// - 대기 캐릭터 HP 0이면 교체 불가
    /// - Death / Grabbed 상태에서는 교체 불가
    /// - 교체 어시스트: PerfectDodgeWindow 중 교체 성공 시 incoming 캐릭터 공격 자동 발동
    /// </summary>
    public class PartyManager : BaseManager<PartyManager>, IManager, ISaveable, IAsyncInitializableManager,
        IUpdatableManager
    {
        private PartyConfigSO          _config;
        private PlayerActor            _player;
        private readonly PartyRosterService<CharacterActorType> _rosterService = new();
        private List<CharacterActorType> _roster => _rosterService.MutableRoster;
        private List<CharacterActorType> _battleOrder => _rosterService.MutableBattleOrder;
        private readonly Dictionary<CharacterActorType, PartyMemberGrowthSO> _growthLookup = new();
        private readonly Dictionary<CharacterActorType, int> _levels = new();
        // 현재 레벨 내 누적 경험치 (다음 레벨까지의 진행분). 레벨업 시 차감 후 캐리오버.
        private readonly Dictionary<CharacterActorType, long> _exp = new();

        // growth.levelCurve가 없을 때 사용하는 폴백 곡선 파라미터.
        private const int   DefaultCurveBaseExp  = 100;
        private const float DefaultCurveExponent = 1.5f;
        // 파티 구성(BuildPartyFromScene → _player 준비) 전에 LoadGame()이 호출되면 보관했다가
        // 구성 완료 후 적용한다. (InventoryManager/RecipeManager/QuestManager와 동일 패턴)
        private PartySaveData _pendingPartyLoad;
        private readonly Dictionary<CharacterActorType, float> _swapCooldownEndTimes = new();
        private int                    _activeIndex  = 0;
        private int                    _maxBattleSize = 4;
        private bool                   _isSwapping   = false;
        private PlayerCombat           _subscribedCombat;
        private readonly Collider[]    _swapEvadeOverlapBuffer = new Collider[128];
        private readonly HashSet<MonsterActor> _swapEvadeEvaluatedMonsters = new();

        [SerializeField] private float _swapCooldown = 0.5f;
        [SerializeField] private float _partySkillGaugeChargePerPlayerHit = 5f;
        
        // 키 N → BattleOrder N-1번째 캐릭터로 지정 스왑 (CharacterSwap_1 = 0번 슬롯/리더).
        private static readonly (string Action, int BattleIndex)[] SwapInputs =
        {
            (PlayerAction.CharacterSwap_1, 0),
            (PlayerAction.CharacterSwap_2, 1),
            (PlayerAction.CharacterSwap_3, 2),
            (PlayerAction.CharacterSwap_4, 3),
        };
        
        public event Action<PlayerActor, PlayerActor> OnSwapStarted;
        public event Action<PlayerActor>              OnSwapCompleted;
        public event Action<CharacterActorType>       OnCharacterUnlocked;
        public event Action                           OnRosterChanged;
        public event Action                           OnBattleOrderChanged;
        public event Action<CharacterActorType>       OnPartyProgressionChanged;
        public event Action<CharacterActorType, long, long> OnExpChanged;  // (type, currentExp, requiredExp)
        public event Action<CharacterActorType, int>        OnLevelUp;     // (type, newLevel)
        public event Action<CharacterActorType, float, float> OnPartySkillGaugeChanged;
        public event Action<CharacterActorType, float, float> OnSwapCooldownChanged;
        public event Action                           OnPartyHealthRefreshed;   // HUD 벤치 엔트리 일괄 갱신 신호

        public PlayerActor               ActiveCharacter     => _player;
        public bool                      HasPendingSceneRestore => _pendingPartyLoad != null;
        public CharacterActorType        ActiveCharacterType => _player?.GetComponent<PlayerSwapBehaviour>()?.ActiveCharacterType ?? CharacterActorType.None;
        public int                       ActiveIndex         => _activeIndex;
        public int                       MaxBattleSize       => _maxBattleSize;
        public IReadOnlyList<CharacterActorType> Roster      => _roster;
        public IReadOnlyList<CharacterActorType> BattleOrder => _battleOrder;
        public float                     SwapCooldownDuration => Mathf.Max(0f, _swapCooldown);
        public float                     SwapCooldownRemaining => GetSwapCooldownRemaining(ActiveCharacterType);
        public float                     SwapCooldownRatio => SwapCooldownDuration > 0f ? SwapCooldownRemaining / SwapCooldownDuration : 0f;
        public bool                      IsSwapOnCooldown => HasAnySwapCooldown();
        public bool                      EnableSwapEvade => _config == null || _config.enableSwapEvade;
        public float                     SwapEvadeWindowBeforeHit => _config != null ? Mathf.Max(0f, _config.swapEvadeWindowBeforeHit) : 0.25f;
        public float                     SwapEvadeGraceAfterHitStart => _config != null ? Mathf.Max(0f, _config.swapEvadeGraceAfterHitStart) : 0.08f;
        public float                     SwapEvadeIFrameDuration => _config != null ? Mathf.Max(0f, _config.swapEvadeIFrameDuration) : 0.35f;
        public float                     SwapEvadeCounterInputWindow => _config != null ? Mathf.Max(0f, _config.swapEvadeCounterInputWindow) : 0.45f;
        public float                     SwapEvadeThreatSearchRange => _config != null && _config.swapEvadeThreatSearchRange > 0f
            ? _config.swapEvadeThreatSearchRange
            : (_config != null ? Mathf.Max(0f, _config.defaultEntryAttackRange) : 6f);
        public float                     SwapEvadeThreatRadiusPadding => _config != null ? Mathf.Max(0f, _config.swapEvadeThreatRadiusPadding) : 0.5f;
        public LayerMask                 SwapEvadeThreatLayer => _config != null ? _config.swapEvadeThreatLayer : ~0;
        public bool                      SwapEvadeEnableHitStop => _config == null || _config.swapEvadeEnableHitStop;
        public float                     SwapEvadeHitStopDuration => _config != null ? Mathf.Max(0f, _config.swapEvadeHitStopDuration) : 0.06f;
        public float                     SwapEvadeHitStopTimeScale => _config != null ? Mathf.Clamp(_config.swapEvadeHitStopTimeScale, 0.01f, 1f) : 0.08f;
        public CameraShakeIdType         SwapEvadeCameraShakeKey => _config != null ? _config.swapEvadeCameraShakeKey : CameraShakeIdType.LiteHit;
        public string                    SwapEvadeFxKey => _config != null ? _config.swapEvadeFxKey : string.Empty;
        public ActorSocketType           SwapEvadeFxSocket => _config != null ? _config.swapEvadeFxSocket : ActorSocketType.Center;
        public Vector3                   SwapEvadeFxOffset => _config != null ? _config.swapEvadeFxOffset : Vector3.zero;
        public bool                      SwapEvadeCompleteDangerRing => _config == null || _config.swapEvadeCompleteDangerRing;
        public bool                      SwapEvadeSpawnDodgeVitalOrb => _config != null && _config.swapEvadeSpawnDodgeVitalOrb;
        public bool                      EnableResidualAttackOnSwap => _config == null || _config.enableResidualAttackOnSwap;
        public float                     ResidualAttackMaxLifetime => _config != null ? Mathf.Max(0.1f, _config.residualAttackMaxLifetime) : 2.4f;
        public float                     ResidualAttackMinVisibleLifetime => _config != null ? Mathf.Max(0f, _config.residualAttackMinVisibleLifetime) : 0.45f;
        public float                     ResidualAttackFadeOutDuration => _config != null ? Mathf.Max(0f, _config.residualAttackFadeOutDuration) : 0.55f;
        public Color                     ResidualAttackDissolveColor => _config != null ? _config.residualAttackDissolveColor : Color.white;
        public Texture                   ResidualAttackDissolveNoiseMask => _config != null ? _config.residualAttackDissolveNoiseMask : null;
        public float                     ResidualAttackDissolveNoiseStrength => _config != null ? Mathf.Max(0f, _config.residualAttackDissolveNoiseStrength) : 0.1f;
        public Vector4                   ResidualAttackDissolveNoiseScrollRotate => _config != null ? _config.residualAttackDissolveNoiseScrollRotate : Vector4.zero;
        public bool                      ResidualAttackAllowHitStop => _config == null || _config.residualAttackAllowHitStop;
        public bool                      ResidualAttackUseRootMotion => _config != null && _config.residualAttackUseRootMotion;
        public float                     ResidualAttackRootMotionMaxDistance => _config != null ? Mathf.Max(0f, _config.residualAttackRootMotionMaxDistance) : 2.5f;
        public LayerMask                 ResidualAttackRootMotionBlocker => _config != null ? _config.residualAttackRootMotionBlocker : 0;
        public int                       ResidualAttackMaxCount => _config != null ? Mathf.Max(1, _config.residualAttackMaxCount) : 1;
        public bool                      ResidualAttackReturnToSameCharacterRunner => _config == null || _config.residualAttackReturnToSameCharacterRunner;
        public float                     ResidualAttackReturnPositionMaxAge => _config != null ? Mathf.Max(0f, _config.residualAttackReturnPositionMaxAge) : 2.4f;
        public float                     ResidualAttackFeedbackMinInterval => _config != null ? Mathf.Max(0f, _config.residualAttackFeedbackMinInterval) : 0.08f;
        public float                     ResidualAttackHitStopDuration => _config != null ? Mathf.Max(0f, _config.residualAttackHitStopDuration) : 0.04f;
        public float                     ResidualAttackHitStopTimeScale => _config != null ? Mathf.Clamp(_config.residualAttackHitStopTimeScale, 0.01f, 1f) : 0.2f;
        public bool                      ResidualAttackShowCharacterOnDamageFloater => _config != null && _config.residualAttackShowCharacterOnDamageFloater;
        public bool                      PreserveComboStatePerCharacter => _config == null || _config.preserveComboStatePerCharacter;
        public float                     ComboStateMaxCarryTime => _config != null ? Mathf.Max(0f, _config.comboStateMaxCarryTime) : 1.8f;

        public PartyMemberDataSO PartyMemberDataSO => _config?.partyMemberData;
        
        private const string AddressableKey = "PartyConfig";

        // ─── IManager 구현 ────────────────────────────────────────────────

        public void Init()
        {
            RegisterSwapInputs();
            SaveManager.Instance.RegisterSaveable(this);
        }

        public UniTask InitializeAsync(CancellationToken cancellationToken) =>
            LoadConfigSOAsync(cancellationToken);

        private async UniTask LoadConfigSOAsync(CancellationToken cancellationToken)
        {
            try
            {
                _config = await AssetManager.Instance.LoadGlobalAsync<PartyConfigSO>(
                    AddressableKey,
                    nameof(PartyManager),
                    cancellationToken);

                _swapCooldown = Mathf.Max(0f, _config.swapCooldown);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PartyManager] ConfigSO 로드 실패: {e.Message}");
                throw;
            }
        }

        public void AfterInit()
        {
            BuildPartyFromScene();

            if (_player == null)
            {
                Debug.LogWarning("[PartyManager] 씬에 PlayerActor가 없습니다.");
                return;
            }

            if (_battleOrder.Count == 0)
            {
                Debug.LogWarning("[PartyManager] 출전 명단(BattleOrder)이 비어있습니다.");
                return;
            }

            InitializePartyStates();
            SubscribeCombatEvents();
            NotifyActivePlayerChanged();

            // 부팅 시 LoadGame()이 AfterInit보다 먼저 호출됐다면 여기서 마저 복원한다.
            TryApplyPendingPartyLoad();

            Debug.Log($"[PartyManager] 파티 구성 완료: 보유 {_roster.Count}명 / 출전 {_battleOrder.Count}/{_maxBattleSize}, 활성={ActiveCharacterType}");
        }

        public void Dispose()
        {
            SwapResidualAttackRunner.CancelAll();
            UnsubscribeCombatEvents();
            UnregisterSwapInputs();
            _roster.Clear();
            _battleOrder.Clear();
            _growthLookup.Clear();
            _levels.Clear();
            _exp.Clear();
            _swapCooldownEndTimes.Clear();
            _pendingPartyLoad = null;

            _config = null;
        }

        public void OnUpdate()
        {
            if (!CanSwap()) return;

            var buffer = InputManager.Instance?.InputBuffer;
            if (buffer == null) return;
            
            foreach (var input in SwapInputs)
            {
                if (!buffer.HasInput(input.Action))
                    continue;

                buffer.ConsumeInput(input.Action);
                RequestSwapTo(input.BattleIndex);
                break;
            }
        }

        public void OnFixedUpdate() { }
        public void OnLateUpdate()  { }

        public void OnSceneChanged(string sceneType)
        {
            SwapResidualAttackRunner.CancelAll();
            UnsubscribeCombatEvents();
            BuildPartyFromScene();
            if (_player != null && _battleOrder.Count > 0)
            {
                InitializePartyStates();
                SubscribeCombatEvents();
                NotifyActivePlayerChanged();
                TryApplyPendingPartyLoad();
            }
        }

        // ─── 교체 요청 ────────────────────────────────────────────────────

        public bool RequestSwapTo(int targetIndex)
        {
            if (!CanSwapTo(targetIndex))                               return false;
            if (targetIndex == _activeIndex)                           return false;
            if (targetIndex < 0 || targetIndex >= _battleOrder.Count) return false;

            var targetType = _battleOrder[targetIndex];
            var previousType = ActiveCharacterType;

            if (_player.GetHealthForCharacter(targetType) <= 0f)      return false;

            bool isSwapEvade = TryEvaluateSwapEvade(out EnemyAttackThreat swapEvadeThreat);
            bool isAssist = !isSwapEvade && _player.GetCombat()?.IsPerfectDodgeWindow == true;

            _isSwapping = true;
            OnSwapStarted?.Invoke(_player, _player);

            var swap = _player.GetComponent<PlayerSwapBehaviour>();
            if (swap == null || !swap.SwapTo(targetType))
            {
                _isSwapping = false;
                return false;
            }

            // 풀 게이지 스왑 특수공격은 임시 비활성화. 우선 일반 스왑 공격만 사용한다.
            bool isSwapSpecial = false;
            bool isEntryAttack = false;
            if (isSwapEvade)
            {
                _player.BeginSwapEvadeIFrame(SwapEvadeIFrameDuration);
                _player.QueueSwapEvade(swapEvadeThreat.Source, SwapEvadeCounterInputWindow);
                if (SwapEvadeCompleteDangerRing)
                    swapEvadeThreat.Combat?.CompleteDangerRing();
            }
            else if (isAssist)
            {
                // §4.3 어시스트 스왑 → 패리 윈도우 우선. 창 내 적 공격은 패리로 처리되고,
                // 비소비 만료 시 기존 어시스트 즉시공격으로 폴백한다.
                _player.OpenAssistParryAndQueueFallback();
            }
            else if (TryFindEntryAttackTarget(swap.GetModelData(targetType), out var entryTarget))
            {
                _player.QueueEntryAttack(entryTarget);
                isEntryAttack = true;
            }

            _activeIndex  = targetIndex;
            RecordSwapCooldown(previousType);
            _isSwapping   = false;

            NotifyActivePlayerChanged();
            OnSwapCompleted?.Invoke(_player);

            string tag = isSwapSpecial ? " [특수공격]" : (isSwapEvade ? " [스왑회피]" : (isAssist ? " [어시스트]" : (isEntryAttack ? " [등장공격]" : "")));
            Debug.Log($"[PartyManager] 교체 → {targetType}{tag}");
            return true;
        }

        private bool TryEvaluateSwapEvade(out EnemyAttackThreat bestThreat)
        {
            bestThreat = default;
            if (!EnableSwapEvade || _player == null) return false;

            float range = SwapEvadeThreatSearchRange;
            if (range <= 0f) return false;

            Vector3 origin = _player.transform.position;
            int hitCount = Physics.OverlapSphereNonAlloc(
                origin,
                range,
                _swapEvadeOverlapBuffer,
                SwapEvadeThreatLayer,
                QueryTriggerInteraction.Ignore);

            bool found = false;
            float bestScore = float.MaxValue;
            _swapEvadeEvaluatedMonsters.Clear();

            for (int i = 0; i < hitCount; i++)
            {
                var hit = _swapEvadeOverlapBuffer[i];
                if (hit == null) continue;

                var monster = hit.GetComponentInParent<MonsterActor>();
                if (monster == null || !_swapEvadeEvaluatedMonsters.Add(monster))
                    continue;

                var combat = monster != null ? monster.Combat : null;
                if (combat == null)
                    continue;

                if (!combat.TryGetSwapEvadeThreat(
                        origin,
                        SwapEvadeWindowBeforeHit,
                        SwapEvadeGraceAfterHitStart,
                        SwapEvadeThreatRadiusPadding,
                        out EnemyAttackThreat threat))
                    continue;

                float score = threat.IsCollisionActive
                    ? -1f
                    : Mathf.Max(0f, threat.TimeToHit);
                if (score >= bestScore)
                    continue;

                bestScore = score;
                bestThreat = threat;
                found = true;
            }

            return found;
        }

        /// <summary>
        /// 교체 직후 incoming 위치 반경 내에 살아있는 MonsterActor가 있는지 검사.
        /// CharacterModelData 우선, 없으면 PartyConfigSO 글로벌 폴백 사용.
        /// </summary>
        private bool TryFindEntryAttackTarget(CharacterModelData modelData, out MonsterActor nearest)
        {
            nearest = null;
            if (_player == null) return false;

            float     range  = _config != null ? _config.defaultEntryAttackRange : 6f;
            LayerMask layer  = _config != null ? _config.entryAttackTargetLayer  : ~0;
            LayerMask losBlk = _config != null ? _config.entryAttackLineOfSightBlocker : 0;
            bool      requireLos = false;

            if (modelData != null)
            {
                if (modelData.entryAttackRange > 0f) range = modelData.entryAttackRange;
                requireLos = modelData.requireLineOfSight;
            }

            if (range <= 0f) return false;

            Vector3 origin = _player.transform.position;
            Collider[] hits = Physics.OverlapSphere(origin, range, layer);
            float bestSqr = float.MaxValue;

            for (int i = 0; i < hits.Length; ++i)
            {
                var monster = hits[i].GetComponentInParent<MonsterActor>();
                if (monster == null || !monster.IsAlive()) continue;

                if (requireLos && losBlk != 0)
                {
                    Vector3 to = monster.transform.position - origin;
                    if (Physics.Raycast(origin, to.normalized, to.magnitude, losBlk))
                        continue;
                }

                float sqr = (monster.transform.position - origin).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; nearest = monster; }
            }

            return nearest != null;
        }

        /// <summary>
        /// 처치 보상으로 보유(Roster)에 추가한다.
        /// BattleOrder가 가득 차지 않았으면 자동 편입.
        /// 이미 보유 중이거나 PlayerSwapBehaviour에 모델이 없으면 무시.
        /// </summary>
        /// <returns>BattleOrder 에 자동 편입되었는지 여부.</returns>
        public bool UnlockCharacter(CharacterActorType type)
        {
            if (type == CharacterActorType.None) return false;
            if (_roster.Contains(type))          return false;

            var swap = _player?.GetComponent<PlayerSwapBehaviour>();
            if (swap == null || swap.GetModelData(type) == null)
            {
                Debug.LogWarning($"[PartyManager] UnlockCharacter: {type} 모델이 PlayerActor 하위에 없습니다.");
                return false;
            }

            _rosterService.AddToRoster(type);
            InitializeLevelIfMissing(type);
            OnRosterChanged?.Invoke();
            OnCharacterUnlocked?.Invoke(type);
            OnPartyProgressionChanged?.Invoke(type);
            Debug.Log($"[PartyManager] {type} 보유 합류!");

            if (_rosterService.AddToBattle(type, _maxBattleSize))
            {
                OnBattleOrderChanged?.Invoke();
                Debug.Log($"[PartyManager] {type} 출전 자동 편입 (BattleOrder {_battleOrder.Count}/{_maxBattleSize})");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 파티 전원(액티브 + 벤치)을 풀 회복한다. 휴식지점(REST_POINT) 인터렉션 진입점.
        /// _roster 전체를 돌며, 액티브 타입은 HealCharacterToFull 내부 분기로 자동 처리된다.
        /// </summary>
        public void HealAllParty(bool reviveDowned)
        {
            if (_player == null) return;
            foreach (var type in _roster)              // Roster 전체 (출전+대기 보유 전원)
                _player.HealCharacterToFull(type, reviveDowned);
            OnPartyHealthRefreshed?.Invoke();
        }

        /// <summary>
        /// 보유한 캐릭터를 BattleOrder 빈 슬롯에 추가한다.
        /// </summary>
        public bool AddToBattle(CharacterActorType type)
        {
            if (type == CharacterActorType.None) return false;
            if (!_rosterService.AddToBattle(type, _maxBattleSize)) return false;
            OnBattleOrderChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// BattleOrder 에서 제거한다. 활성 캐릭터를 빼는 경우 살아있는 다른 출전 멤버로 활성 자동 보정.
        /// 마지막 살아있는 출전 슬롯은 빼지 못한다 (전투 가능 멤버가 남아있어야 함).
        /// </summary>
        public bool RemoveFromBattle(CharacterActorType type)
        {
            int slotIndex = _battleOrder.IndexOf(type);
            if (slotIndex < 0) return false;

            bool removingActive = (slotIndex == _activeIndex);

            if (removingActive)
            {
                int nextActive = FindNearestAliveBattleIndex(slotIndex, excludeIndex: slotIndex);
                if (nextActive < 0) return false;

                CharacterActorType nextType = _battleOrder[nextActive];
                if (!ApplyActiveSwitchInternal(nextType))
                {
                    return false;
                }

                _rosterService.RemoveFromBattle(type, out _);
                _activeIndex = _battleOrder.IndexOf(nextType);
            }
            else
            {
                _rosterService.RemoveFromBattle(type, out _);
                if (slotIndex < _activeIndex) _activeIndex--;
            }

            OnBattleOrderChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// BattleOrder 의 특정 슬롯을 다른 보유 캐릭터로 교체한다.
        /// 슬롯에 있던 캐릭터는 BattleOrder 에서만 빠지고 Roster 에는 유지된다.
        /// 활성 슬롯을 바꾸는 경우, 새 타입이 살아있어야만 허용된다.
        /// </summary>
        public bool ReplaceBattleSlot(int slotIndex, CharacterActorType type)
        {
            if (slotIndex < 0 || slotIndex >= _battleOrder.Count) return false;
            if (type == CharacterActorType.None)                  return false;
            if (!_roster.Contains(type))                          return false;
            if (_battleOrder[slotIndex] == type)                  return false;

            bool replacingActive = (slotIndex == _activeIndex);
            if (replacingActive && _player.GetHealthForCharacter(type) <= 0f) return false;

            if (!_rosterService.ReplaceBattleSlot(slotIndex, type, out int existingIndex))
                return false;

            if (!replacingActive && existingIndex == _activeIndex)
                _activeIndex = slotIndex;

            // 활성 슬롯의 캐릭터가 바뀐 경우 실제 모델 전환
            if (replacingActive)
            {
                ApplyActiveSwitchInternal(type);
            }

            OnBattleOrderChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 편성 화면에서 BattleOrder 전체를 교체한다.
        /// 활성 캐릭터가 newOrder에 없으면 newOrder[0]으로 전환.
        /// </summary>
        public bool SetBattleOrder(IReadOnlyList<CharacterActorType> newOrder)
        {
            if (newOrder == null || newOrder.Count == 0) return false;

            CharacterActorType prevActive = ActiveCharacterType;
            if (!_rosterService.SetBattleOrder(newOrder, _maxBattleSize))
                return false;

            int newActiveIdx = _battleOrder.IndexOf(prevActive);
            if (newActiveIdx >= 0)
            {
                _activeIndex = newActiveIdx;
            }
            else
            {
                _activeIndex = 0;
                ApplyActiveSwitchInternal(_battleOrder[0]);
            }

            OnBattleOrderChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 활성 캐릭터 사망 연출이 끝난 뒤 살아있는 다음 출전 멤버로 전환한다.
        /// 전투 전멸이면 false를 반환해 RespawnPopup 표시 흐름으로 넘긴다.
        /// </summary>
        public bool TrySwitchToNextAliveAfterActiveDeath()
        {
            if (_player == null || _battleOrder.Count < 2) return false;

            int nextIndex = FindNearestAliveBattleIndex(_activeIndex, excludeIndex: _activeIndex);
            if (nextIndex < 0) return false;

            int previousIndex = _activeIndex;
            CharacterActorType targetType = _battleOrder[nextIndex];

            _isSwapping = true;
            OnSwapStarted?.Invoke(_player, _player);

            _activeIndex = nextIndex;
            bool switched = ApplyActiveSwitchInternal(
                targetType,
                recordCooldown: false,
                preserveAnimation: false,
                spawnResidualAttack: false);

            if (!switched)
                _activeIndex = previousIndex;

            _isSwapping = false;

            if (switched)
                Debug.Log($"[PartyManager] 사망 자동 교체 → {targetType}");

            return switched;
        }

        public bool HasAliveBattleMemberExceptActive()
            => _player != null
               && _battleOrder.Count >= 2
               && FindNearestAliveBattleIndex(_activeIndex, excludeIndex: _activeIndex) >= 0;

        public int GetLevel(CharacterActorType type)
        {
            if (type == CharacterActorType.None) return 0;
            if (_levels.TryGetValue(type, out int level)) return level;

            InitializeLevelIfMissing(type);
            return _levels.TryGetValue(type, out level) ? level : 1;
        }

        public bool SetLevelForDebug(CharacterActorType type, int level)
        {
            if (type == CharacterActorType.None) return false;

            int clamped = Mathf.Clamp(level, 1, LevelCapOf(type));
            _levels[type] = clamped;
            _exp[type]    = 0;

            RefreshGrowthStats(type);
            OnExpChanged?.Invoke(type, 0, RequiredExpOf(type, clamped));
            OnLevelUp?.Invoke(type, clamped);
            OnPartyProgressionChanged?.Invoke(type);
            return true;
        }

        // ── 경험치 / 레벨업 ───────────────────────────────────────

        /// <summary>현재 레벨 내 누적 경험치(다음 레벨까지의 진행분).</summary>
        public long GetExp(CharacterActorType type)
            => _exp.TryGetValue(type, out long exp) ? exp : 0;

        /// <summary>현재 레벨에서 다음 레벨로 가는 데 필요한 총 경험치.</summary>
        public long GetRequiredExp(CharacterActorType type)
            => RequiredExpOf(type, GetLevel(type));

        /// <summary>해당 캐릭터가 만렙인지 여부.</summary>
        public bool IsMaxLevel(CharacterActorType type)
            => GetLevel(type) >= LevelCapOf(type);

        /// <summary>
        /// 출전 멤버(BattleOrder) 전원에게 동일 경험치 100% 분배. 몬스터 처치 시 호출.
        /// </summary>
        public void AwardBattleExp(long amount)
        {
            if (amount <= 0) return;
            for (int i = 0; i < _battleOrder.Count; i++)
            {
                var type = _battleOrder[i];
                if (type == CharacterActorType.None) continue;
                AddExp(type, amount);
            }
        }

        /// <summary>
        /// 단일 캐릭터에 경험치 누적 + 레벨업 처리. 디버그/치트/아이템 보상에도 재사용.
        /// 레벨이 올랐으면 true.
        /// </summary>
        public bool AddExp(CharacterActorType type, long amount)
        {
            if (type == CharacterActorType.None || amount <= 0) return false;
            InitializeLevelIfMissing(type);

            int level = _levels[type];
            int cap   = LevelCapOf(type);
            if (level >= cap) return false;                 // 만렙: 경험치 무시

            long exp     = GetExp(type) + amount;
            bool leveled = false;

            while (level < cap)
            {
                long required = RequiredExpOf(type, level);
                if (exp < required) break;
                exp -= required;
                level++;
                leveled = true;
                OnLevelUp?.Invoke(type, level);
            }
            if (level >= cap) exp = 0;                       // 만렙 도달 시 잉여 버림

            _levels[type] = level;
            _exp[type]    = exp;

            OnExpChanged?.Invoke(type, exp, RequiredExpOf(type, level));
            if (leveled)
            {
                RefreshGrowthStats(type);                    // 살아있는 액터에 반영
                OnPartyProgressionChanged?.Invoke(type);     // 기존 UI 갱신 재사용
            }
            return leveled;
        }

        private int LevelCapOf(CharacterActorType type)
            => _growthLookup.TryGetValue(type, out var growth) && growth != null
                ? Mathf.Max(1, growth.levelCap)
                : 100;

        private long RequiredExpOf(CharacterActorType type, int level)
        {
            if (_growthLookup.TryGetValue(type, out var growth) && growth != null && growth.levelCurve != null)
                return growth.levelCurve.GetRequiredExp(level);

            // 폴백 곡선: required(L) = round(baseExp * pow(L, exponent))
            double required = DefaultCurveBaseExp * System.Math.Pow(Mathf.Max(1, level), DefaultCurveExponent);
            return (long)System.Math.Max(1.0, System.Math.Round(required, System.MidpointRounding.AwayFromZero));
        }

        /// <summary>
        /// 레벨 변경 후 성장 스탯을 살아있는 액터에 반영한다.
        /// 활성 캐릭터는 modifier를 보존하는 라이브 갱신(SetBase만), 벤치는 저장 HP만 갱신.
        /// </summary>
        private void RefreshGrowthStats(CharacterActorType type)
        {
            if (type == CharacterActorType.None) return;

            var growthStats = GetGrowthStats(type);
            if (growthStats == null || growthStats.Count == 0) return;

            if (type == ActiveCharacterType && _player != null)
                _player.RefreshGrowthStatsLive(growthStats);
            else
                _player?.UpdateBenchedGrowth(type, growthStats);
        }

        // ── 저장 / 복원 (ISaveable) ───────────────────────────────

        public void ExportSaveData(GameSaveData saveData)
        {
            if (saveData == null) return;
            var party = saveData.party ??= new PartySaveData();

            party.roster = new List<string>(_roster.Count);
            for (int i = 0; i < _roster.Count; i++)
                party.roster.Add(_roster[i].ToString());

            party.battleOrder = new List<string>(_battleOrder.Count);
            for (int i = 0; i < _battleOrder.Count; i++)
                party.battleOrder.Add(_battleOrder[i].ToString());

            party.activeIndex = _activeIndex;

            party.members = new List<PartyMemberSaveEntry>(_levels.Count);
            foreach (var kv in _levels)
            {
                party.members.Add(new PartyMemberSaveEntry
                {
                    type  = kv.Key.ToString(),
                    level = kv.Value,
                    exp   = GetExp(kv.Key),
                });
            }

            // 캐릭터별 현재 체력 (액티브=CurrentHealth, 벤치=_characterHealthMap, 미기록=풀피).
            party.characterHealth = new List<CharacterHpEntry>(_roster.Count);
            if (_player != null)
            {
                foreach (var type in _roster)
                {
                    party.characterHealth.Add(new CharacterHpEntry
                    {
                        type       = type.ToString(),
                        currentHp  = _player.GetHealthForCharacter(type),
                        skillGauge = _player.GetSkillGaugeForCharacter(type),
                    });
                }
            }

            // 위치/씬 정보 (인게임에서 _player가 존재할 때만 유효).
            if (_player != null)
            {
                party.loadSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                party.mapId         = SceneManager.Instance?.CurrentMapID ?? string.Empty;
                party.playerPos     = new SerializableVector3(_player.transform.position);
                party.playerRot     = new SerializableQuaternion(_player.transform.rotation);
                party.hasLocation   = true;
            }
            else
            {
                party.hasLocation = false;
            }
        }

        public void ImportSaveData(GameSaveData saveData)
        {
            var party = saveData?.party;
            if (party == null) return;

            // 파티 구성(_player)이 끝나기 전에 호출될 수 있으므로 보관 후 적용 시도.
            // AfterInit/OnSceneChanged에서 구성이 끝나면 TryApplyPendingPartyLoad가 마저 적용한다.
            _pendingPartyLoad = party;

            // 저장된 씬으로 이동하는 로드는 현재 씬에 플레이어가 있더라도 적용하지 않는다.
            // 대상 씬의 PlayerActor가 준비된 뒤 OnSceneChanged/SceneManager 안정화 단계에서 적용한다.
            if (SaveManager.Instance?.IsPreparingSceneLoad == true)
                return;

            TryApplyPendingPartyLoad();
        }

        public void ResetForNewGame()
        {
            // 보류 중인 로드 데이터와 누적된 성장/쿨다운 상태를 비운다.
            // _roster/_battleOrder/_growthLookup은 다음 씬 진입 시 BuildPartyFromScene이
            // _config(PartyConfigSO)에서 재시딩하며, InitializeLevelIfMissing은 _levels가
            // 비어 있어야 초기 레벨을 다시 부여하므로 여기서 반드시 비워야 한다.
            // (캐릭터별 HP/스킬게이지는 PlayerActor에 있어 새 씬의 신규 플레이어에서 초기화됨)
            _pendingPartyLoad = null;
            _levels.Clear();
            _exp.Clear();
            _swapCooldownEndTimes.Clear();
        }

        /// <summary>
        /// 대상 씬의 플레이어에 보류 중인 파티/위치 데이터를 적용한다.
        /// SceneManager가 카메라 준비 전에 호출해 최종 복원 위치를 보장한다.
        /// </summary>
        public bool EnsurePendingSceneRestoreApplied(PlayerActor scenePlayer)
        {
            if (_pendingPartyLoad == null)
                return true;

            if (scenePlayer == null)
                return false;

            if (_player != scenePlayer)
            {
                UnsubscribeCombatEvents();
                BuildPartyFromScene();
                if (_player == null || _player != scenePlayer || _battleOrder.Count == 0)
                    return false;

                InitializePartyStates();
                SubscribeCombatEvents();
                NotifyActivePlayerChanged();
            }

            TryApplyPendingPartyLoad();
            return _pendingPartyLoad == null;
        }

        /// <summary>
        /// 보관된 세이브 데이터를 파티에 적용한다. BuildPartyFromScene이 _roster/_battleOrder/_activeIndex를
        /// 매번 재구성·덮어쓰므로, 반드시 그 이후(=_player 준비)에 적용해야 손실되지 않는다.
        /// </summary>
        private void TryApplyPendingPartyLoad()
        {
            var party = _pendingPartyLoad;
            if (party == null) return;
            if (_player == null) return;       // 아직 BuildPartyFromScene 전 → 보관 유지

            _pendingPartyLoad = null;

            _roster.Clear();
            if (party.roster != null)
                foreach (var s in party.roster)
                    if (TryParseCharacter(s, out var t) && !_roster.Contains(t)) _roster.Add(t);

            _battleOrder.Clear();
            if (party.battleOrder != null)
                foreach (var s in party.battleOrder)
                    if (TryParseCharacter(s, out var t) && !_battleOrder.Contains(t)) _battleOrder.Add(t);

            _levels.Clear();
            _exp.Clear();
            if (party.members != null)
            {
                foreach (var m in party.members)
                {
                    if (!TryParseCharacter(m.type, out var t)) continue;
                    int cap = LevelCapOf(t);
                    _levels[t] = Mathf.Clamp(m.level, 1, cap);
                    _exp[t]    = m.level >= cap ? 0 : System.Math.Max(0, m.exp);
                }
            }

            // 로스터에 있는데 레벨 기록이 없는 캐릭터는 초기 레벨로 보정.
            InitializeRosterLevels();

            _activeIndex = _battleOrder.Count > 0
                ? Mathf.Clamp(party.activeIndex, 0, _battleOrder.Count - 1)
                : 0;

            OnRosterChanged?.Invoke();
            OnBattleOrderChanged?.Invoke();

            // ActiveCharacterType은 PlayerSwapBehaviour에서 파생되므로, 복원된 _activeIndex를
            // 스왑에 동기화해야 아래 RefreshGrowthStats가 올바른 활성 캐릭터에 적용된다.
            if (_battleOrder.Count > 0)
            {
                InitializePartyStates();
                NotifyActivePlayerChanged();
            }

            // 활성 캐릭터 스탯을 로드된 레벨로 재반영. 풀 회복 동반.
            RefreshGrowthStats(ActiveCharacterType);
            foreach (var kv in _levels)
                OnPartyProgressionChanged?.Invoke(kv.Key);

            // 플레이어 위치/회전 복원 (저장 당시 위치). Respawn이 KCC motor 위치 설정 +
            // Idle 전환 + 카메라 스냅을 처리한다. healPercent는 무시 — HP는 아래에서 정확히 덮어쓴다.
            if (party.hasLocation && _player != null)
            {
                _player.Respawn(party.playerPos.ToVector3(), party.playerRot.ToQuaternion(), 1f);
            }

            // ⚠️ 반드시 마지막 단계: RefreshGrowthStats와 Respawn이 모두 풀 회복하므로,
            // 저장된 정확한 HP를 그 이후에 덮어써야 손실되지 않는다.
            if (party.characterHealth != null && _player != null)
            {
                foreach (var entry in party.characterHealth)
                {
                    if (!TryParseCharacter(entry.type, out var t)) continue;
                    _player.RestoreCharacterHealth(t, entry.currentHp);
                    _player.RestoreCharacterSkillGauge(t, entry.skillGauge);
                }
                OnPartyHealthRefreshed?.Invoke();   // HUD 벤치 엔트리 일괄 갱신
            }
        }

        private static bool TryParseCharacter(string s, out CharacterActorType type)
        {
            if (!string.IsNullOrEmpty(s) && Enum.TryParse(s, out type) && type != CharacterActorType.None)
                return true;
            type = CharacterActorType.None;
            return false;
        }

        public PartyCombatPowerResult GetCombatPower(CharacterActorType type)
        {
            int level = GetLevel(type);
            _growthLookup.TryGetValue(type, out var growth);
            return PartyPowerCalculator.Calculate(type, growth, level);
        }

        public PartyMemberGrowthSO GetGrowthData(CharacterActorType type)
        {
            _growthLookup.TryGetValue(type, out var growth);
            return growth;
        }

        public PartyCombatPowerResult GetEffectiveCombatPower(CharacterActorType type)
        {
            int level = GetLevel(type);
            var stats = CharacterEffectiveStatCalculator.Calculate(type, GetGrowthData(type), level);
            long combatPower = PartyPowerCalculator.CalculateCombatPower(stats);
            return new PartyCombatPowerResult(type, Mathf.Max(1, level), combatPower, stats);
        }

        public IReadOnlyDictionary<StatType, float> GetGrowthStats(CharacterActorType type)
            => GetCombatPower(type).GrowthStats;

        public long GetPartyCombatPower(IReadOnlyList<CharacterActorType> order = null)
        {
            IReadOnlyList<CharacterActorType> targetOrder = order ?? _battleOrder;
            if (targetOrder == null) return 0L;

            long total = 0L;
            for (int i = 0; i < targetOrder.Count; i++)
            {
                CharacterActorType type = targetOrder[i];
                if (type == CharacterActorType.None) continue;
                total += GetEffectiveCombatPower(type).CombatPower;
            }

            return total;
        }

        public IReadOnlyList<PartyCombatPowerResult> GetBattleOrderCombatPowers()
        {
            var results = new List<PartyCombatPowerResult>(_battleOrder.Count);
            for (int i = 0; i < _battleOrder.Count; i++)
                results.Add(GetEffectiveCombatPower(_battleOrder[i]));
            return results;
        }

        public bool CanSwap()
        {
            if (_isSwapping)             return false;
            if (_battleOrder.Count < 2) return false;

            var currentState = _player?.PlayerController?.CurrentState;
            if (currentState is UPlayGround.State.PlayerFinishAttackState { IsTransitionLocked: true })
                return false;

            var state = currentState?.StateName;
            if (state == "Death")   return false;
            if (state == "Grabbed") return false;

            return true;
        }

        public bool CanSwapTo(int targetIndex)
        {
            if (!CanSwap()) return false;
            if (targetIndex == _activeIndex) return false;
            if (targetIndex < 0 || targetIndex >= _battleOrder.Count) return false;

            CharacterActorType targetType = _battleOrder[targetIndex];
            if (IsSwapCooldownActive(targetType)) return false;
            if (_player != null && _player.GetHealthForCharacter(targetType) <= 0f) return false;

            return true;
        }

        public bool IsSwapCooldownActive(CharacterActorType type) => GetSwapCooldownRemaining(type) > 0f;

        public float GetSwapCooldownRemaining(CharacterActorType type)
        {
            if (type == CharacterActorType.None) return 0f;
            if (!_swapCooldownEndTimes.TryGetValue(type, out float endTime)) return 0f;
            return Mathf.Clamp(endTime - Time.time, 0f, SwapCooldownDuration);
        }

        public float GetSwapCooldownRatio(CharacterActorType type)
        {
            float duration = SwapCooldownDuration;
            return duration > 0f ? GetSwapCooldownRemaining(type) / duration : 0f;
        }

        private bool HasAnySwapCooldown()
        {
            if (SwapCooldownDuration <= 0f) return false;
            if (_swapCooldownEndTimes.Count == 0) return false;

            foreach (var pair in _swapCooldownEndTimes)
            {
                if (pair.Value > Time.time)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 활성 캐릭터를 강제로 다른 BattleOrder 멤버로 전환한다 (편성 변경에 의한 보정 용).
        /// 쿨다운/PerfectDodge/EntryAttack 검사는 우회한다.
        /// </summary>
        private bool ApplyActiveSwitchInternal(
            CharacterActorType targetType,
            bool recordCooldown = true,
            bool preserveAnimation = true,
            bool spawnResidualAttack = true)
        {
            if (_player == null) return false;

            CharacterActorType previousType = ActiveCharacterType;
            var swap = _player.GetComponent<PlayerSwapBehaviour>();
            if (swap == null || !swap.SwapTo(targetType, preserveAnimation, spawnResidualAttack)) return false;

            if (recordCooldown)
                RecordSwapCooldown(previousType);
            NotifyActivePlayerChanged();
            OnSwapCompleted?.Invoke(_player);
            return true;
        }

        private void RecordSwapCooldown(CharacterActorType type)
        {
            float duration = SwapCooldownDuration;
            if (type == CharacterActorType.None || duration <= 0f) return;

            _swapCooldownEndTimes[type] = Time.time + duration;
            OnSwapCooldownChanged?.Invoke(type, duration, duration);
        }

        /// <summary> 모든 캐릭터의 스왑 쿨다운을 즉시 해제한다(치트/디버그용). </summary>
        public void ClearAllSwapCooldowns()
        {
            if (_swapCooldownEndTimes.Count == 0) return;

            var types = new List<CharacterActorType>(_swapCooldownEndTimes.Keys);
            _swapCooldownEndTimes.Clear();

            float duration = SwapCooldownDuration;
            foreach (var type in types)
                OnSwapCooldownChanged?.Invoke(type, 0f, duration);
        }

        /// <summary>
        /// from 인덱스에 가까운 BattleOrder 살아있는 슬롯을 탐색.
        /// </summary>
        private int FindNearestAliveBattleIndex(int from, int excludeIndex)
        {
            int count = _battleOrder.Count;
            if (count == 0 || _player == null) return -1;

            for (int offset = 1; offset < count; ++offset)
            {
                int idx = ((from + offset) % count + count) % count;
                if (idx == excludeIndex) continue;
                if (_player.GetHealthForCharacter(_battleOrder[idx]) > 0f) return idx;
            }
            return -1;
        }

        // ─── 내부: 파티 구성 ──────────────────────────────────────────────

        private void BuildPartyFromScene()
        {
            _player = ResolvePlayerActor();
            _roster.Clear();
            _battleOrder.Clear();
            _swapCooldownEndTimes.Clear();

            _maxBattleSize = _config != null ? Mathf.Max(1, _config.maxBattleSize) : 4;
            _swapCooldown = _config != null ? Mathf.Max(0f, _config.swapCooldown) : Mathf.Max(0f, _swapCooldown);
            BuildGrowthLookup();

            if (_config != null && _config.partyOrder.Count > 0)
            {
                _roster.AddRange(_config.partyOrder);
            }
            else
            {
                // SO 없으면 PlayerSwapBehaviour에서 폴백
                var swap = _player?.GetComponent<PlayerSwapBehaviour>();
                if (swap != null)
                {
                    _roster.AddRange(swap.GetAllCharacterTypes());
                }
            }

            // BattleOrder 초기화 — defaultBattleOrder 우선, 없으면 Roster 앞 maxBattleSize
            if (_config != null && _config.defaultBattleOrder.Count > 0)
            {
                foreach (var t in _config.defaultBattleOrder)
                {
                    if (_battleOrder.Count >= _maxBattleSize) break;
                    if (!_roster.Contains(t))                  continue;
                    if (_battleOrder.Contains(t))              continue;
                    _battleOrder.Add(t);
                }
            }

            if (_battleOrder.Count == 0)
            {
                int take = Mathf.Min(_maxBattleSize, _roster.Count);
                for (int i = 0; i < take; ++i) _battleOrder.Add(_roster[i]);
            }

            int startIdx = _config != null ? _config.startActiveIndex : 0;
            _activeIndex = _battleOrder.Count > 0
                ? Mathf.Clamp(startIdx, 0, _battleOrder.Count - 1)
                : 0;

            InitializeRosterLevels();
            OnRosterChanged?.Invoke();
            OnBattleOrderChanged?.Invoke();
        }

        private PlayerActor ResolvePlayerActor()
        {
            var scenePlayer = UnityEngine.Object.FindFirstObjectByType<PlayerActor>();
            if (scenePlayer != null)
                return scenePlayer;

            if (_config == null || string.IsNullOrWhiteSpace(_config.playerActorId))
                return null;

            var loaders = UnityEngine.Object.FindObjectsByType<RuntimePlacementLoader>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (var loader in loaders)
            {
                if (loader != null && loader.TrySpawnPlayerActor(_config.playerActorId, out var spawned))
                {
                    Debug.Log($"[PartyManager] Bake 배치에서 PlayerActor 스폰: {_config.playerActorId}");
                    return spawned;
                }
            }

            return null;
        }

        private void BuildGrowthLookup()
        {
            _growthLookup.Clear();
            if (_config == null || _config.growthData == null) return;

            for (int i = 0; i < _config.growthData.Count; i++)
            {
                var growth = _config.growthData[i];
                if (growth == null || growth.characterType == CharacterActorType.None) continue;
                if (_growthLookup.ContainsKey(growth.characterType))
                {
                    Debug.LogWarning($"[PartyManager] 중복 성장 데이터가 있습니다: {growth.characterType}");
                    continue;
                }

                _growthLookup.Add(growth.characterType, growth);
            }
        }

        private void InitializeRosterLevels()
        {
            for (int i = 0; i < _roster.Count; i++)
                InitializeLevelIfMissing(_roster[i]);
        }

        private void InitializeLevelIfMissing(CharacterActorType type)
        {
            if (type == CharacterActorType.None || _levels.ContainsKey(type)) return;

            int initialLevel = _growthLookup.TryGetValue(type, out var growth) && growth != null
                ? Mathf.Clamp(growth.initialLevel, 1, Mathf.Max(1, growth.levelCap))
                : 1;
            _levels[type] = initialLevel;
        }

        private void InitializePartyStates()
        {
            var swap = _player?.GetComponent<PlayerSwapBehaviour>();
            if (swap == null)
            {
                Debug.LogWarning("[PartyManager] PlayerActor에 PlayerSwapBehaviour가 없습니다.");
                return;
            }

            var initialType = _battleOrder.Count > _activeIndex
                ? _battleOrder[_activeIndex]
                : _battleOrder[0];

            swap.InitializeTo(initialType);
        }

        private void NotifyActivePlayerChanged()
        {
            GameObjectManager.Instance?.SetActivePartyPlayer(_player);
            CameraManager.Instance?.SetTarget(_player?.transform);
        }

        private void SubscribeCombatEvents()
        {
            UnsubscribeCombatEvents();
            _subscribedCombat = _player?.GetCombat();
            if (_subscribedCombat != null)
                _subscribedCombat.OnAttackHit += OnPlayerAttackHit;
        }

        private void UnsubscribeCombatEvents()
        {
            if (_subscribedCombat != null)
                _subscribedCombat.OnAttackHit -= OnPlayerAttackHit;
            _subscribedCombat = null;
        }

        private void OnPlayerAttackHit(AttackData attackData)
        {
            if (_player == null || _partySkillGaugeChargePerPlayerHit <= 0f) return;

            CharacterActorType activeType = ActiveCharacterType;
            for (int i = 0; i < _battleOrder.Count; i++)
            {
                CharacterActorType type = _battleOrder[i];
                if (type == CharacterActorType.None || type == activeType) continue;

                float before = _player.GetSkillGaugeForCharacter(type);
                _player.AddSkillGaugeForCharacter(type, _partySkillGaugeChargePerPlayerHit);
                float current = _player.GetSkillGaugeForCharacter(type);
                if (!Mathf.Approximately(before, current))
                    OnPartySkillGaugeChanged?.Invoke(type, current, _player.GetMaxSkillGaugeForCharacter(type));
            }
        }

        // ─── 입력 등록 ────────────────────────────────────────────────────

        // Register/Unregister가 동일 델리게이트 참조를 써야 하므로 슬롯별 핸들러를 캐싱한다.
        private readonly Dictionary<string, Action<InputAction.CallbackContext>> _swapHandlers = new();

        private void RegisterSwapInputs()
        {
            if (!InputManager.Instance) return;

            foreach (var input in SwapInputs)
            {
                var handler = GetOrCreateSwapHandler(input.Action, input.BattleIndex);
                InputManager.Instance.RegisterInputEvent(
                    InputMapNames.PlayerAction, input.Action,
                    null, handler, null, null, null, InputLayer.Level_0);
            }
        }

        private void UnregisterSwapInputs()
        {
            if (!InputManager.Instance) return;

            foreach (var input in SwapInputs)
            {
                if (!_swapHandlers.TryGetValue(input.Action, out var handler)) continue;
                InputManager.Instance.UnRegisterInputEvent(
                    InputMapNames.PlayerAction, input.Action,
                    null, handler, null);
            }
        }

        private Action<InputAction.CallbackContext> GetOrCreateSwapHandler(string action, int battleIndex)
        {
            if (_swapHandlers.TryGetValue(action, out var existing))
                return existing;

            Action<InputAction.CallbackContext> handler = _ =>
            {
                if (RequestSwapTo(battleIndex))
                    InputManager.Instance?.InputBuffer.ConsumeInput(action);
            };
            _swapHandlers[action] = handler;
            return handler;
        }
    }
}
