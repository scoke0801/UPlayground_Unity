var folder="Assets/08.Models/Characters/BokuseiP09";
var profile=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/Bokusei_P09_Face.asset");
var mesh=profile.mesh;var oldVertices=mesh.vertices;var oldNormals=mesh.normals;var oldTangents=mesh.tangents;
var names=Enumerable.Range(0,mesh.blendShapeCount).Select(mesh.GetBlendShapeName).ToArray();
Vector3[] vertices;Vector2[] uv;int[][] triangles;Vector3[][] deltas;
using(var reader=new System.IO.BinaryReader(System.IO.File.OpenRead("ArtSource/ExternalCharacterBuilder/BokuseiFace/EyesR03/eyes.bin"))){
int count=reader.ReadInt32(),slots=reader.ReadInt32(),shapes=reader.ReadInt32();
if(count!=mesh.vertexCount||shapes!=names.Length||slots!=4)throw new Exception("눈 데이터 계약 불일치");
Func<Vector3[]> points=()=>Enumerable.Range(0,count).Select(i=>new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle())).ToArray();
vertices=points();uv=Enumerable.Range(0,count).Select(i=>new Vector2(reader.ReadSingle(),reader.ReadSingle())).ToArray();
triangles=Enumerable.Range(0,slots).Select(s=>Enumerable.Range(0,reader.ReadInt32()).Select(i=>reader.ReadInt32()).ToArray()).ToArray();
deltas=Enumerable.Range(0,shapes).Select(s=>points()).ToArray();
}
var scratch=new Mesh();scratch.vertices=vertices;scratch.uv=uv;scratch.subMeshCount=triangles.Length;
for(int s=0;s<triangles.Length;s++)scratch.SetTriangles(triangles[s],s);
scratch.RecalculateNormals();scratch.RecalculateTangents();
var normals=scratch.normals;var tangents=scratch.tangents;
var changed=Enumerable.Range(0,vertices.Length).Select(i=>(vertices[i]-oldVertices[i]).sqrMagnitude>1e-16f).ToArray();
for(int i=0;i<vertices.Length;i++)if(!changed[i]){normals[i]=oldNormals[i];tangents[i]=oldTangents[i];}
var normalDeltas=new Vector3[names.Length][];var tangentDeltas=new Vector3[names.Length][];
for(int s=0;s<names.Length;s++){
var previous=new Vector3[vertices.Length];var dn=new Vector3[vertices.Length];var dt=new Vector3[vertices.Length];mesh.GetBlendShapeFrameVertices(s,0,previous,dn,dt);
scratch.vertices=Enumerable.Range(0,vertices.Length).Select(i=>vertices[i]+deltas[s][i]).ToArray();scratch.RecalculateNormals();scratch.RecalculateTangents();
var sn=scratch.normals;var st=scratch.tangents;
for(int i=0;i<vertices.Length;i++)if(changed[i]){dn[i]=sn[i]-normals[i];dt[i]=(Vector3)(st[i]-tangents[i]);}
normalDeltas[s]=dn;tangentDeltas[s]=dt;
}
UnityEngine.Object.DestroyImmediate(scratch);
AssetDatabase.ImportAsset(folder+"/Bokusei_SageIris.png",ImportAssetOptions.ForceSynchronousImport);
var importer=(TextureImporter)AssetImporter.GetAtPath(folder+"/Bokusei_SageIris.png");importer.sRGBTexture=true;importer.mipmapEnabled=true;importer.maxTextureSize=512;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
var iris=AssetDatabase.LoadAssetAtPath<Material>(folder+"/Bokusei_P09_Iris.mat");
if(iris==null){iris=new Material(profile.materials[0]);iris.name="Bokusei_P09_Iris";AssetDatabase.CreateAsset(iris,folder+"/Bokusei_P09_Iris.mat");}
iris.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Bokusei_SageIris.png");iris.mainTextureScale=Vector2.one;iris.mainTextureOffset=Vector2.zero;iris.SetColor("_Color",Color.white);iris.SetVector("_MainTexHSVG",new Vector4(0,1,1,1));
mesh.ClearBlendShapes();mesh.vertices=vertices;mesh.uv=uv;mesh.subMeshCount=triangles.Length;
for(int s=0;s<triangles.Length;s++)mesh.SetTriangles(triangles[s],s);
mesh.normals=normals;mesh.tangents=tangents;
for(int s=0;s<names.Length;s++)mesh.AddBlendShapeFrame(names[s],100,deltas[s],normalDeltas[s],tangentDeltas[s]);
mesh.RecalculateBounds();profile.materials=new[]{profile.materials[0],profile.materials[1],profile.materials[2],iris};profile.ValidateProfile();
foreach(var asset in new UnityEngine.Object[]{mesh,profile,iris}){EditorUtility.SetDirty(asset);AssetDatabase.SaveAssetIfDirty(asset);}
foreach(string path in new[]{"Assets/03.Prefabs/Characters/ExternalModels/Bokusei_P09Compatible.prefab","Assets/03.Prefabs/Characters/ExternalModels/Bokusei_P09_Alternate.prefab"}){
var root=PrefabUtility.LoadPrefabContents(path);try{profile.Apply(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
}
return new{vertices=mesh.vertexCount,expressions=mesh.blendShapeCount,materials=profile.materials.Length,triangles=mesh.triangles.Length/3};
