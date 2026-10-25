// 헤어 공유 메시·바인드 포즈를 유지하고 얼굴별 부착 위치와 앞뒤 폭을 조정한다.
var profile=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/LianLian_P09_Face.asset");
profile.hairAttachments=new[]{
    new UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO.HairAttachment{
        constraintPath="Mesh/Hair/Hair_03/Skeleton_P09_Hair_03/Constraint_Head",positionOffset=new Vector3(.015f,-.018f,0),localScale=new Vector3(.88f,1,.95f)},
    new UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO.HairAttachment{
        constraintPath="Mesh/Hair/Hair_11/Skeleton_P09_Hair_11/Constraint_Head",positionOffset=new Vector3(.015f,0,0),localScale=new Vector3(.88f,1,.95f)}
};
profile.ValidateProfile();
foreach(string name in new[]{"LianLian_P09Compatible","LianLian_P09_Alternate"}){
    string path="Assets/03.Prefabs/Characters/ExternalModels/"+name+".prefab";
    var root=PrefabUtility.LoadPrefabContents(path);
    try{profile.Apply(root);PrefabUtility.SaveAsPrefabAsset(root,path);}
    finally{PrefabUtility.UnloadPrefabContents(root);}
}
EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);
return "Hair 03: 아래 1.8cm/뒤 1.5cm, Hair 11: 뒤 1.5cm. 두 항목 앞뒤 88%/좌우 95%, 높이 유지. 공유 메시·바인드 포즈 보존.";



