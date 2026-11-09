// 승인된 R3 환경을 정규 전용 데이터로 복제한다. 검토본과 공유 편집하지 않는다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.isDirty||EditorApplication.isPlayingOrWillChangePlaymode||EditorUtility.scriptCompilationFailed)throw new Exception("저장된 Edit 상태 필요");
string source="Assets/10.Datas/World/ThemeStudies/ShadeCreekR3",target="Assets/10.Datas/World/OpenWorld/ShadeCreekVisualR3";
if(AssetDatabase.IsValidFolder(target))throw new Exception("정규 전용 데이터가 이미 있음. 중복 복제 금지");
if(!AssetDatabase.CopyAsset(source,target))throw new Exception("전용 데이터 복제 실패");
var paths=System.IO.Directory.GetFiles(source,"*",System.IO.SearchOption.AllDirectories).Where(p=>!p.EndsWith(".meta")).Select(p=>p.Replace('\\','/')).ToArray();
var mappings=paths.Select(p=>new{source=p,target=target+p.Substring(source.Length),oldGuid=AssetDatabase.AssetPathToGUID(p),newGuid=AssetDatabase.AssetPathToGUID(target+p.Substring(source.Length))}).ToArray();
foreach(var map in mappings){
string text=System.IO.File.ReadAllText(map.target);
if(text.StartsWith("%YAML")){foreach(var pair in mappings)text=text.Replace(pair.oldGuid,pair.newGuid);System.IO.File.WriteAllText(map.target,text);AssetDatabase.ImportAsset(map.target);}
else if(map.target.EndsWith(".shader")){text=System.Text.RegularExpressions.Regex.Replace(text,"Shader\\s+\"[^\"]+\"","Shader \"UPlayGround/OpenWorld/ShadeCreekVisualR3/"+System.IO.Path.GetFileNameWithoutExtension(map.target)+"\"");System.IO.File.WriteAllText(map.target,text);AssetDatabase.ImportAsset(map.target);}
}
Func<UnityEngine.Object,UnityEngine.Object> remap=o=>{if(o==null)return null;string p=AssetDatabase.GetAssetPath(o);return p.StartsWith(source+"/")?AssetDatabase.LoadMainAssetAtPath(target+p.Substring(source.Length)):o;};
var terrain=AssetDatabase.LoadAssetAtPath<TerrainData>(target+"/Terrain.asset");terrain.terrainLayers=terrain.terrainLayers.Select(l=>(TerrainLayer)remap(l)).ToArray();
var details=terrain.detailPrototypes;foreach(var prototype in details)prototype.prototype=(GameObject)remap(prototype.prototype);terrain.detailPrototypes=details;
var trees=terrain.treePrototypes;foreach(var prototype in trees)prototype.prefab=(GameObject)remap(prototype.prefab);terrain.treePrototypes=trees;
EditorUtility.SetDirty(terrain);AssetDatabase.SaveAssetIfDirty(terrain);
var forbidden=AssetDatabase.GetDependencies(mappings.Select(m=>m.target).ToArray(),true).Where(p=>p.Contains("/ThemeStudies/ShadeCreekR")).ToArray();
if(forbidden.Length>0)throw new Exception("검토 전용 의존 잔존: "+string.Join(";",forbidden));
System.IO.File.WriteAllLines("output/ShadeCreekR3/Integration/asset-map.tsv",mappings.Select(m=>m.oldGuid+"\t"+m.newGuid+"\t"+m.source+"\t"+m.target));
return new{assets=mappings.Length,forbidden,terrainLayers=terrain.terrainLayers.Length,detailPrototypes=details.Length};
