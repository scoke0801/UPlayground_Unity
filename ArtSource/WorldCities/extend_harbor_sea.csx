// 톤 기준판 승인과 독립적으로 허용된 항구 수면 끝 수정만 수행한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.name!="SaltwindHarbor"||Application.isPlaying||UnityEditor.EditorApplication.isCompiling)throw new System.Exception("항구 Edit Mode 필요");
var water=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).First(t=>t.name=="CoastalWater");
string folder="Assets/10.Datas/World/WorldCities/SaltwindHarbor/SeaReview";
System.IO.Directory.CreateDirectory(folder);
var material=new Material(water.GetComponent<Renderer>().sharedMaterial);material.name="SaltwindOpenSea";
material.SetFloat("_ShoreFade",0);material.SetFloat("_Recession",0);
material.SetFloat("_FoamWidth",1.4f);material.SetFloat("_FoamStrength",.65f);material.SetFloat("_FoamScale",2.2f);
material.SetColor("_FoamColor",new Color(.77f,.88f,.83f,1));
UnityEditor.AssetDatabase.CreateAsset(material,folder+"/OpenSea.mat");
water.GetComponent<Renderer>().sharedMaterial=material;
water.localScale=new Vector3(2000,1,2000);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
return "수면 20km × 20km, 해안 깊이 기반 포말. 좌표/진행 데이터 불변";
