var output="output/BokuseiP09";System.IO.Directory.CreateDirectory(output);
var motion=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/Npc_Male_Idle.asset");
var helper=typeof(UPlayGround.Editor.P09Builder.PrefabBuildPipeline).Assembly.GetType("UPlayGround.Editor.P09Builder.P09PosePreview");
var files=new System.Collections.Generic.List<string>();
foreach(bool after in new[]{true})
foreach(float yaw in new[]{0f,35f,90f})
{
    var preview=new UnityEditor.PreviewRenderUtility();System.IDisposable pose=null;
    try
    {
        var source=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(after?"Assets/03.Prefabs/Characters/ExternalModels/Bokusei_P09Compatible.prefab":"Assets/ExternalAssets/Character/Honoka/Prefab/Honoka_alt4.prefab");
        var instance=UnityEngine.Object.Instantiate(source);preview.AddSingleGO(instance);
        instance.transform.rotation=UnityEngine.Quaternion.Euler(0,yaw,0);
        var face=instance.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>().Single(r=>r.name=="Male_Face_01");
        var aligned=UnityEngine.Object.Instantiate(face.sharedMesh);aligned.ClearBlendShapes();
        using(var reader=new System.IO.BinaryReader(System.IO.File.OpenRead("ArtSource/ExternalCharacterBuilder/BokuseiFace/source.bin"))){int count=reader.ReadInt32();reader.ReadInt32();reader.ReadInt32();var points=new UnityEngine.Vector3[count];for(int i=0;i<count;i++)points[i]=new UnityEngine.Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());aligned.vertices=points;}
        aligned.RecalculateNormals();aligned.RecalculateTangents();aligned.RecalculateBounds();face.sharedMesh=aligned;
        face.sharedMaterials=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/ExternalAssets/Character/ROKO SHOP/Bokusei/01_Prefab/Bokusei.prefab").GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true).Single(r=>r.name=="Body").sharedMaterials;
        pose=(System.IDisposable)System.Activator.CreateInstance(helper,new object[]{instance});
        helper.GetMethod("Sample").Invoke(pose,new object[]{motion,0.35f,null});
        foreach(var filter in instance.GetComponentsInChildren<UnityEngine.MeshFilter>())if(filter.name=="P09 Pose Preview")filter.sharedMesh.RecalculateBounds();
        var head=instance.GetComponent<UnityEngine.Animator>().GetBoneTransform(UnityEngine.HumanBodyBones.Head).position;
        var camera=preview.camera;camera.fieldOfView=10;var target=head+new UnityEngine.Vector3(0,.07f,0);
        camera.transform.position=target+UnityEngine.Vector3.forward*3.1f;camera.transform.LookAt(target);
        camera.nearClipPlane=.01f;camera.farClipPlane=20;camera.clearFlags=UnityEngine.CameraClearFlags.SolidColor;camera.backgroundColor=new UnityEngine.Color(.18f,.19f,.21f);
        preview.ambientColor=new UnityEngine.Color(.55f,.55f,.55f);preview.lights[0].intensity=1.2f;preview.lights[0].transform.rotation=UnityEngine.Quaternion.Euler(25,150,0);preview.lights[1].intensity=.65f;
        preview.BeginStaticPreview(new UnityEngine.Rect(0,0,768,1024));preview.Render(true);var tex=preview.EndStaticPreview();
        var file=output+"/"+(after?"SourceAligned":"SourceFace")+"_"+yaw+".png";System.IO.File.WriteAllBytes(file,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);files.Add(file);UnityEngine.Object.DestroyImmediate(aligned);
    }
    finally{if(pose!=null)pose.Dispose();preview.Cleanup();}
}
return files;


