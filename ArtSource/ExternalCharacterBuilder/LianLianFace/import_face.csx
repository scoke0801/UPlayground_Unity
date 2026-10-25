var folder="Assets/08.Models/Characters/LianLianP09";
var meshPath=folder+"/LianLian_P09_Head.asset";
var profilePath="Assets/10.Datas/Actor/ExternalModels/LianLian_P09_Face.asset";
System.IO.Directory.CreateDirectory(folder);AssetDatabase.Refresh();
var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalAssets/Character/LianLian/Prefabs/LianLian.prefab");
var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics));
try {
var skin=source.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Body");
var target=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Female_Face_01");
var body=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Female_Body_Chest");
var original=skin.sharedMesh;var slots=new[]{0};
var native=target.sharedMesh;var nativeVertices=native.vertices;
int[] faceTriangles,neckIndices,bridgeTriangles;
using(var reader=new System.IO.BinaryReader(System.IO.File.OpenRead("ArtSource/ExternalCharacterBuilder/LianLianFace/topology.bin"))){
Func<int[]> read=()=>Enumerable.Range(0,reader.ReadInt32()).Select(_=>reader.ReadInt32()).ToArray();faceTriangles=read();neckIndices=read();bridgeTriangles=read();
}
var indices=faceTriangles.Distinct().OrderBy(i=>i).ToArray();// 재질 경계의 UV만 분리하고 위치·표정·웨이트는 같은 원본 정점을 사용한다.
var upperIndices=bridgeTriangles.Where(i=>i>=0).Distinct().ToArray();
int upperStart=indices.Length;indices=indices.Concat(upperIndices).ToArray();
var upperMap=new Dictionary<int,int>();for(int i=0;i<upperIndices.Length;i++)upperMap.Add(upperIndices[i],upperStart+i);
var neckMap=new Dictionary<int,int>();for(int i=0;i<neckIndices.Length;i++)neckMap.Add(neckIndices[i],indices.Length+i);
var remap=new Dictionary<int,int>();for(int i=0;i<upperStart;i++)remap.Add(indices[i],i);
var mesh=new Mesh();mesh.name="LianLian_P09_Head";
var deltas=new List<Vector3[]>();
using(var reader=new System.IO.BinaryReader(System.IO.File.OpenRead("ArtSource/ExternalCharacterBuilder/LianLianFace/sculpt.bin"))){
int count=reader.ReadInt32(),shapes=reader.ReadInt32();
if(count!=original.vertexCount||shapes!=original.blendShapeCount)throw new Exception("조형 입력 계약 불일치");
Func<Vector3[]> read=()=>{var a=new Vector3[count];for(int i=0;i<count;i++)a[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());return indices.Select(i=>a[i]).ToArray();};
mesh.vertices=read().Concat(neckIndices.Select(i=>nativeVertices[i])).ToArray();for(int s=0;s<shapes;s++)deltas.Add(read().Concat(new Vector3[neckIndices.Length]).ToArray());
}
mesh.uv=indices.Select(i=>original.uv[i]).Concat(neckIndices.Select(i=>native.uv[i])).ToArray();mesh.subMeshCount=slots.Length+1;
var neckUvs=mesh.uv;var fittedVertices=mesh.vertices;
for(int i=0;i<upperIndices.Length;i++){
var point=(Vector2)fittedVertices[upperStart+i];float closest=float.MaxValue;
for(int j=0;j<neckIndices.Length;j++){
int a=neckIndices[j],b=neckIndices[(j+1)%neckIndices.Length];var start=(Vector2)nativeVertices[a];var edge=(Vector2)(nativeVertices[b]-nativeVertices[a]);
float t=Mathf.Clamp01(Vector2.Dot(point-start,edge)/edge.sqrMagnitude);float distance=(point-start-edge*t).sqrMagnitude;
if(distance<closest){closest=distance;neckUvs[upperStart+i]=Vector2.Lerp(native.uv[a],native.uv[b],t);}
}}
mesh.uv=neckUvs;
for(int s=0;s<slots.Length;s++)mesh.SetTriangles(faceTriangles.Select(i=>remap[i]).ToArray(),s);
mesh.SetTriangles(bridgeTriangles.Select(i=>i>=0?upperMap[i]:neckMap[-i-1]).ToArray(),slots.Length);
var boneMap=new Dictionary<string,string>{{"Head","Head"},{"Neck","Neck"},{"Chest","Chest"},{"Shoulder_L","shoulder_L"},{"Shoulder_R","shoulder_R"},{"Left_Eye","eye_L"},{"Right_Eye","eye_R"}};
Func<int,int> map=i=>{string name=skin.bones[i].name;if(!boneMap.ContainsKey(name))throw new Exception("알 수 없는 가중 본: "+name);int index=Array.FindIndex(target.bones,b=>b.name==boneMap[name]);if(index<0)throw new Exception("대상 본 없음");return index;};
mesh.boneWeights=indices.Select(i=>{var w=original.boneWeights[i];return new BoneWeight{boneIndex0=w.weight0>0?map(w.boneIndex0):0,boneIndex1=w.weight1>0?map(w.boneIndex1):0,boneIndex2=w.weight2>0?map(w.boneIndex2):0,boneIndex3=w.weight3>0?map(w.boneIndex3):0,weight0=w.weight0,weight1=w.weight1,weight2=w.weight2,weight3=w.weight3};}).Concat(neckIndices.Select(i=>native.boneWeights[i])).ToArray();

mesh.bindposes=target.sharedMesh.bindposes;
// P09 원본도 뒤쪽 목의 얼굴/몸 웨이트가 달라 실제 몸체 경계를 기준으로 맞춘다.
var bodyVertices=body.sharedMesh.vertices;
var bodyIndices=neckIndices.Select(i=>Array.FindIndex(bodyVertices,v=>Vector3.Distance(v,nativeVertices[i])<.00001f)).ToArray();
if(bodyIndices.Any(i=>i<0))throw new Exception("P09 몸체의 목 경계 누락");
var targetWeights=mesh.boneWeights;
Func<int,int> bodyBone=i=>Array.FindIndex(target.bones,b=>b==body.bones[i]);
for(int i=0;i<bodyIndices.Length;i++){
var weight=body.sharedMesh.boneWeights[bodyIndices[i]];
weight.boneIndex0=bodyBone(weight.boneIndex0);weight.boneIndex1=bodyBone(weight.boneIndex1);weight.boneIndex2=bodyBone(weight.boneIndex2);weight.boneIndex3=bodyBone(weight.boneIndex3);
if(new[]{weight.boneIndex0,weight.boneIndex1,weight.boneIndex2,weight.boneIndex3}.Any(b=>b<0))throw new Exception("몸체 목 본 대응 누락");
targetWeights[indices.Length+i]=weight;
}
mesh.boneWeights=targetWeights;
var smoothGroups=mesh.GetTriangles(0).Concat(mesh.GetTriangles(1)).Distinct().GroupBy(i=>new Vector3(Mathf.Round(fittedVertices[i].x*100000),Mathf.Round(fittedVertices[i].y*100000),Mathf.Round(fittedVertices[i].z*100000))).Where(g=>g.Count()>1).Select(g=>g.ToArray()).ToArray();
Action<Mesh> updateNormals=m=>{
m.RecalculateNormals();var normals=m.normals;
foreach(var group in smoothGroups){var normal=Vector3.zero;foreach(int i in group)normal+=normals[i];normal.Normalize();foreach(int i in group)normals[i]=normal;}
for(int i=0;i<neckIndices.Length;i++)normals[indices.Length+i]=body.sharedMesh.normals[bodyIndices[i]];
m.normals=normals;m.RecalculateTangents();};
updateNormals(mesh);mesh.RecalculateBounds();
var baseVertices=mesh.vertices;var baseNormals=mesh.normals;var baseTangents=mesh.tangents;
var normalDeltas=new List<Vector3[]>();var tangentDeltas=new List<Vector3[]>();
var scratch=UnityEngine.Object.Instantiate(mesh);
try{foreach(var d in deltas){scratch.vertices=baseVertices.Select((v,i)=>v+d[i]).ToArray();updateNormals(scratch);normalDeltas.Add(scratch.normals.Select((n,i)=>n-baseNormals[i]).ToArray());tangentDeltas.Add(scratch.tangents.Select((t,i)=>new Vector3(t.x-baseTangents[i].x,t.y-baseTangents[i].y,t.z-baseTangents[i].z)).ToArray());}}finally{UnityEngine.Object.DestroyImmediate(scratch);}
// P09 표정 순서를 유지하며 양쪽 표정은 실제 좌우 델타를 합성한다.
var aliases=new[]{"Blink","Blink_L","Blink_R","Wink","Eye_Sad","Eye_Jito","Wink","Eye_Anger","Eye_Surprised","Eye_Jito","Wink","Pupil_Small","Pupil_Off","Eyebrows_Pos_Upper","Eyebrows_Pos_Lower","EyeBrows_Surprised","EyeBrows_Sad1","EyeBrows_Anger1","EyeBrows_Smile","EyeBrows_Anger2","EyeBrows_Sad2","EyeBrows_Smile","EyeBrows_Sad1","EyeBrows_Flat","Mouth_Smile1","Mouth_Joy1","Mouth_Grin1","Mouth_Sad1","Lip_aa","Mouth_Anger2","Mouth_TongueOut","Lip_aa","Lip_ih","Lip_ou","Lip_E","Lip_oh"};if(aliases.Length!=target.sharedMesh.blendShapeCount)throw new Exception("P09 표정 순서 변경 감지");
for(int s=0;s<aliases.Length;s++){
var ids=aliases[s].Split('+').Select(n=>original.GetBlendShapeIndex("Target_Body."+n)).ToArray();if(ids.Any(i=>i<0))throw new Exception("표정 대응 없음: "+aliases[s]);
var dv=new Vector3[mesh.vertexCount];var dn=new Vector3[mesh.vertexCount];var dt=new Vector3[mesh.vertexCount];
foreach(int id in ids)for(int i=0;i<dv.Length;i++){dv[i]+=deltas[id][i];dn[i]+=normalDeltas[id][i];dt[i]+=tangentDeltas[id][i];}
mesh.AddBlendShapeFrame(target.sharedMesh.GetBlendShapeName(s),100,dv,dn,dt);
}
// 원본 표정은 Blender에 보관하고 게임 메시에는 P09 표정 계약을 연결한다.
var savedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
if(savedMesh==null)AssetDatabase.CreateAsset(mesh,meshPath);
else {
savedMesh.Clear();savedMesh.ClearBlendShapes();savedMesh.vertices=mesh.vertices;savedMesh.normals=mesh.normals;savedMesh.tangents=mesh.tangents;savedMesh.uv=mesh.uv;savedMesh.boneWeights=mesh.boneWeights;savedMesh.bindposes=mesh.bindposes;savedMesh.subMeshCount=mesh.subMeshCount;
for(int s=0;s<mesh.subMeshCount;s++)savedMesh.SetTriangles(mesh.GetTriangles(s),s);
for(int s=0;s<mesh.blendShapeCount;s++){var dv=new Vector3[mesh.vertexCount];var dn=new Vector3[mesh.vertexCount];var dt=new Vector3[mesh.vertexCount];mesh.GetBlendShapeFrameVertices(s,0,dv,dn,dt);savedMesh.AddBlendShapeFrame(mesh.GetBlendShapeName(s),100,dv,dn,dt);}
savedMesh.RecalculateBounds();EditorUtility.SetDirty(savedMesh);UnityEngine.Object.DestroyImmediate(mesh);mesh=savedMesh;
}
var mats=new List<Material>();
string materialPath=folder+"/LianLian_P09_Face.mat";
var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);bool createMaterial=material==null;
if(createMaterial)material=new Material(skin.sharedMaterials[0]);
material.name="LianLian_P09_Face";
material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/LianLian_FaceAlbedo.png");
if(material.mainTexture==null)throw new Exception("새 얼굴 알베도 누락");
foreach(string property in new[]{"_UseEmission","_UseEmission2nd","_UseBumpMap","_UseBump2ndMap","_UseRim","_UseBacklight","_UseMatCap","_UseMatCap2nd","_UseMain2ndTex","_UseMain3rdTex","_OutlineWidth"})if(material.HasProperty(property))material.SetFloat(property,0);
if(createMaterial)AssetDatabase.CreateAsset(material,materialPath);else EditorUtility.SetDirty(material);mats.Add(material);mats.Add(AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/Actor/ScenarioResidents/ToonLook/P09_Female_Skin_Toon.mat"));
var profile=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>(profilePath);bool newProfile=profile==null;if(newProfile)profile=ScriptableObject.CreateInstance<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>();profile.mesh=mesh;profile.materials=mats.ToArray();profile.boneNames=target.bones.Select(b=>b.name).ToArray();profile.rendererPaths=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r=>r.name.StartsWith("Female_Face_")).Select(r=>AnimationUtility.CalculateTransformPath(r.transform,root.transform)).ToArray();profile.ValidateProfile();profile.Apply(root);
if(newProfile)AssetDatabase.CreateAsset(profile,profilePath);else EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();return new {vertices=mesh.vertexCount,shapes=mesh.blendShapeCount,bones=mesh.bindposes.Length,profile=profilePath};
}finally{UnityEngine.Object.DestroyImmediate(root);}

