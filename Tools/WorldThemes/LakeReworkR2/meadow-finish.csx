// 접근로 폭을 구분하고 낮은 풀로 초지 면을 연결한다. 공유 프로토타입은 수정하지 않는다.
var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(EditorApplication.isPlayingOrWillChangePlaymode||s.path!="Assets/01.Scenes/ArtReview/WorldThemes/LakeReworkR2.unity")throw new Exception("검토 씬 필요");
var t=Terrain.activeTerrain;var d=t.terrainData;if(!AssetDatabase.GetAssetPath(d).Contains("ThemeStudies/LakeReworkR2/"))throw new Exception("독립 데이터 필요");
var a=d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);
for(int z=0;z<a.GetLength(0);z++)for(int x=0;x<a.GetLength(1);x++){
float wx=x*2400f/(a.GetLength(1)-1),wz=-700+z*2800f/(a.GetLength(0)-1);
if(wx<950||wx>1175||wz<690||wz>820)continue;
float mask=Mathf.SmoothStep(0,1,Mathf.Clamp01(Mathf.Min(Mathf.Min((wx-950)/20,(1175-wx)/20),Mathf.Min((wz-690)/15,(820-wz)/12))));
float h=d.GetInterpolatedHeight(wx/2400,(wz+700)/2800);float n=Mathf.PerlinNoise(wx*.055f,wz*.055f);
float pathX=wz<787?1067-(wz-715)*.042f:1064+(wz-787)*.85f;
pathX+=Mathf.Sin((wz-715)/20)*1.5f; float main=Mathf.Abs(wx-pathX);float cross=wx<1061?Mathf.Abs(wz-(710+(1067-wx)*.15f))+1.2f:100;
float soil=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.25f+n*.3f,2.5f+n*.5f,Mathf.Min(main,cross)));
float wet=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(77.35f+n*.45f,79.2f+n*.7f,h));
for(int l=0;l<a.GetLength(2);l++)a[z,x,l]*=1-mask;
a[z,x,7]+=(1-soil)*(1-wet)*mask;a[z,x,8]+=soil*(1-wet)*mask;a[z,x,10]+=wet*mask;
}
d.SetAlphamaps(0,0,a);
int res=d.detailResolution;var grass=d.GetDetailLayer(0,0,res,res,6);var flowers=d.GetDetailLayer(0,0,res,res,3);
for(int z=0;z<res;z++)for(int x=0;x<res;x++){
float wx=(x+.5f)*2400/res,wz=-700+(z+.5f)*2800/res;if(wx<955||wx>1170||wz<695||wz>850)continue;
int ax=Mathf.Clamp(Mathf.RoundToInt(wx/2400*(a.GetLength(1)-1)),0,a.GetLength(1)-1),az=Mathf.Clamp(Mathf.RoundToInt((wz+700)/2800*(a.GetLength(0)-1)),0,a.GetLength(0)-1);
float meadow=a[az,ax,7],h=d.GetInterpolatedHeight(wx/2400,(wz+700)/2800);float noise=Mathf.PerlinNoise(wx*.11f,wz*.11f);
if(meadow>.7f&&h>78.3f&&d.GetSteepness(wx/2400,(wz+700)/2800)<28){grass[z,x]=Mathf.RoundToInt(Mathf.Lerp(7,16,noise));flowers[z,x]=noise>.68f?1:0;}else{grass[z,x]=0;flowers[z,x]=0;}
}
d.SetDetailLayer(0,0,6,grass);d.SetDetailLayer(0,0,3,flowers);
var layer=d.terrainLayers[7];layer.diffuseRemapMax=new Vector4(1,1,1,1);EditorUtility.SetDirty(layer);AssetDatabase.SaveAssetIfDirty(layer);
GameObject.Find("EastBankShelf").transform.position=new Vector3(1102,76.9f,810);
GameObject.Find("EastBankReturn").transform.position=new Vector3(1120,76.6f,809);
GameObject.Find("WestBankShelf").transform.position=new Vector3(1022,76.5f,814);
GameObject.Find("WestSlopeFoot").transform.rotation=Quaternion.Euler(8,12,-13);
GameObject.Find("WestSlopeShoulder").transform.rotation=Quaternion.Euler(-8,-30,14);
GameObject.Find("EastSlopeFoot").transform.rotation=Quaternion.Euler(7,37,10);
EditorUtility.SetDirty(d);AssetDatabase.SaveAssetIfDirty(d);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s);return "풀밭 면과 접근선, 지형에 연결되는 암반 수정";



