var folder="Assets/08.Models/Characters/SiuhaP09";
var sourceFolder="ArtSource/ExternalCharacterBuilder/SiuhaFace/RepairR02";
var profile=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/Siuha_P09_Face.asset");
var originalMesh=profile.mesh;
var mesh=new Mesh();mesh.name=originalMesh.name;
var shapeNames=new List<string>();var deltas=new List<Vector3[]>();
using(var r=new System.IO.BinaryReader(System.IO.File.OpenRead(sourceFolder+"/repaired_face.bin"))){
int n=r.ReadInt32(),slots=r.ReadInt32(),shapes=r.ReadInt32();
Func<Vector3[]> points=()=>{var a=new Vector3[n];for(int i=0;i<n;i++)a[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());return a;};
mesh.vertices=points();var uv=new Vector2[n];for(int i=0;i<n;i++)uv[i]=new Vector2(r.ReadSingle(),r.ReadSingle());mesh.uv=uv;mesh.subMeshCount=slots;
for(int s=0;s<slots;s++){var tris=new int[r.ReadInt32()];for(int i=0;i<tris.Length;i++)tris[i]=r.ReadInt32();mesh.SetTriangles(tris,s);}
for(int s=0;s<shapes;s++){shapeNames.Add(r.ReadString());deltas.Add(points());}
var weights=new BoneWeight[n];for(int i=0;i<n;i++)weights[i]=new BoneWeight{boneIndex0=r.ReadInt32(),boneIndex1=r.ReadInt32(),boneIndex2=r.ReadInt32(),boneIndex3=r.ReadInt32(),weight0=r.ReadSingle(),weight1=r.ReadSingle(),weight2=r.ReadSingle(),weight3=r.ReadSingle()};mesh.boneWeights=weights;
int bones=r.ReadInt32();for(int i=0;i<bones;i++)if(r.ReadString()!=profile.boneNames[i])throw new Exception("본 순서 변경");
if(r.BaseStream.Position!=r.BaseStream.Length)throw new Exception("교환 데이터 길이 불일치");
}
mesh.bindposes=originalMesh.bindposes;
var baseRoot=AssetDatabase.LoadAssetAtPath<GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics);
var body=baseRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Female_Body_Chest").sharedMesh;
var v=mesh.vertices;var bodyVertices=body.vertices;
var bottom=Enumerable.Range(v.Length-16,16).ToArray();
var bodyIndices=bottom.Select(i=>Array.FindIndex(bodyVertices,p=>Vector3.Distance(p,v[i])<.00001f)).ToArray();
if(bodyIndices.Any(i=>i<0))throw new Exception("몸통 경계 대응 누락");
var groups=mesh.GetTriangles(0).Concat(mesh.GetTriangles(2)).Distinct().GroupBy(i=>new Vector3(Mathf.Round(v[i].x*100000),Mathf.Round(v[i].y*100000),Mathf.Round(v[i].z*100000))).Where(g=>g.Count()>1).Select(g=>g.ToArray()).ToArray();
Action<Mesh> normals=m=>{
m.RecalculateNormals();var ns=m.normals;
// 접합선에서 면의 크기 차이 때문에 평균 노멀이 꺾이지 않도록 면적 가중치를 사용한다.
var ps=m.vertices;var weighted=new Vector3[ps.Length];var ts=m.GetTriangles(0).Concat(m.GetTriangles(2)).ToArray();
for(int i=0;i<ts.Length;i+=3){var normal=Vector3.Cross(ps[ts[i+1]]-ps[ts[i]],ps[ts[i+2]]-ps[ts[i]]);weighted[ts[i]]+=normal;weighted[ts[i+1]]+=normal;weighted[ts[i+2]]+=normal;}
foreach(var group in groups){var total=Vector3.zero;foreach(int i in group)total+=weighted[i];if(total.sqrMagnitude>.000000000001f){total.Normalize();foreach(int i in group)ns[i]=total;}}
for(int i=0;i<16;i++)ns[bottom[i]]=body.normals[bodyIndices[i]];
m.normals=ns;m.RecalculateTangents();
};
normals(mesh);var baseNormals=mesh.normals;var baseTangents=mesh.tangents;
var scratch=UnityEngine.Object.Instantiate(mesh);
try{for(int s=0;s<deltas.Count;s++){
var delta=deltas[s];scratch.vertices=v.Select((p,i)=>p+delta[i]).ToArray();normals(scratch);
var dn=scratch.normals.Select((p,i)=>p-baseNormals[i]).ToArray();var dt=scratch.tangents.Select((p,i)=>new Vector3(p.x-baseTangents[i].x,p.y-baseTangents[i].y,p.z-baseTangents[i].z)).ToArray();
mesh.AddBlendShapeFrame(shapeNames[s],100,delta,dn,dt);
}}finally{UnityEngine.Object.DestroyImmediate(scratch);}
mesh.RecalculateBounds();
originalMesh.Clear();originalMesh.ClearBlendShapes();originalMesh.vertices=mesh.vertices;originalMesh.uv=mesh.uv;originalMesh.normals=mesh.normals;originalMesh.tangents=mesh.tangents;originalMesh.boneWeights=mesh.boneWeights;originalMesh.bindposes=mesh.bindposes;originalMesh.subMeshCount=mesh.subMeshCount;
for(int s=0;s<mesh.subMeshCount;s++)originalMesh.SetTriangles(mesh.GetTriangles(s),s);
for(int s=0;s<mesh.blendShapeCount;s++){var dv=new Vector3[mesh.vertexCount];var dn=new Vector3[mesh.vertexCount];var dt=new Vector3[mesh.vertexCount];mesh.GetBlendShapeFrameVertices(s,0,dv,dn,dt);originalMesh.AddBlendShapeFrame(mesh.GetBlendShapeName(s),100,dv,dn,dt);}
originalMesh.RecalculateBounds();EditorUtility.SetDirty(originalMesh);AssetDatabase.SaveAssetIfDirty(originalMesh);UnityEngine.Object.DestroyImmediate(mesh);

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


