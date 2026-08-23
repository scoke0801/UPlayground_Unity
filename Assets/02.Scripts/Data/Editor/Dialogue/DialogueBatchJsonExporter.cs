using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UPlayGround.Dialogue;

namespace UPlayGround.Dialogue.Editor
{
    /// <summary>
    /// 대화 그래프(DLG) 에셋을 한 번에 JSON으로 추출한다.
    /// 그래프 하나씩 여는 <see cref="DialogueJsonIO.ExportToJson"/>과 달리, 프로젝트 전체 또는 선택 범위를
    /// 대상으로 삼아 원본 폴더 구조를 그대로 미러링해 저장하고 색인(_index.json)을 함께 남긴다.
    /// </summary>
    public static class DialogueBatchJsonExporter
    {
        private const string GraphFilter = "t:DialogueGraphSO";
        private const string IndexFileName = "_index.json";

        // 마지막 출력 폴더를 기억해 반복 추출 시 폴더를 다시 찾아 들어가지 않도록 한다.
        private const string LastFolderPrefKey = "UPlayGround.Dialogue.BatchJsonExport.LastFolder";

        [Serializable]
        private class IndexDto
        {
            public string exportedAtUtc;
            public int graphCount;
            public List<IndexEntryDto> graphs = new();
        }

        [Serializable]
        private class IndexEntryDto
        {
            public string graphId;
            public string graphName;
            public string assetPath;
            public string jsonPath;   // 출력 폴더 기준 상대 경로
            public int    nodeCount;
        }

        /// <summary>프로젝트의 모든 대화 그래프를 JSON으로 추출한다.</summary>
        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/대화/대화 그래프 JSON 일괄 추출 (전체)")]
        public static void ExportAll()
        {
            List<DialogueGraphSO> graphs = LoadGraphs(AssetDatabase.FindAssets(GraphFilter));
            Export(graphs, "전체 대화 그래프");
        }

        /// <summary>Project 창에서 선택한 그래프·폴더 범위의 대화 그래프를 JSON으로 추출한다.</summary>
        [MenuItem("Assets/UPlayGround/대화/선택 대화 그래프 JSON 일괄 추출", priority = 1200)]
        public static void ExportSelection()
        {
            Export(CollectSelectedGraphs(), "선택한 대화 그래프");
        }

        [MenuItem("Assets/UPlayGround/대화/선택 대화 그래프 JSON 일괄 추출", validate = true)]
        private static bool ValidateExportSelection() => CollectSelectedGraphs().Count > 0;

        // ── 수집 ─────────────────────────────────────────────────────────

        // 선택 항목이 폴더면 하위 전체를, 그래프 에셋이면 그 자신을 대상으로 삼는다.
        private static List<DialogueGraphSO> CollectSelectedGraphs()
        {
            var folders = new List<string>();
            var result = new List<DialogueGraphSO>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (UnityEngine.Object selected in Selection.GetFiltered(
                         typeof(UnityEngine.Object), SelectionMode.Assets))
            {
                string path = AssetDatabase.GetAssetPath(selected);
                if (string.IsNullOrEmpty(path)) continue;

                if (AssetDatabase.IsValidFolder(path))
                {
                    folders.Add(path);
                    continue;
                }

                if (selected is DialogueGraphSO graph && seen.Add(path))
                    result.Add(graph);
            }

            if (folders.Count > 0)
            {
                foreach (string guid in AssetDatabase.FindAssets(GraphFilter, folders.ToArray()))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!seen.Add(path)) continue;

                    var graph = AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(path);
                    if (graph != null) result.Add(graph);
                }
            }

