// 축소 표정에는 동공 확대가 아닌 실제 원본의 eye_small 변형을 사용한다.
var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/08.Models/Characters/HichiP09/Hichi_P09_Head.asset");
var delta=new Vector3[mesh.vertexCount];bool found=false;
using(var reader=new System.IO.BinaryReader(System.IO.File.OpenRead("ArtSource/ExternalCharacterBuilder/HichiFace/sculpt.bin"))){
    int count=reader.ReadInt32(),frames=reader.ReadInt32();var basis=new Vector3[count];
    for(int i=0;i<count;i++)basis[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
    for(int f=0;f<frames;f++){
        int length=reader.ReadInt32();string name=System.Text.Encoding.UTF8.GetString(reader.ReadBytes(length));reader.ReadSingle();
        if(name!="eye_small"){reader.BaseStream.Seek(count*12,System.IO.SeekOrigin.Current);continue;}
        for(int i=0;i<count;i++)delta[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle())-basis[i];found=true;break;
    }
}
if(!found)throw new Exception("원본 축소 표정 없음");
var names=new List<string>();var dv=new List<Vector3[]>();var dn=new List<Vector3[]>();var dt=new List<Vector3[]>();
for(int i=0;i<mesh.blendShapeCount;i++){names.Add(mesh.GetBlendShapeName(i));var v=new Vector3[mesh.vertexCount];var n=new Vector3[mesh.vertexCount];var t=new Vector3[mesh.vertexCount];mesh.GetBlendShapeFrameVertices(i,0,v,n,t);dv.Add(v);dn.Add(n);dt.Add(t);}
int target=names.IndexOf("Eye_ShrinkPupil");if(target<0)throw new Exception("P09 표정 없음");
dv[target]=delta;var scratch=UnityEngine.Object.Instantiate(mesh);var vertices=mesh.vertices;
try{scratch.vertices=vertices;scratch.RecalculateNormals();scratch.RecalculateTangents();var bn=scratch.normals;var bt=scratch.tangents;scratch.vertices=vertices.Select((v,i)=>v+delta[i]).ToArray();scratch.RecalculateNormals();scratch.RecalculateTangents();dn[target]=scratch.normals.Select((n,i)=>n-bn[i]).ToArray();dt[target]=scratch.tangents.Select((t,i)=>new Vector3(t.x-bt[i].x,t.y-bt[i].y,t.z-bt[i].z)).ToArray();}finally{UnityEngine.Object.DestroyImmediate(scratch);}
mesh.ClearBlendShapes();for(int i=0;i<names.Count;i++)mesh.AddBlendShapeFrame(names[i],100,dv[i],dn[i],dt[i]);EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);
return "Eye_ShrinkPupil = eye_small";
