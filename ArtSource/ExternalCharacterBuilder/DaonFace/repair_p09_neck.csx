// 다온의 기존 얼굴 경계와 P09 몸통 경계를 잇는다. 얼굴 조형·본·표정 원본은 유지한다.
var path = "Assets/08.Models/Characters/DaonNewIdentity/Daon_P09_Head.asset";
var mesh = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(path);
const int faceCount = 7015;
const int bottomCount = 16;
const int ringCount = 32;
const int resultCount = faceCount + bottomCount + ringCount * 2;
if (mesh.vertexCount == resultCount && mesh.GetTriangles(5).Length == 336)
    return "이미 목 연결면이 적용되었습니다.";
if (mesh.vertexCount != 7125 || mesh.subMeshCount != 6 || mesh.GetTriangles(5).Min() != faceCount)
    throw new System.Exception("다온 원본 토폴로지가 다릅니다. 경계를 다시 조사해야 합니다.");

int[] top = {1,2182,2186,2193,2198,2205,933,931,471,469,468,459,455,452,447,445,254,247,241,235,231,229,228,224,218,215,30,23,17,12,5,2};
int[] bottom = {7029,7030,7033,7036,7038,7066,7062,7059,7054,7050,7047,7044,7019,7021,7024,7026};
var sourceVertices = mesh.vertices;
var sourceNormals = mesh.normals;
var sourceTangents = mesh.tangents;
var sourceWeights = mesh.boneWeights;
var vertices = new UnityEngine.Vector3[resultCount];
var normals = new UnityEngine.Vector3[resultCount];
var tangents = new UnityEngine.Vector4[resultCount];
var uv = new UnityEngine.Vector2[resultCount];
var weights = new UnityEngine.BoneWeight[resultCount];
System.Array.Copy(sourceVertices, vertices, faceCount);
System.Array.Copy(sourceNormals, normals, faceCount);
System.Array.Copy(sourceTangents, tangents, faceCount);
System.Array.Copy(mesh.uv, uv, faceCount);
System.Array.Copy(sourceWeights, weights, faceCount);

