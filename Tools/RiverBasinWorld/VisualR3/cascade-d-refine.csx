// R3b 폭포 D단계(B·C의 수면/낙수면/암반을 대체, 지형은 rebuild 결과 유지).
// - 급류 수면을 s=-20(z=-65)부터 시작해 B1 수면의 직선 끝(지형보다 높이 뜬 각진 판)을 대체하고, B1 수면은 z<-65 삼각형을 제거
// - 낙구는 하류로 볼록한 호, 시작부는 위 수면 밑으로 0.6m 겹쳐 틈을 없앤다
// 실행 순서: cascade-rebuild.csx(지형 굴착·Rock 레이어) → 이 파일. cascade-a/b/c는 1·2차 시도이며 이 파일이 대체한다.
// - 낙수면 양 끝은 큰 바위로 덮어 사각 윤곽을 지운다. 모든 바위는 지형 높이 기준으로 묻는다(공중 선반 금지)
const float CX=307.2f,Z0=-85f,LEN=99.3f,SURF_START=-20f;
var prof=new float[,]{{0,72,2.4f},{12,71.6f,1.6f},{24,68.6f,0.6f},{24.01f,59.6f,2.6f},{46,59.6f,2.6f},{62,57.2f,0.6f},{62.01f,51f,2.4f},{82,51,2.2f},{99.3f,50,1.6f}};
Func<float,int,float> P=(s,c)=>{int n=prof.GetLength(0);if(s<=prof[0,0])return prof[0,c];for(int i=1;i<n;i++)if(s<=prof[i,0]){float t=(s-prof[i-1,0])/(prof[i,0]-prof[i-1,0]);return Mathf.Lerp(prof[i-1,c],prof[i,c],t);}return prof[n-1,c];};
Func<float,float> env=s=>Mathf.Sin(Mathf.Clamp01(s/LEN)*Mathf.PI);
Func<float,float> center=s=>CX+env(s)*(2.2f*Mathf.Sin(s*0.075f+0.6f)+1.4f*(Mathf.PerlinNoise(s*0.05f,3.3f)-0.5f)*2f);
string dir="Assets/10.Datas/World/RiverBasinWorld/VisualR3";
var terrain=Terrain.activeTerrains.First(t=>t.name=="Terrain_2_1");var o=terrain.transform.position;
Func<float,float,float> H=(x,z)=>terrain.SampleHeight(new Vector3(x,0,z))+o.y;
Func<float,float,float,float> reach=(s,water,sign)=>{float z=Z0-s,c0=center(s);float d=0.5f;while(d<18f&&H(c0+sign*d,z)<water+0.2f)d+=0.25f;return Mathf.Min(d+0.6f,18.6f);};
Func<string,Mesh> meshAsset=name=>{var p=dir+"/"+name+".asset";var m=AssetDatabase.LoadAssetAtPath<Mesh>(p);if(m==null){m=new Mesh();AssetDatabase.CreateAsset(m,p);}m.Clear();return m;};
var parent=GameObject.Find("GeneratedEnvironment/B1_ArtR1").transform;var old=parent.Find("B1 · 급류와 폭포");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
var root=new GameObject("B1 · 급류와 폭포");root.transform.SetParent(parent,false);Undo.RegisterCreatedObjectUndo(root,"R3b cascade refine");
Action<string,Mesh,Material> place=(n,m,mat)=>{var g=new GameObject(n);g.transform.SetParent(root.transform,false);g.AddComponent<MeshFilter>().sharedMesh=m;var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;};
// B1 수면 끝 제거
var b1=GameObject.Find("GeneratedEnvironment/B1_ArtR1/B1 · 굽은 수면");var bm=b1.GetComponent<MeshFilter>().sharedMesh;var bv=bm.vertices;var btri=bm.triangles;var bkeep=new List<int>();int bcut=0;
for(int k=0;k<btri.Length;k+=3){var c=b1.transform.TransformPoint((bv[btri[k]]+bv[btri[k+1]]+bv[btri[k+2]])/3f);if(c.z<Z0-SURF_START-0.01f&&c.z<-65f){bcut++;continue;}bkeep.Add(btri[k]);bkeep.Add(btri[k+1]);bkeep.Add(btri[k+2]);}
Undo.RecordObject(bm,"R3b b1 trim");bm.SetTriangles(bkeep,0);EditorUtility.SetDirty(bm);
// 급류 수면
var sv=new List<Vector3>();var st=new List<int>();var suv=new List<Vector2>();const int Cols=18;
var spans=new[]{new[]{SURF_START,24f},new[]{24.2f,62f},new[]{62.2f,LEN}};var lip=new Dictionary<int,float[]>();
foreach(var sp in spans){int baseRow=-1;for(float s=sp[0];;s+=0.75f){float ss=Mathf.Min(s,sp[1]);float z=Z0-ss;float wy=P(ss,1);float l=reach(ss,wy,-1),r=reach(ss,wy,1);float c0=center(ss);
  if(Mathf.Abs(ss-24f)<0.01f)lip[1]=new[]{l,r,c0};if(Mathf.Abs(ss-62f)<0.01f)lip[2]=new[]{l,r,c0};
  int row=sv.Count;for(int c=0;c<=Cols;c++){float x=Mathf.Lerp(c0-l,c0+r,c/(float)Cols);sv.Add(new Vector3(x,wy,z));suv.Add(new Vector2(x/6f,z/6f));}
  if(baseRow>=0)for(int c=0;c<Cols;c++){int a=baseRow+c,b=row+c;st.AddRange(new[]{a,b,a+1,a+1,b,b+1});}baseRow=row;if(ss>=sp[1])break;}}
