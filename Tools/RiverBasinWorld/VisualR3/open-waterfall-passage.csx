// 낙구 통과 공간과 물살 영역만 저작한다. 시각 수면과 수영 판정 메시를 보존한다.
if(EditorApplication.isPlaying || EditorUtility.scriptCompilationFailed)throw new Exception("컴파일이 정상인 Edit Mode에서 실행한다.");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!="Assets/01.Scenes/Ingame/Worlds/RiverBasinWorld.unity")throw new Exception("RiverBasinWorld 씬이 필요하다.");
const string Dir="Assets/10.Datas/World/RiverBasinWorld/VisualR3/";
const float ApproachDepth=2.6f, EdgeBlend=1.8f, FlowSpeed=6f;
var terrain=Terrain.activeTerrains.First(item=>item.name=="Terrain_2_1");
var data=terrain.terrainData;var origin=terrain.transform.position;
int resolution=data.heightmapResolution;
float stepX=data.size.x/(resolution-1),stepZ=data.size.z/(resolution-1);
var heights=data.GetHeights(0,0,resolution,resolution);var before=(float[,])heights.Clone();
var root=GameObject.Find("GeneratedEnvironment/B1_ArtR1/B1 · 급류와 폭포").transform;
var river=GameObject.Find("GeneratedEnvironment/B1_ArtR1/B1 · 강 수면").GetComponent<UPlayGround.Components.FootstepWaterSurface>();
river.RefreshMesh();
Func<float,float,float,float> smooth=(a,b,value)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,value));
var report=new System.Text.StringBuilder();int changed=0;
for(int fallIndex=0;fallIndex<2;fallIndex++){
 var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Dir+"B1Waterfall_"+(fallIndex+1)+".asset");
 var vertices=mesh.vertices;var uv=mesh.uv;int lipRow=-1;
 for(int row=0;row<vertices.Length;row+=25)if(Mathf.Abs(uv[row].y-0.16f)<0.001f)lipRow=row;
 if(lipRow<0)throw new Exception("연결된 낙구 단면이 없다.");
 float lipZ=vertices[lipRow].z;
 float centerX=vertices[lipRow+12].x,top=vertices[lipRow+12].y-0.008f;
 float bottom=fallIndex==0?59.6f:51f;
 float halfWidth=Mathf.Min(centerX-vertices[lipRow].x,vertices[lipRow+24].x-centerX)-0.8f;
 for(int zIndex=0;zIndex<resolution;zIndex++){
  float z=origin.z+zIndex*stepZ;if(z>lipZ+9f||z<lipZ-6f)continue;
  float entrance=smooth(lipZ+9f,lipZ+6f,z);
  float drop=smooth(lipZ+2.5f,lipZ-1.5f,z);
  for(int xIndex=0;xIndex<resolution;xIndex++){
   float x=origin.x+xIndex*stepX;float distance=Mathf.Abs(x-centerX);
   if(distance>=halfWidth)continue;
   float across=1-smooth(halfWidth-EdgeBlend,halfWidth,distance);
   Vector3 waterPoint;float water=river.TrySampleHeight(new Vector3(x,top,z),out waterPoint)?waterPoint.y:top;
   // 낙하면 뒤에 남은 턱도 낮춰, 포물선으로 떨어지는 몸의 통과 공간을 확보한다.
   float target=Mathf.Lerp(water-ApproachDepth,bottom-3f,drop);
   float current=heights[zIndex,xIndex]*data.size.y+origin.y;
   if(target>=current)continue;
   float lowered=Mathf.Lerp(current,target,entrance*across);
   heights[zIndex,xIndex]=(lowered-origin.y)/data.size.y;changed++;
  }
 }
 string name="폭포 물살 "+(fallIndex+1);var existing=root.Find(name);
 var go=existing!=null?existing.gameObject:new GameObject(name);
 if(existing==null){Undo.RegisterCreatedObjectUndo(go,"폭포 물살 영역");go.transform.SetParent(root,false);}
 Undo.RecordObject(go.transform,"폭포 물살 배치");
 go.transform.SetPositionAndRotation(new Vector3(centerX,top,lipZ-0.6f),Quaternion.Euler(0,180,0));
 var currentComponent=go.GetComponent<UPlayGround.Components.WaterfallCurrent>();
 if(currentComponent==null)currentComponent=Undo.AddComponent<UPlayGround.Components.WaterfallCurrent>(go);
 var settings=new SerializedObject(currentComponent);
 float lower=bottom-top-2f;
 settings.FindProperty("_region").boundsValue=new Bounds(new Vector3(0,(lower+0.8f)/2f,1f),new Vector3((halfWidth-EdgeBlend)*2f,0.8f-lower,14f));
 settings.FindProperty("_fallStart").floatValue=0;
 settings.FindProperty("_releaseHeight").floatValue=bottom-top-0.6f;
 settings.FindProperty("_flowSpeed").floatValue=FlowSpeed;
 settings.FindProperty("_minimumFallSpeed").floatValue=3f;
 settings.ApplyModifiedProperties();
 report.AppendLine(name+" center="+go.transform.position+" width="+((halfWidth-EdgeBlend)*2f).ToString("F2")+" release="+(bottom-0.6f));
}
Undo.RegisterCompleteObjectUndo(data,"낙구 통과 공간 확보");data.SetHeights(0,0,heights);EditorUtility.SetDirty(data);terrain.Flush();Physics.SyncTransforms();
// 지형에 붙어 있던 작은 바위·풀만 따라 내린다. 낙구를 가리는 큰 암반과 물보라는 유지한다.
int moved=0;
foreach(var tr in root.GetComponentsInChildren<Transform>(true)){
 if(!PrefabUtility.IsOutermostPrefabInstanceRoot(tr.gameObject))continue;
 var renderers=tr.GetComponentsInChildren<MeshRenderer>(true);if(renderers.Length==0)continue;
 var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
 if(bounds.size.x>4.5f || bounds.size.y>4.5f || bounds.size.z>4.5f)continue;
 int x=Mathf.RoundToInt((tr.position.x-origin.x)/stepX),z=Mathf.RoundToInt((tr.position.z-origin.z)/stepZ);
 if(x<0||z<0||x>=resolution||z>=resolution)continue;
 float delta=(heights[z,x]-before[z,x])*data.size.y;
 if(delta>=-0.03f)continue;
 Undo.RecordObject(tr,"지형에 맞춰 재배치");tr.position+=Vector3.up*delta;moved++;
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
report.AppendLine("lowered="+changed+" resnapped="+moved);
System.IO.Directory.CreateDirectory("output/RiverBasinWorld/WaterfallTraversal");
System.IO.File.WriteAllText("output/RiverBasinWorld/WaterfallTraversal/application.txt",report.ToString());
return report.ToString();

