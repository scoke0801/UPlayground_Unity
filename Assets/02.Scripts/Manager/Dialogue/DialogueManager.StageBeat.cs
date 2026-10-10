using System;
using UnityEngine;
using UPlayGround.Data.Actor.Animation;

namespace UPlayGround.Dialogue
{
    public partial class DialogueManager
    {
        private DialogueStageBeat _stageBeat;
        private GameActor _stageBeatActor;
        private Transform _stageBeatTarget;
        private Transform _stageBeatLookTarget;
        private Action _stageBeatCompleted;
        private Action _stageBeatFailed;
        private float _stageBeatElapsed;
        private bool _isStageBeatMoving;
        private bool _isStageBeatPaused;
        private bool _shouldResumeStageBeatMovement;
        private bool _isStageBeatStartPending;
        private int _stageBeatStartFrame;
        private UPlayGround.Animation.MotionSetAsset _stageBeatMotion;
        private UPlayGround.Animation.ActorAnimator.MotionPlaybackSnapshot _stageBeatPreviousMotion;
        private bool _hasStageBeatMotion;
        private DialogueNodeSO _stageBeatNode;
        private GameObject _stageBeatProp;
        private bool _hasStageApproachResult;
        private UPlayGround.State.EnemyStageApproachResult _stageApproachResult;

        internal bool IsStageBeatActive => _stageBeat != null;

        /// <summary>노드에 저작된 행동을 시작하고 완료까지 해당 대화의 진행만 기다린다.</summary>
        internal void BeginStageBeat(DialogueNodeSO node, Action completed, Action failed)
        {
            CancelStageBeat();
            _stageBeat = node.stageBeat;
            _stageBeatCompleted = completed;
            _stageBeatFailed = failed;
            _stageBeatElapsed = 0f;
            _stageBeatActor = ResolveBeatActor(_stageBeat.actorSpeakerId);
            _stageBeatLookTarget = ResolveSpeakerTransform(_stageBeat.targetSpeakerId);
            _stageBeatMotion = node.stageMotion;
            _stageBeatNode = node;

            NotifyNodeEnter(DialogueChannel.Main, node);
            if (_stageBeatMotion != null || node.stagePropPrefab != null)
            {
                if (_stageBeatActor?.Animator == null || _stageBeat.move)
                {
                    FinishStageBeat(false);
                    return;
                }
                _isStageBeatStartPending = true;
                _stageBeatStartFrame = Time.frameCount;
            }
            if (!_stageBeat.move)
                return;

            Transform basis = _stageBeatLookTarget != null ? _stageBeatLookTarget : _stageBeatActor?.transform;
            if (_stageBeatActor == null || basis == null)
            {
                FinishStageBeat(false);
                return;
            }

            Vector3 origin = basis.position;
            Quaternion rotation = basis.rotation;
            if (_stageBeat.useInitialTargetPose)
            {
                for (int i = 0; i < _stagedActors.Count; i++)
                {
                    var staged = _stagedActors[i];
                    if (staged.Actor == null || staged.Actor.transform != basis)
                        continue;
                    origin = staged.InitialPosition;
                    rotation = staged.InitialRotation;
                    break;
                }
            }
            Vector3 candidate = origin + rotation * _stageBeat.localOffset;
            // 순간 배치용 캡슐 겹침 검사는 경사지의 발밑 접촉도 거부한다.
            // 보행은 지면만 확인하고 실제 벽·액터 충돌은 KCC와 이동 제한시간이 처리한다.
            if (!ActorStagePlacement.TryProbeGround(
                    candidate, _stageBeatActor.transform.position.y,
                    EffectiveStageSettings.MaxHeightDelta,
                    ActorStagePlacement.GroundProbeUp, ActorStagePlacement.GroundProbeDown,
                    _stageBeatActor.transform, out Vector3 grounded))
            {
                FinishStageBeat(false);
                return;
            }

            _stageBeatTarget = new GameObject("DialogueStageTarget").transform;
            _stageBeatTarget.position = grounded;
            // 첫 노드가 행동이면 대역을 생성한 프레임과 같다. 액터 Start의 기본 상태 초기화가
            // 연출 이동을 덮어쓰지 않도록 다음 프레임에 시작한다.
            _isStageBeatStartPending = true;
            _stageBeatStartFrame = Time.frameCount;
        }

