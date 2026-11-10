// 상판 보행용 BoxCollider를 유지하고 LOD2의 아치·교각 표면을 별도 정적 충돌로 저장한다.
if(EditorApplication.isPlaying||EditorUtility.scriptCompilationFailed)throw new Exception("정상 컴파일된 Edit Mode가 필요하다.");
const string PrefabPath="Assets/03.Prefabs/Environment/RiverBasinWorld/VisualR1/B1_BridgeArtR1.prefab";
const string MeshPath="Assets/10.Datas/World/RiverBasinWorld/VisualR1/B1BridgeLowerCollision.asset";
const string ColliderName="Castle_Moat_Bridge";
const float DeckBottom=-0.7f;
var contents=PrefabUtility.LoadPrefabContents(PrefabPath);
try{
 var lod=contents.GetComponent<LODGroup>();var levels=lod.GetLODs();
 var renderer=levels[levels.Length-1].renderers[0];var source=renderer.GetComponent<MeshFilter>();
 var matrix=contents.transform.worldToLocalMatrix*source.transform.localToWorldMatrix;
 var positions=source.sharedMesh.vertices.Select(v=>matrix.MultiplyPoint3x4(v)).ToArray();
 var indices=source.sharedMesh.triangles;
 var vertices=new List<Vector3>();var triangles=new List<int>();var lookup=new Dictionary<Vector3,int>();
 Func<Vector3,int> add=position=>{int index;if(lookup.TryGetValue(position,out index))return index;index=vertices.Count;vertices.Add(position);lookup[position]=index;return index;};
 for(int index=0;index<indices.Length;index+=3){
  var input=new[]{positions[indices[index]],positions[indices[index+1]],positions[indices[index+2]]};
  var polygon=new List<Vector3>(4);
  // 상판 아랫면에서 삼각형을 잘라 보행면·난간과 중복되는 충돌을 제외한다.
  for(int edge=0;edge<3;edge++){
   var a=input[edge];var b=input[(edge+1)%3];bool aInside=a.y<=DeckBottom,bInside=b.y<=DeckBottom;
   if(aInside)polygon.Add(a);
   if(aInside!=bInside)polygon.Add(Vector3.Lerp(a,b,(DeckBottom-a.y)/(b.y-a.y)));
  }
  for(int corner=1;corner+1<polygon.Count;corner++){
   var a=polygon[0];var b=polygon[corner];var c=polygon[corner+1];
   if(Vector3.Cross(b-a,c-a).sqrMagnitude<1e-10f)continue;
   triangles.Add(add(a));triangles.Add(add(b));triangles.Add(add(c));
  }
 }
 if(triangles.Count==0||vertices.Any(v=>float.IsNaN(v.x)||v.y>DeckBottom+.001f))throw new Exception("하부 충돌 메시 생성 오류");
 var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
 if(mesh==null){mesh=new Mesh();mesh.name="B1BridgeLowerCollision";AssetDatabase.CreateAsset(mesh,MeshPath);}
 mesh.Clear();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
 var child=contents.transform.Find(ColliderName)??contents.transform.Find("Castle_Moat_Bridge_LowerCollision");
 if(child==null){child=new GameObject(ColliderName).transform;child.SetParent(contents.transform,false);}
 child.name=ColliderName;
 child.gameObject.layer=contents.layer;child.gameObject.isStatic=true;
 var collider=child.GetComponent<MeshCollider>();if(collider==null)collider=child.gameObject.AddComponent<MeshCollider>();
 collider.sharedMesh=null;collider.sharedMesh=mesh;collider.convex=false;collider.isTrigger=false;collider.enabled=true;
 PrefabUtility.SaveAsPrefabAsset(contents,PrefabPath);
 AssetDatabase.SaveAssetIfDirty(mesh);
 System.IO.Directory.CreateDirectory("output/RiverBasinWorld/BridgeLowerCollision");
 string result="vertices="+vertices.Count+" triangles="+triangles.Count/3+" source="+source.sharedMesh.name;
 System.IO.File.WriteAllText("output/RiverBasinWorld/BridgeLowerCollision/application.txt",result);
 return result;
}finally{PrefabUtility.UnloadPrefabContents(contents);}
