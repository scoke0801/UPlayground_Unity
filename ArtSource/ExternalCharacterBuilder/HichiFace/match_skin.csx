// 공유 다온 재질을 수정하지 않고 새 얼굴에 맞는 피부 재질과 톤 프로파일을 따로 보관한다.
string folder="Assets/08.Models/Characters/HichiP09";
string profilePath="Assets/10.Datas/Actor/ExternalModels/Hichi_P09_ToonLook.asset";
var original=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.BuildPreset>("Assets/10.Datas/Actor/ExternalModels/Daon_P09_BuildPreset.asset").config.ToonLookProfile;
var look=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09ToonLookProfileSO>(profilePath);
var skin=AssetDatabase.LoadAssetAtPath<Material>(folder+"/Hichi_P09_Skin.mat");
if(skin==null){skin=new Material(original.bindings.Single(b=>b.rendererPrefix==""&&b.source.name=="P09_Female_Skin").replacement);skin.name="Hichi_P09_Skin";AssetDatabase.CreateAsset(skin,folder+"/Hichi_P09_Skin.mat");}
skin.SetColor("_Color",Color.white);skin.SetVector("_MainTexHSVG",new Vector4(.025f,.45f,1.06f,1));skin.SetFloat("_ShadowStrength",.55f);skin.SetFloat("_ShadowBlur",.35f);
if(look==null){look=UnityEngine.Object.Instantiate(original);look.name="Hichi_P09_ToonLook";AssetDatabase.CreateAsset(look,profilePath);}
foreach(var binding in look.bindings)if(binding.rendererPrefix==""&&binding.source.name=="P09_Female_Skin")binding.replacement=skin;
look.ValidateBindings();EditorUtility.SetDirty(skin);AssetDatabase.SaveAssetIfDirty(skin);EditorUtility.SetDirty(look);AssetDatabase.SaveAssetIfDirty(look);
foreach(string name in new[]{"Hichi_P09","Hichi_P09_Alternate"}){
    var preset=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.BuildPreset>("Assets/10.Datas/Actor/ExternalModels/"+name+"_BuildPreset.asset");preset.config.ToonLookProfile=look;EditorUtility.SetDirty(preset);AssetDatabase.SaveAssetIfDirty(preset);
    string path="Assets/03.Prefabs/Characters/ExternalModels/"+(name=="Hichi_P09"?"Hichi_P09Compatible":name)+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
    try{
        foreach(var r in root.GetComponentsInChildren<Renderer>(true)){var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++){var binding=original.bindings.FirstOrDefault(b=>b.replacement==mats[i]);if(binding!=null)mats[i]=binding.source;}r.sharedMaterials=mats;}
        var ctx=new UPlayGround.Editor.P09Builder.BuildContext(preset.config);ctx.RootInstance=root;new UPlayGround.Editor.P09Builder.ApplyAppearanceStep().Execute(ctx);PrefabUtility.SaveAsPrefabAsset(root,path);
    }finally{PrefabUtility.UnloadPrefabContents(root);}
}
return profilePath;
