// 수관은 유지하고 과대 하층 식생을 실제 키로 낮춰 접근선과 활동 공간을 연다.
var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
string dir="Assets/10.Datas/World/ThemeStudies/ShadeCreekR1/";
if(s.path!="Assets/01.Scenes/ArtReview/WorldThemes/ShadeCreekR1.unity"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("검토 씬 필요");
var root=GameObject.Find("AuthoredEnvironment/월드 구간/그늘숲");var terrain=Terrain.activeTerrain;
string greenPath="Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Prefabs/Foliage/Trees/PF_Farm_GenTree_C_FullTree_01.prefab";
var green=AssetDatabase.LoadAssetAtPath<GameObject>(greenPath).GetComponentsInChildren<Renderer>(true);
var materials=new Dictionary<Material,Material>();
Func<Material,Material> copy=m=>{if(materials.ContainsKey(m))return materials[m];var c=UnityEngine.Object.Instantiate(m);string p=dir+"Canopy_"+materials.Count+".mat";if(c.HasProperty("_Foliage_Color_Top")){c.SetColor("_Foliage_Color_Top",new Color(.40f,.66f,.48f));c.SetColor("_Foliage_Color_Bottom",new Color(.18f,.42f,.29f));c.SetColor("_Always_On_Fresnel_Color",new Color(.40f,.64f,.45f,0));}AssetDatabase.CreateAsset(c,p);materials.Add(m,c);return c;};
var rows=new List<string>();int trees=0,bushes=0;
foreach(var parent in new[]{root.transform.Find("식생"),root.transform.Find("성 주변 숲")})foreach(Transform q in parent){
var p=q.position;if(p.x<1500||p.x>1810||p.z<680||p.z>980)continue;
string source=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(q.gameObject);var rs=q.GetComponentsInChildren<Renderer>(true);
if(source.EndsWith("PF_Farm_GenTree_C_FullTree_01_Red.prefab")){
if(rs.Length!=green.Length||rs.Any(r=>r.HasPropertyBlock()))throw new Exception("수관 구조 또는 MPB 재검토 필요 "+q.name);
for(int i=0;i<rs.Length;i++){var mats=rs[i].sharedMaterials;var gm=green[i].sharedMaterials;if(mats.Length!=gm.Length)throw new Exception("재질 슬롯 불일치");int slot=i==3?0:1;mats[slot]=copy(gm[slot]);rs[i].sharedMaterials=mats;PrefabUtility.RecordPrefabInstancePropertyModifications(rs[i]);}
rows.Add("tree\t"+GlobalObjectId.GetGlobalObjectIdSlow(q)+"\t"+q.name+"\t4 LOD including green impostor");trees++;
}else if(source.Contains("/Bushes/")&&p.x>1550&&p.x<1775&&p.z>710&&p.z<915&&rs.Length>0){
float height=rs[0].bounds.size.y;if(height<2)continue;
var before=q.localScale;float target=1.1f+Mathf.PerlinNoise(p.x*.13f,p.z*.13f)*.65f;q.localScale*=target/height;
float bottom=q.GetComponentsInChildren<Renderer>(true)[0].bounds.min.y;q.position+=Vector3.up*(terrain.SampleHeight(p)-bottom-.08f);
PrefabUtility.RecordPrefabInstancePropertyModifications(q);rows.Add("bush\t"+GlobalObjectId.GetGlobalObjectIdSlow(q)+"\t"+q.name+"\t"+before+" -> "+q.localScale);bushes++;
}
}
System.IO.File.WriteAllLines("output/ShadeCreekR1/composition.tsv",rows);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(s);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s);
return new{trees,bushes,scope="수목 기하·줄기·기존 진행/적/수면 불변. 관목 실척과 전용 수관 재질 변경"};
