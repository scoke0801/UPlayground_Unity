// 원본·영역 밖 표본과 실제 환경 footprint를 읽기 검사한다. 베이크·수정·자동 복구는 하지 않는다.
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/01.Scenes/ArtReview/WorldThemes/Theme_LakeShrine.unity");
var host=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity");
if(!scene.isLoaded||!host.isLoaded||UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("두 씬이 열린 Edit Mode 필요");
string folder="output/WorldThemeProduction/LakeShrine";
var jsonType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var deserialize=jsonType.GetMethod("DeserializeObject",new Type[]{typeof(string),typeof(Type)});
Func<string,Type,object> read=(text,type)=>deserialize.Invoke(null,new object[]{text,type});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/WorldThemes/A02/lake-study.json"),typeof(Dictionary<string,object>));
var polygon=(float[][])read(plan["corePolygon"].ToString(),typeof(float[][]));
Func<float,float,bool> inside=(x,z)=>{bool hit=false;for(int i=0,j=polygon.Length-1;i<polygon.Length;j=i++)if((polygon[i][1]>z)!=(polygon[j][1]>z)&&x<(polygon[j][0]-polygon[i][0])*(z-polygon[i][1])/(polygon[j][1]-polygon[i][1])+polygon[i][0])hit=!hit;return hit;};
Func<string,string> hash=p=>{using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(System.IO.File.ReadAllBytes(p))).Replace("-","");};
Func<Array,string> contentHash=array=>{var bytes=new byte[Buffer.ByteLength(array)];Buffer.BlockCopy(array,0,bytes,0,bytes.Length);using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","");};
var data=UnityEditor.AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/10.Datas/World/ThemeStudies/LakeShrine/Terrain.asset");
var source=UnityEditor.AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/10.Datas/World/OpenWorld/Terrain.asset");
bool heightEqual=contentHash(data.GetHeights(0,0,data.heightmapResolution,data.heightmapResolution))==contentHash(source.GetHeights(0,0,source.heightmapResolution,source.heightmapResolution));
var detailChecks=new List<object>();bool detailsEqual=true;
for(int i=0;i<source.detailPrototypes.Length;i++){bool same=contentHash(data.GetDetailLayer(0,0,data.detailWidth,data.detailHeight,i))==contentHash(source.GetDetailLayer(0,0,source.detailWidth,source.detailHeight,i));detailsEqual&=same;detailChecks.Add(new{index=i,equal=same});}
var terrain=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).Single();
var origin=terrain.transform.position;var alpha=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);var before=source.GetAlphamaps(0,0,source.alphamapWidth,source.alphamapHeight);
int outsideChannels=0,changedCells=0;
for(int z=0;z<data.alphamapHeight;z++)for(int x=0;x<data.alphamapWidth;x++){
    bool changed=false;bool core=inside(origin.x+x*data.size.x/data.alphamapWidth,origin.z+z*data.size.z/data.alphamapHeight);
    for(int l=0;l<alpha.GetLength(2);l++)if(alpha[z,x,l]!=(l<before.GetLength(2)?before[z,x,l]:0f)){changed=true;if(!core)outsideChannels++;}
    if(changed)changedCells++;
}
var additions=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Theme_LakeShrine_Environment");
Physics.SyncTransforms();
var sourceIds=host.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).ToArray();
float radius=Convert.ToSingle(plan["protectedRadius"]);var anchorRows=(float[][])read(plan["protectedAnchors"].ToString(),typeof(float[][]));
var protectedPoints=sourceIds.Select(c=>new Vector2(c.transform.position.x,c.transform.position.z)).Concat(anchorRows.Select(p=>new Vector2(p[0],p[2]))).ToArray();
var footprints=new List<object>();int outsideFootprints=0,protectedIntersections=0;
foreach(Transform item in additions){
    var boxes=item.GetComponentsInChildren<Renderer>(true).Select(r=>new{kind="Renderer",name=r.name,bounds=r.bounds}).Concat(item.GetComponentsInChildren<Collider>(true).Select(c=>new{kind="Collider",name=c.name,bounds=c.bounds})).ToArray();
    foreach(var box in boxes){var b=box.bounds;bool allInside=inside(b.min.x,b.min.z)&&inside(b.min.x,b.max.z)&&inside(b.max.x,b.min.z)&&inside(b.max.x,b.max.z);if(!allInside)outsideFootprints++;
        bool touches=protectedPoints.Any(p=>{float dx=Mathf.Max(b.min.x-p.x,0f,p.x-b.max.x),dz=Mathf.Max(b.min.z-p.y,0f,p.y-b.max.z);return dx*dx+dz*dz<radius*radius;});if(touches)protectedIntersections++;
        footprints.Add(new{instance=item.name,component=box.name,kind=box.kind,min=new float[]{b.min.x,b.min.y,b.min.z},max=new float[]{b.max.x,b.max.y,b.max.z},coreContainsAllBoundsCorners=allInside,intersectsProtectedRadius=touches});
    }
}
string prefabPath="Assets/03.Prefabs/Environment/WorldThemes/LakeShrine/Theme_LakeShrine_Environment.prefab";
var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
int missing=prefab.GetComponentsInChildren<Transform>(true).Sum(t=>UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
int forbidden=prefab.GetComponentsInChildren<MonoBehaviour>(true).Length+prefab.GetComponentsInChildren<Terrain>(true).Length+prefab.GetComponentsInChildren<Camera>(true).Length+prefab.GetComponentsInChildren<Light>(true).Length;
int nullMaterials=prefab.GetComponentsInChildren<Renderer>(true).Sum(r=>r.sharedMaterials.Count(m=>m==null));
int unsupportedShaders=prefab.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Count(m=>m.shader==null||!m.shader.isSupported);
bool sceneHashEqual=hash(host.path)==plan["baselineSceneHash"].ToString(),terrainHashEqual=hash(UnityEditor.AssetDatabase.GetAssetPath(source))==plan["baselineTerrainHash"].ToString();
var result=new{heightEqual=heightEqual,detailsEqual=detailsEqual,detailChecks=detailChecks,alphaOutsideChangedChannels=outsideChannels,alphaChangedCells=changedCells,scatterMode=data.detailScatterMode.ToString(),sourceScatterMode=source.detailScatterMode.ToString(),footprints=footprints,outsideFootprints=outsideFootprints,protectedIntersections=protectedIntersections,prefabMissingScripts=missing,prefabForbiddenComponents=forbidden,prefabNullMaterials=nullMaterials,prefabUnsupportedShaders=unsupportedShaders,sourceSceneHashEqual=sceneHashEqual,sourceTerrainHashEqual=terrainHashEqual,hostDirty=host.isDirty,studyDirty=scene.isDirty,generatedAssets=new object[0],status="자동 읽기 검사 — 실제 TPS/충돌 보행/LOD 전이/성능/사용자 미술 인수와 별개"};
var serialize=jsonType.GetMethod("SerializeObject",new Type[]{typeof(object)});System.IO.File.WriteAllText(folder+"/verification-final.json",(string)serialize.Invoke(null,new object[]{result}));
if(!heightEqual||!detailsEqual||outsideChannels!=0||outsideFootprints!=0||protectedIntersections!=0||missing!=0||forbidden!=0||nullMaterials!=0||unsupportedShaders!=0||!sceneHashEqual||!terrainHashEqual)throw new Exception("Lake 읽기 검사 실패 — verification-final.json 확인");
return "PASS: height/detail 동일, core밖alpha0, footprint밖0, 보호침범0, prefab Missing/금지컴포넌트/누락재질/미지원shader0, 원본파일 동일. alpha변경셀="+changedCells;
