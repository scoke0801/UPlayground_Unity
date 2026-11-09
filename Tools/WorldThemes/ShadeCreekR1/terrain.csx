// 길과 콘텐츠 지지면은 고정하고 수관 아래 둑·부식층·밟힌 길을 전용 지형에 저작한다.
var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string dir="Assets/10.Datas/World/ThemeStudies/ShadeCreekR1/";
if(s.path!="Assets/01.Scenes/ArtReview/WorldThemes/ShadeCreekR1.unity"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("검토 씬 필요");
var t=Terrain.activeTerrain;var d=t.terrainData;var src=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/10.Datas/World/OpenWorld/RedForestVisualR1/Terrain.asset");
if(AssetDatabase.GetAssetPath(d)!=dir+"Terrain.asset")throw new Exception("전용 Terrain 필요");
var route=new[]{new Vector2(1700,740),new Vector2(1640,750),new Vector2(1605,805),new Vector2(1650,855),new Vector2(1750,870)};
Func<float,float,float> distance=(x,z)=>{var p=new Vector2(x,z);float best=999;for(int i=0;i<route.Length-1;i++){var v=route[i+1]-route[i];best=Mathf.Min(best,Vector2.Distance(p,route[i]+v*Mathf.Clamp01(Vector2.Dot(p-route[i],v)/v.sqrMagnitude)));}return best;};
Func<float,float,float> mask=(x,z)=>1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.76f,1,Mathf.Sqrt(Mathf.Pow((x-1655)/145,2)+Mathf.Pow((z-810)/115,2))));
var protectedPoints=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(e=>e.transform.position).Where(p=>p.x>1500&&p.x<1810&&p.z>680&&p.z<980).ToArray();
var colliders=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider>(true)).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger&&!(c is TerrainCollider)&&c.bounds.size.x<70&&c.bounds.size.z<70).Select(c=>c.bounds).Where(b=>b.max.x>1510&&b.min.x<1800&&b.max.z>695&&b.min.z<925).ToArray();
var h=src.GetHeights(0,0,src.heightmapResolution,src.heightmapResolution);int changed=0;
for(int z=1;z<h.GetLength(0)-1;z++)for(int x=1;x<h.GetLength(1)-1;x++){
float wx=x*d.size.x/(h.GetLength(1)-1),wz=-700+z*d.size.z/(h.GetLength(0)-1),m=mask(wx,wz);if(m<=0)continue;
float weight=m*Mathf.SmoothStep(0,1,Mathf.InverseLerp(9,18,distance(wx,wz)));
foreach(var p in protectedPoints)weight*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(30,38,Vector2.Distance(new Vector2(wx,wz),new Vector2(p.x,p.z))));
foreach(var b in colliders){float dx=Mathf.Max(b.min.x-wx,0,wx-b.max.x),dz=Mathf.Max(b.min.z-wz,0,wz-b.max.z);weight*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(2,6,Mathf.Sqrt(dx*dx+dz*dz)));}
float bank=1.6f*Mathf.Exp(-Mathf.Pow((wx-1578)/17,2)-Mathf.Pow((wz-798)/29,2))+1.2f*Mathf.Exp(-Mathf.Pow((wx-1665)/24,2)-Mathf.Pow((wz-793)/25,2));
float delta=weight*bank;if(delta<.005f)continue;h[z,x]+=delta/d.size.y;changed++;
}d.SetHeights(0,0,h);
var layers=src.terrainLayers.ToList();string basePath="Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Scenes/Terrain/TerrainLayers/";
var sources=new[]{basePath+"TerrainLayer_ForestGround.terrainlayer",AssetDatabase.GetAssetPath(src.terrainLayers[1]),AssetDatabase.GetAssetPath(src.terrainLayers[5]),basePath+"TerrainLayer_RiverBed.terrainlayer"};
var names=new[]{"CanopyHumus","RootEarth","Footpath","WetRiverBed"};var colors=new[]{new Vector4(.47f,.48f,.41f,1),new Vector4(.48f,.47f,.40f,1),new Vector4(.63f,.61f,.53f,1),new Vector4(.56f,.66f,.63f,1)};
for(int i=0;i<4;i++){var l=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<TerrainLayer>(sources[i]));l.tileSize=Vector2.one*(i==0?4.2f:3.5f);l.diffuseRemapMin=Vector4.zero;l.diffuseRemapMax=colors[i];l.normalScale=.3f;l.smoothness=i==3?.32f:0;AssetDatabase.CreateAsset(l,dir+names[i]+".terrainlayer");layers.Add(l);}
var old=src.GetAlphamaps(0,0,src.alphamapWidth,src.alphamapHeight);var a=new float[src.alphamapHeight,src.alphamapWidth,layers.Count];int n=src.alphamapLayers;
for(int z=0;z<a.GetLength(0);z++)for(int x=0;x<a.GetLength(1);x++){
float wx=x*d.size.x/(a.GetLength(1)-1),wz=-700+z*d.size.z/(a.GetLength(0)-1),m=mask(wx,wz);for(int l=0;l<n;l++)a[z,x,l]=old[z,x,l]*(1-m);if(m<=0)continue;
float noise=Mathf.PerlinNoise(wx*.075f,wz*.075f),rd=distance(wx,wz);float path=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.5f+noise,7+noise*3,rd));
float bed=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.5f,6,Mathf.Abs(wx-(1619+Mathf.Sin((wz-775)*.07f)*6)))))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(750,775,wz))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(820,840,wz)))*.60f;
float humus=(1-path)*(1-bed)*(.65f+.2f*noise);float earth=(1-path)*(1-bed)-humus;
a[z,x,n]=m*humus;a[z,x,n+1]=m*earth;a[z,x,n+2]=m*path*(1-bed);a[z,x,n+3]=m*bed;
}d.terrainLayers=layers.ToArray();d.SetAlphamaps(0,0,a);
for(int l=0;l<src.detailPrototypes.Length;l++){var detail=src.GetDetailLayer(0,0,src.detailWidth,src.detailHeight,l);for(int z=0;z<detail.GetLength(0);z++)for(int x=0;x<detail.GetLength(1);x++){float wx=x*d.size.x/(detail.GetLength(1)-1),wz=-700+z*d.size.z/(detail.GetLength(0)-1),m=mask(wx,wz);if(m>0)detail[z,x]=Mathf.RoundToInt(detail[z,x]*(1-m));}d.SetDetailLayer(0,0,l,detail);}
d.SetBaseMapDirty();t.Flush();EditorUtility.SetDirty(d);AssetDatabase.SaveAssetIfDirty(d);
var tree=GameObject.Find("AuthoredEnvironment/월드 구간/그늘숲/성 주변 숲").transform.Cast<Transform>().Single(q=>Vector3.Distance(q.position,new Vector3(1602.2262f,79.83242f,804.5498f))<.05f);
tree.position=new Vector3(1591,t.SampleHeight(new Vector3(1591,0,806))-.25f,806);PrefabUtility.RecordPrefabInstancePropertyModifications(tree);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(s);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s);
System.IO.File.WriteAllText("output/ShadeCreekR1/terrain-edit.txt","기존 지형 기반 둑 / 수정 height="+changed+"; 진행 ID 주변30m·길9m·기존 Collider 접지 고정. 수면 없음. 밝은 젖은 하상은 독립 검토 표현.");
return new{heightSamples=changed,layers=layers.Count,treeRelocated="기존 목재 앞 겹침 줄기 1개를 왼쪽 둑으로 이동"};

