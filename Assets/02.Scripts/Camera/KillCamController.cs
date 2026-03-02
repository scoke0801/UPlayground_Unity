using System.Collections;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Manager;
using UPlayGround.Manager.Handler;

namespace UPlayGround
{
    /// <summary>
    /// 킬캠(피니셔 줌) 연출 컨트롤러
    /// 
    /// 역할:
    /// - 적 사망 시 슬로모션 + 카메라 줌인 + 쉐이크 조합 연출
    /// - CameraManager의 기존 API를 활용하되, 연출 시퀀스를 캡슐화
    /// - 연출 중 카메라 입력을 차단하여 부자연스러운 움직임 방지
    /// 
    /// 사용 흐름:
    /// 1. PlayerCombat.PerformHitDetection에서 킬 감지 시 TryExecute 호출
    /// 2. 확률/쿨다운 체크 후 코루틴으로 연출 시퀀스 실행
    /// 3. 연출 완료 후 원래 카메라 상태로 자동 복귀
    /// </summary>
    public class KillCamController
    {
        private readonly MonoBehaviour _coroutineRunner;
        private readonly KillCamData _data;

        private Coroutine _activeSequence;

        // 연출 중 외부에서 체크용
        public bool IsPlaying => _activeSequence != null;

        public KillCamController(MonoBehaviour coroutineRunner, KillCamData data)
        {
            _coroutineRunner = coroutineRunner;
            _data = data;
        }

        /// <summary>
        /// 킬캠 연출 시도.
        /// 우선은 매번 적용되도록 하기
        /// </summary>
        /// <param name="victim">사망한 적의 Transform</param>
        /// <returns>연출이 실행됐으면 true</returns>
        public bool TryExecute(Transform victim)
        {
            //if (_data == null || IsPlaying)
            //    return false;
            //
            //// 쿨다운 체크
            //if (Time.unscaledTime - _lastTriggerTime < _data.cooldown)
            //    return false;
            //
            //// 확률 체크
            //if (Random.value > _data.triggerChance)
            //    return false;
            //
            //if (victim == null)
            //    return false;
            //
            //_lastTriggerTime = Time.unscaledTime;
            _activeSequence = _coroutineRunner.StartCoroutine(KillCamSequence(victim));
            return true;
        }

        /// <summary>
        /// 연출 강제 중단 (씬 전환 등)
        /// </summary>
        public void ForceStop()
        {
            if (_activeSequence != null)
            {
                _coroutineRunner.StopCoroutine(_activeSequence);
                _activeSequence = null;
            }

            RestoreState();
        }

        /// <summary>
        /// 킬캠 연출 시퀀스
        /// 
        /// 타임라인:
        /// [0] 슬로모션 시작 + 줌인 시작
        /// [zoomInDuration] 줌 완료, 홀드
        /// [+ zoomHoldDuration] 줌아웃 시작 + 슬로모션 해제
        /// [+ zoomOutDuration] 연출 종료
        /// </summary>
        private IEnumerator KillCamSequence(Transform victim)
        {
            var cameraManager = CameraManager.Instance;
            var hitStopManager = GameHitStopManager.Instance;

            if (cameraManager == null)
            {
                _activeSequence = null;
                yield break;
            }

            // --- 현재 상태 저장 ---
            float originalDistance = cameraManager.GetCurrentDistance();
            Vector3 originalOffset = cameraManager.GetCurrentOffset();

            // 카메라 입력 차단
            cameraManager.SetInputLock(true);

            // 슬로모션
            hitStopManager?.Execute(_data.slowMotionDuration, _data.slowMotionTimeScale);

            // 카메라 쉐이크
            if (!string.IsNullOrEmpty(_data.cameraShakeKey))
            {
                cameraManager.StartShake(_data.cameraShakeKey);
            }

            // --- Phase 1: 줌인 ---
            float elapsed = 0f;
            while (elapsed < _data.zoomInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = _data.zoomCurve.Evaluate(elapsed / _data.zoomInDuration);

                float dist = Mathf.Lerp(originalDistance, _data.zoomDistance, t);
                Vector3 offset = Vector3.Lerp(originalOffset, _data.killCamOffset, t);

                cameraManager.SetDistance(dist);
                cameraManager.SetCameraOffset(offset);

                yield return null;
            }

            cameraManager.SetDistance(_data.zoomDistance);
            cameraManager.SetCameraOffset(_data.killCamOffset);

            // --- Phase 2: 홀드 ---
            yield return new WaitForSecondsRealtime(_data.zoomHoldDuration);

            // --- Phase 3: 줌아웃 (복귀) ---
            elapsed = 0f;
            while (elapsed < _data.zoomOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = _data.zoomCurve.Evaluate(elapsed / _data.zoomOutDuration);

                float dist = Mathf.Lerp(_data.zoomDistance, originalDistance, t);
                Vector3 offset = Vector3.Lerp(_data.killCamOffset, originalOffset, t);

                cameraManager.SetDistance(dist);
                cameraManager.SetCameraOffset(offset);

                yield return null;
            }

            // --- 연출 종료 ---
            RestoreState(originalDistance, originalOffset);
            _activeSequence = null;
        }

        private void RestoreState()
        {
            var cameraManager = CameraManager.Instance;
            if (cameraManager != null)
            {
                cameraManager.SetInputLock(false);
            }

            if (Time.timeScale < 1f)
            {
                GameHitStopManager.Instance?.Stop();
            }
        }

        private void RestoreState(float originalDistance, Vector3 originalOffset)
        {
            var cameraManager = CameraManager.Instance;
            if (cameraManager != null)
            {
                cameraManager.SetDistance(originalDistance);
                cameraManager.SetCameraOffset(originalOffset);
                cameraManager.SetInputLock(false);
            }
        }
    }
}
