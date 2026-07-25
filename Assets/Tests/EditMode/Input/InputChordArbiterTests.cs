using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UPlayGround.InputDefine;

namespace UPlayGround.Input.Tests
{
    /// <summary>
    /// 스펙 §9 조합키 런타임 판정 검증.
    /// 카탈로그는 §19.1의 필수 조합 테스트와 같은 구성을 사용한다.
    ///
    ///   Dodge       = LB + East      (조합)
    ///   QuickSlotUp = LB + D-pad Up  (조합)
    ///   Dash        = East           (단일)
    ///   Guard       = LB             (단일, hold)
    ///   Swap1       = D-pad Up       (단일)
    /// </summary>
    public sealed class InputChordArbiterTests
    {
        private const string Map = "PlayerAction";
        private const string LeftShoulder = "<Gamepad>/leftShoulder";
        private const string East = "<Gamepad>/buttonEast";
        private const string DpadUp = "<Gamepad>/dpad/up";

        private InputChordArbiter<int> _arbiter;
        private HashSet<string> _pressed;
        private List<InputArbiterEvent<int>> _dispatch;

        [SetUp]
        public void SetUp()
        {
            _pressed = new HashSet<string>();
            _dispatch = new List<InputArbiterEvent<int>>();
            _arbiter = new InputChordArbiter<int>
            {
                GraceSeconds = 0.12f,
                IsControlPressed = path => _pressed.Contains(path),
            };

            _arbiter.RegisterChord(Map, "Dodge", LeftShoulder, East);
            _arbiter.RegisterChord(Map, "QuickSlot_Up", LeftShoulder, DpadUp);
        }

        #region 헬퍼

        private void Press(string path) =>
            _pressed.Add(InputChordArbiter<int>.NormalizePath(path));

        private void Release(string path) =>
            _pressed.Remove(InputChordArbiter<int>.NormalizePath(path));

        private void Submit(string action, InputArbiterPhase phase, string path, float time) =>
            _arbiter.Submit(Map, action, phase, path, time, 0, _dispatch);

        /// <summary>버튼 1회 눌림(started + performed)을 넣는다.</summary>
        private void PressButton(string action, string path, float time)
        {
            Press(path);
            Submit(action, InputArbiterPhase.Started, path, time);
            Submit(action, InputArbiterPhase.Performed, path, time);
        }

        private void Tick(float time) => _arbiter.Tick(time, _dispatch);

        private int Count(string action, InputArbiterPhase phase) =>
            _dispatch.Count(e => e.ActionName == action && e.Phase == phase);

        #endregion

        [Test]
        public void 단독_Trigger는_grace_이후_단일_액션으로_확정된다()
        {
            PressButton("Dash", East, 0f);

            Tick(0.05f);
            Assert.AreEqual(0, Count("Dash", InputArbiterPhase.Performed), "grace 안에서는 아직 확정되면 안 된다.");

            Tick(0.13f);
            Assert.AreEqual(1, Count("Dash", InputArbiterPhase.Started));
            Assert.AreEqual(1, Count("Dash", InputArbiterPhase.Performed));
        }

        [Test]
        public void 조합_후보가_아닌_컨트롤은_즉시_확정된다()
        {
            PressButton("Attack", "<Gamepad>/buttonWest", 0f);

            Assert.AreEqual(1, Count("Attack", InputArbiterPhase.Performed), "지연 없이 바로 나가야 한다.");
            Assert.AreEqual(0, _arbiter.PendingCount);
        }

        [Test]
        public void Modifier_유지중_Trigger는_조합만_발화하고_단일은_억제된다()
        {
            // LB 유지 (아직 grace 안)
            PressButton("Guard", LeftShoulder, 0f);

            // LB + East → Dodge 성립
            PressButton("Dodge", East, 0.03f);
            PressButton("Dash", East, 0.03f);

            Tick(0.5f);

            Assert.AreEqual(1, Count("Dodge", InputArbiterPhase.Performed), "Dodge 1회");
            Assert.AreEqual(0, Count("Dash", InputArbiterPhase.Performed), "Dash 0회");
            Assert.AreEqual(0, Count("Guard", InputArbiterPhase.Started), "Guard 외부 시작 0회");
        }

        [Test]
        public void Modifier_단독_유지는_grace_이후_Hold가_확정된다()
        {
            PressButton("Guard", LeftShoulder, 0f);

            Tick(0.05f);
            Assert.AreEqual(0, Count("Guard", InputArbiterPhase.Started));

            Tick(0.13f);
            Assert.AreEqual(1, Count("Guard", InputArbiterPhase.Started), "grace 후 Guard started 1회");
        }

        [Test]
        public void 확정된_Hold는_조합_성립시_보정_Canceled를_한번_받는다()
        {
            PressButton("Guard", LeftShoulder, 0f);
            Tick(0.13f);
            Assert.AreEqual(1, Count("Guard", InputArbiterPhase.Started));

            _dispatch.Clear();
            PressButton("Dodge", East, 0.2f);

            var canceled = _dispatch
                .Where(e => e.ActionName == "Guard" && e.Phase == InputArbiterPhase.Canceled)
                .ToList();
            Assert.AreEqual(1, canceled.Count, "보정 Canceled 1회");
            Assert.IsTrue(canceled[0].IsSynthetic, "중재기가 만든 보정 이벤트여야 한다.");

            // 실제 LB release는 중복 전달되지 않는다.
            _dispatch.Clear();
            Release(LeftShoulder);
            Submit("Guard", InputArbiterPhase.Canceled, LeftShoulder, 0.4f);
            Assert.AreEqual(0, Count("Guard", InputArbiterPhase.Canceled), "실제 release에서 중복 전달 금지");
        }

