// 원본 프리팹/클립은 저장하지 않는다. 손과 실제 메시의 축·끝점을 직접 측정한다.
if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Edit Mode 필요");
var path="Assets/03.Prefabs/Actor/Monster/Humanoid/MonsterActor_Sera_GreatSword.prefab";
var root=PrefabUtility.LoadPrefabContents(path);
var rows=new System.Collections.Generic.List<string>{"motion,time,tipX,tipY,tipZ,speed,leftShaftDistance,rightShaftDistance,leftAxis,rightAxis,rootX,rootZ"};
try{
root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
foreach(var b in root.GetComponentsInChildren<MonoBehaviour>(true))if(b!=null)b.enabled=false;
var animator=root.GetComponentInChildren<Animator>(true);animator.fireEvents=false;
var weapon=root.transform.Find("Weapon/Weapon/Spear1H_Epic");
var mesh=weapon.GetComponentInChildren<MeshFilter>().sharedMesh;var bounds=mesh.bounds;
var constraint=weapon.parent.GetComponent<UnityEngine.Animations.ParentConstraint>();
var source=constraint.GetSource(0).sourceTransform;
var left=animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal);var right=animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
AnimationMode.StartAnimationMode();
foreach(var g in AssetDatabase.FindAssets("t:MotionSetAsset",new[]{"Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Humanoid/Spear"})){
var asset=AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>(AssetDatabase.GUIDToAssetPath(g));
if(!asset.name.Contains("Attack")&&!asset.name.Contains("Skill"))continue;
var part=asset.motionSet.motions.FirstOrDefault();if(part==null||part.motionClip==null)continue;
Vector3 previous=Vector3.zero;
for(int s=0;s<=Mathf.CeilToInt(part.Duration*60);s++){
float t=Mathf.Min(s/60f,part.Duration);
AnimationMode.BeginSampling();AnimationMode.SampleAnimationClip(animator.gameObject,part.motionClip,part.ClipStartTime+t*part.playbackSpeed);AnimationMode.EndSampling();
weapon.parent.SetPositionAndRotation(source.TransformPoint(constraint.GetTranslationOffset(0)),source.rotation*Quaternion.Euler(constraint.GetRotationOffset(0)));
var tip=weapon.TransformPoint(new Vector3(bounds.center.x,bounds.max.y,bounds.center.z));
var lp=weapon.InverseTransformPoint(left.position);var rp=weapon.InverseTransformPoint(right.position);
float speed=s==0?0:(tip-previous).magnitude*60;previous=tip;
rows.Add(string.Join(",",new[]{asset.name,t.ToString("F4"),tip.x.ToString("F4"),tip.y.ToString("F4"),tip.z.ToString("F4"),speed.ToString("F4"),new Vector2(lp.x-bounds.center.x,lp.z-bounds.center.z).magnitude.ToString("F4"),new Vector2(rp.x-bounds.center.x,rp.z-bounds.center.z).magnitude.ToString("F4"),lp.y.ToString("F4"),rp.y.ToString("F4"),root.transform.position.x.ToString("F4"),root.transform.position.z.ToString("F4")}));
}
}
}finally{if(AnimationMode.InAnimationMode())AnimationMode.StopAnimationMode();PrefabUtility.UnloadPrefabContents(root);}
System.IO.File.WriteAllLines("Logs/HumanoidGrade/Remaining/sera-spear-samples.csv",rows);return rows.Count;
