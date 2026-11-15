#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using KinematicCharacterController;
using UPlayGround.Ability.Core;
using UPlayGround.Components.Pet;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Pet;
using UPlayGround.Data.Stat;
using UPlayGround.Gameplay.Ability;
using UPlayGround.Gameplay.Tag;
using UPlayGround.MovementController;
using UPlayGround.Components;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.Data.Quest;
using UPlayGround.Data.Combat;

namespace UPlayGround.Ability.PlayModeTests
{
    /// <summary>월드 입력과 분리해 실제 플레이어 이벤트부터 지원 GAS 확정까지 검사한다.</summary>
    public sealed class PetSupportSignalPlayModeTests
    {
        private GameObject _root;
        private PlayerActor _player;
        private PetActor _pet;
        private PetBrain _brain;
        private KinematicCharacterMotor _petMotor;
        private PetSkillCatalogSO _catalog;
        private PetDefinitionSO _definition;
        private PetFollowProfileSO _profile;
        private PetSaveData _save;
        private PetSupportController _support;
        private float _timeScale;

        [SetUp]
        public void SetUp()
        {
            _timeScale = Time.timeScale; Time.timeScale = 1f;
            _root = new GameObject("펫 신호 수직 검사"); _root.SetActive(false);
            var playerObject = new GameObject("플레이어"); playerObject.transform.SetParent(_root.transform);
            _player = playerObject.AddComponent<PlayerActor>();
            var system = playerObject.AddComponent<AbilitySystemComponent>(); system.EnsureInitialized();
            system.InitializeDefaultAttributes();
            SetProperty(typeof(GameActor), _player, "AbilitySystem", system);
            SetProperty(typeof(GameActor), _player, "Abilities", system.ProjectAbilities);
            SetProperty(typeof(GameActor), _player, "Effects", system.ProjectEffects);
            SetProperty(typeof(GameActor), _player, "Tags", system.ProjectTags);
            var playerMovement = playerObject.AddComponent<PlayerMovementController>();
            AddGroundedMotor(playerMovement);
            typeof(GameActor).GetField("MovementController", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_player, playerMovement);
            var petObject = new GameObject("펫"); petObject.transform.SetParent(_root.transform);
            _pet = petObject.AddComponent<PetActor>();
            var petMovement = petObject.AddComponent<PetMovementController>(); AddGroundedMotor(petMovement);
            _definition = ScriptableObject.CreateInstance<PetDefinitionSO>();
            _profile = ScriptableObject.CreateInstance<PetFollowProfileSO>(); _definition.followProfile = _profile;
            _brain = new PetBrain(_pet, _player, petMovement, _definition, null, null);
            SetProperty(typeof(PetBrain), _brain, "HasSafeGround", true);
            // 생성 시 숨김으로 꺼진 모터를 안전 지상 합류 상태로 맞춘다.
            _petMotor = petMovement.Motor; _petMotor.enabled = true;
            typeof(PetActor).GetField("_brain", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_pet, _brain);
            _catalog = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PetSkillCatalogSO>("Assets/10.Datas/Pet/PetSkills.asset"));
            _catalog.supportObstructionMask = 0;
            _catalog.signatureFxKey = null;
            foreach (var skill in _catalog.skills) { skill.activateFxKey = null; skill.petActivateFxKey = null; }
            _save = PetSaveRules.Normalize(null);
            _support = new PetSupportController(_catalog);
            _support.SetPet(_pet); _support.Bind(_player, _save);
            SetHealth(100f);
            Assert.That(_support.IsAvailable, Is.True);
        }

        [TearDown]
        public void TearDown()
        {
            _support?.Dispose(_save);
            Object.DestroyImmediate(_root); Object.DestroyImmediate(_catalog);
            Object.DestroyImmediate(_definition); Object.DestroyImmediate(_profile);
            Time.timeScale = _timeScale;
        }

