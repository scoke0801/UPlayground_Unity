// 조형 원본에서 눈과 하이라이트만 다시 계산한다. 재실행해도 보정량이 누적되지 않는다.
var folder="Assets/08.Models/Characters/DaonNewIdentity";
var profile=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/Daon_P09_Face.asset");
profile.ValidateProfile();
var mesh=profile.mesh;
var source=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(folder+"/Daon_NewIdentity_Base.prefab"));
var target=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics));
var repaired=UnityEngine.Object.Instantiate(mesh);
try
{
    var src=source.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true).Single(r=>r.name=="Body");
    var dst=target.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true).First(r=>r.name=="Female_Face_01");
    var original=src.sharedMesh;
    var indices=new[]{0,2,3,4,5}.SelectMany(s=>original.GetTriangles(s)).Distinct().OrderBy(i=>i).ToArray();
    var affected=mesh.GetTriangles(3).Concat(mesh.GetTriangles(4)).Distinct().ToArray();
    if(mesh.vertexCount!=indices.Length+110 || affected.Length!=1936)
        throw new System.Exception("다온 얼굴 토폴로지가 변경되었습니다. 원본 정점 대응을 다시 확인하세요.");
    var srcHead=source.GetComponent<UnityEngine.Animator>().GetBoneTransform(UnityEngine.HumanBodyBones.Head);
    var dstHead=target.GetComponent<UnityEngine.Animator>().GetBoneTransform(UnityEngine.HumanBodyBones.Head);
    var fitting=dst.transform.worldToLocalMatrix*UnityEngine.Matrix4x4.Translate(dstHead.position)*UnityEngine.Matrix4x4.Scale(UnityEngine.Vector3.one*.93f)*UnityEngine.Matrix4x4.Translate(-srcHead.position);
    var neutral=fitting*srcHead.localToWorldMatrix*original.bindposes[System.Array.IndexOf(src.bones,srcHead)];
    var normalMatrix=neutral.inverse.transpose;
    var vertices=mesh.vertices;var normals=mesh.normals;var tangents=mesh.tangents;
    var sourceVertices=original.vertices;var sourceNormals=original.normals;var sourceTangents=original.tangents;
    foreach(int i in affected)
    {
        int sourceIndex=indices[i];
        vertices[i]=neutral.MultiplyPoint3x4(sourceVertices[sourceIndex]);
        normals[i]=normalMatrix.MultiplyVector(sourceNormals[sourceIndex]).normalized;
        var tangent=sourceTangents[sourceIndex];
        var direction=neutral.MultiplyVector(new UnityEngine.Vector3(tangent.x,tangent.y,tangent.z)).normalized;
        tangents[i]=new UnityEngine.Vector4(direction.x,direction.y,direction.z,tangent.w);
    }
    repaired.vertices=vertices;repaired.normals=normals;repaired.tangents=tangents;repaired.ClearBlendShapes();
    var aliases=new[]{"Eye_Blink","Eye_BlinkLeft","Eye_BlinkRight","Eye_Happy","Eye_Sad","Eye_Half","Eye_Sad","Eye_Angry","Eye_Open","Eye_Half","Eye_addSmile","Iris_Small","Iris_Clear","Brow_Up","Brow_Down","Brow_Outside","Brow_Inside","Brow_Frown","Brow_Curve","Brow_Frown","Brow_Curve2","Brow_Up","Brow_Down","Brow_Inside","Mouth_Smile","Mouth_Happy","Mouth_Teeth","Mouth_Frown","Mouth_Open","Mouth_Clench","Mouth_BeeTongue","あ","い","う","え","お"};
    for(int shape=0;shape<mesh.blendShapeCount;shape++)
    {
        string name=mesh.GetBlendShapeName(shape);
        int sourceShape=original.GetBlendShapeIndex(shape<aliases.Length?aliases[shape]:name);
        if(sourceShape<0 || mesh.GetBlendShapeFrameCount(shape)!=original.GetBlendShapeFrameCount(sourceShape))
            throw new System.Exception("표정 대응 실패: "+name);
        for(int frame=0;frame<mesh.GetBlendShapeFrameCount(shape);frame++)
        {
            var dv=new UnityEngine.Vector3[mesh.vertexCount];var dn=new UnityEngine.Vector3[mesh.vertexCount];var dt=new UnityEngine.Vector3[mesh.vertexCount];
            var sv=new UnityEngine.Vector3[original.vertexCount];var sn=new UnityEngine.Vector3[original.vertexCount];var st=new UnityEngine.Vector3[original.vertexCount];
            mesh.GetBlendShapeFrameVertices(shape,frame,dv,dn,dt);
            original.GetBlendShapeFrameVertices(sourceShape,frame,sv,sn,st);
            foreach(int i in affected)
            {
                dv[i]=neutral.MultiplyVector(sv[indices[i]]);
                dn[i]=neutral.MultiplyVector(sn[indices[i]]);
                dt[i]=neutral.MultiplyVector(st[indices[i]]);
            }
            repaired.AddBlendShapeFrame(name,mesh.GetBlendShapeFrameWeight(shape,frame),dv,dn,dt);
        }
    }
    repaired.RecalculateBounds();
    if(repaired.blendShapeCount!=mesh.blendShapeCount)throw new System.Exception("표정 누락");
    repaired.name=mesh.name;
    UnityEditor.EditorUtility.CopySerialized(repaired,mesh);
    UnityEditor.EditorUtility.SetDirty(mesh);UnityEditor.AssetDatabase.SaveAssetIfDirty(mesh);
    return new{affectedVertices=affected.Length,shapes=mesh.blendShapeCount,mesh=UnityEditor.AssetDatabase.GetAssetPath(mesh)};
}
finally
{
    UnityEngine.Object.DestroyImmediate(repaired);
    UnityEngine.Object.DestroyImmediate(source);
    UnityEngine.Object.DestroyImmediate(target);
}
