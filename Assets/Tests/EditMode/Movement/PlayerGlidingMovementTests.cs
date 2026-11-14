using NUnit.Framework;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.MovementController;
using UPlayGround.State;
using Motion = UPlayGround.Animation.Motion;

namespace UPlayGround.Movement.Tests
{
    /// <summary>프레임률·아날로그 조작·접촉 타이밍의 회귀를 확인한다.</summary>
    public sealed class PlayerGlidingMovementTests
    {
        [TestCase(0f)]
        [TestCase(30f)]
        [TestCase(45f)]
        public void 경사면에서도_최고질주속도는_카메라에동일하게전달된다(float slope)
        {
            var root = new GameObject("질주 경사면 카메라 검사");
            root.SetActive(false);
            try
            {
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var actor = root.AddComponent<PlayerActor>();
                actor.MoveAnimType = UPlayGround.Data.EnumType.BaseMoveAnimType.Sprint;
                var controller = root.AddComponent<PlayerMovementController>();
                typeof(ActorMovementController).GetProperty("Actor").SetValue(controller, actor);
                var motorProperty = typeof(ActorMovementController).GetProperty("Motor");
                var motor = root.AddComponent(motorProperty.PropertyType);
                motorProperty.SetValue(controller, motor);
                motorProperty.PropertyType.GetField("_characterUp", flags).SetValue(motor, Vector3.up);
                var rotation = Quaternion.AngleAxis(slope, Vector3.right);
                motorProperty.PropertyType.GetField("BaseVelocity").SetValue(motor,
                    rotation * Vector3.forward * controller.MaxSprintMoveSpeed);
                var groundingField = motorProperty.PropertyType.GetField("GroundingStatus");
                var grounding = groundingField.GetValue(motor);
                grounding.GetType().GetField("IsStableOnGround").SetValue(grounding, true);
                grounding.GetType().GetField("GroundNormal").SetValue(grounding, rotation * Vector3.up);
                groundingField.SetValue(motor, grounding);
                var state = (PlayerGroundMoveState)System.Activator.CreateInstance(typeof(PlayerGroundMoveState),
                    flags, null, new object[] { controller }, null);
                typeof(ActorMovementController).GetField("_currentState", flags).SetValue(controller, state);
                var arguments = new object[] { null };
                typeof(ActorMovementController).GetMethod("TryGetCameraMotionContext").Invoke(controller, arguments);
                float feedback = (float)arguments[0].GetType().GetField("SprintSpeedFeedback").GetValue(arguments[0]);
                Assert.That(feedback, Is.EqualTo(1f).Within(0.001f));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private sealed class StaminaFallTestState : GameActorState
        {
            public override ActorStateId StateId => ActorStateId.Airborne;
            public override bool CanTransitionState(ActorStateId fromState) => true;
            public StaminaFallTestState(ActorMovementController controller) : base(controller) { }
            public override void OnEnter(GameActorState fromState) { }
        }

        [TestCase(true, 0f, true, 0f)]
        [TestCase(false, 19f, false, 19f)]
        [TestCase(false, 20f, true, 0f)]
        [TestCase(false, 100f, true, 80f)]
        public void 일반점프_실행경로는_첫점프가무료이고_추가점프에만비용을지불한다(
            bool isFirstJump, float initial, bool shouldJump, float remaining)
        {
            var root = new GameObject("일반 점프 비용 회귀 검사");
            root.SetActive(false);
            var settings = ScriptableObject.CreateInstance<UPlayGround.Data.Combat.PlayerStaminaSettingsSO>();
            UPlayGround.Components.PlayerStaminaRuntime stamina = null;
            try
            {
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var actor = root.AddComponent<PlayerActor>();
                var controller = root.AddComponent<PlayerMovementController>();
                var motorProperty = typeof(ActorMovementController).GetProperty("Motor");
                var motor = root.AddComponent(motorProperty.PropertyType);
                motorProperty.PropertyType.GetField("_characterUp", flags).SetValue(motor, Vector3.up);
                typeof(ActorMovementController).GetProperty("Actor").SetValue(controller, actor);
                motorProperty.SetValue(controller, motor);
                typeof(PlayerActor).GetField("PlayerMovementPlayerController", flags).SetValue(actor, controller);
                typeof(PlayerActor).GetField("_playerActorAnimator", flags).SetValue(actor,
                    root.AddComponent<UPlayGround.Animation.PlayerActorAnimator>());
                var ability = root.AddComponent<UPlayGround.Gameplay.Ability.AbilitySystemComponent>();
                ability.EnsureInitialized();
                ability.Attributes.SetBase(UPlayGround.Data.Stat.Attributes.Resource.MaxStamina, 100f);
                ability.Attributes.SetBase(UPlayGround.Data.Stat.Attributes.Resource.Stamina, initial);
                settings.superJumpAirCost = 20f;
                stamina = new UPlayGround.Components.PlayerStaminaRuntime(actor, ability, settings);
                typeof(PlayerActor).GetField("_stamina", flags).SetValue(actor, stamina);
                typeof(PlayerMovementController).GetField("_inputState", flags).SetValue(controller,
                    new PlayerCharacterInputs { JumpInput = UPlayGround.Input.InputCondition.Pressed });
                var state = (PlayerAirborneState)System.Activator.CreateInstance(
                    typeof(PlayerAirborneState), flags, null, new object[] { controller }, null);
                int jumps = isFirstJump ? controller.MaxJumpCount : 1;
                typeof(PlayerAirborneState).GetField("_remainingJumps", flags).SetValue(state, jumps);
                typeof(PlayerAirborneState).GetField("_jumpAnimPlayed", flags).SetValue(state, true);
                Vector3 before = new Vector3(3f, -4f, 0f);
                object[] arguments = { before, .02f };
                typeof(PlayerAirborneState).GetMethod("HandleJump", flags).Invoke(state, arguments);
                Vector3 velocity = (Vector3)arguments[0];
                Assert.That(stamina.Current, Is.EqualTo(remaining));
                Assert.That(typeof(PlayerAirborneState).GetField("_remainingJumps", flags).GetValue(state),
                    Is.EqualTo(shouldJump ? jumps - 1 : jumps));
                Assert.That(velocity.x, Is.EqualTo(before.x));
                float jumpSpeed = isFirstJump ? controller.JumpSpeed : controller.DoubleJumpSpeed;
                Assert.That(velocity.y, Is.EqualTo(shouldJump ? jumpSpeed : before.y));
                Assert.That(controller.HasJumpInput(), Is.False, "잔량 부족 입력도 소비해 자동 재실행을 막는다.");

                typeof(GameActor).GetProperty("ActorTime").SetValue(actor, 10f);
                stamina.Tick(5f, UPlayGround.Components.PlayerStaminaRuntime.IsRecoveryBlockedState(
                    ActorStateId.Airborne, isStableOnGround: false)
                    ? UPlayGround.Components.PlayerStaminaActivity.RecoveryBlocked
                    : UPlayGround.Components.PlayerStaminaActivity.Resting);
                Assert.That(stamina.Current, Is.EqualTo(remaining), "공중 체류 중 회복 금지");
                stamina.Tick(.1f, UPlayGround.Components.PlayerStaminaActivity.Resting);
                Assert.That(stamina.Current, Is.EqualTo(remaining), "착지 직후에도 회복 지연 유지");
                typeof(GameActor).GetProperty("ActorTime").SetValue(actor, 12f);
                stamina.Tick(.1f, UPlayGround.Components.PlayerStaminaActivity.Resting);
                Assert.That(stamina.Current, Is.EqualTo(Mathf.Min(100f, remaining + settings.recoveryPerSecond * .1f))
                    .Within(.001f), "착지 후 지연이 끝나면 회복 재개");
            }
            finally
            {
                stamina?.Dispose();
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [TestCase(19f, false, 19f)]
        [TestCase(20f, true, 0f)]
        [TestCase(100f, true, 80f)]
        public void 공중슈퍼점프는_비용전액이있을때만_소비한다(float initial, bool expected, float remaining)
        {
            var root = new GameObject("공중 슈퍼 점프 비용 검사");
            root.SetActive(false);
            var settings = ScriptableObject.CreateInstance<UPlayGround.Data.Combat.PlayerStaminaSettingsSO>();
            UPlayGround.Components.PlayerStaminaRuntime stamina = null;
            try
            {
                settings.superJumpAirCost = 20f;
                var actor = root.AddComponent<PlayerActor>();
                var ability = root.AddComponent<UPlayGround.Gameplay.Ability.AbilitySystemComponent>();
                ability.EnsureInitialized();
                ability.Attributes.SetBase(UPlayGround.Data.Stat.Attributes.Resource.MaxStamina, 100f);
                ability.Attributes.SetBase(UPlayGround.Data.Stat.Attributes.Resource.Stamina, initial);
                stamina = new UPlayGround.Components.PlayerStaminaRuntime(actor, ability, settings);
                Assert.That(stamina.CanSuperJumpInAir, Is.EqualTo(expected));
                Assert.That(stamina.TrySpendSuperJumpInAir(), Is.EqualTo(expected));
                Assert.That(stamina.Current, Is.EqualTo(remaining));
                settings.superJumpAirCost = 0f;
                Assert.That(stamina.TrySpendSuperJumpInAir(), Is.False, "잘못된 비용으로 무료 재도약 금지");
            }
            finally
            {
                stamina?.Dispose();
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [TestCase(false, false, false)]
        [TestCase(true, true, false)]
        [TestCase(true, false, true)]
        public void 공중재도약은_이륙전과_사용후와_예약중에_중복접수하지않는다(
            bool launched, bool used, bool requested)
        {
            var root = new GameObject("공중 재도약 중복 검사");
            root.SetActive(false);
            try
            {
                var controller = root.AddComponent<PlayerMovementController>();
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var state = (PlayerSuperJumpGlidingState)System.Activator.CreateInstance(
                    typeof(PlayerSuperJumpGlidingState), flags, null, new object[] { controller }, null);
                typeof(PlayerSuperJumpGlidingState).GetProperty("IsActive").SetValue(state, true);
                typeof(PlayerSuperJumpGlidingState).GetField("_hasLaunched", flags).SetValue(state, launched);
                typeof(PlayerSuperJumpGlidingState).GetField("_hasUsedAirJump", flags).SetValue(state, used);
                typeof(PlayerSuperJumpGlidingState).GetField("_isAirJumpRequested", flags).SetValue(state, requested);
                Assert.That(state.TryRequestAirJump(), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [TestCase(0f)]
        [TestCase(10f)]
        [TestCase(18f)]
        public void 접촉창을건너뛴_프레임에도_탈것이_이동한플레이어의_탑승높이를따른다(float horizontalSpeed)
        {
            const float gravity = 35f;
            const float contactTime = .23f;
            const float delayedFrameTime = .459f;
            Vector3 start = new Vector3(100f, 12f, -50f);
            float verticalSpeed = gravity * contactTime;
            Vector3 horizontal = Vector3.forward * horizontalSpeed;
            Vector3 current = start + horizontal * delayedFrameTime + Vector3.up
                * (verticalSpeed * delayedFrameTime - gravity * delayedFrameTime * delayedFrameTime * .5f);
            Vector3 anchor = PlayerGlidingMovement.GetCatchAnchor(current, Vector3.up,
                verticalSpeed - gravity * delayedFrameTime, gravity);
            Assert.That(Vector3.Distance(anchor, current), Is.LessThan(.0001f));

            Vector3 initialAnchor = PlayerGlidingMovement.GetCatchAnchor(start, Vector3.up, verticalSpeed, gravity);
            Assert.That(initialAnchor.y, Is.GreaterThan(start.y));
            Assert.That(initialAnchor.x, Is.EqualTo(start.x));
            Assert.That(initialAnchor.z, Is.EqualTo(start.z));
        }

        [TestCase(0f)]
        [TestCase(90f)]
        [TestCase(180f)]
        public void 보간된손은_현재물리위치와회전을기준으로_접촉판정한다(float yaw)
        {
            Vector3 renderRoot = new Vector3(10f, 20f, 30f);
            Vector3 physicsRoot = renderRoot + new Vector3(1f, -.8f, 2f);
            Quaternion renderRotation = Quaternion.Euler(0f, 30f, 0f);
            Quaternion physicsRotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 localPalm = new Vector3(.24f, 1.54f, .08f);
            Vector3 renderedPalm = renderRoot + renderRotation * localPalm;
            Vector3 expectedGrip = physicsRoot + physicsRotation * localPalm;
            Vector3 actual = PlayerGlidingMovement.GetPhysicsPosePosition(renderedPalm,
                renderRoot, renderRotation, physicsRoot, physicsRotation);
            Assert.That(Vector3.Distance(actual, expectedGrip), Is.LessThan(.00001f));
        }

        [Test]
        public void 활공가속_소진은_가속을멈추고_상승과접촉과활공모두_낙하대상이된다()
        {
            var actorObject = new GameObject("활공 가속 검사");
            actorObject.SetActive(false);
            var resourceObject = new GameObject("활공 스태미나 검사");
            var settings = ScriptableObject.CreateInstance<UPlayGround.Data.Combat.PlayerStaminaSettingsSO>();
            UPlayGround.Components.PlayerStaminaRuntime stamina = null;
            try
            {
                var actor = actorObject.AddComponent<PlayerActor>();
                var controller = actorObject.AddComponent<PlayerMovementController>();
                var controllerFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                typeof(ActorMovementController).GetProperty("Actor").SetValue(controller, actor);
                var motorProperty = typeof(ActorMovementController).GetProperty("Motor");
                var motor = actorObject.AddComponent(motorProperty.PropertyType);
                motorProperty.PropertyType.GetField("_characterUp", controllerFlags).SetValue(motor, Vector3.up);
                motorProperty.SetValue(controller, motor);
                typeof(PlayerActor).GetField("PlayerMovementPlayerController", controllerFlags).SetValue(actor, controller);
                var ability = resourceObject.AddComponent<UPlayGround.Gameplay.Ability.AbilitySystemComponent>();
                ability.EnsureInitialized();
                ability.Attributes.SetBase(UPlayGround.Data.Stat.Attributes.Resource.MaxStamina, 100f);
                ability.Attributes.SetBase(UPlayGround.Data.Stat.Attributes.Resource.Stamina, 100f);
                stamina = new UPlayGround.Components.PlayerStaminaRuntime(actor, ability, settings);
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var swap = actorObject.AddComponent<UPlayGround.Components.PlayerSwapBehaviour>();
                var modelField = typeof(UPlayGround.Components.PlayerSwapBehaviour).GetField("_activeModel", flags);
                modelField.SetValue(swap, actorObject.AddComponent(modelField.FieldType));
                typeof(PlayerActor).GetField("_swapBehaviour", flags).SetValue(actor, swap);
                typeof(PlayerActor).GetField("_stamina", flags).SetValue(actor, stamina);
                var state = (PlayerSuperJumpGlidingState)System.Activator.CreateInstance(
                    typeof(PlayerSuperJumpGlidingState), flags, null, new object[] { controller }, null);
                typeof(PlayerActorState).GetField("playerActor", flags).SetValue(state, actor);
                typeof(PlayerSuperJumpGlidingState).GetProperty("IsActive").SetValue(state, true);
                var phase = typeof(PlayerSuperJumpGlidingState).GetField("_phase", flags);
                state.RequestBoost(true);
                state.UpdateBoost();
                Assert.That(state.IsBoosting, Is.False, "잡기 전에는 소비하지 않는다.");
                phase.SetValue(state, System.Enum.Parse(phase.FieldType, "Gliding"));
                state.UpdateBoost();
                Assert.That(state.IsBoosting, Is.True);
                Assert.That(stamina.Tick(1f, UPlayGround.Components.PlayerStaminaActivity.Sprinting), Is.True);
                Assert.That(stamina.Current, Is.EqualTo(100f - settings.sprintCostPerSecond).Within(.001f));
                float beforePause = stamina.Current;
                stamina.Tick(0f, UPlayGround.Components.PlayerStaminaActivity.Sprinting);
                Assert.That(stamina.Current, Is.EqualTo(beforePause));
                if (!stamina.Tick(10f, UPlayGround.Components.PlayerStaminaActivity.Sprinting)) state.ExhaustBoost();
                Assert.That(stamina.Current, Is.Zero);
                Assert.That(state.IsBoosting, Is.False);
                Assert.That(state.ShouldFallFromStaminaExhaustion, Is.False, "이륙 전 지상 준비는 낙하 대상이 아니다.");
                typeof(PlayerSuperJumpGlidingState).GetField("_hasLaunched", flags).SetValue(state, true);
                foreach (string phaseName in new[] { "Rising", "Catching", "Gliding" })
                {
                    phase.SetValue(state, System.Enum.Parse(phase.FieldType, phaseName));
                    Assert.That(state.ShouldFallFromStaminaExhaustion, Is.True, phaseName);
                }
                typeof(PlayerSuperJumpGlidingState).GetProperty("IsActive").SetValue(state, false);
                Assert.That(state.ShouldFallFromStaminaExhaustion, Is.False, "종료 상태는 재처리하지 않는다.");
                typeof(PlayerSuperJumpGlidingState).GetProperty("IsActive").SetValue(state, true);
                ability.Attributes.SetBase(UPlayGround.Data.Stat.Attributes.Resource.Stamina, 1f);
                Assert.That(state.ShouldFallFromStaminaExhaustion, Is.False, "가속 시작 잔량 미달과 완전 소진은 구분한다.");
                state.RequestBoost(false);
                state.RequestBoost(true);
                state.UpdateBoost();
                Assert.That(state.IsBoosting, Is.False, "시작 잔량 미달");

                ability.Attributes.SetBase(UPlayGround.Data.Stat.Attributes.Resource.Stamina, 0f);
                var falling = new StaminaFallTestState(controller);
                var stateMachine = new ActorStateMachine(controller);
                stateMachine.Register(falling);
                typeof(ActorMovementController).GetProperty("StateMachine").SetValue(controller, stateMachine);
                typeof(ActorMovementController).GetField("_currentState", flags).SetValue(controller, state);
                var velocityField = motorProperty.PropertyType.GetField("BaseVelocity");
                velocityField.SetValue(motor, new Vector3(3f, 5f, 0f));
                state.UpdateState(.02f);
                Assert.That(controller.CurrentState, Is.SameAs(falling));
                Assert.That(state.IsActive, Is.False, "낙하 전환은 활공 정리까지 수행한다.");
                Assert.That(state.LastExitReason, Is.EqualTo("스태미나 소진"));
                Assert.That((Vector3)velocityField.GetValue(motor), Is.EqualTo(new Vector3(3f, 0f, 0f)));
            }
            finally
            {
                stamina?.Dispose();
                UnityEngine.Object.DestroyImmediate(actorObject);
                UnityEngine.Object.DestroyImmediate(resourceObject);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        public void 활공가속은_방향입력없이도_목표속도에닿고_해제하면_순항속도로복귀한다(int frameRate)
        {
            var settings = ScriptableObject.CreateInstance<UPlayGround.Data.Actor.PlayerGlidingSettingsSO>();
            try
            {
                float speed = settings.cruiseSpeed;
                float deltaTime = 1f / frameRate;
                Assert.That(PlayerGlidingMovement.IntegrateCruiseSpeed(speed, settings, 0f,
                    isBoosting: true), Is.EqualTo(speed));
                for (int i = 0; i < frameRate; i++)
                {
                    float next = PlayerGlidingMovement.IntegrateCruiseSpeed(speed, settings, deltaTime,
                        isBoosting: true);
                    Assert.That(next, Is.InRange(speed, settings.boostSpeed));
                    speed = next;
                }
                Assert.That(speed, Is.EqualTo(settings.boostSpeed).Within(.001f));
                for (int i = 0; i < frameRate * 2; i++)
                {
                    float next = PlayerGlidingMovement.IntegrateCruiseSpeed(speed, settings, deltaTime);
                    Assert.That(next, Is.InRange(settings.cruiseSpeed, speed));
                    speed = next;
                }
                Assert.That(speed, Is.EqualTo(settings.cruiseSpeed).Within(.001f));
            }
            finally { UnityEngine.Object.DestroyImmediate(settings); }
        }

        [Test]
        public void 활공가속설정은_순항보다느린속도와_가속불가를거부한다()
        {
            var settings = ScriptableObject.CreateInstance<UPlayGround.Data.Actor.PlayerGlidingSettingsSO>();
            try
            {
                Assert.That(settings.HasValidValues, Is.True);
                settings.boostSpeed = settings.cruiseSpeed - 1f;
                Assert.That(settings.HasValidValues, Is.False);
                settings.boostSpeed = settings.cruiseSpeed;
                settings.boostAcceleration = 0f;
                Assert.That(settings.HasValidValues, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(settings); }
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        public void 선회기울기는_프레임률과무관하게_부드럽게반전한다(int frameRate)
        {
            float turn = 1f;
            for (int i = 0; i < frameRate; i++)
            {
                float next = PlayerGlidingMovement.SmoothTurn(turn, -1f, .18f, 1f / frameRate);
                Assert.That(next, Is.LessThan(turn));
                Assert.That(next, Is.GreaterThanOrEqualTo(-1f));
                turn = next;
            }
            Assert.That(turn, Is.EqualTo(-1f + 2f * Mathf.Exp(-1f / .18f)).Within(.00001f));
            Assert.That(PlayerGlidingMovement.SmoothTurn(turn, 1f, .18f, 0f), Is.EqualTo(turn));
        }

        [Test]
        public void 선회진입경계에서_작게흔들려도_자세를반복재시작하지않는다()
        {
            int pose = 0;
            foreach (float turn in new[] { .16f, .14f, .151f, .149f, .08f })
            {
                pose = PlayerGlidingMovement.SelectTurnPose(pose, turn, .15f, .06f);
                Assert.That(pose, Is.EqualTo(1));
            }
            Assert.That(PlayerGlidingMovement.SelectTurnPose(pose, .05f, .15f, .06f), Is.Zero);
            Assert.That(PlayerGlidingMovement.SelectTurnPose(pose, -.2f, .15f, .06f), Is.EqualTo(-1));
            Assert.That(PlayerGlidingMovement.SelectTurnPose(-1, -.08f, .15f, .06f), Is.EqualTo(-1));
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        public void 도약의_변위는_프레임률과무관하게_포물선과일치한다(int frameRate)
        {
            const float gravity = 35f;
            float speed = Mathf.Sqrt(2f * gravity * 12f);
            float startSpeed = speed;
            float height = 0f;
            for (int i = 0; i < frameRate; i++)
                height += PlayerGlidingMovement.IntegrateBallisticStep(ref speed, gravity, 1f / frameRate) / frameRate;
            Assert.That(height, Is.EqualTo(startSpeed - gravity * 0.5f).Within(0.0001f));
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        public void 비행은_상승에서_하강으로_프레임률과무관하게_수렴한다(int frameRate)
        {
            float speed = 8f;
            for (int i = 0; i < frameRate * 3; i++)
            {
                float previous = speed;
                PlayerGlidingMovement.IntegrateVerticalStep(ref speed, -1.6f, 0.22f, 1f / frameRate);
                Assert.That(speed, Is.LessThanOrEqualTo(previous));
                Assert.That(speed, Is.GreaterThanOrEqualTo(-1.6f));
            }
            Assert.That(speed, Is.EqualTo(-1.6f).Within(0.001f));
        }

        [TestCase(0f, 0f)]
        [TestCase(1f, 4f)]
        [TestCase(-1f, -1.6f)]
        [TestCase(.25f, 1f)]
        [TestCase(-.25f, -.4f)]
        [TestCase(2f, 4f)]
        [TestCase(-2f, -1.6f)]
        public void 비행세로입력으로_상승_고도유지_하강을_선택한다(float input, float expected)
        {
            var settings = ScriptableObject.CreateInstance<UPlayGround.Data.Actor.PlayerGlidingSettingsSO>();
            try
            {
                float command = PlayerGlidingMovement.ResolveVerticalCommand(input, settings);
                Assert.That(command, Is.EqualTo(expected).Within(.0001f));
            }
            finally { UnityEngine.Object.DestroyImmediate(settings); }
        }

        [TestCase(-1f)]
        [TestCase(0f)]
        [TestCase(1f)]
        public void 실제비행속도는_자동전진과_세로입력을_따르고_카메라를무시한다(float input)
        {
            var root = new GameObject("비행 자동 전진·고도 검사");
            root.SetActive(false);
            var settings = ScriptableObject.CreateInstance<UPlayGround.Data.Actor.PlayerGlidingSettingsSO>();
            try
            {
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var controller = root.AddComponent<PlayerMovementController>();
                var binding = root.AddComponent<UPlayGround.Components.PlayerGliderBinding>();
                binding.GetType().GetField("_settings", flags).SetValue(binding, settings);
                controller.SetGliderBinding(binding);
                var motorProperty = typeof(ActorMovementController).GetProperty("Motor");
                var motor = root.AddComponent(motorProperty.PropertyType);
                motorProperty.SetValue(controller, motor);
                motorProperty.PropertyType.GetField("_characterUp", flags).SetValue(motor, Vector3.up);
                var state = (PlayerSuperJumpGlidingState)System.Activator.CreateInstance(
                    typeof(PlayerSuperJumpGlidingState), flags, null, new object[] { controller }, null);
                var type = state.GetType();
                type.GetProperty("IsActive").SetValue(state, true);
                type.GetField("_hasContact", flags).SetValue(state, true);
                type.GetField("_settings", flags).SetValue(state, settings);
                type.GetField("_view", flags).SetValue(state, root.AddComponent<UPlayGround.Components.PlayerGliderView>());
                var phase = type.GetField("_phase", flags);
                phase.SetValue(state, System.Enum.Parse(phase.FieldType, "Gliding"));
                foreach (float pitch in new[] { -30f, 18f, 70f })
                {
                    typeof(PlayerMovementController).GetField("_inputState", flags).SetValue(controller,
                        new PlayerCharacterInputs { MoveInput = new Vector2(0f, input) });
                    typeof(PlayerMovementController).GetField("_cameraRotation", flags).SetValue(controller,
                        Quaternion.Euler(pitch, 125f, 0f));
                    type.GetField("_heading", flags).SetValue(state, Vector3.forward);
                    type.GetField("_speed", flags).SetValue(state, 0f);
                    type.GetField("_verticalSpeed", flags).SetValue(state, 0f);
                    Vector3 velocity = Vector3.zero;
                    state.UpdateVelocity(ref velocity, 1f);
                    Assert.That(velocity.z, Is.EqualTo(settings.horizontalAcceleration).Within(.0001f),
                        "W/S와 무입력 모두 같은 전진 가속을 사용한다.");
                    Assert.That(velocity.x, Is.EqualTo(0f).Within(.0001f), "카메라 회전으로 선회하지 않는다.");
                    Assert.That(input == 0f ? velocity.y == 0f : velocity.y * input > 0f, Is.True,
                        "카메라 상하 각도와 무관하게 W 상승·S 하강·무입력 고도 유지다.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        public void 비행상승과_고도유지의_변위는_프레임률에_독립적이다(int frameRate)
        {
            const float blend = .22f;
            float speed = -1.6f;
            float height = 0f;
            for (int i = 0; i < frameRate; i++)
                height += PlayerGlidingMovement.IntegrateVerticalStep(ref speed, 4f, blend, 1f / frameRate) / frameRate;
            float expected = 4f + (-1.6f - 4f) * blend * (1f - Mathf.Exp(-1f / blend));
            Assert.That(height, Is.EqualTo(expected).Within(.0001f));
            Assert.That(speed, Is.GreaterThan(0f));
            for (int i = 0; i < frameRate * 3; i++)
                PlayerGlidingMovement.IntegrateVerticalStep(ref speed, 0f, blend, 1f / frameRate);
            Assert.That(speed, Is.EqualTo(0f).Within(.001f));
        }

        [Test]
        public void 날개외형이_장애물에겹쳐도_비행을_종료하지않는다()
        {
            var root = new GameObject("날개 충돌 회귀 검사");
            var obstacle = new GameObject("검사용 벽");
            root.SetActive(false);
            try
            {
                root.transform.position = obstacle.transform.position = new Vector3(5000f, 5000f, 5000f);
                obstacle.AddComponent<BoxCollider>();
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var controller = root.AddComponent<PlayerMovementController>();
                var view = root.AddComponent<UPlayGround.Components.PlayerGliderView>();
                typeof(UPlayGround.Components.PlayerGliderView).GetField("_collisionHalfExtents", flags)
                    .SetValue(view, Vector3.one);
                var state = (PlayerSuperJumpGlidingState)System.Activator.CreateInstance(
                    typeof(PlayerSuperJumpGlidingState), flags, null, new object[] { controller }, null);
                var stateType = typeof(PlayerSuperJumpGlidingState);
                stateType.GetProperty("IsActive").SetValue(state, true);
                stateType.GetField("_hasContact", flags).SetValue(state, true);
                stateType.GetField("_view", flags).SetValue(state, view);
                stateType.GetField("_previousVolumeCenter", flags).SetValue(state, root.transform.position);
                Physics.SyncTransforms();
                Assert.That(Physics.CheckBox(root.transform.position, Vector3.one), Is.True);
                state.AfterCharacterUpdate(.02f);
                Assert.That(state.IsActive, Is.True);
                Assert.That(state.LastExitReason, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(obstacle);
            }
        }

        [TestCase(0f, 1f, 4f)]
        [TestCase(-1f, 0f, 0f)]
        public void 비행중_벽과천장에_부딪혀도_비행을_종료하지않는다(float normalY, float normalX, float expectedVertical)
        {
            var root = new GameObject("비행 충돌 회귀 검사");
            root.SetActive(false);
            try
            {
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var controller = root.AddComponent<PlayerMovementController>();
                var motorProperty = typeof(ActorMovementController).GetProperty("Motor");
                var motor = root.AddComponent(motorProperty.PropertyType);
                motorProperty.SetValue(controller, motor);
                motorProperty.PropertyType.GetField("_characterUp", flags).SetValue(motor, Vector3.up);
                var state = (PlayerSuperJumpGlidingState)System.Activator.CreateInstance(
                    typeof(PlayerSuperJumpGlidingState), flags, null, new object[] { controller }, null);
                var stateType = typeof(PlayerSuperJumpGlidingState);
                stateType.GetProperty("IsActive").SetValue(state, true);
                stateType.GetField("_hasLaunched", flags).SetValue(state, true);
                stateType.GetField("_hasContact", flags).SetValue(state, true);
                stateType.GetField("_verticalSpeed", flags).SetValue(state, 4f);
                var hitMethod = stateType.GetMethod("OnMovementHit");
                var report = System.Activator.CreateInstance(hitMethod.GetParameters()[3].ParameterType.GetElementType());
                hitMethod.Invoke(state, new object[] { null, new Vector3(normalX, normalY, 0f), Vector3.zero, report });
                state.AfterCharacterUpdate(.02f);
                Assert.That(state.IsActive, Is.True);
                Assert.That(state.LastExitReason, Is.Null);
                Assert.That(stateType.GetField("_verticalSpeed", flags).GetValue(state), Is.EqualTo(expectedVertical));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void 무입력은_방향을유지하고_약한스틱은_선회량을줄인다()
        {
            Assert.That(PlayerGlidingMovement.RotateHeading(Vector3.forward, Vector3.zero,
                Vector3.up, 100f, 0.1f), Is.EqualTo(Vector3.forward));
            Vector3 weak = PlayerGlidingMovement.RotateHeading(Vector3.forward, Vector3.right * 0.25f,
                Vector3.up, 100f, 0.1f);
            Vector3 strong = PlayerGlidingMovement.RotateHeading(Vector3.forward, Vector3.right,
                Vector3.up, 100f, 0.1f);
            Assert.That(Vector3.Angle(Vector3.forward, weak), Is.EqualTo(2.5f).Within(0.001f));
            Assert.That(Vector3.Angle(Vector3.forward, strong), Is.EqualTo(10f).Within(0.001f));
        }

        [Test]
        public void 일시정지중에는_속도와방향이변하지않는다()
        {
            float speed = -0.2f;
            Assert.That(PlayerGlidingMovement.IntegrateVerticalStep(ref speed, 4f, 0.22f, 0f), Is.EqualTo(-0.2f));
            Assert.That(speed, Is.EqualTo(-0.2f));
            Assert.That(PlayerGlidingMovement.RotateHeading(Vector3.forward, Vector3.back,
                Vector3.up, 110f, 0f), Is.EqualTo(Vector3.forward));
        }

        [Test]
        public void 접촉마커는_앞클립과_재생속도를반영하고_누락을거부한다()
        {
            var clip = new AnimationClip();
            clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 1f, 0f));
            try
            {
                var set = MotionSet.CreateAuthored("활공 접촉 검사");
                set.motions.Add(new Motion { motionClip = clip, playbackSpeed = 2f });
                var contact = new Motion { motionClip = clip, playbackSpeed = 0.5f };
                contact.markers.Add(new MotionMarker { id = PlayerSuperJumpGlidingState.ContactMarker, normalizedTime = 0.25f });
                set.motions.Add(contact);
                Assert.That(PlayerSuperJumpGlidingState.TryGetMarkerTime(set,
                    PlayerSuperJumpGlidingState.ContactMarker, out float time), Is.True);
                Assert.That(time, Is.EqualTo(1f).Within(0.0001f));
                Assert.That(PlayerSuperJumpGlidingState.TryGetMarkerTime(set,
                    PlayerSuperJumpGlidingState.LaunchMarker, out _), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(clip); }
        }
    }
}
