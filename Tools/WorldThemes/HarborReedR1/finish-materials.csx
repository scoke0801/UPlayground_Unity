if(Application.dataPath!="C:/UsingProject/UnityProject/UPlayground/Assets"||!System.IO.File.ReadAllText("Tools/WorldThemes/AgentCoordination/editor.lock").Contains("\"C\""))throw new Exception("C 작업권 필요");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/HarborReedR1.unity"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("C 씬 필요");
if(GameObject.Find("HarborReedR1_ShoreReeds")!=null)throw new Exception("중복 제작 금지");
string folder="Assets/10.Datas/World/ThemeStudies/HarborReedR1";
var harbor=GameObject.Find("AuthoredEnvironment").transform.Find("월드 구간/항구");var deck=harbor.Find("해안/목재 잔교");if(deck==null)throw new Exception("잔교 소유 경로 확인 필요");
var changes=new List<object>();var map=new Dictionary<Material,Material>();
foreach(var renderer in deck.GetComponentsInChildren<Renderer>(true)){
 var mats=renderer.sharedMaterials;
 for(int i=0;i<mats.Length;i++){var original=mats[i];if(original==null)continue;Material copy;
 if(!map.TryGetValue(original,out copy)){
  string destination=folder+"/Wet_"+original.name+".mat";
  if(!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(original),destination))throw new Exception("재질 복제 실패 "+destination);
  copy=AssetDatabase.LoadAssetAtPath<Material>(destination);Color color=original.GetColor("_BaseColor");color=new Color(color.r*.60f,color.g*.64f,color.b*.70f,1);copy.SetColor("_BaseColor",color);if(copy.HasProperty("_Color"))copy.SetColor("_Color",color);copy.SetFloat("_Smoothness",.32f);EditorUtility.SetDirty(copy);AssetDatabase.SaveAssetIfDirty(copy);map.Add(original,copy);
 }
 mats[i]=copy;changes.Add(new{renderer=GlobalObjectId.GetGlobalObjectIdSlow(renderer).ToString(),slot=i,before=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(original)),after=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(copy))});
 }renderer.sharedMaterials=mats;PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
}
string reedSource="Assets/ExternalAssets/Environment/Idyllic Fantasy Nature/Prefabs/Reeds_03.prefab";
if(!AssetDatabase.CopyAsset(reedSource,folder+"/HarborReeds.prefab"))throw new Exception("갈대 복제 실패");
var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/HarborReeds.prefab");
var group=new GameObject("HarborReedR1_ShoreReeds");var terrain=Terrain.activeTerrain;var placements=new List<object>();
var clusters=new[]{new Vector2(1828,-343),new Vector2(1835,-336),new Vector2(1844,-332),new Vector2(1852,-333)};
var existing=harbor.GetComponentsInChildren<Collider>(true).Where(c=>c.enabled&&!c.isTrigger).ToArray();
for(int c=0;c<clusters.Length;c++)for(int i=0;i<8;i++){
 float angle=i*2.399963f+c*.4f;float radius=.5f+Mathf.Sqrt(i)*.72f;var p=new Vector3(clusters[c].x+Mathf.Cos(angle)*radius,0,clusters[c].y+Mathf.Sin(angle)*radius);float ground=terrain.SampleHeight(p)+terrain.transform.position.y;
 if(ground<44.7f||ground>46.8f||existing.Any(col=>col.bounds.Contains(new Vector3(p.x,50,p.z))))continue;
 var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);go.name="HarborReed_"+c+"_"+i;go.transform.SetParent(group.transform,false);go.transform.position=new Vector3(p.x,ground,p.z);go.transform.rotation=Quaternion.Euler(0,i*137.5f+c*31,0);
 var renderers=go.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)throw new Exception("갈대 렌더러 누락");var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
 float height=1.25f+.35f*Mathf.Sin(i*1.9f+c);float scale=height/Mathf.Max(.01f,bounds.size.y);go.transform.localScale*=scale;
 bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);go.transform.position+=Vector3.up*(ground-.08f-bounds.min.y);
 foreach(var col in go.GetComponentsInChildren<Collider>(true))col.enabled=false;
 placements.Add(new{name=go.name,prototypeGuid=AssetDatabase.AssetPathToGUID(folder+"/HarborReeds.prefab"),position=new[]{go.transform.position.x,go.transform.position.y,go.transform.position.z},yaw=go.transform.eulerAngles.y,scale=new[]{go.transform.localScale.x,go.transform.localScale.y,go.transform.localScale.z},ground});
}
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(a=>a!=null);System.IO.File.WriteAllText("output/HarborReedR1/material-vegetation-changes.json",(string)json.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{new{changes,placements,water="기존 CoastalWater 크기·높이·재질 보존",colliders="신규 갈대 Collider 비활성; 상판/석축 지지면 변경 없음"}}));
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);return new{materials=map.Count,renderers=changes.Count,reeds=placements.Count};