        private GameActor ResolveBeatActor(string speakerId)
        {
            Transform actorTransform = ResolveSpeakerTransform(speakerId);
            for (int i = 0; i < _stagedActors.Count; i++)
            {
                GameActor actor = _stagedActors[i].Actor;
                if (actor != null && actor.transform == actorTransform)
                    return actor;
            }
            if (actorTransform != null)
            {
                Debug.LogWarning($"[Dialogue] 무언 이동 화자의 홀드가 없습니다: {speakerId}, 대상={actorTransform.name}, 참여자={_stagedActors.Count}");
            }
            return null;
        }

        private bool TryStartStageBeatMovement()
        {
            _hasStageApproachResult = false;
            _isStageBeatMoving = _stageBeatActor switch
            {
                PlayerActor player => player.TryBeginDialogueStep(
                    _stageBeatTarget, _stageBeat.speedMultiplier, _stageBeat.stopDistance, _stageBeat.faceMovement),
                NpcActor npc => npc.TryBeginDialogueStep(
                    _stageBeatTarget, _stageBeat.speedMultiplier, _stageBeat.stopDistance, _stageBeat.faceMovement),
                MonsterActor monster => monster.TryBeginStageApproach(
                    _stageBeatTarget, _stageBeat.stopDistance, _stageBeat.speedMultiplier,
                    _stageBeat.timeoutSeconds, OnStageApproachCompleted,
                    _stageBeat.faceMovement ? MotionTags.Walk : MotionTags.Walk_B, _stageBeat.faceMovement),
                _ => false,
            };
            return _isStageBeatMoving;
        }

        private void OnStageApproachCompleted(UPlayGround.State.EnemyStageApproachResult result)
        {
            _stageApproachResult = result;
            _hasStageApproachResult = true;
        }

        private void TickStageBeat()
        {
            if (_stageBeat == null)
                return;
            if (_playback.IsPaused)
            {
                if (!_isStageBeatPaused)
                {
                    _shouldResumeStageBeatMovement = _isStageBeatMoving;
                    StopStageBeatMovement();
                }
                _isStageBeatPaused = true;
                return;
            }
            if (_isStageBeatPaused && _shouldResumeStageBeatMovement && _stageBeatTarget != null
                && !TryStartStageBeatMovement())
            {
                FinishStageBeat(false);
                return;
            }
            _isStageBeatPaused = false;
            if (_isStageBeatStartPending)
            {
                if (Time.frameCount <= _stageBeatStartFrame)
                    return;
                _isStageBeatStartPending = false;
                if (_stageBeatMotion != null || _stageBeatNode.stagePropPrefab != null)
                {
                    if (!TryAttachStageProp())
                    {
                        FinishStageBeat(false);
                        return;
                    }
                    if (_stageBeatMotion != null)
                    {
                        _stageBeatPreviousMotion = _stageBeatActor.Animator.CapturePlaybackSnapshot();
                        _stageBeatActor.Animator.PlayMotion(_stageBeatMotion, 0.2f);
                        _hasStageBeatMotion = true;
                    }
                }
                else if (!TryStartStageBeatMovement())
                    FinishStageBeat(false);
                return;
            }
            _stageBeatElapsed += Time.deltaTime;

            if (_isStageBeatMoving)
            {
                if (_stageBeatActor == null || _stageBeatTarget == null)
                {
                    FinishStageBeat(false);
                    return;
                }
                // KCC가 도착을 확정한 뒤 보간 Transform을 다시 비교하면 경계에서 영원히 기다릴 수 있다.
                if (_hasStageApproachResult && _stageApproachResult != UPlayGround.State.EnemyStageApproachResult.Arrived)
                {
                    FinishStageBeat(false);
                    return;
                }
                Vector3 position = _stageBeatActor.ActorController?.Motor != null
                    ? _stageBeatActor.ActorController.Motor.TransientPosition : _stageBeatActor.transform.position;
                Vector3 offset = _stageBeatTarget.position - position;
                offset.y = 0f;
                bool arrived = _stageBeatActor is MonsterActor ? _hasStageApproachResult
                    : offset.sqrMagnitude <= _stageBeat.stopDistance * _stageBeat.stopDistance;
                if (arrived)
                {
                    StopStageBeatMovement();
                    _stageBeatElapsed = 0f;
                }
                else if (_stageBeatElapsed >= _stageBeat.timeoutSeconds)
                {
                    FinishStageBeat(false);
                    return;
                }
            }
            if (!_isStageBeatMoving && _stageBeatElapsed >= _stageBeat.holdSeconds)
                FinishStageBeat(true);
        }

