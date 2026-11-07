using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.Combat
{
    /// <summary>충격파 한 번의 연출을 소유하며 피격 대상 수와 무관하게 재생한다.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/전투/정예 브레이크 연출")]
    public sealed class EliteBreakShockwaveEffectSO : ScriptableObject
    {
        public string visualEffectKey;
        public CameraEffectData cameraEffect;
    }
}
