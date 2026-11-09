// 실제 활동 공간 카메라에 걸리는 서쪽 사면. 포털·도착점·채집 지반은 그대로 둔다.
var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(EditorApplication.isPlayingOrWillChangePlaymode||s.path!="Assets/01.Scenes/ArtReview/WorldThemes/LakeReworkR2.unity")throw new Exception("검토 씬 필요");
if(GameObject.Find("WestLakeBackdrop")!=null)throw new Exception("이미 배치됨");
var t=Terrain.activeTerrain;var d=t.terrainData;var root=new GameObject("WestLakeBackdrop");root.transform.SetParent(GameObject.Find("AuthoredEnvironment").transform);
var path="Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Prefabs/Stones/Cliffs/PF_EastLands_Cliff_01.prefab";
var pos=new[]{new Vector3(710,151,1040),new Vector3(835,141,1090)};var scale=new[]{new Vector3(1.6f,2.4f,1.4f),new Vector3(1.65f,2,1.3f)};
for(int i=0;i<2;i++){var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),s);g.name="WestBackdropOutcrop_"+i;g.transform.SetParent(root.transform);g.transform.SetPositionAndRotation(pos[i],Quaternion.Euler(0,i==0?8:-16,i==0?-6:9));g.transform.localScale=scale[i];}
var points=new[]{new Vector2(679,991),new Vector2(697,1003),new Vector2(720,988),new Vector2(739,1008),new Vector2(793,1022),new Vector2(810,1033),new Vector2(827,1012),new Vector2(847,1028),new Vector2(858,1014),new Vector2(872,1042)};
path="Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Prefabs/Foliage/Trees/PF_Farm_GenTree_B_FullTree_01.prefab";
for(int i=0;i<points.Length;i++){var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),s);g.name="WestSlopeCanopy_"+i;g.transform.SetParent(root.transform);var p=new Vector3(points[i].x,0,points[i].y);p.y=t.SampleHeight(p)-.5f;g.transform.SetPositionAndRotation(p,Quaternion.Euler(0,i*137.5f,0));g.transform.localScale=Vector3.one*(1.25f+(i%3)*.2f);}
var a=d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);
for(int z=0;z<a.GetLength(0);z++)for(int x=0;x<a.GetLength(1);x++){
float wx=x*2400f/(a.GetLength(1)-1),wz=-700+z*2800f/(a.GetLength(0)-1);if(wx<610||wx>920||wz<935||wz>1220)continue;
float mask=Mathf.SmoothStep(0,1,Mathf.Clamp01(Mathf.Min(Mathf.Min((wx-610)/50,(920-wx)/50),Mathf.Min((wz-935)/45,(1220-wz)/50))));
float n=Mathf.PerlinNoise(wx*.014f,wz*.012f);float rock=Mathf.Clamp01((d.GetSteepness(wx/2400,(wz+700)/2800)-35)/30)*.35f;
for(int l=0;l<a.GetLength(2);l++)a[z,x,l]*=1-mask;
a[z,x,7]+=mask*(1-rock)*(.9f-n*.18f);a[z,x,8]+=mask*(1-rock)*(.1f+n*.18f);a[z,x,9]+=mask*rock;
}
d.SetAlphamaps(0,0,a);d.SetBaseMapDirty();EditorUtility.SetDirty(d);AssetDatabase.SaveAssetIfDirty(d);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s);return "서쪽 배경 연결, 지형 높이 변경 없음";
