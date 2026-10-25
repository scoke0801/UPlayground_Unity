var folder="ArtSource/ExternalCharacterBuilder/LianLianFace";
System.IO.Directory.CreateDirectory(folder);
var src=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalAssets/Character/LianLian/Prefabs/LianLian.prefab"));
var dst=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics));
try {
var skin=src.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Body");
var target=dst.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Female_Face_01");
var mesh=skin.sharedMesh; var native=target.sharedMesh;
var srcHead=src.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head).position;
var dstHead=dst.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head).position;
// 실제 얼굴 폭으로 배율을 산출하고 머리 본을 기준으로 정렬한다.
var scale=native.bounds.size.x/mesh.bounds.size.x;
var matrix=target.transform.worldToLocalMatrix*Matrix4x4.Translate(dstHead)*Matrix4x4.Scale(Vector3.one*scale)*Matrix4x4.Translate(-srcHead)*skin.bones[4].localToWorldMatrix*mesh.bindposes[4];
using(var w=new System.IO.BinaryWriter(System.IO.File.Create(folder+"/source.bin"))){
w.Write(mesh.vertexCount);w.Write(mesh.subMeshCount);w.Write(mesh.blendShapeCount);
Action<Vector3[]> points=a=>{foreach(var v in a){w.Write(v.x);w.Write(v.y);w.Write(v.z);}};
points(mesh.vertices.Select(v=>matrix.MultiplyPoint3x4(v)).ToArray());
foreach(var uv in mesh.uv){w.Write(uv.x);w.Write(uv.y);}
for(int s=0;s<mesh.subMeshCount;s++){var t=mesh.GetTriangles(s);w.Write(t.Length);foreach(int i in t)w.Write(i);}
for(int s=0;s<mesh.blendShapeCount;s++){w.Write(mesh.GetBlendShapeName(s));var d=new Vector3[mesh.vertexCount];mesh.GetBlendShapeFrameVertices(s,0,d,null,null);points(d.Select(v=>matrix.MultiplyVector(v)).ToArray());}
w.Write(native.vertexCount);points(native.vertices);var tris=native.GetTriangles(0);w.Write(tris.Length);foreach(int i in tris)w.Write(i);
}
var report=new {scale=scale,sourceHead=srcHead.ToString("F5"),targetHead=dstHead.ToString("F5"),matrix=matrix.ToString("F8"),sourceVertices=mesh.vertexCount,sourceShapes=mesh.blendShapeCount,targetBones=target.bones.Select(b=>b.name).ToArray(),nativeBounds=native.bounds.ToString("F5")};
// SkinHead_Female은 앞면이 열린 캡이므로 피팅 시 원본 얼굴 표면과 합쳐 사용한다.
var scalp=dst.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="SkinHead_Female").sharedMesh;
using(var w=new System.IO.BinaryWriter(System.IO.File.Create(folder+"/scalp.bin"))){
w.Write(scalp.vertexCount);foreach(var v in scalp.vertices){w.Write(v.x);w.Write(v.y);w.Write(v.z);}
w.Write(scalp.triangles.Length);foreach(int i in scalp.triangles)w.Write(i);
}
System.IO.File.WriteAllText(folder+"/coordinates.txt",report.ToString());
return report;
}finally{UnityEngine.Object.DestroyImmediate(src);UnityEngine.Object.DestroyImmediate(dst);}

