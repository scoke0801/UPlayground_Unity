using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Ability.Core;

namespace UPlayGround.UI
{
    /// <summary>
    /// 체력바 위 얇은 레일에 필살기 에너지와 균등 분할 눈금, 준비 상태를 표시한다.
    /// 준비되면 빛과 불티·별빛·발동 키로 발동 시점을 알린다.
    /// </summary>
    public sealed class UIUltimateEnergyRail : MonoBehaviour
    {
        /// <summary>레일 위 눈금 하나. 기본 금빛 마름모 위에 충전 시 켜지는 발광을 겹친다.</summary>
        [Serializable]
        private struct Marker
        {
            public RectTransform root;
            public Graphic lit;
        }

        [Header("구성")]
        [Tooltip("눈금과 선두 광원의 가로 위치 기준. 채움 이미지와 같은 폭이어야 한다.")]
        [SerializeField] private RectTransform _fillArea;
        [Tooltip("UltimateRailEnergyCanvas 머티리얼을 쓰는 Filled 이미지. 머티리얼은 인스턴스로 복제해 쓴다.")]
        [SerializeField] private Image _energyFill;
        [Tooltip("소비 직후 줄어든 만큼을 잠시 남겨 보여 주는 잔상 채움.")]
        [SerializeField] private Image _trailFill;
        [SerializeField] private RectTransform _head;
        [SerializeField] private Graphic _readyGlow;
        [Tooltip("배열 순서와 무관하게 레일을 (개수 + 1)등분한 위치에 배치된다.")]
        [SerializeField] private Marker[] _markers = Array.Empty<Marker>();
        [SerializeField] private TMP_Text _cooldownText;
        [SerializeField] private CanvasGroup _group;

        [Header("가득 참 연출")]
        [Tooltip("UltimateRailSparksCanvas 머티리얼을 쓰는 불티 그래픽.")]
        [SerializeField] private Graphic _sparks;
        [SerializeField] private RectTransform[] _glints = Array.Empty<RectTransform>();
        [Tooltip("레일 끝에서 발동 키를 안내한다. 장치별 글리프는 하위 입력 아이콘이 맡는다.")]
        [SerializeField] private RectTransform _keyPrompt;
        [SerializeField, Min(0f)] private float _readyMorphDuration = 0.35f;
        [SerializeField, Min(0f)] private float _readyExitDuration = 0.12f;
        [SerializeField, Min(0f)] private float _keyPromptPopDuration = 0.18f;
        [SerializeField, Min(0f)] private float _glintSpinSpeed = 40f;
        [SerializeField, Range(0f, 1f)] private float _glintPulse = 0.18f;

        [Header("충전 연출")]
        [SerializeField, Min(0f)] private float _fillDuration = 0.3f;
        [SerializeField, Min(0f)] private float _trailDelay = 0.35f;
        [SerializeField, Min(0f)] private float _trailDuration = 0.4f;
        [Tooltip("한 번에 이 비율 이상 차오르면 채움 위로 빛이 훑고 지나간다(마무리 공격 등).")]
        [SerializeField, Range(0f, 1f)] private float _sweepThreshold = 0.2f;
        [SerializeField, Min(0f)] private float _sweepDuration = 0.45f;
        [SerializeField, Min(0f)] private float _markerPunch = 0.45f;
        [SerializeField, Min(0f)] private float _markerPunchDuration = 0.18f;

        [Header("상태 표시")]
        [Tooltip("기존 프리팹의 봉인 표식. 공용 게이지에서는 숨긴다.")]
        [SerializeField] private GameObject _sealMark;
        [SerializeField, Min(0.1f)] private float _readyPulsePeriod = 1.4f;
        [SerializeField, Range(0f, 1f)] private float _readyGlowMinAlpha = 0.45f;
        [SerializeField, Range(0f, 1f)] private float _unavailableAlpha = 0.45f;
        [SerializeField, Min(0f)] private float _shortCooldownThreshold = 3f;

        private static readonly int ClockId = Shader.PropertyToID("_UnscaledTime");
        private static readonly int ReadyId = Shader.PropertyToID("_Ready");
        private static readonly int SweepId = Shader.PropertyToID("_SweepPosition");
        private static readonly int AspectId = Shader.PropertyToID("_Aspect");

        private Material _energyMaterial, _sparksMaterial;
        private Tween _fillTween, _trailTween, _readyTween, _sweepTween, _promptTween;
        private Tween[] _markerTweens = Array.Empty<Tween>();
        private float _target, _displayed, _trail;
        private float _readyBlend, _sweepPosition = -1f;
        private int _cooldownSignature = int.MinValue;
        private bool _isReady, _isSealed, _reduceMotion;

        private bool CanAnimate => !_reduceMotion && isActiveAndEnabled && Application.isPlaying;

