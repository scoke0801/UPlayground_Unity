var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.name!="SaltwindHarbor"||Application.isPlaying)throw new System.Exception("항구 Edit Mode 필요");
var art=GameObject.Find("HarborEastArt");var env=GameObject.Find("CityEnvironment");var terrain=env.GetComponentInChildren<Terrain>();var data=terrain.terrainData;
string directory="Assets/10.Datas/World/WorldCities/SaltwindHarbor/EastArt";
var old=env.GetComponentsInChildren<Transform>().Where(t=>UnityEditor.PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)&&UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject)=="Assets/03.Prefabs/Environment/WorldCities/HarborHouse.prefab").ToArray();
foreach(var t in old){var p=t.position;t.gameObject.SetActive(false);UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);if(p.x>220&&p.z>160)continue;var house=UnityEngine.Object.Instantiate(art.transform.Find("CoastalTileHouse_1").gameObject,art.transform);house.name="OuterCoastalHouse_"+p.x;house.transform.position=p;}
// 염전 네 모서리까지 같은 높이를 확보하고 주변만 경사로 연결한다.
int n=data.heightmapResolution;var heights=data.GetHeights(0,0,n,n);
for(int z=0;z<n;z++)for(int x=0;x<n;x++){float wx=x*data.size.x/(n-1),wz=z*data.size.z/(n-1);if(wx>=225&&wx<=251&&wz>=169&&wz<=206)heights[z,x]=5.8f/data.size.y;}
data.SetHeights(0,0,heights);UnityEditor.EditorUtility.SetDirty(data);UnityEditor.AssetDatabase.SaveAssetIfDirty(data);
foreach(var t in env.GetComponentsInChildren<Transform>().Where(t=>!t.IsChildOf(art.transform)&&t.name.Contains("Paving")&&t.position.x<150&&t.position.z>125&&t.position.z<215).ToArray()){t.gameObject.SetActive(false);UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);}
// 호수 인물 준의 전용 부품이 남은 두 주민은 기존 역할형 모델의 외형만 이식한다.
var actorNames=new[]{"NPC_Harbor_Minsu","NPC_Harbor_Taesu"};var modelNames=new[]{"Worker_03","Merchant_04"};
var sources=new GameObject[2];var targets=new GameObject[2];
for(int i=0;i<2;i++){
 targets[i]=GameObject.Find(actorNames[i]);sources[i]=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Characters/EquipmentReview/RoleVariants/r02_Performance/"+modelNames[i]+".prefab");
 if(targets[i]==null||sources[i]==null||targets[i].transform.Find("HarborRoleAppearance")!=null)throw new System.Exception("외형 대상 확인/재실행 금지");
 var names=new System.Collections.Generic.HashSet<string>(targets[i].GetComponentsInChildren<Transform>(true).Select(t=>t.name));
 if(sources[i].GetComponentsInChildren<SkinnedMeshRenderer>().Any(r=>r.rootBone==null||r.bones.Any(b=>b==null||!names.Contains(b.name))))throw new System.Exception("리그 매핑 불가: "+actorNames[i]);
}
var report=new System.Collections.Generic.List<string>();
for(int i=0;i<2;i++){
 var target=targets[i];var boneMap=target.GetComponentsInChildren<Transform>(true).GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
 foreach(var r in target.GetComponentsInChildren<Renderer>()){r.enabled=false;UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
 var visual=new GameObject("HarborRoleAppearance");visual.transform.SetParent(target.transform,false);
 foreach(var source in sources[i].GetComponentsInChildren<SkinnedMeshRenderer>()){
  var part=new GameObject(source.name);part.transform.SetParent(visual.transform,false);part.layer=target.layer;
  part.transform.localPosition=sources[i].transform.InverseTransformPoint(source.transform.position);part.transform.localRotation=Quaternion.Inverse(sources[i].transform.rotation)*source.transform.rotation;part.transform.localScale=source.transform.lossyScale;
  var skin=part.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=source.sharedMesh;skin.sharedMaterials=source.sharedMaterials;skin.bones=source.bones.Select(b=>boneMap[b.name]).ToArray();skin.rootBone=boneMap[source.rootBone.name];skin.localBounds=source.localBounds;skin.updateWhenOffscreen=true;
 }
 report.Add(actorNames[i]+" visual="+modelNames[i]+"; original actor/Animator/bones/position preserved");
}
var colors=new[]{new Color(.53f,.62f,.70f),new Color(.68f,.45f,.37f),new Color(.66f,.65f,.52f),new Color(.40f,.56f,.62f),new Color(.71f,.57f,.39f),new Color(.44f,.58f,.43f),new Color(.66f,.47f,.58f),new Color(.48f,.42f,.36f)};
var actors=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UPlayGround.NpcActor>()).OrderBy(a=>a.name).ToArray();
for(int i=0;i<actors.Length;i++){
 var copies=new System.Collections.Generic.Dictionary<Material,Material>();
 foreach(var r in actors[i].GetComponentsInChildren<Renderer>().Where(r=>r.enabled)){
  var mats=r.sharedMaterials;bool changed=false;
  for(int j=0;j<mats.Length;j++){var original=mats[j];if(original==null||original.name.Contains("Skin")||original.name.Contains("Eye")||original.name.Contains("Face"))continue;
   Material copy;if(!copies.TryGetValue(original,out copy)){copy=new Material(original);copy.name=actors[i].name+"_Look_"+copies.Count;if(copy.HasProperty("_Color"))copy.SetColor("_Color",original.name.Contains("Hair")?Color.Lerp(colors[i],new Color(.17f,.13f,.1f),.65f):colors[i]);UnityEditor.AssetDatabase.CreateAsset(copy,directory+"/"+copy.name+".mat");copies.Add(original,copy);}mats[j]=copy;changed=true;
  }
  if(changed){r.sharedMaterials=mats;UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
 }
 report.Add(actors[i].name+" unique material palette="+copies.Count);
}
foreach(var c in art.GetComponentsInChildren<Component>(true)){if(c==null)continue;UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(c);if(c is Transform)UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(c.gameObject);}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);System.IO.File.WriteAllLines("output/HarborEast/resident-appearance.txt",report.ToArray());
return "잔존 주택/염전 접지/주민 외형 이식 완료. 리그/애니메이션 확인 필요";
