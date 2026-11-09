// 검토 사본의 우물 앞 단면과 생활 마당만 저작한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
string dir="Assets/10.Datas/World/ThemeStudies/VillageConnectionR1";
if(EditorApplication.isPlayingOrWillChangePlaymode||scene.isDirty||scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/VillageConnectionR1.unity")throw new Exception("저장된 검토 씬 필요");
var t=Terrain.activeTerrain;var d=t.terrainData;
if(AssetDatabase.GetAssetPath(d)!=dir+"/Terrain.asset")throw new Exception("전용 지형 필요");
var src=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/10.Datas/World/OpenWorld/LakeVisualV2/Terrain.asset");
var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var protectedBounds=all.SelectMany(q=>q.GetComponents<Collider>()).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger&&!(c is TerrainCollider)&&!c.name.Contains("Well")&&c.bounds.size.x<80&&c.bounds.size.z<80).Select(c=>c.bounds).Where(b=>b.max.x>1020&&b.min.x<1095&&b.max.z>385&&b.min.z<445).ToArray();
var h=src.GetHeights(0,0,src.heightmapResolution,src.heightmapResolution);int changed=0;
for(int z=0;z<h.GetLength(0);z++)for(int x=0;x<h.GetLength(1);x++){
float wx=x*d.size.x/(h.GetLength(1)-1),wz=-700+z*d.size.z/(h.GetLength(0)-1);
float distance=Mathf.Sqrt(Mathf.Pow((wx-1044)/10,2)+Mathf.Pow((wz-408)/11,2));if(distance>=1)continue;
float weight=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((distance-.32f)/.68f));
foreach(var b in protectedBounds){float dx=Mathf.Max(b.min.x-wx,0,wx-b.max.x),dz=Mathf.Max(b.min.z-wz,0,wz-b.max.z);weight*=Mathf.SmoothStep(0,1,Mathf.Clamp01((Mathf.Sqrt(dx*dx+dz*dz)-.6f)/2));}
float old=h[z,x]*400,target=Mathf.Min(old,59.70f);float next=Mathf.Lerp(old,target,weight);if(Mathf.Abs(next-old)>.001f){h[z,x]=next/400;changed++;}
}
d.SetHeights(0,0,h);
var layers=src.terrainLayers.ToList();var names=new[]{"WarmTrampledEarth","DryYardEdge"};var colors=new[]{new Vector4(.80f,.68f,.49f,1),new Vector4(.58f,.58f,.34f,1)};
for(int i=0;i<2;i++){string path=dir+"/"+names[i]+".terrainlayer";var l=AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);if(l==null){l=UnityEngine.Object.Instantiate(src.terrainLayers[i==0?5:4]);AssetDatabase.CreateAsset(l,path);}l.tileSize=Vector2.one*(i==0?2.4f:3.2f);l.diffuseRemapMin=Vector4.zero;l.diffuseRemapMax=colors[i];l.normalScale=.25f;EditorUtility.SetDirty(l);AssetDatabase.SaveAssetIfDirty(l);layers.Add(l);}
var oldAlpha=src.GetAlphamaps(0,0,src.alphamapWidth,src.alphamapHeight);var a=new float[src.alphamapHeight,src.alphamapWidth,layers.Count];int painted=0;
for(int z=0;z<a.GetLength(0);z++)for(int x=0;x<a.GetLength(1);x++){
float wx=x*d.size.x/(a.GetLength(1)-1),wz=-700+z*d.size.z/(a.GetLength(0)-1);
float yard=Mathf.Sqrt(Mathf.Pow((wx-1051)/25,2)+Mathf.Pow((wz-410)/27,2));float mask=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((yard-.5f)/.5f));
for(int l=0;l<oldAlpha.GetLength(2);l++)a[z,x,l]=oldAlpha[z,x,l]*(1-mask);
if(mask<=0)continue;float earth=Mathf.Clamp01(.60f+.30f*Mathf.PerlinNoise(wx*.12f,wz*.12f));a[z,x,layers.Count-2]=mask*earth;a[z,x,layers.Count-1]=mask*(1-earth);painted++;
}
d.terrainLayers=layers.ToArray();d.SetAlphamaps(0,0,a);EditorUtility.SetDirty(d);AssetDatabase.SaveAssetIfDirty(d);
System.IO.File.WriteAllText("output/VillageConnectionR1/terrain-edit.txt","height samples="+changed+"; surface samples="+painted+"; original transforms unchanged; well excavation bottom 59.70; other collider footprints protected");
return new {changed,painted};
