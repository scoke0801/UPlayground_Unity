// 수면 중심의 흐름을 유지하고 가장자리 두 정점 열만 실제 둑 단면에 접속한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/ShadeCreekR3.unity"||scene.isDirty||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("R3 Edit 필요");
var terrain=Terrain.activeTerrain;var data=terrain.terrainData;
var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/10.Datas/World/ThemeStudies/ShadeCreekR3/CreekSurface.asset");var vertices=mesh.vertices;
float maximumFold=0;int edges=0;
for(int row=0;row<145;row++){
float centerY=vertices[row*13+6].y;
foreach(int side in new[]{0,12}){
int index=row*13+side;var edge=vertices[index];float ground=terrain.SampleHeight(edge)+terrain.transform.position.y;
float delta=Mathf.Min(0,ground-.025f-centerY);maximumFold=Mathf.Max(maximumFold,-delta);
vertices[index].y=centerY+delta;vertices[row*13+(side==0?1:11)].y=centerY+delta*.2f;if(delta<0)edges++;
}
// 양 끝은 지면 아래로 완만히 접속해 잘린 리본 끝이 드러나지 않게 한다.
float endFade=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,3,Mathf.Min(row,144-row)));
for(int col=0;col<13;col++){int index=row*13+col;var p=vertices[index];float ground=terrain.SampleHeight(p)+terrain.transform.position.y;vertices[index].y=Mathf.Lerp(p.y,Mathf.Min(p.y,ground-.025f),endFade);}
}
mesh.vertices=vertices;mesh.RecalculateBounds();mesh.RecalculateNormals();mesh.RecalculateTangents();EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);
var result=new{edges,maximumFold,centralColumnsUnchanged="2~10열, 양 끝 1.5m 제외",terrainChanged=false,collidersChanged=false,limitation="가장자리의 얕은 비평면 접속. 물리 유체 해석 아님. 측면 화면에서 접힘·좁아짐 재검토 필요"};
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);System.IO.File.WriteAllText("output/ShadeCreekR3/water-edge-join.json",(string)json.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{result}));return result;
