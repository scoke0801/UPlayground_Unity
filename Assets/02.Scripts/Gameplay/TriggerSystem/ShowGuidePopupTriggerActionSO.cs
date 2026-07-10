using System.Collections;
using UnityEngine;
using UPlayGround.Data.UI;
using UPlayGround.Manager;
using UPlayGround.UI.Guide;

namespace UPlayGround.TriggerSystem
{
    [CreateAssetMenu(menuName = "UPlayGround/트리거/액션/Show Guide Popup")]
    public sealed class ShowGuidePopupTriggerActionSO : TriggerActionSO
    {
        [SerializeField] private GuidePopupDataSO _guideData;
        [Min(0)]
        [SerializeField] private int _startPageIndex = 0;
        [Tooltip("켜면 가이드 팝업이 닫힐 때까지 Sequence의 다음 Action 실행을 기다린다.")]
        [SerializeField] private bool _waitForClose = true;

        public override bool CanExecute(TriggerContext context)
        {
            return _guideData != null && UIManager.Instance != null;
        }

        public override bool ConsumesTrigger(TriggerContext context)
        {
            return context != null && context.ActionConsumesTrigger;
        }

        public override IEnumerator Execute(TriggerContext context)
        {
            var popup = GuidePopupRuntime.Open(_guideData, _startPageIndex);
            bool opened = popup != null;

            if (context != null)
                context.ActionConsumesTrigger = opened;

            if (!opened || !_waitForClose)
                yield break;

            while (popup != null && popup.IsVisible)
                yield return null;
        }
    }
}
