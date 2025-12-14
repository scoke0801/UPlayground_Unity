using System;
using System.Collections;
using UnityEngine;

namespace Actor
{
    public class InteractableHitActor : InteractableActor
    {
        [SerializeField] private float _shakeAmount = 5.0f;
        [SerializeField] private float _shakeDuration = 0.5f;
        
        private Quaternion _originalRotation = Quaternion.identity;

        public void Start()
        {
            _originalRotation = transform.rotation;
        }

        public override void Interaction()
        {
            base.Interaction();
        }

        public override void OnHit(int damage)
        {
            base.OnHit(damage);

            GameObject player = GameObjectManager.Instance.Player;
            if (player != null)
            {
                Shake(transform.position - player.transform.position);
            }
        }

        private void Shake(Vector3 attackDirection)
        {
            Vector3 oppositeDirection = attackDirection.normalized;
            
            Quaternion targetRotation = Quaternion.Euler(
                _originalRotation.eulerAngles.x + oppositeDirection.z * _shakeAmount,
                _originalRotation.eulerAngles.y,
                _originalRotation.eulerAngles.z + oppositeDirection.x * _shakeAmount);
            
            StopAllCoroutines();
            StartCoroutine(ShakeAnimation(targetRotation));
        }

        private IEnumerator ShakeAnimation(Quaternion targetRotationQuaternion)
        {
            float elapsedTime = 0.0f;

            float shakeDuration = _shakeDuration * 0.5f;
            while (elapsedTime < shakeDuration)
            {
                transform.rotation = Quaternion.Slerp(_originalRotation, targetRotationQuaternion,
                        elapsedTime / shakeDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            elapsedTime = 0.0f;
            while (elapsedTime < shakeDuration)
            {
                transform.rotation = Quaternion.Slerp(targetRotationQuaternion, _originalRotation, elapsedTime / shakeDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
    }
}