var dataFolder = "Assets/10.Datas/Actor/ExternalModels";
var modelFolder = "Assets/08.Models/Equipment/TravelerPouch";
System.IO.Directory.CreateDirectory(dataFolder);
UnityEditor.AssetDatabase.Refresh();
var pouchPath = modelFolder + "/TravelerPouch.prefab";
var pouchPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(pouchPath);
if (pouchPrefab == null)
{
    var source = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(modelFolder + "/TravelerPouch.fbx");
    var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
    try
    {
        var instance = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(source, scene);
        UnityEditor.PrefabUtility.UnpackPrefabInstance(instance, UnityEditor.PrefabUnpackMode.Completely, UnityEditor.InteractionMode.AutomatedAction);
        foreach (var renderer in instance.GetComponentsInChildren<UnityEngine.Renderer>(true))
        {
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                var name = materials[i].name;
                var path = modelFolder + "/" + name + ".mat";
                var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
                if (mat == null)
                {
                    mat = new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit"));
                    var color = name.Contains("Navy") ? new UnityEngine.Color(0.07f,0.10f,0.14f) : name.Contains("Brown") ? new UnityEngine.Color(0.30f,0.13f,0.08f) : new UnityEngine.Color(0.50f,0.35f,0.13f);
                    mat.SetColor("_BaseColor", color);
                    mat.SetFloat("_Smoothness", 0.22f);
                    mat.SetFloat("_Metallic", name.Contains("Brass") ? 0.6f : 0);
                    UnityEditor.AssetDatabase.CreateAsset(mat, path);
                }
                materials[i] = mat;
            }
            renderer.sharedMaterials = materials;
        }
        pouchPrefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(instance, pouchPath);
    }
    finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
}
var results = new System.Collections.Generic.List<string>();
var honokaPath = dataFolder + "/Honoka_Traveler.asset";
var honoka = UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.ExternalCharacterProfile>(honokaPath);
if (honoka == null)
{
    honoka = UnityEngine.ScriptableObject.CreateInstance<UPlayGround.Editor.P09Builder.ExternalCharacterProfile>();
    honoka.sourcePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/ExternalAssets/Character/Honoka/Prefab/Honoka_alt1.prefab");
    honoka.authoringNotes = "원본 alt1 얼굴·체형·재질 유지. 같은 리그의 코트/장갑/목장식/머리카락과 Blender 제작 파우치를 교체합니다. 정규 다온은 변경하지 않습니다.";
    honoka.slots = new[]
    {
        new UPlayGround.Editor.P09Builder.ExternalCharacterProfile.EquipmentSlot { id="Coat", variants=new[] {
            new UPlayGround.Editor.P09Builder.ExternalCharacterProfile.EquipmentVariant { id="Coat", rendererPaths=new[]{"Coat","Coat/CoatBelt"}},
            new UPlayGround.Editor.P09Builder.ExternalCharacterProfile.EquipmentVariant { id="NoCoat"}}},
        new UPlayGround.Editor.P09Builder.ExternalCharacterProfile.EquipmentSlot { id="Gloves", variants=new[] {
            new UPlayGround.Editor.P09Builder.ExternalCharacterProfile.EquipmentVariant { id="Gauntlet", rendererPaths=new[]{"Gauntlet"}},
            new UPlayGround.Editor.P09Builder.ExternalCharacterProfile.EquipmentVariant { id="NoGauntlet"}}},
        new UPlayGround.Editor.P09Builder.ExternalCharacterProfile.EquipmentSlot { id="Neck", variants=new[] {
            new UPlayGround.Editor.P09Builder.ExternalCharacterProfile.EquipmentVariant { id="Neckwear", rendererPaths=new[]{"Neckwear","Magatama"}},
            new UPlayGround.Editor.P09Builder.ExternalCharacterProfile.EquipmentVariant { id="NoNeckwear"}}},
        new UPlayGround.Editor.P09Builder.ExternalCharacterProfile.EquipmentSlot { id="Hair", variants=new[] {
            new UPlayGround.Editor.P09Builder.ExternalCharacterProfile.EquipmentVariant { id="Short", rendererPaths=new[]{"Body/Hair_back"}},
            new UPlayGround.Editor.P09Builder.ExternalCharacterProfile.EquipmentVariant { id="Long", rendererPaths=new[]{"Body/Hair_back_long"}}}},
        new UPlayGround.Editor.P09Builder.ExternalCharacterProfile.EquipmentSlot { id="Pouch", variants=new[] {
            new UPlayGround.Editor.P09Builder.ExternalCharacterProfile.EquipmentVariant { id="TravelerPouch", attachmentPrefab=pouchPrefab, attachmentBone=UnityEngine.HumanBodyBones.Hips, localPosition=new UnityEngine.Vector3(0.17f,0.015f,0.115f), localEulerAngles=new UnityEngine.Vector3(0,20,0)},
            new UPlayGround.Editor.P09Builder.ExternalCharacterProfile.EquipmentVariant { id="NoPouch"}}}
    };
    UnityEditor.AssetDatabase.CreateAsset(honoka, honokaPath);
}
var katanaPath = dataFolder + "/Katana_Traveler.asset";
var katana = UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.ExternalCharacterProfile>(katanaPath);
if (katana == null)
{
    katana = UnityEngine.ScriptableObject.CreateInstance<UPlayGround.Editor.P09Builder.ExternalCharacterProfile>();
    katana.sourcePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/ExternalAssets/Character/CombatGirlsCharacterPack/School_Katana_Girl/Prefab/School_Katana_FullBody-Magica cloth2.prefab");
    katana.authoringNotes = "원본 얼굴과 헤어 유지. 의상 전환은 해당 Body 마스크와 신발을 함께 전환합니다. 스포츠 의상과 교복을 동시에 켜지 않습니다. 무기는 시각 샘플이며 전투 무기 연결을 의미하지 않습니다.";
    katana.slots = new[] {
        new UPlayGround.Editor.P09Builder.ExternalCharacterProfile.EquipmentSlot { id="Outfit", variants=new[] {
            new UPlayGround.Editor.P09Builder.ExternalCharacterProfile.EquipmentVariant { id="School", rendererPaths=new[]{"SchoolUniform/Body_School_uniform","SchoolUniform/Cloth_School_uniform_Shoes","SchoolUniform/Cloth_School_uniform_Skirt","SchoolUniform/Cloth_School_uniform_Top","SchoolUniform/Colth_School_uniform_Armband"}},
            new UPlayGround.Editor.P09Builder.ExternalCharacterProfile.EquipmentVariant { id="Sportswear", rendererPaths=new[]{"SportsWear/Body_Sportswear","SportsWear/Sportswear_Pants","SportsWear/Sportswear_Shoes","SportsWear/Sportswear_Top"}}}},
        new UPlayGround.Editor.P09Builder.ExternalCharacterProfile.EquipmentSlot { id="Glasses", variants=new[] {
            new UPlayGround.Editor.P09Builder.ExternalCharacterProfile.EquipmentVariant { id="Glasses", rendererPaths=new[]{"Glass/Acc_Glass"}},
            new UPlayGround.Editor.P09Builder.ExternalCharacterProfile.EquipmentVariant { id="NoGlasses"}}}
    };
    UnityEditor.AssetDatabase.CreateAsset(katana, katanaPath);
}
honoka.slots[4].variants[0].localPosition = new UnityEngine.Vector3(-0.14f,0.03f,0.19f);
honoka.slots[4].variants[0].localEulerAngles = new UnityEngine.Vector3(0,-10,0);
UnityEditor.EditorUtility.SetDirty(honoka);
var replacements = new System.Collections.Generic.List<UPlayGround.Editor.P09Builder.ExternalCharacterProfile.MaterialReplacement>();
foreach (var sourceMaterial in katana.sourcePrefab.GetComponentsInChildren<UnityEngine.Renderer>(true).SelectMany(r=>r.sharedMaterials).Distinct())
{
    if(sourceMaterial==null || sourceMaterial.shader.name!="Hidden/InternalErrorShader") continue;
    string path=dataFolder+"/Katana_"+sourceMaterial.name.Replace(" ","_")+".mat";
    var material=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
    if(material==null)
    {
        var shader=UnityEngine.Shader.Find(sourceMaterial.name.Contains("Hair")?"Hidden/lilToonCutout":"lilToon");
        if(shader==null) throw new System.Exception("재질 셰이더 누락");
        material=new UnityEngine.Material(shader);
        material.SetTexture("_MainTex",sourceMaterial.mainTexture);
        material.SetColor("_Color",UnityEngine.Color.white);
        if(sourceMaterial.name.Contains("Hair")) { material.SetFloat("_Cutoff",0.5f); material.renderQueue=2450; }
        UnityEditor.AssetDatabase.CreateAsset(material,path);
    }
    replacements.Add(new UPlayGround.Editor.P09Builder.ExternalCharacterProfile.MaterialReplacement{source=sourceMaterial,replacement=material});
}
katana.materialReplacements=replacements.ToArray();
if(katana.slots.Length==2)
{
    var slots=katana.slots.ToList();
    slots.Add(new UPlayGround.Editor.P09Builder.ExternalCharacterProfile.EquipmentSlot {id="WeaponProps",variants=new[]{
        new UPlayGround.Editor.P09Builder.ExternalCharacterProfile.EquipmentVariant{id="Hidden"},
        new UPlayGround.Editor.P09Builder.ExternalCharacterProfile.EquipmentVariant{id="SourceProps",rendererPaths=new[]{
            "root/add_weapon_l/Weapon_Katana_01_sheath/Weapon_Katana_01_sheath",
            "root/add_weapon_l/Weapon_Katana_02_sheath/Weapon_Katana02_sheath",
            "root/add_weapon_r/Weapon_Katana_01_Blade/Weapon_Katana_01_Blade",
            "root/add_weapon_r/Weapon_Katana_02_Blade/Weapon_Katana02_Blade",
            "Weapon/Weapon_Katana","Weapon/Weapon_Katana_sheath"}}}});
    katana.slots=slots.ToArray();
}
UnityEditor.EditorUtility.SetDirty(katana);
foreach (var profile in new[] {honoka,katana})
{
    var path=UPlayGround.Editor.P09Builder.ExternalCharacterAssembler.OutputFolder+"/"+profile.name+"_v2.prefab";
    if (UnityEditor.AssetDatabase.LoadMainAssetAtPath(path)==null)
        UPlayGround.Editor.P09Builder.ExternalCharacterAssembler.Build(profile,path);
    results.Add(path);
}
UnityEditor.AssetDatabase.SaveAssets();
return results;
