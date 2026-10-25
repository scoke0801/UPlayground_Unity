// 얼굴 아틀라스 경계를 목까지 연장하고 같은 서브메시로 렌더링해 색상·외곽선 단절을 없앤다.
var path="Assets/08.Models/Characters/DaonNewIdentity/Daon_P09_Head.asset";
var mesh=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(path);
if(mesh.vertexCount==7107 && mesh.GetTriangles(5).Length==0) return "이미 얼굴과 목의 표면이 연결되었습니다.";
if(mesh.vertexCount!=7095 || mesh.GetTriangles(5).Length!=336) throw new System.Exception("목 연결면 토폴로지 불일치");
int[] top={1,2182,2186,2193,2198,2205,933,931,471,469,468,459,455,452,447,445,254,247,241,235,231,229,228,224,218,215,30,23,17,12,5,2};
int[] cuts={0,6,16,26,32};
var sourceVertices=mesh.vertices;var sourceUv=mesh.uv;
var faceTriangles=mesh.GetTriangles(0);
var edges=new int[32][];
for(int edge=0;edge<32;edge++)
{
    var a=sourceVertices[top[edge]];var b=sourceVertices[top[(edge+1)%32]];
    for(int t=0;t<faceTriangles.Length;t+=3)
    {
        int[] ids={faceTriangles[t],faceTriangles[t+1],faceTriangles[t+2]};
        int ia=System.Array.FindIndex(ids,i=>(sourceVertices[i]-a).sqrMagnitude<1e-12f);
        int ib=System.Array.FindIndex(ids,i=>(sourceVertices[i]-b).sqrMagnitude<1e-12f);
        if(ia<0 || ib<0)continue;
        edges[edge]=new[]{ids[ia],ids[ib],ids[3-ia-ib]};break;
    }
    if(edges[edge]==null)throw new System.Exception("얼굴 경계 삼각형 누락: "+edge);
}
var remap=Enumerable.Range(0,mesh.vertexCount).ToList();
var uv=sourceUv.ToList();
var slots=new System.Collections.Generic.Dictionary<string,int>();
var assigned=new System.Collections.Generic.HashSet<int>();
var neckTriangles=mesh.GetTriangles(5);
System.Func<int,int> column=i=>i<7031?(i-7015)*2:i<7063?i-7031:i-7063;
for(int t=0;t<neckTriangles.Length;t+=3)
{
    int[] ids={neckTriangles[t],neckTriangles[t+1],neckTriangles[t+2]};
    int[] columns=ids.Select(column).ToArray();
    if(columns.Max()>26)for(int i=0;i<3;i++)if(columns[i]==0)columns[i]=32;
    double center=columns.Average();
    int sector=center<6?0:center<16?1:center<26?2:3;
    for(int corner=0;corner<3;corner++)
    {
        int original=ids[corner],angle=columns[corner];
        string key=original+":"+sector;
        int index;
        if(!slots.TryGetValue(key,out index))
        {
            if(assigned.Add(original)) index=original;
            else {index=remap.Count;remap.Add(original);uv.Add(UnityEngine.Vector2.zero);}
            slots.Add(key,index);
            int edge=angle==cuts[sector+1]?angle-1:angle;
            int faceIndex=angle==cuts[sector+1]?edges[edge][1]:edges[edge][0];
            var inward=(sourceUv[edges[edge][2]]-(sourceUv[edges[edge][0]]+sourceUv[edges[edge][1]])*.5f).normalized;
            float depth=original<7031?1:original<7063?.5f:0;
            uv[index]=sourceUv[faceIndex]+inward*(.005f*depth);
        }
        neckTriangles[t+corner]=index;
    }
}
if(remap.Count!=7107)throw new System.Exception("예상한 4개 UV 경계의 복제 정점 수와 다릅니다: "+remap.Count);
var originalNormals=mesh.normals;var originalTangents=mesh.tangents;var originalWeights=mesh.boneWeights;
var vertices=remap.Select(i=>sourceVertices[i]).ToArray();
var normals=remap.Select(i=>originalNormals[i]).ToArray();
var tangents=remap.Select(i=>originalTangents[i]).ToArray();
var weights=remap.Select(i=>originalWeights[i]).ToArray();
var repaired=UnityEngine.Object.Instantiate(mesh);
try
{
    repaired.Clear();repaired.name=mesh.name;
    repaired.vertices=vertices;repaired.normals=normals;repaired.tangents=tangents;
    repaired.uv=uv.ToArray();repaired.boneWeights=weights;repaired.bindposes=mesh.bindposes;
    // 기존 프리팹·프로파일 슬롯을 보존한다. 비워 둔 목 슬롯은 렌더링하지 않는다.
    repaired.subMeshCount=6;
    repaired.SetTriangles(faceTriangles.Concat(neckTriangles).ToArray(),0);
    for(int sub=1;sub<5;sub++)repaired.SetTriangles(mesh.GetTriangles(sub),sub);
    repaired.SetTriangles(new int[0],5);
    for(int shape=0;shape<mesh.blendShapeCount;shape++)
    for(int frame=0;frame<mesh.GetBlendShapeFrameCount(shape);frame++)
    {
        var delta=new UnityEngine.Vector3[mesh.vertexCount];var normal=new UnityEngine.Vector3[mesh.vertexCount];var tangent=new UnityEngine.Vector3[mesh.vertexCount];
        mesh.GetBlendShapeFrameVertices(shape,frame,delta,normal,tangent);
        repaired.AddBlendShapeFrame(mesh.GetBlendShapeName(shape),mesh.GetBlendShapeFrameWeight(shape,frame),remap.Select(i=>delta[i]).ToArray(),remap.Select(i=>normal[i]).ToArray(),remap.Select(i=>tangent[i]).ToArray());
    }
    repaired.RecalculateBounds();
    UnityEditor.Undo.RecordObject(mesh,"다온 얼굴·목 피부 연결");
    UnityEditor.EditorUtility.CopySerialized(repaired,mesh);
    mesh.vertices=vertices;mesh.normals=normals;mesh.tangents=tangents;mesh.boneWeights=weights;
    mesh.UploadMeshData(false);
    UnityEditor.EditorUtility.SetDirty(mesh);UnityEditor.AssetDatabase.SaveAssetIfDirty(mesh);
    System.IO.Directory.CreateDirectory("output/DaonSkinSeamFix");
    System.IO.File.WriteAllText("output/DaonSkinSeamFix/vertex_remap.txt",string.Join(",",remap.Select(i=>i.ToString()).ToArray()));
    return new{vertices=mesh.vertexCount,uvSeamCopies=remap.Count-7095,shapes=mesh.blendShapeCount,neckTriangles=neckTriangles.Length/3,unusedMaterialSlots=1};
}
finally{UnityEngine.Object.DestroyImmediate(repaired);}
