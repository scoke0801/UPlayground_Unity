// 사당 지반과 서쪽 연결로를 남기고 양끝 호안 높이를 수면으로 내린다.
var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(EditorApplication.isPlayingOrWillChangePlaymode||s.path!="Assets/01.Scenes/ArtReview/WorldThemes/LakeReworkR2.unity")throw new Exception("검토 씬 필요");
var t=Terrain.activeTerrain;var d=t.terrainData;var h=d.GetHeights(0,0,d.heightmapResolution,d.heightmapResolution);
var content=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MonoBehaviour>(true)).Where(c=>c!=null&&(c.GetType().Name=="SceneEntityId"||c.GetType().Name=="FlowGraphTriggerVolume")).Select(c=>c.transform.position).Where(p=>p.x>1000&&p.x<1150&&p.z>780&&p.z<850).ToArray();
var water=GameObject.Find("LakeWater");var proxy=new GameObject("WaterCoverageProbe");proxy.transform.SetPositionAndRotation(water.transform.position,water.transform.rotation);proxy.transform.localScale=water.transform.lossyScale;var mc=proxy.AddComponent<MeshCollider>();mc.sharedMesh=water.GetComponent<MeshFilter>().sharedMesh;
try{for(int z=0;z<h.GetLength(0);z++)for(int x=0;x<h.GetLength(1);x++){
float wx=x*2400f/(h.GetLength(1)-1),wz=-700+z*2800f/(h.GetLength(0)-1);if(wx<1005||wx>1145||wz<800||wz>841)continue;
float west=Mathf.Clamp01(1-Mathf.Pow((wx-1025)/24,2)-Mathf.Pow((wz-817)/19,2));west*=Mathf.SmoothStep(0,1,Mathf.Clamp01((828-wz)/7));
float east=Mathf.Clamp01(1-Mathf.Pow((wx-1127)/23,2)-Mathf.Pow((wz-820)/21,2));east*=Mathf.SmoothStep(0,1,Mathf.Clamp01((wx-1113)/8));
float w=Mathf.SmoothStep(0,1,Mathf.Clamp01(Mathf.Max(west,east)*1.6f));foreach(var p in content)w*=Mathf.SmoothStep(0,1,Mathf.Clamp01((Vector2.Distance(new Vector2(wx,wz),new Vector2(p.x,p.z))-6)/7));
RaycastHit hit;if(w<=0||!mc.Raycast(new Ray(new Vector3(wx,150,wz),Vector3.down),out hit,120))continue;
h[z,x]=Mathf.Lerp(h[z,x],Mathf.Min(h[z,x],76.15f/400),w);
}d.SetHeights(0,0,h);}finally{UnityEngine.Object.DestroyImmediate(proxy);}
var a=d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);for(int z=0;z<a.GetLength(0);z++)for(int x=0;x<a.GetLength(1);x++){float wx=x*2400f/(a.GetLength(1)-1),wz=-700+z*2800f/(a.GetLength(0)-1);if(wx<1005||wx>1145||wz<800||wz>841)continue;float wet=Mathf.Clamp01((79.3f-d.GetInterpolatedHeight(wx/2400,(wz+700)/2800))/2.2f);for(int l=0;l<a.GetLength(2);l++)a[z,x,l]*=1-wet;a[z,x,10]+=wet;}d.SetAlphamaps(0,0,a);EditorUtility.SetDirty(d);AssetDatabase.SaveAssetIfDirty(d);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s);return "양끝 호안 낮춤";
