var meshPath="Assets/08.Models/Characters/DaonNewIdentity/Daon_P09_Head.asset";
var prefabPath="Assets/03.Prefabs/Characters/ScenarioResidents/Npc/Npc_Daon/Npc_Daon.prefab";
var temporary="Assets/__DaonNeckValidation.asset";
if(System.IO.File.Exists(temporary)) throw new System.Exception("검증 임시 경로가 이미 있습니다.");
System.IO.File.Copy("output/DaonNeckFix/Backup/Daon_P09_Head.asset",temporary,false);
UnityEditor.AssetDatabase.ImportAsset(temporary,UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var root=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(prefabPath));
var baked=new UnityEngine.Mesh();
var nativeBaked=new UnityEngine.Mesh();
var nativeSkin=new UnityEngine.GameObject("목 경계 검증").AddComponent<UnityEngine.SkinnedMeshRenderer>();
try
{
    var before=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(temporary);
    var mesh=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(meshPath);
    var skin=root.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true).Single(r=>r.name=="Female_Face_02");
    System.Action<bool,string> check=(ok,message)=>{if(!ok)throw new System.Exception(message);};
    check(mesh.vertexCount==7107 && mesh.subMeshCount==6,"수정 토폴로지 불일치");
    check(skin.sharedMesh==mesh,"정규 얼굴 참조 불일치");
    check(before.blendShapeCount==mesh.blendShapeCount,"표정 수 변경");
    check(before.bindposes.SequenceEqual(mesh.bindposes),"바인드 변경");
    var oldVertices=before.vertices;var vertices=mesh.vertices;
    check(oldVertices.Take(7015).SequenceEqual(vertices.Take(7015)),"얼굴 정점 변경");
    check(before.normals.Take(7015).SequenceEqual(mesh.normals.Take(7015)),"얼굴 노멀 변경");
    check(before.tangents.Take(7015).SequenceEqual(mesh.tangents.Take(7015)),"얼굴 탄젠트 변경");
    check(before.uv.Take(7015).SequenceEqual(mesh.uv.Take(7015)),"얼굴 UV 변경");
    check(before.boneWeights.Take(7015).SequenceEqual(mesh.boneWeights.Take(7015)),"얼굴 웨이트 변경");
    check(before.GetTriangles(0).SequenceEqual(mesh.GetTriangles(0).Take(before.GetTriangles(0).Length)),"얼굴 삼각형 변경");
    check(mesh.GetTriangles(5).Length==0,"목이 별도 재질로 렌더링됩니다.");
    for(int sub=1;sub<5;sub++)check(before.GetTriangles(sub).SequenceEqual(mesh.GetTriangles(sub)),"얼굴 삼각형 변경");
    foreach(var weight in mesh.boneWeights)
    {
        check(UnityEngine.Mathf.Abs(weight.weight0+weight.weight1+weight.weight2+weight.weight3-1)<.00001f,"웨이트 합 오류");
        check(new[]{weight.boneIndex0,weight.boneIndex1,weight.boneIndex2,weight.boneIndex3}.All(i=>i>=0&&i<mesh.bindposes.Length),"본 인덱스 오류");
    }
    int[] top={1,2182,2186,2193,2198,2205,933,931,471,469,468,459,455,452,447,445,254,247,241,235,231,229,228,224,218,215,30,23,17,12,5,2};
    int[] bottom={7029,7030,7033,7036,7038,7066,7062,7059,7054,7050,7047,7044,7019,7021,7024,7026};
    var faceIndices=before.GetTriangles(0);
    var neckIndices=mesh.GetTriangles(0).Skip(faceIndices.Length).ToArray();
    var uvs=mesh.uv;
    int matchingEdges=0;
    for(int edge=0;edge<32;edge++){
        var a=vertices[top[edge]];var b=vertices[top[(edge+1)%32]];
        bool found=false;
        for(int f=0;f<faceIndices.Length&&!found;f+=3){
            int[] source={faceIndices[f],faceIndices[f+1],faceIndices[f+2]};
            int ia=System.Array.FindIndex(source,i=>(vertices[i]-a).sqrMagnitude<1e-12f);
            int ib=System.Array.FindIndex(source,i=>(vertices[i]-b).sqrMagnitude<1e-12f);
            if(ia<0||ib<0)continue;
            for(int n=0;n<neckIndices.Length;n+=3){
                int[] target={neckIndices[n],neckIndices[n+1],neckIndices[n+2]};
                int ja=System.Array.FindIndex(target,i=>(vertices[i]-a).sqrMagnitude<1e-12f);
                int jb=System.Array.FindIndex(target,i=>(vertices[i]-b).sqrMagnitude<1e-12f);
                if(ja>=0&&jb>=0&&uvs[source[ia]]==uvs[target[ja]]&&uvs[source[ib]]==uvs[target[jb]]){found=true;break;}
            }
        }
        check(found,"얼굴·목 UV 경계 불일치: "+edge);matchingEdges++;
    }
    var seamPairs=new System.Collections.Generic.List<int[]>();
    for(int i=0;i<32;i++)
        for(int j=0;j<7015;j++)
            if((vertices[j]-vertices[top[i]]).sqrMagnitude<1e-12f) seamPairs.Add(new[]{j,7063+i});
    for(int i=7095;i<mesh.vertexCount;i++){
        int original=Enumerable.Range(7015,80).First(j=>(vertices[j]-vertices[i]).sqrMagnitude<1e-12f);seamPairs.Add(new[]{original,i});
    }
    float maxSeam=0,maxBottom=0;
    nativeSkin.transform.SetPositionAndRotation(skin.transform.position,skin.transform.rotation);
    nativeSkin.transform.localScale=skin.transform.lossyScale;
    nativeSkin.sharedMesh=before;nativeSkin.bones=skin.bones;nativeSkin.rootBone=skin.rootBone;
    System.Action checkPose=()=>
    {
        skin.BakeMesh(baked);nativeSkin.BakeMesh(nativeBaked);
        var points=baked.vertices;var originalPoints=nativeBaked.vertices;
        check(points.All(v=>!float.IsNaN(v.x)&&!float.IsInfinity(v.x)&&!float.IsNaN(v.y)&&!float.IsInfinity(v.y)&&!float.IsNaN(v.z)&&!float.IsInfinity(v.z)),"유한 좌표 검사 실패");
        foreach(var pair in seamPairs) maxSeam=UnityEngine.Mathf.Max(maxSeam,(points[pair[0]]-points[pair[1]]).magnitude);
        for(int i=0;i<16;i++)maxBottom=UnityEngine.Mathf.Max(maxBottom,(points[7015+i]-originalPoints[bottom[i]]).magnitude);
    };
    var head=skin.bones.Single(b=>b.name=="Head");var neck=skin.bones.Single(b=>b.name=="Neck");
    var headRotation=head.localRotation;var neckRotation=neck.localRotation;
    int poses=0,frames=0;
    foreach(var angles in new[]{UnityEngine.Vector3.zero,new UnityEngine.Vector3(0,45,0),new UnityEngine.Vector3(0,-45,0),new UnityEngine.Vector3(25,0,0),new UnityEngine.Vector3(-25,0,0),new UnityEngine.Vector3(0,0,20),new UnityEngine.Vector3(0,0,-20)})
    {
        head.localRotation=headRotation*UnityEngine.Quaternion.Euler(angles);
        neck.localRotation=neckRotation*UnityEngine.Quaternion.Euler(angles*.5f);
        checkPose();poses++;
    }
    head.localRotation=headRotation;neck.localRotation=neckRotation;
    for(int shape=0;shape<mesh.blendShapeCount;shape++)
    {
        check(before.GetBlendShapeName(shape)==mesh.GetBlendShapeName(shape),"표정 이름 변경");
        check(before.GetBlendShapeFrameCount(shape)==mesh.GetBlendShapeFrameCount(shape),"표정 프레임 변경");
        for(int frame=0;frame<mesh.GetBlendShapeFrameCount(shape);frame++)
        {
            var oldDelta=new UnityEngine.Vector3[before.vertexCount];var oldNormal=new UnityEngine.Vector3[before.vertexCount];var oldTangent=new UnityEngine.Vector3[before.vertexCount];
            var delta=new UnityEngine.Vector3[mesh.vertexCount];var normal=new UnityEngine.Vector3[mesh.vertexCount];var tangent=new UnityEngine.Vector3[mesh.vertexCount];
            before.GetBlendShapeFrameVertices(shape,frame,oldDelta,oldNormal,oldTangent);mesh.GetBlendShapeFrameVertices(shape,frame,delta,normal,tangent);
            check(oldDelta.Take(7015).SequenceEqual(delta.Take(7015))&&oldNormal.Take(7015).SequenceEqual(normal.Take(7015))&&oldTangent.Take(7015).SequenceEqual(tangent.Take(7015)),"얼굴 표정 델타 변경");
            check(before.GetBlendShapeFrameWeight(shape,frame)==mesh.GetBlendShapeFrameWeight(shape,frame),"표정 프레임 가중치 변경");
            skin.SetBlendShapeWeight(shape,mesh.GetBlendShapeFrameWeight(shape,frame));
            head.localRotation=headRotation*UnityEngine.Quaternion.Euler(15,35,0);
            checkPose();frames++;
            skin.SetBlendShapeWeight(shape,0);
        }
    }
    check(maxSeam<.00001f&&maxBottom<.00001f,"경계 벌어짐");
    foreach(var renderer in root.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true))
        check(renderer.sharedMesh!=null&&renderer.sharedMaterials.All(m=>m!=null)&&renderer.bones.All(b=>b!=null),"메시·재질·본 누락");
    int missing=root.GetComponentsInChildren<UnityEngine.Transform>(true).Sum(t=>UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
    check(missing==0,"Missing Script");
    var report="UV 경계 32개 일치 / 얼굴·목 동일 재질\n정점 7125 → 7107, 목 삼각형 120 → 112\n얼굴 7015개 정점·UV·노멀·탄젠트·웨이트·삼각형·표정 델타 보존\n표정 "+mesh.blendShapeCount+"개 / 프레임 "+frames+"개 / 목·머리 자세 "+poses+"개 검사\n상단 경계 최대 오차 "+maxSeam+"m / 원본 하단 경계 최대 오차 "+maxBottom+"m\nMissing Script "+missing+" / 메시·본·재질 누락 0\nAI Editor BakeMesh 검사. 실제 Play Mode·사용자 미술 승인 대기.";
    System.IO.File.WriteAllText("output/DaonSkinSeamFix/validation.txt",report);
    return report;
}
finally
{
    UnityEngine.Object.DestroyImmediate(nativeSkin.gameObject);UnityEngine.Object.DestroyImmediate(root);
    UnityEngine.Object.DestroyImmediate(baked);UnityEngine.Object.DestroyImmediate(nativeBaked);
    UnityEditor.AssetDatabase.DeleteAsset(temporary);
}
