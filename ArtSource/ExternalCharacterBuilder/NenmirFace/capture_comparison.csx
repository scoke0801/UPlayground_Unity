var output="output/NenmirP09";System.IO.Directory.CreateDirectory(output);
var motion=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/Npc_Female_Idle.asset");
var helper=typeof(UPlayGround.Editor.P09Builder.PrefabBuildPipeline).Assembly.GetType("UPlayGround.Editor.P09Builder.P09PosePreview");
var files=new System.Collections.Generic.List<string>();var previewMeshes=new List<Mesh>();
foreach(bool after in new[]{false,true})
foreach(float yaw in new[]{0f,35f,90f})
{
    var preview=new UnityEditor.PreviewRenderUtility();System.IDisposable pose=null;
    try
    {
        var source=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(after?"Assets/03.Prefabs/Characters/ExternalModels/Nenmir_P09Compatible.prefab":"Assets/ExternalAssets/Character/Nenmir/Prefab/Nenmir_Faceonly.prefab");
                source=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/03.Prefabs/Characters/ExternalModels/Nenmir_P09Compatible.prefab");
        var instance=UnityEngine.Object.Instantiate(source);preview.AddSingleGO(instance);
        if(!after){
        var skin=instance.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name.StartsWith("Female_Face_"));
        var copy=UnityEngine.Object.Instantiate(skin.sharedMesh);var points=copy.vertices;
        using(var reader=new System.IO.BinaryReader(System.IO.File.OpenRead("ArtSource/ExternalCharacterBuilder/NenmirFace/source.bin"))){int count=reader.ReadInt32();reader.ReadInt32();reader.ReadInt32();for(int i=0;i<count;i++)points[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());}
        copy.vertices=points;copy.RecalculateNormals();copy.RecalculateTangents();copy.RecalculateBounds();skin.sharedMesh=copy;
        previewMeshes.Add(copy);
        }
        instance.transform.rotation=UnityEngine.Quaternion.Euler(0,yaw,0);
        pose=(System.IDisposable)System.Activator.CreateInstance(helper,new object[]{instance});
        helper.GetMethod("Sample").Invoke(pose,new object[]{motion,0.35f,null});
        foreach(var filter in instance.GetComponentsInChildren<UnityEngine.MeshFilter>())if(filter.name=="P09 Pose Preview")filter.sharedMesh.RecalculateBounds();
        var head=instance.GetComponent<UnityEngine.Animator>().GetBoneTransform(UnityEngine.HumanBodyBones.Head).position;
        var camera=preview.camera;camera.fieldOfView=10;var target=head+new UnityEngine.Vector3(0,.07f,0);
        camera.transform.position=target+UnityEngine.Vector3.forward*3.1f;camera.transform.LookAt(target);
        camera.nearClipPlane=.01f;camera.farClipPlane=20;camera.clearFlags=UnityEngine.CameraClearFlags.SolidColor;camera.backgroundColor=new UnityEngine.Color(.18f,.19f,.21f);
        preview.ambientColor=new UnityEngine.Color(.55f,.55f,.55f);preview.lights[0].intensity=1.2f;preview.lights[0].transform.rotation=UnityEngine.Quaternion.Euler(25,150,0);preview.lights[1].intensity=.65f;
        preview.BeginStaticPreview(new UnityEngine.Rect(0,0,768,1024));preview.Render(true);var tex=preview.EndStaticPreview();
        var file=output+"/"+(after?"Compare_After":"Compare_Before")+"_"+yaw+".png";System.IO.File.WriteAllBytes(file,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);files.Add(file);
    }
    finally{if(pose!=null)pose.Dispose();preview.Cleanup();}
}
foreach(var mesh in previewMeshes)UnityEngine.Object.DestroyImmediate(mesh);return files;




