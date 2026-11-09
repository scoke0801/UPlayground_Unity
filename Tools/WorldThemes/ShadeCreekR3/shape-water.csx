// R2 기준에서 계산하여 반복 실행 시 높이와 표면이 누적되지 않게 한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
string dir="Assets/10.Datas/World/ThemeStudies/ShadeCreekR3/";
if(scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/ShadeCreekR3.unity"||scene.isDirty||EditorApplication.isPlayingOrWillChangePlaymode||EditorUtility.scriptCompilationFailed)throw new Exception("저장된 R3 필요");
var terrain=Terrain.activeTerrain;var d=terrain.terrainData;
if(AssetDatabase.GetAssetPath(d)!=dir+"Terrain.asset")throw new Exception("전용 지형 필요");
var source=AssetDatabase.LoadAssetAtPath<TerrainData>(dir.Replace("R3/","R2/")+"Terrain.asset");
var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var protectedPoints=all.SelectMany(t=>t.GetComponents<MonoBehaviour>()).Where(c=>c!=null&&new[]{"SceneEntityId","SceneArrivalPoint","PortalActor","FlowGraphTriggerVolume"}.Contains(c.GetType().Name)).Select(c=>c.transform.position).ToArray();
var supports=all.SelectMany(t=>t.GetComponents<Collider>()).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger&&!(c is TerrainCollider)).Select(c=>c.bounds).Where(b=>b.max.x>1570&&b.min.x<1670&&b.max.z>755&&b.min.z<850).ToArray();
var route=new[]{new Vector2(1700,740),new Vector2(1640,750),new Vector2(1605,805),new Vector2(1608,814),new Vector2(1650,855),new Vector2(1750,870)};
Func<Vector2,float> routeDistance=p=>{float distance=9999;for(int i=0;i<route.Length-1;i++){var v=route[i+1]-route[i];distance=Mathf.Min(distance,Vector2.Distance(p,route[i]+v*Mathf.Clamp01(Vector2.Dot(p-route[i],v)/v.sqrMagnitude)));}return distance;};
Func<Vector2,float> protection=p=>{float w=Mathf.SmoothStep(0,1,Mathf.InverseLerp(6,10,routeDistance(p)));foreach(var q in protectedPoints)w*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(7,12,Vector2.Distance(p,new Vector2(q.x,q.z))));foreach(var b in supports){float dx=Mathf.Max(b.min.x-p.x,0,p.x-b.max.x),dz=Mathf.Max(b.min.z-p.y,0,p.y-b.max.z);w*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(2,5,Mathf.Sqrt(dx*dx+dz*dz)));}return w;};
Func<float,float,float,float,float,float> mass=(x,z,cx,cz,width)=>Mathf.Exp(-Mathf.Pow((x-cx)/width,2)-Mathf.Pow((z-cz)/(width*.6f),2));

var original=AssetDatabase.LoadAssetAtPath<Mesh>(dir.Replace("R3/","R2/")+"CreekSurface.asset");var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(dir+"CreekSurface.asset");var vertices=original.vertices;
if(vertices.Length!=145*13)throw new Exception("수면 격자 변경");
var centers=new Vector3[145];var widths=new float[145];
for(int row=0;row<145;row++){
 float z=vertices[row*13+6].z;
 float x=1630+7*Mathf.Sin((z-760)*.092f+.6f);
 float end=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,8,Mathf.Min(z-761,833-z)));
 float width=(3.1f+1.15f*Mathf.Sin((z-761)*.15f+.8f))*end+.12f;
 float y=source.GetInterpolatedHeight(x/2400,(z+700)/2800)+.06f;
 centers[row]=new Vector3(x,y,z);widths[row]=width;
 for(int c=0;c<13;c++)vertices[row*13+c]=new Vector3(x+(c/12f-.5f)*width,y,z);
}
var heights=d.GetHeights(0,0,d.heightmapResolution,d.heightmapResolution);int changed=0;
for(int z=0;z<d.heightmapResolution;z++){
 float wz=-700+z*d.size.z/(d.heightmapResolution-1);if(wz<761||wz>833)continue;
 int row=Mathf.Clamp(Mathf.RoundToInt((wz-761)*2),0,144);var center=centers[row];float half=widths[row]*.5f;
 for(int x=0;x<d.heightmapResolution;x++){
 float wx=x*d.size.x/(d.heightmapResolution-1);float distance=Mathf.Abs(wx-center.x);if(distance>half+3)continue;
 float weight=protection(new Vector2(wx,wz));if(weight<=0)continue;
 float cross=distance/Mathf.Max(.1f,half);float targetHeight=center.y-.42f+.6f*Mathf.SmoothStep(0,1,Mathf.Clamp01(cross));
 float edge=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(half,half+3,distance));
 float old=heights[z,x]*d.size.y;heights[z,x]=Mathf.Lerp(old,targetHeight,weight*edge)/d.size.y;changed++;
 }
}
d.SetHeights(0,0,heights);mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);EditorUtility.SetDirty(d);AssetDatabase.SaveAssetIfDirty(d);terrain.Flush();
var water=AssetDatabase.LoadAssetAtPath<Material>(dir+"CreekWater.mat");water.SetFloat("_AmbientFloor",.38f);water.SetFloat("_SurfaceOpacity",.4f);water.SetFloat("_DepthDistance",.45f);water.SetFloat("_ShoreFade",.35f);water.SetFloat("_ReflectionStrength",.25f);EditorUtility.SetDirty(water);AssetDatabase.SaveAssetIfDirty(water);
var rows=new List<object>();for(int i=0;i<145;i++){var p=centers[i];rows.Add(new{row=i,x=p.x,z=p.z,y=p.y,width=widths[i],depth=p.y-d.GetInterpolatedHeight(p.x/2400,(p.z+700)/2800)});}
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);System.IO.File.WriteAllText("output/ShadeCreekR3/water-section-candidate.json",(string)json.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{new{changed,rows,scope="보호 마스크 밖 하상·단면과 수면 메시 후보. 화면·물가 복귀 미검증"}}));return new{changed,rows=rows.Count};
