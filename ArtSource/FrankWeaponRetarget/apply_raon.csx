// Unity execute_code의 메서드 본문으로 실행한다. 원본 FBX와 공용 MotionSet은 보존한다.
const string prefabPath = "Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Raon.prefab";
const string setPath = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Bokusei_KatanaAnimationSet.asset";
const string output = "Assets/07.Animations/Player/Raon/FrankWeapon";
const string motionOutput = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Katana/FrankWeapon";
const string sourceFolder = "Assets/ExternalAssets/AnimationOnly/Frank_Slash_Pack/Assets/Animations/Frank_SlashPack_Katana/FBX_Animation/Root_Motion/";
string[] sourceNames = { "Frank_RPG_Katana_S1_Combo01_01", "Frank_RPG_Katana_S1_Combo01_02", "Frank_RPG_Katana_S1_Combo01_03", "Frank_RPG_Katana_S3_Skill01", "Frank_RPG_Katana_Stance1_Equip" };
if (UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorUtility.scriptCompilationFailed || UnityEditor.EditorApplication.isPlaying)
    throw new System.InvalidOperationException("컴파일과 Play Mode를 마친 뒤 실행하세요.");
var set = UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.Animation.ActorAnimationMotionSet>(setPath);
if (set == null) throw new System.InvalidOperationException("RAON 모션 매핑이 없습니다.");
if (UnityEditor.AssetDatabase.IsValidFolder(output))
    throw new System.InvalidOperationException("생성 폴더가 이미 있습니다. 기존 결과를 검토한 뒤 변경하세요.");

System.Action<string> ensureFolder = null;
ensureFolder = path => {
    if (UnityEditor.AssetDatabase.IsValidFolder(path)) return;
    var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
    ensureFolder(parent);
    UnityEditor.AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
};
System.Func<UnityEngine.Transform, string, UnityEngine.Transform> find = (searchRoot, name) =>
    searchRoot.GetComponentsInChildren<UnityEngine.Transform>(true).Single(t => t.name == name);
System.Func<UnityEngine.Transform, string, UnityEngine.Transform> child = (parent, name) => {
    var result = new UnityEngine.GameObject(name).transform;
    result.SetParent(parent, false);
    return result;
};
System.Action<UnityEngine.Transform, UnityEngine.Matrix4x4> setMatrix = (t, m) => {
    t.localPosition = m.GetColumn(3);
    t.localRotation = m.rotation;
    t.localScale = m.lossyScale;
};
System.Func<string, UnityEngine.AnimationClip> loadBody = name =>
    UnityEditor.AssetDatabase.LoadAllAssetsAtPath(sourceFolder + name + ".FBX")
        .OfType<UnityEngine.AnimationClip>().Single(c => !c.name.StartsWith("__"));
