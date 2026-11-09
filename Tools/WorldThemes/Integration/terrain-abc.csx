if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("안정된 저장 Edit Mode 필요");
AssetDatabase.Refresh();
string folder="output/WorldThemesABCIntegration",root="Assets/10.Datas/World/OpenWorld/WorldThemesABC";
var jt=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
Func<string,Type,object> read=(p,t)=>jt.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)}).Invoke(null,new object[]{System.IO.File.ReadAllText(p),t});
Func<object,string> json=o=>(string)jt.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{o});
var source=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/10.Datas/World/OpenWorld/ShadeCreekVisualR3/Terrain.asset");
if(source.heightmapResolution!=4097||source.alphamapResolution!=1024||source.detailResolution!=1024||source.size!=new Vector3(2400,400,2800))throw new Exception("지형 계약 변경");
if(System.IO.File.Exists(root+"/Terrain.asset"))throw new Exception("중복 실행 금지");
var target=UnityEngine.Object.Instantiate(source);var heights=target.GetHeights(0,0,4097,4097);var layers=target.terrainLayers.ToList();
var conflicts=new List<object>();var changes=new List<object>();var touchedHeight=new HashSet<int>();var touchedAlpha=new HashSet<int>();
foreach(string n in new[]{"QuarryPassR1","CloudridgeR1","HarborReedR1"}){
var cells=(Dictionary<string,float[][]>)read(folder+"/"+n+"-cells.json",typeof(Dictionary<string,float[][]>));
var before=System.IO.File.ReadAllLines(folder+"/"+n+"-before-layers.txt");var after=System.IO.File.ReadAllLines(folder+"/"+n+"-after-layers.txt");
foreach(string g in after)if(!layers.Any(l=>AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(l))==g)){var l=AssetDatabase.LoadAssetAtPath<TerrainLayer>(AssetDatabase.GUIDToAssetPath(g));if(l==null)throw new Exception("레이어 누락 "+g);layers.Add(l);}
var oldAlpha=target.GetAlphamaps(0,0,1024,1024);var nextAlpha=new float[1024,1024,layers.Count];for(int z=0;z<1024;z++)for(int x=0;x<1024;x++)for(int l=0;l<oldAlpha.GetLength(2);l++)nextAlpha[z,x,l]=oldAlpha[z,x,l];
int[] bmap=before.Select(g=>layers.FindIndex(l=>AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(l))==g)).ToArray();int[] amap=after.Select(g=>layers.FindIndex(l=>AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(l))==g)).ToArray();
foreach(var r in cells["height"]){int x=(int)r[0],z=(int)r[1];if(touchedHeight.Contains(z*4097+x)||Mathf.Abs(heights[z,x]-r[2])>0.00000008f){conflicts.Add(new{n,kind="height",x,z});continue;}heights[z,x]=r[3];touchedHeight.Add(z*4097+x);}
foreach(var r in cells["alpha"]){int x=(int)r[0],z=(int)r[1];bool match=!touchedAlpha.Contains(z*1024+x);for(int l=0;l<before.Length;l++)if(bmap[l]<0||Mathf.Abs(nextAlpha[z,x,bmap[l]]-r[2+l])>0.000001f)match=false;
if(!match){conflicts.Add(new{n,kind="alpha",x,z});continue;}for(int l=0;l<layers.Count;l++)nextAlpha[z,x,l]=0;for(int l=0;l<after.Length;l++)nextAlpha[z,x,amap[l]]=r[2+before.Length+l];touchedAlpha.Add(z*1024+x);}
if(conflicts.Count>0){System.IO.File.WriteAllText(folder+"/terrain-conflicts.json",json(conflicts));UnityEngine.Object.DestroyImmediate(target);throw new Exception("지형 충돌 "+conflicts.Count);}
target.terrainLayers=layers.ToArray();target.SetAlphamaps(0,0,nextAlpha);changes.Add(new{region=n,height=cells["height"].Length,alpha=cells["alpha"].Length});
}
target.SetHeights(0,0,heights);
var prototypes=target.detailPrototypes.ToList();var review=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/10.Datas/World/ThemeStudies/CloudridgeR1/Terrain.asset");
var ids=System.IO.File.ReadAllLines(folder+"/detail-guids.txt");var detailChanges=new List<object>();
for(int i=0;i<ids.Length;i++){string g=ids[i];int index=prototypes.FindIndex(p=>AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(p.prototype))==g);var rp=review.detailPrototypes.First(p=>AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(p.prototype))==g);if(index<0){var copy=new DetailPrototype(rp);string pp=AssetDatabase.GetAssetPath(rp.prototype);if(pp.Contains("/ThemeStudies/CloudridgeR1/"))copy.prototype=AssetDatabase.LoadAssetAtPath<GameObject>(pp.Replace("/ThemeStudies/CloudridgeR1/","/OpenWorld/WorldThemesABC/CloudridgeR1/"));if(copy.prototype==null)throw new Exception("전용 prototype 누락");prototypes.Add(copy);target.detailPrototypes=prototypes.ToArray();index=prototypes.Count-1;}else if(!prototypes[index].Equals(rp))throw new Exception("prototype 설정 충돌");var counts=target.GetDetailLayer(0,0,1024,1024,index);var rows=(int[][])read(folder+"/detail-"+i+".json",typeof(int[][]));foreach(var r in rows){if(counts[r[1],r[0]]!=r[2])throw new Exception("식생 셀 충돌");counts[r[1],r[0]]=r[3];}target.SetDetailLayer(0,0,index,counts);detailChanges.Add(new{guid=g,cells=rows.Length});}
AssetDatabase.CreateAsset(target,root+"/Terrain.asset");AssetDatabase.SaveAssetIfDirty(target);
System.IO.File.WriteAllText(folder+"/terrain-merge.json",json(new{changes,detailChanges,conflicts,origin=new[]{0,0,-700},size=new[]{2400,400,2800},layers=layers.Count}));
return new{changes,detailChanges,layers=layers.Count};

