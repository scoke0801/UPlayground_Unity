// 전용 UV 메시·재질을 검수 프리팹에 함께 연결한다. 본체 적용은 별도 검수 후 수행한다.
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("편집 모드가 필요합니다.");
string canonical="Assets/03.Prefabs/Characters/ScenarioResidents/Npc/Npc_Daon/Npc_Daon.prefab";
string folder="Assets/08.Models/Characters/DaonPortrait";
string review="Assets/03.Prefabs/Characters/EquipmentReview/DaonPortrait";
string matPath="Assets/10.Datas/Actor/ScenarioResidents/ToonLook/DaonPortrait/Daon_Head_Skin.mat";
System.IO.Directory.CreateDirectory(review);AssetDatabase.Refresh();
var xml=new System.Xml.XmlDocument();xml.Load("ArtSource/P09ToonLook/DaonPortrait/head.xml");
Func<string,float> number=s=>float.Parse(s,System.Globalization.CultureInfo.InvariantCulture);
Func<System.Xml.XmlNode,float[]> values=n=>n.InnerText.Split(' ').Select(number).ToArray();
Func<System.Xml.XmlNode,Vector3> vector=n=>{var v=values(n);return new Vector3(-v[0],v[1],v[2]);};
Func<string,System.Xml.XmlNode[]> nodes=path=>xml.SelectNodes(path).Cast<System.Xml.XmlNode>().ToArray();
var root=PrefabUtility.LoadPrefabContents(canonical);
try
{
    var face=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Female_Face_02");
    var original=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/08.Models/Characters/DaonSculpt/Female_Face_02.asset");
    var points=nodes("/head/vertices/v").Select(vector).ToArray();
    var groups=nodes("/head/groups/v").Select(x=>x.InnerText).ToArray();
    var bones=groups.Select(n=>Array.FindIndex(face.bones,b=>b.name==n)).ToArray();
    if(bones.Any(i=>i<0))throw new Exception("얼굴 본 매핑 누락");
    var loops=nodes("/head/loops/v").Select(values).ToArray();
    var sourceWeights=nodes("/head/weights/v");
    var positions=new Vector3[loops.Length];var normals=new Vector3[loops.Length];var uv=new Vector2[loops.Length];
    var counts=new byte[loops.Length];var weights=new List<BoneWeight1>();
    for(int i=0;i<loops.Length;i++)
    {
        var l=loops[i];int vi=(int)l[0];positions[i]=points[vi];uv[i]=new Vector2(l[1],l[2]);normals[i]=new Vector3(-l[3],l[4],l[5]);
        var ws=sourceWeights[vi].ChildNodes.Cast<System.Xml.XmlNode>().OrderByDescending(w=>number(w.Attributes["weight"].Value)).ToArray();
        float sum=ws.Sum(w=>number(w.Attributes["weight"].Value));if(sum<.99f)throw new Exception("웨이트 손실: "+vi);
        counts[i]=(byte)ws.Length;
        foreach(var w in ws)weights.Add(new BoneWeight1{boneIndex=bones[int.Parse(w.Attributes["group"].Value)],weight=number(w.Attributes["weight"].Value)/sum});
    }
    var mesh=new Mesh();mesh.name="Female_Face_02";mesh.vertices=positions;mesh.normals=normals;mesh.uv=uv;mesh.subMeshCount=2;
    var triangles=nodes("/head/triangles/v").Select(values).ToArray();
    for(int slot=0;slot<2;slot++)mesh.SetTriangles(triangles.Where(t=>(int)t[0]==slot).SelectMany(t=>new[]{(int)t[3],(int)t[2],(int)t[1]}).ToArray(),slot);
    mesh.bindposes=original.bindposes;
    using(var c=new Unity.Collections.NativeArray<byte>(counts,Unity.Collections.Allocator.Temp))
    using(var w=new Unity.Collections.NativeArray<BoneWeight1>(weights.ToArray(),Unity.Collections.Allocator.Temp))mesh.SetBoneWeights(c,w);
    for(int shape=0;shape<original.blendShapeCount;shape++)
    {
        string name=original.GetBlendShapeName(shape);string key=name.Substring(name.LastIndexOf('.')+1);
        var node=xml.SelectSingleNode("/head/shapes/shape[@name='"+key+"']");if(node==null)throw new Exception("표정 누락: "+name);
        var d=node.SelectNodes("delta/v").Cast<System.Xml.XmlNode>().Select(vector).ToArray();
        var n=node.SelectNodes("normals/v").Cast<System.Xml.XmlNode>().Select(vector).ToArray();
        var dv=loops.Select(l=>d[(int)l[0]]).ToArray();var dn=n.Select((v,i)=>v-normals[i]).ToArray();
        mesh.AddBlendShapeFrame(name,original.GetBlendShapeFrameWeight(shape,0),dv,dn,new Vector3[loops.Length]);
    }
    mesh.RecalculateBounds();mesh.RecalculateTangents();
    string meshPath=folder+"/Female_Face_02.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
    if(saved==null){AssetDatabase.CreateAsset(mesh,meshPath);saved=mesh;}
    else
    {
        EditorUtility.CopySerialized(mesh,saved);
        using(var c=new Unity.Collections.NativeArray<byte>(counts,Unity.Collections.Allocator.Temp))
        using(var w=new Unity.Collections.NativeArray<BoneWeight1>(weights.ToArray(),Unity.Collections.Allocator.Temp))saved.SetBoneWeights(c,w);
        UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);AssetDatabase.SaveAssetIfDirty(saved);
    }
    string texturePath="Assets/04.Images/Characters/DaonPortrait/Daon_Head_Albedo.png";
    var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.maxTextureSize=2048;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Trilinear;
    var platform=importer.GetDefaultPlatformTextureSettings();platform.format=TextureImporterFormat.Automatic;platform.maxTextureSize=2048;importer.SetPlatformTextureSettings(platform);
    var windows=importer.GetPlatformTextureSettings("Standalone");windows.overridden=true;windows.format=TextureImporterFormat.BC7;windows.maxTextureSize=2048;importer.SetPlatformTextureSettings(windows);importer.SaveAndReimport();
    var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
    if(material==null){material=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/Actor/ScenarioResidents/ToonLook/DaonFace/Daon_Face_Skin.mat"));AssetDatabase.CreateAsset(material,matPath);}
    foreach(string property in material.GetTexturePropertyNames())material.SetTexture(property,null);
    var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
    foreach(string property in new[]{"_MainTex","_BaseMap","_BaseColorMap","_OutlineTex"})if(material.HasProperty(property))material.SetTexture(property,texture);
    material.SetColor("_Color",Color.white);material.SetFloat("_UseBumpMap",0);material.SetFloat("_UseMain2ndTex",0);material.SetFloat("_UseMain3rdTex",0);
    material.SetFloat("_OutlineWidth",0);EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);
    string eyeTexturePath="Assets/04.Images/Characters/DaonPortrait/Daon_Eye_Amber.png";
    var eyeImporter=(TextureImporter)AssetImporter.GetAtPath(eyeTexturePath);eyeImporter.maxTextureSize=512;eyeImporter.mipmapEnabled=true;eyeImporter.sRGBTexture=true;eyeImporter.wrapMode=TextureWrapMode.Clamp;
    var eyePlatform=eyeImporter.GetPlatformTextureSettings("Standalone");eyePlatform.overridden=true;eyePlatform.maxTextureSize=512;eyePlatform.format=TextureImporterFormat.BC7;eyeImporter.SetPlatformTextureSettings(eyePlatform);eyeImporter.SaveAndReimport();
    string eyePath="Assets/10.Datas/Actor/ScenarioResidents/ToonLook/DaonPortrait/Daon_Eye_Amber.mat";
    var eye=AssetDatabase.LoadAssetAtPath<Material>(eyePath);
    if(eye==null){eye=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/Actor/ScenarioResidents/ToonLook/DaonFace/Daon_Eye_Hazel.mat"));AssetDatabase.CreateAsset(eye,eyePath);}
    eye.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(eyeTexturePath));eye.SetVector("_MainTexHSVG",new Vector4(0,.85f,1,1));eye.SetFloat("_UseReflection",0);eye.SetFloat("_UseEmission",0);
    EditorUtility.SetDirty(eye);AssetDatabase.SaveAssetIfDirty(eye);
    face.sharedMesh=saved;var slots=face.sharedMaterials;slots[0]=material;slots[1]=eye;face.sharedMaterials=slots;
    PrefabUtility.SaveAsPrefabAsset(root,review+"/Npc_Daon_Portrait.prefab");
    return new{vertices=saved.vertexCount,triangles=saved.triangles.Length/3,shapes=saved.blendShapeCount,meshGuid=AssetDatabase.AssetPathToGUID(meshPath),materialGuid=AssetDatabase.AssetPathToGUID(matPath)};
}
finally{PrefabUtility.UnloadPrefabContents(root);}
