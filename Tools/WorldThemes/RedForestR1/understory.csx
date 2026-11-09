// 전투 중심과 보행선을 비우고 검토 지형에만 낮은 숲풀을 복원한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
string dir="Assets/10.Datas/World/ThemeStudies/RedForestR1";
if(EditorApplication.isPlayingOrWillChangePlaymode||scene.isDirty||scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/RedForestR1.unity")throw new Exception("저장된 검토 씬 필요");
var t=Terrain.activeTerrain;var d=t.terrainData;
if(AssetDatabase.GetAssetPath(d)!=dir+"/Terrain.asset")throw new Exception("전용 데이터 필요");
if(AssetDatabase.LoadAssetAtPath<GameObject>(dir+"/UnderstoryGrass.prefab")!=null)throw new Exception("중복 적용 금지");
var mat=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<Material>("Assets/11.Material/TerrainDetail/TD_Grass_Fresh.mat"));
mat.SetColor("_ColorBottom",new Color(.13f,.26f,.13f));mat.SetColor("_ColorTop",new Color(.29f,.47f,.23f));mat.SetColor("_ColorTopVariation",new Color(.40f,.55f,.26f));
AssetDatabase.CreateAsset(mat,dir+"/UnderstoryGrass.mat");
var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/11.Material/TerrainDetail/TD_Grass_Fresh.prefab"));
foreach(var r in go.GetComponentsInChildren<Renderer>())r.sharedMaterial=mat;
var prefab=PrefabUtility.SaveAsPrefabAsset(go,dir+"/UnderstoryGrass.prefab");UnityEngine.Object.DestroyImmediate(go);
var prototypes=d.detailPrototypes.ToList();var proto=new DetailPrototype(prototypes[2]);proto.prototype=prefab;proto.minHeight=.65f;proto.maxHeight=1.05f;proto.minWidth=.95f;proto.maxWidth=1.45f;prototypes.Add(proto);d.detailPrototypes=prototypes.ToArray();
var route=new[]{new Vector2(1150,475),new Vector2(1150,525),new Vector2(1152.8f,558.4f),new Vector2(1152.8f,560.4f),new Vector2(1135,580),new Vector2(1125,600)};
Func<float,float,float> distance=(x,z)=>{var p=new Vector2(x,z);float best=999;for(int i=0;i<route.Length-1;i++){var v=route[i+1]-route[i];best=Mathf.Min(best,Vector2.Distance(p,route[i]+v*Mathf.Clamp01(Vector2.Dot(p-route[i],v)/v.sqrMagnitude)));}return best;};
var density=new int[d.detailHeight,d.detailWidth];int cells=0,total=0;
for(int z=0;z<d.detailHeight;z++)for(int x=0;x<d.detailWidth;x++){
float wx=x*d.size.x/(d.detailWidth-1),wz=-700+z*d.size.z/(d.detailHeight-1);
float r=Mathf.Sqrt(Mathf.Pow((wx-1150)/76,2)+Mathf.Pow((wz-543)/84,2));if(r>=1||distance(wx,wz)<4.5f)continue;
float battle=Vector2.Distance(new Vector2(wx,wz),new Vector2(1150,525));if(battle<16)continue;
float weight=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.78f,1,r)))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(16,23,battle))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(4.5f,8,distance(wx,wz)));
float patch=Mathf.PerlinNoise(wx*.055f+13,wz*.055f+7);weight*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.3f,.65f,patch));
int n=Mathf.RoundToInt(weight*12);density[z,x]=n;if(n>0){cells++;total+=n;}
}
d.SetDetailLayer(0,0,prototypes.Count-1,density);EditorUtility.SetDirty(d);AssetDatabase.SaveAssetIfDirty(d);AssetDatabase.SaveAssetIfDirty(mat);
System.IO.File.WriteAllText("output/RedForestR1/Understory/applied.txt","낮은 숲풀: cells="+cells+", density sum="+total+"; 길 중심 4.5m와 전투 중심 반경16m 비움; 지형 높이·기존 풀·RP·조명·조우 변경 없음");
return new{cells,total,prototype=prototypes.Count-1};
