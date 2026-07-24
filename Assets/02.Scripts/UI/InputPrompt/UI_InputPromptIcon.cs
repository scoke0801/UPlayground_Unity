using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.UI.InputPrompt;

namespace UPlayGround.UI.InputPrompt
{
    /// <summary>
    /// 한 액션의 입력 키를 키캡 글리프(또는 폴백 텍스트)로 표시하는 위젯.
    ///
    /// - 단일 키(대부분): 인스펙터의 Image(+선택적 폴백 Label)에 그린다. 무설정 동작.
    /// - 콤보(복합 바인딩, 예: Dodge = L1+R1): 콤보 컨테이너/템플릿이 지정돼 있으면 파트별로
    ///   "L1 + R1"처럼 여러 글리프를 그린다. 미지정 시 첫 파트만 표시(공백 대신 degraded).
    ///
    /// 활성 디바이스(키보드+마우스 ↔ 게임패드) 또는 게임패드 브랜드 전환 시
    /// IInputService.OnActiveDeviceChanged를 받아 자동으로 글리프를 교체한다. (요구사항 1·2 + Phase 3)
    /// </summary>
    public class UI_InputPromptIcon : MonoBehaviour
    {
        [Header("표시할 액션")]
        [SerializeField] private string _mapName = InputMapNames.PlayerAction;
        [SerializeField] private string _actionName = PlayerAction.Interact;

        [Header("데이터")]
        [SerializeField] private InputGlyphDataSO _glyphData;

        [Header("단일 키 렌더 타깃")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _fallbackLabel; // 스프라이트 미등록 시 표시(선택)

        [Header("콤보(복합 바인딩) 렌더 — 선택")]
        [Tooltip("파트 항목이 배치될 컨테이너(HorizontalLayoutGroup 권장)")]
        [SerializeField] private Transform _comboContainer;
        [Tooltip("파트 1개를 표시하는 템플릿. 비활성 자식으로 두면 복제해 사용한다.")]
        [SerializeField] private UI_InputPromptGlyphItem _comboItemTemplate;

        // OnDisable에서 안전하게 구독 해제하기 위해 캐시. (Instance getter는 null이면 새로 생성하므로 직접 호출 금지)
        private IInputService _inputManager;
        private readonly List<UI_InputPromptGlyphItem> _comboPool = new();

        private void Awake()
        {
            // 템플릿 자신은 표시되지 않도록 비활성화(복제본만 켠다).
            // 단, 프리팹 에셋이 할당된 경우 에셋을 건드리지 않도록 씬 인스턴스일 때만 처리.
            if (_comboItemTemplate != null && _comboItemTemplate.gameObject.scene.IsValid())
                _comboItemTemplate.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            _inputManager = Svc.Input;
            if (_inputManager != null)
            {
                _inputManager.OnActiveDeviceChanged += OnDeviceChanged;
                _inputManager.OnBindingsChanged += Refresh;
                Refresh();
            }
        }

        private void OnDisable()
        {
            if (_inputManager != null)
            {
                _inputManager.OnActiveDeviceChanged -= OnDeviceChanged;
                _inputManager.OnBindingsChanged -= Refresh;
                _inputManager = null;
            }
        }

        private void OnDeviceChanged(ActiveInputDevice device) => Refresh();

        /// <summary>
        /// 표시할 액션을 런타임에 바꾸고 즉시 갱신한다.
        /// </summary>
        public void SetAction(string mapName, string actionName)
        {
            _mapName = mapName;
            _actionName = actionName;
            Refresh();
        }

        private void Refresh()
        {
            if (_inputManager == null)
                _inputManager = Svc.Input;

            if (_inputManager == null)
                return;

            var device = _inputManager.ActiveDevice;
            var brand = _inputManager.GamepadBrand;
            var result = InputGlyphResolver.Resolve(_mapName, _actionName, device, brand, _glyphData);

            bool canRenderCombo = result.Count > 1 && _comboContainer != null && _comboItemTemplate != null;
            if (canRenderCombo)
                RenderCombo(result.Parts);
            else
                RenderSingle(result.Count > 0 ? result.Primary : GlyphPart.TextOnly(_actionName));
        }

        private void RenderSingle(in GlyphPart part)
        {
            if (_comboContainer != null)
                _comboContainer.gameObject.SetActive(false);

            bool hasSprite = part.HasSprite;

            if (_iconImage != null)
            {
                _iconImage.enabled = hasSprite;
                if (hasSprite)
                    _iconImage.sprite = part.Sprite;
            }

            if (_fallbackLabel != null)
            {
                _fallbackLabel.enabled = !hasSprite;
                if (!hasSprite)
                    _fallbackLabel.text = part.Text;
            }
        }

        private void RenderCombo(IReadOnlyList<GlyphPart> parts)
        {
            // 단일 렌더 타깃은 끄고 콤보 컨테이너를 켠다.
            if (_iconImage != null) _iconImage.enabled = false;
            if (_fallbackLabel != null) _fallbackLabel.enabled = false;
            _comboContainer.gameObject.SetActive(true);

            EnsurePool(parts.Count);

            for (int i = 0; i < _comboPool.Count; i++)
            {
                if (i < parts.Count)
                {
                    _comboPool[i].gameObject.SetActive(true);
                    _comboPool[i].Set(parts[i], showSeparator: i > 0);
                }
                else
                {
                    _comboPool[i].gameObject.SetActive(false);
                }
            }
        }

        private void EnsurePool(int count)
        {
            while (_comboPool.Count < count)
            {
                var item = Instantiate(_comboItemTemplate, _comboContainer);
                _comboPool.Add(item);
            }
        }
    }
}
