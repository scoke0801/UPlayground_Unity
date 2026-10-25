var face=AssetDatabase.LoadAssetAtPath<Material>("Assets/08.Models/Characters/LianLianP09/LianLian_P09_Face.mat");
var body=AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/Actor/ScenarioResidents/ToonLook/P09_Female_Skin_Toon.mat");
var albedo=face.mainTexture;
face.shader=body.shader;face.CopyPropertiesFromMaterial(body);face.mainTexture=albedo;
face.SetColor("_Color",Color.white);face.SetVector("_MainTexHSVG",new Vector4(0,.75f,1,1));
face.SetFloat("_OutlineWidth",0);
EditorUtility.SetDirty(face);AssetDatabase.SaveAssetIfDirty(face);
return new {material=AssetDatabase.GetAssetPath(face),shader=face.shader.name};
