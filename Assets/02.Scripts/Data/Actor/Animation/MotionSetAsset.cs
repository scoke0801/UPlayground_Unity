using UnityEngine;

namespace UPlayGround.Animation
{
    [CreateAssetMenu(fileName = "MotionSet", menuName = "UPlayGround/애니메이션/Motion Set")]
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