// 같은 프리뷰 조명·카메라·기본 포즈로 실제 사용 NPC의 얼굴과 전신을 기록한다.
string stage = SessionState.GetString("P09Toon.CaptureStage", "Before");
string folder = "output/P09ToonLook/Residents/" + stage;
System.IO.Directory.CreateDirectory(folder);
var capture = typeof(UPlayGround.Editor.P09Builder.ScenarioResidentAuthoring).GetMethod("Capture", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
var records = new List<string>();
foreach (string path in System.IO.File.ReadAllLines("output/P09ToonLook/used-npcs.txt").Where(p => !string.IsNullOrWhiteSpace(p)))
{
    foreach (bool face in new[] { true, false })
    {
        var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        try
        {
            root.SetActive(true);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            string output = folder + "/" + System.IO.Path.GetFileNameWithoutExtension(path) + (face ? "_Face.png" : "_Full.png");
            capture.Invoke(null, new object[] { root, output, face, 0f, true, true });
            records.Add(output);
        }
        finally { if (root != null) UnityEngine.Object.DestroyImmediate(root); }
    }
}
return records.Count;
