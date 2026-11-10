// R3b 폭포 재구축(A·B단계 대체). 1차 결과는 곧은 사다리꼴 도랑과 반복 UV 이음새가 인공적이었다.
// 1) backup-r3b의 원본 높이를 복원 2) 굽이(중심선 사행)와 좌우 비대칭 폭 노이즈로 다시 굴착
// 3) 굴착 범위의 급경사면에 Rock 레이어 4) 수면·낙수면 재생성(낙수면 u는 반드시 0~1: 셰이더가 u 양끝을 투명 처리)
const float CX=307.2f,Z0=-85f,LEN=99.3f;
var prof=new float[,]{{0,72,2.4f},{12,71.6f,1.6f},{24,68.6f,0.6f},{24.01f,59.6f,2.6f},{46,59.6f,2.6f},{62,57.2f,0.6f},{62.01f,51f,2.4f},{82,51,2.2f},{99.3f,50,1.6f}};
Func<float,int,float> P=(s,c)=>{int n=prof.GetLength(0);if(s<=prof[0,0])return prof[0,c];for(int i=1;i<n;i++)if(s<=prof[i,0]){float t=(s-prof[i-1,0])/(prof[i,0]-prof[i-1,0]);return Mathf.Lerp(prof[i-1,c],prof[i,c],t);}return prof[n-1,c];};
Func<float,float> env=s=>Mathf.Sin(Mathf.Clamp01(s/LEN)*Mathf.PI);
Func<float,float> center=s=>CX+env(s)*(2.2f*Mathf.Sin(s*0.075f+0.6f)+1.4f*(Mathf.PerlinNoise(s*0.05f,3.3f)-0.5f)*2f);
// 낙구 부근은 폭을 좁혀(암반 사이로 물이 모이는 형태) 폭포 폭을 정한다.
Func<float,float> pinch=s=>1f-0.22f*Mathf.Exp(-Mathf.Pow((s-24f)/5f,2))-0.25f*Mathf.Exp(-Mathf.Pow((s-62f)/4.5f,2));
Func<float,float,float> widthScale=(s,side)=>(1f+env(s)*0.32f*(Mathf.PerlinNoise(s*0.065f+side*17.3f,8.1f)-0.5f)*2f)*pinch(s);
var log=new System.Text.StringBuilder();
string dir="Assets/10.Datas/World/RiverBasinWorld/VisualR3";
// 1) 높이 복원
var terrain=Terrain.activeTerrains.First(t=>t.name=="Terrain_2_1");var data=terrain.terrainData;var o=terrain.transform.position;
string tmp=dir+"/_restore_tmp.asset";System.IO.File.Copy("output/RiverBasinWorld/VisualR3/backup-r3b/Terrain_2_1.asset",tmp,true);AssetDatabase.ImportAsset(tmp);
var orig=AssetDatabase.LoadAssetAtPath<TerrainData>(tmp);
int res=data.heightmapResolution;float sx=data.size.x/(res-1),sz=data.size.z/(res-1),hy=data.size.y;
int ax=Mathf.FloorToInt((CX-26-o.x)/sx),bx=Mathf.CeilToInt((CX+26-o.x)/sx),az=Mathf.FloorToInt((Z0-LEN-4-o.z)/sz),bz=Mathf.CeilToInt((Z0+4-o.z)/sz);
int w=bx-ax+1,h=bz-az+1;var hm=orig.GetHeights(ax,az,w,h);AssetDatabase.DeleteAsset(tmp);
// 2) 굴착
int changed=0;
for(int j=0;j<h;j++)for(int i=0;i<w;i++){float x=o.x+(ax+i)*sx,z=o.z+(az+j)*sz;float s=Z0-z;if(s<-3||s>LEN+2.7f)continue;
 float edge=Mathf.Clamp01(Mathf.Min(s+3,LEN+2.7f-s)/3f);float sc=Mathf.Clamp(s,0,LEN);float dxs=x-center(sc);float side=dxs<0?-1:1;
 float dx=Mathf.Abs(dxs)/widthScale(sc,side);float water=P(sc,1),depth=P(sc,2);
 float bed=water-depth+depth*1.15f*(dx/9f)*(dx/9f);float cur=hm[j,i]*hy+o.y;if(bed>=cur)continue;
 hm[j,i]=(Mathf.Lerp(cur,bed,edge)-o.y)/hy;changed++;}