System.Func<UnityEngine.BoneWeight,UnityEngine.BoneWeight,float,UnityEngine.BoneWeight> blendWeights = (a,b,t) =>
{
    var sums = new System.Collections.Generic.Dictionary<int,float>();
    int[] ids = {a.boneIndex0,a.boneIndex1,a.boneIndex2,a.boneIndex3,b.boneIndex0,b.boneIndex1,b.boneIndex2,b.boneIndex3};
    float[] values = {a.weight0*(1-t),a.weight1*(1-t),a.weight2*(1-t),a.weight3*(1-t),b.weight0*t,b.weight1*t,b.weight2*t,b.weight3*t};
    for (int i=0;i<ids.Length;i++) if(values[i]>0) { if(!sums.ContainsKey(ids[i])) sums[ids[i]]=0; sums[ids[i]]+=values[i]; }
    if(sums.Count>4) throw new System.Exception("목 연결면이 4본 가중치 한계를 넘습니다.");
    var entries=sums.OrderByDescending(e=>e.Value).ToArray();
    var result=new UnityEngine.BoneWeight();
    for(int i=0;i<entries.Length;i++)
    {
        if(i==0){result.boneIndex0=entries[i].Key;result.weight0=entries[i].Value;}
        if(i==1){result.boneIndex1=entries[i].Key;result.weight1=entries[i].Value;}
        if(i==2){result.boneIndex2=entries[i].Key;result.weight2=entries[i].Value;}
        if(i==3){result.boneIndex3=entries[i].Key;result.weight3=entries[i].Value;}
    }
    return result;
};
for(int i=0;i<bottomCount;i++)
{
    int index=faceCount+i;
    vertices[index]=sourceVertices[bottom[i]];
    normals[index]=sourceNormals[bottom[i]];
    tangents[index]=sourceTangents[bottom[i]];
    weights[index]=sourceWeights[bottom[i]];
    uv[index]=new UnityEngine.Vector2(.5722725f + vertices[index].x*.8f,.516f);
}
for(int ring=0;ring<2;ring++)
for(int i=0;i<ringCount;i++)
{
    float t=ring==0?.5f:1f;
    int a=bottom[i/2], b=bottom[(i/2+1)%bottomCount];
    float along=(i%2)*.5f;
    int index=faceCount+bottomCount+ring*ringCount+i;
    var lower=UnityEngine.Vector3.Lerp(sourceVertices[a],sourceVertices[b],along);
    vertices[index]=UnityEngine.Vector3.Lerp(lower,sourceVertices[top[i]],t);
    normals[index]=UnityEngine.Vector3.Lerp(UnityEngine.Vector3.Lerp(sourceNormals[a],sourceNormals[b],along),sourceNormals[top[i]],t).normalized;
    tangents[index]=UnityEngine.Vector4.Lerp(UnityEngine.Vector4.Lerp(sourceTangents[a],sourceTangents[b],along),sourceTangents[top[i]],t);
    weights[index]=ring==1?sourceWeights[top[i]]:blendWeights(blendWeights(sourceWeights[a],sourceWeights[b],along),sourceWeights[top[i]],t);
    // 기존 P09 목 아틀라스의 피부 영역을 사용하며 서로 다른 UV 아일랜드를 가로질러 보간하지 않는다.
    uv[index]=new UnityEngine.Vector2(.5722725f+vertices[index].x*.8f,.516f-t*.011f);
}
var neckTriangles=new System.Collections.Generic.List<int>();
System.Action<int,int,int> addTriangle=(a,b,c)=>
{
    var cross=UnityEngine.Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]);
    var center=(vertices[a]+vertices[b]+vertices[c])/3f;
    var outward=new UnityEngine.Vector3(center.x,center.y-.018f,0);
    if(UnityEngine.Vector3.Dot(cross,outward)<0) {int swap=b;b=c;c=swap;}
    if(cross.sqrMagnitude<1e-14f) throw new System.Exception("목 연결면의 삼각형 면적이 0입니다.");
    neckTriangles.AddRange(new[]{a,b,c});
};
int middle=faceCount+bottomCount, upper=middle+ringCount;
for(int i=0;i<bottomCount;i++)
{
    int a=faceCount+i,b=faceCount+(i+1)%bottomCount;
    int c=middle+i*2,d=middle+(i*2+1)%ringCount,e=middle+(i*2+2)%ringCount;
    addTriangle(a,c,d);addTriangle(a,d,b);addTriangle(b,d,e);
}
for(int i=0;i<ringCount;i++)
{
    int next=(i+1)%ringCount;
    addTriangle(middle+i,upper+i,upper+next);
    addTriangle(middle+i,upper+next,middle+next);
}
var repaired=UnityEngine.Object.Instantiate(mesh);
try
{
    // 이전 목 정점 인덱스가 새 버퍼 크기 밖에 남지 않도록 복제 메시에서 먼저 비운다.
    repaired.Clear();
    repaired.name=mesh.name;
    repaired.vertices=vertices;repaired.normals=normals;repaired.tangents=tangents;
    repaired.uv=uv;repaired.boneWeights=weights;repaired.bindposes=mesh.bindposes;
    repaired.subMeshCount=6;
    for(int sub=0;sub<5;sub++) repaired.SetTriangles(mesh.GetTriangles(sub),sub);
    repaired.SetTriangles(neckTriangles,5);
    for(int shape=0;shape<mesh.blendShapeCount;shape++)
    for(int frame=0;frame<mesh.GetBlendShapeFrameCount(shape);frame++)
    {
        var sourceDelta=new UnityEngine.Vector3[mesh.vertexCount];
        var sourceNormal=new UnityEngine.Vector3[mesh.vertexCount];
        var sourceTangent=new UnityEngine.Vector3[mesh.vertexCount];
        mesh.GetBlendShapeFrameVertices(shape,frame,sourceDelta,sourceNormal,sourceTangent);
        var delta=new UnityEngine.Vector3[resultCount];
        var normalDelta=new UnityEngine.Vector3[resultCount];
        var tangentDelta=new UnityEngine.Vector3[resultCount];
        System.Array.Copy(sourceDelta,delta,faceCount);
        System.Array.Copy(sourceNormal,normalDelta,faceCount);
        System.Array.Copy(sourceTangent,tangentDelta,faceCount);
        for(int i=0;i<ringCount;i++)
        {
            // 경계 위치뿐 아니라 표정 델타와 스킨 가중치도 같아야 고개 회전·발음에서 벌어지지 않는다.
            delta[middle+i]=sourceDelta[top[i]]*.5f;delta[upper+i]=sourceDelta[top[i]];
            normalDelta[middle+i]=sourceNormal[top[i]]*.5f;normalDelta[upper+i]=sourceNormal[top[i]];
            tangentDelta[middle+i]=sourceTangent[top[i]]*.5f;tangentDelta[upper+i]=sourceTangent[top[i]];
        }
        repaired.AddBlendShapeFrame(mesh.GetBlendShapeName(shape),mesh.GetBlendShapeFrameWeight(shape,frame),delta,normalDelta,tangentDelta);
    }
    repaired.RecalculateBounds();
    if(repaired.blendShapeCount!=mesh.blendShapeCount || repaired.vertexCount!=resultCount)
        throw new System.Exception("목 연결면의 표정·정점 검증 실패");
    for(int i=0;i<faceCount;i++)
        if(vertices[i]!=sourceVertices[i]) throw new System.Exception("얼굴 조형 변경 감지");
    UnityEditor.Undo.RecordObject(mesh,"다온 목 연결면 복구");
    UnityEditor.EditorUtility.CopySerialized(repaired,mesh);
    // CopySerialized만으로는 기존 GPU 스킨 버퍼의 정점 수가 갱신되지 않는다.
    mesh.vertices=vertices;mesh.normals=normals;mesh.tangents=tangents;mesh.boneWeights=weights;
    mesh.UploadMeshData(false);
    UnityEditor.EditorUtility.SetDirty(mesh);
    UnityEditor.AssetDatabase.SaveAssetIfDirty(mesh);
    return new {vertices=mesh.vertexCount,neckTriangles=neckTriangles.Count/3,shapes=mesh.blendShapeCount};
}
finally {UnityEngine.Object.DestroyImmediate(repaired);}
