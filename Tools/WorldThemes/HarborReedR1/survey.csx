if(Application.dataPath!="C:/UsingProject/UnityProject/UPlayground/Assets"||!System.IO.File.ReadAllText("Tools/WorldThemes/AgentCoordination/editor.lock").Contains("\"C\""))throw new Exception("C 작업권 필요");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/HarborReedR1.unity"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("C 검토 씬 Edit Mode 필요");
var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
Func<Transform,string> path=t=>{string p=t.name;while(t.parent!=null){t=t.parent;p=t.name+"/"+p;}return p;};
Func<Vector3,float[]> vec=v=>new[]{v.x,v.y,v.z};
var bounds=new Bounds(new Vector3(1910,50,-330),new Vector3(230,100,160));
var renderers=all.Select(t=>t.GetComponent<Renderer>()).Where(r=>r!=null&&r.bounds.Intersects(bounds)).Select(r=>new{path=path(r.transform),id=GlobalObjectId.GetGlobalObjectIdSlow(r).ToString(),min=vec(r.bounds.min),max=vec(r.bounds.max),pos=vec(r.transform.position),materials=r.sharedMaterials.Select(m=>AssetDatabase.GetAssetPath(m)).ToArray()}).ToArray();
var terrainComponent=all.Select(g=>g.GetComponent<Terrain>()).First(g=>g!=null);var d=terrainComponent.terrainData;
var samples=new List<object>();
for(float z=-380;z<=-290;z+=5)for(float x=1800;x<=2000;x+=5){var p=new Vector3(x,0,z);samples.Add(new{x,z,y=terrainComponent.SampleHeight(p)+terrainComponent.transform.position.y});}
var ids=all.SelectMany(g=>g.GetComponents<UPlayGround.Components.SceneEntityId>()).Select(c=>new{path=path(c.transform),id=c.Guid,owner=new SerializedObject(c).FindProperty("_ownerKey").stringValue,pos=vec(c.transform.position)}).ToArray();
var terrain=new{path=AssetDatabase.GetAssetPath(d),position=vec(terrainComponent.transform.position),size=vec(d.size),heightResolution=d.heightmapResolution,alphaWidth=d.alphamapWidth,alphaHeight=d.alphamapHeight,detailWidth=d.detailWidth,detailHeight=d.detailHeight,scatter=d.detailScatterMode.ToString(),layers=d.terrainLayers.Select((l,i)=>new{index=i,path=AssetDatabase.GetAssetPath(l),guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(l)),texture=AssetDatabase.GetAssetPath(l.diffuseTexture),size=l.tileSize.ToString()}).ToArray(),details=d.detailPrototypes.Select(p=>new{prefab=AssetDatabase.GetAssetPath(p.prototype),texture=AssetDatabase.GetAssetPath(p.prototypeTexture)}).ToArray(),trees=d.treePrototypes.Select(p=>AssetDatabase.GetAssetPath(p.prefab)).ToArray(),treeInstances=d.treeInstanceCount};
var result=new{scene=scene.path,terrain,renderers,samples,ids};
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(a=>a!=null);
System.IO.File.WriteAllText("output/HarborReedR1/survey.json",(string)json.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{result}));
return new{renderers=renderers.Length,ids=ids.Length,terrain};