        private static void SetProperty(System.Type type, object owner, string name, object value)
            => type.GetField("<" + name + ">k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(owner, value);

        private static void AddGroundedMotor(ActorMovementController movement)
        {
            var motor = movement.gameObject.AddComponent<KinematicCharacterMotor>();
            motor.GroundingStatus.IsStableOnGround = true;
            SetProperty(typeof(ActorMovementController), movement, "Motor", motor);
        }

        private void SetHealth(float health)
        {
            _player.AbilitySystem.Attributes.SetBase(Attributes.Vital.MaxHealth, 100f);
            _player.AbilitySystem.Attributes.SetBase(Attributes.Vital.Health, health);
        }
        private void Select(string id) { _save.supportSlots[0] = id; }
        private void Perfect(PassiveActivationType kind = PassiveActivationType.PerfectDodge)
            => typeof(PlayerActor).GetMethod("NotifyPassiveActivation", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(_player, new object[] { kind });
        private void Hit(GameplayTag tag) => _player.Abilities.IssueTriggerEvent(tag, target: _player);
        private GameplayEffectSO Effect(string id) => _catalog.Find(id).GetLevel(1).Effect;
        private string Group(string id) => _catalog.Find(id).supportAbility.cooldown.cooldownGroupId;
        private void ExpireMinimumInterval()
            => typeof(PetSupportController).GetField("_minimumIntervalEnds", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_support, Time.time);

        [TestCase(PassiveActivationType.PerfectDodge)]
        [TestCase(PassiveActivationType.PerfectGuard)]
        public void 완벽방어_신호는_기세를_한번만_확정한다(PassiveActivationType kind)
        {
            Select(PetSkillCatalogSO.AttackBuff);
            _support.Tick(_pet, _save);
            Assert.That(_support.ActivationVersion, Is.Zero, "시간 점검은 사건을 만들지 않는다.");
            Perfect(kind);
            Assert.That(_player.Effects.HasActiveEffect(Effect(PetSkillCatalogSO.AttackBuff)), Is.True);
            Perfect(kind); Assert.That(_support.ActivationVersion, Is.EqualTo(1));
        }

        [Test]
        public void 체력35퍼센트_이하에서만_응급회복한다()
        {
            Select(PetSkillCatalogSO.Heal);
            SetHealth(36f); _support.Tick(_pet, _save); Assert.That(_player.CurrentHealth, Is.EqualTo(36f));
            SetHealth(35f); _support.Tick(_pet, _save); Assert.That(_player.CurrentHealth, Is.EqualTo(43f).Within(.001f));
            Assert.That(_player.AbilitySystem.Runtime.Cooldowns.GetRemaining(Group(PetSkillCatalogSO.Heal)), Is.GreaterThan(44f));
        }

        [Test]
        public void 큰피격만_버팀을_발동한다()
        {
            Select(PetSkillCatalogSO.DefenseBuff);
            Hit(GameplayTags.Trigger_Player_Hit_Hit); Hit(GameplayTags.Trigger_Player_Hit_Light); Hit(GameplayTags.Trigger_Player_Hit_Pull);
            Assert.That(_support.ActivationVersion, Is.Zero);
            Hit(GameplayTags.Trigger_Player_Hit_Heavy);
            Assert.That(_player.Effects.HasActiveEffect(Effect(PetSkillCatalogSO.DefenseBuff)), Is.True);
            Assert.That(_player.AbilitySystem.Runtime.Cooldowns.GetRemaining(Group(PetSkillCatalogSO.DefenseBuff)), Is.GreaterThan(14f));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void 지원불가_신호는_대기열과_쿨다운을_남기지_않는다(bool perfect)
        {
            string id = perfect ? PetSkillCatalogSO.AttackBuff : PetSkillCatalogSO.DefenseBuff;
            Select(id); _brain.SetSuspended(true);
            if (perfect) Perfect(); else Hit(GameplayTags.Trigger_Player_Hit_Heavy);
            Assert.That(_support.ActivationVersion, Is.Zero);
            Assert.That(_player.AbilitySystem.Runtime.Cooldowns.GetRemaining(Group(id)), Is.Zero);
            _support.Capture(_save); Assert.That(_save.supportMinimumIntervalRemaining, Is.Zero);
            _brain.SetSuspended(false); _support.Tick(_pet, _save);
            _petMotor.enabled = true;
            Assert.That(_support.ActivationVersion, Is.Zero, "복귀해도 버린 사건은 재생하지 않는다.");
            if (perfect) Perfect(); else Hit(GameplayTags.Trigger_Player_Hit_Heavy);
            Assert.That(_support.ActivationVersion, Is.EqualTo(1));
        }

        [Test]
        public void 슬롯의_다른지원은_전역간격만_공유하고_파티교체는_효과를_유지한다()
        {
            Select(PetSkillCatalogSO.AttackBuff); Perfect();
            _player.Abilities.HandleCharacterSwap(); _player.Abilities.ClearCharacterCooldowns();
            _player.Abilities.RestoreAbilitySystemStateForCharacter(new AbilitySystemSaveData());
            Assert.That(_player.Effects.HasActiveEffect(Effect(PetSkillCatalogSO.AttackBuff)), Is.True);
            Select(PetSkillCatalogSO.DefenseBuff); Hit(GameplayTags.Trigger_Player_Hit_Heavy);
            Assert.That(_support.ActivationVersion, Is.EqualTo(1));
            Assert.That(_player.AbilitySystem.Runtime.Cooldowns.GetRemaining(Group(PetSkillCatalogSO.DefenseBuff)), Is.Zero);
            ExpireMinimumInterval(); Hit(GameplayTags.Trigger_Player_Hit_Heavy);
            Assert.That(_support.ActivationVersion, Is.EqualTo(2));
            Assert.That(_player.AbilitySystem.Runtime.Cooldowns.GetRemaining(Group(PetSkillCatalogSO.AttackBuff)), Is.GreaterThan(9f));
        }

        [Test]
        public void 활성효과는_쿨다운이_끝나도_다시_발동하지_않고_구독해제후_신호는_무시한다()
        {
            Select(PetSkillCatalogSO.AttackBuff); Perfect();
            _player.AbilitySystem.Runtime.Cooldowns.Clear(); ExpireMinimumInterval();
            Perfect(); Assert.That(_support.ActivationVersion, Is.EqualTo(1));
            _player.Effects.RemoveAll(); _support.Dispose(_save);
            Perfect(); Hit(GameplayTags.Trigger_Player_Hit_Heavy);
            Assert.That(_support.ActivationVersion, Is.EqualTo(1));
        }

        [Test]
        public void 저장복원은_미장착지원의_쿨다운과_전역간격도_유지한다()
        {
            Select(PetSkillCatalogSO.AttackBuff); Perfect(); _support.Dispose(_save);
            _player.Effects.RemoveAll(); _player.AbilitySystem.Runtime.Cooldowns.Clear();
            _save = PetSaveRules.Normalize(_save);
            _support = new PetSupportController(_catalog); _support.SetPet(_pet); _support.Bind(_player, _save);
            Assert.That(_player.AbilitySystem.Runtime.Cooldowns.GetRemaining(Group(PetSkillCatalogSO.AttackBuff)), Is.GreaterThan(9f));
            Select(PetSkillCatalogSO.DefenseBuff); Hit(GameplayTags.Trigger_Player_Hit_Heavy);
            Assert.That(_support.ActivationVersion, Is.Zero);
            _support.Capture(_save); Assert.That(_save.supportMinimumIntervalRemaining, Is.GreaterThan(7f));
            ExpireMinimumInterval(); Hit(GameplayTags.Trigger_Player_Hit_Heavy);
            Assert.That(_support.ActivationVersion, Is.EqualTo(1), "저장 당시 준비된 지원은 복원 뒤에도 사용할 수 있다.");
        }

        [Test]
        public void 두번째_슬롯은_해금_이후에만_반응하고_전역간격은_공유한다()
        {
            Select(PetSkillCatalogSO.Heal); _save.supportSlots.Add(PetSkillCatalogSO.AttackBuff);
            Perfect(); Assert.That(_support.ActivationVersion, Is.Zero);
            _save.sharedBondPoints = _catalog.bondProgression.secondSlotPoints;
            Perfect(); Assert.That(_support.LastActivatedSlot, Is.EqualTo(1));
            Assert.That(_support.ActivationVersion, Is.EqualTo(1));
            SetHealth(20f); _support.Tick(_pet, _save); Assert.That(_player.CurrentHealth, Is.EqualTo(20f));
            ExpireMinimumInterval(); _support.Tick(_pet, _save); Assert.That(_player.CurrentHealth, Is.EqualTo(28f).Within(.001f));
            Assert.That(_player.AbilitySystem.Runtime.Cooldowns.GetRemaining(Group(PetSkillCatalogSO.AttackBuff)), Is.GreaterThan(9f));
        }
        [Test]
        public void 중복계열의_두번째_슬롯은_잘못된_세이브에서도_실행하지_않는다()
        {
            Select(PetSkillCatalogSO.Heal); _save.supportSlots.Add(PetSkillCatalogSO.AttackBuff);
            _save.sharedBondPoints = 100;
            _catalog.Find(PetSkillCatalogSO.AttackBuff).family = _catalog.Find(PetSkillCatalogSO.Heal).family;
            Perfect(); Assert.That(_support.ActivationVersion, Is.Zero);
        }
        [Test]
        public void 시그니처는_3단계에만_발동하고_교체와_저장에도_독립대기를_유지한다()
        {
            _save.sharedBondPoints = _catalog.bondProgression.signaturePoints;
            Select(PetSkillCatalogSO.AttackBuff); Perfect();
            var signature = _catalog.signatureAbility;
            Assert.That(_player.AbilitySystem.Attributes.GetCurrent(Attributes.Combat.AttackSpeed), Is.EqualTo(1.03f).Within(.001f));
            Assert.That(_support.SignatureActivationVersion, Is.EqualTo(1));
            Assert.That(_player.Effects.HasActiveEffect(signature.variants[0].targetEffects[0]), Is.True);
            _player.Abilities.HandleCharacterSwap();
            Assert.That(_player.Effects.HasActiveEffect(signature.variants[0].targetEffects[0]), Is.True);
            _support.Dispose(_save); _player.Effects.RemoveAll(); _player.AbilitySystem.Runtime.Cooldowns.Clear();
            _save = PetSaveRules.Normalize(_save);
            _support = new PetSupportController(_catalog); _support.SetPet(_pet); _support.Bind(_player, _save);
            Assert.That(_player.AbilitySystem.Runtime.Cooldowns.GetRemaining(signature.cooldown.cooldownGroupId), Is.GreaterThan(59f));
            _save.sharedBondPoints = 100; ExpireMinimumInterval(); Select(PetSkillCatalogSO.DefenseBuff);
            Hit(GameplayTags.Trigger_Player_Hit_Heavy); Assert.That(_support.SignatureActivationVersion, Is.Zero);
        }
        [Test]
        public void 실제_브레이크만_교란하고_중복노출과_무원인노출은_쿨다운을_쓰지_않는다()
        {
            var enemy = CreateEnemy(out var gauge, out var data);
            try
            {
                Select(PetSkillCatalogSO.Disruption);
                _catalog.Find(PetSkillCatalogSO.Disruption).triggerPolicy.requiresCombat = false;
                gauge.ForceExpose(); Assert.That(_support.ActivationVersion, Is.Zero);
                gauge.RecoverFromExpose(false); Break(gauge, enemy);
                Assert.That(_support.ActivationVersion, Is.EqualTo(1));
                Assert.That(enemy.Effects.HasActiveEffect(Effect(PetSkillCatalogSO.Disruption)), Is.True);
                Assert.That(enemy.AbilitySystem.Attributes.GetCurrent(Attributes.Vital.PoiseRecoveryDelay), Is.GreaterThan(0f));
                Assert.That(enemy.AbilitySystem.Attributes.GetCurrent(Attributes.Vital.Health), Is.EqualTo(100f), "교란은 피해를 주지 않는다.");
                Break(gauge, enemy); Assert.That(_support.ActivationVersion, Is.EqualTo(1));
                Assert.That(_player.AbilitySystem.Runtime.Cooldowns.GetRemaining(Group(PetSkillCatalogSO.Disruption)), Is.GreaterThan(19f));
            }
            finally { Object.DestroyImmediate(data); }
        }
        [Test]
        public void 지원불가_브레이크는_폐기하고_복귀후_재생하지_않는다()
        {
            var enemy = CreateEnemy(out var gauge, out var data);
            try
            {
                Select(PetSkillCatalogSO.Disruption); _catalog.Find(PetSkillCatalogSO.Disruption).triggerPolicy.requiresCombat = false;
                _brain.SetSuspended(true); Break(gauge, enemy);
                Assert.That(_support.ActivationVersion, Is.Zero);
                Assert.That(_player.AbilitySystem.Runtime.Cooldowns.GetRemaining(Group(PetSkillCatalogSO.Disruption)), Is.Zero);
                _brain.SetSuspended(false); _petMotor.enabled = true; _support.Tick(_pet, _save);
                Assert.That(_support.ActivationVersion, Is.Zero);
                gauge.RecoverFromExpose(false); Break(gauge, enemy);
                Assert.That(_support.ActivationVersion, Is.EqualTo(1));
            }
            finally { Object.DestroyImmediate(data); }
        }
        private MonsterActor CreateEnemy(out MonsterBreakGauge gauge, out MonsterBreakGaugeSO data)
        {
            var go = new GameObject("브레이크 검사 대상"); go.transform.SetParent(_root.transform);
            var enemy = go.AddComponent<MonsterActor>();
            var system = go.AddComponent<AbilitySystemComponent>(); system.EnsureInitialized(); system.InitializeDefaultAttributes();
            SetProperty(typeof(GameActor), enemy, "AbilitySystem", system);
            SetProperty(typeof(GameActor), enemy, "Abilities", system.ProjectAbilities);
            SetProperty(typeof(GameActor), enemy, "Effects", system.ProjectEffects);
            system.Attributes.SetBase(Attributes.Vital.MaxHealth, 100f); system.Attributes.SetBase(Attributes.Vital.Health, 100f);
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(GameActor).GetField("_actorType", flags).SetValue(_player, ActorType.Player);
            typeof(GameActor).GetField("_actorType", flags).SetValue(enemy, ActorType.Monster);
            gauge = go.AddComponent<MonsterBreakGauge>();
            typeof(MonsterBreakGauge).GetField("_owner", flags).SetValue(gauge, enemy);
            data = ScriptableObject.CreateInstance<MonsterBreakGaugeSO>(); gauge.Init(data);
            return enemy;
        }
        private void Break(MonsterBreakGauge gauge, MonsterActor enemy)
        {
            var attack = new AttackData { attacker = _player, breakDamage = 1000f };
            var request = HitRequest.FromAttackData(attack);
            var hit = HitContext.Create(request, enemy); gauge.TakeBreakDamage(hit);
        }

        [Test]
        public void 퀘스트와_교전_실제이벤트는_유효성과_중복을_검사하고_구독해제한다()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var managerType = System.Type.GetType("UPlayGround.Manager.PetManager, Assembly-CSharp", true);
            var eventsType = System.Type.GetType("UPlayGround.Manager.EventManager, Assembly-CSharp", true);
            var sceneType = System.Type.GetType("UPlayGround.Manager.SceneManager, Assembly-CSharp", true);
            var saveType = System.Type.GetType("UPlayGround.Manager.SaveManager, Assembly-CSharp", true);
            var manager = _root.AddComponent(managerType);
            var events = _root.AddComponent(eventsType);
            var scene = _root.AddComponent(sceneType);
            var save = _root.AddComponent(saveType);
            var sceneField = typeof(BaseManager<>).MakeGenericType(sceneType).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var saveField = typeof(BaseManager<>).MakeGenericType(saveType).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previousScene = sceneField.GetValue(null); var previousSave = saveField.GetValue(null);
            Services.TryGet<IGameEventObservable>(out var previousEvents);
            Services.TryGet<ICombatEncounterService>(out var previousEncounters);
            var encounters = new PetTestEncounterService();
            System.Action<PetSupportTriggerKind> applied = null;
            var catalog = ScriptableObject.CreateInstance<PetCatalogSO>(); catalog.skills = _catalog;
            try
            {
                sceneField.SetValue(null, scene); saveField.SetValue(null, save);
                if (previousEvents != null) Services.Unregister(previousEvents);
                Services.Register((IGameService)events);
                if (previousEncounters != null) Services.Unregister(previousEncounters);
                Services.Register(encounters);
                managerType.GetField("_save", flags).SetValue(manager, _save);
                managerType.GetField("_actor", flags).SetValue(manager, _pet);
                managerType.GetField("_owner", flags).SetValue(manager, _player);
                managerType.GetField("_ready", flags).SetValue(manager, true);
                managerType.GetField("_sceneType", flags).SetValue(manager, SceneType.Test);
                managerType.GetField("<Catalog>k__BackingField", flags).SetValue(manager, catalog);
                managerType.GetField("<State>k__BackingField", flags).SetValue(manager, PetRuntimeState.Suspended);
                _save.isAccompanying = true;
                managerType.GetMethod("SubscribeBond", flags).Invoke(manager, null);
                var publisher = (IGameEventPublisher)events;
                var objective = new QuestObjectiveEventData { QuestId = "Quest", ObjectiveId = "Goal", RequiredCount = 2, CurrentCount = 1 };
                publisher.Send(QuestEvent.QuestObjectiveUpdated, objective); Assert.That(_save.sharedBondPoints, Is.Zero);
                objective.CurrentCount = 2;
                publisher.Send(QuestEvent.QuestObjectiveUpdated, objective);
                publisher.Send(QuestEvent.QuestObjectiveUpdated, objective);
                Assert.That(_save.sharedBondPoints, Is.EqualTo(5));
                eventsType.GetMethod("OnSceneChanged").Invoke(events, new object[] { SceneType.Test });
                publisher.Send(QuestEvent.QuestCompleted, new QuestStateEventData { QuestId = "Quest" });
                Assert.That(_save.sharedBondPoints, Is.EqualTo(20));
                Time.timeScale = 0f;
                publisher.Send(QuestEvent.QuestCompleted, new QuestStateEventData { QuestId = "Paused" });
                Assert.That(_save.bondLedger.claims.Count, Is.EqualTo(2));
                Time.timeScale = 1f; _save.isAccompanying = false;
                publisher.Send(QuestEvent.QuestCompleted, new QuestStateEventData { QuestId = "Paused" });
                Assert.That(_save.sharedBondPoints, Is.EqualTo(20));
                _save.isAccompanying = true;
                publisher.Send(QuestEvent.QuestCompleted, new QuestStateEventData { QuestId = "Paused" });
                Assert.That(_save.sharedBondPoints, Is.EqualTo(35));
                Assert.That(_save.bondLedger.windowActiveSeconds, Is.Zero, "퀘스트는 시간 기반 활동 창을 충전하지 않는다.");
                managerType.GetField("<State>k__BackingField", flags).SetValue(manager, PetRuntimeState.Ready);
                encounters.Change(CombatEncounterKind.Sweep);
                Assert.That(_save.bondLedger.encounterActive, Is.True);
                applied = (System.Action<PetSupportTriggerKind>)System.Delegate.CreateDelegate(
                    typeof(System.Action<PetSupportTriggerKind>), manager, managerType.GetMethod("OnBondSupport", flags));
                _support.SupportApplied += applied;
                _player.AbilitySystem.Attributes.SetBase(Attributes.Combat.AttackPower, 100f);
                Select(PetSkillCatalogSO.AttackBuff); Perfect();
                Assert.That(_save.sharedBondPoints, Is.EqualTo(37));
                _save.bondLedger.encounterSeconds = 20f; _save.bondLedger.encounterPresentSeconds = 10f;
                encounters.Change(CombatEncounterKind.Explore);
                Assert.That(_save.sharedBondPoints, Is.EqualTo(38), "교전 종료는 동행 비율만큼 보너스를 준다.");
                managerType.GetMethod("UnsubscribeBond", flags).Invoke(manager, null);
                publisher.Send(QuestEvent.QuestCompleted, new QuestStateEventData { QuestId = "AfterDispose" });
                encounters.Change(CombatEncounterKind.Sweep);
                Assert.That(_save.sharedBondPoints, Is.EqualTo(38));
                Assert.That(_save.bondLedger.encounterActive, Is.False);
            }
            finally
            {
                managerType.GetMethod("UnsubscribeBond", flags).Invoke(manager, null);
                Services.Unregister((IGameService)events);
                Services.Unregister(encounters);
                if (previousEncounters != null) Services.Register(previousEncounters);
                if (previousEvents != null) Services.Register(previousEvents);
                if (applied != null) _support.SupportApplied -= applied;
                sceneField.SetValue(null, previousScene); saveField.SetValue(null, previousSave);
                Object.DestroyImmediate(catalog);
            }
        }
        private sealed class PetTestEncounterService : ICombatEncounterService
        {
            public CombatEncounterKind CurrentEncounter { get; private set; }
            public CombatEncounterProfileSO EncounterProfile => null;
            public bool IsBossAttacking => false;
            public event System.Action<CombatEncounterKind> EncounterChanged;
            public void Change(CombatEncounterKind kind) { CurrentEncounter = kind; EncounterChanged?.Invoke(kind); }
            public void SetArenaActive(string arenaId, bool active) { }
            public void ResetEncounter() => Change(CombatEncounterKind.Explore);
        }
    }
}
#endif
