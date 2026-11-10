// R3b 지면 정리: (1) 원형 흙 패드 가장자리 풀기 (2) 길 회랑 절토가 만든 날카로운 언덕 경계 완화 (3) 바뀐 높이에 오브젝트 재안착.
// 원판의 원인은 production-plan.json의 pads(타원 Footpath)와 공터 오버레이 ClearingGravel이다. R3에서 넣은 오버레이 노이즈 패치는 얼룩이 되어 원본으로 되돌린 뒤 가장자리만 일그러뜨린다.
// 꺾임 완화는 1m 월드 격자에서 수행해 Terrain 경계 표본(양쪽 타일이 같은 월드 좌표)을 같은 값으로 쓴다.
// 보호: 길 중심 4m(7m까지 페이드), 강 중심선 14m, B1 다리 25m, 패드 내부. 강 폭포 구간은 강 보호로 제외된다.
const float GX0=150,GX1=480,GZ0=-260,GZ1=160;
// 입력: plan-lines.txt (production-plan.json에서 node로 추출한 route/river/pad 좌표)
var lines=System.IO.File.ReadAllLines("Tools/RiverBasinWorld/VisualR3/plan-lines.txt");var inv=System.Globalization.CultureInfo.InvariantCulture;
Func<string,Vector2[]> pts=s=>s.Split(' ').Skip(1).Select(q=>{var a=q.Split(',');return new Vector2(float.Parse(a[0],inv),float.Parse(a[1],inv));}).ToArray();
var routes=lines.Where(l=>l.StartsWith("route ")).Select(pts).ToList();var river=pts(lines.First(l=>l.StartsWith("river ")));
var pads=lines.Where(l=>l.StartsWith("pad ")).Select(l=>{var a=l.Substring(4).Split(',').Select(q=>float.Parse(q,inv)).ToArray();return new Vector4(a[0],a[1],a[2],a[3]);}).Where(p=>p.x>GX0&&p.x<GX1&&p.y>GZ0&&p.y<GZ1).ToArray();
Func<Vector2,Vector2,Vector2,float> segDist=(p,a,b)=>{var ab=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,ab)/Mathf.Max(ab.sqrMagnitude,1e-4f));return Vector2.Distance(p,a+ab*t);};
Func<Vector2[],Vector2,float> lineDist=(line,p)=>{float d=1e9f;for(int i=1;i<line.Length;i++)d=Mathf.Min(d,segDist(p,line[i-1],line[i]));return d;};
Func<float,float,float,float> smooth=(a,b,x)=>{var t=Mathf.Clamp01((x-a)/(b-a));return t*t*(3-2*t);};
Func<float,float,Vector4,float> padDist=(x,z,pd)=>{float dx=(x-pd.x)/pd.z,dz=(z-pd.y)/pd.w;float d=Mathf.Sqrt(dx*dx+dz*dz);float warp=1+0.32f*(Mathf.PerlinNoise(x*0.06f+pd.x,z*0.06f+pd.y)-0.5f)*2+0.12f*(Mathf.PerlinNoise(x*0.19f,z*0.19f)-0.5f)*2;return d*warp;};
var log=new System.Text.StringBuilder();string bdir="output/RiverBasinWorld/VisualR3/backup-r3b";
// (1-a) 공터 오버레이: 원본 알파 복원 후 가장자리만 일그러뜨림
var cg=GameObject.Find("GeneratedEnvironment/B1_ArtR1/지표와 미세 자갈/ClearingGravel");var cmesh=cg.GetComponent<MeshFilter>().sharedMesh;
string tmp="Assets/10.Datas/World/RiverBasinWorld/VisualR3/_clearing_tmp.asset";System.IO.File.Copy("output/RiverBasinWorld/VisualR3/backup/ClearingGravel.asset",tmp,true);AssetDatabase.ImportAsset(tmp);
var origColors=AssetDatabase.LoadAssetAtPath<Mesh>(tmp).colors;var cv=cmesh.vertices;var cols=new Color[origColors.Length];
var mainPad=pads.OrderBy(p=>Vector2.Distance(new Vector2(p.x,p.y),new Vector2(263,1))).First();
for(int i=0;i<cv.Length;i++){var w=cg.transform.TransformPoint(cv[i]);var c=origColors[i];float d=padDist(w.x,w.z,mainPad);c.a*=1-smooth(0.55f,1.0f,d);cols[i]=c;}
Undo.RecordObject(cmesh,"R3b clearing edge");cmesh.colors=cols;EditorUtility.SetDirty(cmesh);
// 높이·스플랫 작업 대상 Terrain
var terrains=Terrain.activeTerrains.Where(t=>{var o=t.transform.position;return o.x<GX1&&o.x+512>GX0&&o.z<GZ1&&o.z+512>GZ0;}).ToArray();
foreach(var t in terrains){var ap=AssetDatabase.GetAssetPath(t.terrainData);var bp=bdir+"/pre-creases-"+System.IO.Path.GetFileName(ap);if(!System.IO.File.Exists(bp))System.IO.File.Copy(ap,bp);}
// (1-b) 패드 Footpath 가장자리: 길 회랑 밖에서만 풀어 초원/낙엽 바닥으로 넘김
var trees=new List<Vector2>();foreach(var r in GameObject.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)){if(!r.gameObject.activeInHierarchy)continue;var m=r.sharedMaterial;if(m==null||!m.name.Contains("Osmanthus_Bark"))continue;var p=r.transform.position;if(p.x<GX0||p.x>GX1||p.z<GZ0||p.z>GZ1)continue;trees.Add(new Vector2(p.x,p.z));}
Func<float,float,float> meadowWeight=(x,z)=>{float d=999;foreach(var t in trees){var dd=(t.x-x)*(t.x-x)+(t.y-z)*(t.y-z);if(dd<d)d=dd;}d=Mathf.Sqrt(d);float canopy=1-smooth(4.5f,12f,d);float n=Mathf.PerlinNoise(x*0.045f+3.1f,z*0.045f+7.3f)*0.7f+Mathf.PerlinNoise(x*0.16f+11f,z*0.16f+5f)*0.3f;return smooth(0.40f,0.56f,n*0.9f+0.22f-canopy*0.8f)*0.7f;};
Func<float,float,float> routeDist=(x,z)=>{float d=1e9f;var p=new Vector2(x,z);foreach(var r in routes)d=Mathf.Min(d,lineDist(r,p));return d;};
int padCells=0;
foreach(var t in terrains){var d=t.terrainData;var o=t.transform.position;int res=d.alphamapResolution;float cell=d.size.x/res;
 foreach(var pd in pads){float ext=Mathf.Max(pd.z,pd.w)*1.5f;int ax=Mathf.Clamp(Mathf.FloorToInt((pd.x-ext-o.x)/cell),0,res-1),bx=Mathf.Clamp(Mathf.CeilToInt((pd.x+ext-o.x)/cell),0,res-1),az=Mathf.Clamp(Mathf.FloorToInt((pd.y-ext-o.z)/cell),0,res-1),bz=Mathf.Clamp(Mathf.CeilToInt((pd.y+ext-o.z)/cell),0,res-1);if(bx<=ax||bz<=az)continue;
  var a=d.GetAlphamaps(ax,az,bx-ax+1,bz-az+1);
  for(int j=0;j<a.GetLength(0);j++)for(int i=0;i<a.GetLength(1);i++){float x=o.x+(ax+i+0.5f)*cell,z=o.z+(az+j+0.5f)*cell;float pdist=padDist(x,z,pd);if(pdist<0.45f||pdist>1.6f)continue;
   float keepPath=1-smooth(4f,7f,routeDist(x,z));float fp=a[j,i,2];float target=fp*Mathf.Max(keepPath,1-smooth(0.5f,0.95f,pdist));float freed=fp-target;if(freed<=0.001f)continue;
   float mw=meadowWeight(x,z);a[j,i,2]=target;a[j,i,0]+=freed*mw;a[j,i,1]+=freed*(1-mw);padCells++;}
  Undo.RegisterCompleteObjectUndo(d,"R3b pads");d.SetAlphamaps(ax,az,a);}}
