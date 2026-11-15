using UnityEngine;
using UPlayGround.Data.Pet;
using UPlayGround.State;

namespace UPlayGround
{
    public partial class MonsterActor
    {
        private PetActor _distractingPet;
        private PetDisruptionEffectSO _distractionEffect;
        private float _distractionEnds;

        public void ApplyPetDistraction(PetActor pet, PetDisruptionEffectSO effect)
        {
            if (pet == null || effect == null || !IsAlive() || !Effects.HasActiveEffect(effect)) return;
            _distractingPet = pet; _distractionEffect = effect;
            _distractionEnds = Time.time + effect.distractionSeconds;
        }
        public bool IsPetDistractionActive
        {
            get
            {
                if (!isActiveAndEnabled || !IsAlive() || _distractingPet == null || !_distractingPet.isActiveAndEnabled
                    || Time.time >= _distractionEnds || _distractionEffect == null || !Effects.HasActiveEffect(_distractionEffect))
                { ClearPetDistraction(); return false; }
                return true;
            }
        }
        private void ClearPetDistraction()
        { _distractingPet = null; _distractionEffect = null; _distractionEnds = 0f; }

        /// <summary>기존 공격·강제 반응은 유지하며 평상 회전이 가능한 상태에서만 주의를 돌린다.</summary>
        public bool TryGetPetDistractionRotation(Quaternion rotation, float deltaTime, out Quaternion result)
        {
            result = rotation;
            if (!IsPetDistractionActive || ActorController?.CurrentState == null
                || ActorController.CurrentState.BlocksBehaviorTree) return false;
            var state = ActorController.CurrentState.StateId;
            if (state != ActorStateId.Idle && state != ActorStateId.Chase) return false;
            Vector3 direction = _distractingPet.transform.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < .001f) return false;
            result = Quaternion.Slerp(rotation, Quaternion.LookRotation(direction),
                1f - Mathf.Exp(-_distractionEffect.facingSharpness * deltaTime));
            return true;
        }
    }
}
