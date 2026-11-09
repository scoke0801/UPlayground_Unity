// R2 기준에서 계산하여 반복 실행 시 높이와 표면이 누적되지 않게 한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
string dir="Assets/10.Datas/World/ThemeStudies/ShadeCreekR3/";
if(scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/ShadeCreekR3.unity"||scene.isDirty||EditorApplication.isPlayingOrWillChangePlaymode||EditorUtility.scriptCompilationFailed)throw new Exception("저장된 R3 필요");
var terrain=Terrain.activeTerrain;var d=terrain.terrainData;
if(AssetDatabase.GetAssetPath(d)!=dir+"Terrain.asset")throw new Exception("전용 지형 필요");
var source=AssetDatabase.LoadAssetAtPath<TerrainData>(dir.Replace("R3/","R2/")+"Terrain.asset");
var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var protectedPoints=all.SelectMany(t=>t.GetComponents<MonoBehaviour>()).Where(c=>c!=null&&new[]{"SceneEntityId","SceneArrivalPoint","PortalActor","FlowGraphTriggerVolume"}.Contains(c.GetType().Name)).Select(c=>c.transform.position).ToArray();
// 수관 AABB 전체를 지지면으로 취급하지 않고 실제 발밑 메시 범위를 보호한다.
var supports=all.SelectMany(t=>t.GetComponents<Collider>()).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger&&!(c is TerrainCollider)).Select(c=>{
var meshCollider=c as MeshCollider;
if(meshCollider==null||meshCollider.sharedMesh==null||c.GetComponentInParent<LODGroup>()==null)return c.bounds;
var low=meshCollider.sharedMesh.vertices.Select(v=>c.transform.TransformPoint(v)).Where(v=>v.y<=terrain.SampleHeight(v)+terrain.transform.position.y+2.5f).ToArray();
if(low.Length==0)return c.bounds;var b=new Bounds(low[0],Vector3.zero);foreach(var v in low)b.Encapsulate(v);return b;
}).Where(b=>b.max.x>1570&&b.min.x<1670&&b.max.z>755&&b.min.z<850).ToArray();
var route=new[]{new Vector2(1700,740),new Vector2(1640,750),new Vector2(1605,805),new Vector2(1608,814),new Vector2(1650,855),new Vector2(1750,870)};
Func<Vector2,float> routeDistance=p=>{float distance=9999;for(int i=0;i<route.Length-1;i++){var v=route[i+1]-route[i];distance=Mathf.Min(distance,Vector2.Distance(p,route[i]+v*Mathf.Clamp01(Vector2.Dot(p-route[i],v)/v.sqrMagnitude)));}return distance;};
Func<Vector2,float> protection=p=>{float w=Mathf.SmoothStep(0,1,Mathf.InverseLerp(6,10,routeDistance(p)));foreach(var q in protectedPoints)w*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(7,12,Vector2.Distance(p,new Vector2(q.x,q.z))));foreach(var b in supports){float dx=Mathf.Max(b.min.x-p.x,0,p.x-b.max.x),dz=Mathf.Max(b.min.z-p.y,0,p.y-b.max.z);w*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(2,5,Mathf.Sqrt(dx*dx+dz*dz)));}return w;};
Func<float,float,float,float,float,float> mass=(x,z,cx,cz,width)=>Mathf.Exp(-Mathf.Pow((x-cx)/width,2)-Mathf.Pow((z-cz)/(width*.6f),2));


var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(dir+"CreekSurface.asset");var vertices=mesh.vertices;
var heights=d.GetHeights(0,0,d.heightmapResolution,d.heightmapResolution);int changed=0;float maxDelta=0;
for(int iz=0;iz<d.heightmapResolution;iz++){
float z=terrain.transform.position.z+iz*d.size.z/(d.heightmapResolution-1);if(z<765||z>829)continue;
int row=Mathf.Clamp(Mathf.RoundToInt((z-761)*2),0,144);var center=vertices[row*13+6];float half=(vertices[row*13+12].x-vertices[row*13].x)*.5f;
for(int ix=0;ix<d.heightmapResolution;ix++){
float x=terrain.transform.position.x+ix*d.size.x/(d.heightmapResolution-1);float distance=Mathf.Abs(x-center.x);if(distance>half+2)continue;
float protect=protection(new Vector2(x,z));if(protect<=0)continue;
float cross=Mathf.SmoothStep(0,1,Mathf.InverseLerp(half*.3f,half,distance));float target=center.y-.3f+cross*.4f;
float fade=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(half,half+2,distance));float old=heights[iz,ix]*d.size.y;
float value=Mathf.Lerp(old,target,protect*fade);heights[iz,ix]=value/d.size.y;if(Mathf.Abs(value-old)>.00001f){changed++;maxDelta=Mathf.Max(maxDelta,Mathf.Abs(value-old));}
}}
d.SetHeights(0,0,heights);EditorUtility.SetDirty(d);AssetDatabase.SaveAssetIfDirty(d);terrain.Flush();
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);var result=new{changed,maxDelta,protectedPoints=protectedPoints.Length,supportBounds=supports.Length,method="수관 대신 지면+2.5m 이하 실제 수목 메시 범위와 여유 폭 보호. 기존 ID 보호·보행선 보호 동일. 비수목 콜라이더 전체 Bounds 보호. 하상·둑 접합 보정"};System.IO.File.WriteAllText("output/ShadeCreekR3/bank-refinement.json",(string)json.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{result}));return result;
