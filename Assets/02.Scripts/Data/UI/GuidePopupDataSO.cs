using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace UPlayGround.Data.UI
{
    [CreateAssetMenu(fileName = "GuidePopupData", menuName = "UPlayGround/UI/Guide Popup Data")]
    public class GuidePopupDataSO : ScriptableObject
    {
        [SerializeField] private List<GuidePopupPage> _pages = new();

        public IReadOnlyList<GuidePopupPage> Pages => _pages;
    }

    [System.Serializable]
    public class GuidePopupPage
    {
        [SerializeField] private GuidePopupMediaType _mediaType = GuidePopupMediaType.Image;
        [SerializeField] private Sprite _image;
        [SerializeField] private VideoClip _video;
        [SerializeField] private bool _loopVideo = true;
        [SerializeField] private string _title;
        [TextArea(3, 8)]
        [SerializeField] private string _body;

        public GuidePopupMediaType MediaType => _mediaType;
        public Sprite Image => _image;
        public VideoClip Video => _video;
        public bool LoopVideo => _loopVideo;
        public string Title => _title;
        public string Body => _body;
    }

    public enum GuidePopupMediaType
    {
        Image = 0,
        Video = 1,
    }
}
