if(Application.dataPath.Replace('\\','/')!="C:/UsingProject/UnityProject/UPlayground/Assets")throw new Exception("프로젝트 불일치");
if(!System.Text.RegularExpressions.Regex.IsMatch(System.IO.File.ReadAllText("Tools/WorldThemes/AgentCoordination/editor.lock"),"\"owner\"\\s*:\\s*\"B\""))throw new Exception("B 작업권 필요");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/CloudridgeR1.unity"||scene.isDirty||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("B clean Edit 필요");
string source="Assets/08.Models/PoorVillage/PoorHouse_Hut.fbx",copy="Assets/10.Datas/World/ThemeStudies/CloudridgeR1/CaptureDiskHut.fbx";
if(!System.IO.File.Exists(copy)&&!AssetDatabase.CopyAsset(source,copy))throw new Exception("촬영 기준 분리 실패");
var materials=AssetDatabase.LoadAllAssetsAtPath(copy).OfType<Material>().ToArray();return materials.Select(m=>new{name=m.name,shader=m.shader.name,dirty=EditorUtility.IsDirty(m)}).ToArray();
