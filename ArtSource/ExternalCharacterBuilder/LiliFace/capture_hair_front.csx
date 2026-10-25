var output="output/LiliP09FrontFit/LiveSkin";System.IO.Directory.CreateDirectory(output);
var motion=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/Npc_Female_Idle.asset");
var helper=typeof(UPlayGround.Editor.P09Builder.PrefabBuildPipeline).Assembly.GetType("UPlayGround.Editor.P09Builder.P09PosePreview");
var files=new System.Collections.Generic.List<string>();
foreach(string modelName in new[]{"Lili_P09Compatible","Lili_P09_Alternate"})
foreach(float yaw in new[]{-50f,50f,150f})
{
    var preview=new UnityEditor.PreviewRenderUtility();System.IDisposable pose=null;
    try
    {
        var source=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/03.Prefabs/Characters/ExternalModels/"+modelName+".prefab");
        var instance=UnityEngine.Object.Instantiate(source);preview.AddSingleGO(instance);
        instance.transform.rotation=UnityEngine.Quaternion.Euler(0,yaw,0);
        pose=(System.IDisposable)System.Activator.CreateInstance(helper,new object[]{instance});
        helper.GetMethod("Sample").Invoke(pose,new object[]{motion,0.35f,null});
        pose.Dispose();pose=null;
 foreach(var filter in instance.GetComponentsInChildren<UnityEngine.MeshFilter>())if(filter.name=="P09 Pose Preview")filter.sharedMesh.RecalculateBounds();
        var head=instance.GetComponent<UnityEngine.Animator>().GetBoneTransform(UnityEngine.HumanBodyBones.Head).position;
        var camera=preview.camera;camera.fieldOfView=8;var target=head+new UnityEngine.Vector3(0,.10f,0);
        camera.transform.position=target+new UnityEngine.Vector3(0,yaw==150f?.25f:-1f,1.65f);camera.transform.LookAt(target);
        camera.nearClipPlane=.01f;camera.farClipPlane=20;camera.clearFlags=UnityEngine.CameraClearFlags.SolidColor;camera.backgroundColor=new UnityEngine.Color(.18f,.19f,.21f);
        preview.ambientColor=new UnityEngine.Color(.55f,.55f,.55f);preview.lights[0].intensity=1.2f;preview.lights[0].transform.rotation=UnityEngine.Quaternion.Euler(25,150,0);preview.lights[1].intensity=.65f;
        preview.BeginStaticPreview(new UnityEngine.Rect(0,0,768,768));preview.Render(true);var tex=preview.EndStaticPreview();
        var file=output+"/"+modelName+"_"+yaw+".png";System.IO.File.WriteAllBytes(file,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);files.Add(file);
    }
    finally{if(pose!=null)pose.Dispose();preview.Cleanup();}
}
return files;
