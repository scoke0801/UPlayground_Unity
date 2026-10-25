var folder="Assets/08.Models/Characters/DaonNewIdentity";
var meshPath=folder+"/Daon_P09_Head.asset";
var profilePath="Assets/10.Datas/Actor/ExternalModels/Daon_P09_Face.asset";
if(UnityEditor.AssetDatabase.LoadMainAssetAtPath(meshPath)!=null || UnityEditor.AssetDatabase.LoadMainAssetAtPath(profilePath)!=null)throw new System.Exception("기존 얼굴 이식 결과를 보존합니다.");
var source=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(folder+"/Daon_NewIdentity_Base.prefab"));
var target=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics));
try
{
    var src=source.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true).First(r=>r.name=="Body");
    var dst=target.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true).First(r=>r.name=="Female_Face_01");
    var original=src.sharedMesh;var native=dst.sharedMesh;
    int[] submeshes={0,2,3,4,5};
    var indices=submeshes.SelectMany(s=>original.GetTriangles(s)).Distinct().OrderBy(i=>i).ToArray();
    var remap=new System.Collections.Generic.Dictionary<int,int>();for(int i=0;i<indices.Length;i++)remap.Add(indices[i],i);
    var srcHead=source.GetComponent<UnityEngine.Animator>().GetBoneTransform(UnityEngine.HumanBodyBones.Head).position;
    var dstHead=target.GetComponent<UnityEngine.Animator>().GetBoneTransform(UnityEngine.HumanBodyBones.Head).position;
    var fitting=dst.transform.worldToLocalMatrix*UnityEngine.Matrix4x4.Translate(dstHead)*UnityEngine.Matrix4x4.Scale(UnityEngine.Vector3.one*.93f)*UnityEngine.Matrix4x4.Translate(-srcHead);
    var sourceWeights=original.boneWeights;var sourceBind=original.bindposes;
    var matrices=new UnityEngine.Matrix4x4[indices.Length];
    var weights=new UnityEngine.BoneWeight[indices.Length];
    var boneMap=new System.Collections.Generic.Dictionary<string,string>{{"Hips","Hips"},{"Spine","Spine"},{"Chest","Chest"},{"Head","Head"},{"Neck","Neck"},{"LeftEye","eye_L"},{"RightEye","eye_R"}};
    for(int i=0;i<indices.Length;i++)
    {
        var bw=sourceWeights[indices[i]];int[] bones={bw.boneIndex0,bw.boneIndex1,bw.boneIndex2,bw.boneIndex3};float[] values={bw.weight0,bw.weight1,bw.weight2,bw.weight3};int[] mapped=new int[4];var sum=new UnityEngine.Matrix4x4();
        for(int j=0;j<4;j++)
        {
            if(values[j]<=0)continue;
            string name=src.bones[bones[j]].name;
            if(!boneMap.ContainsKey(name))throw new System.Exception("얼굴에서 예상 밖의 가중 본: "+name);
            mapped[j]=System.Array.FindIndex(dst.bones,b=>b.name==boneMap[name]);
            if(mapped[j]<0)throw new System.Exception("P09 본 없음: "+name);
            // 원본 프리팹의 시선 포즈를 중립 얼굴에 굽지 않는다. 눈도 Head 기준 바인드 자세에서 이식한다.
            int poseBone=name=="LeftEye"||name=="RightEye"?System.Array.FindIndex(src.bones,b=>b.name=="Head"):bones[j];
            var mat=src.bones[poseBone].localToWorldMatrix*sourceBind[poseBone];
            for(int k=0;k<16;k++)sum[k]+=mat[k]*values[j];
        }
        matrices[i]=fitting*sum;
        weights[i]=new UnityEngine.BoneWeight{boneIndex0=mapped[0],boneIndex1=mapped[1],boneIndex2=mapped[2],boneIndex3=mapped[3],weight0=bw.weight0,weight1=bw.weight1,weight2=bw.weight2,weight3=bw.weight3};
    }
    var vertices=original.vertices;var normals=original.normals;var tangents=original.tangents;
    var mesh=new UnityEngine.Mesh();mesh.name="Daon_P09_Head";
    mesh.vertices=indices.Select((index,i)=>matrices[i].MultiplyPoint3x4(vertices[index])).ToArray();
    mesh.normals=indices.Select((index,i)=>matrices[i].inverse.transpose.MultiplyVector(normals[index]).normalized).ToArray();
    mesh.tangents=indices.Select((index,i)=>{var t=tangents[index];var v=matrices[i].MultiplyVector(new UnityEngine.Vector3(t.x,t.y,t.z)).normalized;return new UnityEngine.Vector4(v.x,v.y,v.z,t.w);}).ToArray();
    mesh.uv=indices.Select(i=>original.uv[i]).ToArray();mesh.boneWeights=weights;mesh.bindposes=native.bindposes;mesh.subMeshCount=submeshes.Length;
    for(int i=0;i<submeshes.Length;i++)mesh.SetTriangles(original.GetTriangles(submeshes[i]).Select(v=>remap[v]).ToArray(),i);
    var aliases=new string[]{"Eye_Blink","Eye_BlinkLeft","Eye_BlinkRight","Eye_Happy","Eye_Sad","Eye_Half","Eye_Sad","Eye_Angry","Eye_Open","Eye_Half","Eye_addSmile","Iris_Small","Iris_Clear","Brow_Up","Brow_Down","Brow_Outside","Brow_Inside","Brow_Frown","Brow_Curve","Brow_Frown","Brow_Curve2","Brow_Up","Brow_Down","Brow_Inside","Mouth_Smile","Mouth_Happy","Mouth_Teeth","Mouth_Frown","Mouth_Open","Mouth_Clench","Mouth_BeeTongue","あ","い","う","え","お"};
    var names=new System.Collections.Generic.HashSet<string>();
    var dv=new UnityEngine.Vector3[original.vertexCount];var dn=new UnityEngine.Vector3[original.vertexCount];var dt=new UnityEngine.Vector3[original.vertexCount];
    for(int pass=0;pass<2;pass++)for(int shape=0;shape<(pass==0?native.blendShapeCount:original.blendShapeCount);shape++)
    {
        string name=pass==0?native.GetBlendShapeName(shape):original.GetBlendShapeName(shape);
        if(!names.Add(name))continue;
        int sourceShape=pass==0?original.GetBlendShapeIndex(aliases[shape]):shape;
        if(sourceShape<0)throw new System.Exception("표정 매핑 없음: "+name);
        for(int frame=0;frame<original.GetBlendShapeFrameCount(sourceShape);frame++)
        {
            original.GetBlendShapeFrameVertices(sourceShape,frame,dv,dn,dt);
            mesh.AddBlendShapeFrame(name,original.GetBlendShapeFrameWeight(sourceShape,frame),indices.Select((index,i)=>matrices[i].MultiplyVector(dv[index])).ToArray(),indices.Select((index,i)=>matrices[i].MultiplyVector(dn[index])).ToArray(),indices.Select((index,i)=>matrices[i].MultiplyVector(dt[index])).ToArray());
        }
    }
    mesh.RecalculateBounds();
    var output="output/DaonP09Compatibility";System.IO.Directory.CreateDirectory(output);
    System.Func<UnityEngine.Vector3[],string> jsonPoints=points=>"["+string.Join(",",points.Select(v=>"["+v.x.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+v.y.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+v.z.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"]").ToArray())+"]";
    System.IO.File.WriteAllText(output+"/unfitted_vertices.json",jsonPoints(mesh.vertices));
    System.IO.File.WriteAllText(output+"/native_surface.json","{\"vertices\":"+jsonPoints(native.vertices)+",\"triangles\":["+string.Join(",",native.GetTriangles(0).Select(i=>i.ToString()).ToArray())+"],\"customTriangles\":["+string.Join(",",mesh.GetTriangles(0).Select(i=>i.ToString()).ToArray())+"]}");
    UnityEditor.AssetDatabase.CreateAsset(mesh,meshPath);
    var profile=UnityEngine.ScriptableObject.CreateInstance<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>();
    profile.mesh=mesh;profile.materials=submeshes.Select(i=>src.sharedMaterials[i]).ToArray();profile.boneNames=dst.bones.Select(b=>b.name).ToArray();
    profile.rendererPaths=target.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true).Where(r=>r.name.StartsWith("Female_Face_")).Select(r=>UnityEditor.AnimationUtility.CalculateTransformPath(r.transform,target.transform)).ToArray();
    profile.ValidateProfile();profile.Apply(target);UnityEditor.AssetDatabase.CreateAsset(profile,profilePath);UnityEditor.AssetDatabase.SaveAssets();
    return new{vertices=mesh.vertexCount,shapes=mesh.blendShapeCount,materials=profile.materials.Length,profile=profilePath};
}
finally{UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(target);}
