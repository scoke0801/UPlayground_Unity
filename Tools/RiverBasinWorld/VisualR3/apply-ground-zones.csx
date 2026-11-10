// R3-1 지표 구역화. B1 영역(x225~390, z-105~85)의 Meadow/AutumnHumus 반반 혼합을
// "볕 드는 초원 / 수관 아래 낙엽 바닥"으로 분리하고 Footpath 가장자리를 선명하게 한다.
// 결정적 월드 좌표 함수만 쓰므로 Terrain 이음새 양쪽이 같은 값을 받는다. 경계 15m는 원본과 페더링한다.
// 원본 TerrainData는 output/RiverBasinWorld/VisualR3/backup/에 복사한 뒤 수정한다.
const float MinX=225,MaxX=390,MinZ=-105,MaxZ=85,Feather=15;
const int Meadow=0,Humus=1,Footpath=2;
var trees=new List<Vector2>();
foreach(var r in GameObject.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)){
 if(!r.gameObject.activeInHierarchy)continue;var m=r.sharedMaterial;if(m==null||!m.name.Contains("Osmanthus_Bark"))continue;
 var p=r.transform.position;if(p.x<MinX-20||p.x>MaxX+20||p.z<MinZ-20||p.z>MaxZ+20)continue;trees.Add(new Vector2(p.x,p.z));}
Func<float,float,float,float> smooth=(a,b,x)=>{var t=Mathf.Clamp01((x-a)/(b-a));return t*t*(3-2*t);};
Func<float,float,float> meadowWeight=(x,z)=>{
 float d=999;foreach(var t in trees){var dd=(t.x-x)*(t.x-x)+(t.y-z)*(t.y-z);if(dd<d)d=dd;}d=Mathf.Sqrt(d);
 float canopy=1-smooth(4.5f,12f,d);
 float n=Mathf.PerlinNoise(x*0.045f+3.1f,z*0.045f+7.3f)*0.7f+Mathf.PerlinNoise(x*0.16f+11f,z*0.16f+5f)*0.3f;
 return smooth(0.40f,0.56f,n*0.9f+0.22f-canopy*0.8f);};
System.IO.Directory.CreateDirectory("output/RiverBasinWorld/VisualR3/backup");
var log=new System.Text.StringBuilder();log.AppendLine("trees "+trees.Count);
foreach(var terrain in Terrain.activeTerrains){
 var data=terrain.terrainData;var origin=terrain.transform.position;var size=data.size;int res=data.alphamapResolution;float cell=size.x/res;
 int ax=Mathf.Clamp(Mathf.FloorToInt((MinX-origin.x)/cell),0,res-1),bx=Mathf.Clamp(Mathf.CeilToInt((MaxX-origin.x)/cell),0,res-1);
 int az=Mathf.Clamp(Mathf.FloorToInt((MinZ-origin.z)/cell),0,res-1),bz=Mathf.Clamp(Mathf.CeilToInt((MaxZ-origin.z)/cell),0,res-1);
 if(bx<=ax||bz<=az||origin.x>MaxX||origin.x+size.x<MinX||origin.z>MaxZ||origin.z+size.z<MinZ)continue;
 var assetPath=AssetDatabase.GetAssetPath(data);var backup="output/RiverBasinWorld/VisualR3/backup/"+System.IO.Path.GetFileName(assetPath);
 if(!System.IO.File.Exists(backup))System.IO.File.Copy(assetPath,backup);
 int w=bx-ax+1,h=bz-az+1;var a=data.GetAlphamaps(ax,az,w,h);int layers=data.alphamapLayers;int changed=0;
 for(int j=0;j<h;j++)for(int i=0;i<w;i++){
  float x=origin.x+(ax+i+0.5f)*cell,z=origin.z+(az+j+0.5f)*cell;
  float k=Mathf.Min(smooth(MinX,MinX+Feather,x),1-smooth(MaxX-Feather,MaxX,x))*Mathf.Min(smooth(MinZ,MinZ+Feather,z),1-smooth(MaxZ-Feather,MaxZ,z));
  if(k<=0)continue;
  var orig=new float[layers];for(int l=0;l<layers;l++)orig[l]=a[j,i,l];
  var next=(float[])orig.Clone();
  float f=orig[Footpath];next[Footpath]=f<0.03f?0:smooth(0.08f,0.45f,f);
  float others=0;for(int l=0;l<layers;l++)if(l!=Meadow&&l!=Humus&&l!=Footpath)others+=orig[l];
  float rest=Mathf.Max(0,1-next[Footpath]-others);float mw=meadowWeight(x,z);
  next[Meadow]=rest*mw;next[Humus]=rest*(1-mw);
  float sum=0;for(int l=0;l<layers;l++){next[l]=Mathf.Lerp(orig[l],next[l],k);sum+=next[l];}
  for(int l=0;l<layers;l++)a[j,i,l]=next[l]/Mathf.Max(sum,1e-5f);changed++;}
 Undo.RegisterCompleteObjectUndo(data,"R3 ground zones");data.SetAlphamaps(ax,az,a);EditorUtility.SetDirty(data);
 log.AppendLine(terrain.name+" cells "+changed+" backup "+backup);}
AssetDatabase.SaveAssets();return log.ToString();