        [Test]
        public void 억제된_단일은_release_이후_다시_받을_수_있다()
        {
            PressButton("Guard", LeftShoulder, 0f);
            PressButton("Dodge", East, 0.03f);
            PressButton("Dash", East, 0.03f);
            Tick(0.5f);
            Assert.AreEqual(0, Count("Dash", InputArbiterPhase.Performed));

            // East와 LB를 모두 떼고 East만 단독으로 다시 누른다.
            Release(East);
            Submit("Dash", InputArbiterPhase.Canceled, East, 0.6f);
            Release(LeftShoulder);
            Submit("Guard", InputArbiterPhase.Canceled, LeftShoulder, 0.6f);

            _dispatch.Clear();
            PressButton("Dash", East, 1.0f);
            Tick(1.2f);
            Assert.AreEqual(1, Count("Dash", InputArbiterPhase.Performed), "억제 해제 후 다시 통과해야 한다.");
        }

        [Test]
        public void Dpad_조합은_캐릭터_교체를_억제한다()
        {
            PressButton("Guard", LeftShoulder, 0f);
            PressButton("QuickSlot_Up", DpadUp, 0.03f);
            PressButton("CharacterSwap_1", DpadUp, 0.03f);

            Tick(0.5f);

            Assert.AreEqual(1, Count("QuickSlot_Up", InputArbiterPhase.Performed), "QuickSlotUp 1회");
            Assert.AreEqual(0, Count("CharacterSwap_1", InputArbiterPhase.Performed), "CharacterSwap1 0회");
        }

        [Test]
        public void Dpad_단독은_grace_이후_캐릭터_교체로_확정된다()
        {
            PressButton("CharacterSwap_1", DpadUp, 0f);
            Tick(0.13f);

            Assert.AreEqual(1, Count("CharacterSwap_1", InputArbiterPhase.Performed), "CharacterSwap1 1회");
        }

        [Test]
        public void 컨텍스트_변경시_보류_입력은_폐기된다()
        {
            PressButton("Dash", East, 0f);
            PressButton("Guard", LeftShoulder, 0f);
            Assert.AreEqual(2, _arbiter.PendingCount);

            _arbiter.Reset();
            Tick(1.0f);

            Assert.AreEqual(0, _dispatch.Count, "pending Gameplay 입력 0회");
            Assert.AreEqual(0, _arbiter.PendingCount);
        }

        [Test]
        public void 확정_이벤트는_원래_물리_입력_시각을_유지한다()
        {
            PressButton("Dash", East, 1.5f);
            Tick(1.7f);

            InputArbiterEvent<int> performed = _dispatch
                .First(e => e.ActionName == "Dash" && e.Phase == InputArbiterPhase.Performed);
            Assert.AreEqual(1.5f, performed.PhysicalTime, 1e-4f,
                "grace 지연과 무관하게 물리 입력 시각이어야 버퍼 유효 시간이 줄지 않는다.");
        }

        [Test]
        public void 보류중_도착_순서는_그대로_보존된다()
        {
            Press(East);
            Submit("Dash", InputArbiterPhase.Started, East, 0f);
            Submit("Dash", InputArbiterPhase.Performed, East, 0f);
            // grace가 끝나기 전에 이미 손을 뗀 경우
            Release(East);
            Submit("Dash", InputArbiterPhase.Canceled, East, 0.04f);

            Tick(0.13f);

            var phases = _dispatch
                .Where(e => e.ActionName == "Dash")
                .Select(e => e.Phase)
                .ToArray();
            Assert.AreEqual(
                new[]
                {
                    InputArbiterPhase.Started,
                    InputArbiterPhase.Performed,
                    InputArbiterPhase.Canceled,
                },
                phases);
        }

        [Test]
        public void grace가_0이면_지연없이_확정된다()
        {
            _arbiter.GraceSeconds = 0f;
            PressButton("Dash", East, 0f);

            Assert.AreEqual(1, Count("Dash", InputArbiterPhase.Performed));
            Assert.AreEqual(0, _arbiter.PendingCount);
        }

        [Test]
        public void 다른_액션맵의_조합은_서로_간섭하지_않는다()
        {
            _arbiter.Submit("UI", "Dash", InputArbiterPhase.Started, East, 0f, 0, _dispatch);
            _arbiter.Submit("UI", "Dash", InputArbiterPhase.Performed, East, 0f, 0, _dispatch);

            Assert.AreEqual(1, Count("Dash", InputArbiterPhase.Performed),
                "조합 카탈로그가 없는 맵은 즉시 확정돼야 한다.");
        }

        [Test]
        public void 경로_정규화는_바인딩_경로와_컨트롤_경로를_동일하게_취급한다()
        {
            Assert.AreEqual("leftshoulder", InputChordArbiter<int>.NormalizePath("<Gamepad>/leftShoulder"));
            Assert.AreEqual("leftshoulder", InputChordArbiter<int>.NormalizePath("/Gamepad/leftShoulder"));
            Assert.AreEqual("dpad/up", InputChordArbiter<int>.NormalizePath("<Gamepad>/dpad/up"));
            Assert.AreEqual("dpad/up", InputChordArbiter<int>.NormalizePath("/Gamepad/dpad/up"));
            Assert.AreEqual(string.Empty, InputChordArbiter<int>.NormalizePath(null));
        }
    }
}
