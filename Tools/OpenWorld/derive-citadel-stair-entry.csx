var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Environment/WorldCities/StonegateEast/Castle_Terrace_Stairs.prefab").GetComponent<MeshFilter>().sharedMesh;
var path="Assets/10.Datas/World/OpenWorld/CitadelArtRevision/CityEntryStair.asset";
if(AssetDatabase.LoadAssetAtPath<Mesh>(path)!=null)return "기존 진입 계단 메시 보존";
var mesh=UnityEngine.Object.Instantiate(source);mesh.name="CastleTerraceStairs_CityEntry";
var vertices=source.vertices;var triangles=source.triangles;var kept=new List<int>();int removed=0;
for(int i=0;i<triangles.Length;i+=3){
 var a=vertices[triangles[i]];var b=vertices[triangles[i+1]];var c=vertices[triangles[i+2]];var center=(a+b+c)/3;
 // 마지막 디딤판보다 높은 후면 막음판만 열어 본성 연결참과 접속한다.
 bool cap=Mathf.Abs(center.x)<2.8f&&center.z<-3.19f&&Mathf.Max(a.y,b.y,c.y)>3.401f;
 if(cap){removed++;continue;}kept.Add(triangles[i]);kept.Add(triangles[i+1]);kept.Add(triangles[i+2]);
}
if(removed==0)throw new Exception("진입 계단 막음판 없음");
mesh.triangles=kept.ToArray();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);AssetDatabase.SaveAssetIfDirty(mesh);
return "계단 후면 막음판 개방 "+removed;
