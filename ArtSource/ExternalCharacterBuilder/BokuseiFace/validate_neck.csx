var profile=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/Bokusei_P09_Face.asset");
var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabMaleNoPhysics));
var face=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Male_Face_01");var native=face.sharedMesh;
var rows=System.IO.File.ReadAllLines("ArtSource/ExternalCharacterBuilder/BokuseiFace/neck_bindings.txt").Select(l=>l.Split(',')).ToArray();var report=new List<string>();
var sourceBake=new Mesh();var targetBake=new Mesh();
try{
var head=face.bones.Single(b=>b.name=="Head");var neutral=head.localRotation;float max=0;
foreach(float yaw in new[]{-35f,0f,35f}){
head.localRotation=neutral*Quaternion.AngleAxis(yaw,Vector3.forward);
face.sharedMesh=native;face.BakeMesh(targetBake);face.sharedMesh=profile.mesh;face.BakeMesh(sourceBake);
var sv=sourceBake.vertices;var tv=targetBake.vertices;
foreach(var row in rows){int index=int.Parse(row[0]),a=int.Parse(row[1]),b=int.Parse(row[2]);float t=float.Parse(row[3],System.Globalization.CultureInfo.InvariantCulture);max=Mathf.Max(max,Vector3.Distance(sv[index],tv[t<.5f?a:b]));}
}
if(max>.00001f)throw new Exception("목 경계 변형 오차: "+max);
for(int s=0;s<profile.mesh.blendShapeCount;s++){
var dv=new Vector3[profile.mesh.vertexCount];profile.mesh.GetBlendShapeFrameVertices(s,0,dv,null,null);
if(!dv.Any(v=>v.sqrMagnitude>.00000001f))throw new Exception("실제 변형 없는 표정: "+profile.mesh.GetBlendShapeName(s));
if(rows.Any(r=>dv[int.Parse(r[0])].sqrMagnitude>.0000000001f))throw new Exception("표정에서 목 경계 이동");
}
report.Add("목 좌우 35도·중립: P09 목 경계 꼭짓점 대비 최대 오차 "+(max*1000).ToString("F4")+" mm, 허용 0.01 mm");
report.Add("P09 36표정 모두 실제 델타 존재, 목 경계 표정 이동 0. 표정 의미·입안의 최종 미술 승인은 별도.");
System.IO.File.WriteAllLines("output/BokuseiP09/neck_validation.txt",report);return report;
}finally{UnityEngine.Object.DestroyImmediate(sourceBake);UnityEngine.Object.DestroyImmediate(targetBake);UnityEngine.Object.DestroyImmediate(root);}

