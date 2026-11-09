// execute_code에 본문을 전달한다. 본편만 명시적으로 수정하고 다른 열린 씬의 활성·dirty 상태를 보존한다.
// 변경 전 TPS/GPU는 별도 사용자 검수이며 원본 백업으로 재현 경로를 보존한다.
string path = "Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity";
string output = "output/RegionalSky/Followup";
var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
var previousActive = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var otherScenes = new System.Collections.Generic.List<UnityEngine.SceneManagement.Scene>();
var otherDirtyStates = new System.Collections.Generic.List<bool>();
for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
{
    var other = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
    if (other == scene) continue;
    otherScenes.Add(other);
    otherDirtyStates.Add(other.isDirty);
}
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || UnityEditor.EditorApplication.isCompiling
    || !scene.IsValid() || !scene.isLoaded || scene.isDirty)
    throw new System.InvalidOperationException("미저장 변경이 없는 본편이 로드된 Edit Mode여야 합니다.");
var renderings = new System.Collections.Generic.List<UPlayGround.Gameplay.World.SceneEnvironmentRendering>();
var bindings = new System.Collections.Generic.List<UPlayGround.Gameplay.World.WorldLightingSceneBinding>();
var environments = new System.Collections.Generic.List<UPlayGround.Gameplay.World.LivingWorld.EnvironmentController>();
var observers = new System.Collections.Generic.List<UPlayGround.Gameplay.World.LivingWorld.EnvironmentSceneBinding>();
var waters = new System.Collections.Generic.List<Renderer>();
foreach (var root in scene.GetRootGameObjects())
{
    renderings.AddRange(root.GetComponentsInChildren<UPlayGround.Gameplay.World.SceneEnvironmentRendering>(true));
    bindings.AddRange(root.GetComponentsInChildren<UPlayGround.Gameplay.World.WorldLightingSceneBinding>(true));
    environments.AddRange(root.GetComponentsInChildren<UPlayGround.Gameplay.World.LivingWorld.EnvironmentController>(true));
    observers.AddRange(root.GetComponentsInChildren<UPlayGround.Gameplay.World.LivingWorld.EnvironmentSceneBinding>(true));
    foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        if (renderer.name == "LakeWater") waters.Add(renderer);
}
if (renderings.Count != 1 || bindings.Count != 1 || environments.Count != 1 || observers.Count != 1 || waters.Count != 1)
    throw new System.InvalidOperationException("기존 하늘·조명·날씨·관측 바인딩과 LakeWater가 각각 하나여야 합니다.");
var rendering = renderings[0];
var binding = bindings[0];
var water = waters[0];
var clear = UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.World.SkyProfileSO>("Assets/10.Datas/World/Sky/Sky_LakeClear.asset");
var haze = UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.World.SkyProfileSO>("Assets/10.Datas/World/Sky/Sky_CoastalHaze.asset");
var data = new UnityEditor.SerializedObject(rendering);
if (clear == null || haze == null || binding.sunLight == null || binding.globalVolume == null
    || data.FindProperty("_skySource").objectReferenceValue == null)
    throw new System.InvalidOperationException("기존 프로필·하늘·해·Volume 참조가 누락되었습니다.");
var skySource = (Material)data.FindProperty("_skySource").objectReferenceValue;
if (UnityEditor.ShaderUtil.ShaderHasError(skySource.shader))
    throw new System.InvalidOperationException("하늘 셰이더 컴파일 오류를 먼저 복구해야 합니다.");
if (data.FindProperty("_skyRegions").arraySize != 0 || data.FindProperty("_defaultSkyProfile").objectReferenceValue != null)
    throw new System.InvalidOperationException("기존 지역 저작값을 덮어쓰지 않습니다. 먼저 diff를 검토하세요.");
// 본편 수면 메시의 실제 범위와 R07/R08 신단 접근점을 근거로 삼는다. 검수 리그 좌표는 사용하지 않는다.
var bounds = water.bounds;
if (!bounds.Contains(new Vector3(1090, bounds.center.y, 818)) || bounds.size.x <= 1 || bounds.size.z <= 1)
    throw new System.InvalidOperationException("수면 범위와 기존 신단 접근점이 달라졌습니다. 지역 경계를 재조사하세요.");
if (water.sharedMaterial == null || water.sharedMaterial.shader.name != "UPlayGround/Environment/Lake Water")
    throw new System.InvalidOperationException("수면의 실제 반사 소비 셰이더가 바뀌었습니다.");
foreach (var root in scene.GetRootGameObjects())
    if (root.GetComponentsInChildren<ReflectionProbe>(true).Length != 0)
        throw new System.InvalidOperationException("기존 프로브가 있습니다. 중복 생성 전에 영향 범위를 조사하세요.");
