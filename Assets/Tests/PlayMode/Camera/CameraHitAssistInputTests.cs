using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem.PlayModeTests
{
    /// <summary>실제 입력 시스템이 타격 보정의 제어권을 즉시 가져오는지 검증한다.</summary>
    public sealed class CameraHitAssistInputTests
    {
        private sealed class TestAdapter : CameraRuntimeAdapterBase
        {
            public InputAction Look;
            public override bool TryGetPlayerAction(string name, out InputAction action)
            {
                action = name == CameraRuntimeServices.LookAction ? Look : null;
                return action != null;
            }
        }

        [UnityTest]
        public IEnumerator 수동_마우스_입력은_타격_보정을_끊고_연출_전환은_보존한다()
        {
            var settings = ScriptableObject.CreateInstance<CameraSettings>();
            var cameraObject = new GameObject("카메라", typeof(Camera));
            var player = new GameObject("플레이어");
            var target = new GameObject("대상");
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            using var look = new InputAction(type: InputActionType.Value, binding: "<Mouse>/delta");
            bool cursorWasVisible = Cursor.visible;
            InputSettings inputSettings = InputSystem.settings;
            InputSettings.BackgroundBehavior previousBackgroundBehavior = inputSettings.backgroundBehavior;
#if UNITY_EDITOR
            InputSettings.EditorInputBehaviorInPlayMode previousEditorBehavior =
                inputSettings.editorInputBehaviorInPlayMode;
#endif
            try
            {
                inputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
                inputSettings.editorInputBehaviorInPlayMode =
                    InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
                InputSystem.EnableDevice(mouse);
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.enabled = false;
                camera.aspect = 16f / 9f;
                camera.fieldOfView = 58f;
                target.transform.position = new Vector3(9f, 0f, 8f);
                var context = new CameraContext(new CameraState())
                {
                    Settings = settings, MainCamera = camera, Target = player.transform,
                    LastManualInputTime = -100f, RotationTransition = new CameraRotationTransition()
                };
                var pose = new CameraPose
                {
                    CameraPosition = new Vector3(0f, 0f, -5.7f), CameraRotation = Quaternion.identity,
                    Distance = 5.7f, FieldOfView = 58f
                };
                CameraRuntimeServices.Configure(new TestAdapter { Look = look });
                Cursor.visible = false;
                look.Enable();
                yield return null;
                context.HitAssist.Submit(new CameraHitAssist.Request(
                    target.transform, target.transform.position, 1f, 60f, 0.12f, 0.45f));
                context.HitAssist.Apply(ref pose, context, 0.02f);
                Assert.That(context.HitAssist.IsActive, Is.True);
                context.RotationTransition.Start(0f, 0f, 90f, 0f, 1f);
                InputSystem.QueueDeltaStateEvent(mouse.delta, new Vector2(10f, 0f));
                InputSystem.Update();
                Assert.That(look.ReadValue<Vector2>().x, Is.GreaterThan(0f));
                new InGameCameraBehavior().HandleInput(context, 0.016f);
                Assert.That(context.State.CurrentYaw, Is.GreaterThan(0f));
                Assert.That(context.HitAssist.IsActive, Is.False);
                Assert.That(context.RotationTransition.IsActive, Is.True);
                Assert.That(context.LastManualInputTime, Is.EqualTo(Time.unscaledTime));
            }
            finally
            {
                CameraRuntimeServices.Reset();
                InputSystem.RemoveDevice(mouse);
                inputSettings.backgroundBehavior = previousBackgroundBehavior;
#if UNITY_EDITOR
                inputSettings.editorInputBehaviorInPlayMode = previousEditorBehavior;
#endif
                Cursor.visible = cursorWasVisible;
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(player);
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }
    }
}
