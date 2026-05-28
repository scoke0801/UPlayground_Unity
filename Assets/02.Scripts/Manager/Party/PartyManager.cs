using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using UPlayGround.InputDefine;

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
    public class PartyManager : BaseManager<PartyManager>, IManager
    {
        private PartyConfigSO          _config;
        private PlayerActor            _player;
        private List<CharacterActorType> _roster      = new();
        private List<CharacterActorType> _battleOrder = new();
        private readonly Dictionary<CharacterActorType, PartyMemberGrowthSO> _growthLookup = new();
        private readonly Dictionary<CharacterActorType, int> _levels = new();
        private readonly Dictionary<CharacterActorType, float> _swapCooldownEndTimes = new();
        private int                    _activeIndex  = 0;
        private int                    _maxBattleSize = 4;
        private bool                   _isSwapping   = false;
        private PlayerCombat           _subscribedCombat;

        [SerializeField] private float _swapCooldown = 0.5f;
        [SerializeField] private float _partySkillGaugeChargePerPlayerHit = 5f;

        public event Action<PlayerActor, PlayerActor> OnSwapStarted;
        public event Action<PlayerActor>              OnSwapCompleted;
        public event Action<CharacterActorType>       OnCharacterUnlocked;
        public event Action                           OnRosterChanged;
        public event Action                           OnBattleOrderChanged;
        public event Action<CharacterActorType>       OnPartyProgressionChanged;
        public event Action<CharacterActorType, float, float> OnPartySkillGaugeChanged;
        public event Action<CharacterActorType, float, float> OnSwapCooldownChanged;

        public PlayerActor               ActiveCharacter     => _player;
        public CharacterActorType        ActiveCharacterType => _player?.GetComponent<PlayerSwapBehaviour>()?.ActiveCharacterType ?? CharacterActorType.None;
        public int                       ActiveIndex         => _activeIndex;
        public int                       MaxBattleSize       => _maxBattleSize;
        public IReadOnlyList<CharacterActorType> Roster      => _roster;
        public IReadOnlyList<CharacterActorType> BattleOrder => _battleOrder;
        public float                     SwapCooldownDuration => Mathf.Max(0f, _swapCooldown);
        public float                     SwapCooldownRemaining => GetSwapCooldownRemaining(ActiveCharacterType);
        public float                     SwapCooldownRatio => SwapCooldownDuration > 0f ? SwapCooldownRemaining / SwapCooldownDuration : 0f;
        public bool                      IsSwapOnCooldown => HasAnySwapCooldown();
        public bool                      EnableResidualAttackOnSwap => _config == null || _config.enableResidualAttackOnSwap;
        public float                     ResidualAttackMaxLifetime => _config != null ? Mathf.Max(0.1f, _config.residualAttackMaxLifetime) : 1.8f;
        public float                     ResidualAttackFadeOutDuration => _config != null ? Mathf.Max(0f, _config.residualAttackFadeOutDuration) : 0.25f;
        public bool                      ResidualAttackAllowHitStop => _config == null || _config.residualAttackAllowHitStop;
        public bool                      ResidualAttackUseRootMotion => _config != null && _config.residualAttackUseRootMotion;
        public int                       ResidualAttackMaxCount => _config != null ? Mathf.Max(1, _config.residualAttackMaxCount) : 1;

        public PartyMemberDataSO PartyMemberDataSO => _config?.partyMemberData;
        
        private const string AddressableKey = "PartyConfig";

        // ─── IManager 구현 ────────────────────────────────────────────────

        public void Init()
        {
            LoadConfigSO();
            RegisterSwapInputs();
        }

        private async void LoadConfigSO()
        {
            try
            {
                var handle = Addressables.LoadAssetAsync<PartyConfigSO>(AddressableKey);
                _config = await handle.Task;
                if (_config != null)
                    _swapCooldown = Mathf.Max(0f, _config.swapCooldown);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PartyManager] ConfigSO 로드 실패: {e.Message}");
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

            Debug.Log($"[PartyManager] 파티 구성 완료: 보유 {_roster.Count}명 / 출전 {_battleOrder.Count}/{_maxBattleSize}, 활성={ActiveCharacterType}");
        }

        public void Dispose()
        {
            UnsubscribeCombatEvents();
            UnregisterSwapInputs();
            _roster.Clear();
            _battleOrder.Clear();
            _growthLookup.Clear();
            _levels.Clear();
            _swapCooldownEndTimes.Clear();
        }

        public void OnUpdate()
        {
            if (!CanSwap()) return;

            var buffer = InputManager.Instance?.InputBuffer;
            if (buffer == null) return;

            if (buffer.HasInput(PlayerAction.PlayerSwap))
            {
                buffer.ConsumeInput(PlayerAction.PlayerSwap);
                RequestSwapNext();
            }
        }

        public void OnFixedUpdate() { }
        public void OnLateUpdate()  { }

        public void OnSceneChanged(string sceneType)
        {
            UnsubscribeCombatEvents();
            BuildPartyFromScene();
            if (_player != null && _battleOrder.Count > 0)
            {
                InitializePartyStates();
                SubscribeCombatEvents();
                NotifyActivePlayerChanged();
            }
        }

        // ─── 교체 요청 ────────────────────────────────────────────────────

        public bool RequestSwapNext()
        {
            if (_battleOrder.Count < 2) return false;

            for (int offset = 1; offset < _battleOrder.Count; offset++)
            {
                int targetIndex = (_activeIndex + offset) % _battleOrder.Count;
                if (RequestSwapTo(targetIndex))
                    return true;
            }

            return false;
        }

        public bool RequestSwapTo(int targetIndex)
        {
            if (!CanSwapTo(targetIndex))                               return false;
            if (targetIndex == _activeIndex)                           return false;
            if (targetIndex < 0 || targetIndex >= _battleOrder.Count) return false;

            var targetType = _battleOrder[targetIndex];
            var previousType = ActiveCharacterType;

            if (_player.GetHealthForCharacter(targetType) <= 0f)      return false;

            bool isAssist = _player.GetCombat()?.IsPerfectDodgeWindow == true;

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
            if (isAssist)
            {
                _player.QueueSwapAssist();
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

            string tag = isSwapSpecial ? " [특수공격]" : (isAssist ? " [어시스트]" : (isEntryAttack ? " [등장공격]" : ""));
            Debug.Log($"[PartyManager] 교체 → {targetType}{tag}");
            return true;
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

            _roster.Add(type);
            InitializeLevelIfMissing(type);
            OnRosterChanged?.Invoke();
            OnCharacterUnlocked?.Invoke(type);
            OnPartyProgressionChanged?.Invoke(type);
            Debug.Log($"[PartyManager] {type} 보유 합류!");

            if (_battleOrder.Count < _maxBattleSize)
            {
                _battleOrder.Add(type);
                OnBattleOrderChanged?.Invoke();
                Debug.Log($"[PartyManager] {type} 출전 자동 편입 (BattleOrder {_battleOrder.Count}/{_maxBattleSize})");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 보유한 캐릭터를 BattleOrder 빈 슬롯에 추가한다.
        /// </summary>
        public bool AddToBattle(CharacterActorType type)
        {
            if (type == CharacterActorType.None)        return false;
            if (!_roster.Contains(type))                return false;
            if (_battleOrder.Contains(type))            return false;
            if (_battleOrder.Count >= _maxBattleSize)   return false;

            _battleOrder.Add(type);
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

                _battleOrder.RemoveAt(slotIndex);
                _activeIndex = _battleOrder.IndexOf(nextType);
            }
            else
            {
                _battleOrder.RemoveAt(slotIndex);
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

            int existingIndex = _battleOrder.IndexOf(type);

            // BattleOrder 수정
            if (existingIndex >= 0)
            {
                // 두 출전 슬롯 위치 swap
                _battleOrder[existingIndex] = _battleOrder[slotIndex];
                _battleOrder[slotIndex]     = type;

                // 활성 인덱스 보정: 활성 캐릭터가 existingIndex 에 있었다면 slotIndex 로 이동
                if (!replacingActive && existingIndex == _activeIndex)
                {
                    _activeIndex = slotIndex;
                }
            }
            else
            {
                _battleOrder[slotIndex] = type;
            }

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

            var validated = new List<CharacterActorType>();
            foreach (var t in newOrder)
            {
                if (t == CharacterActorType.None)  continue;
                if (!_roster.Contains(t))          continue;
                if (validated.Contains(t))         continue;
                if (validated.Count >= _maxBattleSize) break;
                validated.Add(t);
            }
            if (validated.Count == 0) return false;

            CharacterActorType prevActive = ActiveCharacterType;
            _battleOrder.Clear();
            _battleOrder.AddRange(validated);

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

            int levelCap = _growthLookup.TryGetValue(type, out var growth) && growth != null
                ? Mathf.Max(1, growth.levelCap)
                : 100;

            _levels[type] = Mathf.Clamp(level, 1, levelCap);
            OnPartyProgressionChanged?.Invoke(type);
            return true;
        }

        public PartyCombatPowerResult GetCombatPower(CharacterActorType type)
        {
            int level = GetLevel(type);
            _growthLookup.TryGetValue(type, out var growth);
            return PartyPowerCalculator.Calculate(type, growth, level);
        }

        public long GetPartyCombatPower(IReadOnlyList<CharacterActorType> order = null)
        {
            IReadOnlyList<CharacterActorType> targetOrder = order ?? _battleOrder;
            if (targetOrder == null) return 0L;

            long total = 0L;
            for (int i = 0; i < targetOrder.Count; i++)
            {
                CharacterActorType type = targetOrder[i];
                if (type == CharacterActorType.None) continue;
                total += GetCombatPower(type).CombatPower;
            }

            return total;
        }

        public IReadOnlyList<PartyCombatPowerResult> GetBattleOrderCombatPowers()
        {
            var results = new List<PartyCombatPowerResult>(_battleOrder.Count);
            for (int i = 0; i < _battleOrder.Count; i++)
                results.Add(GetCombatPower(_battleOrder[i]));
            return results;
        }

        public bool CanSwap()
        {
            if (_isSwapping)             return false;
            if (_battleOrder.Count < 2) return false;

            var state = _player?.PlayerController?.CurrentState?.StateName;
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
        private bool ApplyActiveSwitchInternal(CharacterActorType targetType)
        {
            if (_player == null) return false;

            CharacterActorType previousType = ActiveCharacterType;
            var swap = _player.GetComponent<PlayerSwapBehaviour>();
            if (swap == null || !swap.SwapTo(targetType)) return false;

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
            _player = UnityEngine.Object.FindFirstObjectByType<PlayerActor>();
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
                    _roster = swap.GetAllCharacterTypes();
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

        private void RegisterSwapInputs()
        {
            if (!InputManager.Instance) return;

            InputManager.Instance.RegisterInputEvent(
                InputMapNames.PlayerAction, PlayerAction.PlayerSwap,
                null, OnPlayerSwapPerformed, null,
                null, null, InputLayer.Level_0);
        }

        private void UnregisterSwapInputs()
        {
            if (!InputManager.Instance) return;

            InputManager.Instance.UnRegisterInputEvent(
                InputMapNames.PlayerAction, PlayerAction.PlayerSwap,
                null, OnPlayerSwapPerformed, null);
        }

        private void OnPlayerSwapPerformed(InputAction.CallbackContext ctx)
        {
            if (RequestSwapNext())
                InputManager.Instance?.InputBuffer.ConsumeInput(PlayerAction.PlayerSwap);
        }
    }
}
