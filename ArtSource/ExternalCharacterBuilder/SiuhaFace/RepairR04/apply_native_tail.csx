var sources=profile.fittedMeshes.ToArray();
profile.fittedMeshes=new UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO.FittedMesh[0];
profile.hairAttachments=new[]{
new UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO.HairAttachment{constraintPath="Mesh/Hair/Hair_03/Skeleton_P09_Hair_03/Constraint_Head",positionOffset=new Vector3(0,-.025f,0),localScale=Vector3.one},
new UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO.HairAttachment{constraintPath="Mesh/Hair/Hair_11/Skeleton_P09_Hair_11/Constraint_Head",positionOffset=new Vector3(0,.012f,0),localScale=Vector3.one}
};
profile.ValidateProfile();EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);
foreach(string suffix in new[]{"Compatible","_Alternate"}){
var path="Assets/03.Prefabs/Characters/ExternalModels/Siuha_P09"+suffix+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
try{
foreach(string name in new[]{"Hair_03","Hair_11"}){var original=baseRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name==name);var target=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name==name);target.sharedMesh=original.sharedMesh;}
profile.Apply(root);PrefabUtility.SaveAsPrefabAsset(root,path);
}finally{PrefabUtility.UnloadPrefabContents(root);}
}
return "R03 얼굴·두상 압축 원복. Hair03/11 원본 메시 복원. 스케일 1, 부착 위치만 적용.";
