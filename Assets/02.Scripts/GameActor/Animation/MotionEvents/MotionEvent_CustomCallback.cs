using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 커스텀 콜백 이벤트
    /// </summary>
    [Serializable]
    [MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
    [MotionEventDescriptor("CustomCallback", "Utility", 0, "문자열 기반 커스텀 콜백을 발생시킵니다.", "callback", "custom", "콜백")]
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
