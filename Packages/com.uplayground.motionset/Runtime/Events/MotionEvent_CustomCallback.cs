using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 커스텀 콜백 이벤트
    /// </summary>
    [Serializable]
    [MotionEventMeta("CustomCallback", Category = "Utility", CategoryOrder = 40,
        Description = "문자열 기반 커스텀 콜백을 남깁니다.",
        Aliases = new[] { "callback", "custom", "콜백" },
        Icon = "⚙", Color = new[] { 0.60f, 0.60f, 0.65f })]
    [MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
    public class CustomCallbackEvent : MotionEventBase
    {
        public string callbackName;
        public string[] parameters;

        public override string GetDisplayName() => "Callback";

        public override string GetShortLabel()
        {
            if (!string.IsNullOrEmpty(callbackName))
                return $"Call: {callbackName}";
            return "Callback";
        }

        public override void Execute(GameObject target)
        {
            Debug.Log($"Callback: {callbackName}");
        }

        public override void OnCompleteEvent(GameObject target)
        {
        }
    }
}