using UnityEngine;
using UnityEngine.UI;

namespace UPlayGround.UI
{
    /// <summary>아이템 쪽 끝과 양옆이 흐려지는 이름표 연결선.</summary>
    public sealed class UIDropLabelConnector : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            Rect bounds = rectTransform.rect;
            Color clear = color;
            clear.a = 0f;
            Color tip = color;
            tip.a *= 0.15f;
            float halfWidth = bounds.width * 0.5f;
            // 투명한 외곽 정점으로 가는 선의 계단 현상을 줄인다.
            for (int row = 0; row < 2; row++)
            {
                float y = row == 0 ? bounds.yMax : bounds.yMin;
                float width = row == 0 ? halfWidth : halfWidth * 0.35f;
                Color center = row == 0 ? color : tip;
                vertices.AddVert(new Vector3(-width - 0.5f, y, 0f), clear, Vector2.zero);
                vertices.AddVert(new Vector3(-width * 0.5f, y, 0f), center, Vector2.zero);
                vertices.AddVert(new Vector3(width * 0.5f, y, 0f), center, Vector2.zero);
                vertices.AddVert(new Vector3(width + 0.5f, y, 0f), clear, Vector2.zero);
            }
            for (int strip = 0; strip < 3; strip++)
            {
                vertices.AddTriangle(strip, strip + 1, strip + 4);
                vertices.AddTriangle(strip + 1, strip + 5, strip + 4);
            }
        }
    }
}
