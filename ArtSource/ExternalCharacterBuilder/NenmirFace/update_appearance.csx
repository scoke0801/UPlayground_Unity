var preset=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.BuildPreset>("Assets/10.Datas/Actor/ExternalModels/Nenmir_P09_BuildPreset.asset");
preset.config.HairColorSo=AssetDatabase.LoadAssetAtPath<ScriptableObject>(UPlayGround.Editor.P09Builder.PathConfig.CatalogRoot+"/HairColor/HairColorEditPartData_01.asset");
preset.config.ToonLookProfile=AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09ToonLookProfileSO>("Assets/10.Datas/Actor/ScenarioResidents/ToonLook/P09_Residents_ToonLook.asset");
foreach(var slot in new[]{UPlayGround.Editor.P09Builder.BuilderArmorSlot.Chest,UPlayGround.Editor.P09Builder.BuilderArmorSlot.Arm,UPlayGround.Editor.P09Builder.BuilderArmorSlot.Waist,UPlayGround.Editor.P09Builder.BuilderArmorSlot.Leg})preset.config.ArmorSelections.Set(slot,AssetDatabase.LoadAssetAtPath<ScriptableObject>(UPlayGround.Editor.P09Builder.PathConfig.CatalogRoot+"/"+slot+"/"+slot+"EditPartData_03.asset"));
preset.description="Nenmir 기반 새 얼굴. P09 여성 바디, 갈색 헤어 03, 청록 튜닉 03, 작은 청보라색 눈. 기존 Nenmir와 독립된 모델 제작용 프리셋.";
var path="Assets/03.Prefabs/Characters/ExternalModels/Nenmir_P09Compatible.prefab";
var root=PrefabUtility.LoadPrefabContents(path);
try{var ctx=new UPlayGround.Editor.P09Builder.BuildContext(preset.config);ctx.RootInstance=root;new UPlayGround.Editor.P09Builder.ApplyAppearanceStep().Execute(ctx);PrefabUtility.SaveAsPrefabAsset(root,path);EditorUtility.SetDirty(preset);AssetDatabase.SaveAssets();return path;}
finally{PrefabUtility.UnloadPrefabContents(root);}

