using System;
using System.IO;
using System.Reflection;
using UnityEditor;
namespace UPlayGround.UI.Tests
{
    internal static class StaminaShaderDiagnostic
    {
        [InitializeOnLoadMethod]
        private static void CaptureGeneratedShader()
        {
            EditorApplication.delayCall += () =>
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var type = assembly.GetType("UnityEditor.ShaderGraph.ShaderGraphImporter");
                    if (type == null) continue;
                    foreach (var method in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Static))
                    {
                        if (method.Name != "GetShaderText" || method.GetParameters().Length != 2) continue;
                        object[] args = { "Assets/06.Shaders/UI/StaminaWaterCanvas.shadergraph", null };
                        Directory.CreateDirectory("Logs/StaminaHud");
                        File.WriteAllText("Logs/StaminaHud/Generated.shader", (string)method.Invoke(null, args));
                        return;
                    }
                }
            };
        }
    }
}
