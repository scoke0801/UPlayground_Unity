using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;
using UPlayGround.Animation;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Event;
using UPlayGround.Data.Stat;
using UPlayGround.MovementController;
using UPlayGround.Input;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.Combat;
using UPlayGround.State;
using UPlayGround.UI;
using UPlayGround.Gameplay.Passive;
using Random = UnityEngine.Random;
using UPlayGround.AI.CombatDecision;

namespace UPlayGround
{
    // Component
    public partial class PlayerActor : GameActor, IDamageable
    {
        private PassiveAbilityController _passiveAbilities;

        public PlayerEquipment GetPlayerEquipment() => _equipment;
        public PlayerCombat    GetCombat()          => _combat;

        // 연계 라우트 입력 토큰 스트림. 상태(대시/점프/공격)가 발동 확정 시 토큰을 push하고,
        // PlayerAttackState가 Resolve로 라우트를 판정한다. 상태 전환을 넘어 생존한다.
        private ComboInputTracker _comboInputTracker;
        public ComboInputTracker ComboInputTracker => _comboInputTracker ??= new ComboInputTracker();

        /// <summary>
        /// 모델 교체 시 PlayerSwapBehaviour가 호출.
        /// 이전 캐릭터 상태를 저장하고 새 캐릭터 데이터로 컴포넌트를 일괄 갱신한다.
        /// </summary>
        public void RefreshForCharacter(
            CharacterModelData data,
            ActorAnimator.MotionPlaybackSnapshot animationSnapshot = default)
        {
            CharacterActorType previousType = _characterActorType;
            float previousMaxHealth = _maxHealth;
            float previousCurrentHealth = _currentHealth;
            bool wasPreviousHealthFull = previousMaxHealth > 0f
                                         && previousCurrentHealth >= previousMaxHealth - 0.01f;

            // 현재 캐릭터 상태 저장. 씬 직렬화 값은 실제 활성 모델과 다를 수 있으므로
            // 런타임에서 한 번 이상 정상 초기화된 뒤에만 이전 캐릭터 상태로 인정한다.
            if (_hasInitializedCharacterRuntime && _characterActorType != CharacterActorType.None)
            {
                _characterHealthMap[_characterActorType] = _currentHealth;
                _characterSkillMap[_characterActorType]  = _skillGauge.CurrentGauge;
                _characterSkillCooldownMap[_characterActorType] = _skillGauge.GetCooldownRemainingSnapshot();
                if (Abilities != null)
                    _characterAbilityRuntimeMap[_characterActorType] =
                        Abilities.CaptureRuntimeState(forCharacterSwap: true);
                _combat?.SaveComboState(_characterActorType);
            }

            Abilities?.HandleCharacterSwap();

            _characterActorType = data.characterType;
            SetCharacterBaseElement(
                Svc.Party?.GetCombatElement(data.characterType)
                ?? CombatElement.None);
            _hasInitializedCharacterRuntime = true;

            // 연계 토큰 스트림은 캐릭터 종속 — 교체 시 비운다(설계 §8).
            _comboInputTracker?.Clear();

            // 성장 스탯 적용 후 장비 스탯까지 먼저 반영한다.
            // 체력 복원은 최종 MaxHealth 기준으로 처리해야 교체 시 장비 체력 보너스가 비율을 왜곡하지 않는다.
            _maxHealth = ApplyCharacterStats(data);
            _animator            = GetComponentInChildren<ActorAnimator>();
            
            _playerActorAnimator = _animator as PlayerActorAnimator;
            _equipment           = GetComponentInChildren<PlayerEquipment>();
            _equipment?.RefreshWeaponConstraintsFromModel();

            // 장비 데이터는 캐릭터별 레지스트리(InventoryManager)에 시딩한다.
            // 외형은 장착 데이터와 분리하고, 캐릭터 모델의 기본 무기 타입만 사용한다.
            var inventory = Svc.Inventory;
            if (inventory != null)
            {
                inventory.SeedCharacterEquipmentIfAbsent(
                    data.characterType, _equipment != null ? _equipment.StartEquipItems : null);
            }

            ApplyEquipmentStatsForActiveCharacter(preserveHealthRatio: false);

            if (_characterHealthMap.TryGetValue(data.characterType, out var hp))
            {
                // 이전 max 기준 풀피였다면 최종 max 기준 풀피로 유지한다.
                _currentHealth = previousType == data.characterType && wasPreviousHealthFull
                    ? _maxHealth
                    : Mathf.Clamp(hp, 0f, _maxHealth);
            }
            else
            {
                _currentHealth = _maxHealth;
            }
            OnHpChanged?.Invoke(_currentHealth, _maxHealth);

            // 스킬 게이지 복원
            _skillGauge.SetGauge(
                _characterSkillMap.TryGetValue(data.characterType, out var sg) ? sg : 0f);
            _skillGauge.SetCooldownRemainingSnapshot(
                _characterSkillCooldownMap.TryGetValue(data.characterType, out var cooldowns) ? cooldowns : null);
            Abilities?.SetAbilitySet(data.abilitySet);
            Abilities?.RestoreRuntimeState(
                _characterAbilityRuntimeMap.TryGetValue(data.characterType, out var abilityRuntime)
                    ? abilityRuntime
                    : null);
            _passiveAbilities?.RefreshForCharacter(data.characterType);

            _equipment?.SetWeaponType(data.defaultWeaponType);
            
            // 애니메이터에 Actor 재주입 (PlayerEquipment 참조 포함)
            _playerActorAnimator?.Init(this);

            // 전투 컴포넌트 참조 갱신 + 공격 데이터 교체
            _combat.RefreshComponentReferences();
            var partyManager = Svc.Party;
            _combat.RefreshAbilitySet(
                data.abilitySet,
                data.characterType,
                partyManager == null || partyManager.PreserveComboStatePerCharacter,
                partyManager != null ? partyManager.ComboStateMaxCarryTime : 1.8f);
            _combatWeaponStateController?.RefreshReferences();
            ApplyCharacterWeight(data.weightProfile);

            // 새 모델의 ParentConstraint 기본 weight는 prefab 세팅에 의존하므로,
            // 현재 전투 상태에 맞춰 weight + 플래그를 강제 동기화한다.
            _equipment?.ForceSyncMainWeaponState(_combat != null && _combat.IsInCombat);

            // 모델별 공용 소켓
            RefreshSockets(data);

            // 비주얼 효과 컴포넌트 재초기화
            _colorChanger.InitializeRendererData();
            _dissolveController.RefreshRenderers();
            _cameraProximityDither?.RefreshRenderers();

            // Foot IK
            _footIK.Refresh(data.AnimancerComponent?.Animator);

            // 모델 교체 전 재생 중이던 MotionSet이 있으면 같은 AnimKey의 진행률로 복원한다.
            // 초기화/복원 실패 시에는 기존처럼 Idle을 강제로 한 번 재생해 새 Animancer에 포즈를 적용한다.
            bool restoredAnimation = _playerActorAnimator != null
                                     && animationSnapshot.IsValid
                                     && _playerActorAnimator.RestorePlaybackSnapshot(animationSnapshot);
            if (!restoredAnimation)
            {
                PlayerMovementPlayerController?.TransitionToState(new PlayerIdleState(PlayerMovementPlayerController));
                _playerActorAnimator?.PlayMotion(AnimKey.Idle, 0f);
            }

            OnHpChanged?.Invoke(_currentHealth, _maxHealth);
        }

