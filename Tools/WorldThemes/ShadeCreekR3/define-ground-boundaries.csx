// 통행축과 활동 공간을 둘러싼 두 큰 부식층 면을 구분한다. 높이·식생·콘텐츠는 수정하지 않는다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/ShadeCreekR3.unity"||scene.isDirty||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("저장된 R3 Edit 필요");
var terrain=Terrain.activeTerrain;var data=terrain.terrainData;
if(AssetDatabase.GetAssetPath(data)!="Assets/10.Datas/World/ThemeStudies/ShadeCreekR3/Terrain.asset")throw new Exception("전용 Terrain 필요");
var alpha=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);
var route=new[]{new Vector2(1640,750),new Vector2(1605,805),new Vector2(1608,814),new Vector2(1650,855)};
int changed=0;
for(int iz=0;iz<data.alphamapHeight;iz++)for(int ix=0;ix<data.alphamapWidth;ix++){
float x=ix*data.size.x/(data.alphamapWidth-1),z=-700+iz*data.size.z/(data.alphamapHeight-1);
if(x<1580||x>1625||z<780||z>826||alpha[iz,ix,18]>.1f)continue;
var p=new Vector2(x,z);float distance=999;
for(int i=0;i<route.Length-1;i++){var delta=route[i+1]-route[i];distance=Mathf.Min(distance,Vector2.Distance(p,route[i]+delta*Mathf.Clamp01(Vector2.Dot(p-route[i],delta)/delta.sqrMagnitude)));}
float left=Mathf.Sqrt(Mathf.Pow((x-1588)/13,2)+Mathf.Pow((z-806)/17,2));
float right=Mathf.Sqrt(Mathf.Pow((x-1618)/9,2)+Mathf.Pow((z-795)/14,2));
float bed=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.82f,1.02f,Mathf.Min(left,right)+.035f*Mathf.Sin(z*.28f)));
float tread=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.8f,4.1f,distance));
float clearing=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(4,7,Vector2.Distance(p,new Vector2(1608,811))));tread=Mathf.Max(tread,clearing);
float scope=Mathf.SmoothStep(0,1,Mathf.Clamp01(Mathf.Min(x-1580,1625-x,z-780,826-z)/4));
float w=bed*(1-tread)*scope*.98f;
if(w>.01f){for(int l=0;l<data.alphamapLayers;l++)alpha[iz,ix,l]*=1-w;alpha[iz,ix,15]+=w*.96f;alpha[iz,ix,16]+=w*.04f;changed++;}
float pathWeight=tread*scope*.9f;
if(pathWeight>.01f){for(int l=0;l<data.alphamapLayers;l++)alpha[iz,ix,l]*=1-pathWeight;alpha[iz,ix,17]+=pathWeight;changed++;}
}
data.SetAlphamaps(0,0,alpha);data.SetBaseMapDirty();terrain.Flush();EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);
var result=new{changed,heightChanged=false,vegetationChanged=false,scope="두 큰 부식층 면",description="두 큰 부식층 면과 중앙 통행·활동 공간 경계. 물가 재료 보존."};
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);System.IO.File.WriteAllText("output/ShadeCreekR3/ground-boundaries.json",(string)json.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{result}));return result;

