using UnityEngine;

namespace UPlayGround
{
    /// <summary>게임 판정에서 확정한 구간을 레이저와 충돌 이펙트에 함께 반영한다.</summary>
    public sealed class ProjectileBeamVisual : MonoBehaviour
    {
        [SerializeField] private LineRenderer[] _lines = System.Array.Empty<LineRenderer>();
        [SerializeField] private Transform _impact;

        /// <summary>외부 VFX의 독립 Raycast 대신 동일한 판정 끝점을 표시한다.</summary>
        public void SetSegment(Vector3 origin, Vector3 endpoint, bool hasImpact)
        {
            for (int i = 0; i < _lines.Length; i++)
            {
                if (_lines[i] == null) continue;
                _lines[i].useWorldSpace = true;
                _lines[i].positionCount = 2;
                _lines[i].SetPosition(0, origin);
                _lines[i].SetPosition(1, endpoint);
            }
            if (_impact == null) return;
            _impact.position = endpoint;
            _impact.gameObject.SetActive(hasImpact);
        }
    }
}