        /// <summary>캐릭터 속성 색을 에너지와 충전 연출에 적용한다.</summary>
        public void SetElementColor(Color color)
        {
            SetTint(_energyFill, color);
            SetTint(_trailFill, color);
            SetTint(_readyGlow, color);
            SetTint(_sparks, color);
            if (_head != null) SetTint(_head.GetComponent<Graphic>(), color);
            for (int i = 0; i < _markers.Length; i++) SetTint(_markers[i].lit, color);
            for (int i = 0; i < _glints.Length; i++)
                if (_glints[i] != null) SetTint(_glints[i].GetComponent<Graphic>(), color);
        }

        private static void SetTint(Graphic graphic, Color color)
        {
            if (graphic == null) return;
            color.a = graphic.color.a;
            graphic.color = color;
        }

        private void Awake()
        {
            _energyMaterial = CreateMaterialInstance(_energyFill);
            _sparksMaterial = CreateMaterialInstance(_sparks);
            LayoutMarkers();
            Clear();
        }

        /// <summary>
        /// 충전은 부드럽게 차오르고 소비는 즉시 줄인 뒤 잔상을 늦게 비운다.
        /// 눈금을 넘는 순간과 크게 차오른 순간에만 강조한다.
        /// </summary>
        public void SetResource(float current, float maximum, bool immediate = false)
        {
            float target = Normalize(current, maximum);
            if (_isReady && target < _target)
            {
                ExitReady();
                _isReady = false;
                if (_readyGlow != null) _readyGlow.gameObject.SetActive(false);
            }
            _target = target;
            _fillTween?.Kill();
            _trailTween?.Kill();
            if (immediate || !CanAnimate)
            {
                ApplyDisplayed(_target, punchCrossedMarkers: false);
                _trail = _target;
                ApplyTrail();
                return;
            }

            if (_target >= _displayed)
            {
                if (_target - _displayed >= _sweepThreshold) PlaySweep(_target);
                _trail = Mathf.Max(_trail, _target);
                ApplyTrail();
                _fillTween = DOTween.To(() => _displayed, value => ApplyDisplayed(value, true),
                        _target, _fillDuration)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true);
                return;
            }

            ApplyDisplayed(_target, punchCrossedMarkers: false);
            _trailTween = DOTween.To(() => _trail, value => { _trail = value; ApplyTrail(); },
                    _target, _trailDuration)
                .SetDelay(_trailDelay)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
        }

        /// <summary>
        /// 준비 완료는 발광·발동 키로, 일시 제한은 레일 명도로,
        /// 재사용 대기는 숫자로 구분한다.
        /// </summary>
        public void SetAbilityState(bool hasState, in AbilitySlotViewState state)
        {
            bool ready = hasState && state.IsReady;
            bool isUnlearned = hasState && (!state.IsUnlocked || state.BlockReason == AbilityActivationResult.Locked);
            if (isUnlearned && _isReady)
            {
                ExitReady();
                _isReady = false;
            }
            if (ready && !_isReady)
            {
                _isReady = true;
                EnterReady();
            }
            // 자원 부족은 채움량으로, 쿨다운은 숫자로 이미 전달되므로 어둡게 하지 않는다.
            bool isDimmed = !_isReady && (!hasState || (!ready
                && state.BlockReason != AbilityActivationResult.InsufficientResource
                && state.BlockReason != AbilityActivationResult.CooldownActive));
            // T/E도 소비하는 공용 자원이다. R의 미습득 여부는 스킬 슬롯의 자물쇠로만 전달한다.
            SetSealed(false);
            if (_group != null) _group.alpha = isUnlearned ? 1f : isDimmed ? _unavailableAlpha : 1f;
            if (_readyGlow != null) _readyGlow.gameObject.SetActive(_isReady);
            SetCooldown(hasState && !isUnlearned ? state.CooldownRemaining : 0f);
        }

        /// <summary>셰이더 시계와 준비 연출을 진행한다. 연출 감소 시에는 움직임 없이 최종 모습만 둔다.</summary>
        public void Tick(float unscaledTime, bool reduceMotion)
        {
            _reduceMotion = reduceMotion;
            PushMaterialState(reduceMotion ? 0f : unscaledTime);
            UpdateReadyFx(unscaledTime);
            if (!_isReady || _readyGlow == null) return;
            float wave = reduceMotion ? 1f : 0.5f + 0.5f * Mathf.Sin(unscaledTime * Mathf.PI * 2f / _readyPulsePeriod);
            Color color = _readyGlow.color;
            color.a = Mathf.Lerp(_readyGlowMinAlpha, 1f, wave);
            _readyGlow.color = color;
        }

