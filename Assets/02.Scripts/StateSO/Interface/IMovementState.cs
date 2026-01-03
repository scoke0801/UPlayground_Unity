using UnityEngine;

namespace Game.FSM
{
    public interface IMovementState
    {
        void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime, CharacterBrain brain);
        void UpdateRotation(ref Quaternion currentRotation, float deltaTime, CharacterBrain brain);
    }
}
