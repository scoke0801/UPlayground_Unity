using UnityEngine;

namespace UPlayGround.Animation
{
    [CreateAssetMenu(fileName = "MotionSet", menuName = "UPlayGround/MotionSet")]
    public class MotionSetAsset : ScriptableObject
    {
        public MotionSet motionSet = new MotionSet();
    }
}