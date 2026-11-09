// 캡처 전 별도 호출하여 렌더 파이프라인 전환이 끝난 다음 capture.csx를 실행한다.
if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode 촬영만 허용");
if(Resources.FindObjectsOfTypeAll<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>().Any(p=>p.name=="ShadeCreekCaptureRP"))throw new Exception("기존 캡처 세션 종료 필요");
var rp=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/Settings/LakeVillage_RPAsset.asset"));
rp.name="ShadeCreekCaptureRP";rp.hideFlags=HideFlags.HideAndDontSave;rp.gpuResidentDrawerMode=UnityEngine.Rendering.GPUResidentDrawerMode.Disabled;QualitySettings.renderPipeline=rp;
return "임시 RP 준비. 촬영 후 cleanup-capture.csx 실행 필요";

