var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Characters/ExternalModels/Hichi_P09Compatible.prefab"));
var helper=typeof(UPlayGround.Editor.P09Builder.PrefabBuildPipeline).Assembly.GetType("UPlayGround.Editor.P09Builder.P09PosePreview");
var pose=(IDisposable)Activator.CreateInstance(helper,new object[]{root});var baked=new Mesh();
int samples=0;float maximum=0;
try{
    var motion=AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/Npc_Female_Idle.asset");helper.GetMethod("Sample").Invoke(pose,new object[]{motion,.35f,null});
    var face=root.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name.StartsWith("Female_Face_"));
    var origins=System.IO.File.ReadAllText("output/HichiP09/neck_origins.txt").Split(',').Select(int.Parse).ToArray();
    if(origins.Length!=face.sharedMesh.vertexCount)throw new Exception("목 대응 데이터 불일치");
    var head=root.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head);var neutral=head.rotation;
    foreach(float yaw in new[]{-35f,0f,35f}){
        head.rotation=Quaternion.AngleAxis(yaw,Vector3.up)*neutral;
        for(int s=0;s<face.sharedMesh.blendShapeCount;s++){
            face.SetBlendShapeWeight(s,100);face.BakeMesh(baked);var vertices=baked.vertices;
            for(int i=9774;i<origins.Length;i++)if(origins[i]>=0)maximum=Mathf.Max(maximum,Vector3.Distance(vertices[i],vertices[origins[i]]));
            face.SetBlendShapeWeight(s,0);samples++;
        }
    }
    if(maximum>.00001f)throw new Exception("목·얼굴 이음새 이탈: "+maximum);
    var result="목 회전 3종 × 표정 36종 = "+samples+" 표본. 상단 이음새 최대 오차="+maximum+"m";
    System.IO.File.WriteAllText("output/HichiP09/neck_validation.txt",result);return result;
}finally{pose.Dispose();UnityEngine.Object.DestroyImmediate(baked);UnityEngine.Object.DestroyImmediate(root);}
