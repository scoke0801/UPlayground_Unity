var model=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/03.Prefabs/Characters/ExternalModels/Honoka_Traveler_v2.prefab");
var config=new UPlayGround.Editor.P09Builder.CharacterBuildConfig {
    ActorKind=UPlayGround.Editor.P09Builder.BuilderActorKind.Npc,
    ExternalModelPrefab=model,
    ExternalMotionSet=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.Animation.ActorAnimationMotionSet>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/MotionSet_Npc_Female.asset"),
    UseManualName=true,ManualName="NPC_External_Honoka_Traveler",UseMagicaCloth=false,
    SaveBaseFolder=UPlayGround.Editor.P09Builder.ExternalCharacterAssembler.OutputFolder
};
config.Stats.npcDisplayName="여행자";
config.Stats.npcEnableWander=false;
var output=config.SaveBaseFolder+"/Npc/"+config.ManualName+"/"+config.ManualName+".prefab";
if(UnityEditor.AssetDatabase.LoadMainAssetAtPath(output)!=null) return "기존 NPC 샘플 보존: "+output;
var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scratch=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);
UnityEngine.SceneManagement.SceneManager.SetActiveScene(scratch);
try
{
    var result=new UPlayGround.Editor.P09Builder.PrefabBuildPipeline().Build(config);
    if(!result.Success) throw new System.Exception(result.ErrorMessage);
    var preset=UnityEngine.ScriptableObject.CreateInstance<UPlayGround.Editor.P09Builder.BuildPreset>();
    preset.config=config;
    preset.description="외부 모델 장비 빌더의 NPC 연결 샘플. 정규 다온과 시나리오 연결은 변경하지 않음.";
    var presetPath="Assets/10.Datas/Actor/ExternalModels/Honoka_Traveler_NpcPreset.asset";
    if(UnityEditor.AssetDatabase.LoadMainAssetAtPath(presetPath)==null) UnityEditor.AssetDatabase.CreateAsset(preset,presetPath);
    else UnityEngine.Object.DestroyImmediate(preset);
    return new {result.Success,result.PrefabPath,result.GeneratedAssetPaths,result.Logs};
}
finally
{
    UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scratch,true);
}
