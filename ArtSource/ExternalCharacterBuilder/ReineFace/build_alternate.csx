var primary=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.BuildPreset>("Assets/10.Datas/Actor/ExternalModels/Reine_P09_BuildPreset.asset");
var config=JsonUtility.FromJson<UPlayGround.Editor.P09Builder.CharacterBuildConfig>(JsonUtility.ToJson(primary.config));
foreach(var slot in new[]{UPlayGround.Editor.P09Builder.BuilderArmorSlot.Chest,UPlayGround.Editor.P09Builder.BuilderArmorSlot.Arm,UPlayGround.Editor.P09Builder.BuilderArmorSlot.Waist,UPlayGround.Editor.P09Builder.BuilderArmorSlot.Leg})config.ArmorSelections.Set(slot,AssetDatabase.LoadAssetAtPath<ScriptableObject>(UPlayGround.Editor.P09Builder.PathConfig.CatalogRoot+"/"+slot+"/"+slot+"EditPartData_05.asset"));
config.HairStyleSo=AssetDatabase.LoadAssetAtPath<ScriptableObject>(UPlayGround.Editor.P09Builder.PathConfig.CatalogRoot+"/HairStyle/HairStyleEditPartData_11.asset");config.ManualName="Reine_P09_Alternate";
var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics));
try{var ctx=new UPlayGround.Editor.P09Builder.BuildContext(config);ctx.RootInstance=root;new UPlayGround.Editor.P09Builder.ApplyAppearanceStep().Execute(ctx);root.name=config.ManualName;
PrefabUtility.SaveAsPrefabAsset(root,"Assets/03.Prefabs/Characters/ExternalModels/Reine_P09_Alternate.prefab");
string path="Assets/10.Datas/Actor/ExternalModels/Reine_P09_Alternate_BuildPreset.asset";var preset=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.BuildPreset>(path);bool create=preset==null;if(create)preset=ScriptableObject.CreateInstance<UPlayGround.Editor.P09Builder.BuildPreset>();preset.config=config;preset.description="동일 얼굴의 P09 단발 11·방어구 05 호환 검수 조합. 물리·전투 통합은 미검증.";if(create)AssetDatabase.CreateAsset(preset,path);else EditorUtility.SetDirty(preset);AssetDatabase.SaveAssets();return path;
}finally{UnityEngine.Object.DestroyImmediate(root);}

