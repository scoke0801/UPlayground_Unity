// Hichi 전용 몸 재질에 얼굴과 같은 조명 응답과 피부 기조색을 적용한다.
var face=AssetDatabase.LoadAssetAtPath<Material>("Assets/08.Models/Characters/HichiP09/Hichi_P09_Face.mat");
var body=AssetDatabase.LoadAssetAtPath<Material>("Assets/08.Models/Characters/HichiP09/Hichi_P09_Skin.mat");
var sourceTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ExternalAssets/Character/P09_Modular_Humanoid/Model_DATA/Textures/Female_Body/P09_Female_Body_Diff.png");
var readable=new Texture2D(2,2,TextureFormat.RGBA32,false);
try{
    readable.LoadImage(System.IO.File.ReadAllBytes("Assets/08.Models/Characters/HichiP09/Hichi_Face_Albedo.png"));
    var skin=Color.black;for(int i=0;i<16;i++)skin+=readable.GetPixelBilinear(.15f+i*.045f,.012f);skin/=16;skin.a=.65f;
    face.SetFloat("_UseEmission",0);face.SetFloat("_UseEmission2nd",0);EditorUtility.SetDirty(face);AssetDatabase.SaveAssetIfDirty(face);body.parent=null;body.CopyPropertiesFromMaterial(face);body.name="Hichi_P09_Skin";body.mainTexture=sourceTexture;body.SetTexture("_BaseMap",sourceTexture);body.SetTexture("_BaseColorMap",sourceTexture);body.SetFloat("_UseEmission",0);body.SetFloat("_UseEmission2nd",0);
    body.SetFloat("_UseMain2ndTex",1);body.SetTexture("_Main2ndTex",null);body.SetTexture("_Main2ndBlendMask",null);body.SetColor("_Color2nd",skin);body.SetFloat("_Main2ndTexBlendMode",0);body.SetFloat("_Main2ndTexAlphaMode",0);body.SetFloat("_Main2ndEnableLighting",1);body.SetFloat("_Main2ndTexIsDecal",0);
    body.SetTexture("_OutlineTex",null);body.SetTexture("_OutlineWidthMask",null);
    EditorUtility.SetDirty(body);AssetDatabase.SaveAssetIfDirty(body);return new{skin=skin.ToString(),material=AssetDatabase.GetAssetPath(body)};
}finally{UnityEngine.Object.DestroyImmediate(readable);}
