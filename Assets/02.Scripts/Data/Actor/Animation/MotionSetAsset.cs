using UnityEngine;

namespace UPlayGround.Animation
{
    [CreateAssetMenu(fileName = "MotionSet", menuName = "UPlayGround/MotionSet")]
    public class MotionSetAsset : ScriptableObject
    {
        public MotionSet motionSet;
        
        void OnEnable()
        {
            // 초기화
            if (motionSet == null)
            {
                motionSet = new MotionSet
                {
                    motionSetName = name
                };
            }
        }
    }
}