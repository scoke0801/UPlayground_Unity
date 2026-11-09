// 호수 검토 사본의 배경 능선과 재료 덩어리 보정. 모든 기존 개체의 Transform은 보존한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/LakeReworkR2.unity"||EditorApplication.isPlaying)throw new Exception("검토 씬 필요");
var terrain=Terrain.activeTerrain;var data=terrain.terrainData;string dir="Assets/10.Datas/World/ThemeStudies/LakeReworkR2";
var grass=AssetDatabase.LoadAssetAtPath<TerrainLayer>(dir+"/Meadow.terrainlayer");grass.diffuseTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ExternalAssets/Environment/Toon Golden Valley/Terrain/Terrain Textures/TGV_Terrain_Grass_01D.png");grass.tileSize=Vector2.one*3;grass.diffuseRemapMax=new Vector4(.9f,.95f,1,1);grass.normalMapTexture=null;EditorUtility.SetDirty(grass);AssetDatabase.SaveAssetIfDirty(grass);
var transforms=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var anchors=transforms.Where(t=>t.gameObject.activeInHierarchy&&PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)&&t.position.x>800&&t.position.x<1350&&t.position.z>1020&&t.position.z<1390).Select(t=>new Vector2(t.position.x,t.position.z)).ToArray();
var source=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/10.Datas/World/OpenWorld/Terrain.asset");var heights=data.GetHeights(0,0,data.heightmapResolution,data.heightmapResolution);var original=source.GetHeights(0,0,source.heightmapResolution,source.heightmapResolution);int count=0;
for(int z=0;z<heights.GetLength(0);z++)for(int x=0;x<heights.GetLength(1);x++){
float wx=x*2400f/(heights.GetLength(1)-1),wz=-700+z*2800f/(heights.GetLength(0)-1);if(wx<835||wx>1320||wz<1050||wz>1360)continue;
float protect=Mathf.Clamp01((Mathf.Abs(wx-(1067+(wz-1084)*.12f))-18)/22); // 북쪽 귀환 통로의 넓은 지지면 보존.
foreach(var a in anchors){float distance=Vector2.Distance(new Vector2(wx,wz),a);protect=Mathf.Min(protect,Mathf.Clamp01((distance-10)/15));if(protect==0)break;}
Func<float,float,float,float,float> mound=(cx,cz,rx,rz)=>{float f=Mathf.Clamp01(1-Mathf.Pow((wx-cx)/rx,2)-Mathf.Pow((wz-cz)/rz,2));return f*f*(3-2*f);};
float delta=30*mound(948,1192,110,142)+22*mound(1222,1160,94,100)-18*mound(1133,1215,60,125);
if(Mathf.Abs(delta*protect)<.001f)continue;heights[z,x]=original[z,x]+delta*protect/400;count++;
}
data.SetHeights(0,0,heights);
var alpha=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);
for(int z=0;z<alpha.GetLength(0);z++)for(int x=0;x<alpha.GetLength(1);x++){
float wx=x*2400f/(alpha.GetLength(1)-1),wz=-700+z*2800f/(alpha.GetLength(0)-1);if(wx<850||wx>1310||wz<1040||wz>1360)continue;
float edge=Mathf.Clamp01(Mathf.Min(Mathf.Min((wx-850)/35,(1310-wx)/35),Mathf.Min((wz-1040)/50,(1360-wz)/50)));
float slope=data.GetSteepness(wx/2400,(wz+700)/2800);float fold=Mathf.PerlinNoise(wx*.014f+wz*.003f,wz*.008f);
float rock=Mathf.Clamp01((slope-12)/25+(.48f-fold)*2.4f)*edge;float soil=(1-rock)*.12f;
for(int l=0;l<alpha.GetLength(2);l++)alpha[z,x,l]*=1-edge;
alpha[z,x,7]+=(1-rock-soil)*edge;alpha[z,x,8]+=soil*edge;alpha[z,x,9]+=rock*edge;
}
data.SetAlphamaps(0,0,alpha);EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return new{backgroundHeightCells=count,protectedAnchors=anchors.Length};
