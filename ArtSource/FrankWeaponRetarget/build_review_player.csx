const string scenePath = "Assets/01.Scenes/Tests/Equipment/Raon_FrankWeapon_Review.unity";
const string output = "Builds/RaonFrankWeapon_20261025/WeaponAnimationValidation.exe";
if (UnityEditor.EditorApplication.isPlaying || UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorUtility.scriptCompilationFailed)
    throw new System.InvalidOperationException("컴파일과 Play Mode를 마친 뒤 빌드하세요.");
for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
    if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new System.InvalidOperationException("저장하지 않은 씬을 보호하기 위해 빌드를 중단합니다.");
var scenes = UnityEditor.EditorBuildSettings.scenes;
var settingsBytes = System.IO.File.ReadAllBytes("ProjectSettings/EditorBuildSettings.asset");
var toonBytes = System.IO.File.ReadAllBytes("ProjectSettings/lilToonSetting.json");
var addressables = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
var option = addressables.BuildAddressablesWithPlayerBuild;
System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output));
System.IO.Directory.CreateDirectory("output/FrankWeaponRetarget");
System.IO.File.WriteAllBytes("output/FrankWeaponRetarget/EditorBuildSettings.backup", settingsBytes);
System.IO.File.WriteAllBytes("output/FrankWeaponRetarget/lilToonSetting.backup", toonBytes);
try {
    UnityEditor.EditorBuildSettings.scenes = new[] { new UnityEditor.EditorBuildSettingsScene(scenePath, true) };
    addressables.BuildAddressablesWithPlayerBuild = UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
    var report = UnityEditor.BuildPipeline.BuildPlayer(new UnityEditor.BuildPlayerOptions {
        scenes = new[] { scenePath }, locationPathName = output,
        target = UnityEditor.BuildTarget.StandaloneWindows64,
        options = UnityEditor.BuildOptions.Development | UnityEditor.BuildOptions.CompressWithLz4
    });
    var s = report.summary;
    string result = "Result=" + s.result + "\nErrors=" + s.totalErrors + "\nWarnings=" + s.totalWarnings + "\nSize=" + s.totalSize + "\nTime=" + s.totalTime + "\nOutput=" + s.outputPath;
    System.IO.File.WriteAllText("output/FrankWeaponRetarget/build.txt", result);
    return result;
}
catch (System.Exception exception) {
    System.IO.File.WriteAllText("output/FrankWeaponRetarget/build.txt", "Result=Exception\n" + exception);
    throw;
}
finally {
    addressables.BuildAddressablesWithPlayerBuild = option;
    UnityEditor.EditorBuildSettings.scenes = scenes;
    System.IO.File.WriteAllBytes("ProjectSettings/EditorBuildSettings.asset", settingsBytes);
    System.IO.File.WriteAllBytes("ProjectSettings/lilToonSetting.json", toonBytes);
}
