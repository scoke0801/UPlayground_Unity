using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.Gameplay.World.LivingWorld;

namespace UPlayGround.CombatValidation
{
    /// <summary>독립 GPU 검증 씬에서 관측점·공격 자극·날씨를 직접 조작한다.</summary>
    public sealed class AmbientFaunaPreviewDriver : MonoBehaviour
    {
        [SerializeField] private AmbientFaunaHabitat _habitat;
        [SerializeField] private Transform _observer;
        [SerializeField, Min(0)] private float _speed = 2;
        private int _sequence;
        private float _hour = 18.5f, _rain;

        private void Update()
        {
            if (_habitat == null || _observer == null) return;
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            Vector2 movement = pad != null ? pad.leftStick.ReadValue() : Vector2.zero;
            if (keyboard != null)
            {
                movement.x += (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
                movement.y += (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
                if (keyboard.nKey.wasPressedThisFrame) _hour = 22;
                if (keyboard.tKey.wasPressedThisFrame) _hour = 12;
                if (keyboard.rKey.wasPressedThisFrame) _rain = 1 - _rain;
            }
            if (pad != null && pad.dpad.up.wasPressedThisFrame) _hour = 12;
            if (pad != null && pad.dpad.down.wasPressedThisFrame) _hour = 22;
            if (pad != null && pad.dpad.right.wasPressedThisFrame) _rain = 1 - _rain;
            movement = Vector2.ClampMagnitude(movement, 1);
            _observer.position += new Vector3(movement.x, 0, movement.y) * (_speed * Time.deltaTime);
            _habitat.SetPreviewEnvironment(_hour, _rain, 0);
            if ((keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
                || (pad != null && pad.buttonSouth.wasPressedThisFrame))
                _habitat.EmitStimulus(_observer.position, 4, 3, 0.8f, ++_sequence);
        }
    }
}
