// LOD2와 같은 거리의 비교 화면을 기준으로 전용 빌보드의 명도와 알파 경계를 조정한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/ShadeCreekR3.unity"||scene.isDirty||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("저장된 R3 Edit 필요");
var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/ThemeStudies/ShadeCreekR3/Canopy_2.mat");
material.SetFloat("_Value",.55f);material.SetFloat("_Saturation",.7f);material.SetFloat("_AlphaClipThreshold",.55f);
EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);
return new{value=material.GetFloat("_Value"),saturation=material.GetFloat("_Saturation"),alpha=material.GetFloat("_AlphaClipThreshold")};
