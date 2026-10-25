var presetPath="Assets/10.Datas/Actor/ExternalModels/Daon_P09_BuildPreset.asset";
var modelPath="Assets/03.Prefabs/Characters/ExternalModels/Daon_P09Compatible.prefab";
if(UnityEditor.AssetDatabase.LoadMainAssetAtPath(presetPath)!=null||UnityEditor.AssetDatabase.LoadMainAssetAtPath(modelPath)!=null)throw new System.Exception("기존 P09 호환 결과를 보존합니다.");
var saved=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.BuildPreset>("Assets/10.Datas/Actor/ScenarioResidents/Npc_Daon_BuildPreset.asset");
var preset=UnityEngine.ScriptableObject.CreateInstance<UPlayGround.Editor.P09Builder.BuildPreset>();
preset.config=UnityEngine.JsonUtility.FromJson<UPlayGround.Editor.P09Builder.CharacterBuildConfig>(UnityEngine.JsonUtility.ToJson(saved.config));
var config=preset.config;config.ExternalModelPrefab=null;config.ExternalMotionSet=null;config.UseMagicaCloth=false;
config.FaceMeshProfile=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/Daon_P09_Face.asset");
config.UseManualName=true;config.ManualName="Daon_P09_NewIdentity";config.SaveBaseFolder="Assets/03.Prefabs/Characters/ExternalModels";
preset.description="새 얼굴을 P09 여성 바디·헤어·의상 슬롯에 연결한 프리셋. 정규 다온과 구분하여 새 경로에 생성한다.";
var root=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics));
try
{
    UnityEditor.PrefabUtility.UnpackPrefabInstance(root,UnityEditor.PrefabUnpackMode.Completely,UnityEditor.InteractionMode.AutomatedAction);
    var ctx=new UPlayGround.Editor.P09Builder.BuildContext(config);ctx.RootInstance=root;
    new UPlayGround.Editor.P09Builder.ApplyAppearanceStep().Execute(ctx);
    root.name="Daon_P09Compatible";
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,modelPath);
    UnityEditor.AssetDatabase.CreateAsset(preset,presetPath);UnityEditor.AssetDatabase.SaveAssets();
    return new {model=modelPath,preset=presetPath,renderers=root.GetComponentsInChildren<UnityEngine.Renderer>().Where(r=>r.enabled).Select(r=>r.name).ToArray()};
}
finally{UnityEngine.Object.DestroyImmediate(root);}
