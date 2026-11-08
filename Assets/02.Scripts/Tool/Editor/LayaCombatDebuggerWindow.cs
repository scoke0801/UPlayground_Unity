#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.AI.CombatDecision;
using UPlayGround.Debugging.Laya;
using UPlayGround.UI.Debugging.Laya;

namespace UPlayGround.Tool.Editor
{
    /// <summary>현재 Editor 실험과 저장된 Player 기록에 동일 화면을 제공한다.</summary>
    public sealed class LayaCombatDebuggerWindow : EditorWindow
    {
        private LayaDiagnostics _imported;
        private LayaDebuggerView _view;
        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/게임플레이/전투/도구/Laya 전투 디버거")]
        public static void Open() => GetWindow<LayaCombatDebuggerWindow>("Laya 전투 디버거");
        public void CreateGUI()
        {
            rootVisualElement.Clear();
            var bar = new VisualElement(); bar.style.flexDirection = FlexDirection.Row;
            bar.Add(new Button(() => { _imported = null; CreateGUI(); }) { text = "현재 Editor 세션" });
            bar.Add(new Button(() => {
                var path = EditorUtility.OpenFilePanel("Player / Editor 실험 로그", "", "json");
                if (string.IsNullOrEmpty(path)) return;
                try { _imported = LayaDiagnostics.Import(path); CreateGUI(); }
                catch (System.Exception error) { EditorUtility.DisplayDialog("로그 열기 실패", error.Message, "확인"); }
            }) { text = "저장 로그 열기" });
            rootVisualElement.Add(bar);
            var assets = Resources.Load<LayaDebuggerAssets>("Laya/DebuggerAssets");
            if (assets == null) { rootVisualElement.Add(new Label("Laya 디버거 리소스가 없습니다.")); return; }
            var commands = new LayaDebuggerCommands {
                Start = (p,m,u) => LayaTrainingHost.Current?.CreateCommands().Start(p,m,u),
                Check = (u,p) => LayaTrainingHost.Current?.CreateCommands().Check(u,p),
                Stop = () => LayaTrainingHost.Current?.CreateCommands().Stop(),
                Restart = () => LayaTrainingHost.Current?.CreateCommands().Restart(),
                Export = () => LayaTrainingHost.Current?.Export(),
                Pause = p => LayaTrainingHost.Current?.CreateCommands().Pause(p), Close = Close };
            if (_imported != null) commands = new LayaDebuggerCommands { Close = Close };
            _view?.Dispose();
            _view = new LayaDebuggerView(assets, () => _imported ?? LayaTrainingHost.Current?.Diagnostics ?? LayaTrainingHost.LastDiagnostics, commands);
            rootVisualElement.Add(_view);
        }
        private void OnDisable() => _view?.Dispose();
    }
}
#endif
