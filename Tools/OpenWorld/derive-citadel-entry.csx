var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Environment/WorldCities/StonegateEast/Castle_Keep.prefab").GetComponent<MeshFilter>().sharedMesh;
var path="Assets/10.Datas/World/OpenWorld/CitadelArtRevision/KeepCityEntry.asset";
var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
System.IO.Directory.CreateDirectory("Assets/10.Datas/World/OpenWorld/CitadelArtRevision");AssetDatabase.Refresh();
var mesh=UnityEngine.Object.Instantiate(source);mesh.name="CastleKeep_CityEntry";
var vertices=source.vertices;var triangles=source.triangles;var kept=new List<int>();int removed=0;
for(int i=0;i<triangles.Length;i+=3){
 var a=vertices[triangles[i]];var b=vertices[triangles[i+1]];var c=vertices[triangles[i+2]];var center=(a+b+c)/3;
 // 정면·후면 계단 끝을 막는 중앙 난간만 열고 기단·외부 계단·건물은 유지한다.
 bool railing=Mathf.Abs(center.x)<4.7f&&Mathf.Abs(center.z)>10.8f&&center.y>10.015f&&center.y<12.3f&&Mathf.Min(a.y,b.y,c.y)>=9.99f;
 if(railing){removed++;continue;}kept.Add(triangles[i]);kept.Add(triangles[i+1]);kept.Add(triangles[i+2]);
}
if(removed==0)throw new Exception("개방할 난간 삼각형을 찾지 못했습니다.");
mesh.triangles=kept.ToArray();mesh.RecalculateBounds();
if(existing==null){AssetDatabase.CreateAsset(mesh,path);existing=mesh;}
else{EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);}
AssetDatabase.SaveAssetIfDirty(existing);
System.IO.File.WriteAllText("output/OpenWorld/CitadelArtRevision/keep-entry-mesh.txt","제거된 난간 삼각형="+removed+" / 원본 프리팹·메시 변경 없음");
return "도시 쪽 난간 개방 "+removed;