        /// <summary>숨김·교체 시 잔상과 강조, 준비 연출을 모두 비운다.</summary>
        public void Clear()
        {
            KillTweens();
            _target = _trail = 0f;
            _readyBlend = 0f;
            _sweepPosition = -1f;
            _isReady = false;
            SetSealed(false);
            _cooldownSignature = int.MinValue;
            ApplyDisplayed(0f, punchCrossedMarkers: false);
            ApplyTrail();
            for (int i = 0; i < _markers.Length; i++)
                if (_markers[i].root != null) _markers[i].root.localScale = Vector3.one;
            if (_readyGlow != null) _readyGlow.gameObject.SetActive(false);
            if (_cooldownText != null) _cooldownText.gameObject.SetActive(false);
            if (_keyPrompt != null) _keyPrompt.gameObject.SetActive(false);
            if (_group != null) _group.alpha = 1f;
            UpdateReadyFx(0f);
            PushMaterialState(0f);
        }

        // 채움 값은 계속 갱신해 두고 보이는 것만 바꿔서, 습득 직후 실제 충전량이 바로 드러나게 한다.
        private void SetSealed(bool sealedState)
        {
            if (_isSealed == sealedState) return;
            _isSealed = sealedState;
            if (_fillArea != null) _fillArea.gameObject.SetActive(!sealedState);
            if (_sealMark != null) _sealMark.SetActive(sealedState);
        }

        private void EnterReady()
        {
            _readyTween?.Kill();
            if (CanAnimate)
                _readyTween = DOTween.To(() => _readyBlend, value => _readyBlend = value, 1f, _readyMorphDuration)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true);
            else
                _readyBlend = 1f;

