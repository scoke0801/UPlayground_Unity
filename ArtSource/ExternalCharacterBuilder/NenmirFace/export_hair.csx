var root=AssetDatabase.LoadAssetAtPath<GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics);
using(var w=new System.IO.BinaryWriter(System.IO.File.Create("ArtSource/ExternalCharacterBuilder/NenmirFace/hair_source.bin"))){
w.Write(2);
foreach(string name in new[]{"Hair_03","Hair_11"}){
var skin=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name==name);var mesh=skin.sharedMesh;
w.Write(name);w.Write(mesh.vertexCount);foreach(var p in mesh.vertices){w.Write(p.x);w.Write(p.y);w.Write(p.z);}
w.Write(mesh.triangles.Length);foreach(int i in mesh.triangles)w.Write(i);
}
}
return "원본 Hair03/11 메시를 읽기 전용으로 추출했습니다.";
