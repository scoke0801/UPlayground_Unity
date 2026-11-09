var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/LakeReworkR2.unity"||EditorApplication.isPlaying)throw new Exception("검토 씬 필요");
var terrain=Terrain.activeTerrain;
var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
string folder="output/LakeReworkR2";
var records=new List<string>();
foreach(string name in new[]{"ShoreRock_34","ShoreRock_35"}){
var t=all.Single(g=>g.name==name);records.Add(name+" | "+t.position+" | "+t.localScale);
// 사당 지붕 앞의 구형 거석만 낮춘다. 앞선 8.7 보존 대상과 별개다.
t.localScale*=.48f;var p=t.position;p.y=terrain.SampleHeight(p)-.45f;t.position=p;
}
var parent=new GameObject("LakeRework_CanopyMasses");
string treePath="Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Prefabs/Foliage/Trees/";
var poses=new[]{new Vector3(970,0,875),new Vector3(984,0,882),new Vector3(997,0,870),new Vector3(981,0,900),new Vector3(963,0,903),new Vector3(995,0,914),new Vector3(1153,0,915),new Vector3(1165,0,932),new Vector3(1140,0,940),new Vector3(1161,0,950),new Vector3(1180,0,918),new Vector3(1132,0,965)};
for(int i=0;i<poses.Length;i++){
string path=treePath+(i%2==0?"PF_Farm_GenTree_A_FullTree_01.prefab":"PF_Farm_GenTree_B_FullTree_01.prefab");
var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prefab==null)throw new Exception(path);
var obj=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);obj.name="Canopy_"+i;obj.transform.SetParent(parent.transform);
var p=poses[i];p.y=terrain.SampleHeight(p)-.15f;obj.transform.position=p;obj.transform.rotation=Quaternion.Euler(0,i*137.5f,0);obj.transform.localScale=Vector3.one*(.8f+(i%3)*.13f);
foreach(var particle in obj.GetComponentsInChildren<ParticleSystem>(true))particle.gameObject.SetActive(false);
records.Add(obj.name+" | "+p+" | "+obj.transform.localScale+" | "+path);
}
System.IO.File.WriteAllLines(folder+"/composition-objects.txt",records);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);return records;
