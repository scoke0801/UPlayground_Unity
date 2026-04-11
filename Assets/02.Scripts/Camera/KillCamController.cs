using System.Collections;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.Manager.Handler;

namespace UPlayGround
{
    /// <summary>
    /// 킬캠 연출 컨트롤러
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
        private float _lastTriggerTime = -999f;

        public bool IsPlaying => _activeSequence != null;

        public KillCamController(MonoBehaviour coroutineRunner, KillCamData data)
        {
            _coroutineRunner = coroutineRunner;
            _data = data;
        }

        /// <summary>
        /// 킬캠 연출 시도.
        /// 쿨다운 / 확률 / 적 등급 조건을 모두 통과해야 실행된다.
        /// </summary>
        public bool TryExecute(Transform victim)
        {
            if (_data == null || IsPlaying)
                return false;

            if (Time.unscaledTime - _lastTriggerTime < _data.cooldown)
                return false;

            // 처형 공격으로 사망 시에는 처형 연출이 있으므로 킬캠 제외
            // (호출부에서 처형 여부를 걸러주는 것이 이상적이나, 방어적으로 체크)
            MonsterActor actor = victim != null ? victim.GetComponent<MonsterActor>() : null;

            float chance = _data.triggerChance;
            if (actor != null)
            {
                chance = actor.Grade switch
                {
                    MonsterActorGrade.Normal => 0.25f,
                    MonsterActorGrade.Elite  => 0.60f,
                    _                        => 1.00f, // Boss 이상은 100%
                };
            }

            if (Random.value > chance)
                return false;

            _lastTriggerTime = Time.unscaledTime;
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
        /// 킬캠 연출 시퀀스 (기획서 §5.2 타임라인 기준)
        ///
        /// ① 사망 순간  : HitStop Critical 발동 (0.15s, TimeScale 0.05)
        /// ② 0.0~0.3s  : 타겟 방향 FOV -5° 줌인 (EaseIn 0.15s)
        /// ③ 0.3~1.2s  : KillCam 쉐이크 미진동 유지
        /// ④ 1.2~1.8s  : FOV 원복 (EaseOut 0.6s), TimeScale 점진적 복귀
        /// ⑤ 1.8s 이후  : 정상 전투 복귀
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

            VitalOrbManager.Instance.TrySpawn(VitalOrbTrigger.KillKillCam, victim.position);

            float originalDistance = cameraManager.GetCurrentDistance();
            Vector3 originalOffset = cameraManager.GetCurrentOffset();
            float originalFOV = cameraManager.GetCurrentFOV();

            cameraManager.SetInputLock(true);

            // ① HitStop + 슬로모션 시작
            MonsterActor actor = victim.GetComponent<MonsterActor>();
            if (actor != null && actor.Grade != MonsterActorGrade.Normal)
            {
                hitStopManager?.Execute(_data.slowMotionDuration, _data.slowMotionTimeScale);
            }
            else
            {
                hitStopManager?.Execute(_data.slowMotionDuration, _data.slowMotionTimeScale);
            }

            // ② FOV 줌인 (0.15s EaseIn) + 카메라 오프셋 전환
            float elapsed = 0f;
            // float zoomInTime = 0.15f;
            // float targetFOV = originalFOV - 5f;
            //
            // while (elapsed < zoomInTime)
            // {
            //     elapsed += Time.unscaledDeltaTime;
            //     float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / zoomInTime));
            //
            //     float dist = Mathf.Lerp(originalDistance, _data.zoomDistance, t);
            //     Vector3 offset = Vector3.Lerp(originalOffset, _data.killCamOffset, t);
            //
            //     cameraManager.SetDistance(dist);
            //     cameraManager.SetCameraOffset(offset);
            //     cameraManager.GetMainCamera().fieldOfView = Mathf.Lerp(originalFOV, targetFOV, t);
            //
            //     yield return null;
            // }

            // ③ KillCam 쉐이크 시작 (슬로우 구간 미진동)
            if (!string.IsNullOrEmpty(_data.cameraShakeKey))
                cameraManager.StartShake(_data.cameraShakeKey);

            // 슬로우 구간 홀드 (Phase ③)
            float holdTime = 0.9f; // 0.3s~1.2s 구간
            yield return new WaitForSecondsRealtime(holdTime);

            // ④ FOV 원복 + 줌아웃 (0.6s EaseOut)
            // float zoomOutTime = _data.zoomOutDuration;
            // elapsed = 0f;
            //
            // float startFOV = cameraManager.GetCurrentFOV();
            // float startDist = cameraManager.GetCurrentDistance();
            // Vector3 startOffset = cameraManager.GetCurrentOffset();
            //
            // while (elapsed < zoomOutTime)
            // {
            //     elapsed += Time.unscaledDeltaTime;
            //     float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / zoomOutTime));
            //
            //     cameraManager.SetDistance(Mathf.Lerp(startDist, originalDistance, t));
            //     cameraManager.SetCameraOffset(Vector3.Lerp(startOffset, originalOffset, t));
            //     cameraManager.GetMainCamera().fieldOfView = Mathf.Lerp(startFOV, originalFOV, t);
            //
            //     yield return null;
            // }

            // ⑤ 상태 복원
            RestoreState(originalDistance, originalOffset, originalFOV);
            _activeSequence = null;
        }

        private void RestoreState()
        {
            var cameraManager = CameraManager.Instance;
            cameraManager?.SetInputLock(false);

            if (Time.timeScale < 1f)
                GameHitStopManager.Instance?.Stop();
        }

        private void RestoreState(float originalDistance, Vector3 originalOffset, float originalFOV)
        {
            var cameraManager = CameraManager.Instance;
            if (cameraManager != null)
            {
                cameraManager.SetDistance(originalDistance);
                cameraManager.SetCameraOffset(originalOffset);
                cameraManager.GetMainCamera().fieldOfView = originalFOV;
                cameraManager.SetInputLock(false);
            }
        }
    }
}
