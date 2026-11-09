// 통합 전 빈터 재료 대비와 돌아보기의 대형 풀 접합을 보정한다.
var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(s.isDirty||EditorApplication.isPlayingOrWillChangePlaymode||s.path!="Assets/01.Scenes/ArtReview/WorldThemes/RedForestR1.unity")throw new Exception("저장된 검토 씬 필요");
string dir="Assets/10.Datas/World/ThemeStudies/RedForestR1/";var t=Terrain.activeTerrain;var d=t.terrainData;
if(AssetDatabase.GetAssetPath(d)!=dir+"Terrain.asset")throw new Exception("전용 지형 필요");
var names=new[]{"Meadow_Grass_1371_R2","Meadow_Grass_2193_R2","Meadow_Grass_2209_R2","Meadow_Grass_2371_R2","Meadow_Grass_2384_R2","Meadow_Grass_2434_R2","Meadow_Grass_2891_R2"};
var grasses=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(q=>names.Contains(q.name)).ToArray();
if(grasses.Length!=7||grasses.Any(q=>q.GetComponentsInChildren<MonoBehaviour>(true).Length>0||q.GetComponentsInChildren<Collider>(true).Length>0))throw new Exception("장식 풀 계약 불일치");
var mat=AssetDatabase.LoadAssetAtPath<Material>(dir+"UnderstoryGrass.mat");
foreach(var q in grasses){q.localScale=Vector3.one*.48f;var p=q.position;p.y=t.SampleHeight(p)-.03f;q.position=p;foreach(var r in q.GetComponentsInChildren<Renderer>(true)){r.sharedMaterials=r.sharedMaterials.Select(m=>mat).ToArray();PrefabUtility.RecordPrefabInstancePropertyModifications(r);}PrefabUtility.RecordPrefabInstancePropertyModifications(q);}
var a=d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);var layers=d.terrainLayers;
int humus=Array.FindIndex(layers,l=>l.name=="ForestHumus"),earth=Array.FindIndex(layers,l=>l.name=="ClearingEarth"),root=Array.FindIndex(layers,l=>l.name=="RootEarth");
int edited=0;
for(int z=0;z<d.alphamapHeight;z++)for(int x=0;x<d.alphamapWidth;x++){
float wx=x*2400f/(d.alphamapWidth-1),wz=-700+z*2800f/(d.alphamapHeight-1);
float radius=Mathf.Sqrt(Mathf.Pow((wx-1150)/66,2)+Mathf.Pow((wz-543)/74,2));if(radius>=1)continue;
float total=a[z,x,humus]+a[z,x,earth]+a[z,x,root];if(total<.1f)continue;
float noise=Mathf.PerlinNoise(wx*.09f+3,wz*.09f+8),fine=Mathf.PerlinNoise(wx*.27f,wz*.27f);
float patch=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,.66f,noise+fine*.12f));
float weight=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.75f,1,radius)))*.90f;
float h=.15f+patch*.70f;float r=.12f+(.5f-Mathf.Abs(patch-.5f))*.20f;
a[z,x,humus]=Mathf.Lerp(a[z,x,humus],total*h,weight);a[z,x,root]=Mathf.Lerp(a[z,x,root],total*r,weight);a[z,x,earth]=total-a[z,x,humus]-a[z,x,root];edited++;
}
d.SetAlphamaps(0,0,a);d.SetBaseMapDirty();t.Flush();EditorUtility.SetDirty(d);AssetDatabase.SaveAssetIfDirty(d);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(s);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s);
return new{resizedGrasses=grasses.Length,surfaceSamples=edited};