// 임포트 마스크로 잘린 추가 본만 미리보기 데이터에서 읽어 독립 .anim에 굽는다.
// 생성 에셋은 __preview__ 클립을 참조하지 않는다.
System.Func<string, UnityEngine.AnimationClip> loadCurves = name => {
    var clips = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(sourceFolder + name + ".FBX").OfType<UnityEngine.AnimationClip>().ToArray();
    return clips.FirstOrDefault(c => c.name.StartsWith("__preview__")) ?? clips.Single(c => !c.name.StartsWith("__"));
};
string[] bones = { "R_Hand_Weapon", "Weapon_Sword", "Weapon_Blade", "Sword_Case", "Weapon_Sword_Dummy" };
var reference = loadCurves(sourceNames[0]);
var referenceBindings = UnityEditor.AnimationUtility.GetCurveBindings(reference);
var sourcePaths = bones.ToDictionary(n => n, n => referenceBindings.Select(b => b.path).Distinct().Single(p => p.EndsWith("/" + n)));
string[] properties = { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z", "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w", "m_LocalScale.x", "m_LocalScale.y", "m_LocalScale.z" };
System.Func<UnityEngine.AnimationClip, string, string, UnityEngine.AnimationCurve> curve = (clip, path, property) =>
    UnityEditor.AnimationUtility.GetEditorCurve(clip, UnityEditor.EditorCurveBinding.FloatCurve(path, typeof(UnityEngine.Transform), property));
var rest = bones.ToDictionary(n => n, n => properties.Select(p => {
    var c = curve(reference, sourcePaths[n], p);
    if (c == null) throw new System.InvalidOperationException("기준 커브 누락: " + n + " / " + p);
    return c.Evaluate(0);
}).ToArray());
System.Func<float[], UnityEngine.Matrix4x4> restMatrix = v => UnityEngine.Matrix4x4.TRS(
    new UnityEngine.Vector3(v[0], v[1], v[2]), new UnityEngine.Quaternion(v[3], v[4], v[5], v[6]), new UnityEngine.Vector3(v[7], v[8], v[9]));

var created = new System.Collections.Generic.List<string>();
var prefabBytes = System.IO.File.ReadAllBytes(prefabPath);
var setBytes = System.IO.File.ReadAllBytes(setPath);
var root = UnityEditor.PrefabUtility.LoadPrefabContents(prefabPath);
bool committed = false;
UnityEditor.Undo.IncrementCurrentGroup();
int undoGroup = UnityEditor.Undo.GetCurrentGroup();
try {
    var animator = root.GetComponent<UnityEngine.Animator>();
    var hand = animator.GetBoneTransform(UnityEngine.HumanBodyBones.RightHand);
    var hips = animator.GetBoneTransform(UnityEngine.HumanBodyBones.Hips);
    var weapon = root.transform.Find("Weapon/Katana");
    var attachment = weapon.GetComponent<UnityEngine.Animations.ParentConstraint>();
    var oldHand = attachment.GetSource(0).sourceTransform;
    var drawnPosition = oldHand.position + oldHand.rotation * attachment.GetTranslationOffset(0);
    var drawnRotation = oldHand.rotation * UnityEngine.Quaternion.Euler(attachment.GetRotationOffset(0));
    var oldBack = attachment.GetSource(1).sourceTransform;
    var backPosition = oldBack.position + oldBack.rotation * attachment.GetTranslationOffset(1);
    var backRotation = oldBack.rotation * UnityEngine.Quaternion.Euler(attachment.GetRotationOffset(1));
    var socketScale = weapon.localScale;
    var target = bones.ToDictionary(n => n, n => find(root.transform, n));
    var handBasis = child(hand, "FrankWeaponBasis");
    var chain = restMatrix(rest[bones[0]]) * restMatrix(rest[bones[1]]) * restMatrix(rest[bones[2]]);
    setMatrix(handBasis, hand.worldToLocalMatrix * UnityEngine.Matrix4x4.TRS(drawnPosition, drawnRotation, weapon.lossyScale) * chain.inverse);
    target["R_Hand_Weapon"].SetParent(handBasis, false);
    var oldCaseMatrix = target["Sword_Case"].localToWorldMatrix;
    var sheathBasis = child(hips, "FrankSheathBasis");
    setMatrix(sheathBasis, hips.worldToLocalMatrix * oldCaseMatrix * restMatrix(rest["Sword_Case"]).inverse);
    target["Sword_Case"].SetParent(sheathBasis, false);
    foreach (var name in bones) setMatrix(target[name], restMatrix(rest[name]));
    var sheathedSocket = child(target["Weapon_Sword_Dummy"], "SheathedEquipmentSocket");
    sheathedSocket.SetPositionAndRotation(backPosition, backRotation);
    var handSource = attachment.GetSource(0);
    handSource.sourceTransform = target["Weapon_Blade"];
    attachment.SetSource(0, handSource);
    attachment.SetTranslationOffset(0, UnityEngine.Vector3.zero);
    attachment.SetRotationOffset(0, UnityEngine.Vector3.zero);
    var backSource = attachment.GetSource(1);
    backSource.sourceTransform = sheathedSocket;
    attachment.SetSource(1, backSource);
    attachment.SetTranslationOffset(1, UnityEngine.Vector3.zero);
    attachment.SetRotationOffset(1, UnityEngine.Vector3.zero);
    var binding = weapon.GetComponent<UPlayGround.Components.WeaponSocketBinding>();
    if (binding == null) binding = weapon.gameObject.AddComponent<UPlayGround.Components.WeaponSocketBinding>();
    binding.constraint = attachment;
    binding.equipPosition = UPlayGround.Data.EnumType.EquipPosition.RightHand;
    binding.weaponType = UPlayGround.Data.EnumType.WeaponType.Katana;

    ensureFolder(output);
    ensureFolder(motionOutput);
    var converted = new System.Collections.Generic.Dictionary<UnityEngine.AnimationClip, UnityEngine.AnimationClip>();
    foreach (var name in sourceNames) {
        var original = loadBody(name);
        var extra = loadCurves(name);
        var clip = UnityEngine.Object.Instantiate(original);
        clip.name = "Raon_" + name;
        foreach (var b in UnityEditor.AnimationUtility.GetCurveBindings(clip).Where(b => b.type == typeof(UnityEngine.Transform)))
            UnityEditor.AnimationUtility.SetEditorCurve(clip, b, null);
        foreach (var bone in bones) {
            var path = UnityEditor.AnimationUtility.CalculateTransformPath(target[bone], root.transform);
            for (int i = 0; i < properties.Length; i++) {
                var values = curve(extra, sourcePaths[bone], properties[i]) ?? UnityEngine.AnimationCurve.Constant(0, original.length, rest[bone][i]);
                UnityEditor.AnimationUtility.SetEditorCurve(clip, UnityEditor.EditorCurveBinding.FloatCurve(path, typeof(UnityEngine.Transform), properties[i]), values);
            }
        }
        // ParentConstraint는 스케일을 전달하지 않는다. 실제 검/잔상/히트박스 루트를 같은 비율로 굽는다.
        var weaponPath = UnityEditor.AnimationUtility.CalculateTransformPath(weapon, root.transform);
        for (int axis = 0; axis < 3; axis++) {
            var values = new UnityEngine.AnimationCurve();
            int frames = UnityEngine.Mathf.CeilToInt(original.length * original.frameRate);
            for (int frame = 0; frame <= frames; frame++) {
                float time = UnityEngine.Mathf.Min(frame / original.frameRate, original.length);
                float scale = socketScale[axis];
                foreach (var bone in bones.Take(3)) {
                    var c = curve(extra, sourcePaths[bone], properties[7 + axis]);
                    scale *= (c != null ? c.Evaluate(time) : rest[bone][7 + axis]) / rest[bone][7 + axis];
                }
                values.AddKey(time, scale);
            }
            for (int k = 0; k < values.length; k++) {
                UnityEditor.AnimationUtility.SetKeyLeftTangentMode(values, k, UnityEditor.AnimationUtility.TangentMode.Linear);
                UnityEditor.AnimationUtility.SetKeyRightTangentMode(values, k, UnityEditor.AnimationUtility.TangentMode.Linear);
            }
            UnityEditor.AnimationUtility.SetEditorCurve(clip, UnityEditor.EditorCurveBinding.FloatCurve(weaponPath, typeof(UnityEngine.Transform), properties[7 + axis]), values);
        }
        clip.EnsureQuaternionContinuity();
        var pathName = output + "/" + clip.name + ".anim";
        UnityEditor.AssetDatabase.CreateAsset(clip, pathName);
        created.Add(pathName);
        converted.Add(original, clip);
    }
    var replacements = new System.Collections.Generic.Dictionary<UPlayGround.Animation.MotionSetAsset, UPlayGround.Animation.MotionSetAsset>();
    foreach (var original in set.abilityMotions.Values.Concat(set.motionSlots.Values).Where(m => m != null).Distinct()) {
        if (!original.motionSet.motions.Any(m => m.motionClip != null && converted.ContainsKey(m.motionClip))) continue;
        var path = motionOutput + "/" + original.name + "_RaonWeapon.asset";
        if (!UnityEditor.AssetDatabase.CopyAsset(UnityEditor.AssetDatabase.GetAssetPath(original), path)) throw new System.InvalidOperationException("모션 복제 실패: " + path);
        created.Add(path);
        var copy = UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>(path);
        foreach (var motion in copy.motionSet.motions)
            if (motion.motionClip != null && converted.ContainsKey(motion.motionClip)) motion.motionClip = converted[motion.motionClip];
        UnityEditor.EditorUtility.SetDirty(copy);
        UnityEditor.AssetDatabase.SaveAssetIfDirty(copy);
        replacements.Add(original, copy);
    }
    var equip = UnityEngine.ScriptableObject.CreateInstance<UPlayGround.Animation.MotionSetAsset>();
    equip.name = "Raon_Katana_Equip_FrankWeapon";
    equip.motionSet = UPlayGround.Animation.MotionSet.CreateAuthored(equip.name);
    equip.motionSet.motions.Add(new UPlayGround.Animation.Motion { motionName = "Katana_Equip", motionClip = converted[loadBody(sourceNames[4])], playbackSpeed = 1 });
    var equipPath = motionOutput + "/" + equip.name + ".asset";
    UnityEditor.AssetDatabase.CreateAsset(equip, equipPath);
    created.Add(equipPath);
    foreach (var clip in converted.Values)
        foreach (var b in UnityEditor.AnimationUtility.GetCurveBindings(clip).Where(b => b.type == typeof(UnityEngine.Transform)))
            if (root.transform.Find(b.path) == null) throw new System.InvalidOperationException("바인딩 누락: " + b.path);
    if (UnityEngine.Vector3.Distance(target["Weapon_Blade"].position, drawnPosition) > 0.0001f || UnityEngine.Quaternion.Angle(target["Weapon_Blade"].rotation, drawnRotation) > 0.01f)
        throw new System.InvalidOperationException("기준 그립 보정이 기존 무기 위치와 다릅니다.");

    UnityEditor.Undo.RecordObject(set, "RAON 무기 커브 연결");
    foreach (var key in set.abilityMotions.Keys.ToArray()) if (replacements.ContainsKey(set.abilityMotions[key])) set.abilityMotions[key] = replacements[set.abilityMotions[key]];
    foreach (var key in set.motionSlots.Keys.ToArray()) if (replacements.ContainsKey(set.motionSlots[key])) set.motionSlots[key] = replacements[set.motionSlots[key]];
    set.motionSlots[UPlayGround.Data.Actor.Animation.MotionTags.Equip_Weapon] = equip;
    UnityEditor.EditorUtility.SetDirty(set);
    if (UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, prefabPath) == null) throw new System.InvalidOperationException("프리팹 저장 실패");
    UnityEditor.AssetDatabase.SaveAssetIfDirty(set);
    UnityEditor.Undo.CollapseUndoOperations(undoGroup);
    committed = true;
    return new { clips = converted.Count, motions = replacements.Count + 1, paths = created.ToArray() };
}
finally {
    UnityEditor.PrefabUtility.UnloadPrefabContents(root);
    if (!committed) {
        UnityEditor.Undo.RevertAllDownToGroup(undoGroup);
        System.IO.File.WriteAllBytes(prefabPath, prefabBytes);
        System.IO.File.WriteAllBytes(setPath, setBytes);
        foreach (var path in created) UnityEditor.AssetDatabase.DeleteAsset(path);
        UnityEditor.AssetDatabase.ImportAsset(prefabPath);
        UnityEditor.AssetDatabase.ImportAsset(setPath);
    }
}
