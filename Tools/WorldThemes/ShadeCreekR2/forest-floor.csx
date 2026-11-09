// 넓은 활동 지면은 유지하되, 길 바깥을 낮은 숲바닥의 큰 덩어리로 나눈다.
var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string dir="Assets/10.Datas/World/ThemeStudies/ShadeCreekR2/";
if(s.path!="Assets/01.Scenes/ArtReview/WorldThemes/ShadeCreekR2.unity"||s.isDirty||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("저장된 R2 필요");
var d=Terrain.activeTerrain.terrainData;if(AssetDatabase.GetAssetPath(d)!=dir+"Terrain.asset")throw new Exception("R2 Terrain 필요");
string path=dir+"ForestFloor.terrainlayer";if(AssetDatabase.LoadAssetAtPath<TerrainLayer>(path)!=null)throw new Exception("중복 실행 금지");
var layer=UnityEngine.Object.Instantiate(d.terrainLayers[0]);layer.name="ForestFloor";layer.diffuseRemapMax=new Vector4(.40f,.47f,.36f,1);layer.tileSize=Vector2.one*3;AssetDatabase.CreateAsset(layer,path);
var old=d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);var layers=d.terrainLayers.ToList();int index=layers.Count;layers.Add(layer);d.terrainLayers=layers.ToArray();var a=new float[d.alphamapHeight,d.alphamapWidth,index+1];
var route=new[]{new Vector2(1700,740),new Vector2(1640,750),new Vector2(1605,805),new Vector2(1608,814),new Vector2(1650,855),new Vector2(1750,870)};
for(int z=0;z<d.alphamapHeight;z++)for(int x=0;x<d.alphamapWidth;x++){
float wx=x*d.size.x/(d.alphamapWidth-1),wz=-700+z*d.size.z/(d.alphamapHeight-1);var p=new Vector2(wx,wz);float dist=999;for(int i=0;i<route.Length-1;i++){var v=route[i+1]-route[i];dist=Mathf.Min(dist,Vector2.Distance(p,route[i]+v*Mathf.Clamp01(Vector2.Dot(p-route[i],v)/v.sqrMagnitude)));}
float scope=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.7f,1,Mathf.Sqrt(Mathf.Pow((wx-1640)/110,2)+Mathf.Pow((wz-810)/95,2))));
float patch=Mathf.PerlinNoise(wx*.035f+4,wz*.035f+12);float w=scope*Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.2f,8+patch*8,dist))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.22f,.65f,patch))*.92f;
// 기존 하상은 남겨 젖은 단면과 숲바닥이 구분되게 한다.
w*=1-old[z,x,18];for(int l=0;l<index;l++)a[z,x,l]=old[z,x,l]*(1-w);a[z,x,index]=w;
}
d.SetAlphamaps(0,0,a);d.SetBaseMapDirty();Terrain.activeTerrain.Flush();EditorUtility.SetDirty(d);AssetDatabase.SaveAssetIfDirty(d);
var water=AssetDatabase.LoadAssetAtPath<Material>(dir+"CreekWater.mat");water.SetFloat("_AmbientFloor",.35f);water.SetFloat("_SurfaceOpacity",.48f);water.SetFloat("_ShoreFade",.6f);water.SetColor("_ShallowColor",new Color(.26f,.40f,.34f,1));EditorUtility.SetDirty(water);AssetDatabase.SaveAssetIfDirty(water);
return "낮은 숲바닥 큰 면·밟힌 접근선 배합, 비서사 수면의 밝은 단색 면 완화. 높이·식생 개수 불변";
