// 이 검토 버전의 호안 형상과 재료만 저작한다. 원본·콘텐츠 Transform은 변경하지 않는다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
string dir="Assets/10.Datas/World/ThemeStudies/LakeReworkR2";
if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/LakeReworkR2.unity")throw new Exception("검토 씬 필요");
var terrain=Terrain.activeTerrain;var data=terrain.terrainData;
if(AssetDatabase.GetAssetPath(data)!=dir+"/Terrain.asset")throw new Exception("독립 데이터 필요");
var original=AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/10.Datas/World/OpenWorld/Terrain.asset");
var roots=scene.GetRootGameObjects();
var obstacles=roots.SelectMany(g=>g.GetComponentsInChildren<Collider>()).Where(c=>!(c is TerrainCollider)&&c.enabled&&!c.isTrigger&&c.bounds.size.x<65&&c.bounds.size.z<65).Select(c=>c.bounds).Where(b=>b.max.x>1010&&b.min.x<1140&&b.max.z>735&&b.min.z<800).ToArray();
var heights=original.GetHeights(0,0,original.heightmapResolution,original.heightmapResolution);
var changes=new List<string>();
for(int z=0;z<heights.GetLength(0);z++)for(int x=0;x<heights.GetLength(1);x++){
float wx=x*data.size.x/(heights.GetLength(1)-1),wz=-700+z*data.size.z/(heights.GetLength(0)-1);
if(wz<739||wz>795||wx<1015||wx>1130)continue;
if(Mathf.Abs(wx-1065)<11)continue;
if(obstacles.Any(b=>wx>b.min.x-2&&wx<b.max.x+2&&wz>b.min.z-2&&wz<b.max.z+2))continue;
float a=Mathf.Pow((wx-1097)/24,2)+Mathf.Pow((wz-777)/18,2);
float b2=Mathf.Pow((wx-1041)/17,2)+Mathf.Pow((wz-772)/24,2);
float weight=Mathf.Max(Mathf.Clamp01(1-a),Mathf.Clamp01(1-b2));weight=weight*weight*(3-2*weight);
float h=heights[z,x]*400;float target=Mathf.Min(h,75.8f+0.6f*Mathf.PerlinNoise(wx*.06f,wz*.05f));
if(weight>0&&h>target){heights[z,x]=Mathf.Lerp(h,target,weight)/400;changes.Add(wx+","+wz+","+h+","+heights[z,x]*400);}
}
data.SetHeights(0,0,heights);
var layers=original.terrainLayers.ToList();
var layerNames=new[]{"Meadow","Earth","Stone","Shallows"};var indices=new[]{4,5,2,6};var scales=new[]{2.2f,3f,5f,2.5f};
var colors=new[]{new Vector4(.64f,.76f,.68f,1),new Vector4(.8f,.84f,.8f,1),new Vector4(.72f,.79f,.8f,1),new Vector4(.62f,.69f,.65f,1)};
for(int i=0;i<4;i++){
string path=dir+"/"+layerNames[i]+".terrainlayer";var layer=AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
if(layer==null){layer=UnityEngine.Object.Instantiate(layers[indices[i]]);AssetDatabase.CreateAsset(layer,path);}
layer.tileSize=Vector2.one*scales[i];layer.diffuseRemapMin=Vector4.zero;layer.diffuseRemapMax=colors[i];layer.normalScale=.35f;layer.smoothness=.05f;EditorUtility.SetDirty(layer);AssetDatabase.SaveAssetIfDirty(layer);layers.Add(layer);
}
var old=original.GetAlphamaps(0,0,original.alphamapWidth,original.alphamapHeight);var alpha=new float[old.GetLength(0),old.GetLength(1),layers.Count];
for(int z=0;z<old.GetLength(0);z++)for(int x=0;x<old.GetLength(1);x++){
float wx=x*data.size.x/(old.GetLength(1)-1),wz=-700+z*data.size.z/(old.GetLength(0)-1);
float mask=Mathf.Clamp01(Mathf.Min(Mathf.Min((wx-820)/65,(1330-wx)/65),Mathf.Min((wz-685)/30,(1390-wz)/70)));
mask=mask*mask*(3-2*mask);
for(int l=0;l<7;l++)alpha[z,x,l]=old[z,x,l]*(1-mask);
if(mask<=0)continue;
float h=data.GetInterpolatedHeight(wx/2400,(wz+700)/2800);float slope=data.GetSteepness(wx/2400,(wz+700)/2800);
float noise=Mathf.PerlinNoise(wx*.018f,wz*.018f);
float cliff=Mathf.Clamp01((slope-17)/23)*Mathf.Clamp01((wz-980)/90);
cliff=Mathf.Max(cliff,Mathf.Clamp01((noise-.48f)*4)*Mathf.Clamp01((wz-1090)/110));
float shore=Mathf.Clamp01((80.3f-h)/3.5f);float soil=Mathf.Clamp01(old[z,x,1]+old[z,x,5]);
float meadow=(1-shore)*(1-cliff)*(1-soil*.82f);
float stone=cliff*(1-shore);
float shallows=shore;
float earth=Mathf.Max(0,1-meadow-stone-shallows);
alpha[z,x,7]=meadow*mask;alpha[z,x,8]=earth*mask;alpha[z,x,9]=stone*mask;alpha[z,x,10]=shallows*mask;
}
data.terrainLayers=layers.ToArray();data.SetAlphamaps(0,0,alpha);EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);
var water=GameObject.Find("LakeWater").GetComponent<Renderer>();string wp=dir+"/LakeWater.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(wp);
if(material==null){material=new Material(water.sharedMaterial);AssetDatabase.CreateAsset(material,wp);}water.sharedMaterial=material;
material.SetFloat("_Absorption",.65f);material.SetFloat("_SurfaceOpacity",.42f);material.SetFloat("_WaveStrength",.12f);material.SetFloat("_WaveScale",.08f);material.SetFloat("_ReflectionStrength",.55f);material.SetFloat("_RefractionStrength",.008f);material.SetFloat("_FoamStrength",.08f);
EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
System.IO.File.WriteAllLines("output/LakeReworkR2/height-changes.csv",changes);
return new{heightChanges=changes.Count,obstacles=obstacles.Length,scene.isDirty};
