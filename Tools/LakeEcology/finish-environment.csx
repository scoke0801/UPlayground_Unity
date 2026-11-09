// 정규 호수 프리팹에 광학·부유물 레이어를 연결한다. 반복 실행해도 복제하지 않는다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed || scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("정상 정규 월드 Edit Mode 필요");
string folder="Assets/10.Datas/World/OpenWorld/LakeUnderwaterEcology";
var root=scene.GetRootGameObjects().Single(g=>g.name=="LakeUnderwaterEcology");
var water=UnityEngine.Object.FindObjectsByType<UPlayGround.Components.FootstepWaterSurface>(FindObjectsSortMode.None).Single(w=>w.gameObject.scene==scene && w.name=="LakeWater");
var profile=AssetDatabase.LoadAssetAtPath<UPlayGround.Data.World.UnderwaterProfileSO>(folder+"/LakeOptics.asset");
if(profile==null){profile=ScriptableObject.CreateInstance<UPlayGround.Data.World.UnderwaterProfileSO>();AssetDatabase.CreateAsset(profile,folder+"/LakeOptics.asset");}
EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);
var region=water.GetComponent<UPlayGround.Gameplay.World.UnderwaterRegion>();
if(region==null)region=water.gameObject.AddComponent<UPlayGround.Gameplay.World.UnderwaterRegion>();
var binding=new SerializedObject(region);binding.FindProperty("_surface").objectReferenceValue=water;binding.FindProperty("_profile").objectReferenceValue=profile;binding.ApplyModifiedPropertiesWithoutUndo();
region.enabled=false;region.enabled=true;
var rendererData=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
var feature=rendererData.rendererFeatures.OfType<UPlayGround.Gameplay.World.UnderwaterRendererFeature>().FirstOrDefault();
if(feature==null){feature=ScriptableObject.CreateInstance<UPlayGround.Gameplay.World.UnderwaterRendererFeature>();feature.name="Lake Underwater";AssetDatabase.AddObjectToAsset(feature,rendererData);rendererData.rendererFeatures.Add(feature);}
var featureData=new SerializedObject(feature);featureData.FindProperty("_shader").objectReferenceValue=Shader.Find("Hidden/UPlayGround/LakeUnderwater");featureData.ApplyModifiedPropertiesWithoutUndo();feature.Create();rendererData.SetDirty();EditorUtility.SetDirty(rendererData);AssetDatabase.SaveAssetIfDirty(rendererData);
Func<string,string,Material> materialFor=(file,shader)=>{var m=AssetDatabase.LoadAssetAtPath<Material>(folder+"/"+file+".mat");if(m==null){var s=Shader.Find(shader);if(s==null)throw new Exception(shader);m=new Material(s);AssetDatabase.CreateAsset(m,folder+"/"+file+".mat");}return m;};
var underside=root.transform.Find("WaterUnderside");
if(underside==null){underside=new GameObject("WaterUnderside").transform;underside.SetParent(root.transform);}
underside.SetPositionAndRotation(water.surfaceMesh.transform.position,water.surfaceMesh.transform.rotation);underside.localScale=water.surfaceMesh.transform.lossyScale;
var filter=underside.GetComponent<MeshFilter>();if(filter==null)filter=underside.gameObject.AddComponent<MeshFilter>();filter.sharedMesh=water.surfaceMesh.sharedMesh;
var surfaceRenderer=underside.GetComponent<MeshRenderer>();if(surfaceRenderer==null)surfaceRenderer=underside.gameObject.AddComponent<MeshRenderer>();surfaceRenderer.sharedMaterial=materialFor("LakeUnderside","UPlayGround/Water/LakeUnderside");surfaceRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
var particlesMaterial=materialFor("SuspendedParticles","UPlayGround/Water/SuspendedParticles");particlesMaterial.SetFloat("_SurfaceY",water.surfaceMesh.transform.position.y-.1f);EditorUtility.SetDirty(particlesMaterial);AssetDatabase.SaveAssetIfDirty(particlesMaterial);
var particleRoot=root.transform.Find("SuspendedLife");if(particleRoot==null){particleRoot=new GameObject("SuspendedLife").transform;particleRoot.SetParent(root.transform);}
foreach(var school in root.GetComponentsInChildren<Animation>()){
    if(particleRoot.Find(school.name)!=null)continue;
    var go=new GameObject(school.name);go.transform.SetParent(particleRoot);go.transform.position=school.transform.position-Vector3.up*.25f;
    var particles=go.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
    var main=particles.main;main.loop=true;main.prewarm=true;main.startLifetime=14;main.startSpeed=0;main.startSize=new ParticleSystem.MinMaxCurve(.025f,.065f);main.startColor=Color.white;main.maxParticles=24;main.simulationSpace=ParticleSystemSimulationSpace.Local;main.cullingMode=ParticleSystemCullingMode.PauseAndCatchup;
    var emission=particles.emission;emission.rateOverTime=1.5f;
    var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(4,.7f,4);
    var velocity=particles.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.Local;velocity.x=.025f;velocity.y=.012f;velocity.z=.015f;
    var color=particles.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.2f),new GradientAlphaKey(1,.7f),new GradientAlphaKey(0,1)});color.color=gradient;
    var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=particlesMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
}
PrefabUtility.ApplyPrefabInstance(root,InteractionMode.AutomatedAction);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("씬 저장 실패");
return new{region=region.name,particles=particleRoot.childCount,feature=feature.name};