        private void RefreshSockets(CharacterModelData data)
        {
            _socketDict ??= new SerializedDictionary<ActorSocketType, Transform>();
            _socketDict.Clear();

            if (data?.SocketDict == null)
                return;

            foreach (var pair in data.SocketDict)
            {
                if (pair.Key == ActorSocketType.None || pair.Value == null)
                    continue;

                _socketDict[pair.Key] = pair.Value;
            }
        }

        private float ApplyCharacterStats(CharacterModelData data)
        {
            CharacterActorType type = data != null ? data.characterType : _characterActorType;
            IReadOnlyDictionary<StatType, float> growthStats = Svc.Party?.GetGrowthStats(type);

            if (growthStats != null && growthStats.Count > 0)
            {
                Stats?.Init(null);
                foreach (KeyValuePair<StatType, float> pair in growthStats)
                    Stats?.SetBase(pair.Key, pair.Value);
                return Mathf.Max(1f, Stats != null ? Stats.MaxHealth : ActorStatSO.GetDefault(StatType.MaxHealth));
            }

            if (Definition != null && Definition.statData != null)
            {
                Stats?.Init(Definition.statData);
                return Mathf.Max(1f, Stats != null ? Stats.MaxHealth : Definition.statData.GetBase(StatType.MaxHealth));
            }

            Stats?.Init(null);
            return Mathf.Max(1f, Stats != null ? Stats.MaxHealth : ActorStatSO.GetDefault(StatType.MaxHealth));
        }