log.AppendLine("pad cells "+padCells);
// (2) 꺾임 완화: 월드 1m 격자
int W=(int)(GX1-GX0)+1,Hh=(int)(GZ1-GZ0)+1;var h=new float[W,Hh];var protect=new float[W,Hh];
Func<float,float,Terrain> terrainAt=(x,z)=>terrains.FirstOrDefault(t=>x>=t.transform.position.x&&x<=t.transform.position.x+512&&z>=t.transform.position.z&&z<=t.transform.position.z+512);
for(int i=0;i<W;i++)for(int j=0;j<Hh;j++){float x=GX0+i,z=GZ0+j;var t=terrainAt(x,z);h[i,j]=t==null?0:t.SampleHeight(new Vector3(x,0,z))+t.transform.position.y;
 var p=new Vector2(x,z);float pr=1-smooth(4f,7f,routeDist(x,z));pr=Mathf.Max(pr,1-smooth(14f,18f,lineDist(river,p)));pr=Mathf.Max(pr,1-smooth(22f,28f,Vector2.Distance(p,new Vector2(307.2f,-15.36f))));
 foreach(var pd in pads)pr=Mathf.Max(pr,1-smooth(0.7f,1.0f,padDist(x,z,pd)));protect[i,j]=pr;}
var crease=new float[W,Hh];int creaseCount=0;
for(int i=2;i<W-2;i++)for(int j=2;j<Hh-2;j++){float c=h[i,j];float k=Mathf.Max(Mathf.Abs(h[i+1,j]+h[i-1,j]-2*c),Mathf.Abs(h[i,j+1]+h[i,j-1]-2*c));if(k>0.35f){crease[i,j]=Mathf.Clamp01((k-0.35f)/0.5f);creaseCount++;}}
// 꺾임 주변 4m로 팽창해 필렛 폭을 확보
var zone=new float[W,Hh];for(int i=4;i<W-4;i++)for(int j=4;j<Hh-4;j++){if(crease[i,j]<=0)continue;for(int a=-4;a<=4;a++)for(int b=-4;b<=4;b++){float f=crease[i,j]*(1-Mathf.Sqrt(a*a+b*b)/5.7f);if(f>zone[i+a,j+b])zone[i+a,j+b]=f;}}
var orig=(float[,])h.Clone();
for(int it=0;it<24;it++){var nh=(float[,])h.Clone();for(int i=1;i<W-1;i++)for(int j=1;j<Hh-1;j++){float wgt=zone[i,j]*(1-protect[i,j]);if(wgt<=0.001f)continue;float avg=(h[i+1,j]+h[i-1,j]+h[i,j+1]+h[i,j-1])*0.25f;nh[i,j]=h[i,j]+(avg-h[i,j])*0.5f*wgt;}h=nh;}
// 쓰기
int written=0;float maxDelta=0;
foreach(var t in terrains){var d=t.terrainData;var o=t.transform.position;int res=d.heightmapResolution;float hy=d.size.y;
 int ax=Mathf.Clamp((int)(GX0-o.x),0,res-1),bx=Mathf.Clamp((int)(GX1-o.x),0,res-1),az=Mathf.Clamp((int)(GZ0-o.z),0,res-1),bz=Mathf.Clamp((int)(GZ1-o.z),0,res-1);if(bx<=ax||bz<=az)continue;
 var hm=d.GetHeights(ax,az,bx-ax+1,bz-az+1);bool any=false;
 for(int jj=0;jj<hm.GetLength(0);jj++)for(int ii=0;ii<hm.GetLength(1);ii++){int gi=(int)(o.x+ax+ii-GX0),gj=(int)(o.z+az+jj-GZ0);if(gi<0||gj<0||gi>=W||gj>=Hh)continue;float delta=h[gi,gj]-orig[gi,gj];if(Mathf.Abs(delta)<0.005f)continue;hm[jj,ii]=(h[gi,gj]-o.y)/hy;written++;any=true;maxDelta=Mathf.Max(maxDelta,Mathf.Abs(delta));}
 if(any){Undo.RegisterCompleteObjectUndo(d,"R3b creases");d.SetHeights(ax,az,hm);EditorUtility.SetDirty(d);}}
