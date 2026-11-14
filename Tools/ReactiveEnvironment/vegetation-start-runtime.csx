var failed = typeof(UnityEditor.EditorUtility).GetProperty("scriptCompilationFailed", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
if (UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || failed != null && (bool)failed.GetValue(null,null)) return "BUSY";
foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
{
    var type = assembly.GetType("UnityEditor.TestTools.TestRunner.Api.TestRunnerApi");
    if (type == null) continue;
    var active = type.GetMethod("IsTestExecutionActive", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
    if (active != null && (bool)active.Invoke(null,null)) return "BUSY tests";
}
for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)return "BUSY dirty";var setup=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();var restore=new System.Text.StringBuilder();foreach(var s in setup)restore.AppendLine(s.path+"\t"+s.isLoaded+"\t"+s.isActive);System.IO.File.WriteAllText("output/ReactiveEnvironment/VegetationConversion-20261114/runtime-scene-restore.tsv",restore.ToString());UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity",UnityEditor.SceneManagement.OpenSceneMode.Single);UnityEditor.SessionState.SetString("VegetationConversion.PlayOwner","20261114");System.IO.File.WriteAllText("output/ReactiveEnvironment/VegetationConversion-20261114/runtime-start-request.txt",System.DateTime.UtcNow.ToString("o"));UnityEditor.EditorApplication.isPlaying=true;return "식생 런타임 확인 시작";
