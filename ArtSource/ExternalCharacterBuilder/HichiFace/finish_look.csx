var preset=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.BuildPreset>("Assets/10.Datas/Actor/ExternalModels/Hichi_P09_BuildPreset.asset");
var catalog=new UPlayGround.Editor.P09Builder.P09AssetCatalog();catalog.Refresh();
preset.config.HairColorSo=catalog.HairColors.Single(s=>s.name=="HairColorEditPartData_01");
var template=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.BuildPreset>("Assets/10.Datas/Actor/ExternalModels/Daon_P09_BuildPreset.asset");
preset.config.ToonLookProfile=template.config.ToonLookProfile;
var profile=preset.config.FaceMeshProfile;
var face=profile.materials[0];
var body=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/10.Datas/Actor/ScenarioResidents/ToonLook/P09_Female_Skin_Toon.mat");
for(int i=0;i<body.shader.GetPropertyCount();i++)
{
    string key=body.shader.GetPropertyName(i);
    if(!face.HasProperty(key)||(!key.StartsWith("_Shadow")&&!key.StartsWith("_Light")&&key!="_AsUnlit"&&key!="_UseShadow"&&key!="_MonochromeLighting"))continue;
    var type=body.shader.GetPropertyType(i);
    if(type==UnityEngine.Rendering.ShaderPropertyType.Texture)continue;
    if(type==UnityEngine.Rendering.ShaderPropertyType.Color||type==UnityEngine.Rendering.ShaderPropertyType.Vector)face.SetVector(key,body.GetVector(key));
    else face.SetFloat(key,body.GetFloat(key));
}
face.SetFloat("_UseMatCap",0);face.SetFloat("_UseMatCap2nd",0);
face.SetTexture("_ShadowColorTex",null);face.SetTexture("_Shadow2ndColorTex",null);
face.SetFloat("_ShadowBlur",.35f);face.SetFloat("_ShadowStrength",.55f);
face.SetVector("_MainTexHSVG",new UnityEngine.Vector4(0,1,1,1));
profile.materials[2]=body;
foreach(var asset in new UnityEngine.Object[]{preset,profile,face}){UnityEditor.EditorUtility.SetDirty(asset);UnityEditor.AssetDatabase.SaveAssetIfDirty(asset);}
var path="Assets/03.Prefabs/Characters/ExternalModels/Hichi_P09Compatible.prefab";
var root=UnityEditor.PrefabUtility.LoadPrefabContents(path);
try
{
    var ctx=new UPlayGround.Editor.P09Builder.BuildContext(preset.config);ctx.RootInstance=root;
    new UPlayGround.Editor.P09Builder.ApplyAppearanceStep().Execute(ctx);
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,path);
}
finally{UnityEditor.PrefabUtility.UnloadPrefabContents(root);}
return "갈색 머리와 기존 P09 톤 프로파일 연결";
