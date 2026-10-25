var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.name!="StonegateCitadel"||Application.isPlaying)throw new System.Exception("돌등성 Edit Mode 필요");
var root=GameObject.Find("StonegateEastArt");
if(root.transform.Find("GateDeck")!=null)throw new System.Exception("이미 마감됨");
string kit="Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Buildings/Building Kit/";
System.Func<string,Vector3,Vector3,GameObject> Fit=(name,position,size)=>{
 var obj=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(kit+"TGV_Beam_01A.prefab"),scene);obj.name=name;obj.transform.SetParent(root.transform,false);
 var rs=obj.GetComponentsInChildren<Renderer>();var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
 obj.transform.localScale=new Vector3(size.x/bounds.size.x,size.y/bounds.size.y,size.z/bounds.size.z);
 bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);obj.transform.position=position-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
 foreach(var t in obj.GetComponentsInChildren<Transform>())t.gameObject.layer=LayerMask.NameToLayer("Ground");return obj;
};
Fit("GateDeck",new Vector3(180,14.65f,120),new Vector3(21,.45f,5.8f));
Fit("GateFrontRail",new Vector3(180,15.95f,117.2f),new Vector3(21,.18f,.18f));
for(int i=0;i<18;i++)Fit("GateBaluster_"+i,new Vector3(169.8f+i*1.2f,15.1f,117.2f),new Vector3(.14f,.85f,.14f));
// 천장 밑 보조 가새는 실제 체적을 갖고 실루엣을 지탱한다.
for(int side=0;side<2;side++){
 var brace=Fit("GateDiagonalBrace",new Vector3(side==0?172:188,13.3f,117.5f),new Vector3(.3f,2,.3f));brace.transform.rotation=Quaternion.Euler(0,0,side==0?-35:35);
}
string directory="Assets/10.Datas/World/WorldCities/StonegateCitadel/EastArt";System.IO.Directory.CreateDirectory(directory);
var colors=new[]{new Color(.65f,.73f,.78f),new Color(.76f,.64f,.46f),new Color(.48f,.60f,.67f),new Color(.55f,.60f,.48f),new Color(.65f,.43f,.31f),new Color(.48f,.56f,.65f),new Color(.63f,.59f,.46f),new Color(.45f,.59f,.54f),new Color(.69f,.52f,.56f),new Color(.61f,.58f,.75f),new Color(.69f,.61f,.45f),new Color(.49f,.48f,.41f)};
var actors=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UPlayGround.NpcActor>()).OrderBy(a=>a.name).ToArray();
var evidence=new System.Collections.Generic.List<string>();
for(int i=0;i<actors.Length;i++){
 var copies=new System.Collections.Generic.Dictionary<Material,Material>();
 foreach(var renderer in actors[i].GetComponentsInChildren<Renderer>()){
  var materials=renderer.sharedMaterials;bool changed=false;
  for(int slot=0;slot<materials.Length;slot++){
   var original=materials[slot];if(original==null||!(original.name.Contains("Armor")||original.name.Contains("Canvas")))continue;
   Material copy;if(!copies.TryGetValue(original,out copy)){copy=new Material(original);copy.name=actors[i].name+"_Outfit_"+copies.Count;if(copy.HasProperty("_Color"))copy.SetColor("_Color",colors[i%colors.Length]);UnityEditor.AssetDatabase.CreateAsset(copy,directory+"/"+copy.name+".mat");copies.Add(original,copy);}
   materials[slot]=copy;changed=true;
  }
  if(changed){renderer.sharedMaterials=materials;UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);}
 }
 evidence.Add(actors[i].name+" original position="+actors[i].transform.position+" dedicated outfit materials="+copies.Count);
}
foreach(var component in root.GetComponentsInChildren<Component>(true)){if(component==null)continue;UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(component);if(component is Transform)UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(component.gameObject);}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
System.IO.File.WriteAllLines("output/StonegateEast/resident-appearance.txt",evidence.ToArray());
return "성문 바닥/난간/가새 및 주민 전용 의상 재질 반영";