        public void ApplyEquipmentStatsForActiveCharacter(bool preserveHealthRatio = true)
        {
            if (Stats == null || _characterActorType == CharacterActorType.None)
                return;

            float oldMax = Mathf.Max(1f, _maxHealth);
            float oldCurrent = Mathf.Clamp(_currentHealth, 0f, oldMax);
            bool wasFull = oldCurrent >= oldMax - 0.01f;
            float oldRatio = oldMax > 0f ? oldCurrent / oldMax : 1f;

            Stats.RemoveModifiersBySource(_equipmentStatSource);
            _equipmentStatBuffer.Clear();

            var equipment = Svc.Inventory?.GetEquippedEquipment(_characterActorType);
            if (equipment != null)
            {
                for (int i = 0; i < equipment.Count; i++)
                    equipment[i]?.AddStatModifiersTo(_equipmentStatBuffer, _equipmentStatSource);
            }

            for (int i = 0; i < _equipmentStatBuffer.Count; i++)
                Stats.AddModifier(_equipmentStatBuffer[i]);

            _maxHealth = Mathf.Max(1f, Stats.MaxHealth);
            if (preserveHealthRatio)
            {
                _currentHealth = wasFull
                    ? _maxHealth
                    : Mathf.Clamp(_maxHealth * oldRatio, 0f, _maxHealth);
                OnHpChanged?.Invoke(_currentHealth, _maxHealth);
            }
        }

        public void RefreshEquipmentStatsForCharacter(
            CharacterActorType type,
            float previousCurrentHealth,
            float previousMaxHealth)
        {
            if (type == CharacterActorType.None)
                return;

            if (type == _characterActorType)
            {
                ApplyEquipmentStatsForActiveCharacter();
                return;
            }

            if (!_characterHealthMap.ContainsKey(type))
                return;

            float oldMax = Mathf.Max(1f, previousMaxHealth);
            float oldCurrent = Mathf.Clamp(previousCurrentHealth, 0f, oldMax);
            bool wasFull = oldCurrent >= oldMax - 0.01f;
            float oldRatio = oldMax > 0f ? oldCurrent / oldMax : 1f;
            float newMax = GetMaxHealthForCharacter(type);

            _characterHealthMap[type] = wasFull
                ? newMax
                : Mathf.Clamp(newMax * oldRatio, 0f, newMax);
        }

