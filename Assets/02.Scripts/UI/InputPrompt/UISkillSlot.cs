using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Combat;

namespace UPlayGround.UI.InputPrompt
{
    /// <summary>
    /// 고정 스킬바의 슬롯 1개(명조식 스킬 버튼). 슬롯은 <see cref="ComboInputToken"/>으로 정의되며
    /// 그 토큰이 키캡 글리프·콤보 힌트 매칭·게이지 슬롯을 모두 결정한다.
    ///
    /// 글로우는 모두 <b>콤보 힌트</b>로 켜진다(게이지 충족과 무관):
    /// - <b>ReadyGlow</b> / <b>ComboGlow</b>: 현재 콤보의 '다음 키'가 이 슬롯일 때 켠다(CollectHints).
    /// 게이지(자원) 부족은 글로우가 아니라 <b>dim</b>으로만 표현한다.
    /// </summary>
    public class UISkillSlot : MonoBehaviour
    {
        [Header("정의")]
        [Tooltip("이 슬롯이 대표하는 입력 토큰. 키캡/힌트/게이지 슬롯을 결정한다.")]
        [SerializeField] private ComboInputToken _token = ComboInputToken.Skill1;

        [Tooltip("스킬 아이콘. ※v1: 프리팹 직렬화라 캐릭터 교체를 따라가지 않음(스왑 미추적).")]
        [SerializeField] private Sprite _icon;

        [Tooltip("게이지 비용 슬롯 오버라이드(-1=토큰에서 자동: Skill1→0, Skill2→1, 그 외 게이지 없음).")]
        [SerializeField] private int _gaugeSlotOverride = -1;

        [Header("렌더 타깃")]
        [SerializeField] private Image              _iconImage;
        [SerializeField] private UI_InputPromptIcon _keyIcon;   // 키캡 글리프(디바이스 자동 전환)
        [SerializeField] private GameObject         _readyGlow;  // 콤보 다음 키 발광
        [SerializeField] private GameObject         _comboGlow;  // 콤보 다음 키 강조(추가 연출)
        [Tooltip("게이지 부족 시 어둡게(선택). 없으면 무시.")]
        [SerializeField] private CanvasGroup        _dimGroup;
        [SerializeField] private float              _dimAlpha = 0.5f;

        public ComboInputToken Token => _token;

        /// <summary>이 슬롯이 게이지를 요구하는지(스킬 슬롯).</summary>
        public bool RequiresGauge { get; private set; }
        /// <summary>게이지 비용 슬롯 인덱스(RequiresGauge일 때 유효).</summary>
        public int GaugeSlot { get; private set; }

        /// <summary>아이콘/키캡/게이지 슬롯을 1회 설정한다(바인드 시 호출).</summary>
        public void Initialize()
        {
            if (_iconImage != null)
            {
                _iconImage.sprite  = _icon;
                _iconImage.enabled = _icon != null;
            }

            if (_keyIcon != null &&
                ComboTokenInput.TryGetAction(_token, out string map, out string action, out _))
                _keyIcon.SetAction(map, action);

            ResolveGaugeSlot();
            SetComboHint(false);
        }

        private void ResolveGaugeSlot()
        {
            if (_gaugeSlotOverride >= 0)
            {
                RequiresGauge = true;
                GaugeSlot     = _gaugeSlotOverride;
                return;
            }

            switch (_token)
            {
                case ComboInputToken.Skill1: RequiresGauge = true; GaugeSlot = 0; break;
                case ComboInputToken.Skill2: RequiresGauge = true; GaugeSlot = 1; break;
                default:                     RequiresGauge = false; GaugeSlot = -1; break;
            }
        }

        /// <summary>
        /// 게이지 상태 갱신. ready=지금 사용 가능(게이지 충족). 게이지는 글로우가 아니라 <b>dim</b>으로만
        /// 표현한다(부족 시 어둡게). 공유 게이지의 연속 fill은 파티 패널이 담당.
        /// 게이지 비요구 슬롯(약/강 등)은 항상 ready 취급.
        /// </summary>
        public void SetGaugeState(bool ready)
        {
            if (_dimGroup != null)
                _dimGroup.alpha = (RequiresGauge && !ready) ? _dimAlpha : 1f;
        }

        /// <summary>콤보 '다음 키' 강조 토글. ReadyGlow/ComboGlow 모두 콤보 힌트로만 켠다.</summary>
        public void SetComboHint(bool active)
        {
            if (_readyGlow != null)
                _readyGlow.SetActive(active);

            if (_comboGlow != null)
                _comboGlow.SetActive(active);
        }
    }
}
