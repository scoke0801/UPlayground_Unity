// Unity execute_code 메서드 본문으로 실행한다. 게임 이벤트·KCC·워프는 실행하지 않는 120Hz 몸/루트 측정이다.
if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
    throw new InvalidOperationException("정상 컴파일된 편집 모드가 필요합니다.");
string[] characters = { "Raon", "Arin", "Hwarin", "SeolA", "Reine", "Yura", "YeonHoa", "Myomyo", "Sera", "Lili", "Lian", "MyoRyeong" };
string[] weapons = { "Katana", "Katana", "DoubleAxe", "Bow", "Spear", "DualBlade", "Staff", "Staff", "GreatSword", "GreatSword", "Whip", "SwordShield" };
var summary = new System.Collections.Generic.List<string>();
string output = "docs/work-status/weapon-signature/sampling";
System.IO.Directory.CreateDirectory(output);
Func<float, string> number = value => value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
for (int characterIndex = 0; characterIndex < characters.Length; characterIndex++)
{
    string character = characters[characterIndex], weapon = weapons[characterIndex];
    string path = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Signature/" + (character == "Raon" ? "Katana_Raon" : weapon) + "_Signature.asset";
    var asset = AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>(path);
    if (asset == null) throw new InvalidOperationException("측정할 MotionSet 누락: " + path);
    var root = PrefabUtility.LoadPrefabContents("Assets/03.Prefabs/Actor/Player/Models/PlayerModel_" + character + ".prefab");
    UPlayGround.Animation.MotionSetPlaybackController playback = null;
    Animancer.AnimancerComponent player = null;
    try
    {
        foreach (var behavior in root.GetComponentsInChildren<MonoBehaviour>(true)) if (behavior != null) behavior.enabled = false;
        foreach (var constraint in root.GetComponentsInChildren<UnityEngine.Animations.ParentConstraint>(true)) constraint.enabled = false;
        // 모델 프리팹은 비활성 상태로 저장되어 있어 Animator만 켜면 수동 평가도 포즈를 쓰지 못한다.
        root.SetActive(true);
        var animator = root.GetComponent<Animator>();
        if (animator == null || animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman)
            throw new InvalidOperationException(character + ": 모델 Avatar 누락");
        animator.enabled = true; animator.applyRootMotion = true; animator.fireEvents = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; animator.runtimeAnimatorController = null;
        player = animator.GetComponent<Animancer.AnimancerComponent>() ?? animator.gameObject.AddComponent<Animancer.AnimancerComponent>();
        player.enabled = true; player.Animator = animator;
        player.Graph.UpdateMode = UnityEngine.Playables.DirectorUpdateMode.Manual;
        playback = new UPlayGround.Animation.MotionSetPlaybackController(player);
        if (!playback.Play(asset)) throw new InvalidOperationException(character + ": 모션 재생 실패");
        player.Graph.PauseGraph(); playback.SeekTime(0); player.Evaluate(0);
        Vector3 position = Vector3.zero; Quaternion rotation = Quaternion.identity;
        root.transform.SetPositionAndRotation(position, rotation);
        var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        var left = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        var right = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        var leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        var rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        Vector3 initialHips = hips.position - position;
        Vector3 initialLeftHand = leftHand.position - position;
        Vector3 initialRightHand = rightHand.position - position;
        float maxPoseDisplacement = 0f;
        float previous = 0, maxFeet = float.NegativeInfinity, minFeet = float.PositiveInfinity;
        var csv = new System.Text.StringBuilder("time,rootX,rootY,rootZ,hipsRelativeY,leftFootRelativeY,rightFootRelativeY\n");
        int frames = Mathf.CeilToInt(asset.motionSet.TotalDuration * 120);
        for (int frame = 0; frame <= frames; frame++)
        {
            float time = Mathf.Min(frame / 120f, asset.motionSet.TotalDuration);
            if (frame > 0)
            {
                float seek = previous;
                int next, prior; float local;
                if (asset.motionSet.GetMotionAtTime(seek + .000001f, out next, out local)
                    && asset.motionSet.GetMotionAtTime(seek, out prior, out local) && next != prior) seek += .000001f;
                playback.SeekTime(seek); player.Evaluate(0);
                root.transform.SetPositionAndRotation(position, rotation);
                player.Evaluate(Mathf.Max(0, time - seek));
                position += animator.deltaPosition; rotation = (rotation * animator.deltaRotation).normalized;
            }
            root.transform.SetPositionAndRotation(position, rotation);
            if (float.IsNaN(position.sqrMagnitude) || float.IsInfinity(position.sqrMagnitude)) throw new InvalidOperationException(character + ": 비정상 루트 좌표");
            float footLeft = left.position.y - position.y, footRight = right.position.y - position.y;
            maxPoseDisplacement = Mathf.Max(maxPoseDisplacement,
                (hips.position - position - initialHips).magnitude,
                (leftHand.position - position - initialLeftHand).magnitude,
                (rightHand.position - position - initialRightHand).magnitude);
            maxFeet = Mathf.Max(maxFeet, Mathf.Min(footLeft, footRight));
            minFeet = Mathf.Min(minFeet, Mathf.Min(footLeft, footRight));
            csv.AppendLine(string.Join(",", new[] { number(time), number(position.x), number(position.y), number(position.z),
                number(hips.position.y - position.y), number(footLeft), number(footRight) }));
            previous = time;
        }
        if (maxPoseDisplacement <= 0.00001f && position.sqrMagnitude <= 0.0000000001f)
            throw new InvalidOperationException(character + ": 전 구간 포즈·루트가 정지했습니다. 샘플링 활성 상태를 확인하세요.");
        System.IO.File.WriteAllText(output + "/" + character + ".csv", csv.ToString());
        summary.Add(character + " | samples=" + (frames + 1) + " | duration=" + number(asset.motionSet.TotalDuration)
            + " | root=" + position + " | rootYaw=" + number(rotation.eulerAngles.y) + " | feetRange=" + number(minFeet) + ".." + number(maxFeet)
            + " | maxPoseDisplacement=" + number(maxPoseDisplacement));
    }
    finally
    {
        if (playback != null) playback.Dispose();
        if (player != null && player.IsGraphInitialized) player.Graph.Destroy();
        PrefabUtility.UnloadPrefabContents(root);
    }
}
summary.Add("범위: 이벤트·KCC 도약·워프·무기 Generic 트랙·그립 접촉은 별도 검사. 루트와 몸의 유한 좌표·시각 상승만 측정.");
System.IO.File.WriteAllLines(output + "/SUMMARY.txt", summary);
return summary;
