var modelPath="Assets/03.Prefabs/Characters/ExternalModels/Hichi_P09Compatible.prefab";
var presetPath="Assets/10.Datas/Actor/ExternalModels/Hichi_P09_BuildPreset.asset";
if(UnityEditor.AssetDatabase.LoadMainAssetAtPath(modelPath)!=null||UnityEditor.AssetDatabase.LoadMainAssetAtPath(presetPath)!=null)throw new System.Exception("기존 모델 보존");
var cat=new UPlayGround.Editor.P09Builder.P09AssetCatalog();cat.Refresh();
var preset=UnityEngine.ScriptableObject.CreateInstance<UPlayGround.Editor.P09Builder.BuildPreset>();
var config=new UPlayGround.Editor.P09Builder.CharacterBuildConfig();preset.config=config;
config.ActorKind=UPlayGround.Editor.P09Builder.BuilderActorKind.Npc;config.Sex=UPlayGround.Editor.P09Builder.BuilderSex.Female;
config.UseMagicaCloth=false;config.UseManualName=true;config.ManualName="Hichi_P09";config.SaveBaseFolder="Assets/03.Prefabs/Characters/ExternalModels";
config.Stats.npcDisplayName="Hichi 기반 신규 인물";config.Stats.npcDescription="";
config.HairStyleSo=cat.HairStyles.Single(s=>s.name=="HairStyleEditPartData_01");
config.HairColorSo=cat.HairColors.Single(s=>s.name=="HairColorEditPartData_01");
config.FaceTypeSo=cat.FaceTypes.First();config.SkinColorSo=cat.SkinColorsFemale.Single(s=>s.name=="SkinColorEditPartData_Famale_01");
config.ArmorSelections.Set(UPlayGround.Editor.P09Builder.BuilderArmorSlot.Chest,cat.Chests.Single(s=>s.name=="ChestEditPartData_03"));
config.ArmorSelections.Set(UPlayGround.Editor.P09Builder.BuilderArmorSlot.Arm,cat.Arms.Single(s=>s.name=="ArmEditPartData_03"));
config.ArmorSelections.Set(UPlayGround.Editor.P09Builder.BuilderArmorSlot.Waist,cat.Waists.Single(s=>s.name=="WaistEditPartData_03"));
config.ArmorSelections.Set(UPlayGround.Editor.P09Builder.BuilderArmorSlot.Leg,cat.Legs.Single(s=>s.name=="LegEditPartData_03"));
config.ToonLookProfile=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.BuildPreset>("Assets/10.Datas/Actor/ExternalModels/Daon_P09_BuildPreset.asset").config.ToonLookProfile;
config.FaceMeshProfile=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/Hichi_P09_Face.asset");
var errors=config.Validate().ToArray();if(errors.Length>0)throw new System.Exception(string.Join(";",errors));
preset.description="Hichi 얼굴 토폴로지를 새로 조형한 P09 여성 모델. 신규 인물의 역할·월드 배치 전용 데이터는 별도 지정한다.";
var root=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics));
try
{
    var ctx=new UPlayGround.Editor.P09Builder.BuildContext(config);ctx.RootInstance=root;
    new UPlayGround.Editor.P09Builder.ApplyAppearanceStep().Execute(ctx);root.name="Hichi_P09Compatible";
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,modelPath);
    UnityEditor.AssetDatabase.CreateAsset(preset,presetPath);UnityEditor.AssetDatabase.SaveAssets();
    return new{model=modelPath,preset=presetPath,activeRenderers=root.GetComponentsInChildren<UnityEngine.Renderer>().Count(r=>r.enabled)};
}
finally{UnityEngine.Object.DestroyImmediate(root);}