            if (_keyPrompt == null) return;
            _promptTween?.Kill();
            _keyPrompt.gameObject.SetActive(true);
            _keyPrompt.localScale = CanAnimate ? Vector3.zero : Vector3.one;
            if (CanAnimate)
                _promptTween = _keyPrompt.DOScale(Vector3.one, _keyPromptPopDuration)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true);
        }

        // 에너지를 소비하면 키 안내를 즉시 끄고 발광은 짧게 되돌린다.
        private void ExitReady()
        {
            _readyTween?.Kill();
            _promptTween?.Kill();
            if (_keyPrompt != null) _keyPrompt.gameObject.SetActive(false);
            if (CanAnimate)
                _readyTween = DOTween.To(() => _readyBlend, value => _readyBlend = value, 0f, _readyExitDuration)
                    .SetEase(Ease.InCubic)
                    .SetUpdate(true);
            else
                _readyBlend = 0f;
        }

        private void PlaySweep(float endPosition)
        {
            _sweepTween?.Kill();
            _sweepPosition = 0f;
            _sweepTween = DOTween.To(() => _sweepPosition, value => _sweepPosition = value, endPosition, _sweepDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .OnComplete(() => _sweepPosition = -1f);
        }

        private void UpdateReadyFx(float unscaledTime)
        {
            bool showFx = _readyBlend > 0.001f;
            if (_sparks != null)
            {
                bool showSparks = showFx && !_reduceMotion;
                if (_sparks.gameObject.activeSelf != showSparks) _sparks.gameObject.SetActive(showSparks);
                if (showSparks)
                {
                    Color color = _sparks.color;
                    color.a = _readyBlend;
                    _sparks.color = color;
                }
            }

            for (int i = 0; i < _glints.Length; i++)
            {
                RectTransform glint = _glints[i];
                if (glint == null) continue;
                if (glint.gameObject.activeSelf != showFx) glint.gameObject.SetActive(showFx);
                if (!showFx) continue;
                float pulse = _reduceMotion ? 1f : 1f + _glintPulse * Mathf.Sin(unscaledTime * 4f + i * 1.7f);
                glint.localScale = Vector3.one * (_readyBlend * pulse);
                glint.localRotation = Quaternion.Euler(0f, 0f, _reduceMotion ? 0f : unscaledTime * _glintSpinSpeed * (i % 2 == 0 ? 1f : -1f));
            }
        }

        private void PushMaterialState(float clock)
        {
            if (_energyMaterial != null)
            {
                _energyMaterial.SetFloat(ClockId, clock);
                _energyMaterial.SetFloat(ReadyId, _readyBlend);
                _energyMaterial.SetFloat(SweepId, _sweepPosition);
                _energyMaterial.SetFloat(AspectId, AspectOf(_energyFill.rectTransform));
            }

            if (_sparksMaterial != null && _sparks != null && _sparks.gameObject.activeSelf)
            {
                _sparksMaterial.SetFloat(ClockId, clock);
                _sparksMaterial.SetFloat(AspectId, AspectOf(_sparks.rectTransform));
            }
        }

        private void ApplyDisplayed(float value, bool punchCrossedMarkers)
        {
            float previous = _displayed;
            _displayed = value;
            if (_energyFill != null) _energyFill.fillAmount = value;
            ApplyTrail();
            if (_head != null)
            {
                // 가득 차면 선두 광원 대신 레일과 양 끝 별빛이 강조를 맡는다.
                bool showHead = value > 0.001f && value < 0.999f;
                if (_head.gameObject.activeSelf != showHead) _head.gameObject.SetActive(showHead);
                if (showHead && _fillArea != null)
                    _head.anchoredPosition = new Vector2(
                        (value - _fillArea.pivot.x) * _fillArea.rect.width, _head.anchoredPosition.y);
            }

            for (int i = 0; i < _markers.Length; i++)
            {
                float ratio = MarkerRatio(i);
                bool isLit = value >= ratio;
                Graphic lit = _markers[i].lit;
                if (lit != null && lit.gameObject.activeSelf != isLit) lit.gameObject.SetActive(isLit);
                if (punchCrossedMarkers && previous < ratio && isLit) PunchMarker(i);
            }
        }

        private void ApplyTrail()
        {
            if (_trailFill == null) return;
            // 잔상을 상시 바탕으로 겹치면 셰이더의 얇은 윤곽이 불투명한 막대에 묻힌다.
            _trailFill.enabled = _trail > _displayed + 0.001f;
            _trailFill.fillAmount = Mathf.Max(_trail, _displayed);
        }

        private void PunchMarker(int index)
        {
            RectTransform root = _markers[index].root;
            if (root == null || !CanAnimate) return;
            if (_markerTweens.Length != _markers.Length) _markerTweens = new Tween[_markers.Length];
            _markerTweens[index]?.Kill();
            root.localScale = Vector3.one;
            _markerTweens[index] = root.DOPunchScale(Vector3.one * _markerPunch, _markerPunchDuration, 1, 0f)
                .SetUpdate(true)
                .OnKill(() => root.localScale = Vector3.one);
        }

        // 눈금은 저작 위치가 아니라 개수로 결정해 비용 구간이 레일 길이와 항상 맞도록 한다.
        private void LayoutMarkers()
        {
            if (_markerTweens.Length != _markers.Length) _markerTweens = new Tween[_markers.Length];
            if (_fillArea == null) return;
            for (int i = 0; i < _markers.Length; i++)
            {
                RectTransform root = _markers[i].root;
                if (root == null) continue;
                root.anchoredPosition = new Vector2(
                    (MarkerRatio(i) - _fillArea.pivot.x) * _fillArea.rect.width, root.anchoredPosition.y);
            }
        }

        private float MarkerRatio(int index) => (index + 1f) / (_markers.Length + 1f);

        private void SetCooldown(float remaining)
        {
            if (_cooldownText == null) return;
            bool visible = remaining > 0f;
            if (_cooldownText.gameObject.activeSelf != visible) _cooldownText.gameObject.SetActive(visible);
            if (!visible) { _cooldownSignature = int.MinValue; return; }
            bool shortTime = remaining < _shortCooldownThreshold;
            int tick = Mathf.CeilToInt(remaining * (shortTime ? 10f : 1f));
            int signature = shortTime ? -tick - 2 : tick;
            if (_cooldownSignature == signature) return;
            _cooldownSignature = signature;
            _cooldownText.SetText(shortTime ? "{0:1}" : "{0:0}", shortTime ? tick / 10f : tick);
        }

        private void KillTweens()
        {
            _fillTween?.Kill();
            _trailTween?.Kill();
            _readyTween?.Kill();
            _sweepTween?.Kill();
            _promptTween?.Kill();
            _fillTween = _trailTween = _readyTween = _sweepTween = _promptTween = null;
            for (int i = 0; i < _markerTweens.Length; i++) _markerTweens[i]?.Kill();
        }

        private static Material CreateMaterialInstance(Graphic graphic)
        {
            if (graphic == null || graphic.material == null || graphic.material == graphic.defaultMaterial)
                return null;
            var instance = new Material(graphic.material) { name = graphic.material.name + " (Instance)" };
            graphic.material = instance;
            return instance;
        }

        private static float AspectOf(RectTransform rect)
        {
            Rect r = rect.rect;
            return r.height > 0.01f ? r.width / r.height : 1f;
        }

        private static float Normalize(float current, float maximum) =>
            maximum > 0f && !float.IsNaN(current) && !float.IsInfinity(current) && !float.IsInfinity(maximum)
                ? Mathf.Clamp01(current / maximum)
                : 0f;

        private void OnDisable() => Clear();

        private void OnDestroy()
        {
            KillTweens();
            if (_energyMaterial != null) Destroy(_energyMaterial);
            if (_sparksMaterial != null) Destroy(_sparksMaterial);
        }
    }
}
