using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Party;

namespace UPlayGround.UI
{
    /// <summary>실제 자원과 사용 가능 상태를 독립적으로 표시하는 공용 문양 게이지.</summary>
    public sealed class UICharacterGauge : MonoBehaviour
    {
        [SerializeField] private RawImage _image;
        [SerializeField] private Material _materialTemplate;
        [SerializeField] private CharacterGaugeVisualProfileSO _fallbackProfile;
        [SerializeField] private GameObject _readyMark;
        [SerializeField] private GameObject _lockedMark;
        [SerializeField] private TMP_Text _cooldownText;
        [SerializeField] private CanvasGroup _group;
        [SerializeField, Min(0f)] private float _shortCooldownThreshold = 3f;
        private CharacterGaugeVisualProfileSO _profile;
        private Material _material;
        private Tween _fillTween, _readyTween, _swapTween;
        private float _resource, _displayedResource, _pulse;
        private int _cooldownTick = -1;
        private bool _isReady, _isLocked, _reduceMotion;
        private static readonly int ResourceId = Shader.PropertyToID("_Resource");
        private static readonly int TrailId = Shader.PropertyToID("_Trail");
        private static readonly int ClockId = Shader.PropertyToID("_UnscaledTime");
        private static readonly int PulseId = Shader.PropertyToID("_ReadyPulse");
        private static readonly int LockedId = Shader.PropertyToID("_Locked");

        /// <summary>캐릭터 교체 시 이전 표시를 비우고 새 프리셋을 연결합니다.</summary>
        public void Bind(CharacterGaugeVisualProfileSO profile, bool reduceMotion)
        {
            Clear();
            _reduceMotion = reduceMotion;
            _profile = profile != null ? profile : _fallbackProfile;
            var artwork = _profile != null ? _profile.ResolveArtwork() : null;
            if (artwork == null && _fallbackProfile != null)
                artwork = _fallbackProfile.ResolveArtwork();
            if (_image == null || _materialTemplate == null || artwork == null)
            {
                if (_image != null) _image.enabled = false;
                return;
            }
            // 스텐실 캐시가 이전 캐릭터의 색·텍스처를 재사용하지 않도록 소유권을 교체합니다.
            _image.material = null;
            if (_material != null) Destroy(_material);
            _material = new Material(_materialTemplate) { name = "CharacterGauge (Instance)" };
            _image.material = _material;
            _image.enabled = true;
            _image.texture = artwork.silhouetteSprite.texture;
            Rect rect = artwork.silhouetteSprite.rect;
            var texture = artwork.silhouetteSprite.texture;
            _image.uvRect = new Rect(rect.x / texture.width, rect.y / texture.height,
                rect.width / texture.width, rect.height / texture.height);
            _material.SetVector("_SpriteRect", new Vector4(_image.uvRect.x, _image.uvRect.y,
                _image.uvRect.width, _image.uvRect.height));
            _material.SetTexture("_FlowMap", artwork.flowMap);
            _material.SetTexture("_FillMask", artwork.fillMask != null ? artwork.fillMask : Texture2D.whiteTexture);
            _material.SetColor("_BaseColor", _profile.baseColor);
            _material.SetColor("_EnergyColor", _profile.energyColor);
            _material.SetColor("_ReadyColor", _profile.readyColor);
            _material.SetFloat("_EdgeWidth", _profile.edgeWidth);
            _material.SetFloat("_NoiseStrength", _profile.noiseStrength);
            _material.SetFloat("_FlowSpeed", reduceMotion ? 0f : _profile.flowSpeed);
            _material.SetFloat("_GlowIntensity", _profile.glowIntensity);
            _image.rectTransform.sizeDelta = _profile.referenceSize * _profile.accessibilityScale;
            if (transform is RectTransform root) root.anchoredPosition = _profile.anchorOffset;
            PushMaterialState();
            if (_group == null) return;
            _group.alpha = reduceMotion ? 1f : 0f;
            if (!reduceMotion)
                _swapTween = DOTween.To(() => _group.alpha, value => _group.alpha = value,
                    1f, _profile.swapFadeDuration).SetUpdate(true);
        }

        /// <summary>최대값이 없는 상태는 비우고, 연속 소비·회복은 마지막 값으로 수렴시킵니다.</summary>
        public void SetResource(float current, float maximum, bool immediate = false)
        {
            _resource = Normalize(current, maximum);
            _fillTween?.Kill();
            if (immediate || _reduceMotion || _profile == null || !isActiveAndEnabled)
                _displayedResource = _resource;
            else
                _fillTween = DOTween.To(() => _displayedResource, value =>
                {
                    _displayedResource = value;
                    PushMaterialState();
                }, _resource, _profile.fillTweenDuration).SetEase(Ease.OutCubic).SetUpdate(true);
            PushMaterialState();
        }

