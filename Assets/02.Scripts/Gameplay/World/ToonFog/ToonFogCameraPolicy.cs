using UnityEngine;

namespace UPlayGround.Gameplay.World.ToonFog
{
    /// <summary>원근 미니맵과 연출 카메라에 명시적인 예외를 지정한다.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class ToonFogCameraPolicy : MonoBehaviour
    {
        public bool fogEnabled = true;
        public Transform anchorOverride;
    }
}
