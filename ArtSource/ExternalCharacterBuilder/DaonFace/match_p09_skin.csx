// 얼굴 고유 UV·표정 텍스처는 유지하고 몸과 같은 조명 반응 및 피부색을 사용한다.
var face=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/08.Models/Characters/DaonNewIdentity/Daon_ho_face_alt4.mat");
var body=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/10.Datas/Actor/ScenarioResidents/ToonLook/P09_Female_Skin_Toon.mat");
var properties=new System.Collections.Generic.List<string>();
for(int i=0;i<body.shader.GetPropertyCount();i++)
{
    string name=body.shader.GetPropertyName(i);
    if(!face.HasProperty(name))continue;
    if(!name.StartsWith("_Shadow")&&!name.StartsWith("_Light")&&name!="_MonochromeLighting"&&name!="_AsUnlit"&&name!="_VertexLightStrength"&&name!="_UseShadow")continue;
    var type=body.shader.GetPropertyType(i);
    if(type==UnityEngine.Rendering.ShaderPropertyType.Texture)continue;
    if(type==UnityEngine.Rendering.ShaderPropertyType.Color||type==UnityEngine.Rendering.ShaderPropertyType.Vector)
        face.SetVector(name,body.GetVector(name));
    else face.SetFloat(name,body.GetFloat(name));
    properties.Add(name);
}
face.SetFloat("_UseMain2ndTex",0);
face.SetFloat("_UseMain3rdTex",0);
face.SetVector("_MainTexHSVG",new UnityEngine.Vector4(-.025f,.6f,.98f,1f));
UnityEditor.EditorUtility.SetDirty(face);UnityEditor.AssetDatabase.SaveAssetIfDirty(face);
return new{material=UnityEditor.AssetDatabase.GetAssetPath(face),lightingProperties=properties};
