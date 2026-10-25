// 기존 Mesh GUID·UV·웨이트·본·삼각형을 보존하고 목 조형과 해당 노멀만 갱신한다.
var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/08.Models/Characters/HichiP09/Hichi_P09_Head.asset");
var previous=mesh.vertices;var normals=mesh.normals;var tangents=mesh.tangents;
var names=Enumerable.Range(0,mesh.blendShapeCount).Select(mesh.GetBlendShapeName).ToArray();
var previousNormals=new List<Vector3[]>();var previousTangents=new List<Vector3[]>();
for(int s=0;s<names.Length;s++){var dn=new Vector3[mesh.vertexCount];var dt=new Vector3[mesh.vertexCount];mesh.GetBlendShapeFrameVertices(s,0,new Vector3[mesh.vertexCount],dn,dt);previousNormals.Add(dn);previousTangents.Add(dt);}
Vector3[] vertices;var deltas=new List<Vector3[]>();
using(var reader=new System.IO.BinaryReader(System.IO.File.OpenRead("output/HichiNeckFix/neck_contour.bin"))){
    int count=reader.ReadInt32(),shapes=reader.ReadInt32();if(count!=mesh.vertexCount||shapes!=names.Length)throw new Exception("목 조형 계약 불일치");
    Func<Vector3[]> read=()=>{var array=new Vector3[count];for(int i=0;i<count;i++)array[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());return array;};
    vertices=read();for(int s=0;s<shapes;s++)deltas.Add(read());if(reader.BaseStream.Position!=reader.BaseStream.Length)throw new Exception("교환 데이터 길이 오류");
}
var changed=Enumerable.Range(0,vertices.Length).Where(i=>(vertices[i]-previous[i]).sqrMagnitude>1e-14f).ToArray();
if(changed.Length==0)return "목 조형이 이미 반영되어 있습니다.";
var groups=Enumerable.Range(0,vertices.Length).GroupBy(i=>new Vector3Int(Mathf.RoundToInt(vertices[i].x*1000000),Mathf.RoundToInt(vertices[i].y*1000000),Mathf.RoundToInt(vertices[i].z*1000000))).Select(g=>g.ToArray()).ToArray();
Action<Vector3[]> smooth=ns=>{foreach(var group in groups){if(group.Length<2)continue;var sum=Vector3.zero;foreach(int i in group)sum+=ns[i];sum.Normalize();foreach(int i in group)ns[i]=sum;}};
var scratch=UnityEngine.Object.Instantiate(mesh);
try{
    scratch.ClearBlendShapes();scratch.vertices=vertices;scratch.RecalculateNormals();scratch.RecalculateTangents();var newNormals=scratch.normals;smooth(newNormals);var newTangents=scratch.tangents;
    foreach(int i in changed){normals[i]=newNormals[i];tangents[i]=newTangents[i];}
    mesh.ClearBlendShapes();mesh.vertices=vertices;mesh.normals=normals;mesh.tangents=tangents;
    for(int s=0;s<names.Length;s++){
        scratch.vertices=vertices.Select((v,i)=>v+deltas[s][i]).ToArray();scratch.RecalculateNormals();scratch.RecalculateTangents();var ns=scratch.normals;smooth(ns);var ts=scratch.tangents;
        var dn=previousNormals[s];var dt=previousTangents[s];foreach(int i in changed){dn[i]=ns[i]-newNormals[i];dt[i]=new Vector3(ts[i].x-newTangents[i].x,ts[i].y-newTangents[i].y,ts[i].z-newTangents[i].z);}
        mesh.AddBlendShapeFrame(names[s],100,deltas[s],dn,dt);
    }
    mesh.RecalculateBounds();mesh.UploadMeshData(false);EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);
    System.IO.File.WriteAllText("output/HichiNeckFix/changed_indices.txt",string.Join(",",changed.Select(i=>i.ToString()).ToArray()));
    return new{changedVertices=changed.Length,guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(mesh)),vertices=mesh.vertexCount,shapes=mesh.blendShapeCount};
}finally{UnityEngine.Object.DestroyImmediate(scratch);}
