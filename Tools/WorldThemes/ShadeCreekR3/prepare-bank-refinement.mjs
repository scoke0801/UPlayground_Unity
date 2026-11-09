import fs from 'node:fs';
let source=fs.readFileSync('Tools/WorldThemes/ShadeCreekR3/shape-water.csx','utf8');
let pre=source.split('var original=')[0];
const start=pre.indexOf('var supports=');const end=pre.indexOf('var route=',start);
pre=pre.slice(0,start)+`// 수관 AABB 전체를 지지면으로 취급하지 않고 실제 발밑 메시 범위를 보호한다.
var supports=all.SelectMany(t=>t.GetComponents<Collider>()).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger&&!(c is TerrainCollider)).Select(c=>{
var meshCollider=c as MeshCollider;
if(meshCollider==null||meshCollider.sharedMesh==null||c.GetComponentInParent<LODGroup>()==null)return c.bounds;
var low=meshCollider.sharedMesh.vertices.Select(v=>c.transform.TransformPoint(v)).Where(v=>v.y<=terrain.SampleHeight(v)+terrain.transform.position.y+2.5f).ToArray();
if(low.Length==0)return c.bounds;var b=new Bounds(low[0],Vector3.zero);foreach(var v in low)b.Encapsulate(v);return b;
}).Where(b=>b.max.x>1570&&b.min.x<1670&&b.max.z>755&&b.min.z<850).ToArray();
`+pre.slice(end);
const body=`
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
`;
fs.writeFileSync('Tools/WorldThemes/ShadeCreekR3/refine-bank.csx',pre+body);
