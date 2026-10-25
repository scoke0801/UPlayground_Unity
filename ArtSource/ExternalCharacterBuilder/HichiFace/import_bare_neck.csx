// 양쪽 피부 경계 사이에 곡면과 재질 혼합 구간을 추가한다. 원본 얼굴·몸통 계약은 보존한다.
string folder="Assets/08.Models/Characters/HichiP09";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(folder+"/Hichi_P09_Head.asset");
var names=Enumerable.Range(0,mesh.blendShapeCount).Select(mesh.GetBlendShapeName).ToArray();var oldNormals=mesh.normals;var oldTangents=mesh.tangents;
var normalDeltas=new List<Vector3[]>();var tangentDeltas=new List<Vector3[]>();
for(int s=0;s<names.Length;s++){var dn=new Vector3[mesh.vertexCount];var dt=new Vector3[mesh.vertexCount];mesh.GetBlendShapeFrameVertices(s,0,new Vector3[mesh.vertexCount],dn,dt);normalDeltas.Add(dn);tangentDeltas.Add(dt);}
var faceTriangles=mesh.GetTriangles(0);var overlayTriangles=mesh.GetTriangles(1);var bindposes=mesh.bindposes;
Vector3[] vertices,normals;Vector2[] uv,uv2;BoneWeight[] weights;int[] neck;var deltas=new List<Vector3[]>();
using(var r=new System.IO.BinaryReader(System.IO.File.OpenRead("output/HichiBareNeckFix/bare_neck.bin"))){
    int count=r.ReadInt32(),triangleCount=r.ReadInt32(),shapeCount=r.ReadInt32();if(count!=9878||shapeCount!=names.Length)throw new Exception("r03 데이터 계약 오류");
    Func<Vector3[]> points=()=>{var a=new Vector3[count];for(int i=0;i<count;i++)a[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());return a;};
    vertices=points();normals=points();uv=new Vector2[count];uv2=new Vector2[count];for(int i=0;i<count;i++)uv[i]=new Vector2(r.ReadSingle(),r.ReadSingle());for(int i=0;i<count;i++)uv2[i]=new Vector2(r.ReadSingle(),r.ReadSingle());
    weights=new BoneWeight[count];for(int i=0;i<count;i++)weights[i]=new BoneWeight{boneIndex0=r.ReadInt32(),boneIndex1=r.ReadInt32(),boneIndex2=r.ReadInt32(),boneIndex3=r.ReadInt32(),weight0=r.ReadSingle(),weight1=r.ReadSingle(),weight2=r.ReadSingle(),weight3=r.ReadSingle()};
    neck=new int[triangleCount];for(int i=0;i<neck.Length;i++)neck[i]=r.ReadInt32();for(int s=0;s<shapeCount;s++)deltas.Add(points());
    if(r.BaseStream.Position!=r.BaseStream.Length)throw new Exception("r03 데이터 길이 오류");
}
var top=new[]{0,2,8,33,44,52,910,1009,1019,1023,1133,1127,1113,1105,1057,1052,1033,1035,1043,112,111,17};
var center=top.Aggregate(Vector3.zero,(sum,i)=>sum+vertices[i])/top.Length;top=top.OrderBy(i=>Mathf.Atan2(vertices[i].y-center.y,vertices[i].x-center.x)).ToArray();
mesh.ClearBlendShapes();mesh.Clear();mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;mesh.uv2=uv2;mesh.boneWeights=weights;mesh.bindposes=bindposes;
mesh.subMeshCount=3;mesh.SetTriangles(faceTriangles,0);mesh.SetTriangles(overlayTriangles,1);mesh.SetTriangles(neck,2);mesh.RecalculateTangents();var tangents=mesh.tangents;Array.Copy(oldTangents,tangents,9774);mesh.tangents=tangents;
for(int s=0;s<names.Length;s++){
    var dn=new Vector3[vertices.Length];var dt=new Vector3[vertices.Length];Array.Copy(normalDeltas[s],dn,9774);Array.Copy(tangentDeltas[s],dt,9774);
    for(int ring=0;ring<4;ring++)for(int j=0;j<22;j++){int i=9790+ring*22+j;float f=(ring+1)*.25f;dn[i]=normalDeltas[s][top[j]]*f;dt[i]=tangentDeltas[s][top[j]]*f;}
    mesh.AddBlendShapeFrame(names[s],100,deltas[s],dn,dt);
}
mesh.RecalculateBounds();mesh.UploadMeshData(false);EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);
// UV의 목 길이 좌표를 재질 가중치로 해석하는 선형 데이터 텍스처다.
string maskPath=folder+"/Hichi_NeckBlendMask.asset";var mask=AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);
if(mask==null){mask=new Texture2D(1,256,TextureFormat.RGBA32,false,true);mask.name="Hichi_NeckBlendMask";mask.wrapMode=TextureWrapMode.Clamp;mask.filterMode=FilterMode.Bilinear;var pixels=new Color[256];for(int y=0;y<256;y++){float t=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.009f,.037f,y/255f));pixels[y]=new Color(t,t,t,1);}mask.SetPixels(pixels);mask.Apply();AssetDatabase.CreateAsset(mask,maskPath);}
var face=AssetDatabase.LoadAssetAtPath<Material>(folder+"/Hichi_P09_Face.mat");var body=AssetDatabase.LoadAssetAtPath<Material>(folder+"/Hichi_P09_Skin.mat");
string materialPath=folder+"/Hichi_P09_Neck.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);bool create=material==null;if(create)material=new Material(face);material.parent=null;material.CopyPropertiesFromMaterial(face);material.name="Hichi_P09_Neck";
material.SetFloat("_UseMain2ndTex",1);material.SetColor("_Color2nd",Color.white);material.SetTexture("_Main2ndTex",body.mainTexture);material.SetFloat("_Main2ndTex_UVMode",1);material.SetFloat("_Main2ndTexIsDecal",0);material.SetTexture("_Main2ndBlendMask",mask);material.SetFloat("_Main2ndEnableLighting",1);material.SetFloat("_Main2ndTexBlendMode",0);
material.SetFloat("_UseMain3rdTex",1);material.SetColor("_Color3rd",body.GetColor("_Color2nd"));material.SetTexture("_Main3rdTex",null);material.SetFloat("_Main3rdTexIsDecal",0);material.SetTexture("_Main3rdBlendMask",mask);material.SetFloat("_Main3rdEnableLighting",1);material.SetFloat("_Main3rdTexBlendMode",0);
if(create)AssetDatabase.CreateAsset(material,materialPath);else{EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);}
var profile=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/Hichi_P09_Face.asset");profile.materials[2]=material;profile.ValidateProfile();EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);
foreach(string name in new[]{"Hichi_P09Compatible","Hichi_P09_Alternate"}){string path="Assets/03.Prefabs/Characters/ExternalModels/"+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);try{profile.Apply(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
return new{vertices=mesh.vertexCount,neckTriangles=neck.Length/3,shapes=mesh.blendShapeCount};
