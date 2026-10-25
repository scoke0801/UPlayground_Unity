var folder="Assets/08.Models/Characters/DaonNewIdentity";
var source=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/ExternalAssets/Character/Honoka/Prefab/Honoka_alt4.prefab");
Func<float,float,float,float> smooth=(a,b,x)=>{float t=UnityEngine.Mathf.Clamp01((x-a)/(b-a));return t*t*(3-2*t);};
Func<UnityEngine.Vector3,UnityEngine.Vector3> refine=p=>{
    float front=1-smooth(-.055f,.015f,p.y);
    float nose=UnityEngine.Mathf.Exp(-UnityEngine.Mathf.Pow(p.x/.012f,2)-UnityEngine.Mathf.Pow((p.z-1.420f)/.018f,2))*front;
    float ear=smooth(.075f,.105f,UnityEngine.Mathf.Abs(p.x))*UnityEngine.Mathf.Exp(-UnityEngine.Mathf.Pow((p.z-1.445f)/.027f,4))*smooth(-.075f,-.025f,p.y);
    p.y+=.0022f*nose;p.x*=1-.17f*ear;
    return p;
};
foreach(string name in new[]{"Body","Hair","Hair_back","Hair_back_long"})
{
    var mesh=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(folder+"/Daon_"+name+".asset");
    var original=source.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true).Single(r=>r.name==name).sharedMesh;
    if(name=="Body")
    {
        var copy=UnityEngine.Object.Instantiate(mesh);
        try
        {
            var before=copy.vertices;var after=before.Select(refine).ToArray();mesh.vertices=after;mesh.ClearBlendShapes();
            for(int index=0;index<copy.blendShapeCount;index++)
            {
                var delta=new UnityEngine.Vector3[before.Length];var normals=new UnityEngine.Vector3[before.Length];var tangents=new UnityEngine.Vector3[before.Length];
                copy.GetBlendShapeFrameVertices(index,0,delta,normals,tangents);
                for(int i=0;i<delta.Length;i++)delta[i]=refine(before[i]+delta[i])-after[i];
                mesh.AddBlendShapeFrame(copy.GetBlendShapeName(index),copy.GetBlendShapeFrameWeight(index,0),delta,normals,tangents);
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(copy);}
    }
    // 원본의 toon용 커스텀 법선을 유지해 UV 경계와 코 주변의 날카로운 음영을 막는다.
    mesh.normals=original.normals;mesh.tangents=original.tangents;mesh.RecalculateBounds();
    UnityEditor.EditorUtility.SetDirty(mesh);UnityEditor.AssetDatabase.SaveAssetIfDirty(mesh);
}
return "콧대의 과도한 돌출 완화, 귀 끝 둥글림, toon 커스텀 법선 보존";
