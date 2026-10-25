var output="output/ReineP09";System.IO.Directory.CreateDirectory(output);
var motion=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/Npc_Female_Idle.asset");
var helper=typeof(UPlayGround.Editor.P09Builder.PrefabBuildPipeline).Assembly.GetType("UPlayGround.Editor.P09Builder.P09PosePreview");
var files=new System.Collections.Generic.List<string>();
foreach(bool after in new[]{true})
foreach(float headYaw in new[]{-35f,35f})
{
    float yaw=0;var preview=new UnityEditor.PreviewRenderUtility();System.IDisposable pose=null;
    try
    {
        var source=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(after?"Assets/03.Prefabs/Characters/ExternalModels/Reine_P09Compatible.prefab":"Assets/ExternalAssets/Character/Spi_Reien/CharacterPrefab/reien_original_pf.prefab");
        var instance=UnityEngine.Object.Instantiate(source);preview.AddSingleGO(instance);
        instance.transform.rotation=UnityEngine.Quaternion.Euler(0,yaw,0);
        pose=(System.IDisposable)System.Activator.CreateInstance(helper,new object[]{instance});
        helper.GetMethod("Sample").Invoke(pose,new object[]{motion,0.35f,null});
        var headBone=instance.GetComponent<UnityEngine.Animator>().GetBoneTransform(UnityEngine.HumanBodyBones.Head);
        var hair=instance.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>().First(r=>r.name.StartsWith("Hair_"));
        var rotation=UnityEngine.Quaternion.AngleAxis(headYaw,UnityEngine.Vector3.up);
        var expected=rotation*hair.rootBone.rotation;headBone.rotation=rotation*headBone.rotation;
        foreach(var constraint in instance.GetComponentsInChildren<UnityEngine.Animations.ParentConstraint>(true))helper.GetMethod("EvaluateConstraint",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{constraint});
        float error=UnityEngine.Quaternion.Angle(expected,hair.rootBone.rotation);
        if(error>.5f)throw new System.Exception("헤어 회전 추종 오차: "+error);
        foreach(var skin in instance.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true)){
            var filter=skin.GetComponentsInChildren<UnityEngine.MeshFilter>(true).FirstOrDefault(f=>f.name=="P09 Pose Preview"&&f.transform.parent==skin.transform);
            if(filter!=null)skin.BakeMesh(filter.sharedMesh);
        }
        foreach(var filter in instance.GetComponentsInChildren<UnityEngine.MeshFilter>())if(filter.name=="P09 Pose Preview")filter.sharedMesh.RecalculateBounds();
        var head=instance.GetComponent<UnityEngine.Animator>().GetBoneTransform(UnityEngine.HumanBodyBones.Head).position;
        var camera=preview.camera;camera.fieldOfView=10;var target=head+new UnityEngine.Vector3(0,.07f,0);
        camera.transform.position=target+UnityEngine.Vector3.forward*3.1f;camera.transform.LookAt(target);
        camera.nearClipPlane=.01f;camera.farClipPlane=20;camera.clearFlags=UnityEngine.CameraClearFlags.SolidColor;camera.backgroundColor=new UnityEngine.Color(.18f,.19f,.21f);
        preview.ambientColor=new UnityEngine.Color(.55f,.55f,.55f);preview.lights[0].intensity=1.2f;preview.lights[0].transform.rotation=UnityEngine.Quaternion.Euler(25,150,0);preview.lights[1].intensity=.65f;
        preview.BeginStaticPreview(new UnityEngine.Rect(0,0,768,1024));preview.Render(true);var tex=preview.EndStaticPreview();
        var file=output+"/"+"HeadTurn"+"_"+headYaw+".png";System.IO.File.WriteAllBytes(file,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);files.Add(file);
    }
    finally{if(pose!=null)pose.Dispose();preview.Cleanup();}
}
return files;



