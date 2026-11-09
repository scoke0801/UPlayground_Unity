// 거목 양옆의 사면을 암반과 수관 덩어리로 연결한다. 중앙 북쪽 경로는 비운다.
var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(EditorApplication.isPlayingOrWillChangePlaymode||s.path!="Assets/01.Scenes/ArtReview/WorldThemes/LakeReworkR2.unity")throw new Exception("검토 씬 필요");
if(GameObject.Find("LakeRework_SlopeCanopy")!=null)throw new Exception("이미 적용한 배치");
var t=Terrain.activeTerrain;var d=t.terrainData;if(!AssetDatabase.GetAssetPath(d).Contains("ThemeStudies/LakeReworkR2/"))throw new Exception("독립 데이터 필요");
var root=new GameObject("LakeRework_SlopeCanopy");
var positions=new[]{new Vector2(930,1045),new Vector2(944,1058),new Vector2(953,1040),new Vector2(966,1063),new Vector2(982,1045),new Vector2(993,1067),new Vector2(1008,1052),new Vector2(1016,1078),new Vector2(922,1080),new Vector2(938,1091),new Vector2(968,1095),new Vector2(995,1100),new Vector2(1150,1038),new Vector2(1166,1051),new Vector2(1184,1035),new Vector2(1198,1058),new Vector2(1213,1048),new Vector2(1228,1068),new Vector2(1140,1065),new Vector2(1160,1081),new Vector2(1180,1076),new Vector2(1207,1087),new Vector2(1239,1082),new Vector2(1216,1100)};
for(int i=0;i<positions.Length;i++){
string path="Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Prefabs/Foliage/Trees/PF_Farm_GenTree_"+(i%2==0?"A":"B")+"_FullTree_01.prefab";
var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),s);g.name="SlopeCanopy_"+i;g.transform.SetParent(root.transform);var p=new Vector3(positions[i].x,0,positions[i].y);p.y=t.SampleHeight(p)-.5f;g.transform.SetPositionAndRotation(p,Quaternion.Euler(0,i*137.5f,0));g.transform.localScale=Vector3.one*(1.05f+(i%4)*.16f);
}
var a=d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);
for(int z=0;z<a.GetLength(0);z++)for(int x=0;x<a.GetLength(1);x++){
float wx=x*2400f/(a.GetLength(1)-1),wz=-700+z*2800f/(a.GetLength(0)-1);if(wx<850||wx>1290||wz<1000||wz>1270)continue;
float mask=Mathf.SmoothStep(0,1,Mathf.Clamp01(Mathf.Min(Mathf.Min((wx-850)/60,(1290-wx)/60),Mathf.Min((wz-1000)/45,(1270-wz)/60))));
float slope=d.GetSteepness(wx/2400,(wz+700)/2800),n=Mathf.PerlinNoise(wx*.009f,wz*.012f);float rock=Mathf.Clamp01((slope-30)/35)*.65f;
for(int l=0;l<a.GetLength(2);l++)a[z,x,l]*=1-mask;
a[z,x,7]+=(1-rock)*mask*(.9f-n*.12f);a[z,x,8]+=(1-rock)*mask*(.1f+n*.12f);a[z,x,9]+=rock*mask;
}
d.SetAlphamaps(0,0,a);var foot=GameObject.Find("WestSlopeFoot").transform;foot.position=new Vector3(945,137,1124);foot.localScale=new Vector3(2.45f,2.8f,1.7f);
EditorUtility.SetDirty(d);AssetDatabase.SaveAssetIfDirty(d);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s);return "양옆 사면 암반·수관·초지 연결";
