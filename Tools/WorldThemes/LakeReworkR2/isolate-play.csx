// 사용자가 검토 사본을 직접 Play한 뒤, 조작/저장 전에 한 번 실행한다.
if(!EditorApplication.isPlaying)throw new Exception("검토 씬을 먼저 Play하세요.");
if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/01.Scenes/ArtReview/WorldThemes/LakeReworkR2.unity")throw new Exception("검토 씬 외 실행 금지");
var manager=UnityEngine.Object.FindFirstObjectByType<UPlayGround.Manager.SaveManager>();
if(manager==null||manager.ActiveSlot.HasValue)throw new Exception("초기화 완료·활성 저장 슬롯 없음 상태 필요");
string folder=System.IO.Path.GetFullPath("output/LakeReworkR2/TestSaves/"+DateTime.Now.ToString("yyyyMMdd_HHmmss"));
System.IO.Directory.CreateDirectory(folder);
typeof(UPlayGround.Manager.SaveManager).GetField("_saveFolder",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(manager,folder);
return "격리 저장 폴더: "+folder+" / 예약 슬롯 97701 / 아직 파일 저장 없음. Play 종료 시 원래 설정으로 복원.";
