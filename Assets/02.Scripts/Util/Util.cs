using UnityEngine;

namespace UPlayGround
{

    public static class Util
    {
        public static float ApplyRandomValue(float originData, float min, float max)
        {
            float randRange = UnityEngine.Random.Range(min, max);
            return Mathf.Max(1, originData + originData * randRange);
        }
    }
}

