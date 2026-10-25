var folder="Assets/08.Models/Characters/BokuseiP09";
var face=AssetDatabase.LoadAssetAtPath<Material>(folder+"/Bokusei_P09_Face_0.mat");
var body=AssetDatabase.LoadAssetAtPath<Material>(folder+"/Bokusei_P09_Skin.mat");
// 얼굴만 어둡게 만드는 MatCap과 몸에만 남은 노멀·그림자 마스크를 제거한다.
face.SetFloat("_UseMatCap",0);face.SetFloat("_UseMatCap2nd",0);
foreach(string property in new[]{"_UseBumpMap","_UseBump2ndMap","_UseRim","_UseBacklight","_UseReflection","_UseMatCap","_UseMatCap2nd","_UseRimShade"})
if(body.HasProperty(property))body.SetFloat(property,0);
for(int i=0;i<face.shader.GetPropertyCount();i++){
string property=face.shader.GetPropertyName(i);if(!body.HasProperty(property))continue;
if(!property.StartsWith("_Shadow")&&!property.StartsWith("_Light")&&property!="_MonochromeLighting"&&property!="_AsUnlit"&&property!="_VertexLightStrength"&&property!="_UseShadow")continue;
var type=face.shader.GetPropertyType(i);
if(type==UnityEngine.Rendering.ShaderPropertyType.Texture)body.SetTexture(property,face.GetTexture(property));
else if(type==UnityEngine.Rendering.ShaderPropertyType.Color||type==UnityEngine.Rendering.ShaderPropertyType.Vector)body.SetVector(property,face.GetVector(property));
else body.SetFloat(property,face.GetFloat(property));
}
// 기존 몸 atlas의 구운 피부 명암이 새 얼굴의 평평한 목 알베도와 경계를 만들었다.
// UV (0.5, 0.00404)에서 측정한 sRGB 피부색을 공통 노출 피부의 베이스로 사용한다.
body.mainTexture=null;body.SetColor("_Color",new Color(226f/255f,200f/255f,182f/255f,1));
body.SetVector("_MainTexHSVG",new Vector4(0,1,1,1));body.SetFloat("_OutlineWidth",0);
EditorUtility.SetDirty(face);EditorUtility.SetDirty(body);AssetDatabase.SaveAssetIfDirty(face);AssetDatabase.SaveAssetIfDirty(body);
return "얼굴·몸 조명 반응 및 목 피부색 일치. 프로젝트 소유 재질만 변경.";
