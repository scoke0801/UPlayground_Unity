var folder = "output/PlayerSuperJumpGliding";
System.IO.Directory.CreateDirectory(folder);
var clips = new System.Collections.Generic.Dictionary<string, string>();
var uses = new System.Text.StringBuilder("motionSet\tlayer\tindex\tclipPath\tclipName\n");
var sources = new System.Collections.Generic.Dictionary<string, string>();
System.Action<AnimationClip, string> addClip = (clip, source) => {
    string guid; long localId;
    if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(clip, out guid, out localId)) return;
    string key = guid + ":" + localId.ToString();
    if (!clips.ContainsKey(key)) {
        clips[key] = string.Join("\t", new[] { guid,localId.ToString(),AssetDatabase.GetAssetPath(clip),clip.name,clip.length.ToString(System.Globalization.CultureInfo.InvariantCulture),clip.frameRate.ToString(),clip.humanMotion.ToString(),clip.isLooping.ToString() });
        sources[key] = source;
    } else if (!sources[key].Contains(source)) sources[key] += source;
};
foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/ExternalAssets" })) {
    string path = AssetDatabase.GUIDToAssetPath(guid);
    bool include = path.StartsWith("Assets/ExternalAssets/AnimationOnly/", System.StringComparison.OrdinalIgnoreCase);
    foreach (string part in path.Split('/')) include |= part.Equals("Animation", System.StringComparison.OrdinalIgnoreCase) || part.Equals("Animations", System.StringComparison.OrdinalIgnoreCase);
    if (!include) continue;
    foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path)) {var clip=asset as AnimationClip;if(clip!=null)addClip(clip,"A");}
}
foreach (string guid in AssetDatabase.FindAssets("t:MotionSetAsset", new[] { "Assets" })) {
    string path = AssetDatabase.GUIDToAssetPath(guid);
    var asset = AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>(path);
    if (asset == null || asset.motionSet == null) {uses.AppendLine(path+"\tLOAD_FAILURE");continue;}
    System.Action<System.Collections.Generic.List<UPlayGround.Animation.Motion>, string> addUses = (motions, layer) => {
        if (motions == null) return;
        for (int i=0;i<motions.Count;i++) {var m=motions[i];var c=m==null?null:m.motionClip;if(c!=null)addClip(c,"B");uses.AppendLine(path+"\t"+layer+"\t"+i+"\t"+(c==null?"NULL":AssetDatabase.GetAssetPath(c))+"\t"+(c==null?"NULL":c.name));}
    };
    addUses(asset.motionSet.motions,"Base");
    if(asset.motionSet.layers!=null)for(int i=0;i<asset.motionSet.layers.Count;i++){var l=asset.motionSet.layers[i];if(l!=null)addUses(l.motions,i+":"+l.layerName);}
}
var rows=new System.Text.StringBuilder("guid\tlocalId\tpath\tname\tlength\tframeRate\thuman\tloop\tsources\n");
foreach(var pair in clips)rows.AppendLine(pair.Value+"\t"+sources[pair.Key]);
System.IO.File.WriteAllText(folder+"/clip-inventory.tsv",rows.ToString());
System.IO.File.WriteAllText(folder+"/motion-uses.tsv",uses.ToString());
var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Raon.prefab");
var root=UnityEngine.Object.Instantiate(prefab);root.hideFlags=HideFlags.HideAndDontSave;
try {
    var animator=root.GetComponentInChildren<Animator>(true);
    var actorAnimator=root.GetComponentInChildren<UPlayGround.Animation.PlayerActorAnimator>(true);
    var model=new System.Text.StringBuilder();
    model.AppendLine("prefab\t"+AssetDatabase.GetAssetPath(prefab));
    model.AppendLine("guid\t"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab)));
    model.AppendLine("avatar\t"+AssetDatabase.GetAssetPath(animator.avatar));
    model.AppendLine("animatorPath\t"+AnimationUtility.CalculateTransformPath(animator.transform,root.transform));
    model.AppendLine("playerMotionSet\t"+AssetDatabase.GetAssetPath(actorAnimator.PlayerMotionSet));
    foreach(HumanBodyBones bone in new[]{HumanBodyBones.Hips,HumanBodyBones.Head,HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,HumanBodyBones.LeftMiddleProximal,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,HumanBodyBones.RightMiddleProximal,HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot}){
        var t=animator.GetBoneTransform(bone);if(t==null)continue;var p=root.transform.InverseTransformPoint(t.position);
        model.AppendLine(bone+"\t"+AnimationUtility.CalculateTransformPath(t,animator.transform)+"\t"+p.ToString("F5"));
    }
    foreach(var pair in actorAnimator.PlayerMotionSet.motionSets)model.AppendLine("set\t"+pair.Key+"\t"+AssetDatabase.GetAssetPath(pair.Value));
    System.IO.File.WriteAllText(folder+"/model-survey.tsv",model.ToString());
    return "Clips="+clips.Count+"\n"+model.ToString();
}finally{UnityEngine.Object.DestroyImmediate(root);}
