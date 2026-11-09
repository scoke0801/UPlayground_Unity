// R2 기준에서 계산하여 반복 실행 시 높이와 표면이 누적되지 않게 한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
string dir="Assets/10.Datas/World/ThemeStudies/ShadeCreekR3/";
if(scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/ShadeCreekR3.unity"||scene.isDirty||EditorApplication.isPlayingOrWillChangePlaymode||EditorUtility.scriptCompilationFailed)throw new Exception("저장된 R3 필요");
var terrain=Terrain.activeTerrain;var d=terrain.terrainData;
if(AssetDatabase.GetAssetPath(d)!=dir+"Terrain.asset")throw new Exception("전용 지형 필요");
var source=AssetDatabase.LoadAssetAtPath<TerrainData>(dir.Replace("R3/","R2/")+"Terrain.asset");
var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var protectedPoints=all.SelectMany(t=>t.GetComponents<MonoBehaviour>()).Where(c=>c!=null&&new[]{"SceneEntityId","SceneArrivalPoint","PortalActor","FlowGraphTriggerVolume"}.Contains(c.GetType().Name)).Select(c=>c.transform.position).ToArray();
var supports=all.SelectMany(t=>t.GetComponents<Collider>()).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger&&!(c is TerrainCollider)).Select(c=>c.bounds).Where(b=>b.max.x>1570&&b.min.x<1670&&b.max.z>755&&b.min.z<850).ToArray();
var route=new[]{new Vector2(1700,740),new Vector2(1640,750),new Vector2(1605,805),new Vector2(1608,814),new Vector2(1650,855),new Vector2(1750,870)};
Func<Vector2,float> routeDistance=p=>{float distance=9999;for(int i=0;i<route.Length-1;i++){var v=route[i+1]-route[i];distance=Mathf.Min(distance,Vector2.Distance(p,route[i]+v*Mathf.Clamp01(Vector2.Dot(p-route[i],v)/v.sqrMagnitude)));}return distance;};
Func<Vector2,float> protection=p=>{float w=Mathf.SmoothStep(0,1,Mathf.InverseLerp(6,10,routeDistance(p)));foreach(var q in protectedPoints)w*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(7,12,Vector2.Distance(p,new Vector2(q.x,q.z))));foreach(var b in supports){float dx=Mathf.Max(b.min.x-p.x,0,p.x-b.max.x),dz=Mathf.Max(b.min.z-p.y,0,p.y-b.max.z);w*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(2,5,Mathf.Sqrt(dx*dx+dz*dz)));}return w;};
Func<float,float,float,float,float,float> mass=(x,z,cx,cz,width)=>Mathf.Exp(-Mathf.Pow((x-cx)/width,2)-Mathf.Pow((z-cz)/(width*.6f),2));
var heights=source.GetHeights(0,0,source.heightmapResolution,source.heightmapResolution);int changed=0;float maxDelta=0;
for(int z=0;z<d.heightmapResolution;z++){float wz=terrain.transform.position.z+z*d.size.z/(d.heightmapResolution-1);if(wz<765||wz>838)continue;for(int x=0;x<d.heightmapResolution;x++){float wx=terrain.transform.position.x+x*d.size.x/(d.heightmapResolution-1);if(wx<1580||wx>1660)continue;float waterClearance=(wx>1615&&wx<1645&&wz>757&&wz<837)?0:1;float delta=waterClearance*protection(new Vector2(wx,wz))*(1.2f*mass(wx,wz,1604,790,15)+.85f*mass(wx,wz,1636,820,15)+.65f*mass(wx,wz,1622,802,8));if(delta<.002f)continue;heights[z,x]+=delta/d.size.y;changed++;maxDelta=Mathf.Max(maxDelta,delta);}}
// 통행 지지면을 유지하면서 나무 뿌리의 부식층이 활동 공간 가장자리로 이어지게 한다.
var trees=all.Select(t=>t.GetComponent<LODGroup>()).Where(g=>g!=null&&g.transform.position.x>1570&&g.transform.position.x<1680&&g.transform.position.z>750&&g.transform.position.z<860&&g.GetLODs().Any(l=>l.renderers.Any(r=>r!=null&&r.sharedMaterials.Any(m=>m!=null&&AssetDatabase.GetAssetPath(m)==dir+"Canopy_0.mat")))).Select(g=>g.transform.position).ToArray();
var alpha=source.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);int painted=0;
for(int z=0;z<d.alphamapHeight;z++)for(int x=0;x<d.alphamapWidth;x++){
float wx=x*d.size.x/(d.alphamapWidth-1),wz=-700+z*d.size.z/(d.alphamapHeight-1);
float scope=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.7f,1,Mathf.Sqrt(Mathf.Pow((wx-1620)/65,2)+Mathf.Pow((wz-802)/48,2))));if(scope<=0)continue;
float root=0;foreach(var t in trees)root=Mathf.Max(root,mass(wx,wz,t.x,t.z,10));
float path=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.3f,5.5f,routeDistance(new Vector2(wx,wz))));
float bank=mass(wx,wz,1623,801,12)*.7f;
float humus=Mathf.Clamp01(root*.9f+bank)*(1-path*.8f)*scope*(1-alpha[z,x,18]);
for(int l=0;l<d.alphamapLayers;l++)alpha[z,x,l]*=1-humus;
alpha[z,x,15]+=humus*.65f;alpha[z,x,16]+=humus*.35f;painted++;
}
d.SetHeights(0,0,heights);d.SetAlphamaps(0,0,alpha);d.SetBaseMapDirty();terrain.Flush();EditorUtility.SetDirty(d);AssetDatabase.SaveAssetIfDirty(d);
var result=new{changed,maxDelta,painted,protectedPoints=protectedPoints.Length,supports=supports.Length,trees=trees.Length,routeProtection=6,contentProtection=7,scope="높이 변경 X1580~1660 Z765~838. 실플레이 및 새 NavMesh 미검증"};
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);System.IO.File.WriteAllText("output/ShadeCreekR3/floor-pass.json",(string)json.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{result}));return result;
