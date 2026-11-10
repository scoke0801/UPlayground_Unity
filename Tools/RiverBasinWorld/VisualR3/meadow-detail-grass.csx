// R3e 초원 잔디: Terrain 디테일(GPU 인스턴싱). 1차(R3d)는 한 종·한 색·1m 격자 칸당 3개라 줄무늬와 칼같은 경계가 인위적이었다.
// - 0.5m 셀(해상도 1024)에 칸당 0~1개, 셀별 해시 확률로 배치해 격자 줄무늬 제거
// - 두 겹 노이즈로 군락과 빈터, Meadow 경계는 노이즈로 디더링
// - 지형 디테일용 Polyart 풀 3종 + 싱싱/마른 색 변주(가을)
// 길(Footpath)·물가(Shore)·바위(Rock), 강 수면 아래·물가 0.4m, B1 영역 밖은 0. 결정적 해시라 재실행해도 같은 결과.
const float MinX=200,MaxX=420,MinZ=-110,MaxZ=90,Feather=14;const int RES=1024;
// 전용 가을 톤 복제본(원본 Polyart 연두색은 단풍 장면에서 분리되어 보였다). 원본: Farmlands/Prefabs/Foliage/Grass/Grass_02/SM_Farm_*_Detail
string g2="Assets/03.Prefabs/Environment/RiverBasinWorld/VisualR3/";
var prefabs=new[]{g2+"PF_B1_SmallGrass_01_Detail.prefab",g2+"PF_B1_SmallGrass_03_Detail.prefab",g2+"PF_B1_MediumGrass_02_Detail.prefab"}.Select(p=>AssetDatabase.LoadAssetAtPath<GameObject>(p)).ToArray();
var widths=new[]{new Vector2(0.9f,1.5f),new Vector2(0.8f,1.3f),new Vector2(0.4f,0.65f)};var heights=new[]{new Vector2(0.8f,1.5f),new Vector2(0.8f,1.4f),new Vector2(0.4f,0.7f)};
Func<float,float,float,float> smooth=(a,b,x)=>{var t=Mathf.Clamp01((x-a)/(b-a));return t*t*(3-2*t);};
Func<float,float,int,float> hash=(x,z,s)=>{float h=Mathf.Sin(x*12.9898f+z*78.233f+s*37.719f)*43758.5453f;return h-Mathf.Floor(h);};
var prof=new float[,]{{0,72},{12,71.6f},{24,68.6f},{24.01f,59.6f},{46,59.6f},{62,57.2f},{62.01f,51f},{82,51},{99.3f,50}};
Func<float,float> W=s=>{int n=prof.GetLength(0);if(s<=prof[0,0])return prof[0,1];for(int q=1;q<n;q++)if(s<=prof[q,0]){float u=(s-prof[q-1,0])/(prof[q,0]-prof[q-1,0]);return Mathf.Lerp(prof[q-1,1],prof[q,1],u);}return prof[n-1,1];};
var log=new System.Text.StringBuilder();
foreach(var t in Terrain.activeTerrains){var d=t.terrainData;var o=t.transform.position;if(o.x>MaxX||o.x+512<MinX||o.z>MaxZ||o.z+512<MinZ)continue;
 Undo.RegisterCompleteObjectUndo(d,"R3e detail grass");
 d.detailPrototypes=prefabs.Select((p,i)=>new DetailPrototype{usePrototypeMesh=true,prototype=p,useInstancing=true,renderMode=DetailRenderMode.VertexLit,minWidth=widths[i].x,maxWidth=widths[i].y,minHeight=heights[i].x,maxHeight=heights[i].y,noiseSpread=0.6f,density=1f,healthyColor=Color.white,dryColor=new Color(.92f,.84f,.66f),alignToGround=0.7f,positionJitter=1f}).ToArray();
 d.SetDetailResolution(RES,32);d.SetDetailScatterMode(DetailScatterMode.InstanceCountMode);
 float cell=d.size.x/RES;var L=new int[3][,];for(int l=0;l<3;l++)L[l]=new int[RES,RES];int total=0;
 int ares=d.alphamapResolution;var am=d.GetAlphamaps(0,0,ares,ares);
 for(int j=0;j<RES;j++)for(int i=0;i<RES;i++){float x=o.x+(i+0.5f)*cell,z=o.z+(j+0.5f)*cell;
  float k=Mathf.Min(smooth(MinX,MinX+Feather,x),1-smooth(MaxX-Feather,MaxX,x))*Mathf.Min(smooth(MinZ,MinZ+Feather,z),1-smooth(MaxZ-Feather,MaxZ,z));if(k<=0)continue;
  int ai=Mathf.Clamp((int)((x-o.x)/d.size.x*ares),0,ares-1),aj=Mathf.Clamp((int)((z-o.z)/d.size.z*ares),0,ares-1);
  float meadow=am[aj,ai,0],path=am[aj,ai,2],rock=am[aj,ai,3],shore=am[aj,ai,6];
  float dither=(Mathf.PerlinNoise(x*0.23f+3f,z*0.23f+1f)-0.5f)*0.45f;
  float field=smooth(0.12f,0.6f,meadow+dither)*(1-smooth(0.12f,0.38f,path))*(1-smooth(0.2f,0.5f,shore+rock))*k;
  if(field<=0.02f)continue;
  float clump=smooth(0.3f,0.7f,Mathf.PerlinNoise(x*0.085f+11f,z*0.085f+2f)*0.75f+Mathf.PerlinNoise(x*0.3f,z*0.3f)*0.35f);
  float p=field*Mathf.Lerp(0.45f,1f,clump);
  if(Mathf.Abs(x-307f)<28f){float water=z>-85f?72f:W(-85f-z);if(t.SampleHeight(new Vector3(x,0,z))+o.y<water+0.4f)continue;}
  float tall=Mathf.PerlinNoise(x*0.14f+40f,z*0.14f+7f);
  if(hash(x,z,0)<p*0.9f){L[0][j,i]=1;total++;}
  if(hash(x,z,1)<p*0.7f*smooth(0.35f,0.7f,tall)){L[1][j,i]=1;total++;}
  if(hash(x,z,2)<p*0.035f){L[2][j,i]=1;total++;}}
 for(int l=0;l<3;l++)d.SetDetailLayer(0,0,l,L[l]);EditorUtility.SetDirty(d);
 t.detailObjectDistance=90;t.detailObjectDensity=1f;log.AppendLine(t.name+" instances "+total);}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();return log.ToString();
