// 오늘 저작한 수변 주택의 재질을 공유하는 항만 부속물. 원본 모델은 수정하지 않는다.
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("Edit Mode 필요");
string folder="Assets/03.Prefabs/Environment/HarborRevision";
System.IO.Directory.CreateDirectory(folder);AssetDatabase.Refresh();
var wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/08.Models/LakeVillage/Materials/Wall_Wood.mat");
var trim=AssetDatabase.LoadAssetAtPath<Material>("Assets/08.Models/LakeVillage/Materials/Trim_Wood.mat");
var stone=AssetDatabase.LoadAssetAtPath<Material>("Assets/08.Models/LakeVillage/Materials/Foundation_Stone.mat");
if(wood==null||trim==null||stone==null)throw new Exception("수변 재질 누락");
var stonePath=folder+"/Harbor_WeatheredStone.mat";stone=AssetDatabase.LoadAssetAtPath<Material>(stonePath);
if(stone==null){stone=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/PoorVillage/Props/PoorProp_Stone_Field.mat"));stone.name="Harbor_WeatheredStone";stone.color=new Color(.55f,.56f,.52f);stone.mainTextureScale=new Vector2(2,2);AssetDatabase.CreateAsset(stone,stonePath);}
var batches=new Dictionary<Material,List<CombineInstance>>();var temporary=new List<Mesh>();
Action<Mesh,Material,Vector3,Vector3,Quaternion> piece=(mesh,mat,p,s,q)=>{if(!batches.ContainsKey(mat))batches[mat]=new List<CombineInstance>();batches[mat].Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.TRS(p,q,s)});};
var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);var cubeMesh=cube.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(cube);
var cyl=GameObject.CreatePrimitive(PrimitiveType.Cylinder);var cylMesh=cyl.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(cyl);
Action<Material,Vector3,Vector3> box=(m,p,s)=>piece(cubeMesh,m,p,s,Quaternion.identity);
Action<Material,Vector3,Vector3,float> beam=(m,a,b,w)=>piece(cubeMesh,m,(a+b)*.5f,new Vector3(w,Vector3.Distance(a,b),w),Quaternion.FromToRotation(Vector3.up,b-a));
Action<string,Vector3,Vector3> save=(name,center,size)=>{
 var go=new GameObject(name);try{
  foreach(var batch in batches){var mesh=new Mesh{name=name+"_"+batch.Key.name};mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.CombineMeshes(batch.Value.ToArray(),true,true);mesh.RecalculateBounds();string path=folder+"/"+mesh.name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing!=null){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;EditorUtility.SetDirty(mesh);}else AssetDatabase.CreateAsset(mesh,path);var child=new GameObject(batch.Key.name);child.transform.SetParent(go.transform,false);child.AddComponent<MeshFilter>().sharedMesh=mesh;child.AddComponent<MeshRenderer>().sharedMaterial=batch.Key;}
  if(size!=Vector3.zero){var c=go.AddComponent<BoxCollider>();c.center=center;c.size=size;}
  PrefabUtility.SaveAsPrefabAsset(go,folder+"/"+name+".prefab");
 }finally{UnityEngine.Object.DestroyImmediate(go);batches.Clear();}
};
// 지지대를 수면 아래로 연장하고 잔교 상면을 배치 기준 y=0에 맞춘다.
for(int i=0;i<24;i++)box(wood,new Vector3(0,-.12f,-3.84f+i*.334f),new Vector3(6,.24f,.318f));
foreach(float x in new[]{-2.5f,0,2.5f})box(trim,new Vector3(x,-.48f,0),new Vector3(.28f,.5f,8.15f));
foreach(float x in new[]{-2.65f,2.65f})foreach(float z in new[]{-3.6f,0,3.6f}){
 piece(cylMesh,trim,new Vector3(x,-2.0f,z),new Vector3(.4f,2.7f,.4f),Quaternion.identity);
 piece(cylMesh,trim,new Vector3(x,.65f,z),new Vector3(.5f,.09f,.5f),Quaternion.identity);
}
foreach(float z in new[]{-3.6f,3.6f}){beam(trim,new Vector3(-2.65f,-2,z),new Vector3(2.65f,-.5f,z),.18f);beam(trim,new Vector3(2.65f,-2,z),new Vector3(-2.65f,-.5f,z),.18f);}
save("Harbor_TimberDeck",new Vector3(0,-.17f,0),new Vector3(6,.34f,8));
// 석축의 줄눈을 엇갈리게 쌓아 안벽의 두께와 구조를 드러낸다.
box(stone,new Vector3(0,-1.55f,0),new Vector3(8,3,8));
for(int layer=0;layer<4;layer++)for(int i=0;i<8;i++)foreach(int side in new[]{-1,1}){
 float x=-3.5f+i+(layer%2)*.22f;box(stone,new Vector3(Mathf.Clamp(x,-3.5f,3.5f),-.45f-layer*.67f,side*4),new Vector3(.94f,.61f,.2f));
}
for(int x=0;x<4;x++)for(int z=0;z<4;z++)box(stone,new Vector3(-3+x*2,-.06f,-3+z*2),new Vector3(1.98f,.14f,1.98f));
save("Harbor_StoneQuay",new Vector3(0,-1.5f,0),new Vector3(8,3,8));
piece(cylMesh,trim,new Vector3(0,.46f,0),new Vector3(.55f,.46f,.55f),Quaternion.identity);
box(trim,new Vector3(0,.86f,0),new Vector3(1.15f,.2f,.3f));
for(int i=0;i<3;i++)piece(cylMesh,wood,new Vector3(0,.23f+i*.11f,0),new Vector3(.7f,.045f,.7f),Quaternion.identity);
save("Harbor_Mooring",Vector3.zero,Vector3.zero);
// 선체를 곡선 단면의 판재와 뱃전으로 구성한다.
var vertices=new List<Vector3>();var indices=new List<int>();var uv=new List<Vector2>();
for(int i=0;i<=20;i++){
 float z=-4.8f+i*.48f;float f=Mathf.Clamp01(Mathf.Sin(Mathf.PI*i/20));float width=.08f+1.55f*Mathf.Pow(f,.65f);float rise=.6f*Mathf.Pow(1-f,2);
 vertices.Add(new Vector3(-width,1.05f+rise,z));vertices.Add(new Vector3(-width*.65f,.15f+rise,z));vertices.Add(new Vector3(0,-.12f+rise,z));vertices.Add(new Vector3(width*.65f,.15f+rise,z));vertices.Add(new Vector3(width,1.05f+rise,z));
 for(int k=0;k<5;k++)uv.Add(new Vector2(i*.24f,k*.32f));
 if(i>0)for(int k=0;k<4;k++){int a=(i-1)*5+k,b=i*5+k;indices.AddRange(new[]{a,b,a+1,a+1,b,b+1,a+1,b,a,b+1,b,a+1});}
 if(i>0)foreach(int side in new[]{0,4})beam(trim,vertices[(i-1)*5+side],vertices[i*5+side],.14f);
 if(i>2&&i<18&&i%3==0)box(wood,new Vector3(0,.72f,z),new Vector3(width*1.9f,.15f,.4f));
}
var hull=new Mesh{name="Hull"};hull.SetVertices(vertices);hull.SetTriangles(indices,0);hull.SetUVs(0,uv);hull.RecalculateNormals();temporary.Add(hull);piece(hull,wood,Vector3.zero,Vector3.one,Quaternion.identity);
beam(trim,new Vector3(0,.1f,3),new Vector3(0,1.8f,3.7f),.14f);
beam(trim,new Vector3(-.6f,.9f,-2),new Vector3(2.8f,1.3f,1),.09f);
box(wood,new Vector3(2.8f,1.3f,1),new Vector3(.25f,.1f,1.1f));
save("Harbor_FishingSkiff",Vector3.zero,Vector3.zero);
foreach(var m in temporary)UnityEngine.Object.DestroyImmediate(m);
AssetDatabase.SaveAssets();return "항만 부속 프리팹 4종 생성";

