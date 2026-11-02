// Raon의 기존 공용 폴백을 사용하는 모델만 확장한다. false로 먼저 접촉 검사를 수행한다.
bool save = false;
if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
    throw new System.Exception("편집 모드의 컴파일·임포트 완료 후 실행하세요.");
var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Raon.prefab");
var sourceBinding = source.GetComponent<UPlayGround.Components.PlayerGliderBinding>();
var sourceData = new SerializedObject(sourceBinding);
var sharedSet = source.GetComponent<UPlayGround.Animation.PlayerActorAnimator>().PlayerMotionSet;
var originalCommon = sharedSet.GetDefaultMotionSet().fallbackMotionSet;
var playerSet = AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.Animation.PlayerActorAnimationMotionSet>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Humanoid_Player_AnimationSet.asset");
var previewSet = UnityEngine.Object.Instantiate(playerSet);
previewSet.motionSets[UPlayGround.Data.EnumType.WeaponType.NoWeapon] = sharedSet.GetDefaultMotionSet();
var tags = new[] { UPlayGround.Data.Actor.Animation.MotionTags.SuperJump, UPlayGround.Data.Actor.Animation.MotionTags.GliderCatch,
    UPlayGround.Data.Actor.Animation.MotionTags.GliderHang, UPlayGround.Data.Actor.Animation.MotionTags.GliderTurnLeft,
    UPlayGround.Data.Actor.Animation.MotionTags.GliderTurnRight };
