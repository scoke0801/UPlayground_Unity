using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Data.AI;

namespace UPlayGround.UI.Debugging.Laya
{
    /// <summary>Player가 직접 참조하는 디버거 화면·폰트·실험 프로필 묶음.</summary>
    public sealed class LayaDebuggerAssets : ScriptableObject
    {
        public VisualTreeAsset layout;
        public StyleSheet style;
        public PanelSettings panel;
        public Font koreanFont;
        public LayaTrainingProfileSO[] profiles;
    }
}
