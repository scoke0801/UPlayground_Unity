// 기존 수종의 전용 재질로 모든 LOD를 함께 맞추고, 전투 상공 밖에서 수관을 연결한다.
var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(EditorApplication.isPlayingOrWillChangePlaymode||s.path!="Assets/01.Scenes/ArtReview/WorldThemes/RedForestR1.unity")throw new Exception("검토 씬 필요");
string dir="Assets/10.Datas/World/ThemeStudies/RedForestR1";
var t=Terrain.activeTerrain;var red=GameObject.Find("AuthoredEnvironment/월드 구간/붉은나무숲");
var mats=new Dictionary<string,Material>();
foreach(string lod in new[]{"Close","Far"})for(int palette=0;palette<2;palette++){
string key=lod+palette,path=dir+"/Canopy"+key+".mat";
var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Materials/Foliage/Trees/Leaves/MI_Farm_GenericTree_Leaves_"+lod+"_Red.mat"));AssetDatabase.CreateAsset(m,path);}
m.SetColor("_Foliage_Color_Top",palette==0?new Color(.65f,.145f,.095f,1):new Color(.78f,.235f,.105f,1));m.SetColor("_Foliage_Color_Bottom",palette==0?new Color(.27f,.055f,.045f,1):new Color(.40f,.075f,.045f,1));m.SetColor("_Always_On_Fresnel_Color",new Color(.43f,.13f,.07f,0));EditorUtility.SetDirty(m);AssetDatabase.SaveAssetIfDirty(m);mats[key]=m;}
var root=GameObject.Find("RedForestR1_Canopy");if(root==null)root=new GameObject("RedForestR1_Canopy");
var poses=new[]{new Vector3(1110,1.05f,543),new Vector3(1107,1.2f,562),new Vector3(1118,1.0f,577),new Vector3(1109,1.15f,590),new Vector3(1148,.95f,596),new Vector3(1168,1.1f,580),new Vector3(1184,1.2f,585),new Vector3(1193,1.1f,567),new Vector3(1198,1.0f,547),new Vector3(1195,1.05f,525),new Vector3(1187,.95f,493),new Vector3(1120,1.0f,486),new Vector3(1104,1.05f,520),new Vector3(1172,1.0f,607)};
string prefabPath="Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Prefabs/Foliage/Trees/PF_Farm_GenTree_C_FullTree_01_Red.prefab";
for(int i=0;i<poses.Length;i++){
string name="Canopy_"+i.ToString("D2");var existing=root.transform.Find(name);var g=existing!=null?existing.gameObject:(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath),s);g.name=name;g.transform.SetParent(root.transform,true);var p=poses[i];g.transform.position=new Vector3(p.x,t.SampleHeight(new Vector3(p.x,0,p.z))-.12f,p.z);g.transform.rotation=Quaternion.Euler(0,i*137.5f,0);g.transform.localScale=Vector3.one*p.y;
}
int renderers=0;
foreach(var r in red.GetComponentsInChildren<Renderer>(true).Concat(root.GetComponentsInChildren<Renderer>(true))){var p=r.transform.position;if(p.x<1085||p.x>1210||p.z<474||p.z>613)continue;var a=r.sharedMaterials;bool changed=false;int palette=p.x<1140?0:1;for(int i=0;i<a.Length;i++){if(a[i]==null||a[i].shader.name!="UPlayGround/Foliage/LeavesGraph")continue;string lod=a[i].name.Contains("Close")?"Close":"Far";a[i]=mats[lod+palette];changed=true;}if(changed){r.sharedMaterials=a;PrefabUtility.RecordPrefabInstancePropertyModifications(r);renderers++;}}
// 출구에서 떠 있던 비콘텐츠 장식 바위는 실제 메시 최저점으로 접지한다.
var rock=red.GetComponentsInChildren<Transform>(true).Single(q=>q.name=="RockDebris_187_R2");
float min=float.MaxValue;foreach(var mf in rock.GetComponentsInChildren<MeshFilter>(true)){var mesh=mf.sharedMesh;if(mesh==null)continue;foreach(var v in mesh.vertices){var p=mf.transform.TransformPoint(v);min=Mathf.Min(min,p.y-t.SampleHeight(p));}}
rock.position-=Vector3.up*(min+.16f);PrefabUtility.RecordPrefabInstancePropertyModifications(rock);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(s);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s);
System.IO.File.WriteAllText("output/RedForestR1/canopy-edit.txt","new canopy trees="+poses.Length+"; all LOD renderer overrides="+renderers+"; exit decorative rock vertical correction="+(min+.16f));return new{renderers,trees=poses.Length,rockShift=min+.16f};
