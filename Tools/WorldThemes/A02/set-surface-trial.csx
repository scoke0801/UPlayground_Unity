// 같은 지형·구도에서 표면 후보만 바꾸며 원본은 읽기만 한다. 시험 상태는 저장하지 않는다.
string folder="output/WorldThemeProduction/LakeShrine";
string trialPath=folder+"/surface-trial.txt";
string trial=System.IO.File.Exists(trialPath)?System.IO.File.ReadAllText(trialPath).Trim():"Ground6";
if(trial!="Before"&&trial!="Ground6"&&trial!="RiverBed"&&trial!="Mixed")throw new Exception("표면 시험은 Before/Ground6/RiverBed/Mixed만 지원합니다.");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/Theme_LakeShrine.unity")throw new Exception("LakeShrine 독립 씬 Edit Mode 필요");
var jsonType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var deserialize=jsonType.GetMethod("DeserializeObject",new Type[]{typeof(string),typeof(Type)});
Func<string,Type,object> read=(text,type)=>deserialize.Invoke(null,new object[]{text,type});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/WorldThemes/A02/lake-study.json"),typeof(Dictionary<string,object>));
var rows=(Dictionary<string,object>[])read(System.IO.File.ReadAllText(folder+"/painted-cells.json"),typeof(Dictionary<string,object>[]));
var source=UnityEditor.AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/10.Datas/World/OpenWorld/Terrain.asset");
var terrain=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).Single();
var data=terrain.terrainData;
if(UnityEditor.AssetDatabase.GetAssetPath(data)!="Assets/10.Datas/World/ThemeStudies/LakeShrine/Terrain.asset"||data==source)throw new Exception("전용 TerrainData가 아닙니다.");
var surfaceSources=(Dictionary<string,object>)read(plan["surfaceSourceLayers"].ToString(),typeof(Dictionary<string,object>));
int sandIndex=Array.FindIndex(source.terrainLayers,l=>UnityEditor.AssetDatabase.GetAssetPath(l)==surfaceSources["sand"].ToString());
int earthIndex=Array.FindIndex(source.terrainLayers,l=>UnityEditor.AssetDatabase.GetAssetPath(l)==surfaceSources["earth"].ToString());
int rockIndex=Array.FindIndex(source.terrainLayers,l=>UnityEditor.AssetDatabase.GetAssetPath(l)==surfaceSources["rock"].ToString());
int riverIndex=Array.FindIndex(data.terrainLayers,l=>UnityEditor.AssetDatabase.GetAssetPath(l)=="Assets/10.Datas/World/ThemeStudies/LakeShrine/Layer_RiverBed.terrainlayer");
if(sandIndex<0||earthIndex<0||rockIndex<0||riverIndex<0)throw new Exception("표면 remap 누락");
var original=source.GetAlphamaps(0,0,source.alphamapWidth,source.alphamapHeight);
var alpha=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);
foreach(var row in rows){
    int x=Convert.ToInt32(row["x"]),z=Convert.ToInt32(row["z"]);
    float blend=trial=="Before"?0f:Convert.ToSingle(row["blend"]);
    float riverFraction=trial=="RiverBed"?1f:trial=="Mixed"?Mathf.Lerp(.82f,.18f,Mathf.Clamp01(Convert.ToSingle(row["depth"])/2.8f)):0f;
    float sand=Convert.ToSingle(row["sandWeight"]),earth=Convert.ToSingle(row["earthWeight"]),rock=Convert.ToSingle(row["rockWeight"]);
    for(int l=0;l<alpha.GetLength(2);l++){
        float target=l==sandIndex?sand*(1f-riverFraction):l==riverIndex?sand*riverFraction:l==earthIndex?earth:l==rockIndex?rock:0f;
        float baseline=l<original.GetLength(2)?original[z,x,l]:0f;
        alpha[z,x,l]=Mathf.Lerp(baseline,target,blend);
    }
}
data.SetAlphamaps(0,0,alpha);terrain.Flush();
System.IO.File.WriteAllText(folder+"/current-surface-trial.txt",trial);
return "LakeShrine clone 표면 시험: "+trial+", "+rows.Length+"셀. 원본 읽기 전용·아직 저장하지 않음.";