var clips = new AnimationClip[tags.Length];
for (int i = 0; i < tags.Length; i++) {
    var motion = sharedSet.GetMotionSetAsset(UPlayGround.Data.EnumType.WeaponType.NoWeapon, tags[i]);
    if (motion == null || !motion.motionSet.IsValid()) throw new System.Exception("원본 모션 오류: " + tags[i]);
    clips[i] = motion.motionSet.motions[0].motionClip;
    if (!clips[i].humanMotion) throw new System.Exception("Humanoid 클립이 아닙니다.");
}
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var awake = typeof(UPlayGround.Components.PlayerGliderBinding).GetMethod("Awake", flags);
var solve = typeof(UPlayGround.Components.PlayerGliderBinding).GetMethod("OnAnimatorIK", flags);
var report = new System.Text.StringBuilder();
var failures = new System.Collections.Generic.List<string>();
foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/03.Prefabs/Actor/Player/Models" })) {
    string path = AssetDatabase.GUIDToAssetPath(guid);
    if (path == AssetDatabase.GetAssetPath(source)) continue;
    var model = PrefabUtility.LoadPrefabContents(path);
    GameObject viewObject = null;
    try {
        var actor = model.GetComponent<UPlayGround.Animation.PlayerActorAnimator>();
        var animator = model.GetComponent<Animator>();
        if (actor == null || animator == null || !animator.isHuman) throw new System.Exception("플레이어 Humanoid 연결 누락");
        var originalSet = actor.PlayerMotionSet;
        if (originalSet != playerSet || (originalSet.GetDefaultMotionSet() != originalCommon && originalSet.GetDefaultMotionSet() != sharedSet.GetDefaultMotionSet()))
            throw new System.Exception("기존 공용 모션이 다르므로 별도 매핑 필요");
        foreach (var pair in originalSet.motionSets) {
            if (pair.Key != UPlayGround.Data.EnumType.WeaponType.NoWeapon && previewSet.GetActorAnimationMotionSet(pair.Key) != pair.Value)
                throw new System.Exception("기존 무기 모션 보존 실패: " + pair.Key);
        }
        var transforms = model.GetComponentsInChildren<Transform>(true);
        var positions = new Vector3[transforms.Length];
        var rotations = new Quaternion[transforms.Length];
        var scales = new Vector3[transforms.Length];
        for (int i = 0; i < transforms.Length; i++) {
            positions[i] = transforms[i].localPosition; rotations[i] = transforms[i].localRotation; scales[i] = transforms[i].localScale;
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transforms[i].gameObject) != 0)
                throw new System.Exception("Missing Script");
        }
        bool wasActive = model.activeSelf;
        var culling = animator.cullingMode;
        bool rootMotion = animator.applyRootMotion;
        model.SetActive(true); animator.Rebind(); animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; animator.applyRootMotion = false;
        var animancer = model.GetComponent<Animancer.AnimancerComponent>();
        var state = animancer.Play(clips[2], 0f); state.Time = 0f; animancer.Evaluate(0f);
        var binding = model.GetComponent<UPlayGround.Components.PlayerGliderBinding>();
        if (binding == null) {
            binding = model.AddComponent<UPlayGround.Components.PlayerGliderBinding>();
            var data = new SerializedObject(binding);
            data.FindProperty("_settings").objectReferenceValue = sourceBinding.Settings;
            data.FindProperty("_prefab").objectReferenceValue = sourceBinding.Prefab;
            data.FindProperty("_animator").objectReferenceValue = animator;
            for (int side = 0; side < 2; side++) {
                var hand = animator.GetBoneTransform(side == 0 ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
                var middle = animator.GetBoneTransform(side == 0 ? HumanBodyBones.LeftMiddleProximal : HumanBodyBones.RightMiddleProximal);
                if (hand == null || middle == null) throw new System.Exception("손바닥 기준 본 누락");
                var palm = new GameObject(side == 0 ? "GliderPalmLeft" : "GliderPalmRight").transform;
                palm.position = Vector3.Lerp(hand.position, middle.position, 0.55f);
                palm.rotation = model.transform.rotation; palm.SetParent(hand, true);
                string hintField = side == 0 ? "_elbowHintLeft" : "_elbowHintRight";
                var referenceHint = (Transform)sourceData.FindProperty(hintField).objectReferenceValue;
                var hint = new GameObject(referenceHint.name).transform; hint.SetParent(model.transform, false);
                hint.localPosition = referenceHint.localPosition; hint.localRotation = referenceHint.localRotation;
                data.FindProperty(side == 0 ? "_palmLeft" : "_palmRight").objectReferenceValue = palm;
                data.FindProperty(hintField).objectReferenceValue = hint;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        awake.Invoke(binding, null);
        if (!binding.CanUse) throw new System.Exception("활공 바인딩 사용 불가");
        viewObject = (GameObject)PrefabUtility.InstantiatePrefab(sourceBinding.Prefab.gameObject);
        var view = viewObject.GetComponent<UPlayGround.Components.PlayerGliderView>();
        // 루트 1배율 계약과 충돌 범위를 유지하며 실제 외형·손잡이를 함께 조정한다.
        float palmHeight = model.transform.InverseTransformPoint((binding.PalmLeft.position + binding.PalmRight.position) * 0.5f).y;
        float gripHeight = sourceBinding.Prefab.transform.InverseTransformPoint((sourceBinding.Prefab.GripLeft.position + sourceBinding.Prefab.GripRight.position) * 0.5f).y;
        // 선회 중 팔이 완전히 펴지지 않도록 손 높이에서 4cm의 도달 여유를 둔다.
        float gliderScale = (palmHeight - 0.04f) / gripHeight;
        foreach (Transform child in viewObject.transform) {
            child.localPosition *= gliderScale;
            child.localScale *= gliderScale;
            PrefabUtility.RecordPrefabInstancePropertyModifications(child);
        }
        var viewData = new SerializedObject(view);
        viewData.FindProperty("_collisionCenter").vector3Value = sourceBinding.Prefab.CollisionCenter * gliderScale;
        viewData.FindProperty("_collisionHalfExtents").vector3Value = sourceBinding.Prefab.CollisionHalfExtents * gliderScale;
        viewData.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(view);
        viewObject.transform.SetPositionAndRotation(model.transform.position, model.transform.rotation);
        binding.BeginTraversal();
        float catchError = float.MaxValue;
        state = animancer.Play(clips[1], 0f);
        for (int i = 0; i <= 16; i++) {
            state.Time = Mathf.Lerp(0.23f, 0.31f, i / 16f); animancer.Evaluate(0f);
            catchError = Mathf.Min(catchError, Mathf.Max(Vector3.Distance(binding.PalmLeft.position, view.GripLeft.position),
                Vector3.Distance(binding.PalmRight.position, view.GripRight.position)));
        }
        binding.Attach(view);
        float maxError = 0f, maxAngle = 0f;
        for (int clipIndex = 1; clipIndex < clips.Length; clipIndex++) {
            state = animancer.Play(clips[clipIndex], 0f);
            for (int i = 0; i <= 60; i++) {
                state.Time = Mathf.Lerp(clipIndex == 1 ? 0.23f : 0f, clips[clipIndex].length, i / 60f);
                animancer.Evaluate(0f); solve.Invoke(binding, new object[] { 0 });
                maxError = Mathf.Max(maxError, Vector3.Distance(binding.PalmLeft.position, view.GripLeft.position), Vector3.Distance(binding.PalmRight.position, view.GripRight.position));
                maxAngle = Mathf.Max(maxAngle, Quaternion.Angle(binding.PalmLeft.rotation, view.GripLeft.rotation), Quaternion.Angle(binding.PalmRight.rotation, view.GripRight.rotation));
            }
        }
        binding.EndTraversal(); animancer.Stop();
        bool valid = catchError <= binding.Settings.catchHandDistance && maxError <= 0.02f && maxAngle <= 10f;
        report.AppendLine(model.name + " catch=" + catchError.ToString("F5") + " IK=" + maxError.ToString("F5") + " angle=" + maxAngle.ToString("F3") + " valid=" + valid);
        if (!valid) throw new System.Exception("접촉 검사 실패");
        for (int i = 0; i < transforms.Length; i++) {
            transforms[i].localPosition = positions[i]; transforms[i].localRotation = rotations[i]; transforms[i].localScale = scales[i];
        }
        animator.cullingMode = culling; animator.applyRootMotion = rootMotion; model.SetActive(wasActive);
        foreach (UPlayGround.Data.EnumType.WeaponType weapon in System.Enum.GetValues(typeof(UPlayGround.Data.EnumType.WeaponType)))
            foreach (var tag in tags) if (previewSet.GetMotionSetAsset(weapon, tag) != sharedSet.GetMotionSetAsset(UPlayGround.Data.EnumType.WeaponType.NoWeapon, tag))
                throw new System.Exception("무기별 활공 모션 해석 실패: " + weapon + "/" + tag);
        if (save) {
            string viewPath = "Assets/03.Prefabs/Traversal/PlayerGlider_" + model.name.Substring("PlayerModel_".Length) + ".prefab";
            viewObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var savedView = PrefabUtility.SaveAsPrefabAsset(viewObject, viewPath);
            var bindingData = new SerializedObject(binding);
            bindingData.FindProperty("_prefab").objectReferenceValue = savedView.GetComponent<UPlayGround.Components.PlayerGliderView>();
            bindingData.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(model, path);
        }
    } catch (System.Exception exception) { failures.Add(path + ": " + exception.Message); }
    finally { if (viewObject != null) UnityEngine.Object.DestroyImmediate(viewObject); PrefabUtility.UnloadPrefabContents(model); }
}
if (save && failures.Count == 0) {
    Undo.RecordObject(playerSet, "플레이어 공용 도약 활공 연결");
    playerSet.motionSets[UPlayGround.Data.EnumType.WeaponType.NoWeapon] = sharedSet.GetDefaultMotionSet();
    EditorUtility.SetDirty(playerSet); AssetDatabase.SaveAssetIfDirty(playerSet);
}
UnityEngine.Object.DestroyImmediate(previewSet);
System.IO.Directory.CreateDirectory("output/PlayerSuperJumpGliding");
string result = "save=" + save + "\n" + report + "\n실패=" + failures.Count + "\n" + string.Join("\n", failures.ToArray());
System.IO.File.WriteAllText("output/PlayerSuperJumpGliding/playable-bindings-" + (save ? "applied" : "preview") + ".txt", result);
return result;