        private void StopStageBeatMovement()
        {
            if (!_isStageBeatMoving)
                return;
            if (_stageBeatActor is PlayerActor player)
                player.StopDialogueStep();
            else if (_stageBeatActor is NpcActor npc)
                npc.StopDialogueStep();
            else if (_stageBeatActor is MonsterActor monster)
                monster.StopStageApproach(_stageBeatLookTarget);
            _isStageBeatMoving = false;
        }

        private bool TryAttachStageProp()
        {
            if (_stageBeatNode.stagePropPrefab == null)
                return true;
            Animator animator = _stageBeatActor.Animator.GetAnimator;
            if (animator == null || !animator.isHuman)
                return false;
            Transform bone = animator.GetBoneTransform(_stageBeatNode.stagePropBone);
            if (bone == null)
                return false;
            _stageBeatProp = Instantiate(_stageBeatNode.stagePropPrefab, bone);
            _stageBeatProp.transform.localPosition = _stageBeatNode.stagePropLocalPosition;
            _stageBeatProp.transform.localEulerAngles = _stageBeatNode.stagePropLocalEulerAngles;
            _stageBeatProp.transform.localScale = _stageBeatNode.stagePropLocalScale;
            // 전시 소품이 기존 프리팹의 물리 설정으로 손이나 주변 액터를 밀지 않게 한다.
            foreach (Collider collider in _stageBeatProp.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
            foreach (Rigidbody body in _stageBeatProp.GetComponentsInChildren<Rigidbody>(true))
                body.isKinematic = true;
            return true;
        }

        private void FinishStageBeat(bool succeeded)
        {
            Action callback = succeeded ? _stageBeatCompleted : _stageBeatFailed;
            if (!succeeded)
                Debug.LogWarning($"[Dialogue] 무언 행동 중단: actor={_stageBeat?.actorSpeakerId}, position={_stageBeatActor?.transform.position}, target={_stageBeatTarget?.position}, elapsed={_stageBeatElapsed:0.00}. 이동 대상과 지형을 확인하세요.");
            CancelStageBeat();
            callback?.Invoke();
        }

        /// <summary>스킵·씬 이탈·중단 시 이동과 콜백을 함께 해제한다.</summary>
        internal void CancelStageBeat()
        {
            StopStageBeatMovement();
            if (_hasStageBeatMotion && _stageBeatActor?.Animator != null)
                _stageBeatActor.Animator.RestorePlaybackSnapshot(_stageBeatPreviousMotion, 0.2f);
            _hasStageBeatMotion = false;
            _stageBeatMotion = null;
            if (_stageBeatProp != null)
            {
                _stageBeatProp.SetActive(false);
                Destroy(_stageBeatProp);
            }
            _stageBeatProp = null;
            _stageBeatNode = null;
            if (_stageBeatTarget != null)
                Destroy(_stageBeatTarget.gameObject);
            _stageBeatTarget = null;
            _stageBeatActor = null;
            _stageBeatLookTarget = null;
            _stageBeat = null;
            _isStageBeatStartPending = false;
            _stageBeatCompleted = null;
            _stageBeatFailed = null;
            _isStageBeatPaused = false;
        }
    }
}
