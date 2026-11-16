// 사용자 제보: 상류 겹침, 노출된 동굴 상자형 외피, 상자 편심을 국소 보정한다.
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("편집 대기");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var water=GameObject.Find("LakeWaterfall_Northeast").transform.Find("R2 하상과 낙구");
var source=water.Find("UpperStream").GetComponent<MeshFilter>();var fall=water.Find("WideFall").GetComponent<MeshFilter>();
var sv=source.sharedMesh.vertices;var forward=new Vector3(-.45f,0,-.8930286f).normalized;
var vertices=new List<Vector3>();var uvs=new List<Vector2>();var colors=new List<Color>();var triangles=new List<int>();
// 상류 마지막 행과 정점 수·위치를 공유한다. 겹친 상류 연장판과 중복 양면 스킨을 없앤다.
const int Rows=96,Cols=32;
for(int row=0;row<=Rows;row++)for(int col=0;col<=Cols;col++){
 float k=row/(float)Rows,u=col/(float)Cols;var lip=source.transform.TransformPoint(sv[sv.Length-33+col]);
 var center=source.transform.TransformPoint(sv[sv.Length-17]);
 var p=lip+forward*(6.2f*k)+Vector3.down*(1.45f*k+15.55f*k*k);
 p+=(lip-center)*(-.065f*k);
 p+=forward*((Mathf.PerlinNoise(u*6.3f+3,k*4.1f)-.5f)*.16f*k);
 vertices.Add(fall.transform.InverseTransformPoint(p));uvs.Add(new Vector2(u,Mathf.Lerp(.16f,1,k)));colors.Add(Color.red);
}
for(int row=0;row<Rows;row++)for(int col=0;col<Cols;col++){int a=row*33+col,b=a+33;triangles.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
var fm=fall.sharedMesh;fm.Clear();fm.SetVertices(vertices);fm.SetUVs(0,uvs);fm.SetColors(colors);fm.SetTriangles(triangles,0);fm.RecalculateNormals();fm.RecalculateTangents();fm.RecalculateBounds();EditorUtility.SetDirty(fm);
var cave=GameObject.Find("LakeWaterfallCave").transform;var terrain=Terrain.activeTerrain;var td=terrain.terrainData;
// 동굴을 실제 절벽 안으로 넣어 돌 판으로 외부 공간을 둘러싼 기존 형태를 해소한다.
cave.position+=cave.forward*3f;
Func<float,float,float> ground=(x,z)=>terrain.SampleHeight(cave.TransformPoint(new Vector3(x,0,z)))+terrain.transform.position.y-cave.position.y;
Action<string,List<Vector3>,List<int>,List<Vector2>> meshWrite=(name,v,t,uv)=>{var g=cave.Find(name);var m=g.GetComponent<MeshFilter>().sharedMesh;m.Clear();m.SetVertices(v);m.SetTriangles(t,0);m.SetUVs(0,uv);m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();g.GetComponent<MeshCollider>().sharedMesh=null;g.GetComponent<MeshCollider>().sharedMesh=m;EditorUtility.SetDirty(m);};
// 지형 구멍은 이전 범위를 복원하고 현재 동굴의 천장 아래로 옮긴다.
int hr=td.holesResolution;float dx=td.size.x/hr,dz=td.size.z/hr;int ax=Mathf.FloorToInt(1125/dx),az=Mathf.FloorToInt((1005-terrain.transform.position.z)/dz),nw=Mathf.CeilToInt(45/dx),nh=Mathf.CeilToInt(55/dz);
var original=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/10.Datas/World/OpenWorld/LakeWaterfall/R2/Terrain.asset");var holes=original.GetHoles(ax,az,nw,nh);
for(int iz=0;iz<nh;iz++)for(int ix=0;ix<nw;ix++){var p=new Vector3((ax+ix+.5f)*dx,0,terrain.transform.position.z+(az+iz+.5f)*dz);var lp=cave.InverseTransformPoint(p);if(lp.z>=-.8f&&lp.z<=10.05f&&Mathf.Abs(lp.x)<=5.15f)holes[iz,ix]=false;}
td.SetHoles(ax,az,holes);EditorUtility.SetDirty(td);
var vtx=new List<Vector3>();var uvtx=new List<Vector2>();var tri=new List<int>();
for(int iz=0;iz<=30;iz++)for(int ix=0;ix<=28;ix++){float x=Mathf.Lerp(-5.6f,5.6f,ix/28f),z=Mathf.Lerp(-1f,10.4f,iz/30f);vtx.Add(new Vector3(x,ground(x,z)-.015f,z));uvtx.Add(new Vector2(x/3,z/3));}
for(int iz=0;iz<30;iz++)for(int ix=0;ix<28;ix++){int a=iz*29+ix,b=a+29;tri.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
meshWrite("LakeWaterfallCave_Roof",vtx,tri,uvtx);
// 입구 윤곽에서 원래 사면과 만나는 위치까지 암반을 연결한다. 수직 사각 외벽은 없다.
vtx=new List<Vector3>();uvtx=new List<Vector2>();tri=new List<int>();var inner=cave.Find("LakeWaterfallCave_Interior").GetComponent<MeshFilter>().sharedMesh.vertices;
for(int ring=0;ring<=8;ring++)for(int col=0;col<=32;col++){
 var p=inner[col];float a=col/32f*Mathf.PI*2,t=ring/8f;
 float x=Mathf.Cos(a)*(5.7f+.25f*Mathf.Sin(a*3)),y=2f+Mathf.Sin(a)*(3.4f+.2f*Mathf.Cos(a*5));
 float z=-1.4f;for(int j=0;j<80;j++){float test=-1.4f+j*.06f;if(ground(x,test)>=y){z=test;break;}}
 var q=Vector3.Lerp(p,new Vector3(x,y,z+.08f),t);q.z-=Mathf.Sin(t*Mathf.PI)*.15f*Mathf.Sin(a*3+.4f);
 vtx.Add(q);uvtx.Add(new Vector2(q.x/3,q.y/3));
}
for(int row=0;row<8;row++)for(int col=0;col<32;col++){int a=row*33+col,b=a+33;tri.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
meshWrite("LakeWaterfallCave_Cliff",vtx,tri,uvtx);
// 진입 경사 중심만 보행 높이를 유지하고 양끝은 실제 하상 아래에 묻는다.
vtx=new List<Vector3>();uvtx=new List<Vector2>();tri=new List<int>();
for(int iz=0;iz<=32;iz++)for(int ix=0;ix<=32;ix++){
 float z=Mathf.Lerp(-5.5f,.2f,iz/32f),u=ix/32f*2-1;float width=4.5f+.3f*Mathf.Sin(z*1.1f);float x=u*width;
 float y=Mathf.Lerp(-2.3f,.03f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(-5.5f,.2f,z)));
 float edge=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.52f,1,Mathf.Abs(u)));y=Mathf.Lerp(y,Mathf.Min(y-.2f,ground(x,z)-.12f),edge);
 vtx.Add(new Vector3(x,y,z));uvtx.Add(new Vector2(x/3,z/3));
}
for(int iz=0;iz<32;iz++)for(int ix=0;ix<32;ix++){int a=iz*33+ix,b=a+33;tri.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
meshWrite("LakeWaterfallCave_Ramp",vtx,tri,uvtx);
var chest=cave.Find("숨겨진 보물상자");var cp=chest.localPosition;cp.x=0;chest.localPosition=cp;
Physics.SyncTransforms();var floorCollider=cave.Find("LakeWaterfallCave_Interior").GetComponent<MeshCollider>();RaycastHit hit;
if(floorCollider.Raycast(new Ray(chest.position+Vector3.up*2,Vector3.down),out hit,5)){cp=chest.position;cp.y=hit.point.y+.015f;chest.position=cp;}
EditorUtility.SetDirty(cave);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
foreach(var asset in new UnityEngine.Object[]{fm,td})AssetDatabase.SaveAssetIfDirty(asset);
foreach(var mf in cave.GetComponentsInChildren<MeshFilter>())if(AssetDatabase.GetAssetPath(mf.sharedMesh).Contains("LakeWaterfall/R3"))AssetDatabase.SaveAssetIfDirty(mf.sharedMesh);
return "연속 낙수 단면, 사면 안쪽 동굴, 하상에 묻힌 진입 경사, 상자 중앙 반영";
