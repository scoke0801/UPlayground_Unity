// 진행 지반을 남기고 양옆 만입부를 수면으로 연다. 독립 검토 데이터만 수정한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/LakeReworkR2.unity")throw new Exception("검토 씬 필요");
var t=Terrain.activeTerrain;var d=t.terrainData;
if(AssetDatabase.GetAssetPath(d)!="Assets/10.Datas/World/ThemeStudies/LakeReworkR2/Terrain.asset")throw new Exception("독립 지형 필요");
var original=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/10.Datas/World/OpenWorld/Terrain.asset");
var content=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MonoBehaviour>(true)).Where(c=>c!=null&&(c.GetType().Name=="SceneEntityId"||c.GetType().Name=="FlowGraphTriggerVolume")).Select(c=>c.transform.position).Where(p=>p.x>1010&&p.x<1140&&p.z>730&&p.z<850).ToArray();
var h=d.GetHeights(0,0,d.heightmapResolution,d.heightmapResolution);
var water=GameObject.Find("LakeWater");var proxy=new GameObject("WaterCoverageProbe");proxy.transform.SetPositionAndRotation(water.transform.position,water.transform.rotation);proxy.transform.localScale=water.transform.lossyScale;
var mc=proxy.AddComponent<MeshCollider>();mc.sharedMesh=water.GetComponent<MeshFilter>().sharedMesh;
int changed=0;try {
for(int z=0;z<h.GetLength(0);z++)for(int x=0;x<h.GetLength(1);x++){
float wx=x*d.size.x/(h.GetLength(1)-1),wz=-700+z*d.size.z/(h.GetLength(0)-1);
if(wx<1010||wx>1140||wz<756||wz>820)continue;
float e=Mathf.Sqrt(Mathf.Pow((wx-1102)/36,2)+Mathf.Pow((wz-795)/27,2));
float w=Mathf.Sqrt(Mathf.Pow((wx-1035)/24,2)+Mathf.Pow((wz-786)/26,2));
float weight=Mathf.SmoothStep(0,1,Mathf.Clamp01((1-Mathf.Min(e,w))/.55f));
float px=wz<787?1064:1064+(wz-787)*.85f;
weight*=Mathf.SmoothStep(0,1,Mathf.Clamp01((Mathf.Abs(wx-px)-4.5f)/7));
foreach(var p in content){float dist=Vector2.Distance(new Vector2(wx,wz),new Vector2(p.x,p.z));weight*=Mathf.SmoothStep(0,1,Mathf.Clamp01((dist-5)/7));}
if(weight<=0)continue;
RaycastHit hit;if(!mc.Raycast(new Ray(new Vector3(wx,150,wz),Vector3.down),out hit,120))continue;
float old=original.GetInterpolatedHeight(wx/2400,(wz+700)/2800);
float target=75.7f+.45f*Mathf.PerlinNoise(wx*.09f,wz*.08f);
h[z,x]=Mathf.Min(h[z,x],Mathf.Lerp(old,target,weight)/400);changed++;
}
d.SetHeights(0,0,h);
}finally{UnityEngine.Object.DestroyImmediate(proxy);}
foreach(string name in new[]{"Heart_ShoreRock_9_R2","Heart_ShoreRock_10_R2","Heart_ShoreRock_11_R2"}){
var obj=GameObject.Find(name);if(obj==null)continue;var p=obj.transform.position;float old=original.GetInterpolatedHeight(p.x/2400,(p.z+700)/2800);p.y+=t.SampleHeight(p)-old;obj.transform.position=p;}
var a=d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);
for(int z=0;z<a.GetLength(0);z++)for(int x=0;x<a.GetLength(1);x++){
float wx=x*d.size.x/(a.GetLength(1)-1),wz=-700+z*d.size.z/(a.GetLength(0)-1);if(wx<1010||wx>1140||wz<756||wz>820)continue;
float y=d.GetInterpolatedHeight(wx/2400,(wz+700)/2800);float shore=Mathf.Clamp01((79.6f-y)/2.6f);if(shore<=0)continue;
for(int l=0;l<a.GetLength(2);l++)a[z,x,l]*=1-shore;a[z,x,10]+=shore;
}
d.SetAlphamaps(0,0,a);EditorUtility.SetDirty(d);AssetDatabase.SaveAssetIfDirty(d);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);return new{changed,protectedContent=content.Length};
