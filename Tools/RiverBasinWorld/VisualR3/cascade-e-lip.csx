// R3d 낙구 이음: 급류 수면과 낙수면 메시만 재생성(바위·잔돌·물보라 유지).
// 문제: 낙수면 셰이더는 v≈0을 투명하게 시작하는데 위 수면은 직선으로 끝나고 낙구는 볼록 호라, 그 사이로 하상이 띠처럼 보였다.
// 해결: 위 수면 끝 2.5m를 호를 따라 휘게 하고, 낙수면은 위 수면 밑 1m에서 시작해 투명 구간을 수면 아래에 숨긴다.
const float CX=307.2f,Z0=-85f,LEN=99.3f,SURF_START=-20f,BEND=2.5f;
var prof=new float[,]{{0,72,2.4f},{12,71.6f,1.6f},{24,68.6f,0.6f},{24.01f,59.6f,2.6f},{46,59.6f,2.6f},{62,57.2f,0.6f},{62.01f,51f,2.4f},{82,51,2.2f},{99.3f,50,1.6f}};
Func<float,float> W=s=>{int n=prof.GetLength(0);if(s<=prof[0,0])return prof[0,1];for(int i=1;i<n;i++)if(s<=prof[i,0]){float t=(s-prof[i-1,0])/(prof[i,0]-prof[i-1,0]);return Mathf.Lerp(prof[i-1,1],prof[i,1],t);}return prof[n-1,1];};
Func<float,float> env=s=>Mathf.Sin(Mathf.Clamp01(s/LEN)*Mathf.PI);
Func<float,float> center=s=>CX+env(s)*(2.2f*Mathf.Sin(s*0.075f+0.6f)+1.4f*(Mathf.PerlinNoise(s*0.05f,3.3f)-0.5f)*2f);
Func<float,float,float,float> smooth=(a,b,x)=>{var t=Mathf.Clamp01((x-a)/(b-a));return t*t*(3-2*t);};
var terrain=Terrain.activeTerrains.First(t=>t.name=="Terrain_2_1");var o=terrain.transform.position;Func<float,float,float> H=(x,z)=>terrain.SampleHeight(new Vector3(x,0,z))+o.y;
Func<float,float,float,float> reach=(s,water,sign)=>{float z=Z0-s,c0=center(s);float d=0.5f;while(d<18f&&H(c0+sign*d,z)<water+0.2f)d+=0.25f;return Mathf.Min(d+0.6f,18.6f);};
var lipS=new[]{24f,62f};var lipTop=new[]{68.6f,57.2f};var pool=new[]{59.6f,51f};var thr=new[]{1.9f,1.4f};var arc=new[]{1.4f,1.0f};
string dir="Assets/10.Datas/World/RiverBasinWorld/VisualR3";
// 1) 급류 수면
var sv=new List<Vector3>();var st=new List<int>();var suv=new List<Vector2>();const int Cols=18;var lip=new Dictionary<int,float[]>();
var spans=new[]{new[]{SURF_START,24f},new[]{24.2f,62f},new[]{62.2f,LEN}};
foreach(var sp in spans){int baseRow=-1;for(float s=sp[0];;s+=0.75f){float ss=Mathf.Min(s,sp[1]);float z=Z0-ss;float wy=W(ss);float l=reach(ss,wy,-1),r=reach(ss,wy,1);float c0=center(ss);
  int fi=-1;for(int f=0;f<2;f++)if(ss>lipS[f]-BEND-0.01f&&ss<=lipS[f]+0.01f)fi=f;
  if(Mathf.Abs(ss-24f)<0.01f)lip[1]=new[]{l,r,c0};if(Mathf.Abs(ss-62f)<0.01f)lip[2]=new[]{l,r,c0};
  int row=sv.Count;for(int c=0;c<=Cols;c++){float a=c/(float)Cols;float x=Mathf.Lerp(c0-l,c0+r,a);float zz=z;
   if(fi>=0){float bend=smooth(lipS[fi]-BEND,lipS[fi],ss);zz-=arc[fi]*(1-(2*a-1)*(2*a-1))*bend;}
   sv.Add(new Vector3(x,wy,zz));suv.Add(new Vector2(x/6f,zz/6f));}
  if(baseRow>=0)for(int c=0;c<Cols;c++){int a=baseRow+c,b=row+c;st.AddRange(new[]{a,b,a+1,a+1,b,b+1});}baseRow=row;if(ss>=sp[1])break;}}
var surf=AssetDatabase.LoadAssetAtPath<Mesh>(dir+"/B1CascadeSurface.asset");surf.Clear();surf.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;surf.SetVertices(sv);surf.SetUVs(0,suv);surf.SetTriangles(st,0);surf.RecalculateNormals();surf.RecalculateBounds();EditorUtility.SetDirty(surf);
// 2) 낙수면
for(int fi=1;fi<=2;fi++){float top=lipTop[fi-1],bottom=pool[fi-1]-0.6f;var lr=lip[fi];float l=lr[0]*0.9f,r=lr[1]*0.9f,c0=lr[2];float lipZ=Z0-lipS[fi-1];
 var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();const int Rows=24,C=18;
 for(int i=0;i<=Rows;i++){float k=i/(float)Rows;float y=top-0.05f-(top-bottom)*k*k;
  for(int c=0;c<=C;c++){float a=c/(float)C;float bulge=arc[fi-1]*(1-(2*a-1)*(2*a-1));float lead=smooth(0f,0.16f,k);
   float wob=Mathf.PerlinNoise(a*3.1f+fi*7f,k*2.3f)-0.5f;float z=lipZ+1.0f-(1.0f+bulge)*lead-thr[fi-1]*Mathf.Sqrt(k)+wob*0.35f*k;
   float x=Mathf.Lerp(c0-l,c0+r,a);if(c==0)x+=k*0.7f+wob*0.8f;if(c==C)x-=k*0.7f+wob*0.8f;
   v.Add(new Vector3(x,y,z));uv.Add(new Vector2(a,k));}}
 for(int i=0;i<Rows;i++)for(int c=0;c<C;c++){int a=i*(C+1)+c,b=a+C+1;t.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
 // 뒷면은 같은 정점(같은 법선)을 역순으로: 뒤집은 법선은 검게 보였고, 뒷면이 없으면 낙구가 유리 테처럼 보였다
 int nf=t.Count;for(int k=0;k<nf;k+=3)t.AddRange(new[]{t[k],t[k+2],t[k+1]});
 var m=AssetDatabase.LoadAssetAtPath<Mesh>(dir+"/B1Waterfall_"+fi+".asset");m.Clear();m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();EditorUtility.SetDirty(m);}
var fm=AssetDatabase.LoadAssetAtPath<Material>(dir+"/B1Waterfall_R3.mat");fm.SetFloat("_WaterfallEdgeFoamOpacity",0.22f);fm.SetFloat("_EdgeFoamOpacity",0.3f);EditorUtility.SetDirty(fm);
AssetDatabase.SaveAssets();return "ok";
