// R3f B1 강 재구성(z 65 → -184): 상류 구간은 직선 중심선·일정 폭·얕은 수심(1~2m)이라 인위적이고 수영이 불가능했다.
// - 사행 중심선, 좌우 비대칭 폭, 넓고 깊은 소(4.2m) ↔ 좁은 여울(1.8m), 다리 밑은 교각 바닥(71.5m) 때문에 0.6m 여울
// - 폭포 아래 소를 4.2m / 3.8m로 심화(낮추기만)
// - B1 수면과 급류 수면을 하나의 수면으로 대체하고 FootstepWaterSurface를 붙여 수영·물 발소리를 켠다(맵에 등록 수면이 0개였다)
// - 낙구 직전 3.5m는 UV(LakeWater _ShoreFade)로 투명하게 해 낙수면과의 이음선을 없앤다(굴절 합성이 투명 낙수면을 지워 선이 생겼다)
// 좌표: s = -85 - z (폭포 구간 기준). 상류는 s<0, 수면 72m.
const float CX=307.2f,Z0=-85f,LEN=99.3f,S_TOP=-150f,BRIDGE_Z=-15.36f;
var cprof=new float[,]{{0,72},{12,71.6f},{24,68.6f},{24.01f,59.6f},{46,59.6f},{62,57.2f},{62.01f,51f},{82,51},{99.3f,50}};
Func<float,float,float,float> smooth=(a,b,x)=>{var t=Mathf.Clamp01((x-a)/(b-a));return t*t*(3-2*t);};
Func<float,float> W=s=>{if(s<=0)return 72f;int n=cprof.GetLength(0);for(int i=1;i<n;i++)if(s<=cprof[i,0]){float t=(s-cprof[i-1,0])/(cprof[i,0]-cprof[i-1,0]);return Mathf.Lerp(cprof[i-1,1],cprof[i,1],t);}return cprof[n-1,1];};
Func<float,float> envC=s=>Mathf.Sin(Mathf.Clamp01(s/LEN)*Mathf.PI);
Func<float,float> bridgeMask=s=>smooth(7f,18f,Mathf.Abs((Z0-s)-BRIDGE_Z));
Func<float,float> envU=s=>Mathf.Sin(Mathf.Clamp01((s-S_TOP)/(-S_TOP))*Mathf.PI)*bridgeMask(s);
Func<float,float> center=s=>s>=0?CX+envC(s)*(2.2f*Mathf.Sin(s*0.075f+0.6f)+1.4f*(Mathf.PerlinNoise(s*0.05f,3.3f)-0.5f)*2f)
 :CX+envU(s)*(4.5f*Mathf.Sin(s*0.05f+1.3f)+1.6f*(Mathf.PerlinNoise(s*0.04f,7.7f)-0.5f)*2f);
Func<float,float,float> gauss=(x,sg)=>Mathf.Exp(-(x*x)/(2*sg*sg));
// 상류 소: z=30(s=-115), z=-52(s=-33). 다리 밑(s=-69.6)은 좁고 얕다.
Func<float,float,float> halfW=(s,side)=>{float b=6.5f+5.5f*gauss(s+115f,13f)+5f*gauss(s+33f,10f);b=Mathf.Lerp(Mathf.Min(b,8.5f),b,bridgeMask(s));
 return b*(1f+0.22f*(Mathf.PerlinNoise(s*0.06f+side*19f,4.4f)-0.5f)*2f);};
