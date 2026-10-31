var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || scene.isDirty || scene.path!=UPlayGround.Editor.OpenWorldSliceAuthoring.HostPath)throw new Exception("저장된 생성 월드 Edit Mode 필요");
var folder="output/OpenWorld/Exploration/Iteration06";var rows=new List<string>{"UTC="+DateTime.UtcNow.ToString("O"),"recipe 기반 증분; 전체 월드 재생성 아님"};
System.IO.File.WriteAllText(folder+"/application-running.txt",DateTime.UtcNow.ToString("O"));
try{
 var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static;var authoring=typeof(UPlayGround.Editor.OpenWorldSliceAuthoring);
 var recipe=authoring.GetMethod("LoadCityRecipe",flags).Invoke(null,null);var landscape=recipe.GetType().GetField("landscape").GetValue(recipe);var type=landscape.GetType();
 int count=int.Parse(System.IO.File.ReadAllText(folder+"/active-sites.txt"));var names=new[]{"SunkenHall","ChainTemple","BriarRing"}.Take(count).ToArray();
 var terrain=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>()).Single();var data=terrain.terrainData;
 var heights=data.GetHeights(0,0,data.heightmapResolution,data.heightmapResolution);var bytes=new byte[heights.Length*4];
 if(!System.IO.File.Exists(folder+"/backup/heights.bin")){Buffer.BlockCopy(heights,0,bytes,0,bytes.Length);System.IO.File.WriteAllBytes(folder+"/backup/heights.bin",bytes);var holes=data.GetHoles(0,0,data.holesResolution,data.holesResolution);var holeBytes=new byte[holes.Length];Buffer.BlockCopy(holes,0,holeBytes,0,holeBytes.Length);System.IO.File.WriteAllBytes(folder+"/backup/holes.bin",holeBytes);}
 bytes=System.IO.File.ReadAllBytes(folder+"/backup/heights.bin");Buffer.BlockCopy(bytes,0,heights,0,bytes.Length);data.SetHeights(0,0,heights);
 var originalHoles=new bool[data.holesResolution,data.holesResolution];var savedHoles=System.IO.File.ReadAllBytes(folder+"/backup/holes.bin");Buffer.BlockCopy(savedHoles,0,originalHoles,0,savedHoles.Length);data.SetHoles(0,0,originalHoles);
 var patches=(Array)type.GetField("dungeonGround").GetValue(landscape);var selected=patches.Cast<object>().Where(p=>count==3||((string)p.GetType().GetField("id").GetValue(p)).StartsWith("hall-")).ToArray();var array=Array.CreateInstance(patches.GetType().GetElementType(),selected.Length);for(int i=0;i<selected.Length;i++)array.SetValue(selected[i],i);type.GetField("dungeonGround").SetValue(landscape,array);
 authoring.GetMethod("ApplyDungeonGround",flags).Invoke(null,new object[]{new[]{terrain},landscape});
 authoring.GetMethod("PaintDungeonGround",flags).Invoke(null,new object[]{new[]{terrain},recipe});
 var environment=scene.GetRootGameObjects().Single(g=>g.name=="AuthoredEnvironment");
 if(count==3){
  var propIds=new HashSet<string>(((Array)type.GetField("props").GetValue(landscape)).Cast<object>().Select(p=>(string)p.GetType().GetField("id").GetValue(p)));
  foreach(var t in environment.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Explore_")&&!propIds.Contains(t.name)).ToArray())if(t!=null)UnityEngine.Object.DestroyImmediate(t.gameObject);
  var placements=environment.GetComponentsInChildren<Transform>(true).Where(t=>propIds.Contains(t.name)).ToDictionary(t=>t.name);
  foreach(var prop in ((Array)type.GetField("props").GetValue(landscape)).Cast<object>()){
   var t=prop.GetType();if((bool)t.GetField("useAbsoluteHeight").GetValue(prop))continue;var p=(Vector3)t.GetField("position").GetValue(prop);Transform item;if(!placements.TryGetValue((string)t.GetField("id").GetValue(prop),out item))continue;
   bool changed=false;foreach(var patch in selected){var pt=patch.GetType();var rect=pt.GetField("bounds").GetValue(patch);var rt=rect.GetType();float m=(float)pt.GetField("transition").GetValue(patch);var b=new Rect((float)rt.GetField("x").GetValue(rect)-m,(float)rt.GetField("y").GetValue(rect)-m,(float)rt.GetField("width").GetValue(rect)+m*2,(float)rt.GetField("height").GetValue(rect)+m*2);changed|=b.Contains(new Vector2(p.x,p.z));}if(!changed)continue;
   p.y=terrain.SampleHeight(p)+terrain.transform.position.y+(float)t.GetField("groundOffset").GetValue(prop);item.position=p;
   if((bool)t.GetField("alignBounds").GetValue(prop)){var b=(Bounds)authoring.GetMethod("GetPropMeshBounds",flags).Invoke(null,new object[]{item.gameObject});item.position+=new Vector3(p.x-b.center.x,p.y-b.min.y,p.z-b.center.z);}
   float embed=(float)t.GetField("slopeEmbedDepth").GetValue(prop);if(embed>0){var b=(Bounds)authoring.GetMethod("GetPropMeshBounds",flags).Invoke(null,new object[]{item.gameObject});float lowGround=p.y-(float)t.GetField("groundOffset").GetValue(prop);foreach(float x in new[]{b.min.x,b.center.x,b.max.x})foreach(float z in new[]{b.min.z,b.center.z,b.max.z})lowGround=Mathf.Min(lowGround,terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y);item.position+=Vector3.up*Mathf.Min(0,lowGround-b.min.y-embed);}
   rows.Add("접변 소품 재접지 "+item.name+" "+item.position);
  }
 }
 var cliff=environment.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Expansion_cliff_768");
 var cliffPosition=new Vector3(265,0,580);cliffPosition.y=terrain.SampleHeight(cliffPosition)+terrain.transform.position.y-11.2f;cliff.position=cliffPosition;
 var cliffBounds=(Bounds)authoring.GetMethod("GetPropMeshBounds",flags).Invoke(null,new object[]{cliff.gameObject});float low=cliffPosition.y+11.2f;foreach(float x in new[]{cliffBounds.min.x,cliffBounds.center.x,cliffBounds.max.x})foreach(float z in new[]{cliffBounds.min.z,cliffBounds.center.z,cliffBounds.max.z})low=Mathf.Min(low,terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y);cliff.position+=Vector3.up*Mathf.Min(0,low-cliffBounds.min.y-4);
 var previous=environment.transform.Find("AuthoredDungeons");if(previous!=null)UnityEngine.Object.DestroyImmediate(previous.gameObject);
 var root=new GameObject("AuthoredDungeons").transform;root.SetParent(environment.transform,false);
 foreach(var prop in ((Array)type.GetField("props").GetValue(landscape)).Cast<object>().Where(p=>names.Contains(((string)p.GetType().GetField("id").GetValue(p)).Replace("Dungeon_","")))){
  var t=prop.GetType();var asset=AssetDatabase.LoadAssetAtPath<GameObject>((string)t.GetField("prefab").GetValue(prop));if(asset==null)throw new Exception("던전 프리팹 누락");var go=(GameObject)PrefabUtility.InstantiatePrefab(asset,root);go.name=(string)t.GetField("id").GetValue(prop);go.transform.position=(Vector3)t.GetField("position").GetValue(prop);rows.Add(go.name+" "+go.transform.position);
 }
 var discoveries=scene.GetRootGameObjects().Single(g=>g.name=="ExplorationDiscoveries").transform;
 foreach(var child in discoveries.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true).Where(t=>t.Guid.StartsWith("c42bb9f480ec4f3cacdf786ab0a59e3")).ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
 foreach(var p in ((Array)type.GetField("pickups").GetValue(landscape)).Cast<object>().Where(p=>((string)p.GetType().GetField("id").GetValue(p)).StartsWith("c42bb9f480ec4f3cacdf786ab0a59e3")).Take(count)){
  var t=p.GetType();string id=(string)t.GetField("id").GetValue(p);var go=new GameObject(id);go.transform.SetParent(discoveries,false);go.transform.position=(Vector3)t.GetField("position").GetValue(p);go.layer=LayerMask.NameToLayer("InteractableObject");var collider=go.AddComponent<SphereCollider>();collider.radius=.8f;collider.center=Vector3.up*.6f;
  var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>((string)t.GetField("model").GetValue(p)),go.transform);visual.transform.localPosition=Vector3.zero;foreach(var c in visual.GetComponentsInChildren<Collider>())c.enabled=false;
  go.AddComponent<UPlayGround.DropItemActor>().Init(AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Item.ItemSO>((string)t.GetField("item").GetValue(p)),(int)t.GetField("count").GetValue(p),AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.InteractableActorSO>("Assets/10.Datas/Actor/Interaction/DropItem.asset"));go.AddComponent<UPlayGround.Components.SceneEntityId>().EditorSetGuid(id);
 }
 Physics.SyncTransforms();authoring.GetMethod("BakeNavigation",flags).Invoke(null,new object[]{scene});
 AssetDatabase.SaveAssetIfDirty(data);AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath("Assets/10.Datas/World/OpenWorld/Navigation.asset"));UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
 rows.Add("COMPLETE sites="+count);System.IO.File.WriteAllLines(folder+"/application-"+count+".txt",rows);return string.Join("\n",rows.ToArray());
}catch(Exception exception){rows.Add(exception.ToString());System.IO.File.WriteAllLines(folder+"/application-failure.txt",rows);throw;}
