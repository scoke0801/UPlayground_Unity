// 동쪽 정예 Griffin까지 함께 보호한다. 신규 수관만 조정한다.
var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(s.path!="Assets/01.Scenes/ArtReview/WorldThemes/RedForestR1.unity"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("검토 씬 필요");
var root=GameObject.Find("RedForestR1_Canopy");var centers=new[]{new Vector2(1150,525),new Vector2(1230,560)};var rows=new List<string>();
foreach(Transform q in root.transform){bool touched=false;for(int step=0;step<60;step++){
var b=q.GetComponentsInChildren<Renderer>(true).First().bounds;int failed=-1;float gap=999;
for(int i=0;i<centers.Length;i++){float dx=Mathf.Max(b.min.x-centers[i].x,0,centers[i].x-b.max.x),dz=Mathf.Max(b.min.z-centers[i].y,0,centers[i].y-b.max.z);float dist=Mathf.Sqrt(dx*dx+dz*dz);if(dist<34){failed=i;gap=dist;break;}}
if(failed<0)break;if(step==0)q.localScale*=.72f;var away=new Vector3(q.position.x-centers[failed].x,0,q.position.z-centers[failed].y).normalized;var p=q.position+away*(35-gap);p.y=Terrain.activeTerrain.SampleHeight(p)-.25f;q.position=p;touched=true;if(step==59)throw new Exception("비행 공간 조정 실패 "+q.name);
}if(touched){PrefabUtility.RecordPrefabInstancePropertyModifications(q);rows.Add(q.name+" "+q.position.ToString("F3")+" scale="+q.localScale.ToString("F3"));}}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(s);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s);System.IO.File.WriteAllLines("output/RedForestR1/east-flight-adjustment.txt",rows);return rows;