Func<float,float> depthU=s=>{float dep=1.8f+2.4f*Mathf.Max(gauss(s+115f,11f),gauss(s+33f,9f));return Mathf.Lerp(0.6f,dep,bridgeMask(s));};
var log=new System.Text.StringBuilder();string bdir="output/RiverBasinWorld/VisualR3/backup-r3b";
var terrain=Terrain.activeTerrains.First(t=>t.name=="Terrain_2_1");var d=terrain.terrainData;var o=terrain.transform.position;
var ap=AssetDatabase.GetAssetPath(d);var bk=bdir+"/pre-reshape-Terrain_2_1.asset";if(!System.IO.File.Exists(bk))System.IO.File.Copy(ap,bk);
int res=d.heightmapResolution;float hy=d.size.y;
int ax=(int)(CX-36-o.x),bx=(int)(CX+36-o.x),az=(int)(Z0-LEN-2-o.z),bz=(int)(Z0-S_TOP+2-o.z);
var hm=d.GetHeights(ax,az,bx-ax+1,bz-az+1);var before=(float[,])hm.Clone();int up=0,low=0;
for(int j=0;j<hm.GetLength(0);j++)for(int i=0;i<hm.GetLength(1);i++){float x=o.x+ax+i,z=o.z+az+j;float s=Z0-z;float cur=hm[j,i]*hy+o.y;
 if(s<=2f){ // 상류 재구성
  float wgt=Mathf.Min(smooth(S_TOP,S_TOP+6f,s),1-smooth(-6f,2f,s));if(wgt<=0)continue;
  float c=center(s);float dxs=x-c;float side=dxs<0?-1:1;float hw=halfW(s,side);float adx=Mathf.Abs(dxs);float water=W(s);float tgt;
  if(adx<hw){float q=1-(adx/hw)*(adx/hw);tgt=water-depthU(s)*Mathf.Pow(q,0.8f);}
  else{float slope=0.42f+0.25f*Mathf.PerlinNoise(x*0.09f,z*0.09f);float bank=water+0.3f+(adx-hw)*slope;
   float ridge=(1-Mathf.Abs(2*Mathf.PerlinNoise(x*0.17f+5f,z*0.17f+1f)-1))*0.6f+Mathf.PerlinNoise(x*0.07f+30f,z*0.07f)*0.5f-0.55f;
   bank+=ridge*smooth(0.5f,2.5f,adx-hw);
   if(cur>=bank)continue;float reachW=1-smooth(hw+7f,hw+15f,adx);if(reachW<=0)continue;tgt=Mathf.Lerp(cur,bank,reachW);}
  float nw=Mathf.Lerp(cur,tgt,wgt);if(Mathf.Abs(nw-cur)<0.003f)continue;if(nw>cur)up++;else low++;hm[j,i]=(nw-o.y)/hy;}
 else{ // 폭포 아래 소 심화(낮추기만)
  float c=center(s);float adx=Mathf.Abs(x-c);float dp=4.2f*smooth(24.2f,28f,s)*(1-smooth(42f,47f,s))+3.8f*smooth(62.2f,66f,s)*(1-smooth(78f,84f,s));if(dp<=0)continue;
  float hwc=8.5f;if(adx>=hwc)continue;float tgt=W(s)-dp*Mathf.Pow(1-(adx/hwc)*(adx/hwc),0.8f);if(tgt<cur){hm[j,i]=(tgt-o.y)/hy;low++;}}}
Undo.RegisterCompleteObjectUndo(d,"R3f river reshape");d.SetHeights(ax,az,hm);EditorUtility.SetDirty(d);
log.AppendLine("terrain raised "+up+" lowered "+low);
// 오브젝트 재안착(다리·폭포 구성·대형 메시 제외)
var env=GameObject.Find("GeneratedEnvironment").transform;int moved=0;var stack=new Stack<Transform>();stack.Push(env);
Func<float,float,float> deltaAt=(x,z)=>{int i=Mathf.RoundToInt(x-o.x)-ax,j=Mathf.RoundToInt(z-o.z)-az;if(i<0||j<0||j>=hm.GetLength(0)||i>=hm.GetLength(1))return 0;return (hm[j,i]-before[j,i])*hy;};
while(stack.Count>0){var tr=stack.Pop();foreach(Transform ch in tr){if(ch.GetComponent<Terrain>()!=null||ch.name.Contains("Bridge")||ch.name.Contains("급류와 폭포"))continue;
 bool leaf=PrefabUtility.IsOutermostPrefabInstanceRoot(ch.gameObject);var rr=ch.GetComponent<Renderer>();if(!leaf&&rr!=null&&ch.childCount==0)leaf=rr.bounds.extents.magnitude<15f;
 if(!leaf){if(rr==null||ch.childCount>0)stack.Push(ch);continue;}
 float dl=deltaAt(ch.position.x,ch.position.z);if(Mathf.Abs(dl)<0.03f)continue;Undo.RecordObject(ch,"R3f resnap");ch.position+=Vector3.up*dl;moved++;}}
