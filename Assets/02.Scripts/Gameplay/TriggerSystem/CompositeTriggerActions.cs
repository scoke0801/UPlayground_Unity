using System.Collections;
using UnityEngine;

namespace UPlayGround.TriggerSystem
{
    [CreateAssetMenu(menuName = "UPlayGround/Trigger/Action/Delay")]
    public sealed class DelayTriggerActionSO : TriggerActionSO
    {
        [Min(0f)]
        [SerializeField] private float _seconds = 1f;

        public override IEnumerator Execute(TriggerContext context)
        {
            if (_seconds > 0f)
                yield return new WaitForSeconds(_seconds);
        }
    }

    [CreateAssetMenu(menuName = "UPlayGround/Trigger/Action/Sequence")]
    public sealed class SequenceTriggerActionSO : TriggerActionSO
    {
        [SerializeField] private TriggerActionSO[] _steps;

        public override bool CanExecute(TriggerContext context)
        {
            if (_steps == null || _steps.Length == 0)
                return false;

            foreach (var step in _steps)
            {
                if (step != null && step.CanExecute(context))
                    return true;
            }

            return false;
        }

        public override bool ConsumesTrigger(TriggerContext context)
        {
            if (_steps == null || _steps.Length == 0)
                return false;

            foreach (var step in _steps)
            {
                if (step != null && !step.ConsumesTrigger(context))
                    return false;
            }

            return true;
        }

        public override IEnumerator Execute(TriggerContext context)
        {
            if (_steps == null)
                yield break;

            foreach (var step in _steps)
            {
                if (step == null || !step.CanExecute(context))
                    continue;

                yield return step.Execute(context);
            }
        }
    }
}
