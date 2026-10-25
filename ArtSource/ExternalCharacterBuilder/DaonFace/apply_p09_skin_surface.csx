var canonical="Assets/03.Prefabs/Characters/ScenarioResidents/Npc/Npc_Daon/Npc_Daon.prefab";
if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || UnityEditor.EditorApplication.isCompiling)throw new System.Exception("컴파일이 끝난 편집 모드에서 적용합니다.");
var review="Assets/03.Prefabs/Characters/ExternalModels/Daon_P09Compatible.prefab";
var skinPath="Assets/08.Models/Characters/DaonNewIdentity/Daon_P09_Skin.mat";
var oldSkin=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/10.Datas/Actor/ScenarioResidents/ToonLook/P09_Female_Skin_Toon.mat");
var face=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/08.Models/Characters/DaonNewIdentity/Daon_ho_face_alt4.mat");
var dependentPaths=new[]{
"Assets/03.Prefabs/Characters/EquipmentReview/DaonHead/Npc_Daon_Head.prefab",
"Assets/03.Prefabs/Characters/EquipmentReview/DaonPortrait/Npc_Daon_Portrait.prefab",
"Assets/03.Prefabs/Characters/EquipmentReview/DaonSculpt/Npc_Daon_Sculpt.prefab",
"Assets/03.Prefabs/Characters/HarborResidents/NPC_Harbor_Bora.prefab",
"Assets/03.Prefabs/Characters/HarborResidents/NPC_Harbor_Yuna.prefab",
"Assets/03.Prefabs/Environment/Farming/FarmWorker_0.prefab"};
if(UnityEditor.AssetDatabase.LoadMainAssetAtPath(skinPath)!=null)throw new System.Exception("기존 다온 피부 재질을 덮어쓰지 않습니다.");
System.Func<UnityEngine.GameObject,System.Collections.Generic.Dictionary<string,UnityEngine.Material[]>> snapshot=root=>root.GetComponentsInChildren<UnityEngine.Renderer>(true).ToDictionary(r=>UnityEditor.AnimationUtility.CalculateTransformPath(r.transform,root.transform),r=>r.sharedMaterials);
var snapshots=dependentPaths.ToDictionary(p=>p,p=>snapshot(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(p)));
var originalScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
string harborPath="Assets/01.Scenes/Scenario/SaltwindHarbor.unity";
var harbor=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(harborPath);
bool opened=!harbor.isLoaded;
if(!opened&&harbor.isDirty)throw new System.Exception("항구의 미저장 변경을 보존합니다.");
if(opened)harbor=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(harborPath,UnityEditor.SceneManagement.OpenSceneMode.Additive);
var residents=harbor.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UPlayGround.NpcActor>(true)).Where(a=>UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(a.gameObject)==canonical).ToArray();
if(residents.Length!=2){if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(harbor,true);throw new System.Exception("항구 공유 주민 수 변경: "+residents.Length);}
var residentSnapshots=residents.Select(a=>snapshot(a.gameObject)).ToArray();
System.Action<UnityEngine.GameObject,System.Collections.Generic.Dictionary<string,UnityEngine.Material[]>> restore=(root,data)=>{
    foreach(var renderer in root.GetComponentsInChildren<UnityEngine.Renderer>(true)){
        string path=UnityEditor.AnimationUtility.CalculateTransformPath(renderer.transform,root.transform);
        if(renderer.sharedMaterials.SequenceEqual(data[path]))continue;
        renderer.sharedMaterials=data[path];
        UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
    }
};
try
{
    var material=new UnityEngine.Material(face);
    material.name="Daon_P09_Skin";
    material.mainTexture=oldSkin.mainTexture;
    material.color=new UnityEngine.Color(.85f,.75f,.73f,1);
    UnityEditor.AssetDatabase.CreateAsset(material,skinPath);
    foreach(string path in new[]{canonical,review}){
        var root=UnityEditor.PrefabUtility.LoadPrefabContents(path);
        try{
            foreach(var renderer in root.GetComponentsInChildren<UnityEngine.Renderer>(true)){
                var values=renderer.sharedMaterials;
                for(int i=0;i<values.Length;i++)if(values[i]==oldSkin)values[i]=material;
                renderer.sharedMaterials=values;
            }
            if(UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,path)==null)throw new System.Exception("다온 피부 저장 실패: "+path);
        }finally{UnityEditor.PrefabUtility.UnloadPrefabContents(root);}
    }
    foreach(string path in dependentPaths){
        var root=UnityEditor.PrefabUtility.LoadPrefabContents(path);
        try{
            restore(root,snapshots[path]);
            if(UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,path)==null)throw new System.Exception("공유 외형 보존 저장 실패: "+path);
        }finally{UnityEditor.PrefabUtility.UnloadPrefabContents(root);}
    }
    for(int i=0;i<residents.Length;i++)restore(residents[i].gameObject,residentSnapshots[i]);
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(harbor);
    if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(harbor))throw new System.Exception("항구 피부 보존 저장 실패");
    var profile=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09ToonLookProfileSO>("Assets/10.Datas/Actor/ScenarioResidents/ToonLook/P09_Daon_ToonLook.asset");
    foreach(var binding in profile.bindings)if(binding.replacement==oldSkin)binding.replacement=material;
    profile.ValidateBindings();UnityEditor.EditorUtility.SetDirty(profile);UnityEditor.AssetDatabase.SaveAssetIfDirty(profile);
    foreach(string path in dependentPaths){
        var current=snapshot(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path));
        foreach(var pair in snapshots[path])if(!current[pair.Key].SequenceEqual(pair.Value))throw new System.Exception("공유 프리팹 외형 변경: "+path);
    }
    for(int i=0;i<residents.Length;i++){
        var current=snapshot(residents[i].gameObject);
        foreach(var pair in residentSnapshots[i])if(!current[pair.Key].SequenceEqual(pair.Value))throw new System.Exception("항구 주민 외형 변경");
    }
    System.IO.Directory.CreateDirectory("output/DaonSkinSeamFix");
    System.IO.File.WriteAllText("output/DaonSkinSeamFix/applied.txt","다온 전용 피부 저장 완료. 공유 프리팹 6개와 항구 주민 2명의 모든 재질 슬롯이 적용 전 스냅샷과 일치합니다.\n활성 씬: "+originalScene.path);
    return new{material=skinPath,preservedPrefabs=dependentPaths.Length,preservedHarborResidents=residents.Length,activeScene=originalScene.path};
}
finally
{
    if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(harbor,true);
    UnityEngine.SceneManagement.SceneManager.SetActiveScene(originalScene);
}
