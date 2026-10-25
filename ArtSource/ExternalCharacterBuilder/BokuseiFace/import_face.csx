var folder="Assets/08.Models/Characters/BokuseiP09";
var meshPath=folder+"/Bokusei_P09_Head.asset";
var profilePath="Assets/10.Datas/Actor/ExternalModels/Bokusei_P09_Face.asset";
if(AssetDatabase.LoadMainAssetAtPath(meshPath)!=null||AssetDatabase.LoadMainAssetAtPath(profilePath)!=null)throw new Exception("기존 제작본을 보존합니다.");
AssetDatabase.Refresh();
var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalAssets/Character/ROKO SHOP/Bokusei/01_Prefab/Bokusei.prefab");
var root=AssetDatabase.LoadAssetAtPath<GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabMaleNoPhysics);
var skin=source.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Body");
var target=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Male_Face_01");
var original=skin.sharedMesh;var native=target.sharedMesh;
var vertices=new List<Vector3>();var deltas=new List<Vector3[]>();
using(var reader=new System.IO.BinaryReader(System.IO.File.OpenRead("ArtSource/ExternalCharacterBuilder/BokuseiFace/sculpt.bin"))){
int count=reader.ReadInt32(),shapes=reader.ReadInt32();
if(count!=original.vertexCount||shapes!=original.blendShapeCount)throw new Exception("조형 입력 계약 불일치");
Func<Vector3[]> read=()=>{var a=new Vector3[count];for(int i=0;i<count;i++)a[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());return a;};
vertices.AddRange(read());for(int s=0;s<shapes;s++)deltas.Add(read());
if(reader.BaseStream.Position!=reader.BaseStream.Length)throw new Exception("교환 데이터 길이 불일치");
}
var boneMap=new Dictionary<string,string>{{"Head","Head"},{"Neck","Neck"},{"UpperChest","UpperChest"},{"Shoulder_L","shoulder_L"},{"Shoulder_R","shoulder_R"},{"SK_L_Eye","eye_L"},{"SK_R_Eye","eye_R"}};
Func<int,int> mapBone=i=>{string name=skin.bones[i].name;if(!boneMap.ContainsKey(name))throw new Exception("알 수 없는 가중 본: "+name);int index=Array.FindIndex(target.bones,b=>b.name==boneMap[name]);if(index<0)throw new Exception("대상 본 없음");return index;};
var weights=original.boneWeights.Select(w=>new BoneWeight{boneIndex0=w.weight0>0?mapBone(w.boneIndex0):0,boneIndex1=w.weight1>0?mapBone(w.boneIndex1):0,boneIndex2=w.weight2>0?mapBone(w.boneIndex2):0,boneIndex3=w.weight3>0?mapBone(w.boneIndex3):0,weight0=w.weight0,weight1=w.weight1,weight2=w.weight2,weight3=w.weight3}).ToList();
var uv=original.uv.ToList();var neck=new List<int>();var neckMap=new Dictionary<int,int>();
var nv=native.vertices;var nt=native.GetTriangles(0);var nw=native.boneWeights;var nu=native.uv;
float headHeight=target.transform.InverseTransformPoint(target.bones.Single(b=>b.name=="Head").position).z;
for(int i=0;i<nt.Length;i+=3){
var tri=new[]{nt[i],nt[i+1],nt[i+2]};
if(!tri.All(j=>nv[j].z<headHeight+.030f&&nv[j].y>-.037f))continue;
foreach(int j in tri){if(!neckMap.ContainsKey(j)){
var p=nv[j];float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(headHeight-.02f,headHeight+.025f,p.z));
p.x*=Mathf.Lerp(1,.55f,blend);p.y=.008f+(p.y-.008f)*Mathf.Lerp(1,.55f,blend);
neckMap.Add(j,vertices.Count);vertices.Add(p);uv.Add(nu[j]);weights.Add(nw[j]);}
neck.Add(neckMap[j]);}
}
if(neckMap.Count<10)throw new Exception("P09 목 연결면 없음");
var mesh=new Mesh();mesh.name="Bokusei_P09_Head";mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.boneWeights=weights.ToArray();mesh.bindposes=native.bindposes;
mesh.subMeshCount=4;for(int s=0;s<3;s++)mesh.SetTriangles(original.GetTriangles(s),s);mesh.SetTriangles(neck,3);
mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
var aliases=new[]{"eye_close","eye_close_left","eye_close_right","eye_close_smile","eye_sadness","eye_jitome","eye_smile","eye_anger","eye_surprise","eye_jitome","eye_smile","Iris_all_small","faceparts_whiteeye","brow_liftup","brow_lower","brow_slantup","brow_slantdown","brow_anger","brow_smile","brow_anger2","brow_confuse","brow_confident","brow_sad","brow_horizon","mouth_emo_smile","mouth_emo_laugh_1","mouth_emo_laugh_2","mouth_emo_sadness","mouth_a","mouth_emo_chagrin","tongue_pero","mouth_a","mouth_i","mouth_u","mouth_e","mouth_o"};
if(aliases.Length!=native.blendShapeCount)throw new Exception("P09 표정 계약 변경");
var baseNormals=mesh.normals;var baseTangents=mesh.tangents;var scratch=UnityEngine.Object.Instantiate(mesh);
try{for(int s=0;s<aliases.Length;s++){
int id=original.GetBlendShapeIndex(aliases[s]);if(id<0)throw new Exception("대응 표정 없음: "+aliases[s]);
var dv=new Vector3[vertices.Count];Array.Copy(deltas[id],dv,original.vertexCount);
scratch.vertices=vertices.Select((v,i)=>v+dv[i]).ToArray();scratch.RecalculateNormals();scratch.RecalculateTangents();
var dn=scratch.normals.Select((v,i)=>v-baseNormals[i]).ToArray();var dt=scratch.tangents.Select((t,i)=>new Vector3(t.x-baseTangents[i].x,t.y-baseTangents[i].y,t.z-baseTangents[i].z)).ToArray();
mesh.AddBlendShapeFrame(native.GetBlendShapeName(s),100,dv,dn,dt);
}}finally{UnityEngine.Object.DestroyImmediate(scratch);}
var texturePath=folder+"/Bokusei_Face_Albedo.png";AssetDatabase.ImportAsset(texturePath,ImportAssetOptions.ForceSynchronousImport);
var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);importer.sRGBTexture=true;importer.mipmapEnabled=true;importer.maxTextureSize=2048;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
var mats=new List<Material>();
for(int slot=0;slot<3;slot++){
var m=new Material(skin.sharedMaterials[slot]);m.name="Bokusei_P09_Face_"+slot;
if(slot==0)m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
foreach(string property in new[]{"_UseMain2ndTex","_UseMain3rdTex","_UseEmission","_UseBumpMap","_UseRim","_UseBacklight"})if(m.HasProperty(property))m.SetFloat(property,0);
if(m.HasProperty("_OutlineWidth"))m.SetFloat("_OutlineWidth",0);
AssetDatabase.CreateAsset(m,folder+"/"+m.name+".mat");mats.Add(m);
}
mats.Add(target.sharedMaterial);AssetDatabase.CreateAsset(mesh,meshPath);
var profile=ScriptableObject.CreateInstance<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>();profile.sex=UPlayGround.Editor.P09Builder.BuilderSex.Male;
profile.mesh=mesh;profile.materials=mats.ToArray();profile.boneNames=target.bones.Select(b=>b.name).ToArray();
profile.rendererPaths=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r=>r.name.StartsWith("Male_Face_")).Select(r=>AnimationUtility.CalculateTransformPath(r.transform,root.transform)).ToArray();
profile.ValidateProfile();AssetDatabase.CreateAsset(profile,profilePath);AssetDatabase.SaveAssets();
return new{vertices=mesh.vertexCount,neckVertices=neckMap.Count,shapes=mesh.blendShapeCount,bones=mesh.bindposes.Length,profile=profilePath};
