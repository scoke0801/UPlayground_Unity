var folder="output/DaonGazeSkinFix";
System.IO.Directory.CreateDirectory(folder);
var meshPath="Assets/08.Models/Characters/DaonNewIdentity/Daon_P09_Head.asset";
var mesh=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(meshPath);
var before=System.IO.File.ReadAllText(folder+"/Backup/Assets__08.Models__Characters__DaonNewIdentity__Daon_P09_Head.asset");
var after=System.IO.File.ReadAllText(meshPath);
System.Action<bool,string> check=(ok,message)=>{if(!ok)throw new System.Exception(message);};
System.Func<string,string,string> section=(text,key)=>System.Text.RegularExpressions.Regex.Match(text,@"(?ms)^  "+key+@":.*?(?=^  [A-Za-z_]|\z)").Value;
foreach(string key in new[]{"m_BindPose","m_IndexBuffer","m_BoneNameHashes","m_RootBoneNameHash"})
    check(section(before,key)==section(after,key),key+" 변경");
var beforeBuffer=System.Text.RegularExpressions.Regex.Match(before,@"_typelessdata: ([0-9a-f]+)").Groups[1].Value;
var afterBuffer=System.Text.RegularExpressions.Regex.Match(after,@"_typelessdata: ([0-9a-f]+)").Groups[1].Value;
check(beforeBuffer.Length==afterBuffer.Length&&beforeBuffer.Length>0,"정점 버퍼 크기 변경");
var affected=new System.Collections.Generic.HashSet<int>(mesh.GetTriangles(3).Concat(mesh.GetTriangles(4)));
// 이 메시의 stream 0은 position/normal/tangent 40바이트, 나머지 stream은 UV와 웨이트다.
check(section(before,"m_VertexData").Split(new[]{"_typelessdata:"},System.StringSplitOptions.None)[0]==section(after,"m_VertexData").Split(new[]{"_typelessdata:"},System.StringSplitOptions.None)[0],"정점 레이아웃 변경");
for(int i=0;i<mesh.vertexCount;i++)
    if(!affected.Contains(i))check(beforeBuffer.Substring(i*80,80)==afterBuffer.Substring(i*80,80),"눈 외 정점 변경: "+i);
check(beforeBuffer.Substring(mesh.vertexCount*80)==afterBuffer.Substring(mesh.vertexCount*80),"UV 또는 웨이트 변경");
var vertices=mesh.vertices;
float maxMirrorError=0;
foreach(int submesh in new[]{3,4})
{
    var ids=mesh.GetTriangles(submesh).Distinct().ToArray();
    var right=ids.Where(i=>vertices[i].x>0).ToArray();
    foreach(int i in ids.Where(i=>vertices[i].x<0))
    {
        var mirrored=vertices[i];mirrored.x=-mirrored.x;
        float error=right.Min(j=>(vertices[j]-mirrored).magnitude);
        maxMirrorError=UnityEngine.Mathf.Max(maxMirrorError,error);
    }
}
check(maxMirrorError<.00001f,"중립 동공 좌우 비대칭: "+maxMirrorError);
var path="Assets/03.Prefabs/Characters/ScenarioResidents/Npc/Npc_Daon/Npc_Daon.prefab";
var root=UnityEditor.PrefabUtility.LoadPrefabContents(path);
var baked=new UnityEngine.Mesh();
try
{
    var skin=root.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true).Single(r=>r.name=="Female_Face_02");
    check(skin.sharedMesh==mesh,"정규 얼굴 연결 실패");
    for(int shape=0;shape<mesh.blendShapeCount;shape++)
    {
        if(shape>0)skin.SetBlendShapeWeight(shape-1,0);
        skin.SetBlendShapeWeight(shape,100);skin.BakeMesh(baked);
        check(baked.vertices.All(v=>!float.IsNaN(v.x)&&!float.IsInfinity(v.x)&&!float.IsNaN(v.y)&&!float.IsInfinity(v.y)&&!float.IsNaN(v.z)&&!float.IsInfinity(v.z)),"표정 비유한 좌표: "+shape);
        check(baked.bounds.size.sqrMagnitude<1f,"표정 정점 폭발: "+shape);
    }
    check(root.GetComponentsInChildren<UnityEngine.Transform>(true).All(t=>UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0),"Missing Script");
    var profile=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/Daon_P09_Face.asset");
    profile.ValidateProfile();
    check(skin.sharedMaterials.SequenceEqual(profile.materials),"정규 얼굴 재질 연결 실패");
    var face=skin.sharedMaterials[0];var body=skin.sharedMaterials[5];
    foreach(string name in new[]{"_LightMinLimit","_LightMaxLimit","_ShadowStrength","_ShadowBorder","_ShadowBlur","_ShadowReceive"})check(face.GetFloat(name)==body.GetFloat(name),"얼굴·몸 조명 불일치: "+name);
    var report="눈/하이라이트 정점: "+affected.Count+"\n눈 외 정점·UV·웨이트·bindpose·삼각형 인덱스 보존\n중립 좌우 최대 대응 오차(m): "+maxMirrorError+"\n표정 BakeMesh 유한 좌표/범위: "+mesh.blendShapeCount+"개 통과\n정규 얼굴·재질·프로파일 연결 통과, Missing Script 0\n얼굴/몸 주요 조명 파라미터 일치\n";
    System.IO.File.WriteAllText(folder+"/mesh_validation.txt",report);return report;
}
finally{UnityEngine.Object.DestroyImmediate(baked);UnityEditor.PrefabUtility.UnloadPrefabContents(root);}
