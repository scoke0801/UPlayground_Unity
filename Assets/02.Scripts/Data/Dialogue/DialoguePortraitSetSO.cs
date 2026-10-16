using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Dialogue
{
    public enum DialogueExpressionMode { Legacy = 0, Default = 1, Set = 2, Keep = 3, Hidden = 4 }
    public enum DialogueExpression { Neutral = 0, Smile = 1, Serious = 2, Displeased = 3, Worried = 4, Surprised = 5 }
    public enum PortraitPinnedEdge { None = 0, Left = 1, Right = 2 }

    /// <summary>동일 캔버스의 표정과 분리 파츠. 누락 이미지는 기본 정지 이미지로 폴백한다.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/대화/Portrait Set")]
    public sealed class DialoguePortraitSetSO : ScriptableObject
    {
        public CharacterActorType characterType;
        public string[] speakerIds = Array.Empty<string>();
        public Sprite defaultPortrait;
        public Vector2 canvasSize = new(1024, 1024);
        public Vector2 blinkInterval = new(2.5f, 6f);
        [Min(0.01f)] public float blinkDuration = 0.16f;
        public ExpressionFrame[] expressions = Array.Empty<ExpressionFrame>();

        /// <summary>정확한 표정을 찾고 없으면 기본 표정으로 복귀한다.</summary>
        public ExpressionFrame FindExpression(DialogueExpression expression)
        {
            ExpressionFrame fallback = null;
            foreach (var frame in expressions)
            {
                if (frame == null) continue;
                if (frame.expression == DialogueExpression.Neutral && frame.portrait != null) fallback = frame;
                if (frame.expression == expression && frame.portrait != null) return frame;
            }
            return fallback;
        }

        [Serializable]
        public sealed class ExpressionFrame
        {
            public DialogueExpression expression;
            public Sprite portrait;
            public bool hasClosedEyes;
            [Tooltip("완성 정지 이미지 대신 파츠만 합성해도 되는지 아트 검수 후 켭니다.")]
            public bool hasCompleteParts;
            public PortraitPart[] parts = Array.Empty<PortraitPart>();
        }

        [Serializable]
        public sealed class PortraitPart
        {
            public Sprite open;
            public Sprite halfClosed;
            public Sprite closed;
            public Vector2 position;
            public Vector2 size;
            public Vector2 pivot = new(0.5f, 0.5f);
            public float swayDegrees;
            [Min(0.01f)] public float swayPeriod = 4f;
            public float phase;
            [Tooltip("원본 캔버스 픽셀 단위 끝 변위. 0이면 기존 정지/회전 표시를 사용한다.")]
            public Vector2 bendPixels;
            [Range(0f, 0.95f), Tooltip("위쪽부터 움직이지 않는 구간의 비율.")]
            public float pinnedRoot = 0.3f;
            public PortraitPinnedEdge pinnedEdge;
            [Range(0f, 0.95f)] public float pinnedSide = 0.3f;
        }
    }

    /// <summary>해석된 한 표시 영역. 세션 상태를 UI로 복제하지 않는다.</summary>
    public readonly struct DialoguePortraitPresentation
    {
        public readonly Sprite Sprite;
        public readonly DialoguePortraitSetSO Set;
        public readonly DialoguePortraitSetSO.ExpressionFrame Frame;
        public DialoguePortraitPresentation(Sprite sprite, DialoguePortraitSetSO set = null,
            DialoguePortraitSetSO.ExpressionFrame frame = null)
        { Sprite = sprite; Set = set; Frame = frame; }
    }

    /// <summary>캐릭터별 표정 기억은 한 Main 대화 세션에만 존재한다.</summary>
    public sealed class DialogueExpressionSession
    {
        private readonly Dictionary<CharacterActorType, DialogueExpression> _expressions = new();

        /// <summary>세이브와 무관한 대화 표정 기억을 비운다.</summary>
        public void Clear() => _expressions.Clear();

        /// <summary>예약 화자는 기존 해석을 사용하고 명시 화자는 데이터의 ID로만 연결한다.</summary>
        public DialoguePortraitPresentation Resolve(DialogueNodeSO node, CharacterActorType active,
            CharacterActorType protagonist, SpeakerPortraitTableSO table, Sprite legacy)
        {
            if (node == null || node.channel != DialogueChannel.Main || node.expressionMode == DialogueExpressionMode.Legacy)
                return new DialoguePortraitPresentation(legacy);
            if (node.expressionMode == DialogueExpressionMode.Hidden) return default;
            var set = table != null ? table.FindPresentation(node.speakerId, active, protagonist) : null;
            if (set == null) return new DialoguePortraitPresentation(legacy);
            DialogueExpression expression = DialogueExpression.Neutral;
            if (node.expressionMode == DialogueExpressionMode.Set) expression = node.expression;
            else if (node.expressionMode == DialogueExpressionMode.Keep) _expressions.TryGetValue(set.characterType, out expression);
            _expressions[set.characterType] = expression;
            var frame = set.FindExpression(expression);
            Sprite sprite = frame?.portrait != null ? frame.portrait : set.defaultPortrait;
            return new DialoguePortraitPresentation(sprite != null ? sprite : legacy, set, frame);
        }
    }
}