var surf=meshAsset("B1CascadeSurface");surf.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;surf.SetVertices(sv);surf.SetUVs(0,suv);surf.SetTriangles(st,0);surf.RecalculateNormals();surf.RecalculateBounds();EditorUtility.SetDirty(surf);
place("급류 수면",surf,AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/RiverBasinWorld/VisualR1/B1RiverWater.mat"));
// 낙수면
var fallMat=AssetDatabase.LoadAssetAtPath<Material>(dir+"/B1Waterfall_R3.mat");
fallMat.SetColor("_Color1",new Color(.14f,.31f,.31f,0));fallMat.SetColor("_Color2",new Color(.19f,.38f,.37f,0));fallMat.SetColor("_Color3",new Color(.50f,.63f,.60f,0));fallMat.SetColor("_Color4",new Color(.92f,.95f,.92f,0));EditorUtility.SetDirty(fallMat);
var lipTop=new[]{68.6f,57.2f};var pool=new[]{59.6f,51f};var thr=new[]{1.9f,1.4f};var arc=new[]{1.4f,1.0f};var lipS=new[]{24f,62f};
for(int fi=1;fi<=2;fi++){float top=lipTop[fi-1]+0.05f,bottom=pool[fi-1]-0.6f;var lr=lip[fi];float l=lr[0]*0.9f,r=lr[1]*0.9f,c0=lr[2];float lipZ=Z0-lipS[fi-1];
 var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();const int Rows=22,C=18;
 for(int i=0;i<=Rows;i++){float k=i/(float)Rows;float y=top-(top-bottom)*k*k;
  for(int c=0;c<=C;c++){float a=c/(float)C;float bulge=arc[fi-1]*(1-(2*a-1)*(2*a-1));float lead=Mathf.Clamp01(k/0.12f);
   float wob=Mathf.PerlinNoise(a*3.1f+fi*7f,k*2.3f)-0.5f;float z=lipZ+0.6f-(0.6f+bulge)*lead-thr[fi-1]*Mathf.Sqrt(k)+wob*0.35f*k;
   float x=Mathf.Lerp(c0-l,c0+r,a);if(c==0)x+=k*0.7f+wob*0.8f;if(c==C)x-=k*0.7f+wob*0.8f;
   v.Add(new Vector3(x,y+(Mathf.PerlinNoise(a*4.3f+fi*5f,0.5f)-0.5f)*0.3f*(1-k),z));uv.Add(new Vector2(a,k));}}
 for(int i=0;i<Rows;i++)for(int c=0;c<C;c++){int a=i*(C+1)+c,b=a+C+1;t.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
 // 뒷면은 두지 않는다: 위에서 보면 뒷면이 검게 떠 보였고, 뒤에서는 떨어지는 물 너머로 소와 물보라가 보이는 편이 자연스럽다.
 var m=meshAsset("B1Waterfall_"+fi);m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();EditorUtility.SetDirty(m);
 place("낙수면 "+fi,m,fallMat);}
// 암반·물보라
var rockRoot=new GameObject("암반").transform;rockRoot.SetParent(root.transform,false);var fxRoot=new GameObject("물보라").transform;fxRoot.SetParent(root.transform,false);
var rnd=new System.Random(3107);Func<float,float,float> R=(a,b)=>a+(float)rnd.NextDouble()*(b-a);
string rp="Assets/03.Prefabs/Environment/RiverBasinWorld/VisualR1/PF_EastLands_RiverRock_0";var rocks=new[]{rp+"1_Gold_17.prefab",rp+"2_Gold_16.prefab",rp+"3_Gold_14.prefab",rp+"4_Gold_15.prefab"}.Select(p=>AssetDatabase.LoadAssetAtPath<GameObject>(p)).ToArray();
var rockH=new[]{1.55f,1.1f,1.06f,0.82f};int count=0;
// sink: 바위 높이 대비 묻히는 비율. 지형 높이 기준으로 둔다.
Action<int,Vector3,float,float,string> put=(ri,pos,scale,sink,name)=>{var g=(GameObject)PrefabUtility.InstantiatePrefab(rocks[ri],rockRoot);g.name=name;float gy=Mathf.Min(H(pos.x,pos.z),pos.y);g.transform.position=new Vector3(pos.x,gy-rockH[ri]*scale*sink,pos.z);g.transform.rotation=Quaternion.Euler(R(-10,10),R(0,360),R(-10,10));g.transform.localScale=new Vector3(scale*R(0.9f,1.2f),scale*R(0.85f,1.1f),scale*R(0.9f,1.2f));count++;};
for(int f=0;f<2;f++){var lr=lip[f+1];float l=lr[0]*0.9f,r=lr[1]*0.9f,c0=lr[2];float lz=Z0-lipS[f];
 foreach(var side in new[]{-1f,1f}){float e=c0+side*(side<0?l:r);
  // 낙수면 끝을 덮는 바위: 낙구 높이, 중간, 바닥
  put(0,new Vector3(e+side*0.6f,lipTop[f]+0.4f,lz+0.2f),R(2.6f,3.2f),0.35f,"낙구 끝 바위");
  put(rnd.Next(2),new Vector3(e+side*0.3f,(lipTop[f]+pool[f])*0.5f,lz-thr[f]*0.6f),R(2.2f,2.8f),0.3f,"낙수 옆 바위");
  put(rnd.Next(4),new Vector3(e-side*0.4f,pool[f]+0.2f,lz-thr[f]-0.8f),R(1.8f,2.4f),0.45f,"소 가장자리 바위");
  put(rnd.Next(4),new Vector3(e+side*R(2.5f,4.5f),lipTop[f]+3f,lz+R(-2f,2f)),R(1.6f,2.4f),0.4f,"강둑 바위");}
 put(1,new Vector3(c0-l*R(0.3f,0.45f),lipTop[f]+0.2f,lz+0.5f),R(1.3f,1.7f),0.4f,"물줄기 가르는 바위");
 for(int k=0;k<3;k++)put(rnd.Next(4),new Vector3(c0+R(-0.7f,0.7f)*l,pool[f],lz-thr[f]-R(4f,12f)),R(1.1f,2f),0.5f,"소 바위");
 var splash=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalAssets/Environment/PolyartStudio/SharedResources/Effects/P_WaterSplash.prefab");
 var mist=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalAssets/Environment/PolyartStudio/SharedResources/Effects/P_WaterMist.prefab");
 int ns=f==0?4:3;for(int k=0;k<ns;k++){float a=(k+0.5f)/ns;float bz=lz-thr[f]-arc[f]*(1-(2*a-1)*(2*a-1))-0.5f;var g=(GameObject)PrefabUtility.InstantiatePrefab(splash,fxRoot);g.name="낙수 물보라";g.transform.position=new Vector3(Mathf.Lerp(c0-l*0.75f,c0+r*0.75f,a),pool[f]+0.2f,bz);}
 var mg=(GameObject)PrefabUtility.InstantiatePrefab(mist,fxRoot);mg.name="낙수 안개";mg.transform.position=new Vector3(c0,pool[f]+1f,lz-thr[f]-arc[f]-1.5f);mg.transform.localScale=Vector3.one*(f==0?1.4f:1.1f);}
foreach(var span in new[]{new[]{6f,22f,1f},new[]{44f,60f,2f}}){for(int k=0;k<5;k++){float s=R(span[0],span[1]);float z=Z0-s;float c0=center(s);float side=rnd.NextDouble()<0.5?-1:1;float x=c0+side*R(4f,8f);put(rnd.Next(4),new Vector3(x,P(s,1)+2f,z),R(0.9f,1.7f),0.35f,"급류 바위");}}
System.IO.File.WriteAllText("output/RiverBasinWorld/VisualR3/cascade-lips.txt","lip1 "+string.Join(",",lip[1])+"\nlip2 "+string.Join(",",lip[2]));
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);AssetDatabase.SaveAssets();return "b1Cut "+bcut+" rocks "+count;
