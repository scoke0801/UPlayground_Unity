using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace UPlayGround.Tool.Editor
{
    public class URPMaterialConverter : EditorWindow
    {
        // --- 폴더 선택 상태 ---
        private string selectedFolderPath = "Assets"; // 기본값: 전체 Assets
        private bool searchSubFolders = true;

        private Vector2 scrollPosition;
        private List<Material> problemMaterials = new List<Material>();
        private Dictionary<Material, Shader> originalShaders = new Dictionary<Material, Shader>();
        private HashSet<Material> convertedMaterials = new HashSet<Material>();

        // Built-in → URP 셰이더 매핑
        private readonly Dictionary<string, string> shaderMappings = new Dictionary<string, string>
        {
            { "Standard",                        "Universal Render Pipeline/Lit" },
            { "Standard (Specular setup)",        "Universal Render Pipeline/Lit" },
            { "Unlit/Color",                      "Universal Render Pipeline/Unlit" },
            { "Unlit/Texture",                    "Universal Render Pipeline/Unlit" },
            { "Unlit/Transparent",                "Universal Render Pipeline/Unlit" },
            { "Unlit/Transparent Cutout",         "Universal Render Pipeline/Unlit" },
            { "Mobile/Diffuse",                   "Universal Render Pipeline/Simple Lit" },
            { "Mobile/Bumped Specular",           "Universal Render Pipeline/Simple Lit" },
            { "Mobile/Bumped Diffuse",            "Universal Render Pipeline/Simple Lit" },
            { "Legacy Shaders/Diffuse",           "Universal Render Pipeline/Simple Lit" },
            { "Legacy Shaders/Specular",          "Universal Render Pipeline/Lit" },
            { "Legacy Shaders/Bumped Diffuse",    "Universal Render Pipeline/Lit" },
            { "Legacy Shaders/Bumped Specular",   "Universal Render Pipeline/Lit" },
        };

        // Built-in 셰이더별 프로퍼티 매핑 (구 이름 → URP 이름)
        // 매핑이 없는 셰이더는 fallback(commonPropertyMappings)으로 처리
        private readonly Dictionary<string, Dictionary<string, string>> propertyMappings = new Dictionary<string, Dictionary<string, string>>
        {
            {
                "Standard", new Dictionary<string, string>
                {
                    { "_MainTex",           "_BaseMap" },
                    { "_Color",             "_BaseColor" },
                    { "_BumpMap",           "_BumpMap" },
                    { "_BumpScale",         "_BumpScale" },
                    { "_MetallicGlossMap",  "_MetallicGlossMap" },
                    { "_Metallic",          "_Metallic" },
                    { "_Glossiness",        "_Smoothness" },
                    { "_OcclusionMap",      "_OcclusionMap" },
                    { "_OcclusionStrength", "_OcclusionStrength" },
                    { "_EmissionMap",       "_EmissionMap" },
                    { "_EmissionColor",     "_EmissionColor" },
                    { "_Cutoff",            "_Cutoff" },
                }
            },
            {
                "Standard (Specular setup)", new Dictionary<string, string>
                {
                    { "_MainTex",           "_BaseMap" },
                    { "_Color",             "_BaseColor" },
                    { "_BumpMap",           "_BumpMap" },
                    { "_BumpScale",         "_BumpScale" },
                    { "_SpecGlossMap",      "_SpecGlossMap" },
                    { "_SpecColor",         "_SpecColor" },
                    { "_Glossiness",        "_Smoothness" },
                    { "_OcclusionMap",      "_OcclusionMap" },
                    { "_OcclusionStrength", "_OcclusionStrength" },
                    { "_EmissionMap",       "_EmissionMap" },
                    { "_EmissionColor",     "_EmissionColor" },
                }
            },
        };

        // Standard 매핑과 동일한 기본 매핑 (매핑 테이블에 없는 셰이더용 fallback)
        private readonly Dictionary<string, string> commonPropertyMappings = new Dictionary<string, string>
        {
            { "_MainTex",           "_BaseMap" },
            { "_Color",             "_BaseColor" },
            { "_BumpMap",           "_BumpMap" },
            { "_BumpScale",         "_BumpScale" },
            { "_MetallicGlossMap",  "_MetallicGlossMap" },
            { "_Metallic",          "_Metallic" },
            { "_Glossiness",        "_Smoothness" },
            { "_OcclusionMap",      "_OcclusionMap" },
            { "_OcclusionStrength", "_OcclusionStrength" },
            { "_EmissionMap",       "_EmissionMap" },
            { "_EmissionColor",     "_EmissionColor" },
            { "_Cutoff",            "_Cutoff" },
        };

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/유틸/변환기/URP 머티리얼 변환기")]
        public static void ShowWindow()
        {
            GetWindow<URPMaterialConverter>("URP 머티리얼 변환기");
        }

        private void OnEnable()
        {
            // 프로젝트 선택 폴더가 있으면 그걸로 초기화
            SyncFolderFromSelection();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("URP 머티리얼 변환기", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawFolderSelector();
            EditorGUILayout.Space();

            if (GUILayout.Button("스캔", GUILayout.Height(28)))
                ScanMaterials();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"발견된 문제 머티리얼: {problemMaterials.Count}개", EditorStyles.helpBox);

            if (problemMaterials.Count == 0)
            {
                EditorGUILayout.HelpBox("변환이 필요한 머티리얼이 없습니다.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();
            DrawBulkButtons();
            EditorGUILayout.Space();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            for (int i = 0; i < problemMaterials.Count; i++)
                DrawMaterialRow(problemMaterials[i], i);
            EditorGUILayout.EndScrollView();
        }

        // ─── UI 드로우 헬퍼 ────────────────────────────────────────────

        private void DrawFolderSelector()
        {
            EditorGUILayout.LabelField("스캔 범위", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("대상 폴더:", GUILayout.Width(70));
            EditorGUILayout.LabelField(selectedFolderPath, EditorStyles.textField);

            if (GUILayout.Button("선택", GUILayout.Width(50)))
                PickFolder();

            if (GUILayout.Button("전체", GUILayout.Width(40)))
                selectedFolderPath = "Assets";

            EditorGUILayout.EndHorizontal();

            // Project 창에서 선택한 폴더를 바로 반영
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("현재 선택 폴더 사용", GUILayout.Height(22)))
                SyncFolderFromSelection();

            searchSubFolders = EditorGUILayout.ToggleLeft("하위 폴더 포함", searchSubFolders);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawBulkButtons()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("모두 변환", GUILayout.Height(28)))
                ConvertAll();
            if (GUILayout.Button("변환 모두 되돌리기", GUILayout.Height(28)))
                RevertAll();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawMaterialRow(Material material, int index)
        {
            if (material == null) return;

            EditorGUILayout.BeginVertical("box");

            // 헤더
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{index + 1}. {material.name}", EditorStyles.boldLabel);

            if (GUILayout.Button("선택", GUILayout.Width(50)))
            {
                Selection.activeObject = material;
                EditorGUIUtility.PingObject(material);
            }
            EditorGUILayout.EndHorizontal();

            string currentShader = material.shader ? material.shader.name : "없음";
            EditorGUILayout.LabelField($"현재 셰이더: {currentShader}");
            EditorGUILayout.LabelField($"권장 URP:   {GetTargetShaderName(currentShader)}");

            // 텍스처 미리보기
            Texture mainTex = material.mainTexture;
            if (mainTex != null)
            {
                EditorGUILayout.BeginHorizontal();

                DrawTexturePreview("현재", mainTex, convertedMaterials.Contains(material) ? new Color(1f, 0.8f, 1f) : Color.white);
                DrawTexturePreview("변환 후 예상", mainTex, convertedMaterials.Contains(material) ? Color.white : new Color(1f, 0.8f, 1f));

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(4);

            // 변환 / 되돌리기 버튼
            EditorGUILayout.BeginHorizontal();
            if (!convertedMaterials.Contains(material))
            {
                if (GUILayout.Button("변환"))
                    ConvertMaterial(material);
            }
            else
            {
                GUI.enabled = false;
                GUILayout.Button("변환 완료 ✓");
                GUI.enabled = true;

                if (GUILayout.Button("되돌리기", GUILayout.Width(80)))
                    RevertMaterial(material);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private static void DrawTexturePreview(string label, Texture texture, Color tint)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(110));
            EditorGUILayout.LabelField(label, GUILayout.Width(110));
            Rect rect = GUILayoutUtility.GetRect(100, 100, GUILayout.Width(100), GUILayout.Height(100));
            Color prev = GUI.color;
            GUI.color = tint;
            EditorGUI.DrawPreviewTexture(rect, texture);
            GUI.color = prev;
            EditorGUILayout.EndVertical();
        }

        // ─── 폴더 선택 ────────────────────────────────────────────────

        private void PickFolder()
        {
            // OS 다이얼로그로 폴더 선택 후 Assets 상대 경로로 변환
            string abs = EditorUtility.OpenFolderPanel("스캔할 폴더 선택", Application.dataPath, "");
            if (string.IsNullOrEmpty(abs)) return;

            if (abs.StartsWith(Application.dataPath))
            {
                selectedFolderPath = "Assets" + abs.Substring(Application.dataPath.Length).Replace('\\', '/');
            }
            else
            {
                EditorUtility.DisplayDialog("오류", "Assets 폴더 내부의 경로만 선택할 수 있습니다.", "확인");
            }
        }

        private void SyncFolderFromSelection()
        {
            // Project 창에서 폴더를 선택한 경우 해당 경로로 자동 설정
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                selectedFolderPath = path;
        }

        // ─── 스캔 ──────────────────────────────────────────────────────

        private void ScanMaterials()
        {
            problemMaterials.Clear();

            // searchSubFolders가 false면 바로 아래 .mat 파일만 탐색
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { selectedFolderPath });

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                // 하위 폴더 제외 옵션: 경로의 디렉토리가 선택 폴더와 같은지 확인
                if (!searchSubFolders)
                {
                    string dir = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
                    if (dir != selectedFolderPath.TrimEnd('/'))
                        continue;
                }

                var mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                if (mat != null && IsProblemMaterial(mat))
                    problemMaterials.Add(mat);
            }

            Debug.Log($"[URPConverter] '{selectedFolderPath}' 스캔 완료 — 문제 머티리얼 {problemMaterials.Count}개");
            Repaint();
        }

        private bool IsProblemMaterial(Material material)
        {
            if (material.shader == null) return true;
            string s = material.shader.name;

            return (s.Contains("Standard") && !s.Contains("Universal")) ||
                   s.Contains("Legacy Shaders") ||
                   s.Contains("Mobile/") ||
                   (s.StartsWith("Unlit/") && !s.Contains("Universal")) ||
                   s == "Hidden/InternalErrorShader";
        }

        // ─── 변환 ──────────────────────────────────────────────────────

        private void ConvertAll()
        {
            if (!EditorUtility.DisplayDialog("확인",
                $"'{selectedFolderPath}' 내 {problemMaterials.Count}개 머티리얼을 변환합니다.\n실행 취소 가능합니다.",
                "변환", "취소")) return;

            foreach (var mat in problemMaterials)
                ConvertMaterial(mat);

            ScanMaterials();
        }

        private void ConvertMaterial(Material material)
        {
            if (material == null) return;

            Undo.RecordObject(material, $"Convert {material.name} to URP");

            if (!originalShaders.ContainsKey(material))
                originalShaders[material] = material.shader;

            string srcShader = material.shader.name;
            string dstShader = GetTargetShaderName(srcShader);

            Shader urpShader = Shader.Find(dstShader);
            if (urpShader == null)
            {
                Debug.LogWarning($"[URPConverter] 셰이더 없음: {dstShader}");
                return;
            }

            var backup = BackupProperties(material, srcShader);
            material.shader = urpShader;
            convertedMaterials.Add(material);
            RestoreProperties(material, backup, srcShader);

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            Debug.Log($"[URPConverter] {material.name}: {srcShader} → {dstShader}");
        }

        private void RevertAll()
        {
            if (!EditorUtility.DisplayDialog("확인", "모든 변환을 되돌리시겠습니까?", "되돌리기", "취소")) return;

            foreach (var mat in new List<Material>(convertedMaterials))
                RevertMaterial(mat);

            ScanMaterials();
        }

        private void RevertMaterial(Material material)
        {
            if (!originalShaders.TryGetValue(material, out var original)) return;

            Undo.RecordObject(material, $"Revert {material.name}");
            material.shader = original;
            convertedMaterials.Remove(material);

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            Debug.Log($"[URPConverter] {material.name} 되돌리기 완료");
        }

        // ─── 프로퍼티 백업 / 복원 ────────────────────────────────────

        private Dictionary<string, object> BackupProperties(Material material, string srcShader)
        {
            var backup = new Dictionary<string, object>();
            var mappings = propertyMappings.TryGetValue(srcShader, out var specific) ? specific : commonPropertyMappings;

            foreach (string propName in mappings.Keys)
                BackupSingleProperty(material, propName, backup);

            return backup;
        }

        private void BackupSingleProperty(Material material, string propName, Dictionary<string, object> backup)
        {
            if (!material.HasProperty(propName)) return;
            try
            {
                if (propName.EndsWith("Tex") || propName.EndsWith("Map") || propName == "_MainTex")
                {
                    var tex = material.GetTexture(propName);
                    if (tex == null) return;
                    backup[propName + "_tex"]    = tex;
                    backup[propName + "_scale"]  = material.GetTextureScale(propName);
                    backup[propName + "_offset"] = material.GetTextureOffset(propName);
                }
                else if (propName.Contains("Color"))
                {
                    backup[propName + "_color"] = material.GetColor(propName);
                }
                else
                {
                    backup[propName + "_float"] = material.GetFloat(propName);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[URPConverter] 백업 실패 {propName}: {e.Message}");
            }
        }

        private void RestoreProperties(Material material, Dictionary<string, object> backup, string srcShader)
        {
            var mappings = propertyMappings.TryGetValue(srcShader, out var specific) ? specific : commonPropertyMappings;

            foreach (var kv in mappings)
                RestoreSingleProperty(material, backup, kv.Key, kv.Value);
        }

        private void RestoreSingleProperty(Material material, Dictionary<string, object> backup, string oldProp, string newProp)
        {
            if (!material.HasProperty(newProp)) return;
            try
            {
                if (backup.TryGetValue(oldProp + "_tex", out var texObj) && texObj is Texture tex)
                {
                    material.SetTexture(newProp, tex);
                    if (backup.TryGetValue(oldProp + "_scale", out var s) && s is Vector2 scale)
                        material.SetTextureScale(newProp, scale);
                    if (backup.TryGetValue(oldProp + "_offset", out var o) && o is Vector2 offset)
                        material.SetTextureOffset(newProp, offset);
                }
                else if (backup.TryGetValue(oldProp + "_color", out var colObj) && colObj is Color color)
                {
                    material.SetColor(newProp, color);
                }
                else if (backup.TryGetValue(oldProp + "_float", out var fObj) && fObj is float f)
                {
                    material.SetFloat(newProp, f);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[URPConverter] 복원 실패 {oldProp}→{newProp}: {e.Message}");
            }
        }

        private string GetTargetShaderName(string srcShader)
        {
            return shaderMappings.TryGetValue(srcShader, out var target) ? target : "Universal Render Pipeline/Lit";
        }
    }
}
