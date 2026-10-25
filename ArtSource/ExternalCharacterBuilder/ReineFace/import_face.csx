var folder="Assets/08.Models/Characters/ReineP09";
var meshPath=folder+"/Reine_P09_Head.asset";
var profilePath="Assets/10.Datas/Actor/ExternalModels/Reine_P09_Face.asset";
System.IO.Directory.CreateDirectory(folder);AssetDatabase.Refresh();
var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalAssets/Character/Spi_Reien/CharacterPrefab/reien_original_pf.prefab");
var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics));
try {
var skin=source.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="ME_chr_reien_face_01");
var target=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Female_Face_01");
var body=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Female_Body_Chest");
var original=skin.sharedMesh;var slots=new[]{0,1,2,3,4};
var native=target.sharedMesh;var nativeVertices=native.vertices;
int[] faceTriangles,neckIndices,bridgeTriangles;
using(var reader=new System.IO.BinaryReader(System.IO.File.OpenRead("ArtSource/ExternalCharacterBuilder/ReineFace/topology.bin"))){
Func<int[]> read=()=>Enumerable.Range(0,reader.ReadInt32()).Select(_=>reader.ReadInt32()).ToArray();faceTriangles=read();neckIndices=read();bridgeTriangles=read();
}
var indices=slots.SelectMany(s=>s==0?faceTriangles:original.GetTriangles(s)).Distinct().OrderBy(i=>i).ToArray();// 재질 경계의 UV만 분리하고 위치·표정·웨이트는 같은 원본 정점을 사용한다.
var upperIndices=bridgeTriangles.Where(i=>i>=0).Distinct().ToArray();
int upperStart=indices.Length;indices=indices.Concat(upperIndices).ToArray();
var upperMap=new Dictionary<int,int>();for(int i=0;i<upperIndices.Length;i++)upperMap.Add(upperIndices[i],upperStart+i);
var neckMap=new Dictionary<int,int>();for(int i=0;i<neckIndices.Length;i++)neckMap.Add(neckIndices[i],indices.Length+i);
var remap=new Dictionary<int,int>();for(int i=0;i<upperStart;i++)remap.Add(indices[i],i);
var mesh=new Mesh();mesh.name="Reine_P09_Head";
var deltas=new List<Vector3[]>();
using(var reader=new System.IO.BinaryReader(System.IO.File.OpenRead("ArtSource/ExternalCharacterBuilder/ReineFace/sculpt.bin"))){
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
for(int s=0;s<slots.Length;s++)mesh.SetTriangles((s==0?faceTriangles:original.GetTriangles(slots[s])).Select(i=>remap[i]).ToArray(),s);
mesh.SetTriangles(bridgeTriangles.Select(i=>i>=0?upperMap[i]:neckMap[-i-1]).ToArray(),slots.Length);
var boneMap=new Dictionary<string,string>{{"Head","Head"},{"Neck","Neck"},{"Chest","Chest"},{"Shoulder_L","shoulder_L"},{"Shoulder_R","shoulder_R"},{"Eye.L","eye_L"},{"Eye.R","eye_R"},{"Spine","Spine"}};
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

// 원본 머리의 열린 정수리를 닫는다. 외곽 경계는 그대로 두고 내부에만 둥근 캡을 저작한다.
var scalpRing=new[]{199,197,195,193,191,189,188,186,184,182,180,175,170,167,2,1,4,7,10,40,44,47,50,201}.Select(i=>remap[i]).ToArray();
var oldCount=mesh.vertexCount;var positions=mesh.vertices.ToList();var uvs=mesh.uv.ToList();var weights=mesh.boneWeights.ToList();
var center=Vector3.zero;foreach(int i in scalpRing)center+=positions[i];center/=scalpRing.Length;
int headIndex=Array.FindIndex(target.bones,b=>b.name=="Head");
foreach(float angle in new[]{30f,60f})foreach(int i in scalpRing){
float rad=angle*Mathf.Deg2Rad;var v=center+(positions[i]-center)*Mathf.Cos(rad);v.z=center.z+.028f*Mathf.Sin(rad);positions.Add(v);
uvs.Add(Vector2.Lerp(uvs[i],new Vector2(.5f,.90f),1-Mathf.Cos(rad)));weights.Add(new BoneWeight{boneIndex0=headIndex,weight0=1});}
int tip=positions.Count;positions.Add(center+Vector3.forward*.028f);uvs.Add(new Vector2(.5f,.90f));weights.Add(new BoneWeight{boneIndex0=headIndex,weight0=1});
var capTriangles=new List<int>();int scalpCount=scalpRing.Length;
for(int row=0;row<2;row++)for(int i=0;i<scalpCount;i++){
int j=(i+1)%scalpCount;int a=row==0?scalpRing[i]:oldCount+i;int b=row==0?scalpRing[j]:oldCount+j;int c=oldCount+row*scalpCount+i;int d=oldCount+row*scalpCount+j;
capTriangles.AddRange(new[]{a,b,c,b,d,c});}
for(int i=0;i<scalpCount;i++)capTriangles.AddRange(new[]{oldCount+scalpCount+i,oldCount+scalpCount+(i+1)%scalpCount,tip});
mesh.vertices=positions.ToArray();mesh.uv=uvs.ToArray();mesh.boneWeights=weights.ToArray();
mesh.SetTriangles(mesh.GetTriangles(slots.Length).Concat(capTriangles).ToArray(),slots.Length);
for(int i=0;i<deltas.Count;i++)deltas[i]=deltas[i].Concat(new Vector3[positions.Count-oldCount]).ToArray();
fittedVertices=mesh.vertices;

var smoothGroups=mesh.GetTriangles(0).Concat(mesh.GetTriangles(slots.Length)).Distinct().GroupBy(i=>new Vector3(Mathf.Round(fittedVertices[i].x*100000),Mathf.Round(fittedVertices[i].y*100000),Mathf.Round(fittedVertices[i].z*100000))).Where(g=>g.Count()>1).Select(g=>g.ToArray()).ToArray();
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
var aliases=new[]{"eye_close","eye_close_L","eye_close_R","eye_joy","eye_sad","eye_jito","eye_nagomi","eye_angry","eye_surp","eye_doya","eye_happy","eye_small","eye_highlight_main_hide","brow_up","brow_down","brow_surprised","brow_sad_a","brow_anger_a","brow_joy","brow_anger_b","brow_sad_b","brow_nagomi","brow_trouble","brow_tsuri","mouth_smile_a","mouth_wa","mouth_smile_b","mouth_sad_1","mouth_a","mouth_H","mouth_tang_a","mouth_a","mouth_i_1","mouth_u","mouth_e_1","mouth_o"};if(aliases.Length!=target.sharedMesh.blendShapeCount)throw new Exception("P09 표정 순서 변경 감지");
for(int s=0;s<aliases.Length;s++){
var ids=aliases[s].Split('+').Select(n=>original.GetBlendShapeIndex(n)).ToArray();if(ids.Any(i=>i<0))throw new Exception("표정 대응 없음: "+aliases[s]);
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
var bodyMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/Actor/ScenarioResidents/ToonLook/P09_Female_Skin_Toon.mat");
for(int slot=0;slot<slots.Length;slot++){
string path=folder+"/Reine_P09_Material_"+slot+".mat";
var m=AssetDatabase.LoadAssetAtPath<Material>(path);bool create=m==null;
if(create)m=new Material(skin.sharedMaterials[slots[slot]]);
if(slot==0){m.shader=bodyMaterial.shader;m.CopyPropertiesFromMaterial(bodyMaterial);m.SetColor("_Color",Color.white);}
if(slot==0||slot==1||slot==3)m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Reine_FaceAlbedo.png");
foreach(string prop in new[]{"_UseEmission","_UseEmission2nd","_UseRim","_UseBacklight","_UseMatCap","_UseMatCap2nd","_UseBumpMap","_OutlineWidth"})if(m.HasProperty(prop))m.SetFloat(prop,0);
if(create)AssetDatabase.CreateAsset(m,path);else EditorUtility.SetDirty(m);mats.Add(m);
}
mats.Add(bodyMaterial);
var profile=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>(profilePath);bool newProfile=profile==null;if(newProfile)profile=ScriptableObject.CreateInstance<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>();profile.mesh=mesh;profile.materials=mats.ToArray();profile.boneNames=target.bones.Select(b=>b.name).ToArray();profile.rendererPaths=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r=>r.name.StartsWith("Female_Face_")).Select(r=>AnimationUtility.CalculateTransformPath(r.transform,root.transform)).ToArray();profile.ValidateProfile();profile.Apply(root);
if(newProfile)AssetDatabase.CreateAsset(profile,profilePath);else EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();return new {vertices=mesh.vertexCount,shapes=mesh.blendShapeCount,bones=mesh.bindposes.Length,profile=profilePath};
}finally{UnityEngine.Object.DestroyImmediate(root);}




