// 수초의 흐름과 농어 유영을 전용 에셋으로 정리한다.
if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)throw new Exception("정상 Edit Mode 필요");
string folder="Assets/10.Datas/World/OpenWorld/LakeUnderwaterEcology";
var root=GameObject.Find("LakeUnderwaterEcology");
foreach(string name in new[]{"Lake_Grass_01","Lake_Grass_02","Lake_Grass_03"}){
    var material=AssetDatabase.LoadAssetAtPath<Material>(folder+"/"+name+".mat");
    material.SetFloat("_Wind_Strength",.12f);material.SetFloat("_Wind_Speed",.08f);material.SetFloat("_Bend_Strength",.45f);EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);
}
var underside=AssetDatabase.LoadAssetAtPath<Material>(folder+"/LakeUnderside.mat");underside.SetFloat("_RippleStrength",.015f);EditorUtility.SetDirty(underside);AssetDatabase.SaveAssetIfDirty(underside);
string controllerPath=folder+"/PerchAmbient.controller";
var controller=AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
if(controller==null){
    controller=UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
    var clip=AssetDatabase.LoadAllAssetsAtPath("Assets/ExternalAssets/Animals/Animal pack deluxe/Animations/perch_fish_swim_anim.FBX").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
    var state=controller.layers[0].stateMachine.AddState("Swim");state.motion=clip;controller.layers[0].stateMachine.defaultState=state;EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
}
foreach(var animator in root.GetComponentsInChildren<Animator>()){animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;}
PrefabUtility.ApplyPrefabInstance(root,InteractionMode.AutomatedAction);
var scene=root.scene;UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return new {fishAnimators=root.GetComponentsInChildren<Animator>().Length};