log.AppendLine("crease samples "+creaseCount+" written "+written+" maxDelta "+maxDelta.ToString("F2"));
// (3) 오브젝트 재안착: 프리팹 인스턴스 루트 또는 자식 없는 작은 메시만, 바닥 높이 변화량만큼 이동(Terrain·대형 수면 메시 제외)
int moved=0;var env=GameObject.Find("GeneratedEnvironment").transform;var cand=new List<Transform>();var stack=new Stack<Transform>();stack.Push(env);
while(stack.Count>0){var tr=stack.Pop();foreach(Transform ch in tr){if(ch.GetComponent<Terrain>()!=null)continue;
 if(PrefabUtility.IsOutermostPrefabInstanceRoot(ch.gameObject)){cand.Add(ch);continue;}
 var rr=ch.GetComponent<Renderer>();if(rr!=null&&ch.childCount==0){if(rr.bounds.extents.magnitude<15f)cand.Add(ch);continue;}stack.Push(ch);}}
foreach(var obj in cand){var p=obj.position;int gi=Mathf.RoundToInt(p.x-GX0),gj=Mathf.RoundToInt(p.z-GZ0);if(gi<0||gj<0||gi>=W||gj>=Hh)continue;float delta=h[gi,gj]-orig[gi,gj];if(Mathf.Abs(delta)<0.03f)continue;Undo.RecordObject(obj,"R3b resnap");obj.position=p+Vector3.up*delta;moved++;}
log.AppendLine("objects resnapped "+moved);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(env.gameObject.scene);AssetDatabase.SaveAssets();return log.ToString();
