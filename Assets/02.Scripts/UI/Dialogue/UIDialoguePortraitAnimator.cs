using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Dialogue;

namespace UPlayGround.UI
{
    /// <summary>기존 Image 안에서 검수된 파츠만 합성한다. 파츠가 없으면 원본 정지 이미지를 유지한다.</summary>
    public sealed class UIDialoguePortraitAnimator : MonoBehaviour
    {
        private Image _image;
        private DialoguePortraitPresentation _presentation;
        private readonly List<UIDialoguePortraitPartImage> _parts = new();
        private float _phase;
        private float _nextBlink;
        private float _blinkTime;

        /// <summary>같은 표정은 위상을 유지하고 교체 시 눈 프레임을 함께 초기화한다.</summary>
        public void Show(Image image, DialoguePortraitPresentation presentation)
        {
            if (_image == image && _presentation.Set == presentation.Set && _presentation.Frame == presentation.Frame) return;
            Clear();
            _image = image;
            _presentation = presentation;
            var frame = presentation.Frame;
            if (image == null || presentation.Set == null || frame == null || !frame.hasCompleteParts || frame.parts.Length == 0) return;
            foreach (var part in frame.parts)
                if (part == null || part.open == null) return;
            for (int i = 0; i < frame.parts.Length; i++)
            {
                var part = frame.parts[i];
                var child = new GameObject("PortraitPart", typeof(RectTransform), typeof(CanvasRenderer), typeof(UIDialoguePortraitPartImage));
                child.transform.SetParent(image.transform, false);
                var partImage = child.GetComponent<UIDialoguePortraitPartImage>();
                partImage.raycastTarget = false;
                partImage.sprite = part.open;
                _parts.Add(partImage);
            }
            image.enabled = false;
            ScheduleBlink();
            ApplyParts();
        }

        /// <summary>숨김과 재사용 시 남은 파츠·눈 프레임을 해제한다.</summary>
        public void Clear()
        {
            foreach (var part in _parts)
                if (part != null) { part.gameObject.SetActive(false); Destroy(part.gameObject); }
            _parts.Clear();
            if (_image != null) _image.enabled = true;
            _presentation = default;
            _phase = 0;
            _blinkTime = 0;
        }

        private void OnDisable() => Clear();
        private void OnDestroy() => Clear();

        private void Update()
        {
            if (_parts.Count == 0 || UISvc.Dialogue?.IsPaused == true) return;
            float elapsed = Time.unscaledDeltaTime;
            _phase += elapsed;
            if (_nextBlink > 0) _nextBlink -= elapsed;
            else
            {
                _blinkTime += elapsed;
                if (_blinkTime >= _presentation.Set.blinkDuration) { _blinkTime = 0; ScheduleBlink(); }
            }
            ApplyParts();
        }

        private void ScheduleBlink()
        {
            Vector2 interval = _presentation.Set.blinkInterval;
            _nextBlink = Random.Range(Mathf.Max(0.01f, interval.x), Mathf.Max(interval.x, interval.y));
        }

        private void ApplyParts()
        {
            var set = _presentation.Set;
            var frame = _presentation.Frame;
            Vector2 bounds = _image.rectTransform.rect.size;
            float scale = Mathf.Min(bounds.x / Mathf.Max(1, set.canvasSize.x), bounds.y / Mathf.Max(1, set.canvasSize.y));
            float blink = _blinkTime / Mathf.Max(0.01f, set.blinkDuration);
            for (int i = 0; i < _parts.Count; i++)
            {
                var part = frame.parts[i];
                var image = _parts[i];
                image.color = _image.color;
                image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                image.rectTransform.pivot = part.pivot;
                image.rectTransform.anchoredPosition = part.position * scale;
                image.rectTransform.sizeDelta = part.size * scale;
                float sway = Mathf.Sin(_phase * Mathf.PI * 2 / Mathf.Max(0.01f, part.swayPeriod) + part.phase);
                bool hasBend = part.bendPixels != Vector2.zero;
                image.rectTransform.localRotation = hasBend ? Quaternion.identity
                    : Quaternion.Euler(0, 0, sway * part.swayDegrees);
                image.SetBend(part.bendPixels * (sway * scale), part.pinnedRoot, part.pinnedEdge, part.pinnedSide);
                image.sprite = part.open;
                if (frame.hasClosedEyes || part.halfClosed == null || part.closed == null || blink <= 0) continue;
                image.sprite = blink > 0.25f && blink < 0.75f ? part.closed : part.halfClosed;
            }
        }
    }
}
