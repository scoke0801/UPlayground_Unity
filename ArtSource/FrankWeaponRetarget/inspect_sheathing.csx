const string path = "Assets/ExternalAssets/AnimationOnly/Frank_Slash_Pack/Assets/Animations/Frank_SlashPack_Katana/FBX_Animation/In_Place/Frank_RPG_Katana_Unequip.FBX";
var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
root.hideFlags = HideFlags.HideAndDontSave;
var avatar = AvatarBuilder.BuildHumanAvatar(root, ((ModelImporter)AssetImporter.GetAtPath(path)).humanDescription);
var animator = root.AddComponent<Animator>(); animator.avatar = avatar; animator.applyRootMotion = false;
animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
try {
    var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Single(c=>c.name.StartsWith("__"));
    var result = new System.Collections.Generic.List<object>();
    foreach (float time in new[]{0f,0.8f,1.3f,1.6f,1.8f,2f,2.5f}) {
        clip.SampleAnimation(root, time);
        result.Add(new {time, bones=root.GetComponentsInChildren<Transform>().Where(t=>new[]{"hand_r","hand_l","Weapon_Sword","Weapon_Blade","Sword_Case"}.Contains(t.name)).Select(t=>new{name=t.name,p=t.position.ToString("F4"),x=t.right.ToString("F4"),y=t.up.ToString("F4"),scale=t.lossyScale.ToString("F4")}).ToArray()});
    }
    return new{valid=avatar.isValid,human=avatar.isHuman,result};
} finally {
    UnityEngine.Object.DestroyImmediate(root);
    UnityEngine.Object.DestroyImmediate(avatar);
}
