// 현재 수동 배치를 읽고 후속 저작의 비교 기준을 남긴다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||scene.isDirty||EditorApplication.isCompiling||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("저장된 월드 Edit Mode 필요");
string folder="output/OpenWorld/EnvironmentFollowup";System.IO.Directory.CreateDirectory(folder);
var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
Func<Transform,string> path=null;path=t=>t.parent==null?t.name:path(t.parent)+"/"+t.name;
var ids=all.SelectMany(t=>t.GetComponents<UPlayGround.Components.SceneEntityId>()).Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")).OrderBy(s=>s).ToArray();
if(System.IO.File.Exists(folder+"/identities-before.txt"))throw new Exception("기준 이미 존재");
System.IO.File.WriteAllLines(folder+"/identities-before.txt",ids);
var selected=all.Where(t=>PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)&&path(t).StartsWith("AuthoredEnvironment/")&&((t.position.x>420&&t.position.x<800&&t.position.z>1000&&t.position.z<1450)||(t.position.x>1450&&t.position.x<2100&&t.position.z>100&&t.position.z<1150)||(t.position.x>1800&&t.position.z< -200)||(t.position.x>500&&t.position.x<660&&t.position.z>0&&t.position.z<240))).ToArray();
var rows=selected.Select(t=>{var rs=t.GetComponentsInChildren<Renderer>(true);var b=rs.Length==0?new Bounds(t.position,Vector3.zero):rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return path(t)+"|"+t.position.ToString("F2")+"|"+t.eulerAngles.ToString("F1")+"|"+t.localScale.ToString("F2")+"|"+b.min.ToString("F2")+"|"+b.max.ToString("F2")+"|"+PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);}).ToArray();
System.IO.File.WriteAllLines(folder+"/placement-before.txt",rows);
var terrain=GameObject.Find("AuthoredEnvironment").GetComponentInChildren<Terrain>();
return new{ids=ids.Length,placements=rows.Length,terrain=AssetDatabase.GetAssetPath(terrain.terrainData),layers=terrain.terrainData.terrainLayers.Select(l=>new{path=AssetDatabase.GetAssetPath(l),texture=l.diffuseTexture==null?"":l.diffuseTexture.name,tile=l.tileSize.ToString()}).ToArray()};
