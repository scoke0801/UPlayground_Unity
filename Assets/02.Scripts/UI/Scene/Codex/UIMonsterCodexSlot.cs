using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UPlayGround.Data.Codex;

namespace UPlayGround.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class UIMonsterCodexSlot : MonoBehaviour, ISelectHandler,
        IUIFocusPresentation
    {
        [SerializeField] private Image _portrait;
        [SerializeField] private TextMeshProUGUI _name;
        [SerializeField] private Image _progressFill;
        [SerializeField] private TextMeshProUGUI _progressLabel;
        [SerializeField] private GameObject _selection;

        private MonsterCodexEntryView _view;
        private Action<MonsterCodexEntryView> _onClick;
        private Button _button;

        public string ActorId => _view?.actorId;
        public Selectable Selectable => _button;
        public bool SuppressGlobalFocusIndicator => _selection != null;
        public RectTransform GlobalFocusIndicatorTarget => null;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(InvokeClick);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(InvokeClick);
        }

        public void Bind(
            MonsterCodexEntryView view,
            Action<MonsterCodexEntryView> onClick)
        {
            _view = view;
            _onClick = onClick;
            bool discovered = view != null && view.discovered;

            if (_portrait != null)
            {
                _portrait.sprite = view?.portrait;
                _portrait.color = discovered ? Color.white : Color.black;
            }
            if (_name != null)
                _name.text = discovered ? view.displayName : "???";
            if (_progressFill != null)
                _progressFill.fillAmount = view?.recordRatio ?? 0f;
            if (_progressLabel != null)
                _progressLabel.text = $"{(view?.recordRatio ?? 0f) * 100f:0}%";

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (_selection != null)
                _selection.SetActive(selected);
        }

        private void InvokeClick()
        {
            if (_view != null)
                _onClick?.Invoke(_view);
        }

        public void OnSelect(BaseEventData eventData)
        {
            InvokeClick();
        }
    }
}
