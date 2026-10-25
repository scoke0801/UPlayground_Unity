// 후보가 있으면 저장 전 검사하고, 없으면 실제 저장된 얼굴을 검사한다.
var profile=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/Nenmir_P09_Face.asset");
var mesh=Resources.FindObjectsOfTypeAll<Mesh>().FirstOrDefault(m=>m.name=="Nenmir_P09_RepairCandidate")??profile.mesh;
var vertices=mesh.vertices;
Func<Vector3,string> key=v=>string.Format("{0},{1},{2}",Mathf.RoundToInt(v.x*100000),Mathf.RoundToInt(v.y*100000),Mathf.RoundToInt(v.z*100000));
Action<bool,string> check=(condition,message)=>{if(!condition)throw new Exception(message);};
var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics));
var bakedFace=new Mesh();var bakedBody=new Mesh();
try{
var face=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Female_Face_01");
var body=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Female_Body_Chest");
face.sharedMesh=mesh;
var bodyVertices=body.sharedMesh.vertices;
var boundary=Enumerable.Range(0,vertices.Length).Where(i=>i>=vertices.Length-16).ToArray();
var bodyIndices=boundary.Select(i=>Array.FindIndex(bodyVertices,p=>Vector3.Distance(p,vertices[i])<.00001f)).ToArray();
check(bodyIndices.All(i=>i>=0),"P09 몸체와 목 경계 위치 불일치");
var faceEdges=new Dictionary<string,int>();
foreach(int sub in new[]{0,3}){var tris=mesh.GetTriangles(sub);for(int i=0;i<tris.Length;i+=3)for(int j=0;j<3;j++){
var a=key(vertices[tris[i+j]]);var b=key(vertices[tris[i+(j+1)%3]]);var edge=string.CompareOrdinal(a,b)<0?a+"|"+b:b+"|"+a;
if(!faceEdges.ContainsKey(edge))faceEdges[edge]=0;faceEdges[edge]++;
}}
var seamKeys=new HashSet<string>(boundary.Select(i=>key(vertices[i])));
var openNeckEdges=faceEdges.Where(e=>e.Value==1&&e.Key.Split('|').All(seamKeys.Contains)).Count();
check(openNeckEdges==16,"몸체 연결 경계가 닫힌 16변이 아닙니다: "+openNeckEdges);
var unexpected=faceEdges.Where(e=>e.Value==1).Select(e=>e.Key.Split('|')).Count(e=>e.Any(k=>{
var p=vertices.First(v=>key(v)==k);return p.z<1.35f&&p.y>-.015f&&!seamKeys.Contains(k);
}));
check(unexpected==0,"목과 턱 사이에 열린 경계가 있습니다: "+unexpected);
// 닫힌 눈·입술은 표정에서 갈라져야 하므로 수리한 목·귀 뒤·두피 경계만 검사한다.
var duplicates=mesh.GetTriangles(0).Concat(mesh.GetTriangles(3)).Distinct().Where(i=>vertices[i].y>-.015f||vertices[i].z>1.435f).GroupBy(i=>key(vertices[i])).Where(g=>g.Count()>1).Select(g=>g.ToArray()).ToArray();
var head=root.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head);var rotation=head.rotation;
float maxGap=0,maxSplit=0;int bakes=0;
foreach(float pitch in new[]{-45f,0f,45f})foreach(float yaw in new[]{-45f,0f,45f}){
head.rotation=Quaternion.Euler(pitch,yaw,0)*rotation;
for(int shape=-1;shape<mesh.blendShapeCount;shape++){
if(shape>=0)face.SetBlendShapeWeight(shape,100);
face.BakeMesh(bakedFace);body.BakeMesh(bakedBody);var fv=bakedFace.vertices;var bv=bakedBody.vertices;
for(int i=0;i<boundary.Length;i++)maxGap=Mathf.Max(maxGap,Vector3.Distance(face.transform.TransformPoint(fv[boundary[i]]),body.transform.TransformPoint(bv[bodyIndices[i]])));
foreach(var group in duplicates)foreach(int i in group)maxSplit=Mathf.Max(maxSplit,Vector3.Distance(fv[i],fv[group[0]]));
check(fv.All(v=>!float.IsNaN(v.x)&&!float.IsInfinity(v.x)&&!float.IsNaN(v.y)&&!float.IsInfinity(v.y)&&!float.IsNaN(v.z)&&!float.IsInfinity(v.z)),"유효하지 않은 스키닝 좌표");
if(shape>=0)face.SetBlendShapeWeight(shape,0);bakes++;
}}
check(maxGap<.0001f,"몸체 연결 틈: "+maxGap);
check(maxSplit<.0001f,"UV 분리 경계의 스키닝 틈: "+maxSplit);
var report="목 경계 16변, 예상 밖 열린 목 경계 0. 목 9자세 × 중립/36표정 = "+bakes+"회. 몸체 연결 최대 오차 "+maxGap+"m, UV 경계 최대 오차 "+maxSplit+"m. 정점 "+mesh.vertexCount+".";
System.IO.Directory.CreateDirectory("output/NenmirP09");System.IO.File.WriteAllText("output/NenmirP09/seam_validation.txt",report);return report;
}finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(bakedFace);UnityEngine.Object.DestroyImmediate(bakedBody);}

