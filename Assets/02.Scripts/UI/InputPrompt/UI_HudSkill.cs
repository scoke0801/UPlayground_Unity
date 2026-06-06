using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data.Combat;
using UPlayGround.Input;
using UPlayGround.Manager;

namespace UPlayGround.UI.InputPrompt
{
    /// <summary>
    /// 고정 스킬바 HUD(명조식). 슬롯(<see cref="UISkillSlot"/>)들을 호스팅하며 두 신호로 구동한다:
    /// - <b>게이지/Ready</b>: <see cref="PlayerSkillGauge.OnGaugeChanged"/> 이벤트 기반(핫패스 아님).
    /// - <b>콤보 다음 키 강조</b>: <see cref="ComboRouteResolver.CollectHints"/>를 입력 윈도우 변화 시에만 재계산.
    ///
    /// <see cref="UI_Base"/>를 상속해 UIManager 생명주기(Show/Hide)로 구동된다(다른 HUD와 동일).
    /// 활성 캐릭터는 <see cref="PartyManager"/>에서 받고 교체(OnSwapCompleted)를 따른다.
    /// ※ 슬롯 아이콘은 v1에서 프리팹 직렬화라 교체를 따라가지 않음(스왑 미추적, 캐릭터별 스킬 아이콘 파이프라인 부재).
    /// 프리팹 배치/슬롯 구성/글로우 연출은 Unity 에디터 작업.
    /// </summary>
    public class UI_HudSkill : UI_Base
    {
        [Tooltip("스킬바를 구성하는 슬롯들(왼→오). 각 슬롯은 자신의 토큰으로 정의된다.")]
        [SerializeField] private List<UISkillSlot> _slots = new();

        private PlayerActor      _player;
        private PlayerCombat     _combat;
        private PlayerSkillGauge _gauge;
        private Func<ComboRouteEntry, bool> _resourceFilter; // 메서드그룹 delegate 캐시

        private PartyManager _partyManager;
        private bool _subscribedSwap;

        private readonly List<ComboRouteResolver.ComboRouteHint> _hints = new();
        private int _lastSignature = NoSignature;
        private bool _hasVisibleCooldown;
        private const int NoSignature = int.MinValue;

        // ── UI_Base 생명주기 ─────────────────────────────────────────
        protected override void OnShow()
        {
            base.OnShow();

            EnsureSlotsBound();

            _partyManager = PartyManager.Instance;
            if (_partyManager != null)
            {
                _partyManager.OnSwapCompleted += OnSwapCompleted;
                _subscribedSwap = true;
                Bind(_partyManager.ActiveCharacter);
            }
            else
            {
                Bind(FindFirstObjectByType<PlayerActor>());
            }
        }

        protected override void OnHide()
        {
            Teardown();
        }

        protected override void OnDispose()
        {
            Teardown();
        }

        private void Teardown()
        {
            if (_subscribedSwap && _partyManager != null)
                _partyManager.OnSwapCompleted -= OnSwapCompleted;
            _subscribedSwap = false;
            _partyManager   = null;
            Bind(null);
        }

        private void OnSwapCompleted(PlayerActor newPlayer) => Bind(newPlayer);

        private void Bind(PlayerActor player)
        {
            // 이전 게이지 구독 해제
            if (_gauge != null)
            {
                _gauge.OnGaugeChanged -= OnGaugeChanged;
                _gauge.OnCooldownChanged -= OnSkillCooldownChanged;
            }

            _player         = player;
            _combat         = player != null ? player.GetCombat() : null;
            _gauge          = player != null ? player.SkillGauge  : null;
            _resourceFilter = _combat != null ? _combat.CanAffordRoute : null;
            _lastSignature  = NoSignature; // 콤보 힌트 강제 재계산

            // 슬롯 1회 초기화(아이콘/키캡/게이지 슬롯)
            EnsureSlotsBound();
            for (int i = 0; i < _slots.Count; i++)
                _slots[i]?.Initialize();

            if (_gauge != null)
            {
                _gauge.OnGaugeChanged += OnGaugeChanged;
                _gauge.OnCooldownChanged += OnSkillCooldownChanged;
                OnGaugeChanged(_gauge.CurrentGauge, _gauge.MaxGauge); // 현재 상태 즉시 반영
            }
            else
            {
                ApplyGaugeStates();
            }

            if (player == null)
                ClearComboHints();
        }

        // ── 게이지(이벤트 구동) ──────────────────────────────────────
        private void OnGaugeChanged(float current, float max)
        {
            ApplyGaugeStates();
            _lastSignature = NoSignature; // 자원 게이트가 바뀌면 콤보 힌트도 다시 계산해야 한다.
        }

        private void OnSkillCooldownChanged(int skillSlot, float remaining, float duration)
        {
            ApplyGaugeStates();
            _lastSignature = NoSignature;
        }

        private void ApplyGaugeStates()
        {
            _hasVisibleCooldown = false;

            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot == null) continue;
                _hasVisibleCooldown |= slot.SetGaugeState(_gauge);
            }
        }

        // ── 콤보 다음 키 강조(입력 변화 시에만) ──────────────────────
        protected override void Update()
        {
            base.Update();
            if (!IsVisible || _player == null || _combat == null) return;

            if (_hasVisibleCooldown)
                ApplyGaugeStates();

            var window  = _player.ComboInputTracker.GetWindow();
            bool grounded = IsGrounded();
            int signature = ComputeSignature(window, grounded);
            if (signature == _lastSignature) return;
            _lastSignature = signature;

            ComboRouteResolver.CollectHints(
                window, _combat.ComboRoutes, _player.Tags, grounded, _resourceFilter, _hints);

            ApplyComboHints();
        }

        private void ApplyComboHints()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot == null) continue;
                slot.SetComboHint(HasHintFor(slot.Token));
            }
        }

        private bool HasHintFor(ComboInputToken token)
        {
            for (int i = 0; i < _hints.Count; i++)
                if (_hints[i].NextToken == token) return true;
            return false;
        }

        private void ClearComboHints()
        {
            _hints.Clear();
            for (int i = 0; i < _slots.Count; i++)
                _slots[i]?.SetComboHint(false);
        }

        private bool IsGrounded()
        {
            var controller = _player.PlayerController;
            return controller == null || controller.Motor == null
                   || controller.Motor.GroundingStatus.IsStableOnGround;
        }

        private static int ComputeSignature(IReadOnlyList<ComboInputToken> window, bool grounded)
        {
            int sig = grounded ? 1 : 2;
            for (int i = 0; i < window.Count; i++)
                sig = sig * 31 + ((int)window[i] + 1);
            return sig;
        }

        private void EnsureSlotsBound()
        {
            if (_slots.Count > 0)
                return;

            var slots = GetComponentsInChildren<UISkillSlot>(true);
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] != null)
                    _slots.Add(slots[i]);
        }
    }
}
