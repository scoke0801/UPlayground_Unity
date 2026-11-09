if(Application.dataPath!="C:/UsingProject/UnityProject/UPlayground/Assets"||!System.IO.File.ReadAllText("Tools/WorldThemes/AgentCoordination/editor.lock").Contains("\"C\""))throw new Exception("C 작업권 필요");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/HarborReedR1.unity"||scene.isDirty||EditorApplication.isPlayingOrWillChangePlaymode||EditorUtility.scriptCompilationFailed)throw new Exception("저장된 C 검토 씬 필요");
string folder="Assets/10.Datas/World/ThemeStudies/HarborReedR1";
var terrain=Terrain.activeTerrain;var data=terrain.terrainData;
if(AssetDatabase.GetAssetPath(data)!=folder+"/Terrain.asset")throw new Exception("Terrain 소유권 불일치");
if(System.IO.File.Exists("output/HarborReedR1/terrain-delta.json"))throw new Exception("재적용 금지: 이미 기록된 증분");
var baseline=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/10.Datas/World/OpenWorld/ShadeCreekVisualR3/Terrain.asset");
Func<float,float> smooth=v=>Mathf.SmoothStep(0,1,Mathf.Clamp01(v));
Func<float,float,float> mask=(x,z)=>smooth((x-1818)/10)*smooth((2008-x)/12)*smooth((z+410)/18)*smooth((-317-z)/12);
var rockBounds=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).Where(r=>r.transform.position.x>1815&&r.transform.position.x<1875&&r.transform.position.z<-318&&r.transform.position.z>-350).Select(r=>r.bounds).ToArray();
var heightChanges=new List<object>();
int resolution=data.heightmapResolution;
int hx0=Mathf.FloorToInt(1818/data.size.x*(resolution-1)),hx1=Mathf.CeilToInt(1870/data.size.x*(resolution-1));
int hz0=Mathf.FloorToInt(340/data.size.z*(resolution-1)),hz1=Mathf.CeilToInt(385/data.size.z*(resolution-1));
var heights=data.GetHeights(hx0,hz0,hx1-hx0+1,hz1-hz0+1);
for(int iz=0;iz<heights.GetLength(0);iz++)for(int ix=0;ix<heights.GetLength(1);ix++){
 float x=(hx0+ix)*data.size.x/(resolution-1),z=(hz0+iz)*data.size.z/(resolution-1)-700;float old=heights[iz,ix]*data.size.y;
 if(old>=49||old<41.9f)continue;
 bool support=rockBounds.Any(b=>x>b.min.x-1&&x<b.max.x+1&&z>b.min.z-1&&z<b.max.z+1);if(support)continue;
 UnityEngine.AI.NavMeshHit nav;if(UnityEngine.AI.NavMesh.SamplePosition(new Vector3(x,old,z),out nav,1.3f,-1))continue;
 float weight=smooth((x-1818)/10)*smooth((1866-x)/10)*smooth((z+360)/9)*smooth((-319-z)/8)*smooth((49-old)/1.4f);
 float curve=-337+7*Mathf.Exp(-Mathf.Pow((x-1848)/10,2))-6*Mathf.Exp(-Mathf.Pow((x-1829)/9,2));
 float target=Mathf.Clamp(45.9f+(z-curve)*.34f,42,49);float next=Mathf.Lerp(old,target,weight);
 if(Mathf.Abs(next-old)<.005f)continue;
 heights[iz,ix]=next/data.size.y;heightChanges.Add(new{xIndex=hx0+ix,zIndex=hz0+iz,x,z,before=old/data.size.y,after=heights[iz,ix]});
}
data.SetHeights(hx0,hz0,heights);
var layers=data.terrainLayers.ToList();int sand=layers.Count;
if(!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(layers[6]),folder+"/DrySand.terrainlayer"))throw new Exception("모래 레이어 복제 실패");
var dry=AssetDatabase.LoadAssetAtPath<TerrainLayer>(folder+"/DrySand.terrainlayer");dry.tileSize=new Vector2(3.6f,3.6f);EditorUtility.SetDirty(dry);AssetDatabase.SaveAssetIfDirty(dry);layers.Add(dry);
if(!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(layers[18]),folder+"/TidalBed.terrainlayer"))throw new Exception("퇴적층 복제 실패");
var wet=AssetDatabase.LoadAssetAtPath<TerrainLayer>(folder+"/TidalBed.terrainlayer");wet.tileSize=new Vector2(3.2f,3.2f);EditorUtility.SetDirty(wet);AssetDatabase.SaveAssetIfDirty(wet);layers.Add(wet);data.terrainLayers=layers.ToArray();
int ax0=Mathf.FloorToInt(1818/data.size.x*data.alphamapWidth),ax1=Mathf.CeilToInt(2008/data.size.x*data.alphamapWidth);
int az0=Mathf.FloorToInt(290/data.size.z*data.alphamapHeight),az1=Mathf.CeilToInt(383/data.size.z*data.alphamapHeight);
var alpha=data.GetAlphamaps(ax0,az0,ax1-ax0+1,az1-az0+1);var alphaChanges=new List<object>();
for(int iz=0;iz<alpha.GetLength(0);iz++)for(int ix=0;ix<alpha.GetLength(1);ix++){
 float x=(ax0+ix+.5f)/data.alphamapWidth*data.size.x,z=(az0+iz+.5f)/data.alphamapHeight*data.size.z-700;
 float y=data.GetInterpolatedHeight(x/data.size.x,(z+700)/data.size.z);float weight=mask(x,z)*smooth((49.2f-y)/1.2f);if(weight<.01f)continue;
 var before=Enumerable.Range(0,alpha.GetLength(2)).Select(i=>alpha[iz,ix,i]).ToArray();
 float tidal=.18f+.42f*Mathf.Exp(-Mathf.Pow((y-45.5f)/1.2f,2));
 for(int k=0;k<alpha.GetLength(2);k++)alpha[iz,ix,k]*=1-weight;
 alpha[iz,ix,sand]+=weight*(1-tidal);alpha[iz,ix,sand+1]+=weight*tidal;
 alphaChanges.Add(new{xIndex=ax0+ix,zIndex=az0+iz,x,z,before,after=Enumerable.Range(0,alpha.GetLength(2)).Select(i=>alpha[iz,ix,i]).ToArray()});
}
data.SetAlphamaps(ax0,az0,alpha);
var detailChanges=new List<object>();
for(int k=0;k<data.detailPrototypes.Length;k++){
 var map=data.GetDetailLayer(ax0,az0,ax1-ax0+1,az1-az0+1,k);bool changed=false;
 for(int iz=0;iz<map.GetLength(0);iz++)for(int ix=0;ix<map.GetLength(1);ix++){
  if(map[iz,ix]==0)continue;float x=(ax0+ix+.5f)/data.detailWidth*data.size.x,z=(az0+iz+.5f)/data.detailHeight*data.size.z-700;
  if(mask(x,z)<.5f||data.GetInterpolatedHeight(x/data.size.x,(z+700)/data.size.z)>47.4f)continue;
  detailChanges.Add(new{xIndex=ax0+ix,zIndex=az0+iz,prototypeGuid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(data.detailPrototypes[k].prototype)),before=map[iz,ix],after=0});map[iz,ix]=0;changed=true;
 }
 if(changed)data.SetDetailLayer(ax0,az0,k,map);
}
EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);
var result=new{coordinateSpace="world XZ; origin=(0,0,-700); size=(2400,400,2800)",heightResolution=resolution,alphaResolution=data.alphamapWidth,detailResolution=data.detailWidth,maskBounds=new[]{1818,2008,-410,-317},heightBounds=new[]{1818,1866,-360,-319},heightChanges,alphaChanges,detailChanges,layerMap=layers.Select((l,i)=>new{index=i,guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(l)),path=AssetDatabase.GetAssetPath(l)}).ToArray(),protection="기존 높이49m 이상·기존 NavMesh1.3m 이내·서쪽 기존 렌더러 접지 영역 제외. 상판/콘텐츠 Transform 변경 없음",merge="before/after 절대 표본과 레이어 GUID를 대조. 배열 index만 병합하거나 전체 Terrain 교체 금지"};
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(a=>a!=null);
System.IO.File.WriteAllText("output/HarborReedR1/terrain-delta.json",(string)json.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{result}));
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return new{height=heightChanges.Count,alpha=alphaChanges.Count,detail=detailChanges.Count};
