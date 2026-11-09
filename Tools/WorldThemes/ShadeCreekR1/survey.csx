// A00 Editor lease에서만 실행한다. 메모리/파일 읽기와 조사 JSON 출력만 하며 씬·에셋 저장은 하지 않는다.
var host=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity");
if(!host.isLoaded||UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("열린 Host Edit Mode 필요");
bool sourceDirty=host.isDirty;
var jsonType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var deserialize=jsonType.GetMethod("DeserializeObject",new Type[]{typeof(string),typeof(Type)});
var roots=host.GetRootGameObjects();
var terrain=roots.SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).Single(t=>t.terrainData!=null);
var data=terrain.terrainData;
var all=roots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var environment=roots.Single(g=>g.name=="AuthoredEnvironment");
var shade=environment.transform.Find("월드 구간/그늘숲");
if(shade==null)throw new Exception("현재 그늘숲 환경 루트 없음. hierarchy 재조사 필요");
var reference=new Bounds(new Vector3(1642.5f,200f,797.5f),new Vector3(165f,1800f,175f));
Func<Transform,string> path=t=>{string p=t.name;while(t.parent!=null){t=t.parent;p=t.name+"/"+p;}return p;};
Func<Vector3,float[]> vec=p=>new[]{p.x,p.y,p.z};
Func<UnityEngine.Object,string> guid=o=>o==null?null:UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(o));
Func<UnityEngine.Object,string> globalId=o=>UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(o).ToString();
Func<UnityEngine.Object,object> assetRef=o=>{if(o==null)return null;string g;long id;UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o,out g,out id);return new{path=UnityEditor.AssetDatabase.GetAssetPath(o),guid=g,fileId=id.ToString(System.Globalization.CultureInfo.InvariantCulture),name=o.name};};
Func<Mesh,object> meshInfo=mesh=>{string assetGuid;long localId;if(mesh==null)return null;UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh,out assetGuid,out localId);return new{path=UnityEditor.AssetDatabase.GetAssetPath(mesh),guid=assetGuid,fileId=localId,vertices=mesh.vertexCount,submeshes=mesh.subMeshCount};};
var staticObjects=shade.GetComponentsInChildren<Transform>(true).Select(t=>UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject)).Where(g=>g!=null).Distinct().Where(g=>g.GetComponentsInChildren<Renderer>(true).Any(r=>r.bounds.Intersects(reference))).ToArray();
var candidateTreeRoots=staticObjects.Where(g=>UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g)=="Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Prefabs/Foliage/Trees/PF_Farm_GenTree_C_FullTree_01_Red.prefab").ToArray();
var leafTargets=candidateTreeRoots.SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true).Select(r=>new{tree=g,renderer=r})).ToArray();
var leafRows=leafTargets.Select(t=>new{treeGlobalId=globalId(t.tree),treePath=path(t.tree.transform),rendererGlobalId=globalId(t.renderer),rendererPath=path(t.renderer.transform),hasPropertyBlock=t.renderer.HasPropertyBlock(),mesh=assetRef(t.renderer.GetComponent<MeshFilter>()==null?null:t.renderer.GetComponent<MeshFilter>().sharedMesh),materials=t.renderer.sharedMaterials.Select((m,i)=>new{slot=i,asset=assetRef(m)}).ToArray(),lodMembership=t.tree.GetComponentsInChildren<LODGroup>(true).SelectMany(l=>l.GetLODs().Select((level,i)=>new{lod=i,threshold=level.screenRelativeTransitionHeight,contains=level.renderers.Contains(t.renderer)})).Where(x=>x.contains).ToArray()}).ToArray();
var ownedTargets=new HashSet<int>();
foreach(var t in leafTargets){ownedTargets.Add(t.tree.GetInstanceID());ownedTargets.Add(t.tree.transform.GetInstanceID());ownedTargets.Add(t.renderer.GetInstanceID());ownedTargets.Add(t.renderer.gameObject.GetInstanceID());ownedTargets.Add(t.renderer.transform.GetInstanceID());foreach(var m in t.renderer.sharedMaterials)if(m!=null)ownedTargets.Add(m.GetInstanceID());}
var ownershipReferences=new List<object>();var scanErrors=new List<object>();
foreach(var component in roots.SelectMany(g=>g.GetComponentsInChildren<MonoBehaviour>(true)).Where(c=>c!=null)){
    try{var so=new UnityEditor.SerializedObject(component);var property=so.GetIterator();while(property.NextVisible(true)){
        if(property.propertyType!=UnityEditor.SerializedPropertyType.ObjectReference)continue;
        var target=property.objectReferenceValue;if(target==null||!ownedTargets.Contains(target.GetInstanceID()))continue;
        ownershipReferences.Add(new{ownerPath=path(component.transform),ownerGlobalId=globalId(component),type=component.GetType().FullName,property=property.propertyPath,targetGlobalId=globalId(target),targetName=target.name});
    }}catch(Exception e){scanErrors.Add(new{path=path(component.transform),type=component.GetType().FullName,error=e.GetType().Name+":"+e.Message});}
}
var objects=staticObjects.Select(g=>new{path=path(g.transform),globalId=globalId(g),position=vec(g.transform.position),rotation=vec(g.transform.eulerAngles),scale=vec(g.transform.lossyScale),prefab=UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g),scripts=g.GetComponentsInChildren<MonoBehaviour>(true).Where(c=>c!=null).Select(c=>c.GetType().FullName).ToArray(),renderers=g.GetComponentsInChildren<MeshRenderer>(true).Select(r=>new{path=path(r.transform),globalId=globalId(r),enabled=r.enabled,active=r.gameObject.activeInHierarchy,hasPropertyBlock=r.HasPropertyBlock(),boundsMin=vec(r.bounds.min),boundsMax=vec(r.bounds.max),mesh=meshInfo(r.GetComponent<MeshFilter>()==null?null:r.GetComponent<MeshFilter>().sharedMesh),materials=r.sharedMaterials.Select((m,i)=>new{slot=i,name=m==null?null:m.name,path=UnityEditor.AssetDatabase.GetAssetPath(m),guid=guid(m),shader=m==null||m.shader==null?null:m.shader.name}).ToArray()}).ToArray(),colliders=g.GetComponentsInChildren<Collider>(true).Select(c=>new{path=path(c.transform),type=c.GetType().Name,enabled=c.enabled,trigger=c.isTrigger,mesh=c is MeshCollider?meshInfo(((MeshCollider)c).sharedMesh):null}).ToArray(),lods=g.GetComponentsInChildren<LODGroup>(true).Select(l=>new{path=path(l.transform),fade=l.fadeMode.ToString(),size=l.size,levels=l.GetLODs().Select(x=>new{threshold=x.screenRelativeTransitionHeight,renderers=x.renderers.Where(r=>r!=null).Select(globalId).ToArray()}).ToArray()}).ToArray()}).ToArray();
var anchorTypes=new[]{"SceneEntityId","FlowGraphRunner","ContentInteraction","InteractionPoint","NpcLifePresentation"};
var anchors=all.Where(t=>reference.Contains(t.position)).SelectMany(t=>t.GetComponents<MonoBehaviour>().Where(c=>c!=null&&(anchorTypes.Contains(c.GetType().Name)||(c.GetType().Namespace??"").Contains("Interaction"))).Select(c=>new{path=path(t),position=vec(t.position),type=c.GetType().FullName,serialized=UnityEditor.EditorJsonUtility.ToJson(c)})).ToArray();
var volumes=all.SelectMany(t=>t.GetComponents<Component>()).Where(c=>c!=null&&c.GetType().Name=="NavMeshModifierVolume").Select(c=>{
    var so=new UnityEditor.SerializedObject(c);var center=so.FindProperty("m_Center");var size=so.FindProperty("m_Size");var area=so.FindProperty("m_Area");
    var localCenter=center==null?Vector3.zero:center.vector3Value;var localSize=size==null?Vector3.zero:size.vector3Value;
    var corners=new List<Vector3>();for(int ix=-1;ix<=1;ix+=2)for(int iy=-1;iy<=1;iy+=2)for(int iz=-1;iz<=1;iz+=2)corners.Add(c.transform.TransformPoint(localCenter+Vector3.Scale(localSize*.5f,new Vector3(ix,iy,iz))));
    var worldBounds=new Bounds(corners[0],Vector3.zero);foreach(var p in corners)worldBounds.Encapsulate(p);
    return new{path=path(c.transform),position=vec(c.transform.position),localCenter=vec(localCenter),localSize=vec(localSize),worldCorners=corners.Select(vec).ToArray(),area=area==null?-1:area.intValue,intersects=worldBounds.Intersects(reference)};
}).Where(v=>v.intersects).ToArray();
var waterCandidates=roots.SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).Where(r=>r.bounds.Intersects(reference)&&r.sharedMaterials.Any(m=>m!=null&&((m.shader!=null&&m.shader.name.IndexOf("water",StringComparison.OrdinalIgnoreCase)>=0)||m.name.IndexOf("water",StringComparison.OrdinalIgnoreCase)>=0||m.name.IndexOf("river",StringComparison.OrdinalIgnoreCase)>=0||m.name.IndexOf("creek",StringComparison.OrdinalIgnoreCase)>=0))).Select(r=>new{path=path(r.transform),globalId=globalId(r),position=vec(r.transform.position),boundsMin=vec(r.bounds.min),boundsMax=vec(r.bounds.max),hasPropertyBlock=r.HasPropertyBlock(),materials=r.sharedMaterials.Select(m=>new{path=UnityEditor.AssetDatabase.GetAssetPath(m),guid=guid(m),shader=m==null||m.shader==null?null:m.shader.name}).ToArray()}).ToArray();
var samples=new List<object>();
var protectedPositions=new[]{new Vector3(1605f,79.90235f,805f),new Vector3(1608f,81.3757f,814f),new Vector3(1627.73f,89.6549759f,857.16f)};
var supports=protectedPositions.Select(p=>new{position=vec(p),hits=Physics.RaycastAll(p+Vector3.up*4f,Vector3.down,12f,-1,QueryTriggerInteraction.Ignore).Where(h=>h.collider.gameObject.scene==host).OrderBy(h=>h.distance).Select(h=>new{path=path(h.collider.transform),position=vec(h.point),distance=h.distance}).ToArray()}).ToArray();
for(float z=710;z<=885;z+=5)for(float x=1560;x<=1725;x+=5){
    float nx=(x-terrain.transform.position.x)/data.size.x,nz=(z-terrain.transform.position.z)/data.size.z;
    if(nx<0||nx>1||nz<0||nz>1)continue;
    var p=new Vector3(x,data.GetInterpolatedHeight(nx,nz)+terrain.transform.position.y,z);
    UnityEngine.AI.NavMeshHit hit;bool nav=UnityEngine.AI.NavMesh.SamplePosition(p,out hit,2f,-1);
    var alpha=data.GetAlphamaps(Mathf.Clamp((int)(nx*data.alphamapWidth),0,data.alphamapWidth-1),Mathf.Clamp((int)(nz*data.alphamapHeight),0,data.alphamapHeight-1),1,1);
    samples.Add(new{x=x,z=z,height=p.y,slope=data.GetSteepness(nx,nz),alpha=Enumerable.Range(0,alpha.GetLength(2)).Select(i=>alpha[0,0,i]).ToArray(),navSample=nav,navAreaMask=nav?hit.mask:0,navPosition=nav?vec(hit.position):null});
}
if(host.isDirty!=sourceDirty)throw new Exception("조사 중 Host dirty 상태 변동. A00 확인 필요");
var result=new{status="최신 정규 씬 조사. 독립 제작 승인·정규 통합 불가",hostDirty=host.isDirty,coordinateSpace="world",referenceBounds=new[]{1560,1725,710,885},referenceBoundsOrder=new[]{"minX","maxX","minZ","maxZ"},terrain=UnityEditor.AssetDatabase.GetAssetPath(data),terrainPosition=vec(terrain.transform.position),terrainSize=vec(data.size),scatter=data.detailScatterMode.ToString(),detailPrototypeCount=data.detailPrototypes.Length,layers=data.terrainLayers.Select((l,i)=>new{index=i,path=UnityEditor.AssetDatabase.GetAssetPath(l),guid=guid(l)}).ToArray(),objects=objects,protectedAnchors=anchors,navModifierVolumes=volumes,waterCandidates=waterCandidates,samples=samples,hostOnlyProtectedSupports=supports,leafRendererCandidates=leafRows,serializedOwnershipReferences=ownershipReferences,ownershipScanErrors=scanErrors,limits=new[]{"waterCandidates는 재질명/셰이더명에 의한 조사 후보이며 부재 증명이 아니다", "NavMesh 표본은 전역 loaded NavMesh 조회이며 Host 전용 양방향 이동 검증이 아니다", "serialized 앵커는 참조용이고 아트 씬 복제 대상이 아니다", "역참조는 현재 Host SerializedObject만 조사한다. runtime lookup/MPB writer/vertex color/바람 동작 부재를 증명하지 않는다", "Physics hits는 Host scene만 필터한다. broadphase 동기화 상태는 A00 확인 필요", "LOD 전이·TPS 가독성·성능 미검증"}};
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
string output=(string)json.GetMethod("SerializeObject",new Type[]{typeof(object)}).Invoke(null,new object[]{result});
System.IO.Directory.CreateDirectory("output/ShadeCreekR1");
System.IO.File.WriteAllText("output/ShadeCreekR1/live-probe.json",output);
return "A05 읽기 조사: 환경="+objects.Length+", 앵커="+anchors.Length+", volume="+volumes.Length+", water후보="+waterCandidates.Length+", 표본="+samples.Count+". 저장 없음";

