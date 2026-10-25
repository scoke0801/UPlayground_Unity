// 실제 Hichi 목 경계 22점과 P09 몸통 경계 16점을 연결한다. 임계 높이로 자른 턱 조각은 사용하지 않는다.
var profile=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/Hichi_P09_Face.asset");
var mesh=profile.mesh;const int faceCount=9774;
if(mesh.vertexCount==faceCount+114)return "목 연결면 적용됨";
var native=AssetDatabase.LoadAssetAtPath<GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics).GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Female_Face_01").sharedMesh;
int[] top={0,2,8,33,44,52,910,1009,1019,1023,1133,1127,1113,1105,1057,1052,1033,1035,1043,112,111,17};
int[] bottom={4,6,9,11,14,15,18,21,23,51,47,44,39,35,32,29};
var sv=mesh.vertices;var sn=mesh.normals;var st=mesh.tangents;var su=mesh.uv;var sw=mesh.boneWeights;
var nv=native.vertices;var nn=native.normals;var nt=native.tangents;var nw=native.boneWeights;
Func<Vector3,float> angle=p=>Mathf.Atan2(p.y-.015f,p.x);
top=top.OrderBy(i=>angle(sv[i])).ToArray();bottom=bottom.OrderBy(i=>angle(nv[i])).ToArray();
var vertices=sv.Take(faceCount).ToList();var normals=sn.Take(faceCount).ToList();var tangents=st.Take(faceCount).ToList();var uv=su.Take(faceCount).ToList();var weights=sw.Take(faceCount).ToList();
var origins=Enumerable.Range(0,faceCount).ToList();var triangles=new List<int>();
// 각 삼각형은 별도 UV 정점을 사용해 얼굴 아틀라스 좌우 경계를 가로질러 보간하지 않는다.
Action<int[],bool[]> add=(ids,isTop)=>
{
    var positions=ids.Select((id,k)=>isTop[k]?sv[id]:nv[id]).ToArray();
    var center=(positions[0]+positions[1]+positions[2])/3;
    var cross=Vector3.Cross(positions[1]-positions[0],positions[2]-positions[0]);
    if(Vector3.Dot(cross,new Vector3(center.x,center.y-.015f,0))<0){int id=ids[1];ids[1]=ids[2];ids[2]=id;bool up=isTop[1];isTop[1]=isTop[2];isTop[2]=up;}
    int near=top.OrderBy(i=>(sv[i]-center).sqrMagnitude).First();
    for(int k=0;k<3;k++)
    {
        int id=ids[k];bool upper=isTop[k];triangles.Add(vertices.Count);
        vertices.Add(upper?sv[id]:nv[id]);normals.Add(upper?sn[id]:nn[id]);tangents.Add(upper?st[id]:nt[id]);weights.Add(upper?sw[id]:nw[id]);
        origins.Add(upper?id:-1);uv.Add(upper?su[id]:new Vector2(su[near].x,.008f));
    }
};
int a=0,b=0;
while(a<top.Length||b<bottom.Length)
{
    float nextTop=a+1<top.Length?angle(sv[top[a+1]]):angle(sv[top[0]])+2*Mathf.PI;
    float nextBottom=b+1<bottom.Length?angle(nv[bottom[b+1]]):angle(nv[bottom[0]])+2*Mathf.PI;
    if(a<top.Length&&(b>=bottom.Length||nextTop<nextBottom)){add(new[]{top[a%top.Length],top[(a+1)%top.Length],bottom[b%bottom.Length]},new[]{true,true,false});a++;}
    else{add(new[]{top[a%top.Length],bottom[b%bottom.Length],bottom[(b+1)%bottom.Length]},new[]{true,false,false});b++;}
}
var names=new List<string>();var deltas=new List<Vector3[]>();
for(int s=0;s<mesh.blendShapeCount;s++){var delta=new Vector3[mesh.vertexCount];mesh.GetBlendShapeFrameVertices(s,0,delta,null,null);names.Add(mesh.GetBlendShapeName(s));deltas.Add(origins.Select(i=>i>=0?delta[i]:Vector3.zero).ToArray());}
var faceTriangles=mesh.GetTriangles(0);var overlayTriangles=mesh.GetTriangles(1);var bindposes=mesh.bindposes;
mesh.ClearBlendShapes();mesh.Clear();mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetTangents(tangents);mesh.SetUVs(0,uv);mesh.boneWeights=weights.ToArray();mesh.bindposes=bindposes;
mesh.subMeshCount=3;mesh.SetTriangles(faceTriangles,0);mesh.SetTriangles(overlayTriangles,1);mesh.SetTriangles(triangles,2);
var scratch=UnityEngine.Object.Instantiate(mesh);
try{
    for(int s=0;s<names.Count;s++){
        scratch.vertices=vertices.Select((v,i)=>v+deltas[s][i]).ToArray();scratch.RecalculateNormals();scratch.RecalculateTangents();
        var shapeN=scratch.normals;var shapeT=scratch.tangents;
        // 조형 표정의 노멀 변화만 더하고 중립의 매끈한 경계 노멀은 보존한다.
        scratch.vertices=vertices.ToArray();scratch.RecalculateNormals();scratch.RecalculateTangents();var baseN=scratch.normals;var baseT=scratch.tangents;
        mesh.AddBlendShapeFrame(names[s],100,deltas[s],shapeN.Select((v,i)=>v-baseN[i]).ToArray(),shapeT.Select((v,i)=>new Vector3(v.x-baseT[i].x,v.y-baseT[i].y,v.z-baseT[i].z)).ToArray());
    }
}finally{UnityEngine.Object.DestroyImmediate(scratch);}
mesh.RecalculateBounds();mesh.UploadMeshData(false);profile.materials[2]=profile.materials[0];
profile.materials[0].SetFloat("_OutlineWidth",0);profile.materials[1].SetFloat("_OutlineWidth",0);
foreach(var asset in new UnityEngine.Object[]{mesh,profile,profile.materials[0],profile.materials[1]}){EditorUtility.SetDirty(asset);AssetDatabase.SaveAssetIfDirty(asset);}
System.IO.File.WriteAllText("output/HichiP09/neck_origins.txt",string.Join(",",origins.Select(i=>i.ToString()).ToArray()));
return new{vertices=mesh.vertexCount,neckTriangles=triangles.Count/3,top=top.Length,bottom=bottom.Length};
