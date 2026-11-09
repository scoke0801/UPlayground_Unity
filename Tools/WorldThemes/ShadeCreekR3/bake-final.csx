// 생성 완료를 확인한 데이터만 에셋으로 저장·연결한다. 등록 데이터는 먼저 해제한다.
var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string path="Assets/10.Datas/World/ThemeStudies/ShadeCreekR3/NavigationR3Final.asset";
if(s.path!="Assets/01.Scenes/ArtReview/WorldThemes/ShadeCreekR3.unity"||s.isDirty||EditorApplication.isPlayingOrWillChangePlaymode||System.IO.File.Exists(path))throw new Exception("저장된 검토본 최초 최종 베이크만 허용");
var surface=GameObject.Find("AuthoredEnvironment").GetComponent<Unity.AI.Navigation.NavMeshSurface>();
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var sources=(List<UnityEngine.AI.NavMeshBuildSource>)surface.GetType().GetMethod("CollectSources",flags).Invoke(surface,null);
var bounds=(Bounds)surface.GetType().GetMethod("CalculateWorldBounds",flags).Invoke(surface,new object[]{sources});
if(sources.Count<1||bounds.size.sqrMagnitude<1)throw new Exception("실제 지형 수집 실패");
var data=UnityEngine.AI.NavMeshBuilder.BuildNavMeshData(surface.GetBuildSettings(),sources,bounds,surface.transform.position,surface.transform.rotation);
if(data==null||data.sourceBounds.size.sqrMagnitude<1)throw new Exception("NavMesh 생성 실패: 기존 데이터 유지");
AssetDatabase.CreateAsset(data,path);surface.RemoveData();surface.navMeshData=data;surface.AddData();EditorUtility.SetDirty(surface);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(s);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s);
int indices=UnityEngine.AI.NavMesh.CalculateTriangulation().indices.Length;if(indices<3)throw new Exception("삼각형이 없는 NavMesh");
System.IO.File.WriteAllText("output/ShadeCreekR3/navigation-final.txt","수로 단면과 콜라이더를 포함한 최종 검토본 geometry="+sources.Count+"; triangle indices="+indices+"; 원본 NavMesh 미수정");return new{sources=sources.Count,indices};



