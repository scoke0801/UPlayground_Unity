using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.Ability.Core;
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
            // 이전에 비활성화된 모델에도 카메라 디더용 런타임 머티리얼이 남아
            // 있을 수 있다. 장비 컴포넌트가 이를 내장 무기 원본으로 캐시하기 전에
            // 모든 렌더러를 에셋 원본 머티리얼로 되돌린다.
            _cameraProximityDither?.RestoreOriginalMaterialsImmediately();

            CharacterActorType previousType = _characterActorType;
            float previousMaxHealth = _maxHealth;
            float previousCurrentHealth = _currentHealth;
            bool wasPreviousHealthFull = previousMaxHealth > 0f
                                         && previousCurrentHealth >= previousMaxHealth - 0.01f;

            // 현재 캐릭터 상태 저장. 씬 직렬화 값은 실제 활성 모델과 다를 수 있으므로
            // 런타임에서 한 번 이상 정상 초기화된 뒤에만 이전 캐릭터 상태로 인정한다.
            if (_hasInitializedCharacterRuntime && _characterActorType != CharacterActorType.None)
            {
                if (Abilities != null)
                    _characterAbilitySystemMap[_characterActorType] =
                        Abilities.CaptureAbilitySystemStateForCharacter();
                _combat?.SaveComboState(_characterActorType);
            }

            Abilities?.HandleCharacterSwap();
            if (_equipmentStatEffectHandle.IsValid)
            {
                AbilitySystem.RemoveEffect(_equipmentStatEffectHandle);
                _equipmentStatEffectHandle = default;
            }
            if (_skillTreeStatEffectHandle.IsValid)
            {
                AbilitySystem.RemoveEffect(_skillTreeStatEffectHandle);
                _skillTreeStatEffectHandle = default;
            }

            _characterActorType = data.characterType;
            SetCharacterBaseElement(
                Svc.Party?.GetCombatElement(data.characterType)
                ?? CombatElement.None);
            _hasInitializedCharacterRuntime = true;

            // 연계 토큰 스트림은 캐릭터 종속 — 교체 시 비운다(설계 §8).
            _comboInputTracker?.Clear();

            // 성장 스탯 적용 후 장비 스탯까지 먼저 반영한다.
            // 체력 복원은 최종 MaxHealth 기준으로 처리해야 교체 시 장비 체력 보너스가 비율을 왜곡하지 않는다.
            ApplyCharacterStats(data);
            ApplySkillTreeStatsForActiveCharacter(preserveCurrentResources: false);
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

            Abilities?.SetAbilitySet(data.abilitySet);
            Abilities?.SetResourceRules(data.abilityResourceRules);
            if (_characterAbilitySystemMap.TryGetValue(
                    data.characterType, out AbilitySystemSaveData savedState))
            {
                Abilities?.RestoreAbilitySystemStateForCharacter(savedState);
                if (previousType == data.characterType && wasPreviousHealthFull)
                    _currentHealth = _maxHealth;
            }
            else
            {
                _currentHealth = _maxHealth;
                AbilitySystem.Attributes.SetBase(
                    global::UPlayGround.Data.Stat.Attributes.Resource.UltimateEnergy, 0f);
                AbilitySystem.Runtime.Cooldowns.Clear();
            }
            OnHpChanged?.Invoke(_currentHealth, _maxHealth);
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
                PlayerMovementPlayerController?.TransitionToState(ActorStateId.Idle);
                _playerActorAnimator?.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Idle, 0f);
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
            IReadOnlyDictionary<AttributeId, float> baseStats =
                Svc.Party?.GetBaseStats(type);

            if (baseStats != null && baseStats.Count > 0)
            {
                AbilitySystem.InitializeDefaultAttributes();
                AbilitySystem.SetAttributeBases(baseStats);
                return Mathf.Max(1f, AbilitySystem.Attributes.GetCurrent(
                    global::UPlayGround.Data.Stat.Attributes.Vital.MaxHealth));
            }

            if (Definition != null && Definition.attributeProfile != null)
            {
                AbilitySystem.InitializeDefaultAttributes();
                if (!AbilitySystem.InitializeAttributes(
                        Definition.attributeProfile, out string profileError))
                {
                    Debug.LogError(
                        $"[PlayerActor] {Definition.name} Attribute Profile 적용 실패: " +
                        profileError,
                        Definition.attributeProfile);
                }
                return Mathf.Max(1f, AbilitySystem.Attributes.GetCurrent(
                    global::UPlayGround.Data.Stat.Attributes.Vital.MaxHealth));
            }

            AbilitySystem.InitializeDefaultAttributes();
            if (Definition != null)
            {
                Debug.LogError(
                    $"[PlayerActor] {Definition.name}에 Attribute Profile이 없습니다.",
                    Definition);
            }
            return Mathf.Max(1f, AbilitySystem.Attributes.GetCurrent(
                global::UPlayGround.Data.Stat.Attributes.Vital.MaxHealth));
        }

        public void ApplyEquipmentStatsForActiveCharacter(bool preserveHealthRatio = true)
        {
            if (AbilitySystem == null || _characterActorType == CharacterActorType.None)
                return;

            float oldMax = Mathf.Max(1f, _maxHealth);
            float oldCurrent = Mathf.Clamp(_currentHealth, 0f, oldMax);
            bool wasFull = oldCurrent >= oldMax - 0.01f;
            float oldRatio = oldMax > 0f ? oldCurrent / oldMax : 1f;

            if (_equipmentStatEffectHandle.IsValid)
                AbilitySystem.RemoveEffect(_equipmentStatEffectHandle);
            _equipmentStatBuffer.Clear();

            var equipment = Svc.Inventory?.GetEquippedEquipment(_characterActorType);
            if (equipment != null)
            {
                for (int i = 0; i < equipment.Count; i++)
                    equipment[i]?.AddAttributeModifiersTo(_equipmentStatBuffer);
            }

            _equipmentStatEffectHandle = AbilitySystem.ApplyAttributeEffect(
                $"Equipment.{_characterActorType}",
                _equipmentStatBuffer);

            float newMaxHealth = Mathf.Max(1f, AbilitySystem.Attributes.GetCurrent(
                global::UPlayGround.Data.Stat.Attributes.Vital.MaxHealth));
            if (preserveHealthRatio)
            {
                _currentHealth = wasFull
                    ? newMaxHealth
                    : Mathf.Clamp(newMaxHealth * oldRatio, 0f, newMaxHealth);
                OnHpChanged?.Invoke(_currentHealth, _maxHealth);
            }
        }

        public void ApplySkillTreeStatsForActiveCharacter(bool preserveCurrentResources = true)
        {
            if (AbilitySystem == null || _characterActorType == CharacterActorType.None)
                return;

            float oldMax = Mathf.Max(1f, _maxHealth);
            float oldCurrent = Mathf.Clamp(_currentHealth, 0f, oldMax);

            if (_skillTreeStatEffectHandle.IsValid)
                AbilitySystem.RemoveEffect(_skillTreeStatEffectHandle);
            _skillTreeStatBuffer.Clear();

            IReadOnlyList<UPlayGround.Data.Party.SkillStatModifierEntry> modifiers =
                Svc.Party?.GetSkillStatModifiers(_characterActorType);
            if (modifiers != null)
                for (int i = 0; i < modifiers.Count; i++)
                    _skillTreeStatBuffer.Add(modifiers[i].ToRuntimeValue());

            _skillTreeStatEffectHandle = AbilitySystem.ApplyAttributeEffect(
                $"SkillTree.{_characterActorType}",
                _skillTreeStatBuffer);

            if (!preserveCurrentResources)
                return;
            float newMax = Mathf.Max(1f, AbilitySystem.Attributes.GetCurrent(
                global::UPlayGround.Data.Stat.Attributes.Vital.MaxHealth));
            _currentHealth = Mathf.Clamp(oldCurrent, 0f, newMax);
            Stamina?.ClampToMaximum();
            OnHpChanged?.Invoke(_currentHealth, _maxHealth);
        }

        public void RefreshSkillTreeStatsForCharacter(CharacterActorType type)
        {
            if (type == CharacterActorType.None)
                return;
            if (type == _characterActorType)
            {
                ApplySkillTreeStatsForActiveCharacter();
                _passiveAbilities?.RefreshForCharacter(type);
                return;
            }

            if (!TryGetStoredAttribute(
                    type,
                    global::UPlayGround.Data.Stat.Attributes.Vital.Health,
                    out float currentHealth))
                return;

            float newMax = GetMaxHealthForCharacter(type);
            SetStoredAttribute(
                type,
                global::UPlayGround.Data.Stat.Attributes.Vital.Health,
                Mathf.Clamp(currentHealth, 0f, newMax));
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

            if (!TryGetStoredAttribute(
                    type, global::UPlayGround.Data.Stat.Attributes.Vital.Health, out _))
                return;

            float oldMax = Mathf.Max(1f, previousMaxHealth);
            float oldCurrent = Mathf.Clamp(previousCurrentHealth, 0f, oldMax);
            bool wasFull = oldCurrent >= oldMax - 0.01f;
            float oldRatio = oldMax > 0f ? oldCurrent / oldMax : 1f;
            float newMax = GetMaxHealthForCharacter(type);

            SetStoredAttribute(
                type,
                global::UPlayGround.Data.Stat.Attributes.Vital.Health,
                wasFull ? newMax : Mathf.Clamp(newMax * oldRatio, 0f, newMax));
        }

        /// <summary>
        /// 개발 도구에서 활성 캐릭터의 base 스탯을 즉시 교체한다.
        /// 장비/버프 modifier는 보존하고 HP와 Poise는 새 최대치로 회복한다.
        /// </summary>
        public void RefreshBaseStatsLive(
            IReadOnlyDictionary<AttributeId, float> baseStats)
        {
            if (baseStats == null || baseStats.Count == 0) return;

            // 개발 중 수치 조정이 사망 상태를 우회하는 부활 경로가 되지 않게 한다.
            if (!IsAlive()) return;

            AbilitySystem.SetAttributeBases(baseStats);

            _currentHealth = Mathf.Max(1f, AbilitySystem.Attributes.GetCurrent(
                global::UPlayGround.Data.Stat.Attributes.Vital.MaxHealth));         // 풀 회복
            OnHpChanged?.Invoke(_currentHealth, _maxHealth);

            GetComponent<PoiseStat>()?.RecoverFull();
        }

        /// <summary>
        /// 지정 캐릭터의 현재 체력 반환. 한 번도 활성화된 적 없으면 최대 체력으로 취급한다.
        /// </summary>
        public float GetHealthForCharacter(CharacterActorType type)
        {
            if (type == _characterActorType) return _currentHealth;
            return TryGetStoredAttribute(
                type, global::UPlayGround.Data.Stat.Attributes.Vital.Health, out float hp)
                    ? hp
                    : GetMaxHealthForCharacter(type);
        }

        public bool HasHealthRecordForCharacter(CharacterActorType type)
            => type == _characterActorType
               || TryGetStoredAttribute(type, global::UPlayGround.Data.Stat.Attributes.Vital.Health, out _);

        /// <summary>
        /// 지정 캐릭터의 최대 체력 반환. 현재 캐릭터가 아니면 PlayerSwapBehaviour의 모델 데이터에서 조회한다.
        /// </summary>
        public float GetMaxHealthForCharacter(CharacterActorType type)
        {
            if (type == _characterActorType) return _maxHealth;

            IReadOnlyDictionary<AttributeId, float> effectiveStats =
                UPlayGround.Data.Party.CharacterEffectiveStatCalculator.Calculate(type);
            if (effectiveStats != null
                && effectiveStats.TryGetValue(
                    global::UPlayGround.Data.Stat.Attributes.Vital.MaxHealth, out float maxHealth))
                return Mathf.Max(1f, maxHealth);

            return Mathf.Max(
                1f,
                UPlayGroundAttributeDefaults.Get(global::UPlayGround.Data.Stat.Attributes.Vital.MaxHealth));
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

            // 벤치: 캐릭터별 ASC 스냅샷에 기록한다.
            float max = GetMaxHealthForCharacter(type);
            bool hasRecord = TryGetStoredAttribute(
                type, global::UPlayGround.Data.Stat.Attributes.Vital.Health, out float stored);
            if (!hasRecord) return;                       // 기록 없음 = 이미 풀피
            if (!reviveDowned && stored <= 0f) return;    // 부활 비활성 시 다운 제외
            SetStoredAttribute(type, global::UPlayGround.Data.Stat.Attributes.Vital.Health, max);
        }

        public float GetSkillGaugeForCharacter(CharacterActorType type)
        {
            if (type == _characterActorType) return _skillGauge != null ? _skillGauge.CurrentGauge : 0f;
            return TryGetStoredAttribute(
                type, global::UPlayGround.Data.Stat.Attributes.Resource.UltimateEnergy, out float gauge)
                    ? gauge
                    : 0f;
        }

        public AbilitySystemSaveData GetAbilitySystemForCharacter(CharacterActorType type)
        {
            if (type == CharacterActorType.None)
                return null;
            if (type == _characterActorType)
                return Abilities?.CaptureAbilitySystemStateForCharacter(
                    forCharacterSwap: false);
            return _characterAbilitySystemMap.TryGetValue(
                    type, out AbilitySystemSaveData data)
                ? data
                : null;
        }

        public void RestoreCharacterAbilitySystem(
            CharacterActorType type,
            AbilitySystemSaveData data)
        {
            if (type == CharacterActorType.None)
                return;
            if (type == _characterActorType)
            {
                Abilities?.RestoreAbilitySystemStateForCharacter(data);
                return;
            }

            if (data == null)
            {
                _characterAbilitySystemMap.Remove(type);
            }
            else
            {
                data.version = AbilitySystemSaveData.CurrentVersion;
                data.attributes ??= new List<AttributeSaveEntry>();
                data.cooldowns ??= new List<GasCooldownSaveEntry>();
                data.activeEffects ??= new List<ActiveEffectSaveEntry>();
                _characterAbilitySystemMap[type] = data;
            }
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

            CharacterModelData model = _swapBehaviour?.GetModelData(type);
            GameplayAbilitySO ability = skillSlot
                == PlayerAbilityResourceView.ElementalImbueSkillSlot
                    ? Svc.Party?.GetElementalImbueAbility(type)
                    : model?.abilitySet?.GetPlayerAbility(
                        (PlayerSkillSlot)skillSlot);
            if (ability == null)
                return false;

            float required = ability.cost?.policy switch
            {
                AbilityCostPolicy.None => 0f,
                AbilityCostPolicy.Fixed => Mathf.Max(0f, ability.cost.value),
                AbilityCostPolicy.All => GetSkillGaugeForCharacter(type),
                AbilityCostPolicy.PercentOfMax =>
                    Mathf.Max(
                        0f,
                        GetMaxSkillGaugeForCharacter(type)
                        * ability.cost.value),
                _ => 0f,
            };
            if (GetSkillGaugeForCharacter(type) < required)
                return false;

            if (GetStoredCooldownRemaining(
                    type,
                    ability.cooldown.ResolveGroupId(ability.abilityId)) > 0f)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 파티 HUD의 궁극기 "준비" 표시용 판정. 실제 발동은 비용(<see cref="PlayerAbilityResourceView.CanUseSkill"/>)으로
        /// 게이트하지만, 글로우 같은 준비 연출은 게이지가 가득 찼을 때만 켠다
        /// (스킬바의 <c>UISkillSlot._showOnlyWhenGaugeFull</c>와 동일 의미). 쿨타임 중이면 false.
        /// </summary>
        public bool IsUltimateReadyForCharacter(CharacterActorType type)
            => IsSkillGaugeFullForCharacter(type)
               && CanUseSkillForCharacter(type, PlayerAbilityResourceView.UltimateSkillSlot);

        public void AddSkillGaugeForCharacter(CharacterActorType type, float amount)
        {
            if (type == CharacterActorType.None || amount <= 0f || _skillGauge == null) return;

            if (type == _characterActorType)
            {
                _skillGauge.AddGauge(amount);
                return;
            }

            float max = _skillGauge.MaxGauge;
            float current = GetSkillGaugeForCharacter(type);
            SetStoredAttribute(
                type,
                global::UPlayGround.Data.Stat.Attributes.Resource.UltimateEnergy,
                Mathf.Clamp(current + amount, 0f, max));
        }

        public bool ConsumeFullSkillGaugeForCharacter(CharacterActorType type)
        {
            if (!IsSkillGaugeFullForCharacter(type)) return false;

            if (type == _characterActorType)
                _skillGauge.SetGauge(0f);
            else
                SetStoredAttribute(
                    type, global::UPlayGround.Data.Stat.Attributes.Resource.UltimateEnergy, 0f);

            return true;
        }

        public float GetConcertoForCharacter(CharacterActorType type)
        {
            if (type == CharacterActorType.None) return 0f;
            if (type == _characterActorType)
            {
                return AbilitySystem?.Attributes.GetCurrent(
                           global::UPlayGround.Data.Stat.Attributes.Resource.Concerto)
                       ?? 0f;
            }

            return TryGetStoredAttribute(
                type,
                global::UPlayGround.Data.Stat.Attributes.Resource.Concerto,
                out float concerto)
                    ? concerto
                    : 0f;
        }

        public float GetMaxConcertoForCharacter(CharacterActorType type)
        {
            if (type == CharacterActorType.None) return 0f;
            if (type == _characterActorType)
            {
                float activeMaximum = AbilitySystem?.Attributes.GetCurrent(
                    global::UPlayGround.Data.Stat.Attributes.Resource.MaxConcerto) ?? 0f;
                if (activeMaximum > 0f) return activeMaximum;
            }
            else if (TryGetStoredAttribute(
                         type,
                         global::UPlayGround.Data.Stat.Attributes.Resource.MaxConcerto,
                         out float storedMaximum)
                     && storedMaximum > 0f)
            {
                return storedMaximum;
            }

            return Mathf.Max(
                0f,
                UPlayGroundAttributeDefaults.Get(
                    global::UPlayGround.Data.Stat.Attributes.Resource.MaxConcerto));
        }

        public bool IsConcertoFullForCharacter(CharacterActorType type)
            => UPlayGround.Data.Party.PartyConcertoPolicy.IsReady(
                GetConcertoForCharacter(type),
                GetMaxConcertoForCharacter(type));

        public bool HasSwapSpecialAbilityForCharacter(CharacterActorType type)
        {
            CharacterModelData model = _swapBehaviour?.GetModelData(type);
            return model?.abilitySet?.GetCombatAbility(
                       PlayerCombatAbilitySlot.SwapSpecialAttack) != null;
        }

        public void AddConcertoForCharacter(CharacterActorType type, float amount)
        {
            if (type == CharacterActorType.None || amount <= 0f) return;

            if (type == _characterActorType)
            {
                AbilitySystem?.ApplyResourceDelta(
                    AbilityResourceType.Concerto,
                    amount,
                    "GE_Party.ConcertoGain");
                return;
            }

            float maximum = GetMaxConcertoForCharacter(type);
            float current = GetConcertoForCharacter(type);
            SetStoredAttribute(
                type,
                global::UPlayGround.Data.Stat.Attributes.Resource.Concerto,
                Mathf.Clamp(current + amount, 0f, maximum));
        }

        public bool ConsumeFullConcertoForCharacter(CharacterActorType type)
        {
            if (!IsConcertoFullForCharacter(type)) return false;

            if (type == _characterActorType)
            {
                return AbilitySystem != null
                       && AbilitySystem.TryApplyResourceCost(
                           AbilityResourceType.Concerto,
                           GetMaxConcertoForCharacter(type));
            }

            SetStoredAttribute(
                type,
                global::UPlayGround.Data.Stat.Attributes.Resource.Concerto,
                0f);
            return true;
        }

        private AbilitySystemSaveData GetOrCreateStoredState(CharacterActorType type)
        {
            if (!_characterAbilitySystemMap.TryGetValue(
                    type, out AbilitySystemSaveData data))
            {
                data = new AbilitySystemSaveData();
                _characterAbilitySystemMap[type] = data;
            }
            return data;
        }

        private bool TryGetStoredAttribute(
            CharacterActorType type,
            AttributeId attributeId,
            out float value)
        {
            value = 0f;
            if (!_characterAbilitySystemMap.TryGetValue(
                    type, out AbilitySystemSaveData data)
                || data?.attributes == null)
                return false;

            for (int i = 0; i < data.attributes.Count; i++)
            {
                AttributeSaveEntry entry = data.attributes[i];
                if (entry == null
                    || !string.Equals(
                        entry.attributeId,
                        attributeId.Value,
                        StringComparison.Ordinal))
                    continue;
                value = entry.baseValue;
                return true;
            }
            return false;
        }

        private void SetStoredAttribute(
            CharacterActorType type,
            AttributeId attributeId,
            float value)
        {
            AbilitySystemSaveData data = GetOrCreateStoredState(type);
            for (int i = 0; i < data.attributes.Count; i++)
            {
                AttributeSaveEntry entry = data.attributes[i];
                if (entry == null
                    || !string.Equals(
                        entry.attributeId,
                        attributeId.Value,
                        StringComparison.Ordinal))
                    continue;
                entry.baseValue = value;
                return;
            }
            data.attributes.Add(new AttributeSaveEntry(attributeId.Value, value));
        }

        private float GetStoredCooldownRemaining(
            CharacterActorType type,
            string groupId)
        {
            if (!_characterAbilitySystemMap.TryGetValue(
                    type, out AbilitySystemSaveData data)
                || data?.cooldowns == null)
                return 0f;
            for (int i = 0; i < data.cooldowns.Count; i++)
            {
                GasCooldownSaveEntry entry = data.cooldowns[i];
                if (entry != null
                    && string.Equals(
                        entry.groupId, groupId, StringComparison.Ordinal))
                    return Mathf.Max(0f, entry.remainingSeconds);
            }
            return 0f;
        }

        private void InitComponents()
        {
            if (_combat     == null) _combat     = GetComponent<PlayerCombat>();
            if (_equipment  == null) _equipment  = GetComponentInChildren<PlayerEquipment>();
            if (_skillGauge == null) _skillGauge = GetComponent<PlayerAbilityResourceView>();
            if (_stamina == null)
            {
                PlayerStaminaSettingsSO settings = PlayerStaminaSettingsSO.Load();
                if (settings != null)
                {
                    _stamina = new PlayerStaminaRuntime(
                        this,
                        AbilitySystem,
                        settings);
                    _stamina.Changed += HandleStaminaChanged;
                }
                else
                {
                    Debug.LogError(
                        "[PlayerActor] Resources/PlayerStaminaSettings.asset을 찾지 못했습니다.",
                        this);
                }
            }
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

        private void HandleStaminaChanged(float current, float maximum) =>
            OnStaminaChanged?.Invoke(current, maximum);

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
