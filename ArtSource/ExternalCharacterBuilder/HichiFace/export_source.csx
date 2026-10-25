// 바인드 자세의 얼굴과 실제 P09 베이스를 기록한다. 원본 눈 본의 현재 포즈를 굽지 않는다.
var dir="ArtSource/ExternalCharacterBuilder/HichiFace";
System.IO.Directory.CreateDirectory(dir);
var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalAssets/Character/Hichi/Prefab/Hichi.prefab");
var target=AssetDatabase.LoadAssetAtPath<GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics);
var src=source.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="hichi_face");
var dst=target.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Female_Face_01");
var mesh=src.sharedMesh;var native=dst.sharedMesh;
System.Func<Vector3[],float[][]> points=vs=>vs.Select(v=>new[]{v.x,v.y,v.z}).ToArray();
var shapes=new System.Collections.Generic.List<object>();
for(int s=0;s<mesh.blendShapeCount;s++)
{
    var delta=new Vector3[mesh.vertexCount];mesh.GetBlendShapeFrameVertices(s,0,delta,null,null);
    shapes.Add(new{name=mesh.GetBlendShapeName(s),weight=mesh.GetBlendShapeFrameWeight(s,0),delta=points(delta)});
}
var data=new{vertices=points(mesh.vertices),normals=points(mesh.normals),uv=mesh.uv.Select(v=>new[]{v.x,v.y}).ToArray(),submeshes=Enumerable.Range(0,mesh.subMeshCount).Select(mesh.GetTriangles).ToArray(),shapes,nativeVertices=points(native.vertices),nativeTriangles=native.GetTriangles(0),nativeShapes=Enumerable.Range(0,native.blendShapeCount).Select(native.GetBlendShapeName).ToArray(),bones=dst.bones.Select(b=>b.name).ToArray(),sourceBones=src.bones.Select(b=>b.name).ToArray(),weights=mesh.boneWeights.Select(w=>new{indices=new[]{w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3},values=new[]{w.weight0,w.weight1,w.weight2,w.weight3}}).ToArray(),nativeHead=points(new[]{dst.transform.InverseTransformPoint(dst.bones.Single(b=>b.name=="Head").position)})[0]};
var jsonType=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var json=(string)jsonType.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{data});
System.IO.File.WriteAllText(dir+"/source.json",json);
return new{vertices=mesh.vertexCount,shapes=shapes.Count,bones=dst.bones.Length};
