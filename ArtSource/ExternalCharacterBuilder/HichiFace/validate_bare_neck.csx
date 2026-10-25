// 회전·표정 상태에서 새 연결면 양 끝이 얼굴 및 몸통과 함께 움직이는지 검사한다.
var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Characters/ExternalModels/Hichi_P09Compatible.prefab"));
var helper=typeof(UPlayGround.Editor.P09Builder.PrefabBuildPipeline).Assembly.GetType("UPlayGround.Editor.P09Builder.P09PosePreview");
var pose=(IDisposable)Activator.CreateInstance(helper,new object[]{root});var baked=new Mesh();var bodyBaked=new Mesh();
int samples=0;float topGap=0,bottomGap=0;
try{
    var motion=AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/Npc_Female_Idle.asset");helper.GetMethod("Sample").Invoke(pose,new object[]{motion,.35f,null});
    var renderers=root.GetComponentsInChildren<SkinnedMeshRenderer>(true);var face=renderers.First(r=>r.gameObject.activeInHierarchy&&r.name.StartsWith("Female_Face_"));var body=renderers.Single(r=>r.name=="Female_Body_Chest");
    var v=face.sharedMesh.vertices;var bv=body.sharedMesh.vertices;
    var top=new[]{0,2,8,33,44,52,910,1009,1019,1023,1133,1127,1113,1105,1057,1052,1033,1035,1043,112,111,17};
    var center=top.Aggregate(Vector3.zero,(sum,i)=>sum+v[i])/top.Length;top=top.OrderBy(i=>Mathf.Atan2(v[i].y-center.y,v[i].x-center.x)).ToArray();
    var bottom=Enumerable.Range(9774,16).Select(i=>Enumerable.Range(0,bv.Length).OrderBy(j=>(v[i]-bv[j]).sqrMagnitude).First()).ToArray();
    var head=root.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head);var neutral=head.localRotation;
    foreach(var turn in new[]{Vector2.zero,new Vector2(-35,0),new Vector2(35,0),new Vector2(0,-20),new Vector2(0,20)}){
        head.localRotation=neutral*Quaternion.Euler(turn.y,turn.x,0);body.BakeMesh(bodyBaked);var bodyVertices=bodyBaked.vertices;
        for(int s=0;s<face.sharedMesh.blendShapeCount;s++){
            face.SetBlendShapeWeight(s,100);face.BakeMesh(baked);var vertices=baked.vertices;
            for(int j=0;j<22;j++)topGap=Mathf.Max(topGap,Vector3.Distance(vertices[9856+j],vertices[top[j]]));
            for(int j=0;j<16;j++)bottomGap=Mathf.Max(bottomGap,Vector3.Distance(face.transform.TransformPoint(vertices[9774+j]),body.transform.TransformPoint(bodyVertices[bottom[j]])));
            face.SetBlendShapeWeight(s,0);samples++;
        }
    }
    var result="표정·고개 회전 "+samples+" 표본 / 위쪽 틈="+topGap+"m / 아래쪽 틈="+bottomGap+"m";
    System.IO.File.WriteAllText("output/HichiBareNeckFix/seam_validation.txt",result);
    if(topGap>.00001f||bottomGap>.00001f)throw new Exception(result);return result;
}finally{pose.Dispose();UnityEngine.Object.DestroyImmediate(baked);UnityEngine.Object.DestroyImmediate(bodyBaked);UnityEngine.Object.DestroyImmediate(root);}
