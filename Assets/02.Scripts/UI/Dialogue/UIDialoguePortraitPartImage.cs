using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;
using UPlayGround.Dialogue;

namespace UPlayGround.UI
{
    /// <summary>초상화 파츠의 위쪽을 고정하고 아래로 갈수록 연속적으로 휘게 표시한다.</summary>
    public sealed class UIDialoguePortraitPartImage : Image
    {
        private const int Columns = 12;
        private const int Rows = 24;
        private Vector2 _bendOffset;
        private float _pinnedRoot;
        private PortraitPinnedEdge _pinnedEdge;
        private float _pinnedSide;

        /// <summary>표시 배율이 적용된 끝 변위를 설정한다. 시간은 부모 표시기가 소유한다.</summary>
        public void SetBend(Vector2 offset, float pinnedRoot,
            PortraitPinnedEdge pinnedEdge = PortraitPinnedEdge.None, float pinnedSide = 0.3f)
        {
            pinnedRoot = Mathf.Clamp(pinnedRoot, 0f, 0.95f);
            pinnedSide = Mathf.Clamp(pinnedSide, 0f, 0.95f);
            if (_bendOffset == offset && Mathf.Approximately(_pinnedRoot, pinnedRoot)
                && _pinnedEdge == pinnedEdge && Mathf.Approximately(_pinnedSide, pinnedSide)) return;
            _bendOffset = offset;
            _pinnedRoot = pinnedRoot;
            _pinnedEdge = pinnedEdge;
            _pinnedSide = pinnedSide;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            Sprite currentSprite = overrideSprite != null ? overrideSprite : sprite;
            if (currentSprite == null || _bendOffset == Vector2.zero)
            {
                base.OnPopulateMesh(vertices);
                return;
            }

            // 파츠는 Full Rect/Simple로 저작한다. 잘린 이미지의 UV를 사각 격자에 보존한다.
            vertices.Clear();
            Rect rect = GetPixelAdjustedRect();
            Vector4 uv = DataUtility.GetOuterUV(currentSprite);
            Color32 tint = color;
            for (int row = 0; row <= Rows; row++)
            {
                float height = row / (float)Rows;
                float distance = Mathf.Clamp01((1f - height - _pinnedRoot) / (1f - _pinnedRoot));
                float weight = distance * distance * (3f - 2f * distance);
                for (int column = 0; column <= Columns; column++)
                {
                    float width = column / (float)Columns;
                    Vector2 offset = _bendOffset * (weight * GetSideWeight(width));
                    vertices.AddVert(new Vector3(rect.xMin + rect.width * width + offset.x,
                            rect.yMin + rect.height * height + offset.y), tint,
                        new Vector2(Mathf.Lerp(uv.x, uv.z, width), Mathf.Lerp(uv.y, uv.w, height)));
                }
            }
            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    int lower = row * (Columns + 1) + column;
                    int upper = lower + Columns + 1;
                    vertices.AddTriangle(lower, upper, lower + 1);
                    vertices.AddTriangle(lower + 1, upper, upper + 1);
                }
            }
        }

        private float GetSideWeight(float width)
        {
            if (_pinnedEdge == PortraitPinnedEdge.None) return 1f;
            float distance = _pinnedEdge == PortraitPinnedEdge.Left ? width : 1f - width;
            distance = Mathf.Clamp01((distance - _pinnedSide) / (1f - _pinnedSide));
            return distance * distance * (3f - 2f * distance);
        }
    }
}
