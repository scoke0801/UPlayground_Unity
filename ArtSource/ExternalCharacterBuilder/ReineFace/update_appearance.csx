var folder="Assets/08.Models/Characters/ReineP09";
var face=AssetDatabase.LoadAssetAtPath<Material>(folder+"/Reine_P09_Material_0.mat");
var original=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09ToonLookProfileSO>("Assets/10.Datas/Actor/ScenarioResidents/ToonLook/P09_Residents_ToonLook.asset");
string toonPath="Assets/10.Datas/Actor/ExternalModels/Reine_P09_ToonLook.asset";
var toon=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09ToonLookProfileSO>(toonPath);
if(toon==null){toon=UnityEngine.Object.Instantiate(original);AssetDatabase.CreateAsset(toon,toonPath);}
string skinPath=folder+"/Reine_P09_Skin.mat";
var skin=AssetDatabase.LoadAssetAtPath<Material>(skinPath);
if(skin==null){skin=new Material(original.bindings[0].replacement);AssetDatabase.CreateAsset(skin,skinPath);}
// 단색의 색 공간 변환 차이를 없애기 위해 얼굴과 같은 텍스처에서 턱 경계의 피부색을 읽는다.
skin.mainTexture=face.mainTexture;skin.SetColor("_Color",Color.white);skin.SetVector("_MainTexHSVG",new Vector4(0,1,1,1));
skin.mainTextureScale=Vector2.zero;skin.mainTextureOffset=new Vector2(.5f,.16f);
foreach(var m in new[]{face,skin}){
foreach(string p in new[]{"_UseBumpMap","_UseBump2ndMap","_UseReflection","_UseMatCap","_UseMatCap2nd","_UseRim","_UseBacklight","_UseRimShade","_UseMain2ndTex","_UseMain3rdTex","_OutlineWidth"})if(m.HasProperty(p))m.SetFloat(p,0);
foreach(string p in new[]{"_ShadowColorTex","_Shadow2ndColorTex","_Shadow3rdColorTex","_ShadowStrengthMask","_ShadowBlurMask","_ShadowBorderMask","_MainColorAdjustMask"})if(m.HasProperty(p))m.SetTexture(p,null);
// P09 얼굴 재질의 따뜻한 그림자를 사용하되 Reine의 얕은 코 굴곡이 읽히도록 경계를 조정한다.
m.SetFloat("_ShadowNormalStrength",1);m.SetFloat("_ShadowBorder",.60f);m.SetFloat("_ShadowBlur",.65f);
m.SetColor("_ShadowColor",new Color(.92f,.78f,.76f,1));
m.SetColor("_Shadow2ndColor",new Color(.9786666f,.96f,1,0));
EditorUtility.SetDirty(m);
}
foreach(var binding in toon.bindings)if(binding.source==original.bindings[0].source)binding.replacement=skin;
toon.ValidateBindings();EditorUtility.SetDirty(toon);
var profile=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/Reine_P09_Face.asset");
profile.materials[5]=skin;
// 줄인 이마·두상에 맞춰 헤어 높이와 폭을 조정하고 눈과 두피 관통을 검수한다.
profile.hairAttachments=new[]{new UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO.HairAttachment{constraintPath="Mesh/Hair/Hair_03/Skeleton_P09_Hair_03/Constraint_Head",positionOffset=new Vector3(-.006f,.008f,0),localScale=new Vector3(1f,.87f,1f)},new UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO.HairAttachment{
constraintPath="Mesh/Hair/Hair_11/Skeleton_P09_Hair_11/Constraint_Head",positionOffset=new Vector3(0,.014f,0),localScale=new Vector3(.98f,.90f,1f)}};
EditorUtility.SetDirty(profile);
foreach(string suffix in new[]{"","_Alternate"}){
string presetPath="Assets/10.Datas/Actor/ExternalModels/Reine_P09"+suffix+"_BuildPreset.asset";
var preset=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.BuildPreset>(presetPath);
preset.config.ToonLookProfile=toon;
preset.config.HairStyleSo=AssetDatabase.LoadAssetAtPath<ScriptableObject>(UPlayGround.Editor.P09Builder.PathConfig.CatalogRoot+"/HairStyle/HairStyleEditPartData_"+(suffix==""?"11":"03")+".asset");
preset.config.HairColorSo=AssetDatabase.LoadAssetAtPath<ScriptableObject>(UPlayGround.Editor.P09Builder.PathConfig.CatalogRoot+"/HairColor/HairColorEditPartData_01.asset");
string modelPath="Assets/03.Prefabs/Characters/ExternalModels/Reine_P09"+(suffix==""?"Compatible":suffix)+".prefab";
var root=PrefabUtility.LoadPrefabContents(modelPath);
try{var context=new UPlayGround.Editor.P09Builder.BuildContext(preset.config);context.RootInstance=root;new UPlayGround.Editor.P09Builder.ApplyAppearanceStep().Execute(context);PrefabUtility.SaveAsPrefabAsset(root,modelPath);}
finally{PrefabUtility.UnloadPrefabContents(root);}
EditorUtility.SetDirty(preset);
}
AssetDatabase.SaveAssets();return new{skinSample=skin.mainTextureOffset.ToString(),toon=toonPath};





