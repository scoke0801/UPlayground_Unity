var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string dir="Assets/10.Datas/World/ThemeStudies/ShadeCreekR3/";
if(scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/ShadeCreekR3.unity"||scene.isDirty||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("저장된 R3 필요");
var d=Terrain.activeTerrain.terrainData;var alpha=d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);
var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(dir+"CreekSurface.asset");var v=mesh.vertices;
var path=new[]{new Vector2(1640,750),new Vector2(1605,805),new Vector2(1608,814),new Vector2(1650,855)};
int count=0;
for(int z=0;z<d.alphamapHeight;z++)for(int x=0;x<d.alphamapWidth;x++){
float wx=x*d.size.x/(d.alphamapWidth-1),wz=-700+z*d.size.z/(d.alphamapHeight-1);var p=new Vector2(wx,wz);
float scope=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.72f,1,Mathf.Sqrt(Mathf.Pow((wx-1620)/53,2)+Mathf.Pow((wz-803)/43,2))));if(scope<=0)continue;
float distance=999;for(int i=0;i<path.Length-1;i++){var delta=path[i+1]-path[i];distance=Mathf.Min(distance,Vector2.Distance(p,path[i]+delta*Mathf.Clamp01(Vector2.Dot(p-path[i],delta)/delta.sqrMagnitude)));}
float clearing=Mathf.Exp(-Mathf.Pow((wx-1611)/10,2)-Mathf.Pow((wz-810)/10,2));
float tread=Mathf.Max(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.3f,5,distance)),clearing*.8f);
float humus=(1-tread)*scope*.85f;
for(int l=0;l<d.alphamapLayers;l++)alpha[z,x,l]*=1-humus;alpha[z,x,15]+=humus*.7f;alpha[z,x,19]+=humus*.3f;
if(wz>762&&wz<832){int row=Mathf.Clamp(Mathf.RoundToInt((wz-761)*2),0,144);float center=v[row*13+6].x,half=(v[row*13+12].x-v[row*13].x)*.5f;float wet=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(half*.6f,half+2.4f,Mathf.Abs(wx-center))))*scope;for(int l=0;l<d.alphamapLayers;l++)alpha[z,x,l]*=1-wet;alpha[z,x,18]+=wet;}
count++;
}
d.SetAlphamaps(0,0,alpha);d.SetBaseMapDirty();Terrain.activeTerrain.Flush();EditorUtility.SetDirty(d);AssetDatabase.SaveAssetIfDirty(d);
var humusLayer=AssetDatabase.LoadAssetAtPath<TerrainLayer>(dir+"CanopyHumus.terrainlayer");humusLayer.diffuseRemapMax=new Vector4(.32f,.35f,.29f,1);humusLayer.tileSize=Vector2.one*2.2f;EditorUtility.SetDirty(humusLayer);AssetDatabase.SaveAssetIfDirty(humusLayer);
var wetLayer=AssetDatabase.LoadAssetAtPath<TerrainLayer>(dir+"WetRiverBed.terrainlayer");wetLayer.diffuseRemapMax=new Vector4(.32f,.36f,.34f,1);wetLayer.smoothness=.3f;EditorUtility.SetDirty(wetLayer);AssetDatabase.SaveAssetIfDirty(wetLayer);
var water=AssetDatabase.LoadAssetAtPath<Material>(dir+"CreekWater.mat");water.SetFloat("_ShoreFade",.10f);water.SetFloat("_AmbientFloor",.52f);water.SetFloat("_SurfaceOpacity",.56f);water.SetFloat("_WaveStrength",.2f);EditorUtility.SetDirty(water);AssetDatabase.SaveAssetIfDirty(water);
return new{count,scope="지형 높이 불변. 길·활동 공간과 부식층·하상 표면 경계 보정"};
