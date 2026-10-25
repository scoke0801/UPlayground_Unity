var folder="Assets/08.Models/Characters/DaonNewIdentity";
System.IO.Directory.CreateDirectory(folder);
UnityEditor.AssetDatabase.Refresh();
var source=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/ExternalAssets/Character/Honoka/Prefab/Honoka_alt4.prefab");
var meshes=new System.Collections.Generic.Dictionary<string,UnityEngine.Mesh>();
var report=new System.Collections.Generic.List<string>();
using(var reader=new System.IO.BinaryReader(System.IO.File.OpenRead("ArtSource/ExternalCharacterBuilder/DaonFace/sculpt.bin")))
{
    Func<string> readName=()=>System.Text.Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
    Func<int,UnityEngine.Vector3[]> readVertices=n=>{var a=new UnityEngine.Vector3[n];for(int i=0;i<n;i++) a[i]=new UnityEngine.Vector3(-reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());return a;};
    int meshCount=reader.ReadInt32();
    for(int part=0;part<meshCount;part++)
    {
        string name=readName();int count=reader.ReadInt32();int shapeCount=reader.ReadInt32();
        var originalPoints=readVertices(count);var targetPoints=readVertices(count);
        var skin=source.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true).Single(r=>r.name==name);
        var original=skin.sharedMesh;
        var vertices=original.vertices;
        var cells=new System.Collections.Generic.Dictionary<UnityEngine.Vector3Int,System.Collections.Generic.List<int>>();
        Func<UnityEngine.Vector3,UnityEngine.Vector3Int> cell=p=>new UnityEngine.Vector3Int(UnityEngine.Mathf.FloorToInt(p.x*10000),UnityEngine.Mathf.FloorToInt(p.y*10000),UnityEngine.Mathf.FloorToInt(p.z*10000));
        for(int i=0;i<count;i++){var key=cell(originalPoints[i]);if(!cells.ContainsKey(key))cells[key]=new System.Collections.Generic.List<int>();cells[key].Add(i);}
        var indices=new int[vertices.Length];float maxError=0;
        for(int i=0;i<vertices.Length;i++)
        {
            var key=cell(vertices[i]);float best=float.MaxValue;int match=-1;
            for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)for(int z=-1;z<=1;z++)
            {
                System.Collections.Generic.List<int> candidates;
                if(!cells.TryGetValue(key+new UnityEngine.Vector3Int(x,y,z),out candidates))continue;
                foreach(int candidate in candidates){float distance=(vertices[i]-originalPoints[candidate]).sqrMagnitude;if(distance<best){best=distance;match=candidate;}}
            }
            if(match<0 || best>4e-10f)throw new System.Exception("정점 대응 실패: "+name+"/"+i+"/"+best);
            indices[i]=match;maxError=UnityEngine.Mathf.Max(maxError,UnityEngine.Mathf.Sqrt(best));vertices[i]=targetPoints[match];
        }
        var mesh=UnityEngine.Object.Instantiate(original);mesh.name="Daon_"+name;mesh.vertices=vertices;mesh.ClearBlendShapes();
        var byName=new System.Collections.Generic.Dictionary<string,UnityEngine.Vector3[]>();
        for(int i=0;i<shapeCount;i++){string shapeName=readName();byName.Add(shapeName,readVertices(count));}
        if(shapeCount!=original.blendShapeCount)throw new System.Exception("표정 개수 불일치: "+name);
        for(int shape=0;shape<original.blendShapeCount;shape++)
        {
            string shapeName=original.GetBlendShapeName(shape);
            if(!byName.ContainsKey(shapeName) || original.GetBlendShapeFrameCount(shape)!=1)throw new System.Exception("표정 매핑 실패: "+shapeName);
            var absolute=byName[shapeName];var delta=new UnityEngine.Vector3[vertices.Length];var normals=new UnityEngine.Vector3[vertices.Length];var tangents=new UnityEngine.Vector3[vertices.Length];
            original.GetBlendShapeFrameVertices(shape,0,delta,normals,tangents);
            for(int i=0;i<delta.Length;i++){var d=absolute[indices[i]]-vertices[i];delta[i]=d.sqrMagnitude<1e-14f?UnityEngine.Vector3.zero:d;}
            mesh.AddBlendShapeFrame(shapeName,original.GetBlendShapeFrameWeight(shape,0),delta,normals,tangents);
        }
        mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
        string path=folder+"/Daon_"+name+".asset";
        if(UnityEditor.AssetDatabase.LoadMainAssetAtPath(path)!=null)throw new System.Exception("기존 조형을 덮어쓰지 않습니다: "+path);
        UnityEditor.AssetDatabase.CreateAsset(mesh,path);meshes.Add(name,mesh);
        report.Add(name+": vertices="+mesh.vertexCount+", expressions="+mesh.blendShapeCount+", maxMatchError="+maxError);
    }
}
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
try
{
    var instance=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(source,scene);
    UnityEditor.PrefabUtility.UnpackPrefabInstance(instance,UnityEditor.PrefabUnpackMode.Completely,UnityEditor.InteractionMode.AutomatedAction);
    instance.name="Daon_NewIdentity_Base";
    foreach(var skin in instance.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true))
        if(meshes.ContainsKey(skin.name))skin.sharedMesh=meshes[skin.name];
    instance.transform.Find("Body/Horn").GetComponent<UnityEngine.Renderer>().enabled=false;
    var basePath=folder+"/Daon_NewIdentity_Base.prefab";
    if(UnityEditor.AssetDatabase.LoadMainAssetAtPath(basePath)!=null)throw new System.Exception("기존 조형 프리팹이 있습니다.");
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(instance,basePath);
}
finally {UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
UnityEditor.AssetDatabase.SaveAssets();
System.IO.File.WriteAllLines("output/DaonFaceRedesign/import_report.txt",report);
return report;
