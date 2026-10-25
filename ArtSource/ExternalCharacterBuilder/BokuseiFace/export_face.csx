var folder="ArtSource/ExternalCharacterBuilder/BokuseiFace";
var src=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalAssets/Character/ROKO SHOP/Bokusei/01_Prefab/Bokusei.prefab"));
var dst=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabMaleNoPhysics));
try {
var skin=src.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Body");
var target=dst.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Male_Face_01");
var mesh=skin.sharedMesh;var native=target.sharedMesh;
int sourceHead=Array.FindIndex(skin.bones,b=>b.name=="Head");
int targetHead=Array.FindIndex(target.bones,b=>b.name=="Head");
var srcHead=skin.bones[sourceHead].position;var dstHead=target.bones[targetHead].position;
// 귀의 폭 차이가 머리 전체를 지나치게 축소하지 않도록 실제 두개골 높이로 배율을 정한다.
float scale=(native.bounds.max.z-target.transform.InverseTransformPoint(dstHead).z)/(mesh.bounds.max.y-srcHead.y);
var matrix=target.transform.worldToLocalMatrix*Matrix4x4.Translate(dstHead)*Matrix4x4.Scale(Vector3.one*scale)*Matrix4x4.Translate(-srcHead)*skin.bones[sourceHead].localToWorldMatrix*mesh.bindposes[sourceHead];
using(var w=new System.IO.BinaryWriter(System.IO.File.Create(folder+"/source.bin"))){
w.Write(mesh.vertexCount);w.Write(mesh.subMeshCount);w.Write(mesh.blendShapeCount);
Action<Vector3[]> points=a=>{foreach(var v in a){w.Write(v.x);w.Write(v.y);w.Write(v.z);}};
points(mesh.vertices.Select(v=>matrix.MultiplyPoint3x4(v)).ToArray());
foreach(var uv in mesh.uv){w.Write(uv.x);w.Write(uv.y);}
for(int s=0;s<mesh.subMeshCount;s++){var t=mesh.GetTriangles(s);w.Write(t.Length);foreach(int i in t)w.Write(i);}
for(int s=0;s<mesh.blendShapeCount;s++){if(mesh.GetBlendShapeFrameCount(s)!=1)throw new Exception("다중 프레임 표정 발견");w.Write(mesh.GetBlendShapeName(s));var d=new Vector3[mesh.vertexCount];mesh.GetBlendShapeFrameVertices(s,0,d,null,null);points(d.Select(v=>matrix.MultiplyVector(v)).ToArray());}
w.Write(native.vertexCount);points(native.vertices);var tris=native.GetTriangles(0);w.Write(tris.Length);foreach(int i in tris)w.Write(i);
}
var data=new{scale=scale,matrix=matrix.ToString("F7"),sourceHead=srcHead.ToString("F5"),targetHead=dstHead.ToString("F5"),targetHeadLocal=target.transform.InverseTransformPoint(dstHead).ToString("F5"),sourceVertices=mesh.vertexCount,sourceShapes=mesh.blendShapeCount,targetBones=target.bones.Select(b=>b.name).ToArray(),weightedBones=mesh.boneWeights.SelectMany(w=>new[]{w.weight0>0?skin.bones[w.boneIndex0].name:null,w.weight1>0?skin.bones[w.boneIndex1].name:null,w.weight2>0?skin.bones[w.boneIndex2].name:null,w.weight3>0?skin.bones[w.boneIndex3].name:null}).Where(n=>n!=null).Distinct().ToArray(),eyeRotations=skin.bones.Where(b=>b.name.Contains("Eye")).Select(b=>new{name=b.name,rotation=b.localEulerAngles.ToString("F4")}).ToArray()};
var jsonType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
System.IO.File.WriteAllText(folder+"/coordinates.json",(string)jsonType.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{data}));
return data;
}finally{UnityEngine.Object.DestroyImmediate(src);UnityEngine.Object.DestroyImmediate(dst);}
