
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace UPlayGround.Data
{
    [CreateAssetMenu(fileName = "CameraShakeData", menuName = "UP/SO/CameraShakeData")]
    public class CameraShakeData : ScriptableObject
    {
        public string key;
        
        public bool UseMainCamera = true;
        public List<Camera> Cameras = new List<Camera>();
        
        [Space]
        public float Delay = 0.0f;
        public float Duration = 1.0f;
        public CameraShaker.ShakeSpace ShakeSpace = CameraShaker.ShakeSpace.Screen;
        public Vector3 ShakeStrength = new Vector3(0.1f, 0.1f, 0.1f);
        public AnimationCurve ShakeCurve = AnimationCurve.Linear(0, 1, 1, 0);
        
        [Space]
        public float ShakesDelay = 0;
    }
}