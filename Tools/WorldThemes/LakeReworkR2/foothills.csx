// 중앙 통로 양옆에 낮은 능선을 이어 사면의 큰 평면을 나눈다.
var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(EditorApplication.isPlayingOrWillChangePlaymode||s.path!="Assets/01.Scenes/ArtReview/WorldThemes/LakeReworkR2.unity")throw new Exception("검토 씬 필요");
var t=Terrain.activeTerrain;var d=t.terrainData;if(!AssetDatabase.GetAssetPath(d).Contains("ThemeStudies/LakeReworkR2/"))throw new Exception("독립 데이터 필요");
if(System.IO.File.Exists("output/LakeReworkR2/foothills-applied.txt"))throw new Exception("이미 적용됨");
var props=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(q=>PrefabUtility.IsOutermostPrefabInstanceRoot(q.gameObject)&&q.position.x>880&&q.position.x<1270&&q.position.z>985&&q.position.z<1150&&!q.name.Contains("SlopeFoot")&&!q.name.Contains("SlopeShoulder")).ToArray();
var old=props.Select(q=>t.SampleHeight(q.position)).ToArray();var h=d.GetHeights(0,0,d.heightmapResolution,d.heightmapResolution);
for(int z=0;z<h.GetLength(0);z++)for(int x=0;x<h.GetLength(1);x++){
float wx=x*2400f/(h.GetLength(1)-1),wz=-700+z*2800f/(h.GetLength(0)-1);if(wx<880||wx>1270||wz<985||wz>1150)continue;
float left=Mathf.Clamp01(1-Mathf.Pow((wx-971)/74,2)-Mathf.Pow((wz-1060)/70,2));
float right=Mathf.Clamp01(1-Mathf.Pow((wx-1194)/70,2)-Mathf.Pow((wz-1067)/75,2));
h[z,x]+=(18*left*left+23*right*right)/400;
}
d.SetHeights(0,0,h);for(int i=0;i<props.Length;i++){var p=props[i].position;p.y+=t.SampleHeight(p)-old[i];props[i].position=p;}
var a=d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);
for(int z=0;z<a.GetLength(0);z++)for(int x=0;x<a.GetLength(1);x++){
float wx=x*2400f/(a.GetLength(1)-1),wz=-700+z*2800f/(a.GetLength(0)-1);if(wx<865||wx>1280||wz<1000||wz>1260)continue;
float weight=Mathf.Clamp01(Mathf.Min(Mathf.Min((wx-865)/40,(1280-wx)/40),Mathf.Min((wz-1000)/35,(1260-wz)/45)));
float removed=a[z,x,9]*weight*.8f;a[z,x,9]-=removed;a[z,x,7]+=removed;
}
d.SetAlphamaps(0,0,a);EditorUtility.SetDirty(d);AssetDatabase.SaveAssetIfDirty(d);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s);System.IO.File.WriteAllText("output/LakeReworkR2/foothills-applied.txt","기존 식생·암석 접지 높이 동반 조정: "+props.Length);return props.Length;
