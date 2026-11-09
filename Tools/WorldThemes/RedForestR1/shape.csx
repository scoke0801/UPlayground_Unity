// 전용 사본에서 빈터를 둘러싼 사면과 부식층을 함께 저작한다. 원래 보행선은 고정한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
string dir="Assets/10.Datas/World/ThemeStudies/RedForestR1";
if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/RedForestR1.unity")throw new Exception("검토 씬 필요");
var t=Terrain.activeTerrain;var d=t.terrainData;
if(AssetDatabase.GetAssetPath(d)!=dir+"/Terrain.asset")throw new Exception("공유 지형 편집 금지");
var src=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/10.Datas/World/OpenWorld/LakeVisualV2/Terrain.asset");
var route=new[]{new Vector2(1150,475),new Vector2(1150,525),new Vector2(1152.8f,558.4f),new Vector2(1152.8f,560.4f),new Vector2(1135,580),new Vector2(1125,600)};
Func<float,float,float> routeDistance=(x,z)=>{var p=new Vector2(x,z);float best=999;for(int i=0;i<route.Length-1;i++){var a=route[i];var v=route[i+1]-a;best=Mathf.Min(best,Vector2.Distance(p,a+v*Mathf.Clamp01(Vector2.Dot(p-a,v)/v.sqrMagnitude)));}return best;};
Func<float,float,float> mask=(x,z)=>{float r=Mathf.Sqrt(Mathf.Pow((x-1150)/76,2)+Mathf.Pow((z-543)/84,2));return 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.68f,1,r));};
var colliders=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider>(true)).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger&&!(c is TerrainCollider)&&c.bounds.size.x<70&&c.bounds.size.z<70).Select(c=>c.bounds).Where(b=>b.max.x>1074&&b.min.x<1226&&b.max.z>459&&b.min.z<627).ToArray();
var h=src.GetHeights(0,0,src.heightmapResolution,src.heightmapResolution);var original=(float[,])h.Clone();int heights=0;
for(int z=0;z<h.GetLength(0);z++)for(int x=0;x<h.GetLength(1);x++){
float wx=x*d.size.x/(h.GetLength(1)-1),wz=-700+z*d.size.z/(h.GetLength(0)-1);float m=mask(wx,wz);if(m<=0||wz<550)continue;
float weight=m*Mathf.SmoothStep(0,1,Mathf.InverseLerp(5,13,routeDistance(wx,wz)))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(32,42,Vector2.Distance(new Vector2(wx,wz),new Vector2(1150,525))));
foreach(var b in colliders){float dx=Mathf.Max(b.min.x-wx,0,wx-b.max.x),dz=Mathf.Max(b.min.z-wz,0,wz-b.max.z);weight*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1,5,Mathf.Sqrt(dx*dx+dz*dz)));}
if(weight<.001f)continue;float sum=0;int count=0;for(int dz=-8;dz<=8;dz+=2)for(int dx=-8;dx<=8;dx+=2){sum+=original[z+dz,x+dx];count++;}float next=Mathf.Lerp(original[z,x],sum/count,weight*.85f);if(Mathf.Abs(next-h[z,x])*400>.001f){h[z,x]=next;heights++;}
}
d.SetHeights(0,0,h);
var layers=src.terrainLayers.ToList();
var sources=new[]{"Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Scenes/Terrain/TerrainLayers/TerrainLayer_ForestGround.terrainlayer",AssetDatabase.GetAssetPath(src.terrainLayers[1]),AssetDatabase.GetAssetPath(src.terrainLayers[5]),AssetDatabase.GetAssetPath(src.terrainLayers[2])};
var names=new[]{"ForestHumus","RootEarth","ClearingEarth","WeatheredRock"};
var colors=new[]{new Vector4(.48f,.40f,.34f,1),new Vector4(.60f,.52f,.45f,1),new Vector4(.57f,.51f,.43f,1),new Vector4(.52f,.50f,.46f,1)};
for(int i=0;i<names.Length;i++){string path=dir+"/"+names[i]+".terrainlayer";var l=AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);if(l==null){l=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<TerrainLayer>(sources[i]));AssetDatabase.CreateAsset(l,path);}l.tileSize=Vector2.one*(i==0?2.1f:i==3?4.5f:3.2f);l.diffuseRemapMin=Vector4.zero;l.diffuseRemapMax=colors[i];l.normalScale=.28f;l.smoothness=0;EditorUtility.SetDirty(l);AssetDatabase.SaveAssetIfDirty(l);layers.Add(l);}
var old=src.GetAlphamaps(0,0,src.alphamapWidth,src.alphamapHeight);var alpha=new float[src.alphamapHeight,src.alphamapWidth,layers.Count];int painted=0;
for(int z=0;z<alpha.GetLength(0);z++)for(int x=0;x<alpha.GetLength(1);x++){
float wx=x*d.size.x/(alpha.GetLength(1)-1),wz=-700+z*d.size.z/(alpha.GetLength(0)-1),m=mask(wx,wz);
for(int l=0;l<old.GetLength(2);l++)alpha[z,x,l]=old[z,x,l]*(1-m);if(m<=0)continue;
float noise=Mathf.PerlinNoise(wx*.085f,wz*.085f);float clearing=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.45f,1,Mathf.Sqrt(Mathf.Pow((wx-1150)/25,2)+Mathf.Pow((wz-526)/32,2))));
float path=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(2,7,routeDistance(wx,wz))))*.48f;float earth=Mathf.Max(clearing*.75f,path);
float rock=Mathf.InverseLerp(22,48,d.GetSteepness(wx/2400,(wz+700)/2800))*.42f*(.4f+.6f*noise);float humus=(1-earth-rock)*(.65f+.22f*noise);
int n=layers.Count;alpha[z,x,n-4]=m*humus;alpha[z,x,n-3]=m*(1-earth-rock-humus);alpha[z,x,n-2]=m*earth;alpha[z,x,n-1]=m*rock;painted++;
}
d.terrainLayers=layers.ToArray();d.SetAlphamaps(0,0,alpha);
for(int layer=0;layer<d.detailPrototypes.Length;layer++){var a=src.GetDetailLayer(0,0,src.detailWidth,src.detailHeight,layer);for(int z=0;z<a.GetLength(0);z++)for(int x=0;x<a.GetLength(1);x++){float wx=x*d.size.x/(a.GetLength(1)-1),wz=-700+z*d.size.z/(a.GetLength(0)-1);float m=mask(wx,wz);if(m>0)a[z,x]=Mathf.RoundToInt(a[z,x]*(1-m));}d.SetDetailLayer(0,0,layer,a);}
EditorUtility.SetDirty(d);AssetDatabase.SaveAssetIfDirty(d);System.IO.File.WriteAllText("output/RedForestR1/terrain-edit.txt","height samples="+heights+" surface samples="+painted+"; path 5m and Griffin 32m ground fixed; existing collider support fixed; original detail scatter/prototypes unchanged");
return new{heights,painted};
