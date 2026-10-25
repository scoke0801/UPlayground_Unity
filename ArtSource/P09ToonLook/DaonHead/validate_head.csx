// 정적 무결성·표정 BakeMesh와 고정 포즈 캡처. 실제 대화 플레이 검증과 구분한다.
string path="Assets/03.Prefabs/Characters/EquipmentReview/DaonHead/Npc_Daon_Head.prefab";
string output="output/P09DaonHead";
var report=new List<string>();
var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Characters/ScenarioResidents/Npc/Npc_Daon/Npc_Daon.prefab");
if(prefab.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))!=0)throw new Exception("Missing Script");
foreach(string name in new[]{"Female_Face_02","Hair_03"})
{
    var original=source.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name==name);
    var current=prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name==name);
    if(name=="Hair_03"&&!original.sharedMaterials.SequenceEqual(current.sharedMaterials))throw new Exception("재질 변경: "+name);
    if(!original.bones.Select(b=>b.name).SequenceEqual(current.bones.Select(b=>b.name)))throw new Exception("본 변경: "+name);
    if(!original.sharedMesh.bindposes.SequenceEqual(current.sharedMesh.bindposes))throw new Exception("바인드 포즈 변경: "+name);
    var mesh=current.sharedMesh;
    var counts=mesh.GetBonesPerVertex();var weights=mesh.GetAllBoneWeights();int offset=0;float maxSumError=0;
    for(int i=0;i<counts.Length;i++)
    {
        float sum=0;
        for(int j=0;j<counts[i];j++)
        {
            var w=weights[offset++];sum+=w.weight;
            if(w.weight<0||w.boneIndex<0||w.boneIndex>=current.bones.Length)throw new Exception("잘못된 웨이트");
        }
        maxSumError=Mathf.Max(maxSumError,Mathf.Abs(sum-1));
    }
    if(maxSumError>.0001f)throw new Exception("웨이트 정규화 실패");
    if(mesh.triangles.Any(i=>i<0||i>=mesh.vertexCount))throw new Exception("잘못된 삼각형 인덱스");
    report.Add(name+": 본/바인드 포즈 보존, 웨이트 합 최대 오차 "+maxSumError);
}
var root=UnityEngine.Object.Instantiate(prefab);
try
{
    root.SetActive(true);var face=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Female_Face_02");
    var original=source.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Female_Face_02").sharedMesh;
    if(face.sharedMesh.blendShapeCount!=original.blendShapeCount)throw new Exception("표정 개수 변경");
    var baked=new Mesh();
    try
    {
        for(int i=0;i<face.sharedMesh.blendShapeCount;i++)
        {
            if(face.sharedMesh.GetBlendShapeName(i)!=original.GetBlendShapeName(i))throw new Exception("표정 순서 변경");
            face.SetBlendShapeWeight(i,100);face.BakeMesh(baked);
            if(baked.vertices.Any(v=>float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)||v.sqrMagnitude>100))throw new Exception("표정 변형 오류");
            face.SetBlendShapeWeight(i,0);
        }
    }
    finally{UnityEngine.Object.DestroyImmediate(baked);}
    report.Add("36개 표정의 이름/순서 보존 및 단독 100% BakeMesh 유한 좌표 검사 통과");
}
finally{UnityEngine.Object.DestroyImmediate(root);}
var capture=typeof(UPlayGround.Editor.P09Builder.ScenarioResidentAuthoring).GetMethod("Capture",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
foreach(string pose in new[]{"Eye_Blink","Lip_aa","Mouth_Smile","HeadTurn"})
{
    root=UnityEngine.Object.Instantiate(prefab);
    try
    {
        root.SetActive(true);var face=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Female_Face_02");
        if(pose=="HeadTurn")
        {
            var head=face.bones.Single(b=>b.name=="Head");var before=head.rotation;
            head.localRotation*=Quaternion.Euler(0,30,0);
            // Preview는 ParentConstraint의 플레이 루프를 진행하지 않으므로 같은 회전을 수동 주입한다.
            var hair=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Hair_03");
            var delta=head.rotation*Quaternion.Inverse(before);
            hair.rootBone.position=head.position+delta*(hair.rootBone.position-head.position);
            hair.rootBone.rotation=delta*hair.rootBone.rotation;
        }
        else
        {
            int index=Enumerable.Range(0,face.sharedMesh.blendShapeCount).Single(i=>face.sharedMesh.GetBlendShapeName(i).EndsWith(pose));
            face.SetBlendShapeWeight(index,100);
        }
        capture.Invoke(null,new object[]{root,output+"/Pose_"+pose+".png",true,0f,false,true});
    }
    finally{if(root!=null)UnityEngine.Object.DestroyImmediate(root);}
}
report.Add("Missing Script 0. 눈감기/입벌림/미소 PreviewRenderUtility 캡처 생성. 고개회전은 얼굴과 헤어에 동일 회전 수동 주입; 런타임 ParentConstraint 검증 아님");
System.IO.File.WriteAllLines(output+"/validation_result.txt",report);
return report;
