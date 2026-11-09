var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string dir="Assets/10.Datas/World/ThemeStudies/ShadeCreekR3/";
if(scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/ShadeCreekR3.unity"||scene.isDirty||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("R3 Edit 필요");
var d=Terrain.activeTerrain.terrainData;var alpha=d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);
// 실제 카메라의 전경에 잡히는 뿌리 주변 두 면. 중앙 통행선은 열린 흙으로 잇는다.
Func<float,float,float> bed=(x,z)=>{
float left=Mathf.Exp(-Mathf.Pow((x-1596)/10,2)-Mathf.Pow((z-797)/12,2));
float right=Mathf.Exp(-Mathf.Pow((x-1613)/7,2)-Mathf.Pow((z-792)/8,2));
float pathX=1604+(z-797)*.16f;float corridor=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.8f,4.4f,Mathf.Abs(x-pathX)));
return Mathf.Clamp01(Mathf.Max(left,right)*1.5f)*(1-corridor*.95f);};
int changed=0;
for(int z=0;z<d.alphamapHeight;z++)for(int x=0;x<d.alphamapWidth;x++){
float wx=x*d.size.x/(d.alphamapWidth-1),wz=-700+z*d.size.z/(d.alphamapHeight-1);if(wx<1585||wx>1625||wz<780||wz>815)continue;
float scope=Mathf.SmoothStep(0,1,Mathf.Clamp01(Mathf.Min(wx-1585,1625-wx,wz-780,815-wz)/5));float w=bed(wx,wz)*scope;
if(w<.01f)continue;for(int l=0;l<d.alphamapLayers;l++)alpha[z,x,l]*=1-w;alpha[z,x,15]+=w*.82f;alpha[z,x,16]+=w*.18f;changed++;
}
var detail=d.GetDetailLayer(0,0,d.detailWidth,d.detailHeight,8);
for(int z=0;z<d.detailHeight;z++)for(int x=0;x<d.detailWidth;x++){
float wx=x*d.size.x/(d.detailWidth-1),wz=-700+z*d.size.z/(d.detailHeight-1);if(wx<1587||wx>1623||wz<782||wz>813)continue;
float w=bed(wx,wz);detail[z,x]=w>.62f?2:(w>.35f?1:0);
}
var prototypes=d.detailPrototypes;prototypes[8].minHeight=.22f;prototypes[8].maxHeight=.42f;d.detailPrototypes=prototypes;
d.SetAlphamaps(0,0,alpha);d.SetDetailLayer(0,0,8,detail);d.SetBaseMapDirty();Terrain.activeTerrain.Flush();EditorUtility.SetDirty(d);AssetDatabase.SaveAssetIfDirty(d);return new{changed,scope="전경 뿌리 부식층과 낮은 식생 경계. 지형 높이·씬 배치 불변"};
