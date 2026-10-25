var output="output/ExternalCharacterBuilder";
System.IO.Directory.CreateDirectory(output);
var motion=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/Npc_Female_Idle.asset");
var helperType=typeof(UPlayGround.Editor.P09Builder.PrefabBuildPipeline).Assembly.GetType("UPlayGround.Editor.P09Builder.P09PosePreview");
var files=new System.Collections.Generic.List<string>();
foreach(var model in new[]{"Honoka_Traveler","Katana_Traveler"})
foreach(var variant in new[]{0,1})
foreach(var portrait in new[]{false,true})
{
    var preview=new UnityEditor.PreviewRenderUtility();
    System.IDisposable pose=null;
    try
    {
        var source=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/03.Prefabs/Characters/ExternalModels/"+model+"_v2.prefab");
        var instance=UnityEngine.Object.Instantiate(source);
        preview.AddSingleGO(instance);
        instance.transform.rotation=UnityEngine.Quaternion.Euler(0,portrait?0:20,0);
        var visuals=instance.GetComponent<UPlayGround.Components.CharacterEquipmentVisuals>();
        var slot=visuals.Slots[0];
        if(!visuals.TryEquip(slot.id,slot.variants[variant].id)) throw new System.Exception("장비 전환 실패");
        pose=(System.IDisposable)System.Activator.CreateInstance(helperType,new object[]{instance});
        helperType.GetMethod("Sample").Invoke(pose,new object[]{motion,0.35f,null});
        foreach(var filter in instance.GetComponentsInChildren<UnityEngine.MeshFilter>())
            if(filter.gameObject.name=="P09 Pose Preview") filter.sharedMesh.RecalculateBounds();
        var animator=instance.GetComponent<UnityEngine.Animator>();
        var head=animator.GetBoneTransform(UnityEngine.HumanBodyBones.Head).position;
        var camera=preview.camera;
        camera.fieldOfView=portrait?10:28;
        var target=portrait ? head+new UnityEngine.Vector3(0,0.07f,0) : new UnityEngine.Vector3(0,0.83f,0);
        camera.transform.position=target+UnityEngine.Vector3.forward*3.1f;
        camera.transform.LookAt(target);
        camera.nearClipPlane=0.01f;
        camera.farClipPlane=20;
        camera.clearFlags=UnityEngine.CameraClearFlags.SolidColor;
        camera.backgroundColor=new UnityEngine.Color(0.18f,0.19f,0.21f);
        preview.ambientColor=new UnityEngine.Color(0.5f,0.5f,0.5f);
        preview.lights[0].intensity=1;
        preview.lights[0].transform.rotation=UnityEngine.Quaternion.Euler(35,150,0);
        preview.lights[1].intensity=0.65f;
        preview.BeginStaticPreview(new UnityEngine.Rect(0,0,768,1024));
        preview.Render(true);
        var tex=preview.EndStaticPreview();
        var path=output+"/"+model+"_"+variant+(portrait?"_Face":"_Full")+".png";
        System.IO.File.WriteAllBytes(path,tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        files.Add(path);
    }
    finally { if(pose!=null) pose.Dispose(); preview.Cleanup(); }
}
return files;
