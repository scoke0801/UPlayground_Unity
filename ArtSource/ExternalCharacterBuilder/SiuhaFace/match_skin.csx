// 생성한 피부 알베도와 전용 몸 재질을 연결한다. 공용 P09 재질은 보존한다.
string folder="Assets/08.Models/Characters/SiuhaP09";
AssetDatabase.ImportAsset(folder+"/Siuha_Face_Albedo.png",ImportAssetOptions.ForceSynchronousImport);
var importer=(TextureImporter)AssetImporter.GetAtPath(folder+"/Siuha_Face_Albedo.png");
importer.sRGBTexture=true;importer.mipmapEnabled=true;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Siuha_Face_Albedo.png");
var common=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09ToonLookProfileSO>("Assets/10.Datas/Actor/ScenarioResidents/ToonLook/P09_Residents_ToonLook.asset");
var originalBody=AssetDatabase.LoadAssetAtPath<Material>("Assets/ExternalAssets/Character/P09_Modular_Humanoid/Model_DATA/Materials/Female_Body/P09_Female_Skin.mat");
var commonBody=AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/Actor/ScenarioResidents/ToonLook/P09_Female_Skin_Toon.mat");
if(originalBody==null||commonBody==null)throw new Exception("P09 원본 몸 재질 누락");
foreach(string name in new[]{"Siuha_P09_suiha_face","Siuha_P09_suiha_face_alpha"}){
var face=AssetDatabase.LoadAssetAtPath<Material>(folder+"/"+name+".mat");face.mainTexture=texture;
for(int i=0;i<commonBody.shader.GetPropertyCount();i++){
string property=commonBody.shader.GetPropertyName(i);if(!face.HasProperty(property))continue;
if(!property.StartsWith("_Shadow")&&!property.StartsWith("_Light")&&property!="_MonochromeLighting"&&property!="_AsUnlit"&&property!="_VertexLightStrength"&&property!="_UseShadow")continue;
var type=commonBody.shader.GetPropertyType(i);if(type==UnityEngine.Rendering.ShaderPropertyType.Texture)continue;
if(type==UnityEngine.Rendering.ShaderPropertyType.Color||type==UnityEngine.Rendering.ShaderPropertyType.Vector)face.SetVector(property,commonBody.GetVector(property));else face.SetFloat(property,commonBody.GetFloat(property));
}
face.SetFloat("_UseEmission",0);face.SetFloat("_UseEmission2nd",0);face.SetFloat("_OutlineWidth",0);EditorUtility.SetDirty(face);
}
string bodyPath=folder+"/Siuha_P09_Skin.mat";var body=AssetDatabase.LoadAssetAtPath<Material>(bodyPath);bool newBody=body==null;if(newBody)body=new Material(commonBody);body.name="Siuha_P09_Skin";
var readable=new Texture2D(2,2,TextureFormat.RGBA32,false);Color color;
try{readable.LoadImage(System.IO.File.ReadAllBytes(folder+"/Siuha_Face_Albedo.png"));color=readable.GetPixelBilinear(.5f,.15f);}finally{UnityEngine.Object.DestroyImmediate(readable);}
color.a=.95f;body.SetFloat("_UseMain2ndTex",1);body.SetTexture("_Main2ndTex",null);body.SetTexture("_Main2ndBlendMask",null);body.SetColor("_Color2nd",color);body.SetFloat("_Main2ndTexBlendMode",0);body.SetFloat("_Main2ndTexAlphaMode",0);body.SetFloat("_Main2ndEnableLighting",1);body.SetFloat("_Main2ndTexIsDecal",0);body.SetFloat("_OutlineWidth",0);
if(newBody)AssetDatabase.CreateAsset(body,bodyPath);else EditorUtility.SetDirty(body);
string toonPath="Assets/10.Datas/Actor/ExternalModels/Siuha_P09_ToonLook.asset";
var toon=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09ToonLookProfileSO>(toonPath);bool newToon=toon==null;if(newToon)toon=ScriptableObject.CreateInstance<UPlayGround.Editor.P09Builder.P09ToonLookProfileSO>();
toon.bindings=common.bindings.Select(b=>new UPlayGround.Editor.P09Builder.P09ToonLookProfileSO.MaterialBinding{source=b.source,rendererPrefix=b.rendererPrefix,replacement=b.source==originalBody&&string.IsNullOrEmpty(b.rendererPrefix)?body:b.replacement}).ToArray();
toon.ValidateBindings();if(newToon)AssetDatabase.CreateAsset(toon,toonPath);else EditorUtility.SetDirty(toon);
var faceProfile=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/Siuha_P09_Face.asset");faceProfile.materials[2]=body;EditorUtility.SetDirty(faceProfile);
foreach(string suffix in new[]{"","_Alternate"}){
var preset=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.BuildPreset>("Assets/10.Datas/Actor/ExternalModels/Siuha_P09"+suffix+"_BuildPreset.asset");preset.config.ToonLookProfile=toon;EditorUtility.SetDirty(preset);
var path="Assets/03.Prefabs/Characters/ExternalModels/Siuha_P09"+(suffix==""?"Compatible":suffix)+".prefab";
var root=PrefabUtility.LoadPrefabContents(path);try{var context=new UPlayGround.Editor.P09Builder.BuildContext(preset.config);context.RootInstance=root;new UPlayGround.Editor.P09Builder.ApplyAppearanceStep().Execute(context);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
}
AssetDatabase.SaveAssets();return new {texture=texture.name,color=color.ToString(),toon=toonPath};