Undo.RegisterCompleteObjectUndo(data,"R3b cascade rebuild");data.SetHeights(ax,az,hm);
// 3) 급경사 Rock 레이어(굴착 범위 ±20m)
int rockLayer=3;int ares=data.alphamapResolution;float acell=data.size.x/ares;
int aax=Mathf.FloorToInt((CX-22-o.x)/acell),abx=Mathf.CeilToInt((CX+22-o.x)/acell),aaz=Mathf.FloorToInt((Z0-LEN-2-o.z)/acell),abz=Mathf.CeilToInt((Z0+2-o.z)/acell);
var am=data.GetAlphamaps(aax,aaz,abx-aax+1,abz-aaz+1);int layers=data.alphamapLayers;int painted=0;
for(int j=0;j<am.GetLength(0);j++)for(int i=0;i<am.GetLength(1);i++){float x=o.x+(aax+i+0.5f)*acell,z=o.z+(aaz+j+0.5f)*acell;float s=Z0-z;if(s<2||s>LEN-2)continue;
 float steep=data.GetSteepness((x-o.x)/data.size.x,(z-o.z)/data.size.z);
 float lipNear=Mathf.Max(Mathf.Exp(-Mathf.Pow((s-25f)/9f,2)),Mathf.Exp(-Mathf.Pow((s-63f)/8f,2)));
 float rw=Mathf.Clamp01((steep-24f)/14f)*(0.55f+0.45f*lipNear);
 rw*=Mathf.Lerp(0.6f,1f,Mathf.PerlinNoise(x*0.21f,z*0.21f));if(rw<=0.02f)continue;
 for(int l=0;l<layers;l++)am[j,i,l]*=1-rw;am[j,i,rockLayer]+=rw;painted++;}
data.SetAlphamaps(aax,aaz,am);EditorUtility.SetDirty(data);
log.AppendLine("carved "+changed+" rockPainted "+painted);
// 4) 수면·낙수면
Func<float,float,float> H=(x,z)=>terrain.SampleHeight(new Vector3(x,0,z))+o.y;
Func<float,float,float,float> reach=(s,water,sign)=>{float z=Z0-s,c0=center(s);float d=0.5f;while(d<16f&&H(c0+sign*d,z)<water+0.2f)d+=0.25f;return Mathf.Min(d+0.6f,16.6f);};
Func<string,Mesh> meshAsset=name=>{var p=dir+"/"+name+".asset";var m=AssetDatabase.LoadAssetAtPath<Mesh>(p);if(m==null){m=new Mesh();AssetDatabase.CreateAsset(m,p);}m.Clear();return m;};
var parent=GameObject.Find("GeneratedEnvironment/B1_ArtR1").transform;var old=parent.Find("B1 · 급류와 폭포");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
var root=new GameObject("B1 · 급류와 폭포");root.transform.SetParent(parent,false);Undo.RegisterCreatedObjectUndo(root,"R3b cascade");
Action<string,Mesh,Material> place=(n,m,mat)=>{var g=new GameObject(n);g.transform.SetParent(root.transform,false);g.AddComponent<MeshFilter>().sharedMesh=m;var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;};
var sv=new List<Vector3>();var st=new List<int>();var suv=new List<Vector2>();const int Cols=16;
var spans=new[]{new[]{0f,24f},new[]{24.2f,62f},new[]{62.2f,LEN}};var lip=new Dictionary<int,float[]>();
foreach(var sp in spans){int baseRow=-1;for(float s=sp[0];;s+=0.75f){float ss=Mathf.Min(s,sp[1]);float z=Z0-ss;float wy=P(ss,1);float l=reach(ss,wy,-1),r=reach(ss,wy,1);float c0=center(ss);
  if(Mathf.Abs(ss-24f)<0.01f)lip[1]=new[]{l,r,c0};if(Mathf.Abs(ss-62f)<0.01f)lip[2]=new[]{l,r,c0};
  int row=sv.Count;for(int c=0;c<=Cols;c++){float x=Mathf.Lerp(c0-l,c0+r,c/(float)Cols);sv.Add(new Vector3(x,wy,z));suv.Add(new Vector2(x/6f,z/6f));}
  if(baseRow>=0)for(int c=0;c<Cols;c++){int a=baseRow+c,b=row+c;st.AddRange(new[]{a,b,a+1,a+1,b,b+1});}baseRow=row;if(ss>=sp[1])break;}}
