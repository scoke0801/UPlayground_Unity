// 저장된 월드의 조명을 복제하고 게임 로직 없는 임시 Play Mode 검수 씬을 만든다.
if (EditorApplication.isPlayingOrWillChangePlaymode || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
    throw new Exception("저장된 편집 모드 씬에서 실행하세요.");
string output = "output/P09ToonLook";
System.IO.Directory.CreateDirectory(output);
string folder = "Assets/10.Datas/Actor/ScenarioResidents/ToonLook/Textures";
UPlayGround.Editor.P09Builder.PathConfig.EnsureFolderExists(folder);
var textureReport = new List<string>();
foreach (string suffix in new[] { "Diff", "Normal" })
{
    string source = "Assets/ExternalAssets/Character/P09_Modular_Humanoid/Model_DATA/Textures/Female_Body/P09_Female_Body_" + suffix + ".png";
    string target = folder + "/P09_Female_Body_" + suffix + "_4K.png";
    if (AssetDatabase.LoadAssetAtPath<Texture2D>(target) == null && !AssetDatabase.CopyAsset(source, target)) throw new Exception("텍스처 복사 실패");
    var importer = (TextureImporter)AssetImporter.GetAtPath(target);
    importer.maxTextureSize = 4096;
    importer.isReadable = false;
    var platform = importer.GetPlatformTextureSettings("Standalone");
    platform.overridden = true;
    platform.maxTextureSize = 4096;
    platform.format = suffix == "Diff" ? TextureImporterFormat.DXT1 : TextureImporterFormat.DXT5;
    importer.SetPlatformTextureSettings(platform);
    importer.SaveAndReimport();
    foreach (string p in new[] { source, target })
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        var ti = (TextureImporter)AssetImporter.GetAtPath(p);
        textureReport.Add(p + "|" + texture.width + "x" + texture.height + "|" + texture.format + "|readable=" + ti.isReadable + "|ProfilerBytes=" + UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture));
    }
}
System.IO.File.WriteAllLines(output + "/texture-comparison.txt", textureReport.ToArray());
var worldScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
string worldPath = worldScene.path;
var worldAmbientMode = RenderSettings.ambientMode;
var sky = RenderSettings.ambientSkyColor;
var equator = RenderSettings.ambientEquatorColor;
var ground = RenderSettings.ambientGroundColor;
var ambient = RenderSettings.ambientIntensity;
var skybox = RenderSettings.skybox;
var probe = RenderSettings.ambientProbe;
var lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.type == LightType.Directional && l.enabled).ToArray();
var lightCopies = lights.Select(l => UnityEngine.Object.Instantiate(l.gameObject)).ToArray();
var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Characters/ScenarioResidents/Npc/Npc_Daon/Npc_Daon.prefab"));
root.name = "P09_Review_Daon";
root.SetActive(false);
var npc = root.GetComponent<UPlayGround.NpcActor>();
if (npc != null) UnityEngine.Object.DestroyImmediate(npc);
foreach (var script in root.GetComponentsInChildren<MonoBehaviour>(true).Reverse())
    if (script != null) UnityEngine.Object.DestroyImmediate(script);
foreach (var animator in root.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
var review = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, review);
foreach (var light in lightCopies) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(light, review);
UnityEngine.SceneManagement.SceneManager.SetActiveScene(review);
RenderSettings.ambientMode = worldAmbientMode;
RenderSettings.ambientSkyColor = sky;
RenderSettings.ambientEquatorColor = equator;
RenderSettings.ambientGroundColor = ground;
RenderSettings.ambientIntensity = ambient;
RenderSettings.ambientProbe = probe;
RenderSettings.skybox = skybox;
RenderSettings.fog = false;
root.SetActive(true);
var camera = new GameObject("P09_ReviewCamera").AddComponent<Camera>();
camera.tag = "MainCamera";
camera.fieldOfView = 30;
camera.nearClipPlane = 0.01f;
camera.farClipPlane = 100;
camera.clearFlags = CameraClearFlags.SolidColor;
camera.backgroundColor = new Color(0.18f, 0.2f, 0.22f);
var animatorForHead = root.GetComponentInChildren<Animator>();
var head = animatorForHead.GetBoneTransform(HumanBodyBones.Head);
var targetPosition = head.position + Vector3.up * 0.08f;
camera.transform.position = targetPosition + Vector3.forward * 0.95f;
camera.transform.LookAt(targetPosition);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(review, "Assets/P09ToonReview_Temporary.unity");
UnityEditor.SceneManagement.EditorSceneManager.CloseScene(worldScene, true);
UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene = null;
System.IO.File.WriteAllText(output + "/review-setup.txt", "Source=" + worldPath + "\nAmbient=" + sky + "," + equator + "," + ground + " intensity=" + ambient + "\nLights=" + string.Join(";", lightCopies.Select(l => l.name + " " + l.transform.eulerAngles + " intensity=" + l.GetComponent<Light>().intensity).ToArray()) + "\n검수 씬은 게임 로직과 애니메이션을 제거한 고정 포즈이다. 통합 월드 검증과 구분한다.");
return textureReport;
