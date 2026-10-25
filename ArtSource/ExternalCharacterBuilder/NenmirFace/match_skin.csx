// 얼굴 고유 UV·표정 텍스처는 유지하고 몸과 같은 조명 반응 및 피부색을 사용한다.
var face=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/08.Models/Characters/NenmirP09/Nenmir_P09_Mat_Nenmir_Face.mat");
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
var jsonType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var settings=(Dictionary<string,float>)jsonType.GetMethods().Single(m=>m.Name=="DeserializeObject"&&m.IsGenericMethodDefinition&&m.GetParameters().Length==1&&m.GetParameters()[0].ParameterType==typeof(string)).MakeGenericMethod(typeof(Dictionary<string,float>)).Invoke(null,new object[]{System.IO.File.ReadAllText("ArtSource/ExternalCharacterBuilder/NenmirFace/appearance.json")});
face.SetVector("_MainTexHSVG",new UnityEngine.Vector4(0f,settings["saturation"],1f,settings["gamma"]));
foreach(var setting in settings.Where(p=>p.Key.StartsWith("_"))){if(!face.HasProperty(setting.Key))throw new InvalidOperationException("얼굴 셰이더 속성 누락: "+setting.Key);face.SetFloat(setting.Key,setting.Value);}
UnityEditor.EditorUtility.SetDirty(face);UnityEditor.AssetDatabase.SaveAssetIfDirty(face);
return new{material=UnityEditor.AssetDatabase.GetAssetPath(face),lightingProperties=properties};

