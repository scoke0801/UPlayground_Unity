// 정규용 독립 자원을 복제하고 내부 참조를 정규 데이터로 연결한다.
var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(s.isDirty||EditorApplication.isPlayingOrWillChangePlaymode||s.path!="Assets/01.Scenes/ArtReview/WorldThemes/RedForestR1.unity")throw new Exception("저장된 검토 씬 필요");
string source="Assets/10.Datas/World/ThemeStudies/RedForestR1",target="Assets/10.Datas/World/OpenWorld/RedForestVisualR1";
if(AssetDatabase.IsValidFolder(target))throw new Exception("정규 데이터 중복 생성 금지");
AssetDatabase.CreateFolder("Assets/10.Datas/World/OpenWorld","RedForestVisualR1");
var paths=System.IO.Directory.GetFiles(source).Where(p=>!p.EndsWith(".meta")).Select(p=>p.Replace('\\','/')).ToArray();
foreach(var p in paths)if(!AssetDatabase.CopyAsset(p,target+"/"+System.IO.Path.GetFileName(p)))throw new Exception("복제 실패 "+p);
Func<UnityEngine.Object,UnityEngine.Object> remap=o=>{if(o==null)return null;string p=AssetDatabase.GetAssetPath(o);return p.StartsWith(source+"/")?AssetDatabase.LoadMainAssetAtPath(target+p.Substring(source.Length)):o;};
var terrain=AssetDatabase.LoadAssetAtPath<TerrainData>(target+"/Terrain.asset");
terrain.terrainLayers=terrain.terrainLayers.Select(l=>(TerrainLayer)remap(l)).ToArray();
var protos=terrain.detailPrototypes;foreach(var p in protos)p.prototype=(GameObject)remap(p.prototype);terrain.detailPrototypes=protos;EditorUtility.SetDirty(terrain);AssetDatabase.SaveAssetIfDirty(terrain);
string prefabPath=target+"/UnderstoryGrass.prefab";var root=PrefabUtility.LoadPrefabContents(prefabPath);
try{foreach(var r in root.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=r.sharedMaterials.Select(m=>(Material)remap(m)).ToArray();PrefabUtility.SaveAsPrefabAsset(root,prefabPath);}finally{PrefabUtility.UnloadPrefabContents(root);}
var rows=paths.Select(p=>AssetDatabase.AssetPathToGUID(p)+"\t"+AssetDatabase.AssetPathToGUID(target+"/"+System.IO.Path.GetFileName(p))+"\t"+p).ToArray();
System.IO.File.WriteAllLines("output/RedForestR1/Integration/asset-map.tsv",rows);return rows;
