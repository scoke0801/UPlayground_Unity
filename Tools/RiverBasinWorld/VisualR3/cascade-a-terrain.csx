// R3b 폭포 A단계: 백업 → 하상 굴착 → 기존 계단식 수면 삼각형 제거.
// 원래 하상은 72→50m 일정 경사이고 수면만 24m마다 평탄+수직벽으로 깔려 계단처럼 보였다.
// 수면 프로필을 급류→주폭포(약 9m)→소→급류→보조 폭포(약 6m)→소→하류로 바꾸고, 하상은 수면 아래로만 판다(올리지 않음).
const float CX=307.2f,Z0=-85f,Z1=-184.3f;
// (s=Z0에서 하류로의 거리, 수면 높이, 수심). 낙차 구간은 별도 낙수면이 덮는다.
var prof=new float[,]{{0,72,2.4f},{12,71.6f,1.6f},{24,68.6f,0.6f},{24.01f,59.6f,2.6f},{46,59.6f,2.6f},{62,57.2f,0.6f},{62.01f,51f,2.4f},{82,51,2.2f},{99.3f,50,1.6f}};
Func<float,int,float> P=(s,c)=>{int n=prof.GetLength(0);if(s<=prof[0,0])return prof[0,c];for(int i=1;i<n;i++)if(s<=prof[i,0]){float t=(s-prof[i-1,0])/(prof[i,0]-prof[i-1,0]);return Mathf.Lerp(prof[i-1,c],prof[i,c],t);}return prof[n-1,c];};
var log=new System.Text.StringBuilder();
string bdir="output/RiverBasinWorld/VisualR3/backup-r3b";System.IO.Directory.CreateDirectory(bdir);
foreach(var t in Terrain.activeTerrains){var o=t.transform.position;if(CX<o.x-20||CX>o.x+532||Z1>o.z+512||Z0<o.z)continue;
 var d=t.terrainData;var ap=AssetDatabase.GetAssetPath(d);var bp=bdir+"/"+System.IO.Path.GetFileName(ap);if(!System.IO.File.Exists(bp))System.IO.File.Copy(ap,bp);
 int res=d.heightmapResolution;float sx=d.size.x/(res-1),sz=d.size.z/(res-1),hy=d.size.y;
 int ax=Mathf.Clamp(Mathf.FloorToInt((CX-20-o.x)/sx),0,res-1),bx=Mathf.Clamp(Mathf.CeilToInt((CX+20-o.x)/sx),0,res-1),az=Mathf.Clamp(Mathf.FloorToInt((Z1-3-o.z)/sz),0,res-1),bz=Mathf.Clamp(Mathf.CeilToInt((Z0+3-o.z)/sz),0,res-1);
 if(bx<=ax||bz<=az)continue;int w=bx-ax+1,h=bz-az+1;var hm=d.GetHeights(ax,az,w,h);int changed=0;float maxCut=0;
 for(int j=0;j<h;j++)for(int i=0;i<w;i++){float x=o.x+(ax+i)*sx,z=o.z+(az+j)*sz;float s=Z0-z;if(s<-3||s>102)continue;
  float edge=Mathf.Clamp01(Mathf.Min(s+3,102-s)/3f);float dx=Mathf.Abs(x-CX);
  float water=P(s,1),depth=P(s,2);
  // 단면: 중앙은 수심만큼, 9m에서 수면 +0.15·수심. 그 바깥은 기존 지형 유지.
  float bed=water-depth+depth*1.15f*(dx/9f)*(dx/9f);
  float cur=hm[j,i]*hy+o.y;if(bed>=cur)continue;float nw=Mathf.Lerp(cur,bed,edge);
  maxCut=Mathf.Max(maxCut,cur-nw);hm[j,i]=(nw-o.y)/hy;changed++;}
 Undo.RegisterCompleteObjectUndo(d,"R3b cascade carve");d.SetHeights(ax,az,hm);EditorUtility.SetDirty(d);log.AppendLine(t.name+" carved "+changed+" maxCut "+maxCut.ToString("F2"));}
// 기존 계단식 수면(RiverOutside)에서 이 구간 삼각형을 제거한다.
var rp=GameObject.Find("GeneratedEnvironment/수계 · 평탄 소와 낙차/RiverPools");var mesh=rp.GetComponent<MeshFilter>().sharedMesh;var mp=AssetDatabase.GetAssetPath(mesh);
var mb=bdir+"/"+System.IO.Path.GetFileName(mp);if(!System.IO.File.Exists(mb))System.IO.File.Copy(mp,mb);
var v=mesh.vertices;var tri=mesh.triangles;var keep=new List<int>();int removed=0;var tr=rp.transform;
for(int k=0;k<tri.Length;k+=3){var c=tr.TransformPoint((v[tri[k]]+v[tri[k+1]]+v[tri[k+2]])/3f);if(c.x>CX-30&&c.x<CX+30&&c.z<Z0+0.5f&&c.z>Z1-1.5f){removed++;continue;}keep.Add(tri[k]);keep.Add(tri[k+1]);keep.Add(tri[k+2]);}
Undo.RecordObject(mesh,"R3b river cut");mesh.SetTriangles(keep,0);mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
log.AppendLine("RiverOutside removed tris "+removed+" backup "+mb);
AssetDatabase.SaveAssets();return log.ToString();