var surf=meshAsset("B1CascadeSurface");surf.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;surf.SetVertices(sv);surf.SetUVs(0,suv);surf.SetTriangles(st,0);surf.RecalculateNormals();surf.RecalculateBounds();EditorUtility.SetDirty(surf);
place("급류 수면",surf,AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/RiverBasinWorld/VisualR1/B1RiverWater.mat"));
var fallMat=AssetDatabase.LoadAssetAtPath<Material>(dir+"/B1Waterfall_R3.mat");
fallMat.SetColor("_Color1",new Color(.12f,.36f,.34f,0));fallMat.SetColor("_Color2",new Color(.17f,.44f,.41f,0));fallMat.SetColor("_Color3",new Color(.48f,.66f,.62f,0));fallMat.SetColor("_Color4",new Color(.93f,.96f,.93f,0));
fallMat.SetFloat("_SmallDots2Opacity",.55f);fallMat.SetFloat("_NoiseLinesOpacity",1.6f);EditorUtility.SetDirty(fallMat);
var falls=new[]{new[]{24f,68.6f,59.6f,1.9f},new[]{62f,57.2f,51f,1.4f}};
for(int fi=1;fi<=2;fi++){var f=falls[fi-1];float s0=f[0],top=f[1]+0.05f,bottom=f[2]-0.6f,thr=f[3];var lr=lip[fi];float l=lr[0]*0.92f,r=lr[1]*0.92f,c0=lr[2];float lipZ=Z0-s0;
 var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();const int Rows=20,C=16;
 for(int i=0;i<=Rows;i++){float k=i/(float)Rows;float y=top-(top-bottom)*k*k;float z=lipZ-thr*Mathf.Sqrt(k);
  for(int c=0;c<=C;c++){float a=c/(float)C;float lipBump=(Mathf.PerlinNoise(a*4.3f+fi*5f,0.5f)-0.5f)*0.35f;float wob=(Mathf.PerlinNoise(a*3.1f+fi*7f,k*2.3f)-0.5f);
   float x=Mathf.Lerp(c0-l,c0+r,a);if(c==0)x+=k*0.6f+wob*0.8f;if(c==C)x-=k*0.6f+wob*0.8f;
   v.Add(new Vector3(x,y+lipBump*(1-k),z+wob*0.35f*k));uv.Add(new Vector2(a,k));}}
 for(int i=0;i<Rows;i++)for(int c=0;c<C;c++){int a=i*(C+1)+c,b=a+C+1;t.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
 int n=v.Count;var back=new List<int>();for(int k=0;k<t.Count;k+=3)back.AddRange(new[]{t[k]+n,t[k+2]+n,t[k+1]+n});v.AddRange(v.ToArray());uv.AddRange(uv.ToArray());t.AddRange(back);
 var m=meshAsset("B1Waterfall_"+fi);m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();EditorUtility.SetDirty(m);
 place("낙수면 "+fi,m,fallMat);
 log.AppendLine("fall"+fi+" center "+c0.ToString("F2")+" width "+(l+r).ToString("F1"));}
System.IO.File.WriteAllText("output/RiverBasinWorld/VisualR3/cascade-lips.txt","lip1 "+string.Join(",",lip[1])+"\nlip2 "+string.Join(",",lip[2]));
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);AssetDatabase.SaveAssets();return log.ToString();
