var presetPath="Assets/10.Datas/Actor/ExternalModels/Reine_P09_BuildPreset.asset";
var modelPath="Assets/03.Prefabs/Characters/ExternalModels/Reine_P09Compatible.prefab";
if(AssetDatabase.LoadMainAssetAtPath(presetPath)!=null||AssetDatabase.LoadMainAssetAtPath(modelPath)!=null)throw new Exception("기존 모델을 보존합니다.");
var preset=ScriptableObject.CreateInstance<UPlayGround.Editor.P09Builder.BuildPreset>();
var c=new UPlayGround.Editor.P09Builder.CharacterBuildConfig();preset.config=c;
c.ActorKind=UPlayGround.Editor.P09Builder.BuilderActorKind.Npc;c.Sex=UPlayGround.Editor.P09Builder.BuilderSex.Female;
c.UseMagicaCloth=false;c.UseManualName=true;c.ManualName="Reine_P09_NewFace";c.SaveBaseFolder="Assets/03.Prefabs/Characters/ExternalModels";
c.FaceMeshProfile=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09FaceMeshProfileSO>("Assets/10.Datas/Actor/ExternalModels/Reine_P09_Face.asset");
string cat=UPlayGround.Editor.P09Builder.PathConfig.CatalogRoot;
Func<string,int,ScriptableObject> part=(kind,index)=>AssetDatabase.LoadAssetAtPath<ScriptableObject>(cat+"/"+kind+"/"+kind+"EditPartData_"+index.ToString("00")+".asset");
c.HairStyleSo=part("HairStyle",3);c.HairColorSo=part("HairColor",4);c.FaceTypeSo=AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath("b6600d193d78a4b76bacbae62e649d6d"));
c.SkinColorSo=part("SkinColor",1);c.EyeColorSo=part("EyeColor",1);
c.ArmorSelections.Set(UPlayGround.Editor.P09Builder.BuilderArmorSlot.Chest,part("Chest",3));
c.ArmorSelections.Set(UPlayGround.Editor.P09Builder.BuilderArmorSlot.Arm,part("Arm",3));
c.ArmorSelections.Set(UPlayGround.Editor.P09Builder.BuilderArmorSlot.Waist,part("Waist",3));
c.ArmorSelections.Set(UPlayGround.Editor.P09Builder.BuilderArmorSlot.Leg,part("Leg",3));
c.ToonLookProfile=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09ToonLookProfileSO>("Assets/10.Datas/Actor/ScenarioResidents/ToonLook/P09_Residents_ToonLook.asset"); c.Stats.npcDisplayName="Reine P09";
preset.description="Reine 토폴로지에서 새로 조형한 얼굴. P09 여성 바디·헤어 03·의상 03. 기존 Reine 액터와 독립된 모델 제작 프리셋.";
var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(UPlayGround.Editor.P09Builder.PathConfig.BasePrefabFemaleNoPhysics));
try{PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
var context=new UPlayGround.Editor.P09Builder.BuildContext(c);context.RootInstance=root;new UPlayGround.Editor.P09Builder.ApplyAppearanceStep().Execute(context);
root.name="Reine_P09Compatible";
foreach(var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r=>r.sharedMesh==c.FaceMeshProfile.mesh)){var b=r.localBounds;b.Expand(.06f);r.localBounds=b;}
PrefabUtility.SaveAsPrefabAsset(root,modelPath);AssetDatabase.CreateAsset(preset,presetPath);AssetDatabase.SaveAssets();return new{model=modelPath,preset=presetPath,errors=c.Validate().ToArray()};
}finally{UnityEngine.Object.DestroyImmediate(root);}

