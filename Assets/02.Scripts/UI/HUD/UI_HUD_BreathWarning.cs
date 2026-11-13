using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>
    /// 숨이 줄어 위험해질수록 시야가 좁아지고 심박처럼 수축하는 터널 시야로 익사 직전을 알린다.
    /// 수중 색조는 카메라별 월드 물 합성이 소유하며 캐릭터 잠수 여부로 화면 전체에 덧씌우지 않는다.
    /// 수치 정책은 인스펙터 값이고, 그리기는 UnderwaterBreathOverlay 셰이더가 맡는다.
    /// </summary>
    public class UI_HUD_BreathWarning : UI_Base
    {
        private static readonly int UnderwaterBlendId = Shader.PropertyToID("_UnderwaterBlend");
        private static readonly int DangerId = Shader.PropertyToID("_Danger");
        private static readonly int UnscaledTimeId = Shader.PropertyToID("_UnscaledTime");

        [Header("참조")]
        [Tooltip("전체 화면을 덮는 오버레이 이미지. UnderwaterBreathOverlay 재질을 쓴다.")]
        [SerializeField] private Image _overlay;

        [Header("위험 기준 (남은 숨 비율)")]
        [Tooltip("이 비율 이하부터 경고가 시작된다.")]
        [SerializeField, Range(0f, 1f)] private float _dangerStartRatio = 0.4f;
        [Tooltip("이 비율 이하에서 경고가 최대가 된다.")]
        [SerializeField, Range(0f, 1f)] private float _dangerFullRatio = 0.05f;

        [Header("전환 속도 (초)")]
        [Tooltip("위험도가 오르내리는 데 걸리는 시간. 숨이 차오르는 느낌을 주려고 오를 때보다 천천히 내린다.")]
        [SerializeField, Min(0.01f)] private float _dangerRiseSeconds = 0.35f;
        [SerializeField, Min(0.01f)] private float _dangerFallSeconds = 0.9f;

        private Material _material;
        private PlayerActor _player;
        private float _danger;

        #region UI_Base

        protected override void OnInit()
        {
            base.OnInit();
            _layer = CanvasLayer.HUD;
            _canCloseWithEsc = false;
        }

        protected override void OnShow()
        {
            base.OnShow();
            EnsureMaterial();
            ResetEffect();
        }

        protected override void OnHide()
        {
            ResetEffect();
            base.OnHide();
        }

        protected override void OnDispose()
        {
            if (_material != null) Destroy(_material);
            _material = null;
            base.OnDispose();
        }

        #endregion

        protected override void Update()
        {
            base.Update();
            if (!IsVisible || _material == null) return;

            _player = UISvc.Actors?.Player;
            bool isUnderwater = _player != null && _player.IsAlive() && _player.IsUnderwater;
            float deltaTime = Time.unscaledDeltaTime;

            float dangerTarget = isUnderwater ? ResolveDanger(_player.BreathRatio) : 0f;
            float dangerSeconds = dangerTarget > _danger ? _dangerRiseSeconds : _dangerFallSeconds;
            _danger = Mathf.MoveTowards(_danger, dangerTarget, deltaTime / dangerSeconds);

            ApplyToMaterial();
        }

        /// <summary>남은 숨 비율을 0(안전)~1(위급) 위험도로 바꾼다.</summary>
        private float ResolveDanger(float breathRatio) =>
            1f - Mathf.Clamp01(Mathf.InverseLerp(_dangerFullRatio, _dangerStartRatio, breathRatio));

        private void ApplyToMaterial()
        {
            // 보이지 않는 동안은 그리지 않아 전체 화면 블렌딩 비용을 아낀다.
            bool isVisibleEffect = _danger > 0.001f;
            if (_overlay.enabled != isVisibleEffect) _overlay.enabled = isVisibleEffect;
            if (!isVisibleEffect) return;

            _material.SetFloat(UnderwaterBlendId, 0f);
            _material.SetFloat(DangerId, _danger);
            _material.SetFloat(UnscaledTimeId, Time.unscaledTime);
        }

        private void EnsureMaterial()
        {
            if (_material != null || _overlay == null) return;
            // 에셋 재질을 직접 바꾸면 값이 저장소에 남으므로 인스턴스를 쓴다.
            _material = new Material(_overlay.material) { name = "UnderwaterBreathOverlay (Instance)" };
            _overlay.material = _material;
            _overlay.raycastTarget = false;
        }

        private void ResetEffect()
        {
            _danger = 0f;
            if (_overlay != null) _overlay.enabled = false;
        }
    }
}