        /// <summary>
        /// 전투 중 레벨업 등으로 활성 캐릭터의 성장 스탯을 즉시 반영한다.
        /// 기둥 A: base 스탯만 교체(SetBase)하여 장비/버프 modifier를 보존한다. Init()을 호출하지 않는다.
        /// 레벨업 정책에 따라 HP/Poise는 풀 회복한다.
        /// </summary>
        public void RefreshGrowthStatsLive(IReadOnlyDictionary<StatType, float> growthStats)
        {
            if (growthStats == null || growthStats.Count == 0) return;

            // 다운된(HP 0) 활성 캐릭터는 레벨업으로 부활시키지 않는다(벤치 경로와 대칭).
            // 사망 중에는 스왑이 막혀 게임오버→리로드 시 ApplyCharacterStats가 커밋된 레벨로 스탯을 재구성하므로 손실 없음.
            if (!IsAlive()) return;

            foreach (KeyValuePair<StatType, float> pair in growthStats)
                Stats?.SetBase(pair.Key, pair.Value);   // Init() 미호출 → modifier 유지

            _maxHealth     = Mathf.Max(1f, Stats != null ? Stats.MaxHealth : _maxHealth);
            _currentHealth = _maxHealth;                // 풀 회복
            OnHpChanged?.Invoke(_currentHealth, _maxHealth);

            // Poise 풀 회복(브레이크 해제 포함). MaxPoise 성장은 SetBase가 이미 반영.
            GetComponent<PoiseStat>()?.RecoverFull();
        }

        /// <summary>
        /// 벤치(대기 중) 캐릭터가 레벨업했을 때의 갱신. 화면에 모델이 없으므로 스탯 컨테이너는 건드리지 않고,
        /// 저장된 현재 HP만 새 최대치로 풀 회복한다(다음 스왑 시 풀 HP로 등장). 기둥 B.
        /// </summary>
        public void UpdateBenchedGrowth(CharacterActorType type, IReadOnlyDictionary<StatType, float> growthStats)
        {
            if (type == CharacterActorType.None || type == _characterActorType) return;
            if (growthStats == null) return;

            // 기록이 없으면(한 번도 피해를 입지 않음) 이미 풀피로 취급되므로 손대지 않는다.
            // 다운된(HP 0) 멤버는 레벨업으로 부활시키지 않는다.
            if (!_characterHealthMap.TryGetValue(type, out float stored) || stored <= 0f) return;

            float newMax = GetMaxHealthForCharacter(type);
            _characterHealthMap[type] = newMax;          // 살아있는 대기 멤버만 풀 회복
        }

        /// <summary>
        /// 지정 캐릭터의 현재 체력 반환. 한 번도 활성화된 적 없으면 최대 체력으로 취급한다.
        /// </summary>
        public float GetHealthForCharacter(CharacterActorType type)
        {
            if (type == _characterActorType) return _currentHealth;
            return _characterHealthMap.TryGetValue(type, out var hp) ? hp : GetMaxHealthForCharacter(type);
        }

        public bool HasHealthRecordForCharacter(CharacterActorType type)
            => type == _characterActorType || _characterHealthMap.ContainsKey(type);

        /// <summary>
        /// 지정 캐릭터의 최대 체력 반환. 현재 캐릭터가 아니면 PlayerSwapBehaviour의 모델 데이터에서 조회한다.
        /// </summary>
        public float GetMaxHealthForCharacter(CharacterActorType type)
        {
            if (type == _characterActorType) return _maxHealth;

            IReadOnlyDictionary<StatType, float> effectiveStats =
                UPlayGround.Data.Party.CharacterEffectiveStatCalculator.Calculate(type);
            if (effectiveStats != null && effectiveStats.TryGetValue(StatType.MaxHealth, out float maxHealth))
                return Mathf.Max(1f, maxHealth);

            return Mathf.Max(1f, ActorStatSO.GetDefault(StatType.MaxHealth));
        }

        /// <summary>지정 캐릭터를 풀 회복. reviveDowned=true면 HP 0(다운) 멤버도 되살린다.</summary>
        public void HealCharacterToFull(CharacterActorType type, bool reviveDowned)
        {
            if (type == _characterActorType)
            {
                // 액티브: 다운 상태면 player는 이미 사망 플로우이므로 정상 케이스는 생존.
                // 풀 회복 (Heal은 IsAlive 가드 → 직접 세팅으로 부활까지 커버)
                if (!reviveDowned && !IsAlive()) return;
                float old = _currentHealth;
                _currentHealth = _maxHealth;
                if (_currentHealth > old)
                {
                    OnHpChanged?.Invoke(_currentHealth, _maxHealth);
                    ActorSvc.UI.ShowDamageFloaterHeal(transform.position, _currentHealth - old);
                }
                return;
            }

            // 벤치: _characterHealthMap 직접 기록 (이벤트 안 나감 → HUD 별도 갱신 필요)
            float max = GetMaxHealthForCharacter(type);
            bool hasRecord = _characterHealthMap.TryGetValue(type, out float stored);
            if (!hasRecord) return;                       // 기록 없음 = 이미 풀피
            if (!reviveDowned && stored <= 0f) return;    // 부활 비활성 시 다운 제외
            _characterHealthMap[type] = max;
        }

