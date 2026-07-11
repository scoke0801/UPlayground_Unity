using System;
using UnityEngine;

namespace UPlayGround.Particle
{
    public class ParticlePositionFixer : MonoBehaviour
    {
        private Vector3 _fixedWorldPosition;
        private void Awake()
        {
            // 시작 시 월드 위치를 고정
            _fixedWorldPosition = transform.position;
        }

        private void LateUpdate()
        {
            //transform.position = _fixedWorldPosition;
        }
    }
}