if(Application.dataPath!="C:/UsingProject/UnityProject/UPlayground/Assets"||!System.IO.File.ReadAllText("Tools/WorldThemes/AgentCoordination/editor.lock").Contains("\"C\""))throw new Exception("C 작업권 필요");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/HarborReedR1.unity"||scene.isDirty||EditorApplication.isPlayingOrWillChangePlaymode||EditorUtility.scriptCompilationFailed)throw new Exception("저장된 C 검토 씬 필요");
string folder="Assets/10.Datas/World/ThemeStudies/HarborReedR1";
var terrain=Terrain.activeTerrain;var data=terrain.terrainData;
if(AssetDatabase.GetAssetPath(data)!=folder+"/Terrain.asset")throw new Exception("Terrain 소유권 불일치");
if(System.IO.File.Exists("output/HarborReedR1/height-refinement.json"))throw new Exception("재적용 금지");
var baseline=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/10.Datas/World/OpenWorld/ShadeCreekVisualR3/Terrain.asset");
Func<float,float> smooth=v=>Mathf.SmoothStep(0,1,Mathf.Clamp01(v));
Func<float,float,float> mask=(x,z)=>smooth((x-1818)/10)*smooth((2008-x)/12)*smooth((z+410)/18)*smooth((-317-z)/12);
var rockBounds=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).Where(r=>r.transform.position.x>1815&&r.transform.position.x<1875&&r.transform.position.z<-318&&r.transform.position.z>-350).Select(r=>r.bounds).ToArray();
var heightChanges=new List<object>();
int resolution=data.heightmapResolution;
int hx0=Mathf.FloorToInt(1818/data.size.x*(resolution-1)),hx1=Mathf.CeilToInt(1870/data.size.x*(resolution-1));
int hz0=Mathf.FloorToInt(340/data.size.z*(resolution-1)),hz1=Mathf.CeilToInt(385/data.size.z*(resolution-1));
var heights=data.GetHeights(hx0,hz0,hx1-hx0+1,hz1-hz0+1);
for(int iz=0;iz<heights.GetLength(0);iz++)for(int ix=0;ix<heights.GetLength(1);ix++){
 float x=(hx0+ix)*data.size.x/(resolution-1),z=(hz0+iz)*data.size.z/(resolution-1)-700;float old=heights[iz,ix]*data.size.y;
 if(old>=45.9f||old<41.9f)continue;
 bool support=rockBounds.Any(b=>x>b.min.x-1&&x<b.max.x+1&&z>b.min.z-1&&z<b.max.z+1);if(support)continue;
 // 기존 수면 위 지반은 고정하며 수중 NavMesh를 육상 보행선으로 오인하지 않는다.
 float weight=smooth((x-1818)/10)*smooth((1866-x)/10)*smooth((z+360)/9)*smooth((-319-z)/8)*smooth((45.9f-old)/.7f);
 float curve=-337+7*Mathf.Exp(-Mathf.Pow((x-1848)/10,2))-6*Mathf.Exp(-Mathf.Pow((x-1829)/9,2));
 float target=Mathf.Clamp(45.9f+(z-curve)*.34f,42,46.4f);float next=Mathf.Lerp(old,target,weight);
 if(Mathf.Abs(next-old)<.005f)continue;
 heights[iz,ix]=next/data.size.y;heightChanges.Add(new{xIndex=hx0+ix,zIndex=hz0+iz,x,z,before=old/data.size.y,after=heights[iz,ix]});
}
data.SetHeights(hx0,hz0,heights);
EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(a=>a!=null);
System.IO.File.WriteAllText("output/HarborReedR1/height-refinement.json",(string)json.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{new{heightChanges,protection="수면45.9m 이상 기존 지반/구조물 접지 영역 고정. 771점 실제 보행선 별도 전후 검사. 수중 NavMesh는 해안 통합 후 재베이크 대상."}}));
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);return new{height=heightChanges.Count};
