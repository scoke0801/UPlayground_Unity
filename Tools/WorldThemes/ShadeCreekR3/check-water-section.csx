// 수면 메쉬와 실제 Terrain 단면을 비교한다. KCC 물가 복귀 판정은 별도다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/ShadeCreekR3.unity"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("R3 Edit 필요");
var terrain=Terrain.activeTerrain;var data=terrain.terrainData;
var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/10.Datas/World/ThemeStudies/ShadeCreekR3/CreekSurface.asset");
var vertices=mesh.vertices;var rows=new List<object>();int buriedCenters=0,exposedEdges=0;float maxSlope=0;
Func<float,float,float> ground=(x,z)=>terrain.transform.position.y+data.GetInterpolatedHeight((x-terrain.transform.position.x)/data.size.x,(z-terrain.transform.position.z)/data.size.z);
for(int row=0;row<vertices.Length/13;row++){
var center=vertices[row*13+6];var left=vertices[row*13];var right=vertices[row*13+12];
float centerDepth=center.y-ground(center.x,center.z);
float leftGap=left.y-ground(left.x,left.z),rightGap=right.y-ground(right.x,right.z);
if(centerDepth<-.02f)buriedCenters++;if(leftGap>.05f||rightGap>.05f)exposedEdges++;
float slope=0;for(int sign=-1;sign<=1;sign+=2){float edge=sign<0?left.x:right.x;for(int j=0;j<6;j++){float x=edge+sign*j*.5f;float delta=Mathf.Abs(ground(x+sign*.5f,center.z)-ground(x,center.z));slope=Mathf.Max(slope,Mathf.Atan2(delta,.5f)*Mathf.Rad2Deg);}}
maxSlope=Mathf.Max(maxSlope,slope);rows.Add(new{row,z=center.z,width=right.x-left.x,centerDepth,leftGap,rightGap,outwardSlopeDegrees=slope});
}
var result=new{sampleRows=rows.Count,buriedCenters,exposedEdges,maxSlope,rows,interpretation="양수 edgeGap은 Terrain 위에 노출된 메쉬 끝 후보. 양수 centerDepth는 수면 아래 하상 깊이. 반미터 표본 기울기는 KCC 복귀 검증이 아님. 끝단 테이퍼와 보호 구간은 화면과 함께 판정.",waterOwnership="R1에서 만든 비서사 독립 검토 후보. 기존 승인 수면·DroughtVillage CreekArt 아님",tpsReturn="미수행"};
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
System.IO.File.WriteAllText("output/ShadeCreekR3/water-section-final.json",(string)json.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{result}));
return new{sampleRows=rows.Count,buriedCenters,exposedEdges,maxSlope};
