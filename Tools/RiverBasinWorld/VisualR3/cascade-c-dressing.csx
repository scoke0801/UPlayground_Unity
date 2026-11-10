// R3b 폭포 C단계: 낙구 암반 턱·물줄기 가르는 바위·소/급류 바위·물보라와 안개, 그리고 B1 수면의 강둑 위 돌출 삼각형 제거.
// 결정적 난수(System.Random 고정 시드)로 재실행해도 같은 배치가 나온다.
const float Z0=-85f;
var lipLines=System.IO.File.ReadAllLines("output/RiverBasinWorld/VisualR3/cascade-lips.txt");
Func<int,float[]> lipOf=i=>lipLines[i].Split(' ')[1].Split(',').Select(float.Parse).ToArray();
var lips=new[]{lipOf(0),lipOf(1)};var lipS=new[]{24f,62f};var lipTop=new[]{68.6f,57.2f};var pool=new[]{59.6f,51f};var thr=new[]{1.9f,1.4f};
var terrain=Terrain.activeTerrains.First(t=>t.name=="Terrain_2_1");Func<float,float,float> H=(x,z)=>terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y;
var root=GameObject.Find("GeneratedEnvironment/B1_ArtR1/B1 · 급류와 폭포").transform;
foreach(var n in new[]{"암반","물보라"}){var old=root.Find(n);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);}
var rockRoot=new GameObject("암반").transform;rockRoot.SetParent(root,false);var fxRoot=new GameObject("물보라").transform;fxRoot.SetParent(root,false);
var rnd=new System.Random(3107);Func<float,float,float> R=(a,b)=>a+(float)rnd.NextDouble()*(b-a);
string rp="Assets/03.Prefabs/Environment/RiverBasinWorld/VisualR1/PF_EastLands_RiverRock_0";var rocks=new[]{rp+"1_Gold_17.prefab",rp+"2_Gold_16.prefab",rp+"3_Gold_14.prefab",rp+"4_Gold_15.prefab"}.Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();
string cp="Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Prefabs/Stones/Cliffs/PF_EastLands_Cliff_0";var cliffs=new[]{AssetDatabase.LoadAssetAtPath<GameObject>(cp+"2.prefab"),AssetDatabase.LoadAssetAtPath<GameObject>(cp+"3.prefab")};
int count=0;
Action<GameObject,Transform,Vector3,float,string> put=(prefab,parent,pos,scale,name)=>{var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);g.name=name;g.transform.position=pos;g.transform.rotation=Quaternion.Euler(R(-8,8),R(0,360),R(-8,8));g.transform.localScale=Vector3.one*scale;count++;};
for(int f=0;f<2;f++){float l=lips[f][0],r=lips[f][1],c0=lips[f][2];float lz=Z0-lipS[f];
 // 낙구 양옆 암반 턱: 물이 바위 사이로 모여 떨어지는 형태
 foreach(var side in new[]{-1f,1f}){float edge=side<0?l:r;
  put(cliffs[f%2],rockRoot,new Vector3(c0+side*(edge*0.92f+R(2.2f,3.2f)),lipTop[f]-R(1.6f,2.4f),lz+R(-1.5f,0.5f)),R(0.15f,0.2f),"낙구 암반");
  put(cliffs[(f+1)%2],rockRoot,new Vector3(c0+side*(edge*0.85f+R(2.5f,4f)),pool[f]-R(1.2f,2f),lz-R(4f,7f)),R(0.13f,0.17f),"소 암반");}
 // 물줄기를 가르는 바위 2개
 for(int k=0;k<2;k++){float x=c0+(k==0?-1:1)*R(0.25f,0.45f)*(k==0?l:r);put(rocks[k],rockRoot,new Vector3(x,lipTop[f]-R(0.5f,0.8f),lz+R(-0.2f,0.6f)),R(1.4f,2.0f),"낙구 바위");}
 // 소 안 바위
 for(int k=0;k<4;k++){float x=c0+R(-0.8f,0.8f)*l;float z=lz-thr[f]-R(3f,12f);put(rocks[rnd.Next(4)],rockRoot,new Vector3(x,pool[f]-R(0.5f,1.1f),z),R(1.1f,2.2f),"소 바위");}
 // 물보라·안개
 var splash=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalAssets/Environment/PolyartStudio/SharedResources/Effects/P_WaterSplash.prefab");
 var mist=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalAssets/Environment/PolyartStudio/SharedResources/Effects/P_WaterMist.prefab");
 int ns=f==0?4:3;for(int k=0;k<ns;k++){float a=(k+0.5f)/ns;var g=(GameObject)PrefabUtility.InstantiatePrefab(splash,fxRoot);g.name="낙수 물보라";g.transform.position=new Vector3(Mathf.Lerp(c0-l*0.8f,c0+r*0.8f,a),pool[f]+0.2f,lz-thr[f]-0.4f);}
 var m=(GameObject)PrefabUtility.InstantiatePrefab(mist,fxRoot);m.name="낙수 안개";m.transform.position=new Vector3(c0,pool[f]+1f,lz-thr[f]-1.5f);m.transform.localScale=Vector3.one*(f==0?1.4f:1.1f);}
// 급류 바위(물가 쪽)
foreach(var span in new[]{new[]{8f,23f,1f},new[]{44f,61f,2f}}){for(int k=0;k<5;k++){float s=R(span[0],span[1]);float z=Z0-s;int f=(int)span[2]-1;float c0=lips[f][2];float side=rnd.NextDouble()<0.5?-1:1;float x=c0+side*R(4f,8f);float y=H(x,z);put(rocks[rnd.Next(4)],rockRoot,new Vector3(x,y-R(0.2f,0.5f),z),R(0.9f,1.7f),"급류 바위");}}
// B1 수면에서 강둑 위로 튀어나온 삼각형 제거(세 꼭짓점 모두 지형보다 0.3m 이상 아래 수면이면 보이지 않는다)
var surf=GameObject.Find("GeneratedEnvironment/B1_ArtR1/B1 · 굽은 수면");var mesh=surf.GetComponent<MeshFilter>().sharedMesh;var mp=AssetDatabase.GetAssetPath(mesh);
var bk="output/RiverBasinWorld/VisualR3/backup-r3b/"+System.IO.Path.GetFileName(mp);if(!System.IO.File.Exists(bk))System.IO.File.Copy(mp,bk);
var v=mesh.vertices;var tri=mesh.triangles;var keep=new List<int>();int cut=0;var tr=surf.transform;
Func<int,bool> buried=i=>{var w=tr.TransformPoint(v[i]);return H(w.x,w.z)>w.y+0.3f;};
for(int k=0;k<tri.Length;k+=3){if(buried(tri[k])&&buried(tri[k+1])&&buried(tri[k+2])){cut++;continue;}keep.Add(tri[k]);keep.Add(tri[k+1]);keep.Add(tri[k+2]);}
Undo.RecordObject(mesh,"R3b surface trim");mesh.SetTriangles(keep,0);EditorUtility.SetDirty(mesh);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.gameObject.scene);AssetDatabase.SaveAssets();
return "rocks "+count+" surfaceTrisCut "+cut;
