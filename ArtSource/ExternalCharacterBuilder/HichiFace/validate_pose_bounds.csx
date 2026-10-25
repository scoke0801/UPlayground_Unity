var root=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/03.Prefabs/Characters/ExternalModels/Hichi_P09Compatible.prefab"));
var helper=typeof(UPlayGround.Editor.P09Builder.PrefabBuildPipeline).Assembly.GetType("UPlayGround.Editor.P09Builder.P09PosePreview");
var pose=(System.IDisposable)System.Activator.CreateInstance(helper,new object[]{root});
var baked=new UnityEngine.Mesh();int samples=0;int outside=0;var lines=new System.Collections.Generic.List<string>();
try
{
    var face=root.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>().Single(r=>r.name.StartsWith("Female_Face_"));
    foreach(string name in new[]{"Npc_Female_Idle","Npc_Walk"})
    {
        var motion=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/"+name+".asset");
        foreach(float time in new[]{.2f,.5f,.8f})
        {
            helper.GetMethod("Sample").Invoke(pose,new object[]{motion,time,null});face.BakeMesh(baked);
            var bounds=face.localBounds;var matrix=face.rootBone.worldToLocalMatrix*face.transform.localToWorldMatrix;
            int failed=baked.vertices.Count(v=>!bounds.Contains(matrix.MultiplyPoint3x4(v)));
            outside+=failed;samples++;lines.Add(name+" @ "+time+": bounds outside="+failed);
        }
    }
}
finally{pose.Dispose();UnityEngine.Object.DestroyImmediate(baked);UnityEngine.Object.DestroyImmediate(root);}
System.IO.File.WriteAllLines("output/HichiP09/pose_bounds.txt",lines.ToArray());
if(outside>0)throw new System.Exception("얼굴 컬링 bounds 이탈 "+outside);
return new{samples=samples,outside=outside};
