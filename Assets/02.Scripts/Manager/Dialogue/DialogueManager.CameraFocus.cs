using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.CameraSystem;
using UPlayGround.Diagnostics;
using UPlayGround.Manager;

namespace UPlayGround.Dialogue
{
    /// <summary>
    /// 대사 한 줄이 제3의 인물을 가리킬 때, 그 인물을 잠시 잡았다가 대화 구도로 돌아오는 주목 컷.
    ///
    /// 대화 진행 자체는 멈추지 않는다. 라인을 읽는 동안 대상을 보여주고 시간이 지나면 스스로 복귀하므로
    /// 플레이어가 먼저 진행해도 다음 라인의 샷이 그대로 덮어써 연출이 진행을 막지 않는다.
    /// </summary>
    public partial class DialogueManager
    {
        private readonly DialogueFocusCutaway _lineFocus = new();
        private readonly HashSet<string> _warnedMissingFocusSpeakerIds = new(StringComparer.Ordinal);

        // 주목 컷 구도와 복귀할 라인 구도. SequenceId는 push 직전에 새로 부여한다.
        private DialogueShotRequest _lineFocusRequest;
        private DialogueShotRequest _lineFocusReturnRequest;

        /// <summary>
        /// 이 라인이 지정한 주목 대상을 해석한다. 화자 자신이나 월드에 없는 인물은 대상이 될 수 없다.
        /// 카메라뿐 아니라 화자·청자의 시선도 이 대상을 향하므로 카메라 구성보다 먼저 호출한다.
        /// </summary>
        private Transform ResolveLineFocusTarget(DialogueNodeSO node, Transform speaker)
        {
            if (node == null
                || string.IsNullOrEmpty(node.focusSpeakerId)
                || node.focusHoldSeconds <= 0f)
            {
                return null;
            }

            Transform target = ResolveSpeakerTransform(node.focusSpeakerId);
            if (target == null)
            {
                WarnMissingFocusSubjectOnce(node.focusSpeakerId, node);
                return null;
            }

            // 화자 자신을 가리키면 컷할 곳이 없다 — 라인 구도가 이미 그를 잡고 있다.
            return target != speaker ? target : null;
        }

        /// <summary>이 라인의 주목 컷을 예약한다. 대기 시간이 0이면 즉시 대상 구도로 넘어간다.</summary>
        private void BeginLineFocusCutaway(
            DialogueNodeSO node,
            Transform focusTarget,
            in DialogueShotRequest lineRequest)
        {
            if (focusTarget == null)
                return;

            _lineFocusReturnRequest = lineRequest;

            // 복귀 push는 같은 라인의 재진입이므로 대사 길이를 다시 세지 않는다.
            // 그대로 두면 짧은 라인 누적이 한 라인에서 두 번 올라가 이후 구도 판정이 밀린다.
            _lineFocusReturnRequest.LineLength = 0;
            _lineFocusRequest = new DialogueShotRequest
            {
                Speaker = focusTarget,

                // 어깨를 걸치는 기준은 이 라인의 화자다 — 대상을 "대화 쪽에서 본" 구도가 된다.
                Listener = lineRequest.Speaker,
                ShotType = node.focusShotType,
                LineLength = 0
            };

            RuntimeLog.Trace(
                RuntimeLogCategory.System,
                $"[Dialogue] 주목 컷 예약: '{node.focusSpeakerId}' 대기 {node.focusDelaySeconds}s / 유지 {node.focusHoldSeconds}s");

            if (_lineFocus.Begin(node.focusDelaySeconds, node.focusHoldSeconds)
                == DialogueFocusStep.EnterFocus)
            {
                PushLineFocusShot();
            }
        }

        /// <summary>주목 컷의 시간을 진행시킨다. 대화가 정지 중이면 연출도 함께 멈춘다.</summary>
        private void TickLineFocusCutaway()
        {
            if (!_lineFocus.IsActive || _playback.IsPaused)
                return;

            switch (_lineFocus.Tick(Time.unscaledDeltaTime))
            {
                case DialogueFocusStep.EnterFocus:
                    PushLineFocusShot();
                    break;

                case DialogueFocusStep.ReturnToLine:
                    PushLineFocusReturnShot();
                    break;
            }
        }

        /// <summary>진행 중인 주목 컷을 버린다. 다음 라인이 자기 구도를 push하므로 복귀는 필요 없다.</summary>
        private void ClearLineFocusCutaway()
        {
            _lineFocus.Reset();
            _lineFocusRequest = default;
            _lineFocusReturnRequest = default;
        }

        private void PushLineFocusShot()
        {
            // 연출 도중 대상이 사라지면(파티 합류·스트리밍) 컷을 포기하고 라인 구도를 유지한다.
            if (_lineFocusRequest.Speaker == null)
            {
                ClearLineFocusCutaway();
                return;
            }

            _lineFocusRequest.SequenceId = NextDialogueShotSequence();
            _dialogueCameraPushed |= CameraManager.Instance?.PushDialogueCamera(_lineFocusRequest) ?? false;
        }

        private void PushLineFocusReturnShot()
        {
            if (_lineFocusReturnRequest.Speaker == null)
            {
                ClearLineFocusCutaway();
                return;
            }

            _lineFocusReturnRequest.SequenceId = NextDialogueShotSequence();
            _dialogueCameraPushed |= CameraManager.Instance?.PushDialogueCamera(_lineFocusReturnRequest) ?? false;
            ClearLineFocusCutaway();
        }

        /// <summary>저작된 주목 대상이 월드에 없음을 대상마다 한 번만 알린다.</summary>
        private void WarnMissingFocusSubjectOnce(string speakerId, DialogueNodeSO node)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!_warnedMissingFocusSpeakerIds.Add(speakerId))
                return;

            Debug.LogWarning(
                $"[Dialogue] 주목 컷 대상 '{speakerId}'의 월드 액터를 찾지 못해 연출을 건너뜁니다."
                + " 화자 ID 표기 또는 해당 인물의 배치를 확인하세요.",
                node);
#endif
        }
    }
}
