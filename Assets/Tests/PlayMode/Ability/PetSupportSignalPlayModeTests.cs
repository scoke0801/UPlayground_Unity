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
    }
}
#endif
