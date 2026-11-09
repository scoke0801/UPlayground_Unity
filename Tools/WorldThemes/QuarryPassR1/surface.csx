if(Application.dataPath!="C:/UsingProject/UnityProject/UPlayground/Assets"||!System.IO.File.ReadAllText("Tools/WorldThemes/AgentCoordination/editor.lock").Contains("QuarryPassR1-A-20261108"))throw new Exception("A 잠금 필요");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/QuarryPassR1.unity"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("A Edit 씬 필요");
string folder="Assets/10.Datas/World/ThemeStudies/QuarryPassR1";
var terrain=Terrain.activeTerrain;var data=terrain.terrainData;if(AssetDatabase.GetAssetPath(data)!=folder+"/Terrain.asset")throw new Exception("공유 Terrain 편집 금지");
var original=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/10.Datas/World/OpenWorld/ShadeCreekVisualR3/Terrain.asset");
// 원본은 읽기만 하며 기준선 해시 확인 후에만 기존 표면을 재구성한다.
using(var sha=System.Security.Cryptography.SHA256.Create()){string h=BitConverter.ToString(sha.ComputeHash(System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(original)))).Replace("-","");if(h!="88F76A3D1D5A59467D632BBFAEB4EAFF55D9572C9582AC526861C751FAEC57BB")throw new Exception("원본 지형 기준선 변경");}
string cliffSource="Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Scenes/Terrain/TerrainLayers/TerrainLayer_Rock.terrainlayer";
if(!System.IO.File.Exists(folder+"/StrataStone.terrainlayer"))AssetDatabase.CopyAsset(cliffSource,folder+"/StrataStone.terrainlayer");
if(!System.IO.File.Exists(folder+"/QuarryDust.terrainlayer"))AssetDatabase.CopyAsset("Assets/10.Datas/World/OpenWorld/LakeVisualV2/Earth.terrainlayer",folder+"/QuarryDust.terrainlayer");
var stone=AssetDatabase.LoadAssetAtPath<TerrainLayer>(folder+"/StrataStone.terrainlayer");stone.tileSize=new Vector2(8,8);stone.normalScale=.45f;stone.smoothness=0;stone.diffuseRemapMax=new Vector4(.75f,.73f,.69f,1);EditorUtility.SetDirty(stone);AssetDatabase.SaveAssetIfDirty(stone);
var dust=AssetDatabase.LoadAssetAtPath<TerrainLayer>(folder+"/QuarryDust.terrainlayer");dust.tileSize=new Vector2(5,5);dust.diffuseRemapMax=new Vector4(.76f,.70f,.59f,1);dust.normalScale=.3f;EditorUtility.SetDirty(dust);AssetDatabase.SaveAssetIfDirty(dust);
var old=original.GetAlphamaps(0,0,original.alphamapWidth,original.alphamapHeight);int layers=original.terrainLayers.Length;
data.terrainLayers=original.terrainLayers.Concat(new[]{stone,dust}).ToArray();var next=new float[data.alphamapHeight,data.alphamapWidth,layers+2];
var changed=new List<object>();
Func<float,float,float> mask=(x,z)=>{float q=Mathf.Sqrt(Mathf.Pow((x-370)/105f,2)+Mathf.Pow((z-1195)/140f,2));float p=Mathf.Sqrt(Mathf.Pow((x-470)/115f,2)+Mathf.Pow((z-1375)/115f,2));return 1-Mathf.SmoothStep(0,1,Mathf.Clamp01((Mathf.Min(q,p)-.72f)/.28f));};
for(int z=0;z<data.alphamapHeight;z++)for(int x=0;x<data.alphamapWidth;x++){
float wx=terrain.transform.position.x+x*data.size.x/(data.alphamapWidth-1),wz=terrain.transform.position.z+z*data.size.z/(data.alphamapHeight-1);float influence=mask(wx,wz);
for(int l=0;l<layers;l++)next[z,x,l]=old[z,x,l];if(influence<=0)continue;
float slope=original.GetSteepness(x/(float)(data.alphamapWidth-1),z/(float)(data.alphamapHeight-1));float height=original.GetInterpolatedHeight(x/(float)(data.alphamapWidth-1),z/(float)(data.alphamapHeight-1));
float strata=Mathf.SmoothStep(0,1,Mathf.InverseLerp(16,42,slope));float noise=Mathf.PerlinNoise(wx*.018f,wz*.022f);float bed=Mathf.SmoothStep(0,1,Mathf.InverseLerp(180,205,height));
float rock=Mathf.Clamp01(strata*.85f+bed*.2f+(noise-.5f)*.18f);float strength=influence*.94f;
for(int l=0;l<layers;l++)next[z,x,l]*=1-strength;next[z,x,layers]=strength*rock;next[z,x,layers+1]=strength*(1-rock);
// Terrain의 8비트 채널 반올림으로 총합이 달라지지 않도록 변경 셀만 합계255로 양자화한다.
float total=0;for(int l=0;l<layers+2;l++)total+=next[z,x,l];int assigned=0;var units=new int[layers+2];var fractions=new float[layers+2];
for(int l=0;l<layers+2;l++){float value=next[z,x,l]*255/total;units[l]=Mathf.FloorToInt(value);fractions[l]=value-units[l];assigned+=units[l];}
for(int missing=255-assigned;missing>0;missing--){int best=0;for(int l=1;l<layers+2;l++)if(fractions[l]>fractions[best])best=l;units[best]++;fractions[best]=-1;}
for(int l=0;l<layers+2;l++)next[z,x,l]=units[l]/255f;
}
data.SetAlphamaps(0,0,next);EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);
var actual=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);int outside=0;
for(int z=0;z<data.alphamapHeight;z++)for(int x=0;x<data.alphamapWidth;x++){
bool different=false;for(int l=0;l<layers+2;l++)if(actual[z,x,l]!=(l<layers?old[z,x,l]:0)){different=true;break;}if(!different)continue;
float wx=x*data.size.x/(data.alphamapWidth-1),wz=-700+z*data.size.z/(data.alphamapHeight-1);if(mask(wx,wz)<=0)outside++;
var a=new float[layers];var b=new float[layers+2];for(int l=0;l<layers;l++)a[l]=old[z,x,l];for(int l=0;l<layers+2;l++)b[l]=actual[z,x,l];changed.Add(new{x,z,worldX=wx,worldZ=wz,before=a,after=b});
}
var jt=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);Func<object,string> json=o=>(string)jt.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{o});
System.IO.File.WriteAllText("output/QuarryPassR1/terrain-delta.json",json(new{space="world metres; terrain origin (0,0,-700)",heightResolution=data.heightmapResolution,alphaResolution=data.alphamapResolution,size=new[]{2400,400,2800},heightChanges=0,detailChanges=0,treeChanges=0,mask="union of ellipses (370,1195;105,140) and (470,1375;115,115), smooth edge from radius .72 to 1",beforeLayers=original.terrainLayers.Select(l=>AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(l))).ToArray(),afterLayers=data.terrainLayers.Select(l=>AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(l))).ToArray(),outsideMaskChanges=outside,cells=changed}));
if(outside!=0)throw new Exception("마스크 밖 표면 변화 "+outside);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);return new{changed=changed.Count,outside,heightChanges=0};
