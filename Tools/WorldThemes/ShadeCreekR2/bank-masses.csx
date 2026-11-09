// 물가의 긴 띠를 비대칭 둑 돌출부와 얕은 단면으로 끊는다. R1 높이를 기준으로 중복 누적을 막는다.
var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string dir="Assets/10.Datas/World/ThemeStudies/ShadeCreekR2/";
if(s.path!="Assets/01.Scenes/ArtReview/WorldThemes/ShadeCreekR2.unity"||s.isDirty||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("저장된 R2 필요");
var t=Terrain.activeTerrain;var d=t.terrainData;var src=AssetDatabase.LoadAssetAtPath<TerrainData>(dir.Replace("R2/","R1/")+"Terrain.asset");if(AssetDatabase.GetAssetPath(d)!=dir+"Terrain.asset")throw new Exception("전용 지형 필요");
var all=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var fixedPoints=all.SelectMany(g=>g.GetComponents<UPlayGround.Components.SceneEntityId>()).Select(g=>g.transform.position).Where(p=>p.x>1550&&p.x<1710&&p.z>730&&p.z<900).ToArray();
var supports=all.SelectMany(g=>g.GetComponents<Collider>()).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger&&!(c is TerrainCollider)&&c.bounds.size.x<70&&c.bounds.size.z<70).Select(c=>c.bounds).Where(b=>b.max.x>1590&&b.min.x<1660&&b.max.z>750&&b.min.z<845).ToArray();
var route=new[]{new Vector2(1700,740),new Vector2(1640,750),new Vector2(1605,805),new Vector2(1608,814),new Vector2(1650,855),new Vector2(1750,870)};
Func<float,float,float> protect=(x,z)=>{var p=new Vector2(x,z);float dist=999;for(int i=0;i<route.Length-1;i++){var v=route[i+1]-route[i];dist=Mathf.Min(dist,Vector2.Distance(p,route[i]+v*Mathf.Clamp01(Vector2.Dot(p-route[i],v)/v.sqrMagnitude)));}float w=Mathf.SmoothStep(0,1,Mathf.InverseLerp(5.5f,9,dist));foreach(var q in fixedPoints)w*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(6,11,Vector2.Distance(p,new Vector2(q.x,q.z))));foreach(var b in supports){float dx=Mathf.Max(b.min.x-x,0,x-b.max.x),dz=Mathf.Max(b.min.z-z,0,z-b.max.z);w*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.5f,4,Mathf.Sqrt(dx*dx+dz*dz)));}return w;};
var h=src.GetHeights(0,0,src.heightmapResolution,src.heightmapResolution);int changed=0;
for(int z=0;z<h.GetLength(0);z++){float wz=-700+z*d.size.z/(h.GetLength(0)-1);if(wz<755||wz>840)continue;for(int x=0;x<h.GetLength(1);x++){float wx=x*d.size.x/(h.GetLength(1)-1);if(wx<1605||wx>1655)continue;float mound=1.5f*Mathf.Exp(-Mathf.Pow((wx-1628)/8,2)-Mathf.Pow((wz-788)/5.5f,2))+1.1f*Mathf.Exp(-Mathf.Pow((wx-1626)/7,2)-Mathf.Pow((wz-811)/6,2))+.8f*Mathf.Exp(-Mathf.Pow((wx-1641)/8,2)-Mathf.Pow((wz-799)/6,2));mound+=.65f*Mathf.Exp(-Mathf.Pow((wx-1620)/6,2)-Mathf.Pow((wz-797)/4,2))+.45f*Mathf.Exp(-Mathf.Pow((wx-1636)/5,2)-Mathf.Pow((wz-772)/3.5f,2));float delta=mound*protect(wx,wz);if(delta<.003f)continue;h[z,x]+=delta/d.size.y;changed++;}}
d.SetHeights(0,0,h);
// 1차 보정의 거친 뿌리 무늬를 낮추고 부식층을 차가운 저채도 흙으로 맞춘다.
var humus=AssetDatabase.LoadAssetAtPath<TerrainLayer>(dir+"CanopyHumus.terrainlayer");humus.tileSize=Vector2.one*2.6f;humus.diffuseRemapMax=new Vector4(.40f,.46f,.39f,1);humus.normalScale=.14f;EditorUtility.SetDirty(humus);AssetDatabase.SaveAssetIfDirty(humus);
var wet=AssetDatabase.LoadAssetAtPath<TerrainLayer>(dir+"WetRiverBed.terrainlayer");wet.diffuseRemapMax=new Vector4(.38f,.46f,.42f,1);wet.smoothness=.36f;EditorUtility.SetDirty(wet);AssetDatabase.SaveAssetIfDirty(wet);
d.SetBaseMapDirty();t.Flush();EditorUtility.SetDirty(d);AssetDatabase.SaveAssetIfDirty(d);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(s);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s);return new{changed,protectedPoints=fixedPoints.Length,supports=supports.Length};