            return result;
        }

        private static List<DialogueGraphSO> LoadGraphs(string[] guids)
        {
            var graphs = new List<DialogueGraphSO>(guids.Length);
            foreach (string guid in guids)
            {
                var graph = AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (graph != null) graphs.Add(graph);
            }
            return graphs;
        }

        // ── 추출 ─────────────────────────────────────────────────────────

        private static void Export(List<DialogueGraphSO> graphs, string scopeLabel)
        {
            if (graphs == null || graphs.Count == 0)
            {
                EditorUtility.DisplayDialog("대화 JSON 일괄 추출", "추출할 대화 그래프를 찾지 못했습니다.", "확인");
                return;
            }

            string outputRoot = AskOutputFolder(scopeLabel, graphs.Count);
            if (string.IsNullOrEmpty(outputRoot)) return;

            var index = new IndexDto
            {
                exportedAtUtc = DateTime.UtcNow.ToString("o"),
                graphCount    = graphs.Count,
            };
            int failed = 0;

            try
            {
                for (int i = 0; i < graphs.Count; i++)
                {
                    DialogueGraphSO graph = graphs[i];
                    string assetPath = AssetDatabase.GetAssetPath(graph);

                    if (EditorUtility.DisplayCancelableProgressBar(
                            "대화 JSON 일괄 추출", assetPath, (float)i / graphs.Count))
                        break;

                    string relativePath = ToRelativeJsonPath(assetPath, graph.name);
                    string fullPath = Path.Combine(outputRoot, relativePath);

                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                        File.WriteAllText(fullPath, DialogueJsonIO.ToJson(graph));
                    }
                    catch (Exception exception)
                    {
                        failed++;
                        Debug.LogError(
                            $"[DialogueBatchJsonExporter] 추출 실패 — {assetPath}\n{exception}", graph);
                        continue;
                    }

                    index.graphs.Add(new IndexEntryDto
                    {
                        graphId   = graph.graphId,
                        graphName = graph.graphName,
                        assetPath = assetPath,
                        jsonPath  = relativePath.Replace('\\', '/'),
                        nodeCount = graph.nodes != null ? graph.nodes.Count : 0,
                    });
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            index.graphCount = index.graphs.Count;
            File.WriteAllText(
                Path.Combine(outputRoot, IndexFileName), JsonUtility.ToJson(index, prettyPrint: true));

            string summary = $"{index.graphs.Count}개 그래프를 JSON으로 추출했습니다." +
                             (failed > 0 ? $" (실패 {failed}개 — Console 확인)" : string.Empty);
            Debug.Log($"[DialogueBatchJsonExporter] {summary}\n→ {outputRoot}");

            if (EditorUtility.DisplayDialog("대화 JSON 일괄 추출", $"{summary}\n\n{outputRoot}", "폴더 열기", "닫기"))
                EditorUtility.RevealInFinder(Path.Combine(outputRoot, IndexFileName));
        }

        private static string AskOutputFolder(string scopeLabel, int count)
        {
            string lastFolder = EditorPrefs.GetString(LastFolderPrefKey, string.Empty);
            if (!Directory.Exists(lastFolder)) lastFolder = Application.dataPath;

            string outputRoot = EditorUtility.SaveFolderPanel(
                $"{scopeLabel} {count}개를 저장할 폴더", lastFolder, string.Empty);
            if (string.IsNullOrEmpty(outputRoot)) return null;

            EditorPrefs.SetString(LastFolderPrefKey, outputRoot);
            return outputRoot;
        }

        // "Assets/10.Datas/Dialogue/Test/DLG_A.asset" → "10.Datas/Dialogue/Test/DLG_A.json"
        // 원본 폴더 구조를 유지해 이름이 같은 그래프끼리 서로 덮어쓰지 않게 한다.
        private static string ToRelativeJsonPath(string assetPath, string fallbackName)
        {
            if (string.IsNullOrEmpty(assetPath))
                return SanitizeFileName(fallbackName) + ".json";

            string withoutExtension = Path.ChangeExtension(assetPath, ".json");
            const string assetsPrefix = "Assets/";
            if (withoutExtension.StartsWith(assetsPrefix, StringComparison.Ordinal))
                withoutExtension = withoutExtension.Substring(assetsPrefix.Length);

            return withoutExtension.Replace('/', Path.DirectorySeparatorChar);
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
                name = name.Replace(invalid, '_');
            return name;
        }
    }
}
