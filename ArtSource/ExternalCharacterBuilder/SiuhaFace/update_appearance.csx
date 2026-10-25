// 눈을 가리지 않는 기본 헤어와 같은 얼굴을 쓰는 대체 조합을 저장한다.
foreach(string suffix in new[]{"","_Alternate"}){
var preset=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.BuildPreset>("Assets/10.Datas/Actor/ExternalModels/Siuha_P09"+suffix+"_BuildPreset.asset");
int hair=suffix==""?3:11;
preset.config.HairStyleSo=AssetDatabase.LoadAssetAtPath<ScriptableObject>(UPlayGround.Editor.P09Builder.PathConfig.CatalogRoot+"/HairStyle/HairStyleEditPartData_"+hair.ToString("00")+".asset");
preset.description="Siuha 기반 소형 눈 조형 얼굴. P09 여성 바디·갈색 헤어 "+hair+". 독립 모델 제작 프리셋; 기존 액터를 교체하지 않음.";
var path="Assets/03.Prefabs/Characters/ExternalModels/Siuha_P09"+(suffix==""?"Compatible":suffix)+".prefab";
var root=PrefabUtility.LoadPrefabContents(path);
try{var ctx=new UPlayGround.Editor.P09Builder.BuildContext(preset.config);ctx.RootInstance=root;new UPlayGround.Editor.P09Builder.ApplyAppearanceStep().Execute(ctx);PrefabUtility.SaveAsPrefabAsset(root,path);EditorUtility.SetDirty(preset);}
finally{PrefabUtility.UnloadPrefabContents(root);}
}
AssetDatabase.SaveAssets();return "기본 Hair03 / 대체 Hair11 저장";
