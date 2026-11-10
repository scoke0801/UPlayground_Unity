// 저장한 작업 시작 높이를 기준으로 수중 하상만 조형한다. 재실행해도 굴착이 누적되지 않는다.
if(EditorApplication.isPlaying||EditorUtility.scriptCompilationFailed)throw new Exception("정상 컴파일된 Edit Mode가 필요하다.");
const string Output="output/RiverBasinWorld/RiverbedSculpt/";
var terrain=Terrain.activeTerrains.First(item=>item.name=="Terrain_2_1");var data=terrain.terrainData;
if(terrain.gameObject.scene.path!="Assets/01.Scenes/Ingame/Worlds/RiverBasinWorld.unity")throw new Exception("RiverBasinWorld 씬이 필요하다.");
var jsonType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(item=>item!=null);
var deserialize=jsonType.GetMethod("DeserializeObject",new Type[]{typeof(string),typeof(Type)});
var config=(Dictionary<string,object>)deserialize.Invoke(null,new object[]{System.IO.File.ReadAllText("Tools/RiverBasinWorld/VisualR3/riverbed-sculpt.json"),typeof(Dictionary<string,object>)});
Func<string,float> setting=key=>Convert.ToSingle(config[key],System.Globalization.CultureInfo.InvariantCulture);
int resolution=data.heightmapResolution;var origin=terrain.transform.position;
float stepX=data.size.x/(resolution-1),stepZ=data.size.z/(resolution-1);
var live=data.GetHeights(0,0,resolution,resolution);var baseline=(float[,])live.Clone();
string heightPath=Output+"backup/heights.bin";
System.IO.Directory.CreateDirectory(Output+"backup");
if(!System.IO.File.Exists(heightPath)){
 using(var writer=new System.IO.BinaryWriter(System.IO.File.OpenWrite(heightPath))){writer.Write(resolution);for(int z=0;z<resolution;z++)for(int x=0;x<resolution;x++)writer.Write(live[z,x]);}
}else{
 using(var reader=new System.IO.BinaryReader(System.IO.File.OpenRead(heightPath))){if(reader.ReadInt32()!=resolution)throw new Exception("백업 해상도 불일치");for(int z=0;z<resolution;z++)for(int x=0;x<resolution;x++)baseline[z,x]=reader.ReadSingle();}
}
var water=GameObject.Find("GeneratedEnvironment/B1_ArtR1/B1 · 강 수면").GetComponent<UPlayGround.Components.FootstepWaterSurface>();water.RefreshMesh();
var riverVertices=water.surfaceMesh.sharedMesh.vertices;
int minX=Mathf.Max(2,Mathf.FloorToInt((setting("minimumX")-origin.x)/stepX));
int maxX=Mathf.Min(resolution-3,Mathf.CeilToInt((setting("maximumX")-origin.x)/stepX));
int minZ=Mathf.Max(2,Mathf.FloorToInt((setting("minimumZ")-origin.z)/stepZ));
int maxZ=Mathf.Min(resolution-3,Mathf.CeilToInt((setting("maximumZ")-origin.z)/stepZ));
var result=(float[,])live.Clone();var masks=new float[resolution,resolution];
Func<float,float,float,float> smooth=(a,b,value)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,value));
Func<float,float,float> gaussian=(distance,spread)=>Mathf.Exp(-distance*distance/(2*spread*spread));
Func<float,float,float,float> smoothMinimum=(a,b,radius)=>{float weight=Mathf.Max(radius-Mathf.Abs(a-b),0)/radius;return Mathf.Min(a,b)-weight*weight*radius*0.25f;};
for(int zi=minZ;zi<=maxZ;zi++)for(int xi=minX;xi<=maxX;xi++){
 float x=origin.x+xi*stepX,z=origin.z+zi*stepZ;
 result[zi,xi]=baseline[zi,xi];float original=baseline[zi,xi]*data.size.y+origin.y;
 Vector3 waterPoint;if(!water.TrySampleHeight(new Vector3(x,original,z),out waterPoint))continue;
 float submerged=waterPoint.y-original;if(submerged<=0.15f)continue;
 // 강폭은 실제 수면 단면에서 읽고 사각형 작업 범위를 강폭으로 쓰지 않는다.
 int nearest=-1;float best=float.MaxValue;
 for(int row=0;row<riverVertices.Length;row+=21){float distance=Mathf.Abs(riverVertices[row+10].z-z);if(distance<best){best=distance;nearest=row;}}
 float center=(riverVertices[nearest].x+riverVertices[nearest+20].x)*0.5f;
 float wander=(Mathf.PerlinNoise(z*0.045f,18.7f)-0.5f)*2*setting("channelWander");
 float halfWidth=(riverVertices[nearest+20].x-riverVertices[nearest].x)*0.5f;
 float across=(x-center-wander)/Mathf.Max(halfWidth,1f);
 float bowl=Mathf.Pow(Mathf.Clamp01(1-across*across),1.25f);
 float s=-85-z;float warp=(Mathf.PerlinNoise(x*0.12f,z*0.025f+8.2f)-0.5f)*3.8f;
 // 수면의 낙차를 바닥에 그대로 복제하지 않고, 침식된 경사를 넓고 비대칭하게 연결한다.
 float bedLevel=72-0.4f*smooth(0,12,s)-3*smooth(12,24,s)
  -9*smooth(18+warp,32+warp,s)-2.4f*smooth(46,62,s)
  -6.2f*smooth(56+warp,71+warp,s)-smooth(82,99.3f,s);
 float plunge=setting("plungeDepth")*(gaussian(s-34-wander,7.5f)+gaussian(s-73+wander,6.5f));
 float macro=(Mathf.PerlinNoise(x*setting("macroFrequency"),z*setting("macroFrequency")*0.68f)-0.5f)*2*setting("macroAmplitude");
 float detail=(Mathf.PerlinNoise(x*setting("detailFrequency")+31,z*setting("detailFrequency"))-0.5f)*2*setting("detailAmplitude");
 float target=bedLevel-(setting("channelDepth")+plunge)*bowl+(macro+detail)*bowl;
 float domain=smooth(setting("minimumZ"),setting("minimumZ")+setting("edgeFade"),z)
  *(1-smooth(setting("maximumZ")-setting("edgeFade"),setting("maximumZ"),z));
 float mask=smooth(0.15f,1.4f,submerged)*domain;
 masks[zi,xi]=mask;
 result[zi,xi]=Mathf.Lerp(original,smoothMinimum(original,target,setting("blendRadius")),mask)-origin.y;
 result[zi,xi]/=data.size.y;
}
// 볼록한 절삭 모서리만 낮춰 완화한다. 저점은 채우지 않아 수영 통과 깊이를 보존한다.
for(int pass=0;pass<(int)setting("erosionPasses");pass++){
 var previous=(float[,])result.Clone();
 for(int z=minZ;z<=maxZ;z++)for(int x=minX;x<=maxX;x++){
  float mask=masks[z,x];if(mask<=0)continue;float average=0,weights=0;
  for(int dz=-2;dz<=2;dz++)for(int dx=-2;dx<=2;dx++){float weight=1f/(1+dx*dx+dz*dz);average+=previous[z+dz,x+dx]*weight;weights+=weight;}
  result[z,x]=Mathf.Min(previous[z,x],Mathf.Lerp(previous[z,x],average/weights,0.6f*mask));
 }
}
int changed=0,raised=0;float maxCut=0;
for(int z=minZ;z<=maxZ;z++)for(int x=minX;x<=maxX;x++){
 float cut=(baseline[z,x]-result[z,x])*data.size.y;
 if(cut<-.001f)raised++;if(cut>.003f){changed++;maxCut=Mathf.Max(maxCut,cut);}
}
if(raised>0)throw new Exception("수중 하상에 상승 표본이 생겼다.");
// 재시도 때 이전 이동량을 누적하지 않도록 현재 지형과의 차이만 배치물에 반영한다.
int moved=0;var art=GameObject.Find("GeneratedEnvironment/B1_ArtR1").transform;
foreach(var tr in art.GetComponentsInChildren<Transform>(true)){
 if(!PrefabUtility.IsOutermostPrefabInstanceRoot(tr.gameObject))continue;
 int x=Mathf.RoundToInt((tr.position.x-origin.x)/stepX),z=Mathf.RoundToInt((tr.position.z-origin.z)/stepZ);
 if(x<minX||x>maxX||z<minZ||z>maxZ||masks[z,x]<=0)continue;
 if(tr.GetComponentsInChildren<MeshRenderer>(true).Length==0)continue;
 float delta=(result[z,x]-live[z,x])*data.size.y;if(Mathf.Abs(delta)<.03f)continue;
 Undo.RecordObject(tr,"수중 배치물 지형 추종");tr.position+=Vector3.up*delta;moved++;
}
Undo.RegisterCompleteObjectUndo(data,"침식 하상 조형");data.SetHeights(0,0,result);EditorUtility.SetDirty(data);terrain.Flush();Physics.SyncTransforms();
// 완만한 바닥에는 기존 강바닥 자갈을, 경사진 침식 면에는 암반을 섞는다.
int rockLayer=Array.FindIndex(data.terrainLayers,layer=>layer.name=="Rock");
int bedLayer=Array.FindIndex(data.terrainLayers,layer=>layer.name=="Shore");
if(rockLayer<0||bedLayer<0)throw new Exception("기존 암반·강바닥 레이어가 필요하다.");
int alphaWidth=data.alphamapWidth,alphaHeight=data.alphamapHeight,layers=data.alphamapLayers;
var alphaSource=data.GetAlphamaps(0,0,alphaWidth,alphaHeight);
string alphaPath=Output+"backup/alphamaps.bin";
if(!System.IO.File.Exists(alphaPath)){
 using(var writer=new System.IO.BinaryWriter(System.IO.File.OpenWrite(alphaPath))){writer.Write(alphaWidth);writer.Write(alphaHeight);writer.Write(layers);for(int z=0;z<alphaHeight;z++)for(int x=0;x<alphaWidth;x++)for(int layer=0;layer<layers;layer++)writer.Write(alphaSource[z,x,layer]);}
}else{
 using(var reader=new System.IO.BinaryReader(System.IO.File.OpenRead(alphaPath))){if(reader.ReadInt32()!=alphaWidth||reader.ReadInt32()!=alphaHeight||reader.ReadInt32()!=layers)throw new Exception("텍스처 백업 해상도 불일치");for(int z=0;z<alphaHeight;z++)for(int x=0;x<alphaWidth;x++)for(int layer=0;layer<layers;layer++)alphaSource[z,x,layer]=reader.ReadSingle();}
}
var alphaResult=data.GetAlphamaps(0,0,alphaWidth,alphaHeight);int painted=0;
for(int z=0;z<alphaHeight;z++)for(int x=0;x<alphaWidth;x++){
 float worldX=origin.x+x*data.size.x/(alphaWidth-1),worldZ=origin.z+z*data.size.z/(alphaHeight-1);
 int xi=Mathf.RoundToInt((worldX-origin.x)/stepX),zi=Mathf.RoundToInt((worldZ-origin.z)/stepZ);
 if(xi<minX||xi>maxX||zi<minZ||zi>maxZ)continue;
 float weight=masks[zi,xi];if(weight<=0)continue;
 float gradientX=(result[zi,xi+1]-result[zi,xi-1])*data.size.y/(stepX*2);
 float gradientZ=(result[zi+1,xi]-result[zi-1,xi])*data.size.y/(stepZ*2);
 float slope=Mathf.Sqrt(gradientX*gradientX+gradientZ*gradientZ);
 float rock=0.15f+0.45f*smooth(0.25f,1.3f,slope)+0.15f*Mathf.PerlinNoise(worldX*.13f+7,worldZ*.13f);
 for(int layer=0;layer<layers;layer++){float target=layer==rockLayer?rock:(layer==bedLayer?1-rock:0);alphaResult[z,x,layer]=Mathf.Lerp(alphaSource[z,x,layer],target,weight);}
 painted++;
}
data.SetAlphamaps(0,0,alphaResult);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(terrain.gameObject.scene);
AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.SaveScene(terrain.gameObject.scene);
string report="changed="+changed+" raised="+raised+" maxCut="+maxCut.ToString("F2")+" moved="+moved+" painted="+painted;
System.IO.File.WriteAllText(Output+"application.txt",report);return report;
