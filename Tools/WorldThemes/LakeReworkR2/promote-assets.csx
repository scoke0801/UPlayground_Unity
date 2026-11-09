// 승인된 검토 에셋을 정규 데이터로 복제한다. 원본과 검토 에셋은 수정하지 않는다.
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Edit Mode 필요");
for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("미저장 씬 보호");
string source="Assets/10.Datas/World/ThemeStudies/LakeReworkR2/",destination="Assets/10.Datas/World/OpenWorld/LakeVisualV2/";
if(AssetDatabase.IsValidFolder(destination.TrimEnd('/')))throw new Exception("이미 준비한 정규 데이터");
AssetDatabase.CreateFolder("Assets/10.Datas/World/OpenWorld","LakeVisualV2");
var names=new[]{"BankBush.mat","Earth.terrainlayer","LakeShoreSky.asset","LakeWater.mat","Meadow.terrainlayer","MeadowGrass.mat","MeadowGrass.prefab","Navigation.asset","Shallows.terrainlayer","Stone.terrainlayer","Terrain.asset"};
var rows=new List<string>();foreach(string name in names){if(!AssetDatabase.CopyAsset(source+name,destination+name))throw new Exception("복제 실패: "+name);rows.Add(AssetDatabase.AssetPathToGUID(source+name)+" "+AssetDatabase.AssetPathToGUID(destination+name)+" "+name);}
Func<UnityEngine.Object,UnityEngine.Object> remap=o=>{if(o==null)return null;string p=AssetDatabase.GetAssetPath(o);return p.StartsWith(source)?AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(destination+p.Substring(source.Length)):o;};
var prefab=PrefabUtility.LoadPrefabContents(destination+"MeadowGrass.prefab");try{foreach(var r in prefab.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=r.sharedMaterials.Select(m=>(Material)remap(m)).ToArray();PrefabUtility.SaveAsPrefabAsset(prefab,destination+"MeadowGrass.prefab");}finally{PrefabUtility.UnloadPrefabContents(prefab);}
var terrain=AssetDatabase.LoadAssetAtPath<TerrainData>(destination+"Terrain.asset");terrain.terrainLayers=terrain.terrainLayers.Select(l=>(TerrainLayer)remap(l)).ToArray();var details=terrain.detailPrototypes;foreach(var detail in details)detail.prototype=(GameObject)remap(detail.prototype);terrain.detailPrototypes=details;EditorUtility.SetDirty(terrain);AssetDatabase.SaveAssetIfDirty(terrain);
System.IO.Directory.CreateDirectory("output/LakeReworkR2/Integration");System.IO.File.WriteAllLines("output/LakeReworkR2/Integration/asset-map.txt",rows);return rows;
