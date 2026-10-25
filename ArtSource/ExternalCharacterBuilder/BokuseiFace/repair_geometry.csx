var profile=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/Bokusei_P09_Face.asset");
var mesh=profile.mesh;var native=AssetDatabase.LoadAssetAtPath<GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabMaleNoPhysics).GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Male_Face_01").sharedMesh;
var original=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalAssets/Character/ROKO SHOP/Bokusei/01_Prefab/Bokusei.prefab").GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Body").sharedMesh;
var weights=mesh.boneWeights.Take(original.vertexCount).ToArray();
var bindings=System.IO.File.ReadAllLines("ArtSource/ExternalCharacterBuilder/BokuseiFace/neck_bindings.txt").Select(line=>line.Split(',')).ToArray();
foreach(var parts in bindings){
int index=int.Parse(parts[0]),a=int.Parse(parts[1]),b=int.Parse(parts[2]);float t=float.Parse(parts[3],System.Globalization.CultureInfo.InvariantCulture);
var combined=new Dictionary<int,float>();var nw=native.boneWeights;
Action<BoneWeight,float> add=(w,f)=>{var ids=new[]{w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3};var values=new[]{w.weight0,w.weight1,w.weight2,w.weight3};for(int k=0;k<4;k++){if(!combined.ContainsKey(ids[k]))combined[ids[k]]=0;combined[ids[k]]+=values[k]*f;}};
add(nw[a],1-t);add(nw[b],t);var pairs=combined.OrderByDescending(p=>p.Value).Take(4).ToArray();var idsOut=new int[4];var valuesOut=new float[4];float sum=pairs.Sum(p=>p.Value);
for(int k=0;k<pairs.Length;k++){idsOut[k]=pairs[k].Key;valuesOut[k]=pairs[k].Value/sum;}
weights[index]=new BoneWeight{boneIndex0=idsOut[0],boneIndex1=idsOut[1],boneIndex2=idsOut[2],boneIndex3=idsOut[3],weight0=valuesOut[0],weight1=valuesOut[1],weight2=valuesOut[2],weight3=valuesOut[3]};
}
Vector3[] vertices;var deltas=new List<Vector3[]>();
using(var reader=new System.IO.BinaryReader(System.IO.File.OpenRead("ArtSource/ExternalCharacterBuilder/BokuseiFace/sculpt.bin"))){
int count=reader.ReadInt32(),shapes=reader.ReadInt32();if(count!=original.vertexCount||shapes!=original.blendShapeCount)throw new Exception("입력 계약 변경");
Func<Vector3[]> read=()=>{var points=new Vector3[count];for(int i=0;i<count;i++)points[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());return points;};
vertices=read();for(int s=0;s<shapes;s++)deltas.Add(read());
}
mesh.ClearBlendShapes();mesh.Clear();mesh.vertices=vertices;mesh.uv=original.uv;mesh.boneWeights=weights;mesh.bindposes=native.bindposes;mesh.subMeshCount=3;
for(int s=0;s<3;s++)mesh.SetTriangles(original.GetTriangles(s),s);
mesh.RecalculateNormals();mesh.RecalculateTangents();
var normals=mesh.normals;var tangents=mesh.tangents;var scratch=UnityEngine.Object.Instantiate(mesh);
var aliases=new[]{"eye_close","eye_close_left","eye_close_right","eye_close_smile","eye_sadness","eye_jitome","eye_smile","eye_anger","eye_surprise","eye_jitome","eye_smile","Iris_all_small","faceparts_whiteeye","brow_liftup","brow_lower","brow_slantup","brow_slantdown","brow_anger","brow_smile","brow_anger2","brow_confuse","brow_confident","brow_sad","brow_horizon","mouth_emo_smile","mouth_emo_laugh_1","mouth_emo_laugh_2","mouth_emo_sadness","mouth_a","mouth_emo_chagrin","tongue_pero","mouth_a","mouth_i","mouth_u","mouth_e","mouth_o"};
try{for(int s=0;s<aliases.Length;s++){
var dv=deltas[original.GetBlendShapeIndex(aliases[s])];
// 바디와 공유하는 목 경계는 표정으로 움직이지 않는다.
foreach(var parts in bindings)dv[int.Parse(parts[0])]=Vector3.zero;
scratch.vertices=vertices.Select((v,i)=>v+dv[i]).ToArray();scratch.RecalculateNormals();scratch.RecalculateTangents();
var dn=scratch.normals.Select((v,i)=>v-normals[i]).ToArray();var dt=scratch.tangents.Select((v,i)=>new Vector3(v.x-tangents[i].x,v.y-tangents[i].y,v.z-tangents[i].z)).ToArray();
mesh.AddBlendShapeFrame(native.GetBlendShapeName(s),100,dv,dn,dt);
}}finally{UnityEngine.Object.DestroyImmediate(scratch);}
mesh.RecalculateBounds();profile.materials=profile.materials.Take(3).ToArray();profile.ValidateProfile();EditorUtility.SetDirty(mesh);EditorUtility.SetDirty(profile);
var path="Assets/03.Prefabs/Characters/ExternalModels/Bokusei_P09Compatible.prefab";var root=PrefabUtility.LoadPrefabContents(path);
try{profile.Apply(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
AssetDatabase.SaveAssets();return new{vertices=mesh.vertexCount,neckBoundary=bindings.Length,shapes=mesh.blendShapeCount};
