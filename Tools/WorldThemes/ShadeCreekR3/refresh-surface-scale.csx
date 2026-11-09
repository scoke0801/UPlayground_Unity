// 전용 부식층의 잎 크기와 노멀을 조정하고 Terrain 바인딩을 갱신한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/ShadeCreekR3.unity"||scene.isDirty||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("저장된 R3 Edit 필요");
var layer=AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/10.Datas/World/ThemeStudies/ShadeCreekR3/CanopyHumus.terrainlayer");
layer.tileSize=new Vector2(7,7);layer.normalScale=.65f;
EditorUtility.SetDirty(layer);AssetDatabase.SaveAssetIfDirty(layer);
var terrain=Terrain.activeTerrain;var data=terrain.terrainData;
data.terrainLayers=data.terrainLayers;data.SetBaseMapDirty();terrain.Flush();
EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);
return "전용 표면 갱신. 높이·배치 변경 없음";
