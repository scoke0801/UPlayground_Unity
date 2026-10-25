// 헤어 공유 메시·바인드 포즈를 유지하고 부착 위치와 앞뒤 폭을 맞춘다.
var profile=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/Lili_P09_Face.asset");
profile.hairAttachments=new[]{
    new UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO.HairAttachment{
        constraintPath="Mesh/Hair/Hair_03/Skeleton_P09_Hair_03/Constraint_Head",positionOffset=new Vector3(.02f,-.01f,0),localScale=new Vector3(.88f,1,1)},
    new UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO.HairAttachment{
        constraintPath="Mesh/Hair/Hair_11/Skeleton_P09_Hair_11/Constraint_Head",positionOffset=new Vector3(.02f,0,0),localScale=new Vector3(.88f,1,1)}
};
profile.ValidateProfile();
foreach(string name in new[]{"Lili_P09Compatible","Lili_P09_Alternate"}){
    string path="Assets/03.Prefabs/Characters/ExternalModels/"+name+".prefab";
    var root=PrefabUtility.LoadPrefabContents(path);
    try{profile.Apply(root);PrefabUtility.SaveAsPrefabAsset(root,path);}
    finally{PrefabUtility.UnloadPrefabContents(root);}
}
EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);
return "Hair 03: 아래 1cm/뒤 2cm, Hair 11: 뒤 2cm, 앞뒤 폭 88%. 두피·공유 메시·목·표정·GUID 보존.";
