// Blender 검수본을 원본의 본·재질·표정 이름을 유지한 독립 메시로 만든다.
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("편집 모드가 필요합니다.");
string canonical="Assets/03.Prefabs/Characters/ScenarioResidents/Npc/Npc_Daon/Npc_Daon.prefab";
string folder="Assets/08.Models/Characters/DaonSculpt";
string review="Assets/03.Prefabs/Characters/EquipmentReview/DaonSculpt";
string output="output/P09DaonSculpt";
System.IO.Directory.CreateDirectory(folder);System.IO.Directory.CreateDirectory(review);System.IO.Directory.CreateDirectory(output);
AssetDatabase.Refresh();
var xml=new System.Xml.XmlDocument();xml.Load("ArtSource/P09ToonLook/DaonSculpt/sculpt.xml");
Func<string,float> number=s=>float.Parse(s,System.Globalization.CultureInfo.InvariantCulture);
Func<System.Xml.XmlNode,float[]> values=n=>n.InnerText.Split(' ').Select(number).ToArray();
Func<System.Xml.XmlNode,Vector3> vector=n=>{var v=values(n);return new Vector3(-v[0],v[1],v[2]);};
Func<string,System.Xml.XmlNode[]> nodes=path=>xml.SelectNodes(path).Cast<System.Xml.XmlNode>().ToArray();
var root=PrefabUtility.LoadPrefabContents(canonical);
var report=new List<string>();
try
{
    var face=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Female_Face_02");
    var hair=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Hair_03");
    var original=AssetDatabase.LoadAllAssetsAtPath("Assets/ExternalAssets/Character/P09_Modular_Humanoid/Model_DATA/FBX/P09_250802.fbx").OfType<Mesh>().Single(m=>m.name=="Female_Face_02");
    var source=nodes("/sculpt/face/original/v").Select(vector).ToArray();
    var target=nodes("/sculpt/face/vertices/v").Select(vector).ToArray();
    var vertices=original.vertices;
    var indices=new int[vertices.Length];float maxError=0;
    for(int i=0;i<vertices.Length;i++)
    {
        float best=float.MaxValue;int match=-1;
        for(int j=0;j<source.Length;j++){float d=(vertices[i]-source[j]).sqrMagnitude;if(d<best){best=d;match=j;}}
        if(best>0.00000001f)throw new Exception("원본 정점 대응 실패: "+i+" / "+Mathf.Sqrt(best));
        indices[i]=match;maxError=Mathf.Max(maxError,Mathf.Sqrt(best));vertices[i]=target[match];
    }
    var faceMesh=UnityEngine.Object.Instantiate(original);faceMesh.name="Female_Face_02";faceMesh.vertices=vertices;
    faceMesh.ClearBlendShapes();
    for(int shape=0;shape<original.blendShapeCount;shape++)
    {
        string name=original.GetBlendShapeName(shape);
        string key=name.Contains(".")?name.Substring(name.LastIndexOf('.')+1):name;
        var shapeNode=xml.SelectSingleNode("/sculpt/face/shapes/shape[@name='"+key+"']");
        if(shapeNode==null)throw new Exception("표정 누락: "+name);
        if(original.GetBlendShapeFrameCount(shape)!=1)throw new Exception("복수 표정 프레임은 별도 검토가 필요합니다.");
        var positions=shapeNode.ChildNodes.Cast<System.Xml.XmlNode>().Select(vector).ToArray();
        var delta=new Vector3[vertices.Length];var normals=new Vector3[vertices.Length];var tangents=new Vector3[vertices.Length];
        original.GetBlendShapeFrameVertices(shape,0,delta,normals,tangents);
        for(int i=0;i<delta.Length;i++)delta[i]=positions[indices[i]]-vertices[i];
        faceMesh.AddBlendShapeFrame(name,original.GetBlendShapeFrameWeight(shape,0),delta,normals,tangents);
    }
    faceMesh.RecalculateBounds();
    var points=nodes("/sculpt/hair/vertices/v").Select(vector).ToArray();
    var groups=nodes("/sculpt/hair/groups/v").Select(x=>x.InnerText).ToArray();
    var boneIndices=groups.Select(n=>Array.FindIndex(hair.bones,b=>b.name==n)).ToArray();
    if(boneIndices.Any(i=>i<0))throw new Exception("헤어 본 매핑 누락");
    var loops=nodes("/sculpt/hair/loops/v").Select(values).ToArray();
    var sourceWeights=nodes("/sculpt/hair/weights/v");
    var hv=new Vector3[loops.Length];var hn=new Vector3[loops.Length];var uv=new Vector2[loops.Length];
    var boneCounts=new byte[loops.Length];var weights=new List<BoneWeight1>();
    for(int i=0;i<loops.Length;i++)
    {
        var l=loops[i];int vi=(int)l[0];hv[i]=points[vi];uv[i]=new Vector2((float)l[1],(float)l[2]);
        hn[i]=new Vector3(-(float)l[3],(float)l[4],(float)l[5]);
        var ws=sourceWeights[vi].ChildNodes.Cast<System.Xml.XmlNode>().OrderByDescending(w=>number(w.Attributes["weight"].Value)).ToArray();float sum=ws.Sum(w=>number(w.Attributes["weight"].Value));
        if(sum<.99f)throw new Exception("헤어 웨이트 손실: "+vi+" / "+sum);
        boneCounts[i]=(byte)ws.Length;
        foreach(var w in ws)weights.Add(new BoneWeight1{boneIndex=boneIndices[int.Parse(w.Attributes["group"].Value)],weight=number(w.Attributes["weight"].Value)/sum});
    }
    var triangles=nodes("/sculpt/hair/triangles/v").Select(values).SelectMany(t=>new[]{(int)t[2],(int)t[1],(int)t[0]}).ToArray();
    var hairMesh=new Mesh();hairMesh.name="Hair_03";hairMesh.vertices=hv;hairMesh.normals=hn;hairMesh.uv=uv;
    hairMesh.triangles=triangles;hairMesh.bindposes=hair.sharedMesh.bindposes;
    using(var counts=new Unity.Collections.NativeArray<byte>(boneCounts,Unity.Collections.Allocator.Temp))
    using(var allWeights=new Unity.Collections.NativeArray<BoneWeight1>(weights.ToArray(),Unity.Collections.Allocator.Temp))hairMesh.SetBoneWeights(counts,allWeights);
    hairMesh.RecalculateBounds();hairMesh.RecalculateTangents();
    foreach(var mesh in new[]{faceMesh,hairMesh})
    {
        string path=folder+"/"+mesh.name+".asset";
        var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(saved==null)AssetDatabase.CreateAsset(mesh,path);
        else
        {
            EditorUtility.CopySerialized(mesh,saved);
            // CopySerialized만으로는 스킨 가중치의 GPU 버퍼가 갱신되지 않는다.
            using(var counts=new Unity.Collections.NativeArray<byte>(mesh.GetBonesPerVertex().ToArray(),Unity.Collections.Allocator.Temp))
            using(var allWeights=new Unity.Collections.NativeArray<BoneWeight1>(mesh.GetAllBoneWeights().ToArray(),Unity.Collections.Allocator.Temp))saved.SetBoneWeights(counts,allWeights);
            UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);AssetDatabase.SaveAssetIfDirty(saved);
        }
    }
    face.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(folder+"/Female_Face_02.asset");
    hair.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(folder+"/Hair_03.asset");
    PrefabUtility.SaveAsPrefabAsset(root,review+"/Npc_Daon_Sculpt.prefab");
    report.Add("원본 정점 최대 대응 오차(m): "+maxError);
    report.Add("얼굴 정점/표정: "+face.sharedMesh.vertexCount+" / "+face.sharedMesh.blendShapeCount);
    report.Add("헤어 정점/삼각형: "+hair.sharedMesh.vertexCount+" / "+hair.sharedMesh.triangles.Length/3);
}
finally{PrefabUtility.UnloadPrefabContents(root);}
var capture=typeof(UPlayGround.Editor.P09Builder.ScenarioResidentAuthoring).GetMethod("Capture",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
foreach(bool after in new[]{false,true})
foreach(float yaw in new[]{0f,35f,90f})
{
    var instance=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(after?review+"/Npc_Daon_Sculpt.prefab":canonical));
    try
    {
        instance.SetActive(true);
        if(!after)
            foreach(var skin in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r=>r.name=="Female_Face_02"||r.name=="Hair_03"))
                skin.sharedMesh=AssetDatabase.LoadAllAssetsAtPath("Assets/ExternalAssets/Character/P09_Modular_Humanoid/Model_DATA/FBX/P09_250802.fbx").OfType<Mesh>().Single(m=>m.name==skin.name);
        capture.Invoke(null,new object[]{instance,output+"/"+(after?"After":"Before")+"_"+yaw+".png",true,yaw,false,true});
    }
    finally{if(instance!=null)UnityEngine.Object.DestroyImmediate(instance);}
}
System.IO.File.WriteAllLines(output+"/import_result.txt",report);
return report;
