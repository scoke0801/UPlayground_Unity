var output="output/LianLianP09";System.IO.Directory.CreateDirectory(output);
var motion=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/Npc_Female_Idle.asset");
var helper=typeof(UPlayGround.Editor.P09Builder.PrefabBuildPipeline).Assembly.GetType("UPlayGround.Editor.P09Builder.P09PosePreview");
var files=new System.Collections.Generic.List<string>();
foreach(bool after in new[]{false,true})
foreach(float yaw in new[]{0f})
{
    var preview=new UnityEditor.PreviewRenderUtility();System.IDisposable pose=null; Mesh comparisonMesh=null;
    try
    {
        var source=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/03.Prefabs/Characters/ExternalModels/LianLian_P09Compatible.prefab");
        var instance=UnityEngine.Object.Instantiate(source);preview.AddSingleGO(instance);
        instance.transform.rotation=UnityEngine.Quaternion.Euler(0,yaw,0);
        if(!after){
            Vector3[] original;
            using(var reader=new System.IO.BinaryReader(System.IO.File.OpenRead("ArtSource/ExternalCharacterBuilder/LianLianFace/source.bin"))){int n=reader.ReadInt32();reader.ReadInt32();reader.ReadInt32();original=new Vector3[n];for(int i=0;i<n;i++)original[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());}
            int[] triangles,bridge;
            using(var reader=new System.IO.BinaryReader(System.IO.File.OpenRead("ArtSource/ExternalCharacterBuilder/LianLianFace/topology.bin"))){Func<int[]> read=()=>Enumerable.Range(0,reader.ReadInt32()).Select(_=>reader.ReadInt32()).ToArray();triangles=read();read();bridge=read();}
            var ids=triangles.Distinct().OrderBy(i=>i).Concat(bridge.Where(i=>i>=0).Distinct()).ToArray();
            var face=instance.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name.StartsWith("Female_Face_"));
            var mesh=UnityEngine.Object.Instantiate(face.sharedMesh);mesh.name="조형 전 비교용"; comparisonMesh=mesh;
            var points=mesh.vertices;for(int i=0;i<ids.Length;i++)points[i]=original[ids[i]];
            mesh.ClearBlendShapes();mesh.vertices=points;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();face.sharedMesh=mesh;
        }
        pose=(System.IDisposable)System.Activator.CreateInstance(helper,new object[]{instance});
        helper.GetMethod("Sample").Invoke(pose,new object[]{motion,0.35f,null});
        foreach(var filter in instance.GetComponentsInChildren<UnityEngine.MeshFilter>())if(filter.name=="P09 Pose Preview")filter.sharedMesh.RecalculateBounds();
        var head=instance.GetComponent<UnityEngine.Animator>().GetBoneTransform(UnityEngine.HumanBodyBones.Head).position;
        var camera=preview.camera;camera.fieldOfView=6;var target=head+new UnityEngine.Vector3(0,.07f,0);
        camera.transform.position=target+UnityEngine.Vector3.forward*3.1f;camera.transform.LookAt(target);
        camera.nearClipPlane=.01f;camera.farClipPlane=20;camera.clearFlags=UnityEngine.CameraClearFlags.SolidColor;camera.backgroundColor=new UnityEngine.Color(.18f,.19f,.21f);
        preview.ambientColor=new UnityEngine.Color(.55f,.55f,.55f);preview.lights[0].intensity=1.2f;preview.lights[0].transform.rotation=UnityEngine.Quaternion.Euler(25,150,0);preview.lights[1].intensity=.65f;
        preview.BeginStaticPreview(new UnityEngine.Rect(0,0,768,1024));preview.Render(true);var tex=preview.EndStaticPreview();
        var file=output+"/"+(after?"Compare_After":"Compare_Before")+"_"+yaw+".png";System.IO.File.WriteAllBytes(file,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);files.Add(file);
    }
    finally{if(pose!=null)pose.Dispose();preview.Cleanup();if(comparisonMesh!=null)UnityEngine.Object.DestroyImmediate(comparisonMesh);}
}
return files;




