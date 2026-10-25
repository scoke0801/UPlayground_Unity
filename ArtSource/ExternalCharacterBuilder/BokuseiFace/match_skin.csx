var folder="Assets/08.Models/Characters/BokuseiP09";
var face=AssetDatabase.LoadAssetAtPath<Material>(folder+"/Bokusei_P09_Face_0.mat");
var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/ExternalAssets/Character/P09_Modular_Humanoid/Model_DATA/Materials/Male_Body/P09_Male_Skin.mat");
var path=folder+"/Bokusei_P09_Skin.mat";var body=AssetDatabase.LoadAssetAtPath<Material>(path);bool create=body==null;if(create)body=new Material(source);
body.name="Bokusei_P09_Skin";
for(int i=0;i<face.shader.GetPropertyCount();i++){
string name=face.shader.GetPropertyName(i);if(!body.HasProperty(name))continue;
if(!name.StartsWith("_Shadow")&&!name.StartsWith("_Light")&&name!="_MonochromeLighting"&&name!="_AsUnlit"&&name!="_VertexLightStrength"&&name!="_UseShadow")continue;
var type=face.shader.GetPropertyType(i);if(type==UnityEngine.Rendering.ShaderPropertyType.Texture)continue;
if(type==UnityEngine.Rendering.ShaderPropertyType.Color||type==UnityEngine.Rendering.ShaderPropertyType.Vector)body.SetVector(name,face.GetVector(name));else body.SetFloat(name,face.GetFloat(name));
}
body.SetVector("_MainTexHSVG",new Vector4(.025f,.35f,1.03f,1f));body.SetColor("_Color",new Color(1f,.975f,.93f,1));
if(create)AssetDatabase.CreateAsset(body,path);else EditorUtility.SetDirty(body);
var profilePath="Assets/10.Datas/Actor/ExternalModels/Bokusei_P09_ToonLook.asset";
var profile=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09ToonLookProfileSO>(profilePath);bool createProfile=profile==null;if(createProfile)profile=ScriptableObject.CreateInstance<UPlayGround.Editor.P09Builder.P09ToonLookProfileSO>();
profile.bindings=new[]{new UPlayGround.Editor.P09Builder.P09ToonLookProfileSO.MaterialBinding{rendererPrefix="",source=source,replacement=body}};
if(createProfile)AssetDatabase.CreateAsset(profile,profilePath);else EditorUtility.SetDirty(profile);profile.ValidateBindings();
foreach(string suffix in new[]{"","_Alternate"}){
var preset=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.BuildPreset>("Assets/10.Datas/Actor/ExternalModels/Bokusei_P09"+suffix+"_BuildPreset.asset");preset.config.ToonLookProfile=profile;EditorUtility.SetDirty(preset);
var model="Assets/03.Prefabs/Characters/ExternalModels/Bokusei_P09"+(suffix==""?"Compatible":suffix)+".prefab";
var root=PrefabUtility.LoadPrefabContents(model);try{profile.Apply(root);PrefabUtility.SaveAsPrefabAsset(root,model);}finally{PrefabUtility.UnloadPrefabContents(root);}
}
AssetDatabase.SaveAssets();return profilePath;
