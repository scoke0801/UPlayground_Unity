using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

namespace UPlayGround.UI.InputPrompt
{
    /// <summary>실제 전투 라우트에서 입력 진행도와 후속 분기를 읽어 훈련 안내를 표시한다.</summary>
    public class UIComboRouteHint : MonoBehaviour
    {
        [SerializeField] private Transform _rowContainer;
        [SerializeField] private UIComboRouteHintRow _rowTemplate;
        [SerializeField, Min(1)] private int _maxRows = 5;
        [SerializeField] private bool _trainingOnly;
        [SerializeField] private CanvasGroup _panel;
        [SerializeField] private UIComboRouteHintRow _historyRow;
        [SerializeField] private UIComboRouteHintRow _skillRow;
        [SerializeField] private TMP_Text _feedbackLabel;
        [SerializeField, Min(0.02f)] private float _refreshInterval = 0.05f;
        [SerializeField, Min(0f)] private float _feedbackDuration = 1.2f;
        [SerializeField, Min(0f)] private float _fadeDuration = 0.12f;

        private PlayerActor _player;
        private PlayerCombat _combat;
        private IUIPartyService _party;
        private Func<ComboRouteEntry, bool> _resourceFilter;
        private readonly List<ComboRouteResolver.ComboRouteHint> _hints = new();
        private readonly List<UIComboRouteHintRow> _pool = new();
        private readonly List<ComboInputToken> _candidate = new();
        private readonly List<ComboInputToken> _completedPattern = new();
        private static readonly ComboInputToken[] SkillPattern = { ComboInputToken.Skill1 };
        private float _nextRefresh;
        private float _feedbackUntil;
        private bool _isVisible;
        private Tween _fade;

        private void Awake()
        {
            if (_rowTemplate != null) _rowTemplate.gameObject.SetActive(false);
            if (_panel != null)
            {
                _panel.alpha = 0f;
                _panel.interactable = false;
                _panel.blocksRaycasts = false;
            }
        }

        private void OnEnable() => _nextRefresh = 0f;

        private void OnDisable()
        {
            Bind(null);
            _party = null;
            _fade?.Kill();
            _fade = null;
            _isVisible = false;
            if (_panel != null) _panel.alpha = 0f;
            HideRows();
        }

        private void Bind(PlayerActor player)
        {
            if (_combat != null) _combat.OnComboRouteExecuted -= OnRouteExecuted;
            _player = player;
            _combat = player != null ? player.GetCombat() : null;
            _resourceFilter = _combat != null ? _combat.CanAffordRoute : (Func<ComboRouteEntry, bool>)null;
            if (_combat != null) _combat.OnComboRouteExecuted += OnRouteExecuted;
            _feedbackUntil = 0f;
            _completedPattern.Clear();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + _refreshInterval;
            bool contextReady = !_trainingOnly || Services.TryGet<IUITrainingService>(out _);
            if (!contextReady)
            {
                if (_player != null) Bind(null);
                SetVisible(false);
                return;
            }
            // 훈련장 세션과 파티는 HUD보다 늦게 등록될 수 있다.
            if (_party == null) Services.TryGet(out _party);
            PlayerActor active = _party?.ActiveCharacter;
            if (active != _player) Bind(active);
            bool visible = _player != null && _player.IsAlive() && _combat?.ComboRoutes?.Count > 0;
            SetVisible(visible);
            if (!visible) return;

            var window = _player.ComboInputTracker.GetWindow();
            var motor = _player.PlayerController?.Motor;
            bool grounded = motor == null || motor.GroundingStatus.IsStableOnGround;
            ComboRouteResolver.CollectHints(window, _combat.ComboRoutes, _player.Tags,
                grounded, _trainingOnly ? null : _resourceFilter, _hints, _trainingOnly, _combat.RouteContext);
            RemoveShadowedHints(window, grounded);
            _hints.Sort(CompareHints);
            RenderRows();
            RenderHistory(window);
            RenderSkill(grounded);
        }

        private static int CompareHints(ComboRouteResolver.ComboRouteHint a, ComboRouteResolver.ComboRouteHint b)
        {
            int contextual = b.Route.HasSourceCondition.CompareTo(a.Route.HasSourceCondition);
            if (contextual != 0) return contextual;
            int progress = b.MatchedLength.CompareTo(a.MatchedLength);
            if (progress != 0) return progress;
            int length = a.Route.inputPattern.Count.CompareTo(b.Route.inputPattern.Count);
            return length != 0 ? length : string.CompareOrdinal(a.Route.routeName, b.Route.routeName);
        }

