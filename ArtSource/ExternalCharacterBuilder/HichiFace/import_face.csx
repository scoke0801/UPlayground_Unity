// Blender 교환 데이터로 새 얼굴을 만들고 실제 P09 본 배열과 목 웨이트를 보존한다.
var folder="Assets/08.Models/Characters/HichiP09";
var meshPath=folder+"/Hichi_P09_Head.asset";
var profilePath="Assets/10.Datas/Actor/ExternalModels/Hichi_P09_Face.asset";
if(UnityEditor.AssetDatabase.LoadMainAssetAtPath(meshPath)!=null || UnityEditor.AssetDatabase.LoadMainAssetAtPath(profilePath)!=null)
    throw new System.Exception("기존 제작 결과가 있습니다. 재생성 대신 명시적인 revision을 사용하세요.");
var srcRoot=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/ExternalAssets/Character/Hichi/Prefab/Hichi.prefab");
var baseRoot=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics);
var src=srcRoot.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true).Single(r=>r.name=="hichi_face");
var dst=baseRoot.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true).Single(r=>r.name=="Female_Face_01");
var native=dst.sharedMesh;var original=src.sharedMesh;
var vertices=new System.Collections.Generic.List<UnityEngine.Vector3>();
var names=new System.Collections.Generic.List<string>();var frames=new System.Collections.Generic.List<UnityEngine.Vector3[]>();
using(var reader=new System.IO.BinaryReader(System.IO.File.OpenRead("ArtSource/ExternalCharacterBuilder/HichiFace/sculpt.bin")))
{
    int count=reader.ReadInt32();int frameCount=reader.ReadInt32();
    if(count!=original.vertexCount)throw new System.Exception("원본 정점 대응 불일치");
    for(int i=0;i<count;i++)vertices.Add(new UnityEngine.Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle()));
    for(int f=0;f<frameCount;f++)
    {
        int length=reader.ReadInt32();var name=System.Text.Encoding.UTF8.GetString(reader.ReadBytes(length));float weight=reader.ReadSingle();
        if(weight!=100)throw new System.Exception("표정 프레임 가중치 변경: "+name);
        var delta=new UnityEngine.Vector3[count];
        for(int i=0;i<count;i++)delta[i]=new UnityEngine.Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle())-vertices[i];
        names.Add(name);frames.Add(delta);
    }
    if(reader.BaseStream.Position!=reader.BaseStream.Length)throw new System.Exception("교환 데이터 길이 불일치");
}
int headIndex=System.Array.FindIndex(dst.bones,b=>b.name=="Head");
if(headIndex<0)throw new System.Exception("P09 Head 본 없음");
var boneMap=new System.Collections.Generic.Dictionary<string,string>{{"Head","Head"},{"LeftEye","eye_L"},{"RightEye","eye_R"}}; System.Func<int,int> remap=i=>{string name=src.bones[i].name;if(!boneMap.ContainsKey(name))throw new System.Exception("가중 본 대응 없음: "+name);return System.Array.FindIndex(dst.bones,b=>b.name==boneMap[name]);}; var weights=original.boneWeights.Select(w=>new UnityEngine.BoneWeight{boneIndex0=w.weight0>0?remap(w.boneIndex0):0,boneIndex1=w.weight1>0?remap(w.boneIndex1):0,boneIndex2=w.weight2>0?remap(w.boneIndex2):0,boneIndex3=w.weight3>0?remap(w.boneIndex3):0,weight0=w.weight0,weight1=w.weight1,weight2=w.weight2,weight3=w.weight3}).ToList();
var uv=original.uv.ToList();var neckTriangles=new System.Collections.Generic.List<int>();var map=new System.Collections.Generic.Dictionary<int,int>();
var nv=native.vertices;var nt=native.GetTriangles(0);var nw=native.boneWeights;var nu=native.uv;
float neckTop=dst.transform.InverseTransformPoint(dst.bones[headIndex].position).z+.030f;
for(int i=0;i<nt.Length;i+=3)
{
    int[] face={nt[i],nt[i+1],nt[i+2]};
    if(!face.All(j=>nv[j].z<neckTop&&nv[j].y>-.037f))continue;
    foreach(int j in face)
    {
        if(!map.ContainsKey(j)){map.Add(j,vertices.Count);var p=nv[j];float b=UnityEngine.Mathf.SmoothStep(0,1,UnityEngine.Mathf.InverseLerp(neckTop-.05f,neckTop-.005f,p.z));p.x*=UnityEngine.Mathf.Lerp(1,.55f,b);p.y=.008f+(p.y-.008f)*UnityEngine.Mathf.Lerp(1,.55f,b);vertices.Add(p);weights.Add(nw[j]);uv.Add(nu[j]);}
        neckTriangles.Add(map[j]);
    }
}
if(map.Count<10)throw new System.Exception("P09 목 연결면을 찾지 못했습니다.");
var mesh=new UnityEngine.Mesh();mesh.name="Hichi_P09_Head";
mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.boneWeights=weights.ToArray();mesh.bindposes=native.bindposes;
mesh.subMeshCount=3;mesh.SetTriangles(original.GetTriangles(0),0);mesh.SetTriangles(original.GetTriangles(1),1);mesh.SetTriangles(neckTriangles,2);
mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
var baseNormals=mesh.normals;var baseTangents=mesh.tangents;
var aliases=new[]{"eye_close","eye_close_L","eye_close_R","eye_joy","eye_Tsukare","eye_jito","eye_tare","eye_angry","eye_open","eye_jito_2","eye_smile","eye_small","eye_pupil_X","brow_up","brow_down","brow_joy","brow_sad","brow_angry","brow_tare","brow_angry_2","brow_sad_2","brow_joy_2","brow_sad","brow_straight","mouth_smile","mouth_ワ","mouth_grin_1","mouth_sad","mouth_A_2","mouth_I","mouth_ぺろ_1","vrc.v_aa","vrc.v_ih","vrc.v_ou","vrc.v_e","vrc.v_oh"};
if(aliases.Length!=native.blendShapeCount)throw new System.Exception("P09 표정 계약 변경");
var shapeNormals=new System.Collections.Generic.List<UnityEngine.Vector3[]>();
var shapeTangents=new System.Collections.Generic.List<UnityEngine.Vector3[]>();
var scratch=UnityEngine.Object.Instantiate(mesh);
try
{
    for(int f=0;f<frames.Count;f++)
    {
        var delta=frames[f];System.Array.Resize(ref delta,vertices.Count);frames[f]=delta;
        scratch.vertices=vertices.Select((v,i)=>v+delta[i]).ToArray();scratch.RecalculateNormals();scratch.RecalculateTangents();
        var normals=scratch.normals;var tangents=scratch.tangents;
        shapeNormals.Add(normals.Select((v,i)=>v-baseNormals[i]).ToArray());
        shapeTangents.Add(tangents.Select((v,i)=>new UnityEngine.Vector3(v.x-baseTangents[i].x,v.y-baseTangents[i].y,v.z-baseTangents[i].z)).ToArray());
    }
}
finally{UnityEngine.Object.DestroyImmediate(scratch);}
for(int f=0;f<aliases.Length;f++)
{
    int index=names.IndexOf(aliases[f]);
    if(index<0)throw new System.Exception("원본 대응 표정 없음: "+aliases[f]);
    mesh.AddBlendShapeFrame(native.GetBlendShapeName(f),100,frames[index],shapeNormals[index],shapeTangents[index]);
}
// 원본 표정은 Blender에 보관하고 런타임에는 P09 소비 표정만 둔다.
UnityEditor.AssetDatabase.ImportAsset(folder+"/Hichi_Face_Albedo.png",UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(folder+"/Hichi_Face_Albedo.png");
importer.sRGBTexture=true;importer.mipmapEnabled=true;importer.maxTextureSize=2048;importer.textureCompression=UnityEditor.TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
var material=new UnityEngine.Material(src.sharedMaterial);material.name="Hichi_P09_Face";
material.mainTexture=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(folder+"/Hichi_Face_Albedo.png");
if(material.HasProperty("_UseMain2ndTex"))material.SetFloat("_UseMain2ndTex",0);
if(material.HasProperty("_UseMain3rdTex"))material.SetFloat("_UseMain3rdTex",0);
UnityEditor.AssetDatabase.CreateAsset(material,folder+"/Hichi_P09_Face.mat");
UnityEditor.AssetDatabase.CreateAsset(mesh,meshPath);
var profile=UnityEngine.ScriptableObject.CreateInstance<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>();
profile.sex=UPlayGround.Editor.P09Builder.BuilderSex.Female;profile.mesh=mesh;
var overlay=new UnityEngine.Material(src.sharedMaterials[1]);overlay.name="Hichi_P09_Overlay";overlay.mainTexture=material.mainTexture;UnityEditor.AssetDatabase.CreateAsset(overlay,folder+"/Hichi_P09_Overlay.mat"); profile.materials=new[]{material,overlay,dst.sharedMaterials[0]};profile.boneNames=dst.bones.Select(b=>b.name).ToArray();
profile.rendererPaths=baseRoot.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true).Where(r=>r.name.StartsWith("Female_Face_")).Select(r=>UnityEditor.AnimationUtility.CalculateTransformPath(r.transform,baseRoot.transform)).ToArray();
profile.ValidateProfile();UnityEditor.AssetDatabase.CreateAsset(profile,profilePath);UnityEditor.AssetDatabase.SaveAssets();
return new{mesh=meshPath,vertices=mesh.vertexCount,neckVertices=map.Count,shapes=mesh.blendShapeCount,bones=profile.boneNames.Length};