        /// <summary>완충과 실행 가능을 혼동하지 않고 런타임 판정만 표시합니다.</summary>
        public void SetAbilityState(bool hasState, in AbilitySlotViewState state)
        {
            bool ready = hasState && state.IsReady;
            bool locked = !hasState || (!ready && state.BlockReason != AbilityActivationResult.InsufficientResource);
            if (_readyMark != null) _readyMark.SetActive(ready);
            if (_lockedMark != null) _lockedMark.SetActive(locked);
            if (ready && !_isReady && !_reduceMotion && _profile != null && isActiveAndEnabled)
            {
                _readyTween?.Kill();
                _pulse = 1f;
                _readyTween = DOTween.To(() => _pulse, value =>
                {
                    _pulse = value;
                    PushMaterialState();
                }, 0f, _profile.readyPulseDuration).SetUpdate(true);
            }
            if (!ready)
            {
                _readyTween?.Kill();
                _pulse = 0f;
            }
            _isReady = ready;
            _isLocked = locked;
            SetCooldown(hasState ? state.CooldownRemaining : 0f);
            PushMaterialState();
        }

        /// <summary>일시정지 중에도 흐름을 진행하며 연출 감소 시에는 정지합니다.</summary>
        public void Tick(float unscaledTime, bool reduceMotion)
        {
            if (_reduceMotion != reduceMotion)
            {
                _reduceMotion = reduceMotion;
                if (reduceMotion)
                {
                    _fillTween?.Kill(); _readyTween?.Kill(); _swapTween?.Kill();
                    _displayedResource = _resource; _pulse = 0f;
                    if (_group != null) _group.alpha = 1f;
                }
                if (_material != null && _profile != null)
                    _material.SetFloat("_FlowSpeed", reduceMotion ? 0f : _profile.flowSpeed);
                PushMaterialState();
            }
            if (_material == null || _image == null) return;
            // Mask가 생성하는 스텐실 머티리얼에도 인스턴스의 동적 값을 전달합니다.
            var rendered = _image.materialForRendering;
            if (rendered != null)
            {
                rendered.SetFloat(ClockId, reduceMotion ? 0f : unscaledTime);
                rendered.SetFloat("_FlowSpeed", reduceMotion || _profile == null ? 0f : _profile.flowSpeed);
                ApplyDynamicProperties(rendered);
            }
        }

        /// <summary>숨김·교체 시 잔상, 준비 강조, 상태 표식을 모두 초기화합니다.</summary>
        public void Clear()
        {
            _fillTween?.Kill(); _readyTween?.Kill(); _swapTween?.Kill();
            _fillTween = _readyTween = _swapTween = null;
            _resource = _displayedResource = _pulse = 0f;
            _isReady = _isLocked = false;
            _cooldownTick = -1;
            if (_readyMark != null) _readyMark.SetActive(false);
            if (_lockedMark != null) _lockedMark.SetActive(false);
            if (_cooldownText != null) _cooldownText.gameObject.SetActive(false);
            if (_group != null) _group.alpha = 1f;
            PushMaterialState();
        }

        /// <summary>유효하지 않은 자원 값은 빈 상태로 처리합니다.</summary>
        public static float Normalize(float current, float maximum) =>
            maximum > 0f && !float.IsNaN(current) && !float.IsInfinity(current)
                && !float.IsInfinity(maximum) ? Mathf.Clamp01(current / maximum) : 0f;

        private void SetCooldown(float remaining)
        {
            if (_cooldownText == null) return;
            bool visible = remaining > 0f;
            _cooldownText.gameObject.SetActive(visible);
            if (!visible) { _cooldownTick = -1; return; }
            bool shortTime = remaining < _shortCooldownThreshold;
            int tick = Mathf.CeilToInt(remaining * (shortTime ? 10f : 1f));
            int signature = shortTime ? -tick - 2 : tick;
            if (_cooldownTick == signature) return;
            _cooldownTick = signature;
            _cooldownText.SetText(shortTime ? "{0:1}" : "{0:0}", shortTime ? tick / 10f : tick);
        }

        private void PushMaterialState()
        {
            if (_material != null) ApplyDynamicProperties(_material);
            if (_image != null) _image.SetMaterialDirty();
        }

        private void ApplyDynamicProperties(Material material)
        {
            material.SetFloat(ResourceId, _resource);
            material.SetFloat(TrailId, _displayedResource);
            material.SetFloat(PulseId, _pulse);
            material.SetFloat(LockedId, _isLocked ? 1f : 0f);
        }

        private void OnDisable() => Clear();
        private void OnDestroy()
        {
            Clear();
            if (_image != null) _image.material = null;
            if (_material != null) Destroy(_material);
        }
    }
}