        private void RemoveShadowedHints(IReadOnlyList<ComboInputToken> window, bool grounded)
        {
            // 같은 강 입력에 긴 패턴이 우선하면 실제로 나가지 않을 짧은 마무리를 안내하지 않는다.
            for (int i = _hints.Count - 1; i >= 0; i--)
            {
                var hint = _hints[i];
                if (hint.MatchedLength != hint.Route.inputPattern.Count - 1) continue;
                _candidate.Clear();
                for (int j = 0; j < window.Count; j++) _candidate.Add(window[j]);
                _candidate.Add(hint.NextToken);
                var winner = ComboRouteResolver.Resolve(_candidate, _combat.ComboRoutes,
                    _player.Tags, grounded, _resourceFilter, _combat.RouteContext);
                if (winner != null && winner != hint.Route) _hints.RemoveAt(i);
            }
        }

        private void RenderRows()
        {
            int count = Mathf.Min(_maxRows, _hints.Count);
            while (_pool.Count < count && _rowTemplate != null && _rowContainer != null)
                _pool.Add(Instantiate(_rowTemplate, _rowContainer));
            for (int i = 0; i < _pool.Count; i++)
            {
                bool shown = i < count;
                _pool[i].gameObject.SetActive(shown);
                if (!shown) continue;
                var hint = _hints[i];
                if (_trainingOnly)
                {
                    bool available = _combat.CanAffordRoute(hint.Route);
                    string status = GetRouteStatus(hint, available);
                    string label = hint.Route.HasSourceCondition ? "다음: " + hint.Route.DisplayLabel : hint.Route.DisplayLabel;
                    _pool[i].SetSequence(hint.Route.inputPattern, hint.MatchedLength, label, status);
                }
                else if (ComboTokenInput.TryGetAction(hint.NextToken, out string map, out string action, out bool hold))
                    _pool[i].Set(map, action, hint.Route.DisplayLabel, hold);
            }
        }

        private static string GetRouteStatus(ComboRouteResolver.ComboRouteHint hint, bool available)
        {
            if (!available) return "기력 / 해금 확인";
            if (hint.Route.HasSourceCondition)
                return string.IsNullOrEmpty(hint.Route.purpose) ? "후속 공격" : hint.Route.purpose;
            if (hint.MatchedLength == 0) return "입력 순서";
            return hint.MatchedLength == hint.Route.inputPattern.Count - 1 ? "다음 입력으로 마무리" : "이어가기";
        }

        private void RenderHistory(IReadOnlyList<ComboInputToken> window)
        {
            bool completed = Time.unscaledTime < _feedbackUntil;
            if (_historyRow != null)
                _historyRow.SetSequence(completed ? _completedPattern : window,
                    completed ? _completedPattern.Count : window.Count, "현재 연계", "");
            if (!completed && _feedbackLabel != null)
            {
                int hiddenCount = Mathf.Max(0, _hints.Count - _maxRows);
                _feedbackLabel.text = hiddenCount > 0
                    ? $"추천 연계 {_maxRows}개 표시 / 외 {hiddenCount}개"
                    : _combat.RouteContext.SourceMotion.IsValid
                        ? "밝은 입력을 눌러 이어가세요"
                        : "약공격이나 스킬로 시작하세요";
            }
        }

        private void RenderSkill(bool grounded)
        {
            if (_skillRow == null || _player.Abilities == null) return;
            var result = _player.Abilities.EvaluatePlayerSlot(PlayerSkillSlot.Ability, grounded, null, out _);
            bool ready = result == UPlayGround.Ability.Core.AbilityActivationResult.Success;
            bool canLink = (_combat.ResolveCancelMask() & PlayerInterruptAction.Skill) != 0;
            string status = "재사용 대기 / 조건 미충족";
            if (ready) status = canLink ? "지금 연결 가능" : "준비됨 / 공격 후 연결";
            _skillRow.SetSequence(SkillPattern, 0, "고유 스킬 → 약 / 강 후속", status);
        }

        private void OnRouteExecuted(ComboRouteEntry route)
        {
            _completedPattern.Clear();
            _completedPattern.AddRange(route.inputPattern);
            _feedbackUntil = Time.unscaledTime + _feedbackDuration;
            if (_feedbackLabel != null) _feedbackLabel.text = route.DisplayLabel + " / 연계 성공";
        }

        private void SetVisible(bool visible)
        {
            if (_isVisible == visible) return;
            _isVisible = visible;
            if (!visible) HideRows();
            if (_panel == null) return;
            _fade?.Kill();
            _fade = DOTween.To(() => _panel.alpha, value => _panel.alpha = value,
                visible ? 1f : 0f, _fadeDuration).SetUpdate(true);
        }

        private void HideRows()
        {
            for (int i = 0; i < _pool.Count; i++) _pool[i].gameObject.SetActive(false);
        }
    }
}
