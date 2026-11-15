#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.AI.BehaviorTree;
using UPlayGround.Data.Pet;
using UPlayGround.Data.Stat;
using UPlayGround.Data.EnumType;
using UPlayGround.Gameplay.Ability;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.Ability.PlayModeTests
{
    public sealed class PetDistractionPlayModeTests
    {
        [Test]
        public void 교란은_다음공격만_막고_강제회전과_만료후_대기를_남기지_않는다()
        {
            var enemyObject = new GameObject("교란 AI 검사"); enemyObject.SetActive(false);
            var petObject = new GameObject("주의를 끄는 펫"); petObject.SetActive(false);
            var request = ScriptableObject.CreateInstance<RequestEnemyActionNode>();
            var groundSlot = ScriptableObject.CreateInstance<RequestEnemyAttackSlotNode>();
            var flyingSlot = ScriptableObject.CreateInstance<RequestFlyingAttackSlotNode>();
            try
            {
                var enemy = enemyObject.AddComponent<PetDistractionTestMonster>();
                var system = enemyObject.AddComponent<AbilitySystemComponent>(); system.EnsureInitialized(); system.InitializeDefaultAttributes();
                SetProperty(typeof(GameActor), enemy, "AbilitySystem", system);
                SetProperty(typeof(GameActor), enemy, "Abilities", system.ProjectAbilities);
                SetProperty(typeof(GameActor), enemy, "Effects", system.ProjectEffects);
                system.Attributes.SetBase(Attributes.Vital.MaxHealth, 100f); system.Attributes.SetBase(Attributes.Vital.Health, 100f);
                enemyObject.AddComponent<KinematicCharacterController.KinematicCharacterMotor>();
                var movement = enemyObject.AddComponent<EnemyMovementController>();
                typeof(GameActor).GetField("MovementController", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(enemy, movement);
                var pet = petObject.AddComponent<PetActor>(); petObject.transform.position = Vector3.right * 3f;
                petObject.SetActive(true); enemyObject.SetActive(true);
                var effect = AssetDatabase.LoadAssetAtPath<PetDisruptionEffectSO>("Assets/10.Datas/Pet/GE_Pet_Disruption_L1.asset");
                Assert.That(enemy.Effects.TryApplyEffect(effect, pet, out _), Is.True);
                enemy.ApplyPetDistraction(pet, effect);
                Assert.That(enemy.IsPetDistractionActive, Is.True);
                var idle = new PetDistractionTestState(movement, ActorStateId.Idle, false);
                SetState(movement, idle);
                Assert.That(enemy.TryGetPetDistractionRotation(Quaternion.identity, .1f, out var rotation), Is.True);
                Assert.That(Quaternion.Angle(rotation, Quaternion.identity), Is.GreaterThan(0f));
                var attack = new PetDistractionTestState(movement, ActorStateId.Attack, false); SetState(movement, attack);
                Assert.That(enemy.TryGetPetDistractionRotation(Quaternion.identity, .1f, out _), Is.False);
                Assert.That(movement.CurrentState, Is.SameAs(attack));
                SetState(movement, new PetDistractionTestState(movement, ActorStateId.Idle, true));
                Assert.That(enemy.TryGetPetDistractionRotation(Quaternion.identity, .1f, out _), Is.False);
                SetState(movement, idle);
                var blackboard = new Blackboard();
                blackboard.SetString(EnemyBlackboardKeys.ResolverFailureReason, "아직 공격을 요청하지 않음");
                var context = new BehaviorTreeContext(enemyObject, blackboard);
                request.Intent = EnemyActionIntent.Attack; request.Initialize(context);
                groundSlot.Initialize(context); flyingSlot.Initialize(context);
                Assert.That(groundSlot.Tick(), Is.EqualTo(BTStatus.Failure));
                Assert.That(flyingSlot.Tick(), Is.EqualTo(BTStatus.Failure));
                SetState(movement, attack); blackboard.SetBool(EnemyBlackboardKeys.HasAttackSlot, true);
                EnemyActionResolver.TryTransition(context, new EnemyActionRequest(EnemyActionIntent.Attack), true, out _);
                blackboard.TryGetBool(EnemyBlackboardKeys.HasAttackSlot, out bool held);
                Assert.That(held, Is.True, "기존 공격 슬롯을 빼앗지 않는다.");
                Assert.That(movement.CurrentState, Is.SameAs(attack));
                SetState(movement, idle);
                Assert.That(EnemyActionResolver.TryTransition(context, new EnemyActionRequest(EnemyActionIntent.Attack), true,
                    out var blocked), Is.False);
                blackboard.TryGetBool(EnemyBlackboardKeys.HasAttackSlot, out held);
                Assert.That(held, Is.False, "미확정 공격의 예약 표시를 남기지 않는다.");
                Assert.That(blocked, Is.EqualTo("펫의 움직임에 주의가 끌려 새 공격을 늦춥니다."));
                Assert.That(request.Tick(), Is.EqualTo(BTStatus.Failure));
                blackboard.TryGetString(EnemyBlackboardKeys.ResolverFailureReason, out var before);
                Assert.That(before, Is.EqualTo("아직 공격을 요청하지 않음"));
                typeof(MonsterActor).GetField("_distractionEnds", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, Time.time - 1f);
                Assert.That(enemy.IsPetDistractionActive, Is.False);
                Assert.That(groundSlot.Tick(), Is.EqualTo(BTStatus.Success));
                Assert.That(flyingSlot.Tick(), Is.EqualTo(BTStatus.Success));
                request.Tick();
                blackboard.TryGetString(EnemyBlackboardKeys.ResolverFailureReason, out var after);
                Assert.That(after, Is.Not.EqualTo("아직 공격을 요청하지 않음"));
                enemy.ApplyPetDistraction(pet, effect); enemy.Effects.RemoveAll();
                Assert.That(enemy.IsPetDistractionActive, Is.False);
                enemy.Effects.TryApplyEffect(effect, pet, out _); enemy.ApplyPetDistraction(pet, effect);
                enemyObject.SetActive(false); Assert.That(enemy.IsPetDistractionActive, Is.False);
                enemyObject.SetActive(true); Assert.That(enemy.IsPetDistractionActive, Is.False, "풀 재사용은 과거 주의를 되살리지 않는다.");
            }
            finally { Object.DestroyImmediate(request); Object.DestroyImmediate(groundSlot); Object.DestroyImmediate(flyingSlot);
                Object.DestroyImmediate(enemyObject); Object.DestroyImmediate(petObject); }
        }
        private static void SetProperty(System.Type type, object owner, string name, object value)
            => type.GetField("<" + name + ">k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(owner, value);
        private static void SetState(ActorMovementController movement, GameActorState state)
            => typeof(ActorMovementController).GetField("_currentState", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(movement, state);
    }

    /// <summary>월드 생성 없이 실제 효과·AI 결정 경계만 검사하는 액터다.</summary>
    public sealed class PetDistractionTestMonster : MonsterActor
    {
        protected override void Awake() { }
        protected override void Start() { }
    }
    public sealed class PetDistractionTestState : GameActorState
    {
        private readonly ActorStateId _id;
        private readonly bool _blocked;
        public PetDistractionTestState(ActorMovementController controller, ActorStateId id, bool blocked) : base(controller)
        { _id = id; _blocked = blocked; }
        public override ActorStateId StateId => _id;
        public override bool BlocksBehaviorTree => _blocked;
        public override bool CanTransitionState(ActorStateId fromState) => true;
    }
}
#endif
