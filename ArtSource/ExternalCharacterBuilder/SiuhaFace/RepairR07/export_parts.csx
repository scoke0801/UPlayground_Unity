// 실제 P09 카탈로그 전체와 현재 얼굴을 같은 메시 좌표로 내보낸다.
var folder="ArtSource/ExternalCharacterBuilder/SiuhaFace/RepairR07";
System.IO.Directory.CreateDirectory(folder);
var root=AssetDatabase.LoadAssetAtPath<GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics);
var parts=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r=>r.name.StartsWith("Hair_")||r.name=="SkinHead_Female"||r.name=="Female_Face_01"||r.name=="Female_Body_Chest").ToArray();
foreach(var skin in parts){
var m=skin.sharedMesh;
System.IO.File.WriteAllLines(folder+"/"+skin.name+"_bones.txt",skin.bones.Select(b=>b.name).ToArray());
using(var w=new System.IO.BinaryWriter(System.IO.File.Create(folder+"/"+skin.name+".bin"))){
w.Write(m.vertexCount);w.Write(m.subMeshCount);foreach(var v in m.vertices){w.Write(v.x);w.Write(v.y);w.Write(v.z);}foreach(var uv in m.uv){w.Write(uv.x);w.Write(uv.y);}
for(int s=0;s<m.subMeshCount;s++){var t=m.GetTriangles(s);w.Write(t.Length);foreach(int i in t)w.Write(i);}
foreach(var b in m.boneWeights){w.Write(b.boneIndex0);w.Write(b.boneIndex1);w.Write(b.boneIndex2);w.Write(b.boneIndex3);w.Write(b.weight0);w.Write(b.weight1);w.Write(b.weight2);w.Write(b.weight3);}
}
}
var profile=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/Siuha_P09_Face.asset");
System.IO.File.WriteAllLines(folder+"/Face_bones.txt",profile.boneNames);
return parts.Select(r=>r.name).ToArray();
