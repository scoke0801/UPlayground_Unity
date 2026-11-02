var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("월드 Edit Mode 필요");
var env=GameObject.Find("AuthoredEnvironment");var root=env.transform.Find("랜드마크 유적 구역");if(root==null)throw new Exception("시설 누락");
var boxes=root.GetComponentsInChildren<Transform>(true).Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)&&!PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject).Contains("Wall_")).Select(t=>{var rs=t.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);b.Expand(new Vector3(22,0,22));return b;}).ToArray();
int removed=0;foreach(var t in env.GetComponentsInChildren<Transform>(true).Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)).ToArray()){
 if(t.IsChildOf(root))continue;string path=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);if(!path.Contains("/Foliage/")||t.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true).Length>0)continue;var p=t.position;
 if(boxes.Any(b=>p.x>b.min.x&&p.x<b.max.x&&p.z>b.min.z&&p.z<b.max.z)){UnityEngine.Object.DestroyImmediate(t.gameObject);removed++;}
}
// 원본의 단면 석벽을 양쪽에서 볼 수 있도록 이 시설 전용 재질만 파생한다.
string materialFolder="Assets/10.Datas/World/OpenWorld/LandmarkSites";if(!AssetDatabase.IsValidFolder(materialFolder))AssetDatabase.CreateFolder("Assets/10.Datas/World/OpenWorld","LandmarkSites");
var materials=new Dictionary<Material,Material>();int renderers=0;
foreach(var t in root.GetComponentsInChildren<Transform>(true).Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)&&PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject).Contains("Wall_"))){
 foreach(var r in t.GetComponentsInChildren<Renderer>(true)){var ms=r.sharedMaterials;for(int i=0;i<ms.Length;i++){
  if(ms[i]==null||!ms[i].HasProperty("_Cull"))continue;Material result;if(!materials.TryGetValue(ms[i],out result)){
   string path=materialFolder+"/"+ms[i].name.Replace("_TwoSided","")+"_TwoSided.mat";result=AssetDatabase.LoadAssetAtPath<Material>(path);if(result==null){result=new Material(ms[i]);result.name=ms[i].name+"_TwoSided";result.SetFloat("_Cull",0);result.doubleSidedGI=true;AssetDatabase.CreateAsset(result,path);}materials.Add(ms[i],result);
  }ms[i]=result;
 }r.sharedMaterials=ms;PrefabUtility.RecordPrefabInstancePropertyModifications(r);renderers++;}
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);System.IO.File.WriteAllText("output/OpenWorld/LandmarkSites/polish.txt","추가 식생 정리="+removed+" / 석벽 렌더러="+renderers+" / 양면 재질="+materials.Count);return "석벽 양면 재질 적용="+renderers;
