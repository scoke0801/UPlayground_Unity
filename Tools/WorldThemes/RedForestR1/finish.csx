// 반복 소품 대신 사면의 암반 노출과 수관 아래 부식층을 묶는다.
var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(EditorApplication.isPlayingOrWillChangePlaymode||s.path!="Assets/01.Scenes/ArtReview/WorldThemes/RedForestR1.unity")throw new Exception("검토 씬 필요");
var t=Terrain.activeTerrain;var d=t.terrainData;string dir="Assets/10.Datas/World/ThemeStudies/RedForestR1/";
var humus=AssetDatabase.LoadAssetAtPath<TerrainLayer>(dir+"ForestHumus.terrainlayer");humus.tileSize=Vector2.one*4;humus.diffuseRemapMax=new Vector4(.38f,.34f,.31f,1);EditorUtility.SetDirty(humus);AssetDatabase.SaveAssetIfDirty(humus);
var stone=AssetDatabase.LoadAssetAtPath<TerrainLayer>(dir+"WeatheredRock.terrainlayer");stone.tileSize=Vector2.one*7;stone.diffuseRemapMax=new Vector4(.62f,.59f,.56f,1);EditorUtility.SetDirty(stone);AssetDatabase.SaveAssetIfDirty(stone);
var a=d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);int n=d.terrainLayers.Length;
for(int z=0;z<a.GetLength(0);z++)for(int x=0;x<a.GetLength(1);x++){
float wx=x*2400f/(a.GetLength(1)-1),wz=-700+z*2800f/(a.GetLength(0)-1);float total=0;for(int j=n-4;j<n;j++)total+=a[z,x,j];if(total<.01f)continue;
float left=Mathf.Sqrt(Mathf.Pow((wx-1120)/20,2)+Mathf.Pow((wz-547)/12,2));float right=Mathf.Sqrt(Mathf.Pow((wx-1180)/15,2)+Mathf.Pow((wz-565)/16,2));float noise=Mathf.PerlinNoise(wx*.21f,wz*.21f);float rock=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.3f,1,Mathf.Min(left,right)+noise*.18f)))*.80f;
float darkEdge=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,20,Mathf.Abs(wx-1118))))*.55f;
for(int j=n-4;j<n;j++)a[z,x,j]*=1-rock;a[z,x,n-1]+=total*rock;
float earth=a[z,x,n-2]*darkEdge;a[z,x,n-2]-=earth;a[z,x,n-4]+=earth;
}
d.SetAlphamaps(0,0,a);d.SetBaseMapDirty();t.Flush();EditorUtility.SetDirty(d);AssetDatabase.SaveAssetIfDirty(d);
var root=GameObject.Find("RedForestR1_Canopy");int adjusted=0;
foreach(Transform q in root.transform){var b=q.GetComponentsInChildren<Renderer>(true).First().bounds;float dx=Mathf.Max(b.min.x-1150,0,1150-b.max.x),dz=Mathf.Max(b.min.z-525,0,525-b.max.z);float gap=Mathf.Sqrt(dx*dx+dz*dz);if(gap<34){var away=new Vector3(q.position.x-1150,0,q.position.z-525).normalized;var p=q.position+away*(34-gap+1);p.y=t.SampleHeight(p)-.3f;q.position=p;PrefabUtility.RecordPrefabInstancePropertyModifications(q);adjusted++;}}
// 변형된 사면의 근접 원색 풀만 숨기고 다른 지역 식생은 보존한다.
var red=GameObject.Find("AuthoredEnvironment/월드 구간/붉은나무숲");int hidden=0;
foreach(var r in red.GetComponentsInChildren<Renderer>(true)){var p=r.transform.position;if(p.x<1100||p.x>1208||p.z<480||p.z>609)continue;if(!r.sharedMaterials.Any(m=>m!=null&&m.shader.name.Contains("Grass")))continue;r.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(r);hidden++;}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(s);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s);return new{flightMarginsAdjusted=adjusted,grassRenderersHidden=hidden};
