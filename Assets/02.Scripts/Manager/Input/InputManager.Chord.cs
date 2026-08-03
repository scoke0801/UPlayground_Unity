using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UPlayGround.InputDefine;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 입력 시스템 관리 매니저 - 조합키 런타임 중재 (스펙 §9)
    /// </summary>
    public partial class InputManager
    {
        private readonly InputChordArbiter<InputAction.CallbackContext> _chordArbiter = new();
        private readonly List<InputArbiterEvent<InputAction.CallbackContext>> _arbiterDispatch = new(8);

        // 조합 Modifier의 눌림 여부는 "지금 이벤트를 보낸 장치" 기준으로만 판정한다.
        // 키보드 이벤트가 패드 Modifier로 조합을 성립시키는 오판을 막는다.
        private InputDevice _arbiterProbeDevice;
        private bool _flushingArbiterDispatch;

        private void InitChordArbiter()
        {
            _chordArbiter.GraceSeconds = InputChordArbiter<InputAction.CallbackContext>.DefaultGraceSeconds;
            _chordArbiter.IsControlPressed = IsProbeControlPressed;
            RebuildChordCatalog();
        }

        /// <summary>
        /// 현재 effective binding을 기준으로 조합 후보 카탈로그를 다시 만든다.
        /// 바인딩 프로필이 적용될 때마다 호출해야 한다.
        /// </summary>
        private void RebuildChordCatalog()
        {
            _chordArbiter.ClearCatalog();
            _chordArbiter.Reset();

            foreach (InputActionMap map in actionMapCache.Values)
            {
                foreach (InputAction action in map.actions)
                {
                    var bindings = action.bindings;
                    for (int i = 0; i < bindings.Count; i++)
                    {
                        if (!bindings[i].isComposite)
                            continue;

                        string modifier = null;
                        string trigger = null;
                        for (int p = i + 1; p < bindings.Count && bindings[p].isPartOfComposite; p++)
                        {
                            if (string.Equals(bindings[p].name, "modifier", StringComparison.OrdinalIgnoreCase))
                                modifier = bindings[p].effectivePath;
                            else if (string.Equals(bindings[p].name, "binding", StringComparison.OrdinalIgnoreCase))
                                trigger = bindings[p].effectivePath;
                        }

                        if (!string.IsNullOrWhiteSpace(modifier) && !string.IsNullOrWhiteSpace(trigger))
                            _chordArbiter.RegisterChord(map.name, action.name, modifier, trigger);
                    }
                }
            }
        }

        private bool IsProbeControlPressed(string relativePath)
        {
            InputControl control = FindControlOnDevice(_arbiterProbeDevice, relativePath);
            return control is ButtonControl button && button.isPressed;
        }

        /// <summary>
        /// 물리 입력을 중재기에 넣고, 확정된 이벤트만 콜백 라우터로 흘려보낸다.
        /// </summary>
        private void SubmitToChordArbiter(
            InputAction.CallbackContext context,
            InputArbiterPhase phase)
        {
            InputAction action = context.action;
            InputActionMap map = action?.actionMap;
            if (map == null)
                return;

            _arbiterProbeDevice = context.control?.device;
            _chordArbiter.Submit(
                map.name,
                action.name,
                phase,
                context.control?.path,
                Time.unscaledTime,
                context,
                _arbiterDispatch);

            FlushArbiterDispatch();
        }

        private void TickChordArbiter()
        {
            if (_chordArbiter.PendingCount == 0)
                return;

            _chordArbiter.Tick(Time.unscaledTime, _arbiterDispatch);
            FlushArbiterDispatch();
        }

        /// <summary>
        /// 확정 큐를 인덱스로 훑어 비운다. Move/Look 같은 PassThrough 액션이 매 프레임 통과하므로
        /// 복사본을 만들지 않는다. 콜백이 다시 입력을 넣으면 재진입 대신 이 루프가 이어서 처리한다.
        /// </summary>
        private void FlushArbiterDispatch()
        {
            if (_flushingArbiterDispatch || _arbiterDispatch.Count == 0)
                return;

            _flushingArbiterDispatch = true;
            try
            {
                for (int i = 0; i < _arbiterDispatch.Count; i++)
                    DispatchArbitratedEvent(_arbiterDispatch[i]);
            }
            finally
            {
                _arbiterDispatch.Clear();
                _flushingArbiterDispatch = false;
            }
        }

        private void DispatchArbitratedEvent(InputArbiterEvent<InputAction.CallbackContext> evt)
        {
            switch (evt.Phase)
            {
                case InputArbiterPhase.Started:
                    ExecuteCallbacksForAction(evt.Context, startCallbackDict, evt.MapName, evt.ActionName);
                    break;

                case InputArbiterPhase.Performed:
                    TryBufferPlayerAction(evt);
                    ExecuteCallbacksForAction(evt.Context, performCallbackDict, evt.MapName, evt.ActionName);
                    break;

                case InputArbiterPhase.Canceled:
                    if (evt.IsSynthetic)
                        _inputBuffer?.ConsumeInput(evt.ActionName);
                    ExecuteCallbacksForAction(evt.Context, cancelCallbackDict, evt.MapName, evt.ActionName);
                    break;
            }
        }

        /// <summary>
        /// 전투 선입력 버퍼는 물리 입력 시점이 아니라 중재 확정 시점에 넣는다(스펙 §9.3).
        /// 다만 만료 기준은 원래 물리 입력 시각을 사용해 grace 지연만큼 버퍼 창이 줄지 않게 한다.
        /// </summary>
        private void TryBufferPlayerAction(InputArbiterEvent<InputAction.CallbackContext> evt)
        {
            if (CurrentLayer != InputLayer.Level_0)
                return;
            if (evt.MapName != InputMapNames.PlayerAction)
                return;

            // 아래 목록은 "중재 확정 시 버퍼에 적재할 액션"이며,
            // "버퍼에 1개만 유지할 액션"은 InputBuffer.IsSingleSlotAction이 단독으로 판정한다.
            // 두 정책은 별개이므로 여기서 교체 여부를 다시 지정하지 않는다.
            switch (evt.ActionName)
            {
                case InputDefine.PlayerAction.Attack:
                case InputDefine.PlayerAction.HeavyAttack:
                case InputDefine.PlayerAction.Dodge:
                case InputDefine.PlayerAction.Jump:
                case InputDefine.PlayerAction.Dash:
                case InputDefine.PlayerAction.SkillAbility:
                case InputDefine.PlayerAction.SkillUltimate:
                case InputDefine.PlayerAction.ElementBuff:
                case InputDefine.PlayerAction.CharacterSwap_1:
                case InputDefine.PlayerAction.CharacterSwap_2:
                case InputDefine.PlayerAction.CharacterSwap_3:
                case InputDefine.PlayerAction.CharacterSwap_4:
                    _inputBuffer.AddInput(
                        evt.ActionName,
                        bufferTime: GetPlayerActionBufferTime(evt.ActionName),
                        timestamp: ToBufferTimestamp(evt.PhysicalTime));
                    break;
            }
        }

        // 중재기는 unscaledTime, InputBuffer는 Time.time을 쓴다.
        // 지연분(unscaled)을 scaled 축으로 되돌려 만료 기준을 물리 입력 시점에 맞춘다.
        private static float ToBufferTimestamp(float unscaledPhysicalTime)
        {
            float delay = Mathf.Max(0f, Time.unscaledTime - unscaledPhysicalTime);
            return Time.time - delay;
        }
    }
}
