if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)throw new Exception("준비 필요");
var host=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(host.isDirty)throw new Exception("미저장 씬");
var folder="output/OpenWorld/Exploration/Iteration06";System.IO.Directory.CreateDirectory("Assets/03.Prefabs/Environment/OpenWorld/Dungeons");
var paths=new[]{"Assets/ExternalAssets/Environment/Fantastic Dungeon Pack/scenes/demoscene_dungeon_level_3_hall.unity","Assets/ExternalAssets/Environment/Fantastic Dungeon Pack/scenes/demoscene_dungeon_level_4_temple.unity","Assets/ExternalAssets/Environment/LowPolyFantasyArena2/Scenes/LowPolyArena2_Demo.unity"};
var keys=new[]{"SunkenHall","ChainTemple","BriarRing"};var rows=new List<string>();
for(int i=0;i<paths.Length;i++){
 var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(paths[i],UnityEditor.SceneManagement.OpenSceneMode.Additive);
 try{
  var roots=scene.GetRootGameObjects();var model=roots.Single(g=>g.name==(i==0?"level_hall":i==1?"level_temple":"DemoObjects"));
  var wrapper=new GameObject(keys[i]);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(wrapper,scene);
  try{
   var copy=UnityEngine.Object.Instantiate(model,wrapper.transform);copy.name=model.name;copy.transform.SetPositionAndRotation(model.transform.position,model.transform.rotation);
   foreach(var c in copy.GetComponentsInChildren<MonoBehaviour>(true).ToArray())if(c!=null && !c.GetType().FullName.StartsWith("UnityEngine.Rendering.Universal.UniversalAdditionalLightData"))UnityEngine.Object.DestroyImmediate(c);
   foreach(var c in copy.GetComponentsInChildren<Camera>(true))UnityEngine.Object.DestroyImmediate(c.gameObject);
   foreach(var c in copy.GetComponentsInChildren<AudioListener>(true))UnityEngine.Object.DestroyImmediate(c);
   foreach(var t in copy.GetComponentsInChildren<Transform>(true).Where(t=>t.name.ToLowerInvariant().Contains("portal")).ToArray())if(t!=null)t.gameObject.SetActive(false);
   if(i==0)foreach(var t in copy.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("MOD_Gateway_Door_01_large_")).ToArray())t.gameObject.SetActive(false);
   if(i==0){var door=copy.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="COMP_Door_01_large" && Mathf.Abs(t.position.z+28)<.1f);door.localScale=new Vector3(1.5f,1.25f,1);door.position+=Vector3.right;}
   if(i==1){
    foreach(var t in copy.GetComponentsInChildren<Transform>(true).Where(t=>t.name.Contains("Railing")&&t.position.z< -16&&t.position.x> -15&&t.position.x< -5).ToArray())t.gameObject.SetActive(false);
    var path=AssetDatabase.FindAssets("P_MOD_Floor_01_O_straight_med t:Prefab",new[]{"Assets/ExternalAssets/Environment/Fantastic Dungeon Pack"}).Select(AssetDatabase.GUIDToAssetPath).Single(p=>System.IO.Path.GetFileNameWithoutExtension(p)=="P_MOD_Floor_01_O_straight_med");
    for(int z=-26;z<=-14;z+=4)foreach(int x in new[]{-12,-8}){var deck=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),wrapper.transform);deck.name="EntranceStoneDeck";var renderers=deck.GetComponentsInChildren<MeshRenderer>();var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);deck.transform.position+=new Vector3(x-b.center.x,-b.max.y,z-b.center.z);}
   }
   if(i==2){
    var entrance=new Bounds(new Vector3(-50,8,44),new Vector3(55,20,10));
    foreach(Transform t in copy.transform)if(t.GetComponentsInChildren<MeshRenderer>(true).Any(r=>r.bounds.Intersects(entrance))&&(t.name.StartsWith("Wood_")||t.name.StartsWith("Fence_")||t.name.StartsWith("Tree_")||t.name.StartsWith("Rock_")||t.name.StartsWith("BigRock_")||t.name.StartsWith("Log_")))t.gameObject.SetActive(false);
    foreach(var r in copy.GetComponentsInChildren<Renderer>(true))if(r.sharedMaterials.Any(m=>m!=null&&m.shader.name.Contains("Portal")))r.gameObject.SetActive(false);
   }
   PrefabUtility.SaveAsPrefabAsset(wrapper,"Assets/03.Prefabs/Environment/OpenWorld/Dungeons/"+keys[i]+".prefab");
  }finally{UnityEngine.Object.DestroyImmediate(wrapper);}
  Physics.SyncTransforms();
  for(int z=-45;z<=65;z+=5)for(int x=-45;x<=45;x+=5){
   var hits=Physics.RaycastAll(new Vector3(x,100,z),Vector3.down,200).Where(h=>h.collider.gameObject.scene==scene&&h.normal.y>.9f).OrderByDescending(h=>h.point.y).ToArray();
   if(hits.Length>0)rows.Add(keys[i]+" "+x+","+z+" floors="+string.Join("|",hits.Select(h=>h.point.y.ToString("F2")+":"+h.collider.name).Distinct().ToArray()));
  }
  if(i==2){var terrain=roots.Single(g=>g.GetComponent<Terrain>()!=null).GetComponent<Terrain>();rows.Add("ARENA TERRAIN "+terrain.transform.position+" "+terrain.terrainData.size+" asset="+AssetDatabase.GetAssetPath(terrain.terrainData));}
 }finally{UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(host);}
}
System.IO.File.WriteAllLines(folder+"/candidate-floors.txt",rows);return "COMPLETE exports; floor samples="+rows.Count;