log.AppendLine("resnapped "+moved);
// 통합 수면
Func<float,float,float> H=(x,z)=>terrain.SampleHeight(new Vector3(x,0,z))+o.y;
Func<float,float,float,float> reach=(s,water,sign)=>{float z=Z0-s,c0=center(s);float dd=0.5f;while(dd<20f&&H(c0+sign*dd,z)<water+0.2f)dd+=0.25f;return Mathf.Min(dd+0.6f,20.6f);};
var lipS=new[]{24f,62f};var arc=new[]{1.4f,1.0f};const float BEND=2.5f,FADE=3.5f;
var sv=new List<Vector3>();var st=new List<int>();var suv=new List<Vector2>();const int Cols=20;
var spans=new[]{new[]{S_TOP+0.5f,24f},new[]{24.2f,62f},new[]{62.2f,LEN}};
foreach(var sp in spans){int baseRow=-1;for(float s=sp[0];;s+=0.75f){float ss=Mathf.Min(s,sp[1]);float z=Z0-ss;float wy=W(ss);float l=reach(ss,wy,-1),r=reach(ss,wy,1);float c0=center(ss);
  int fi=-1;for(int f=0;f<2;f++)if(ss>lipS[f]-FADE-0.01f&&ss<=lipS[f]+0.01f)fi=f;
  int row=sv.Count;for(int c=0;c<=Cols;c++){float a=c/(float)Cols;float x=Mathf.Lerp(c0-l,c0+r,a);float zz=z;float fadeT=0;
   if(fi>=0){float bend=smooth(lipS[fi]-BEND,lipS[fi],ss);zz-=arc[fi]*(1-(2*a-1)*(2*a-1))*bend;fadeT=smooth(lipS[fi]-FADE,lipS[fi],ss);}
   sv.Add(new Vector3(x,wy,zz));suv.Add(new Vector2(0.5f,0.5f+0.5f*fadeT));}
  if(baseRow>=0)for(int c=0;c<Cols;c++){int a=baseRow+c,b=row+c;st.AddRange(new[]{a,b,a+1,a+1,b,b+1});}baseRow=row;if(ss>=sp[1])break;}}
string dir="Assets/10.Datas/World/RiverBasinWorld/VisualR3";var mpth=dir+"/B1RiverUnified.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(mpth);if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,mpth);}
mesh.Clear();mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.SetVertices(sv);mesh.SetUVs(0,suv);mesh.SetTriangles(st,0);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
var matPath=dir+"/B1RiverUnified_R3.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(mat==null){AssetDatabase.CopyAsset("Assets/10.Datas/World/RiverBasinWorld/VisualR1/B1RiverWater.mat",matPath);mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);}
mat.SetFloat("_ShoreFade",0.5f);mat.SetFloat("_Recession",0f);EditorUtility.SetDirty(mat);
var art=GameObject.Find("GeneratedEnvironment/B1_ArtR1").transform;var go=art.Find("B1 · 강 수면")?.gameObject;if(go==null){go=new GameObject("B1 · 강 수면");go.transform.SetParent(art,false);Undo.RegisterCreatedObjectUndo(go,"R3f river");}
var mf=go.GetComponent<MeshFilter>();if(mf==null)mf=go.AddComponent<MeshFilter>();mf.sharedMesh=mesh;var mr=go.GetComponent<MeshRenderer>();if(mr==null)mr=go.AddComponent<MeshRenderer>();mr.sharedMaterial=mat;mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
var fws=go.GetComponent<UPlayGround.Components.FootstepWaterSurface>();if(fws==null)fws=go.AddComponent<UPlayGround.Components.FootstepWaterSurface>();fws.surfaceMesh=mf;
fws.profile=AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.FootstepSurfaceProfileSO>(AssetDatabase.GUIDToAssetPath("b5646ecc442e5f7429609efbcba9b53d"));fws.maximumDepth=0.55f;fws.minimumDepth=0.015f;
// 대체된 수면은 비활성(에셋 보존)
foreach(var n in new[]{"B1 · 굽은 수면","B1 · 급류와 폭포/급류 수면"}){var t=art.Find(n);if(t!=null){Undo.RecordObject(t.gameObject,"R3f off");t.gameObject.SetActive(false);}}
// 수심 표본(수영 진입 1.2m 기준)
var sb=new System.Text.StringBuilder();foreach(var s in new[]{-115f,-90f,-69.6f,-33f,-10f,10f,35f,72f}){float z=Z0-s,c0=center(s);sb.Append("s"+s+"(z"+z.ToString("F0")+") depth "+(W(s)-H(c0,z)).ToString("F2")+" w "+(reach(s,W(s),-1)+reach(s,W(s),1)).ToString("F1")+"; ");}
log.AppendLine(sb.ToString()+" profile "+(fws.profile?fws.profile.name:"null"));
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);AssetDatabase.SaveAssets();return log.ToString();
