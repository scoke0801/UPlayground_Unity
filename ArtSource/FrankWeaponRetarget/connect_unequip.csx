// 기존 RAON 보정 리그를 재사용하고 납도 슬롯만 교체한다.
const string sourcePath = "Assets/ExternalAssets/AnimationOnly/Frank_Slash_Pack/Assets/Animations/Frank_SlashPack_Katana/FBX_Animation/In_Place/Frank_RPG_Katana_Unequip.FBX";
const string clipPath = "Assets/07.Animations/Player/Raon/FrankWeapon/Raon_Frank_RPG_Katana_Unequip.anim";
const string motionPath = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Katana/FrankWeapon/Raon_Katana_Unequip_FrankWeapon.asset";
const string setPath = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Bokusei_KatanaAnimationSet.asset";
if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
    throw new System.InvalidOperationException("에디터 컴파일과 재생을 마친 뒤 실행하세요.");
if (AssetDatabase.LoadMainAssetAtPath(clipPath) != null || AssetDatabase.LoadMainAssetAtPath(motionPath) != null)
    throw new System.InvalidOperationException("이미 생성된 납도 에셋을 먼저 확인하세요.");
var clips = AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<AnimationClip>().ToArray();
var body = clips.Single(c => !c.name.StartsWith("__"));
var extra = clips.Single(c => c.name.StartsWith("__preview__"));
var reference = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/07.Animations/Player/Raon/FrankWeapon/Raon_Frank_RPG_Katana_S1_Combo01_01.anim");
var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Raon.prefab");
var set = AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.Animation.ActorAnimationMotionSet>(setPath);
var slot = UPlayGround.Data.Actor.Animation.MotionTags.UnEquip_Weapon;
var previous = set.motionSlots[slot];
var result = UnityEngine.Object.Instantiate(body);
result.name = "Raon_Frank_RPG_Katana_Unequip";
var extraBindings = AnimationUtility.GetCurveBindings(extra);
var referenceBindings = AnimationUtility.GetCurveBindings(reference);
string[] bones = { "R_Hand_Weapon", "Weapon_Sword", "Weapon_Blade", "Sword_Case", "Weapon_Sword_Dummy" };
foreach (var binding in AnimationUtility.GetCurveBindings(result).Where(b => b.type == typeof(Transform)))
    AnimationUtility.SetEditorCurve(result, binding, null);
foreach (var bone in bones) {
    string targetPath = referenceBindings.Select(b => b.path).Distinct().Single(p => p.EndsWith("/" + bone));
    string originalPath = extraBindings.Select(b => b.path).Distinct().Single(p => p.EndsWith("/" + bone));
    if (prefab.transform.Find(targetPath) == null) throw new System.InvalidOperationException("대상 본 누락: " + targetPath);
    foreach (var binding in referenceBindings.Where(b => b.type == typeof(Transform) && b.path == targetPath)) {
        var sourceBinding = EditorCurveBinding.FloatCurve(originalPath, typeof(Transform), binding.propertyName);
        var value = AnimationUtility.GetEditorCurve(extra, sourceBinding);
        if (value == null) value = AnimationCurve.Constant(0, body.length, AnimationUtility.GetEditorCurve(reference, binding).Evaluate(0));
        AnimationUtility.SetEditorCurve(result, binding, value);
    }
}
foreach (string axis in new[] { "x", "y", "z" }) {
    string property = "m_LocalScale." + axis;
    var value = new AnimationCurve();
    for (int frame = 0; frame <= Mathf.CeilToInt(body.length * body.frameRate); frame++) {
        float time = Mathf.Min(frame / body.frameRate, body.length);
        float scale = 1;
        foreach (string bone in bones.Take(3)) {
            string p = extraBindings.Select(b => b.path).Distinct().Single(x => x.EndsWith("/" + bone));
            string r = referenceBindings.Select(b => b.path).Distinct().Single(x => x.EndsWith("/" + bone));
            var c = AnimationUtility.GetEditorCurve(extra, EditorCurveBinding.FloatCurve(p, typeof(Transform), property));
            float rest = AnimationUtility.GetEditorCurve(reference, EditorCurveBinding.FloatCurve(r, typeof(Transform), property)).Evaluate(0);
            scale *= c == null ? 1 : c.Evaluate(time) / rest;
        }
        value.AddKey(time, scale);
    }
    for (int i = 0; i < value.length; i++) {
        AnimationUtility.SetKeyLeftTangentMode(value, i, AnimationUtility.TangentMode.Linear);
        AnimationUtility.SetKeyRightTangentMode(value, i, AnimationUtility.TangentMode.Linear);
    }
    AnimationUtility.SetEditorCurve(result, EditorCurveBinding.FloatCurve("Weapon/Katana", typeof(Transform), property), value);
}
result.EnsureQuaternionContinuity();
AssetDatabase.CreateAsset(result, clipPath);
if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(previous), motionPath))
    throw new System.InvalidOperationException("기존 납도 MotionSet 복제 실패");
var motion = AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>(motionPath);
motion.motionSet.motions[0].motionClip = result;
EditorUtility.SetDirty(motion);
AssetDatabase.SaveAssetIfDirty(motion);
Undo.RecordObject(set, "Bokusei 납도 무기 커브 연결");
set.motionSlots[slot] = motion;
EditorUtility.SetDirty(set);
AssetDatabase.SaveAssetIfDirty(set);
Selection.activeObject = motion;
EditorGUIUtility.PingObject(motion);
return new { slot = slot.ToString(), clip = clipPath, motion = motionPath, length = result.length,
    validPaths = AnimationUtility.GetCurveBindings(result).Where(b => b.type == typeof(Transform)).All(b => prefab.transform.Find(b.path) != null) };
