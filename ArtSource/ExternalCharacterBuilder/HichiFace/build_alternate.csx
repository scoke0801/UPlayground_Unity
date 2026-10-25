var source=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.BuildPreset>("Assets/10.Datas/Actor/ExternalModels/Hichi_P09_BuildPreset.asset");
var presetPath="Assets/10.Datas/Actor/ExternalModels/Hichi_P09_Alternate_BuildPreset.asset";
var modelPath="Assets/03.Prefabs/Characters/ExternalModels/Hichi_P09_Alternate.prefab";
if(UnityEditor.AssetDatabase.LoadMainAssetAtPath(modelPath)!=null) return "대체 모델이 이미 존재합니다.";
var preset=UnityEngine.ScriptableObject.CreateInstance<UPlayGround.Editor.P09Builder.BuildPreset>();
preset.config=UnityEngine.JsonUtility.FromJson<UPlayGround.Editor.P09Builder.CharacterBuildConfig>(UnityEngine.JsonUtility.ToJson(source.config));
var cat=new UPlayGround.Editor.P09Builder.P09AssetCatalog();cat.Refresh();var config=preset.config;
config.HairStyleSo=cat.HairStyles.Single(s=>s.name=="HairStyleEditPartData_13");
config.ArmorSelections.Set(UPlayGround.Editor.P09Builder.BuilderArmorSlot.Chest,cat.Chests.Single(s=>s.name=="ChestEditPartData_11"));
config.ArmorSelections.Set(UPlayGround.Editor.P09Builder.BuilderArmorSlot.Arm,cat.Arms.Single(s=>s.name=="ArmEditPartData_11"));
config.ArmorSelections.Set(UPlayGround.Editor.P09Builder.BuilderArmorSlot.Waist,cat.Waists.Single(s=>s.name=="WaistEditPartData_11"));
config.ArmorSelections.Set(UPlayGround.Editor.P09Builder.BuilderArmorSlot.Leg,cat.Legs.Single(s=>s.name=="LegEditPartData_11"));
config.ManualName="Hichi_P09_Alternate";config.Stats.npcDisplayName="Hichi 기반 새 인물";source.config.Stats.npcDisplayName="Hichi 기반 새 인물";
preset.description="Hair_13 + Armor_011 대체 조합. 기본 모델과 같은 얼굴 프로파일을 사용한다. 표시 이름은 임시 이름이다.";
var root=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics));
try
{
    var ctx=new UPlayGround.Editor.P09Builder.BuildContext(config);ctx.RootInstance=root;new UPlayGround.Editor.P09Builder.ApplyAppearanceStep().Execute(ctx);
    root.name="Hichi_P09_Alternate";UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,modelPath);
    UnityEditor.AssetDatabase.CreateAsset(preset,presetPath);UnityEditor.EditorUtility.SetDirty(source);UnityEditor.AssetDatabase.SaveAssetIfDirty(source);UnityEditor.AssetDatabase.SaveAssetIfDirty(preset);
    return new{model=modelPath,preset=presetPath};
}
finally{UnityEngine.Object.DestroyImmediate(root);}
