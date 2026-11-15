using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Data.Party;
using UPlayGround.Data.Event;
using UPlayGround.Manager;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.Combat.Tests
{
    /// <summary>시그니처 구간의 소유권과 대상 없는 투사체 좌표를 확인한다.</summary>
    public sealed class SignatureMotionEventTests
    {
        private readonly List<UnityEngine.Object> _objects = new();
        private readonly List<IGameService> _services = new();
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [TearDown]
        public void TearDown()
        {
            for (int i = _services.Count - 1; i >= 0; i--) Services.Unregister(_services[i]);
            for (int i = _objects.Count - 1; i >= 0; i--)
                if (_objects[i] != null) UnityEngine.Object.DestroyImmediate(_objects[i]);
            _services.Clear();
            _objects.Clear();
        }

        [Test]
        public void 입력이동_이전구간종료가_새구간을_닫지않는다()
        {
            PlayerActor player = NewPlayer();
            PlayerAttackState state = NewAttackState(player);
            object previous = new(), current = new();
            state.BeginInputMovement(previous, 0.3f, 90f);
            state.BeginInputMovement(current, 0.55f, 180f);
            state.EndInputMovement(previous);
            Assert.That(typeof(PlayerAttackState).GetField("_inputMovementSource", PrivateInstance).GetValue(state), Is.SameAs(current));
            state.EndInputMovement(current);
            Assert.That(typeof(PlayerAttackState).GetField("_inputMovementSource", PrivateInstance).GetValue(state), Is.Null);
        }

        [Test]
        public void 도약중력은_기존상승하강배율에_곱하고_구간끝에서_복구한다()
        {
            PlayerActor player = NewPlayer();
            PlayerAttackState state = NewAttackState(player);
            float rise = state.GetGravityMultiplier(1f), fall = state.GetGravityMultiplier(-1f);
            object previous = new(), current = new();
            state.BeginGravityOverride(previous, 0.6f);
            state.BeginGravityOverride(current, 0.3f);
            state.EndGravityOverride(previous);
            Assert.That(state.GetGravityMultiplier(1f), Is.EqualTo(rise * 0.3f).Within(0.0001f));
            Assert.That(state.GetGravityMultiplier(-1f), Is.EqualTo(fall * 0.3f).Within(0.0001f));
            state.EndGravityOverride(current);
            Assert.That(state.GetGravityMultiplier(1f), Is.EqualTo(rise));
            Assert.That(state.GetGravityMultiplier(-1f), Is.EqualTo(fall));
        }

        [Test]
        public void 대상없는_플레이어투사체는_높은발사점이아닌_액터루트의_전방을쓴다()
        {
            PlayerActor player = NewPlayer();
            player.transform.position = new Vector3(3f, 2f, 5f);
            player.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            var motionEvent = new SpawnProjectileEvent
            {
                targetMode = ProjectileTargetMode.TargetPosition,
                playerTargetFallbackDistance = 12f
            };
            object[] arguments = { player, new Vector3(9f, 20f, 9f), Vector3.forward, null };
            bool found = (bool)typeof(SpawnProjectileEvent).GetMethod("TryResolveTargetPosition", PrivateInstance).Invoke(motionEvent, arguments);
            Assert.That(found, Is.True);
            Assert.That(Vector3.Distance((Vector3)arguments[3], new Vector3(15f, 2f, 5f)), Is.LessThan(0.001f));
        }

        [Test]
        public void 무기조준IK가없어도_원거리락온을_정면폴백보다_우선한다()
        {
            PlayerActor player = NewPlayer();
            var target = new GameObject("원거리 락온 대상");
            _objects.Add(target);
            target.transform.position = new Vector3(5f, 1f, 80f);
            Services.TryGet<ILockOnTargetService>(out var previous);
            var service = new LockOnStub { Target = target.transform };
            try
            {
                if (previous != null) Services.Unregister(previous);
                Services.Register(service);
                var motionEvent = new SpawnProjectileEvent
                {
                    targetMode = ProjectileTargetMode.TargetPosition,
                    playerTargetFallbackDistance = 12f
                };
                object[] arguments = { player, new Vector3(0f, 10f, 0f), Vector3.forward, null };
                bool found = (bool)typeof(SpawnProjectileEvent).GetMethod("TryResolveTargetPosition", PrivateInstance).Invoke(motionEvent, arguments);
                Assert.That(found, Is.True);
                Assert.That(Vector3.Distance((Vector3)arguments[3], target.transform.position), Is.LessThan(.001f));
            }
            finally
            {
                Services.Unregister(service);
                if (previous != null) Services.Register(previous);
            }
        }

        [Test]
        public void 얼굴이없는_정의는_패널을표시하지않는다()
        {
            PlayerActor player = NewPlayer();
            var actors = new ActorQueryStub { Player = player };
            Register(actors);
            var party = DispatchProxy.Create<IPartyService, SignaturePartyProxy>();
            var definition = ScriptableObject.CreateInstance<PlayerCharacterDefinitionSO>();
            _objects.Add(definition);
            ((SignaturePartyProxy)(object)party).Definition = definition;
            Register(party);
            var presenter = new PresenterStub();
            Register(presenter);
            new UltimateFaceStripMotionEvent().Execute(player.gameObject);
            Assert.That(presenter.ShowCount, Is.Zero);
        }

        [Test]
        public void 같은얼굴이벤트를_다른시전자가써도_이전종료가_새패널을닫지않는다()
        {
            PlayerActor previous = NewPlayer(), current = NewPlayer();
            var actors = new ActorQueryStub { Player = previous };
            Register(actors);
            var party = DispatchProxy.Create<IPartyService, SignaturePartyProxy>();
            var definition = ScriptableObject.CreateInstance<PlayerCharacterDefinitionSO>();
            _objects.Add(definition);
            var texture = new Texture2D(2, 2);
            _objects.Add(texture);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
            _objects.Add(sprite);
            definition.ultimateFaceSprite = sprite;
            ((SignaturePartyProxy)(object)party).Definition = definition;
            Register(party);
            var presenter = new PresenterStub();
            Register(presenter);
            var motionEvent = new UltimateFaceStripMotionEvent();
            motionEvent.Execute(previous.gameObject);
            actors.Player = current;
            motionEvent.Execute(current.gameObject);
            long currentToken = presenter.ActiveToken;
            motionEvent.OnCompleteEvent(previous.gameObject);
            Assert.That(presenter.ActiveToken, Is.EqualTo(currentToken));
            motionEvent.OnCompleteEvent(current.gameObject);
            Assert.That(presenter.ActiveToken, Is.Zero);
        }

        [Test]
        public void 플레이어전용_이벤트는_다른대상과_몬스터실행을_무시한다()
        {
            PlayerActor player = NewPlayer();
            Register(new ActorQueryStub { Player = player });
            var other = new GameObject("다른 이벤트 대상");
            _objects.Add(other);
            foreach (MotionEventBase motionEvent in new MotionEventBase[]
                     { new AttackInputMovementEvent(), new AttackGravityEvent(), new UltimateFaceStripMotionEvent() })
            {
                Assert.That(motionEvent.EnemyExecutionPolicy, Is.EqualTo(MotionEventEnemyExecutionPolicy.Ignored));
                Assert.DoesNotThrow(() => motionEvent.Execute(other));
                Assert.DoesNotThrow(() => motionEvent.OnCompleteEvent(other));
            }
        }

        private PlayerActor NewPlayer()
        {
            var root = new GameObject("시그니처 구간 검사");
            root.SetActive(false);
            _objects.Add(root);
            return root.AddComponent<PlayerActor>();
        }

        private static PlayerAttackState NewAttackState(PlayerActor player)
        {
            var controller = player.gameObject.AddComponent<PlayerMovementController>();
            typeof(ActorMovementController).GetProperty(nameof(ActorMovementController.Actor)).SetValue(controller, player);
            return new PlayerAttackState(controller);
        }

        private void Register(IGameService service)
        {
            Services.Register(service);
            _services.Add(service);
        }

        private sealed class LockOnStub : ILockOnTargetService
        {
            public Transform Target;
            public Transform GetLockOnTarget() => Target;
        }

        private sealed class ActorQueryStub : IActorQueryService
        {
            public IWorldActor Player { get; set; }
            public Transform PlayerTransform => Player?.Transform;
            public IEnumerable<IWorldActor> AllActors => Array.Empty<IWorldActor>();
            public IWorldActor FindActor(string actorId) => null;
        }

        private sealed class PresenterStub : IFaceStripPresenter
        {
            public long ActiveToken;
            public int ShowCount;
            public long Show(FaceStripRequest request) => ActiveToken = ++ShowCount;
            public void Hide(long token) { if (token == ActiveToken) ActiveToken = 0; }
        }
    }

    public class SignaturePartyProxy : DispatchProxy
    {
        public PlayerCharacterDefinitionSO Definition;
        protected override object Invoke(MethodInfo targetMethod, object[] args)
            => targetMethod.Name == nameof(IPartyService.GetCharacterDefinition) ? Definition
                : targetMethod.ReturnType.IsValueType ? Activator.CreateInstance(targetMethod.ReturnType) : null;
    }
}
