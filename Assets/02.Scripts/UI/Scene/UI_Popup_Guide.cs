using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;
using UPlayGround.Data.Path;
using UPlayGround.Data.UI;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>
    /// 이미지와 설명 텍스트를 페이지 단위로 보여주는 가이드 팝업.
    /// UIManager.ShowUI(UIKeyType.GuidePopup)로 표시한 뒤 Setup(data)를 호출한다.
    /// </summary>
    public class UI_Popup_Guide : UI_PopupBase
    {
        public const string UIKey = "GuidePopup";

        [Header("표시")]
        [SerializeField] private Image _guideImage;
        [SerializeField] private RawImage _guideVideoImage;
        [SerializeField] private VideoPlayer _videoPlayer;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _bodyText;
        [SerializeField] private TextMeshProUGUI _pageText;

        [Header("버튼")]
        [SerializeField] private Button _previousButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _closeButton;

        [Header("동작")]
        [SerializeField] private bool _pauseGameWhileOpen = true;
        [SerializeField] private bool _closeOnLastNext = true;

        private readonly List<GuidePopupPage> _pages = new();
        private int _pageIndex;
        private RenderTexture _videoTexture;
        private bool _pausedByThisPopup;

        protected override bool BlocksLowerInput => true;

        protected override void Awake()
        {
            // 이 팝업은 Canvas_Popup 전체를 덮는 UI다. 프리팹 빌드 시 독립 Canvas의
            // 구동값이 직렬화되더라도 런타임에서는 항상 정상 크기로 복구한다.
            if (transform is RectTransform rectTransform)
            {
                rectTransform.localScale = Vector3.one;
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }

            base.Awake();

            if (_previousButton != null) _previousButton.onClick.AddListener(ShowPreviousPage);
            if (_nextButton != null) _nextButton.onClick.AddListener(ShowNextPageOrClose);
            if (_closeButton != null) _closeButton.onClick.AddListener(ClosePopup);
        }

        protected override void OnShow()
        {
            // UI_PopupBase.OnShow가 Dim 페이드 인 + Panel 스케일 팝인 트윈을 재생한다.
            base.OnShow();

            _pausedByThisPopup = false;
            if (_pauseGameWhileOpen && Svc.GameTime != null && !Svc.GameTime.IsPaused)
            {
                Svc.GameTime?.SetPause(true);
                _pausedByThisPopup = true;
            }

            Refresh();
            SelectDefaultButton();
        }

        protected override void OnHide()
        {
            StopVideo();

            if (_pausedByThisPopup)
            {
                Svc.GameTime?.SetPause(false);
                _pausedByThisPopup = false;
            }

            base.OnHide();
        }

        protected override void RegisterInputEvents()
        {
            Svc.Input?.RegisterInputEvent(InputMapNames.UI, UIAction.DialogueNext,
                null, OnInputNext, null, null, null, InputLayer.Level_2);
        }

        protected override void UnRegisterInputEvents()
        {
            if (Svc.Input == null)
                return;

            Svc.Input?.UnRegisterInputEvent(InputMapNames.UI, UIAction.DialogueNext,
                null, OnInputNext, null);
        }

        public override bool PerformBackFunction()
        {
            ClosePopup();
            return true;
        }

        public void Setup(GuidePopupDataSO data, int startPageIndex = 0)
        {
            Setup(data != null ? data.Pages : null, startPageIndex);
        }

        public void Setup(IReadOnlyList<GuidePopupPage> pages, int startPageIndex = 0)
        {
            _pages.Clear();

            if (pages != null)
            {
                for (int i = 0; i < pages.Count; i++)
                {
                    if (pages[i] != null)
                        _pages.Add(pages[i]);
                }
            }

            _pageIndex = Mathf.Clamp(startPageIndex, 0, Mathf.Max(0, _pages.Count - 1));
            Refresh();
        }

        private void OnInputNext(InputAction.CallbackContext ctx)
        {
            ShowNextPageOrClose();
        }

        private void ShowPreviousPage()
        {
            if (_pageIndex <= 0)
                return;

            _pageIndex--;
            Refresh();
        }

        private void ShowNextPageOrClose()
        {
            if (_pages.Count == 0)
            {
                ClosePopup();
                return;
            }

            if (_pageIndex < _pages.Count - 1)
            {
                _pageIndex++;
                Refresh();
                return;
            }

            if (_closeOnLastNext)
                ClosePopup();
        }

        private void ClosePopup()
        {
            // HideUI → UI_PopupBase.Hide가 클로즈 트윈(Dim 페이드 아웃 + Panel 축소) 후 실제로 숨긴다.
            UISvc.UI?.HideUI(UIKey);
        }

        private void Refresh()
        {
            StopVideo();

            bool hasPage = _pages.Count > 0;
            GuidePopupPage page = hasPage ? _pages[_pageIndex] : null;
            bool showVideo = page != null && page.MediaType == GuidePopupMediaType.Video && page.Video != null;

            if (_guideImage != null)
            {
                _guideImage.sprite = !showVideo ? page?.Image : null;
                _guideImage.enabled = !showVideo && page?.Image != null;
                _guideImage.preserveAspect = true;
            }

            if (_guideVideoImage != null)
                _guideVideoImage.enabled = showVideo;

            if (showVideo)
                PlayVideo(page);

            if (_titleText != null)
                _titleText.text = hasPage ? page.Title : string.Empty;

            if (_bodyText != null)
                _bodyText.text = hasPage ? page.Body : string.Empty;

            if (_pageText != null)
                _pageText.text = hasPage ? $"{_pageIndex + 1}/{_pages.Count}" : "0/0";

            if (_previousButton != null)
                _previousButton.interactable = _pageIndex > 0;

            if (_nextButton != null)
            {
                var label = _nextButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                    label.text = hasPage && _pageIndex >= _pages.Count - 1 ? "닫기" : "다음";
            }

            SelectDefaultButton();
        }

        private void PlayVideo(GuidePopupPage page)
        {
            if (_videoPlayer == null || _guideVideoImage == null || page?.Video == null)
                return;

            if (_videoTexture == null)
                _videoTexture = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32);

            _videoPlayer.playOnAwake = false;
            _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            _videoPlayer.targetTexture = _videoTexture;
            _videoPlayer.clip = page.Video;
            _videoPlayer.isLooping = page.LoopVideo;
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

            _guideVideoImage.texture = _videoTexture;
            _videoPlayer.Play();
        }

        private void StopVideo()
        {
            if (_videoPlayer != null)
            {
                _videoPlayer.Stop();
                _videoPlayer.clip = null;
            }

            if (_guideVideoImage != null)
                _guideVideoImage.texture = null;
        }

        private void SelectDefaultButton()
        {
            if (!IsVisible || EventSystem.current == null)
                return;

            Button target = _nextButton != null && _nextButton.interactable ? _nextButton : _closeButton;
            if (target != null)
                EventSystem.current.SetSelectedGameObject(target.gameObject);
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            StopVideo();

            if (_videoTexture != null)
            {
                _videoTexture.Release();
                Destroy(_videoTexture);
                _videoTexture = null;
            }
        }
    }
}
