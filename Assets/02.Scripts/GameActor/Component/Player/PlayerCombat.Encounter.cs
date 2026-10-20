using UPlayGround.Manager;

namespace UPlayGround.Components
{
    public partial class PlayerCombat
    {
        /// <summary>콤보 이어치기를 포함한 공격 시작을 공통 정렬과 관찰자에게 알린다.</summary>
        private void NotifyAttackStarted()
        {
            _playerActor.PlayerController?.BeginActionAlignment();
            UPlayGround.Combat.EncounterCompositionTelemetry.RecordAttack(_playerActor);
            OnAttackStarted?.Invoke(_currentAttackData);
        }

        private float ResolveEncounterAssistRange(float distance, bool isDash)
        {
            if (!Services.TryGet<ICombatEncounterService>(out var encounter) || encounter.EncounterProfile == null)
                return distance;
            return distance * encounter.EncounterProfile.GetAssistMultiplier(encounter.CurrentEncounter, isDash);
        }
    }
}