        /// <summary>
        /// 세이브 로드용: 지정 캐릭터의 현재 체력을 정확한 값으로 복원한다.
        /// 풀 회복이 아니라 저장된 값을 그대로 반영하므로, 파티 로드의 풀 회복 단계 이후에 호출해야 한다.
        /// hp 0 이하면 다운 상태로 복원한다(부활시키지 않음).
        /// </summary>
        public void RestoreCharacterHealth(CharacterActorType type, float hp)
        {
            if (type == CharacterActorType.None) return;

            if (type == _characterActorType)
            {
                _currentHealth = Mathf.Clamp(hp, 0f, _maxHealth);
                OnHpChanged?.Invoke(_currentHealth, _maxHealth);
                return;
            }

            // 벤치: _characterHealthMap에 직접 기록 (이벤트 없음 → HUD는 PartyManager가 일괄 갱신)
            float max = GetMaxHealthForCharacter(type);
            _characterHealthMap[type] = Mathf.Clamp(hp, 0f, max);
        }

        /// <summary>
        /// 세이브 로드용: 지정 캐릭터의 스킬 게이지를 저장된 값으로 복원한다.
        /// 액티브는 PlayerSkillGauge에, 벤치는 _characterSkillMap에 기록한다.
        /// </summary>
        public void RestoreCharacterSkillGauge(CharacterActorType type, float gauge)
        {
            if (type == CharacterActorType.None) return;

            float max = _skillGauge != null ? _skillGauge.MaxGauge : 1f;
            float clamped = Mathf.Clamp(gauge, 0f, max);

            if (type == _characterActorType)
                _skillGauge?.SetGauge(clamped);
            else
                _characterSkillMap[type] = clamped;
        }

        public float GetSkillGaugeForCharacter(CharacterActorType type)
        {
            if (type == _characterActorType) return _skillGauge != null ? _skillGauge.CurrentGauge : 0f;
            return _characterSkillMap.TryGetValue(type, out var gauge) ? gauge : 0f;
        }

        public AbilityRuntimeSaveData GetAbilityRuntimeForCharacter(CharacterActorType type)
        {
            if (type == CharacterActorType.None)
                return null;
            if (type == _characterActorType)
                return Abilities?.CaptureRuntimeState();
            return _characterAbilityRuntimeMap.TryGetValue(type, out AbilityRuntimeSaveData data)
                ? data
                : null;
        }

        public void RestoreCharacterAbilityRuntime(
            CharacterActorType type,
            AbilityRuntimeSaveData data)
        {
            if (type == CharacterActorType.None)
                return;
            if (type == _characterActorType)
            {
                Abilities?.RestoreRuntimeState(data);
                return;
            }

            if (data == null)
                _characterAbilityRuntimeMap.Remove(type);
            else
                _characterAbilityRuntimeMap[type] = data;
        }

        public float GetMaxSkillGaugeForCharacter(CharacterActorType type)
            => _skillGauge != null ? _skillGauge.MaxGauge : 1f;

        public bool IsSkillGaugeFullForCharacter(CharacterActorType type)
        {
            float max = GetMaxSkillGaugeForCharacter(type);
            return max > 0f && GetSkillGaugeForCharacter(type) >= max;
        }