System.IO.Directory.CreateDirectory(output + "/baseline");
string backup = output + "/baseline/LakeVillageWorld.before-integration.unity";
if (System.IO.File.Exists(backup)) throw new System.InvalidOperationException("기존 작업 전 백업을 덮어쓰지 않습니다.");
System.IO.File.Copy(path, backup);
UnityEditor.Undo.IncrementCurrentGroup();
int group = UnityEditor.Undo.GetCurrentGroup();
UnityEditor.Undo.SetCurrentGroupName("호수 지역 하늘 연결");
try
{
    var regionObject = new GameObject("SkyRegion_LakeShoreMist");
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(regionObject, scene);
    UnityEditor.Undo.RegisterCreatedObjectUndo(regionObject, "실제 호수 지역 생성");
    regionObject.transform.position = new Vector3(bounds.center.x, 100, bounds.center.z);
    var region = UnityEditor.Undo.AddComponent<UPlayGround.Gameplay.World.SkyRegionBinding>(regionObject);
    region.profile = haze;
    region.priority = 10;
    region.size = new Vector3(bounds.size.x + 40, 2000, bounds.size.z + 40);
    region.blendDistance = 60;
    var probeObject = new GameObject("RegionalSky_LakeReflection");
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(probeObject, scene);
    UnityEditor.Undo.RegisterCreatedObjectUndo(probeObject, "호수 반사 연결");
    probeObject.transform.position = bounds.center + Vector3.up * 2;
    var probe = UnityEditor.Undo.AddComponent<ReflectionProbe>(probeObject);
    probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
    probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
    probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
    probe.resolution = 64;
    probe.cullingMask = 0;
    probe.clearFlags = UnityEngine.Rendering.ReflectionProbeClearFlags.Skybox;
    probe.boxProjection = false;
    probe.size = new Vector3(bounds.size.x + 200, 2000, bounds.size.z + 200);
    UnityEditor.Undo.RecordObjects(new UnityEngine.Object[] { rendering, binding, observers[0], water }, "기존 작성자 연결");
    data.Update();
    data.FindProperty("_defaultSkyProfile").objectReferenceValue = clear;
    data.FindProperty("_skyRegions").arraySize = 1;
    data.FindProperty("_skyRegions").GetArrayElementAtIndex(0).objectReferenceValue = region;
    data.FindProperty("_environment").objectReferenceValue = environments[0];
    data.FindProperty("_skyObserver").objectReferenceValue = null;
    data.FindProperty("_sunLight").objectReferenceValue = binding.sunLight;
    data.FindProperty("_skyReflectionProbe").objectReferenceValue = probe;
    data.FindProperty("_reflectionUpdateSeconds").floatValue = 2;
    data.ApplyModifiedProperties();
    binding.environment = environments[0];
    binding.skyRendering = rendering;
    binding.disableWorldLighting = false;
    var observerData = new UnityEditor.SerializedObject(observers[0]);
    observerData.FindProperty("_skyRendering").objectReferenceValue = rendering;
    observerData.ApplyModifiedProperties();
    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(observers[0]);
    // 수면 셰이더는 단일 unity_SpecCube0를 사용하므로 블렌딩을 약속하지 않는다.
    water.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Simple;
    water.probeAnchor = probe.transform;
    if (UnityEngine.SceneManagement.SceneManager.GetActiveScene() != previousActive)
        throw new System.InvalidOperationException("활성 씬이 바뀌었습니다. 저장을 중단합니다.");
    for (int i = 0; i < otherScenes.Count; i++)
        if (!otherScenes[i].isLoaded || otherScenes[i].isDirty != otherDirtyStates[i])
            throw new System.InvalidOperationException("다른 작업 씬의 상태가 바뀌었습니다. 저장을 중단합니다.");
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    UnityEditor.Undo.FlushUndoRecordObjects();
    if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))
        throw new System.InvalidOperationException("씬 저장 실패");
    UnityEditor.Undo.CollapseUndoOperations(group);
    System.IO.File.WriteAllText(output + "/integration-applied.json", "{\"scene\":\"" + path
        + "\",\"activeScenePreserved\":\"" + previousActive.path
        + "\",\"waterBounds\":\"" + bounds.ToString()
        + "\",\"regionCenter\":\"" + region.transform.position.ToString()
        + "\",\"regionSize\":\"" + region.size.ToString()
        + "\",\"blendMeters\":60,\"probeResolution\":64,\"probeIntervalSeconds\":2,\"playMode\":\"사용자 테스트 대기\"}");
    return "본편 기본=LakeClear / 실제 수면 범위+20m=CoastalHaze, 외곽 혼합60m. 조명·날씨·실제 카메라·64px/2초 반사 연결. 화면/GPU 검증 대기.";
}
catch
{
    UnityEditor.Undo.RevertAllDownToGroup(group);
    System.IO.File.Copy(backup, path, true);
    throw;
}
