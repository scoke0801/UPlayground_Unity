// 실제 원본의 추가 본 커브를 이식한 임시 사본만 검사한다. 저장하지 않는다.
var root=PrefabUtility.LoadPrefabContents("Assets/03.Prefabs/Actor/Monster/Humanoid/MonsterActor_Sera_GreatSword.prefab");
var rows=new System.Collections.Generic.List<string>{"motion,time,leftAxisDistance,leftAlong,rightAxisDistance,rightAlong"};
try{
root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
foreach(var b in root.GetComponentsInChildren<MonoBehaviour>(true))if(b!=null)b.enabled=false;
var animator=root.GetComponent<Animator>();
var left=animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal);var right=animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
bool added=false;
AnimationMode.StartAnimationMode();
foreach(var g in AssetDatabase.FindAssets("t:MotionSetAsset",new[]{"Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Humanoid/Spear"})){
var asset=AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>(AssetDatabase.GUIDToAssetPath(g));if(!asset.name.Contains("Attack")&&!asset.name.Contains("Skill"))continue;
var part=asset.motionSet.motions.First();var path=AssetDatabase.GetAssetPath(part.motionClip);var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
var hand=source.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="R_Hand_Weapon");if(hand==null)continue;
if(!added){var parentPath=AnimationUtility.CalculateTransformPath(hand.parent,source.transform);var copy=UnityEngine.Object.Instantiate(hand.gameObject,root.transform.Find(parentPath),false);copy.name="R_Hand_Weapon";added=true;}
var curves=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>c.name.StartsWith("__preview__"));var clip=UnityEngine.Object.Instantiate(part.motionClip);
foreach(var binding in AnimationUtility.GetCurveBindings(curves).Where(b=>b.path.Contains("/R_Hand_Weapon")))AnimationUtility.SetEditorCurve(clip,binding,AnimationUtility.GetEditorCurve(curves,binding));
var bone=root.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Spear_Bone01");
for(int s=0;s<=Mathf.CeilToInt(part.Duration*60);s++){
float t=Mathf.Min(s/60f,part.Duration);AnimationMode.BeginSampling();AnimationMode.SampleAnimationClip(root,clip,t);AnimationMode.EndSampling();
var lp=bone.InverseTransformPoint(left.position);var rp=bone.InverseTransformPoint(right.position);
rows.Add(asset.name+","+t.ToString("F4")+","+new Vector2(lp.y,lp.z).magnitude.ToString("F4")+","+(-lp.x).ToString("F4")+","+new Vector2(rp.y,rp.z).magnitude.ToString("F4")+","+(-rp.x).ToString("F4"));
}
UnityEngine.Object.DestroyImmediate(clip);
}
}finally{if(AnimationMode.InAnimationMode())AnimationMode.StopAnimationMode();PrefabUtility.UnloadPrefabContents(root);}
System.IO.File.WriteAllLines("Logs/HumanoidGrade/Remaining/spear-retarget-probe.csv",rows);return rows.Count;
