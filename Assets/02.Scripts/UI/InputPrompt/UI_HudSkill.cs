using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.Combat;
using UPlayGround.Input;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.MovementController;

namespace UPlayGround.UI.InputPrompt
{
    /// <summary>
    /// 고정 스킬바 HUD. Ability(Skill1) / Ultimate(Skill2) 슬롯(<see cref="UISkillSlot"/>)들을 호스팅하며 두 신호로 구동한다:
    /// - <b>자원/Ready</b>: <see cref="PlayerSkillGauge.OnGaugeChanged"/> 이벤트 기반(핫패스 아님).
    /// - <b>콤보 다음 키 강조</b>: <see cref="ComboRouteResolver.CollectHints"/>를 입력 윈도우 변화 시에만 재계산.
    ///
    /// <see cref="UI_Base"/>를 상속해 UIManager 생명주기(Show/Hide)로 구동된다(다른 HUD와 동일).
    /// 활성 캐릭터는 <see cref="IUIPartyService"/>에서 받고 교체(OnSwapCompleted)를 따른다.
    /// ※ 슬롯 아이콘은 v1에서 프리팹 직렬화라 교체를 따라가지 않음(스왑 미추적, 캐릭터별 스킬 아이콘 파이프라인 부재).
    /// 프리팹 배치/슬롯 구성/글로우 연출은 Unity 에디터 작업.
    /// </summary>
    public class UI_HudSkill : UI_Base
    {
        [Tooltip("스킬바를 구성하는 슬롯들(왼→오). 각 슬롯은 자신의 토큰으로 정의된다.")]
        [SerializeField] private List<UISkillSlot> _slots = new();

        [Header("고정 슬롯 보강")]
        [Tooltip("프리팹에 Dash 슬롯이 없으면 런타임에 자동으로 추가한다.")]
        [SerializeField] private bool _ensureDashSlot = true;
        [Tooltip("Dash 슬롯 아이콘. 비워두면 키캡만 표시한다.")]
        [SerializeField] private Sprite _dashIcon;
        [Tooltip("Dash 슬롯 삽입 위치(0=맨 앞).")]
        [SerializeField] private int _dashSlotIndex = 1;

        private PlayerActor      _player;
        private PlayerCombat     _combat;
        private PlayerSkillGauge _gauge;
        private PlayerMovementController _movement; // 대시 쿨타임 소스(이동 컨트롤러 소유)
        private Func<ComboRouteEntry, bool> _resourceFilter; // 메서드그룹 delegate 캐시

        private IUIPartyService _partyManager;
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

            _partyManager = UISvc.Party;
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
            // 이전 대시 쿨타임 구독 해제
            if (_movement != null)
                _movement.OnDashCooldownChanged -= OnDashCooldownChanged;

            _player         = player;
            _combat         = player != null ? player.GetCombat() : null;
            _gauge          = player != null ? player.SkillGauge  : null;
            _movement       = player != null ? player.PlayerController : null;
            _resourceFilter = _combat != null ? _combat.CanAffordRoute : null;
            _lastSignature  = NoSignature; // 콤보 힌트 강제 재계산

            // 슬롯 1회 초기화(아이콘/키캡/게이지 슬롯)
            EnsureSlotsBound();
            for (int i = 0; i < _slots.Count; i++)
                _slots[i]?.Initialize();

            // 대시 슬롯의 쿨타임 소스를 이동 컨트롤러에 배선(게이지와 무관).
            WireDashCooldownSource();
            if (_movement != null)
                _movement.OnDashCooldownChanged += OnDashCooldownChanged;

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

        // 대시 쿨타임 시작/종료 트리거. 진행 중 잔여 시간은 Update의 _hasVisibleCooldown 폴링이 채운다.
        private void OnDashCooldownChanged(float remaining, float duration)
        {
            ApplyGaugeStates();
            _lastSignature = NoSignature;
        }

        // 대시 슬롯을 찾아 이동 컨트롤러의 쿨타임을 소스로 배선한다(플레이어 없으면 해제).
        private void WireDashCooldownSource()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot == null || slot.Token != ComboInputToken.Dash) continue;
                slot.SetCooldownSource(_movement != null ? SampleDashCooldown : null);
            }
        }

        private void SampleDashCooldown(out float remaining, out float duration)
        {
            if (_movement != null)
            {
                remaining = _movement.DashCooldownRemaining;
                duration  = _movement.DashCooldownDuration;
            }
            else
            {
                remaining = 0f;
                duration  = 0f;
            }
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
            if (!IsVisible || _player == null) return;

            // 쿨타임 잔여 시간 폴링은 전투와 무관(대시는 이동 기능)하므로 _combat 게이트 앞에서 처리한다.
            if (_hasVisibleCooldown)
                ApplyGaugeStates();

            if (_combat == null) return;

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
            if (_slots.Count == 0)
            {
                var slots = GetComponentsInChildren<UISkillSlot>(true);
                for (int i = 0; i < slots.Length; i++)
                    if (slots[i] != null)
                        _slots.Add(slots[i]);
            }

            EnsureDashSlot();
        }

        private void EnsureDashSlot()
        {
            if (!_ensureDashSlot || HasSlot(ComboInputToken.Dash))
                return;

            UISkillSlot template = FindSlotTemplate();
            if (template == null)
                return;

            Transform parent = template.transform.parent != null ? template.transform.parent : transform;
            UISkillSlot dashSlot = Instantiate(template, parent);
            dashSlot.name = "UISkillSlot_Dash";
            dashSlot.Configure(
                ComboInputToken.Dash,
                _dashIcon,
                inputActionOverride: null,
                useGaugeFeature: false,  // 대시는 게이지 비용이 없다(자원 UI 없음).
                showGaugeUi: false,
                showCooldownUi: true);   // 쿨타임은 이동 컨트롤러 소스로 표시한다.

            int index = Mathf.Clamp(_dashSlotIndex, 0, _slots.Count);
            dashSlot.transform.SetSiblingIndex(index);
            _slots.Insert(index, dashSlot);
        }

        private bool HasSlot(ComboInputToken token)
        {
            for (int i = 0; i < _slots.Count; i++)
                if (_slots[i] != null && _slots[i].Token == token)
                    return true;
            return false;
        }

        private UISkillSlot FindSlotTemplate()
        {
            for (int i = 0; i < _slots.Count; i++)
                if (_slots[i] != null)
                    return _slots[i];
            return null;
        }
    }
}