        public bool CanUseSkillForCharacter(CharacterActorType type, int skillSlot)
        {
            if (type == CharacterActorType.None || _skillGauge == null) return false;

            if (type == _characterActorType)
                return _skillGauge.CanUseSkill(skillSlot);

            float cost = _skillGauge.GetSkillCost(skillSlot);
            if (float.IsInfinity(cost) || GetSkillGaugeForCharacter(type) < cost)
                return false;

            if (_characterSkillCooldownMap.TryGetValue(type, out var cooldowns)
                && cooldowns != null
                && (uint)skillSlot < (uint)cooldowns.Length
                && cooldowns[skillSlot] > 0f)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 파티 HUD의 궁극기 "준비" 표시용 판정. 실제 발동은 비용(<see cref="PlayerSkillGauge.CanUseSkill"/>)으로
        /// 게이트하지만, 글로우 같은 준비 연출은 게이지가 가득 찼을 때만 켠다
        /// (스킬바의 <c>UISkillSlot._showOnlyWhenGaugeFull</c>와 동일 의미). 쿨타임 중이면 false.
        /// </summary>
        public bool IsUltimateReadyForCharacter(CharacterActorType type)
            => IsSkillGaugeFullForCharacter(type)
               && CanUseSkillForCharacter(type, PlayerSkillGauge.UltimateSkillSlot);

        public void AddSkillGaugeForCharacter(CharacterActorType type, float amount)
        {
            if (type == CharacterActorType.None || amount <= 0f || _skillGauge == null) return;

            if (type == _characterActorType)
            {
                _skillGauge.AddGauge(amount);
                return;
            }

            float max = _skillGauge.MaxGauge;
            float current = _characterSkillMap.TryGetValue(type, out var gauge) ? gauge : 0f;
            _characterSkillMap[type] = Mathf.Clamp(current + amount, 0f, max);
        }

        public bool ConsumeFullSkillGaugeForCharacter(CharacterActorType type)
        {
            if (!IsSkillGaugeFullForCharacter(type)) return false;

            if (type == _characterActorType)
                _skillGauge.SetGauge(0f);
            else
                _characterSkillMap[type] = 0f;

            return true;
        }

        private void InitComponents()
        {
            if (_combat     == null) _combat     = GetComponent<PlayerCombat>();
            if (_equipment  == null) _equipment  = GetComponentInChildren<PlayerEquipment>();
            if (_skillGauge == null) _skillGauge = GetComponent<PlayerSkillGauge>();
            if (_combatWeaponStateController == null)
                _combatWeaponStateController = gameObject.GetOrAddComponent<PlayerCombatWeaponStateController>();
            if (_footIK     == null) _footIK     = GetComponent<FootIKController>();
            if (_swapBehaviour == null) _swapBehaviour = GetComponent<PlayerSwapBehaviour>();
            if (_behaviorPredictor == null) _behaviorPredictor = gameObject.GetOrAddComponent<PlayerBehaviorPredictor>();
            if (_passiveAbilities == null)
                _passiveAbilities = gameObject.GetOrAddComponent<PassiveAbilityController>();

            if (_skillGauge != null)
                _skillGauge.OnGaugeChanged += (cur, max) => OnSkillGaugeChanged?.Invoke(cur, max);
            // SetCombatStateProvider는 OnEnable/OnDisable에서 관리
        }

        public void EnsureCharacterRuntimeInitialized()
        {
            EnsureInitialCharacterModelInitialized();
        }

        private void EnsureInitialCharacterModelInitialized()
        {
            CharacterModelData activeModel = null;
            var models = GetComponentsInChildren<CharacterModelData>(true);
            for (int i = 0; i < models.Length; i++)
            {
                if (models[i] != null && models[i].gameObject.activeInHierarchy)
                {
                    activeModel = models[i];
                    break;
                }
            }

            if (activeModel == null)
                return;

            bool needsRefresh =
                !_hasInitializedCharacterRuntime ||
                _characterActorType != activeModel.characterType ||
                _equipment == null ||
                _equipment.GetMainWeaponType() != activeModel.defaultWeaponType;

            if (needsRefresh)
                RefreshForCharacter(activeModel);
        }
    }
}
