// 밝은 젖은 하상과 낮은 암반 가장자리로 숲바닥의 큰 면을 나눈다.
var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string dir="Assets/10.Datas/World/ThemeStudies/ShadeCreekR1/";
if(s.path!="Assets/01.Scenes/ArtReview/WorldThemes/ShadeCreekR1.unity"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("검토 씬 필요");
var t=Terrain.activeTerrain;var d=t.terrainData;int n=d.terrainLayers.Length-4;
var humus=d.terrainLayers[n];humus.diffuseRemapMax=new Vector4(.64f,.64f,.55f,1);EditorUtility.SetDirty(humus);AssetDatabase.SaveAssetIfDirty(humus);
var bed=d.terrainLayers[n+3];bed.diffuseRemapMax=new Vector4(.90f,.97f,.94f,1);bed.tileSize=Vector2.one*2.4f;bed.smoothness=.48f;EditorUtility.SetDirty(bed);AssetDatabase.SaveAssetIfDirty(bed);
var a=d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);
for(int z=0;z<a.GetLength(0);z++)for(int x=0;x<a.GetLength(1);x++){
float wx=x*d.size.x/(a.GetLength(1)-1),wz=-700+z*d.size.z/(a.GetLength(0)-1);if(wz<758||wz>836||wx<1608||wx>1642)continue;
float center=1623+Mathf.Sin((wz-765)*.073f)*5;float width=2.7f+Mathf.PerlinNoise(wz*.12f,4)*1.7f;
float weight=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(width,width+3,Mathf.Abs(wx-center))))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(758,770,wz))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(824,836,wz)))*.88f;
float total=0;for(int j=n;j<n+4;j++)total+=a[z,x,j];for(int j=n;j<n+4;j++)a[z,x,j]*=1-weight;a[z,x,n+3]+=total*weight;
}d.SetAlphamaps(0,0,a);d.SetBaseMapDirty();EditorUtility.SetDirty(d);AssetDatabase.SaveAssetIfDirty(d);t.Flush();
var root=new GameObject("ShadeCreekR1_BankRocks");root.transform.SetParent(GameObject.Find("AuthoredEnvironment").transform,false);
string source="Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Prefabs/Stones/RiverRocks/PF_EastLands_RiverRock_01.prefab";
var points=new[]{new Vector3(1628,0,774),new Vector3(1629.7f,0,775.2f),new Vector3(1624.5f,0,792),new Vector3(1626,0,794),new Vector3(1615.5f,0,812),new Vector3(1617,0,815)};
var copies=new Dictionary<Material,Material>();
for(int i=0;i<points.Length;i++){
var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(source),s);g.name="WetBankRock_"+i;g.transform.SetParent(root.transform,false);g.transform.localScale=new Vector3(.22f,.12f,.26f)*(i%2==0?1.25f:1);g.transform.rotation=Quaternion.Euler(0,i*73,0);var p=points[i];p.y=t.SampleHeight(p)-.15f;g.transform.position=p;
foreach(var c in g.GetComponentsInChildren<Collider>(true)){c.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
foreach(var r in g.GetComponentsInChildren<Renderer>(true)){var mats=r.sharedMaterials;for(int j=0;j<mats.Length;j++){var m=mats[j];if(!copies.ContainsKey(m)){var c=UnityEngine.Object.Instantiate(m);if(c.HasProperty("_BaseColor"))c.SetColor("_BaseColor",new Color(.52f,.65f,.62f));if(c.HasProperty("_Smoothness"))c.SetFloat("_Smoothness",.42f);AssetDatabase.CreateAsset(c,dir+"WetStone_"+copies.Count+".mat");copies.Add(m,c);}mats[j]=copies[m];}r.sharedMaterials=mats;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(s);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s);
return "밝은 굽은 하상과 낮은 돌 가장자리 3군. 상시 수면·서사 상태·지지면 불변";
