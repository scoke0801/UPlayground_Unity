var profile=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/Daon_P09_Face.asset");
var mesh=profile.mesh;
if(mesh.subMeshCount!=5)throw new System.Exception("목 보정은 한 번만 적용합니다.");
var nativeRoot=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics);
var nativeSkin=nativeRoot.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true).First(r=>r.name=="Female_Face_01");
var native=nativeSkin.sharedMesh;var nativeVertices=native.vertices;
var nativeHead=native.GetTriangles(0).Distinct().Where(i=>nativeVertices[i].z>1.38f).Select(i=>nativeVertices[i]).ToArray();
var old=mesh.vertices;int oldCount=old.Length;
var offsets=new UnityEngine.Vector3[oldCount];
var faceVertices=new System.Collections.Generic.HashSet<int>(mesh.GetTriangles(0));
System.Func<float,float,float,float> smooth=(a,b,x)=>{float t=UnityEngine.Mathf.Clamp01((x-a)/(b-a));return t*t*(3-2*t);};
for(int i=0;i<oldCount;i++)
{
    if(!faceVertices.Contains(i))continue;
    var p=old[i];float mask=smooth(1.425f,1.46f,p.z);
    if(p.y>0.012f)mask=UnityEngine.Mathf.Max(mask,smooth(1.37f,1.405f,p.z));
    if(mask<=0)continue;
    float best=float.MaxValue;var nearest=p;
    foreach(var v in nativeHead){float distance=(v-p).sqrMagnitude;if(distance<best){best=distance;nearest=v;}}
    offsets[i]=(nearest-p)*mask;
}
var neckTriangles=new System.Collections.Generic.List<int>();var nativeTriangles=native.GetTriangles(0);
for(int i=0;i<nativeTriangles.Length;i+=3)
{
    int a=nativeTriangles[i],b=nativeTriangles[i+1],c=nativeTriangles[i+2];
    if(new[]{a,b,c}.All(index=>nativeVertices[index].z<1.365f&&nativeVertices[index].y>-.025f))neckTriangles.AddRange(new[]{a,b,c});
}
var neck=neckTriangles.Distinct().OrderBy(i=>i).ToArray();
if(neck.Length<10)throw new System.Exception("P09 목 표면 추출 실패");
var remap=new System.Collections.Generic.Dictionary<int,int>();for(int i=0;i<neck.Length;i++)remap.Add(neck[i],oldCount+i);
var triangles=Enumerable.Range(0,mesh.subMeshCount).Select(i=>mesh.GetTriangles(i)).ToArray();
var vertices=old.Select((v,i)=>v+offsets[i]).Concat(neck.Select(i=>nativeVertices[i])).ToArray();
var normals=mesh.normals.Concat(neck.Select(i=>native.normals[i])).ToArray();
var tangents=mesh.tangents.Concat(neck.Select(i=>native.tangents[i])).ToArray();
var uv=mesh.uv.Concat(neck.Select(i=>native.uv[i])).ToArray();
var weights=mesh.boneWeights.Concat(neck.Select(i=>native.boneWeights[i])).ToArray();
var names=new System.Collections.Generic.List<string>();var frameWeights=new System.Collections.Generic.List<float>();var deltas=new System.Collections.Generic.List<UnityEngine.Vector3[]>();var normalDeltas=new System.Collections.Generic.List<UnityEngine.Vector3[]>();var tangentDeltas=new System.Collections.Generic.List<UnityEngine.Vector3[]>();
for(int shape=0;shape<mesh.blendShapeCount;shape++)for(int frame=0;frame<mesh.GetBlendShapeFrameCount(shape);frame++)
{
    var dv=new UnityEngine.Vector3[oldCount];var dn=new UnityEngine.Vector3[oldCount];var dt=new UnityEngine.Vector3[oldCount];mesh.GetBlendShapeFrameVertices(shape,frame,dv,dn,dt);
    System.Array.Resize(ref dv,vertices.Length);System.Array.Resize(ref dn,vertices.Length);System.Array.Resize(ref dt,vertices.Length);
    names.Add(mesh.GetBlendShapeName(shape));frameWeights.Add(mesh.GetBlendShapeFrameWeight(shape,frame));deltas.Add(dv);normalDeltas.Add(dn);tangentDeltas.Add(dt);
}
mesh.ClearBlendShapes();mesh.vertices=vertices;mesh.normals=normals;mesh.tangents=tangents;mesh.uv=uv;mesh.boneWeights=weights;mesh.subMeshCount=6;
for(int i=0;i<5;i++)mesh.SetTriangles(triangles[i],i);mesh.SetTriangles(neckTriangles.Select(i=>remap[i]).ToArray(),5);
for(int i=0;i<names.Count;i++)mesh.AddBlendShapeFrame(names[i],frameWeights[i],deltas[i],normalDeltas[i],tangentDeltas[i]);
mesh.RecalculateBounds();UnityEditor.EditorUtility.SetDirty(mesh);
var nativePreset=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.BuildPreset>("Assets/10.Datas/Actor/ScenarioResidents/Npc_Daon_BuildPreset.asset");
var neckMaterial=nativePreset.config.ToonLookProfile.Resolve("Neck",nativeSkin.sharedMaterials[0]);
profile.materials=profile.materials.Concat(new[]{neckMaterial}).ToArray();UnityEditor.EditorUtility.SetDirty(profile);UnityEditor.AssetDatabase.SaveAssets();
var path="Assets/03.Prefabs/Characters/ExternalModels/Daon_P09Compatible.prefab";var root=UnityEditor.PrefabUtility.LoadPrefabContents(path);
try{profile.Apply(root);UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,path);}finally{UnityEditor.PrefabUtility.UnloadPrefabContents(root);}
return new{scalpVertices=offsets.Count(v=>v.sqrMagnitude>0.00000001f),neckVertices=neck.Length,neckTriangles=neckTriangles.Count/3};
