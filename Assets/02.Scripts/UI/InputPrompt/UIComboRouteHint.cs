using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.Combat;
using UPlayGround.Input;
using UPlayGround.Manager;

namespace UPlayGround.UI.InputPrompt
{
    /// <summary>
    /// 콤보 입력 기반 스킬 키 제시 HUD(명조식 상태 글로우, 방식1).
    ///
    /// 현재 입력 토큰 윈도우(<see cref="ComboInputTracker"/>)를 도중(prefix)으로 갖는 발동가능 라우트의
    /// '다음에 누를 토큰'을 <see cref="ComboRouteResolver.CollectHints"/>로 모아, 분기별로 한 줄씩
    /// (키 글리프 + 스킬명) 띄운다. 윈도우/지상상태가 바뀔 때만 재계산해 글리프 재해석 핫호출을 막는다.
    ///
    /// 활성 캐릭터는 <see cref="IUIPartyService"/>에서 받고 교체(OnSwapCompleted)에 따라간다.
    /// 프리팹 배치/행 템플릿/글리프 데이터 연결은 Unity 에디터 작업.
    /// </summary>
    public class UIComboRouteHint : MonoBehaviour
    {
        [Header("행 렌더")]
        [Tooltip("힌트 행이 배치될 컨테이너(VerticalLayoutGroup 권장)")]
        [SerializeField] private Transform _rowContainer;
        [Tooltip("힌트 1줄 템플릿. 비활성 자식으로 두면 복제해 사용한다.")]
        [SerializeField] private UIComboRouteHintRow _rowTemplate;
        [Tooltip("동시에 표시할 최대 분기 수")]
        [SerializeField] private int _maxRows = 4;

        private PlayerActor _player;
        private PlayerCombat _combat;
        private PlayerAbilityResourceView _gauge;
        private Func<ComboRouteEntry, bool> _resourceFilter; // 메서드그룹 delegate 캐시(프레임 alloc 방지)

        private IUIPartyService _partyManager;

        private readonly List<ComboRouteResolver.ComboRouteHint> _hints = new();
        private readonly List<UIComboRouteHintRow>              _pool  = new();

        private int _lastSignature = NoSignature;
        private const int NoSignature = int.MinValue;

        private void Awake()
        {
            // 템플릿 자신은 숨기고 복제본만 켠다(씬 인스턴스일 때만 — 프리팹 에셋 보호).
            if (_rowTemplate != null && _rowTemplate.gameObject.scene.IsValid())
                _rowTemplate.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            _partyManager = UISvc.Party;
            if (_partyManager != null)
            {
                _partyManager.OnSwapCompleted += OnSwapCompleted;
                Bind(_partyManager.ActiveCharacter);
            }
            else
            {
                Bind(FindFirstObjectByType<PlayerActor>());
            }
        }

        private void OnDisable()
        {
            if (_partyManager != null)
            {
                _partyManager.OnSwapCompleted -= OnSwapCompleted;
                _partyManager = null;
            }
            Bind(null);
        }

        private void OnSwapCompleted(PlayerActor newPlayer) => Bind(newPlayer);

        private void Bind(PlayerActor player)
        {
            if (_gauge != null)
            {
                _gauge.OnGaugeChanged -= OnGaugeChanged;
                _gauge.OnCooldownChanged -= OnSkillCooldownChanged;
            }

            _player         = player;
            _combat         = player != null ? player.GetCombat() : null;
            _gauge          = player != null ? player.SkillGauge  : null;
            // 자원 조건 + 성장 해금(잠긴 약+강 조합은 힌트에서도 숨김) 결합.
            _resourceFilter = _combat != null
                ? route => _combat.CanAffordRoute(route) && IsRouteUnlocked(route)
                : (Func<ComboRouteEntry, bool>)null;
            _lastSignature  = NoSignature; // 강제 재계산

            if (_gauge != null)
            {
                _gauge.OnGaugeChanged += OnGaugeChanged;
                _gauge.OnCooldownChanged += OnSkillCooldownChanged;
            }

            if (player == null)
                HideAll();
        }

        private void OnGaugeChanged(float current, float max)
        {
            _lastSignature = NoSignature; // 자원 게이트가 바뀌면 힌트를 다시 계산한다.
        }

        private void OnSkillCooldownChanged(int skillSlot, float remaining, float duration)
        {
            _lastSignature = NoSignature;
        }

        private void Update()
        {
            if (_player == null || _combat == null)
                return;

            var tracker = _player.ComboInputTracker;
            var window  = tracker.GetWindow();

            bool grounded = IsGrounded();
            int signature = ComputeSignature(window, grounded);
            if (signature == _lastSignature)
                return;                       // 입력 윈도우/지상상태 불변 → 재해석 생략
            _lastSignature = signature;

            ComboRouteResolver.CollectHints(
                window, _combat.ComboRoutes, _player.Tags, grounded, _resourceFilter, _hints);

            // 완성에 가까운(이미 더 많이 입력된) 분기를 우선 노출 → _maxRows 컷에서 임의 누락 방지.
            _hints.Sort((a, b) => b.MatchedLength.CompareTo(a.MatchedLength));

            Render();
        }

        private bool IsRouteUnlocked(ComboRouteEntry route)
        {
            if (_partyManager == null || route == null) return true;
            return _partyManager.IsComboRouteUnlocked(_partyManager.ActiveCharacterType, route.routeName);
        }

        private bool IsGrounded()
        {
            var controller = _player.PlayerController;
            // 컨트롤러/모터 미확보 시 지상 가정(지상 라우트를 과하게 숨기지 않음).
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

        private void Render()
        {
            int shown = Mathf.Min(_hints.Count, _maxRows);
            EnsurePool(shown);

            for (int i = 0; i < _pool.Count; i++)
            {
                if (i < shown && ComboTokenInput.TryGetAction(
                        _hints[i].NextToken, out string map, out string action, out bool isHold))
                {
                    _pool[i].gameObject.SetActive(true);
                    _pool[i].Set(map, action, _hints[i].Route.DisplayLabel, isHold);
                }
                else
                {
                    _pool[i].gameObject.SetActive(false);
                }
            }

            if (_rowContainer != null)
                _rowContainer.gameObject.SetActive(shown > 0);
        }

        private void HideAll()
        {
            _hints.Clear();
            for (int i = 0; i < _pool.Count; i++)
                _pool[i].gameObject.SetActive(false);
            if (_rowContainer != null)
                _rowContainer.gameObject.SetActive(false);
        }

        private void EnsurePool(int count)
        {
            while (_pool.Count < count && _rowTemplate != null && _rowContainer != null)
            {
                var row = Instantiate(_rowTemplate, _rowContainer);
                _pool.Add(row);
            }
        }
    }
}
