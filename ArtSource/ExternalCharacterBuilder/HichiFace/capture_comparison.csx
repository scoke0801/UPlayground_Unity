var output="output/HichiP09";System.IO.Directory.CreateDirectory(output);
var motion=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/Npc_Female_Idle.asset");
var helper=typeof(UPlayGround.Editor.P09Builder.PrefabBuildPipeline).Assembly.GetType("UPlayGround.Editor.P09Builder.P09PosePreview");
var files=new System.Collections.Generic.List<string>();
foreach(bool after in new[]{false,true})
foreach(float yaw in new[]{0f,35f,90f})
{
    var preview=new UnityEditor.PreviewRenderUtility();System.IDisposable pose=null;var copies=new System.Collections.Generic.List<UnityEngine.Mesh>();
    try
    {
        var source=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/03.Prefabs/Characters/ExternalModels/Hichi_P09Compatible.prefab");
        var instance=UnityEngine.Object.Instantiate(source);preview.AddSingleGO(instance);
        
        foreach(var r in instance.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true)) {
            if(r.name.StartsWith("Hair_"))r.gameObject.SetActive(false);
            if(!after && r.name.StartsWith("Female_Face_")){
                var original=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/ExternalAssets/Character/Hichi/Prefab/Hichi.prefab").GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true).Single(s=>s.name=="hichi_face");
                var copy=UnityEngine.Object.Instantiate(r.sharedMesh);var v=copy.vertices;var raw=original.sharedMesh.vertices;
                for(int i=0;i<raw.Length;i++){var p=raw[i];v[i]=new UnityEngine.Vector3(p.x*.94f,-p.z*.94f,1.34228509f+(p.y-1.22f)*.98f);}
                copies.Add(copy);copy.vertices=v;copy.RecalculateNormals();copy.RecalculateTangents();copy.RecalculateBounds();r.sharedMesh=copy;r.sharedMaterials=new[]{original.sharedMaterials[0],original.sharedMaterials[1],r.sharedMaterials[2]};
            }
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
        var file=output+"/"+(after?"Sculpt_After":"Sculpt_Before")+"_"+yaw+".png";System.IO.File.WriteAllBytes(file,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);files.Add(file);
    }
    finally{if(pose!=null)pose.Dispose();preview.Cleanup();foreach(var copy in copies)UnityEngine.Object.DestroyImmediate(copy);}
}
return files;
