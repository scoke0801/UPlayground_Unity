// 저장 전 후보만 만든다. 기존 에셋은 commit_candidate.csx에서 검증 후 갱신한다.
var profile=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/Siuha_P09_Face.asset");
foreach(var old in Resources.FindObjectsOfTypeAll<Mesh>().Where(m=>m.name.StartsWith("Siuha_R07_")&&!EditorUtility.IsPersistent(m)).ToArray())UnityEngine.Object.DestroyImmediate(old);
foreach(var old in Resources.FindObjectsOfTypeAll<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>().Where(p=>p.name=="Siuha_R07_Candidate").ToArray())UnityEngine.Object.DestroyImmediate(old);
var candidate=UnityEngine.Object.Instantiate(profile);candidate.name="Siuha_R07_Candidate";candidate.hideFlags=HideFlags.HideAndDontSave;
var baseRoot=AssetDatabase.LoadAssetAtPath<GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics);
Func<string,Vector3[]> read=name=>{
using(var reader=new System.IO.BinaryReader(System.IO.File.OpenRead("ArtSource/ExternalCharacterBuilder/SiuhaFace/RepairR07/"+name+"_positions.bin"))){
var points=new Vector3[reader.ReadInt32()];for(int i=0;i<points.Length;i++)points[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());if(reader.BaseStream.Position!=reader.BaseStream.Length)throw new Exception("좌표 길이 불일치");return points;}
};
var face=UnityEngine.Object.Instantiate(profile.mesh);face.name="Siuha_R07_Face";face.hideFlags=HideFlags.HideAndDontSave;
var positions=read("Face");if(positions.Length!=face.vertexCount)throw new Exception("얼굴 정점 수 변경");
face.vertices=positions;
var body=baseRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Female_Body_Chest").sharedMesh;
var bodyVertices=body.vertices;var bodyNormals=body.normals;
var duplicates=face.GetTriangles(0).Concat(face.GetTriangles(2)).Distinct().GroupBy(i=>new Vector3(Mathf.Round(positions[i].x*100000),Mathf.Round(positions[i].y*100000),Mathf.Round(positions[i].z*100000))).Where(g=>g.Count()>1).Select(g=>g.ToArray()).ToArray();
Action<Mesh> normals=mesh=>{
mesh.RecalculateNormals();var values=mesh.normals;var points=mesh.vertices;var weighted=new Vector3[points.Length];var tris=mesh.GetTriangles(0).Concat(mesh.GetTriangles(2)).ToArray();
for(int t=0;t<tris.Length;t+=3){var normal=Vector3.Cross(points[tris[t+1]]-points[tris[t]],points[tris[t+2]]-points[tris[t]]);weighted[tris[t]]+=normal;weighted[tris[t+1]]+=normal;weighted[tris[t+2]]+=normal;}
foreach(var group in duplicates){var total=Vector3.zero;foreach(int i in group)total+=weighted[i];foreach(int i in group)values[i]=total.normalized;}
for(int i=points.Length-16;i<points.Length;i++){int match=Array.FindIndex(bodyVertices,p=>Vector3.Distance(p,positions[i])<.00001f);if(match<0)throw new Exception("몸 경계 변경");values[i]=bodyNormals[match];}
mesh.normals=values;mesh.RecalculateTangents();
};
face.ClearBlendShapes();normals(face);var baseNormals=face.normals;var baseTangents=face.tangents;
var scratch=UnityEngine.Object.Instantiate(face);
try{for(int s=0;s<profile.mesh.blendShapeCount;s++){
var dv=new Vector3[face.vertexCount];profile.mesh.GetBlendShapeFrameVertices(s,0,dv,null,null);scratch.vertices=positions.Select((p,i)=>p+dv[i]).ToArray();normals(scratch);
var dn=scratch.normals.Select((n,i)=>n-baseNormals[i]).ToArray();var dt=scratch.tangents.Select((n,i)=>new Vector3(n.x-baseTangents[i].x,n.y-baseTangents[i].y,n.z-baseTangents[i].z)).ToArray();
face.AddBlendShapeFrame(profile.mesh.GetBlendShapeName(s),100,dv,dn,dt);
}}finally{UnityEngine.Object.DestroyImmediate(scratch);}
face.RecalculateBounds();candidate.mesh=face;
var fitted=new List<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO.FittedMesh>();
var attachments=new List<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO.HairAttachment>();
foreach(var skin in baseRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r=>r.name.StartsWith("Hair_"))){
var mesh=UnityEngine.Object.Instantiate(skin.sharedMesh);mesh.name="Siuha_R07_"+skin.name;mesh.hideFlags=HideFlags.HideAndDontSave;var points=read(skin.name);if(points.Length!=mesh.vertexCount)throw new Exception("헤어 정점 수 변경");mesh.vertices=points;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
fitted.Add(new UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO.FittedMesh{rendererPath=AnimationUtility.CalculateTransformPath(skin.transform,baseRoot.transform),sourceMesh=skin.sharedMesh,mesh=mesh});
attachments.Add(new UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO.HairAttachment{constraintPath="Mesh/Hair/"+skin.name+"/Skeleton_P09_"+skin.name+"/Constraint_Head",positionOffset=Vector3.zero,localScale=Vector3.one});
}
candidate.fittedMeshes=fitted.ToArray();candidate.hairAttachments=attachments.ToArray();candidate.ValidateProfile();
return new {face=face.vertexCount,hair=fitted.Count};
