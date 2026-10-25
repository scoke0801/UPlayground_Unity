var folder="ArtSource/ExternalCharacterBuilder/SiuhaFace/RepairR02";
var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics));
try{
var names=new[]{"Hair_03","Hair_11","SkinHead_Female","Female_Body_Chest"};
foreach(string name in names){
var skin=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name==name);var m=skin.sharedMesh;
using(var w=new System.IO.BinaryWriter(System.IO.File.Create(folder+"/"+name+".bin"))){
w.Write(m.vertexCount);w.Write(m.subMeshCount);foreach(var v in m.vertices){w.Write(v.x);w.Write(v.y);w.Write(v.z);}foreach(var uv in m.uv){w.Write(uv.x);w.Write(uv.y);}
for(int s=0;s<m.subMeshCount;s++){var t=m.GetTriangles(s);w.Write(t.Length);foreach(int i in t)w.Write(i);}
foreach(var b in m.boneWeights){w.Write(b.boneIndex0);w.Write(b.boneIndex1);w.Write(b.boneIndex2);w.Write(b.boneIndex3);w.Write(b.weight0);w.Write(b.weight1);w.Write(b.weight2);w.Write(b.weight3);}
}
}
return "원본 헤어 03/11·뒤통수 캡·몸통 내보냄";
}finally{UnityEngine.Object.DestroyImmediate(root);}
