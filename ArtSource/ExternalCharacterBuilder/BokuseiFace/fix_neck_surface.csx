var folder="Assets/08.Models/Characters/BokuseiP09";
var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(folder+"/Bokusei_P09_Head.asset");
var source=AssetDatabase.LoadAssetAtPath<GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabMaleNoPhysics);
var native=source.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Male_Face_01").sharedMesh;
var body=source.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Male_Armor_003_Chest").sharedMesh;
var vertices=mesh.vertices;var oldNormals=mesh.normals;var normals=mesh.normals;var weights=mesh.boneWeights;
var nativeVertices=native.vertices;var bodyVertices=body.vertices;var bodyNormals=body.normals;
var rows=System.IO.File.ReadAllLines("ArtSource/ExternalCharacterBuilder/BokuseiFace/neck_bindings.txt").Select(l=>l.Split(',')).ToArray();
var targets=new Dictionary<int,int>();var seamPositions=new Dictionary<int,Vector3>();var seamNormals=new Dictionary<int,Vector3>();
var audit=new List<string>();
foreach(var row in rows){
int index=int.Parse(row[0]),a=int.Parse(row[1]),b=int.Parse(row[2]);float t=float.Parse(row[3],System.Globalization.CultureInfo.InvariantCulture);int chosen=t<.5f?a:b;
var p=nativeVertices[chosen];int bodyIndex=Enumerable.Range(0,bodyVertices.Length).OrderBy(j=>(bodyVertices[j]-p).sqrMagnitude).First();
if(Vector3.Distance(bodyVertices[bodyIndex],p)>.00002f)throw new Exception("몸 목 경계 대응 없음");
targets.Add(index,chosen);seamPositions.Add(index,bodyVertices[bodyIndex]);seamNormals.Add(index,bodyNormals[bodyIndex]);
}
if(targets.Values.Distinct().Count()!=16)throw new Exception("P09 목 경계 꼭짓점 누락");
var boundary=targets.Keys.ToArray();
// 적용된 결과에 재실행해도 주변 정점의 보간을 반복하지 않는다.
if(boundary.All(i=>Vector3.Distance(vertices[i],seamPositions[i])<.0000001f&&Vector3.Angle(normals[i],seamNormals[i])<.01f))return "목 경계 보정이 이미 적용되어 있습니다.";
var originalPositions=vertices.ToArray();
foreach(int index in mesh.GetTriangles(0).Distinct()){
int closest=boundary.OrderBy(j=>(originalPositions[j]-originalPositions[index]).sqrMagnitude).First();
float distance=Vector3.Distance(originalPositions[index],originalPositions[closest]);
float blend=1-Mathf.SmoothStep(0,1,Mathf.Clamp01(distance/.022f));
vertices[index]+=(seamPositions[closest]-originalPositions[closest])*blend;
normals[index]=Vector3.Slerp(oldNormals[index],seamNormals[closest],blend).normalized;
}
foreach(int index in boundary){vertices[index]=seamPositions[index];normals[index]=seamNormals[index];weights[index]=native.boneWeights[targets[index]];}
var shapeNames=new List<string>();var deltas=new List<Vector3[]>();var normalDeltas=new List<Vector3[]>();var tangentDeltas=new List<Vector3[]>();
for(int s=0;s<mesh.blendShapeCount;s++){
var dv=new Vector3[mesh.vertexCount];var dn=new Vector3[mesh.vertexCount];var dt=new Vector3[mesh.vertexCount];mesh.GetBlendShapeFrameVertices(s,0,dv,dn,dt);
for(int i=0;i<dv.Length;i++)if(Vector3.Angle(oldNormals[i],normals[i])>.001f){var rotation=Quaternion.FromToRotation(oldNormals[i],normals[i]);dn[i]=rotation*(oldNormals[i]+dn[i])-normals[i];dt[i]=rotation*dt[i];}
foreach(int i in boundary){dv[i]=Vector3.zero;dn[i]=Vector3.zero;dt[i]=Vector3.zero;}
shapeNames.Add(mesh.GetBlendShapeName(s));deltas.Add(dv);normalDeltas.Add(dn);tangentDeltas.Add(dt);
}
mesh.ClearBlendShapes();mesh.vertices=vertices;mesh.normals=normals;mesh.boneWeights=weights;mesh.RecalculateTangents();
for(int s=0;s<shapeNames.Count;s++)mesh.AddBlendShapeFrame(shapeNames[s],100,deltas[s],normalDeltas[s],tangentDeltas[s]);
mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);
audit.Add("37 경계 정점을 P09의 실제 16 경계 꼭짓점으로 연결. 위치·웨이트·노멀 일치.");
audit.Add("경계 인근 22mm에서 위치·노멀 보간, 표정 경계 델타 0.");
System.IO.File.WriteAllLines("output/BokuseiNeckFix/surface_result.txt",audit);
return audit;
