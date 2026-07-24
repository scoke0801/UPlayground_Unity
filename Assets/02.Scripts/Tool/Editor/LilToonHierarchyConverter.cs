using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Tool.Editor
{
    /// <summary>
    /// Hierarchy에서 대상 GameObject를 선택하면, 그 하위(자식 포함) Renderer들이 참조하는
    /// 머티리얼을 lilToon 기반 셰이더로 일괄 변환한다.
    /// 변환 시 메인 텍스처(_MainTex/_BaseMap)와 메인 컬러(_Color/_BaseColor)를 lilToon
    /// 프로퍼티로 이관하며, 원본 셰이더를 기억해 되돌리기를 지원한다.
    ///
    /// 주의: Renderer.sharedMaterial(에셋)을 직접 변경하므로 같은 머티리얼을 참조하는
    /// 다른 오브젝트에도 반영된다. (URPMaterialConverter와 동일한 정책)
    /// </summary>
    public class LilToonHierarchyConverter : EditorWindow
    {
        private const string LilToonShaderName = "lilToon";

        private Vector2 scrollPosition;

        // 스캔 결과: 대상 하위에서 발견한 (변환 가능한) 고유 머티리얼
        private readonly List<Material> foundMaterials = new List<Material>();

        // 되돌리기용 원본 셰이더 기억
        private readonly Dictionary<Material, Shader> originalShaders = new Dictionary<Material, Shader>();
        private readonly HashSet<Material> convertedMaterials = new HashSet<Material>();

        // 이미 lilToon인 머티리얼은 스캔 목록에서 제외할지 여부
        private bool skipAlreadyLilToon = true;

        // 원본의 불투명/컷아웃/반투명 렌더링 모드를 lilToon 모드로 자동 매핑할지 여부
        private bool autoMapRenderingMode = true;

        // lilToon 렌더링 모드 (lilToon.RenderingMode enum과 값 일치)
        private enum LilRenderingMode { Opaque = 0, Cutout = 1, Transparent = 2 }

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/유틸/변환기/lilToon 계층 변환기")]
        public static void ShowWindow()
        {
            GetWindow<LilToonHierarchyConverter>("lilToon 계층 변환기");
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            // 선택이 바뀌면 헤더 정보만 갱신 (자동 스캔은 하지 않음)
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("lilToon 계층 변환기", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Hierarchy에서 대상 GameObject를 선택하고 [스캔]을 누르면, 하위 Renderer들의 머티리얼을 찾습니다.\n" +
                "[모두 변환]을 누르면 lilToon 셰이더로 변경하고 메인 텍스처/컬러를 이관합니다.",
                MessageType.Info);

            EditorGUILayout.Space();
            DrawTargetInfo();
            EditorGUILayout.Space();

            skipAlreadyLilToon = EditorGUILayout.ToggleLeft("이미 lilToon인 머티리얼 제외", skipAlreadyLilToon);
            autoMapRenderingMode = EditorGUILayout.ToggleLeft("렌더링 모드(불투명/컷아웃/반투명) 자동 매핑", autoMapRenderingMode);

            using (new EditorGUI.DisabledScope(!HasValidTarget()))
            {
                if (GUILayout.Button("선택 대상 하위 스캔", GUILayout.Height(28)))
                    ScanSelection();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"발견된 머티리얼: {foundMaterials.Count}개", EditorStyles.helpBox);

            if (foundMaterials.Count == 0)
            {
                EditorGUILayout.HelpBox("스캔된 머티리얼이 없습니다. 대상을 선택하고 스캔하세요.", MessageType.None);
                return;
            }

            EditorGUILayout.Space();
            DrawBulkButtons();
            EditorGUILayout.Space();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            for (int i = 0; i < foundMaterials.Count; i++)
                DrawMaterialRow(foundMaterials[i], i);
            EditorGUILayout.EndScrollView();
        }

        // ─── UI 드로우 ────────────────────────────────────────────────

        private void DrawTargetInfo()
        {
            var targets = GetSelectedRoots();
            EditorGUILayout.BeginVertical("box");
            if (targets.Count == 0)
            {
                EditorGUILayout.LabelField("대상: (Hierarchy에서 GameObject를 선택하세요)");
            }
            else if (targets.Count == 1)
            {
                EditorGUILayout.LabelField($"대상: {targets[0].name}");
            }
            else
            {
                EditorGUILayout.LabelField($"대상: {targets.Count}개 GameObject 선택됨");
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawBulkButtons()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("모두 변환", GUILayout.Height(28)))
                ConvertAll();

            using (new EditorGUI.DisabledScope(convertedMaterials.Count == 0))
            {
                if (GUILayout.Button("변환 모두 되돌리기", GUILayout.Height(28)))
                    RevertAll();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawMaterialRow(Material material, int index)
        {
            if (material == null) return;

            EditorGUILayout.BeginVertical("box");

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

            EditorGUILayout.BeginHorizontal();
            bool converted = convertedMaterials.Contains(material);
            if (!converted)
            {
                if (GUILayout.Button("변환"))
                    ConvertMaterial(material);
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
                    GUILayout.Button("변환 완료 ✓");

                if (GUILayout.Button("되돌리기", GUILayout.Width(80)))
                    RevertMaterial(material);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        // ─── 대상 / 스캔 ──────────────────────────────────────────────

        private bool HasValidTarget() => GetSelectedRoots().Count > 0;

        private static List<GameObject> GetSelectedRoots()
        {
            // Hierarchy(씬) GameObject만 대상으로 삼는다. 프로젝트 에셋 선택은 제외.
            return Selection.gameObjects
                .Where(go => go != null && go.scene.IsValid())
                .Distinct()
                .ToList();
        }

        private void ScanSelection()
        {
            foundMaterials.Clear();

            var roots = GetSelectedRoots();
            if (roots.Count == 0)
            {
                Debug.LogWarning("[lilToon변환기] Hierarchy에서 대상 GameObject를 선택하세요.");
                return;
            }

            var seen = new HashSet<Material>();
            foreach (var root in roots)
            {
                // 비활성 자식 포함
                var renderers = root.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    foreach (var mat in renderer.sharedMaterials)
                    {
                        if (mat == null) continue;
                        if (!seen.Add(mat)) continue;
                        if (skipAlreadyLilToon && IsLilToon(mat)) continue;
                        foundMaterials.Add(mat);
                    }
                }
            }

            Debug.Log($"[lilToon변환기] 대상 {roots.Count}개 하위 스캔 완료 — 머티리얼 {foundMaterials.Count}개");
            Repaint();
        }

        private static bool IsLilToon(Material material)
        {
            if (material == null || material.shader == null) return false;
            return material.shader.name.Contains("lilToon");
        }

        // ─── 변환 ──────────────────────────────────────────────────────

        private void ConvertAll()
        {
            var targets = foundMaterials.Where(m => m != null && !convertedMaterials.Contains(m)).ToList();
            if (targets.Count == 0)
            {
                EditorUtility.DisplayDialog("알림", "변환할 머티리얼이 없습니다.", "확인");
                return;
            }

            if (!EditorUtility.DisplayDialog("확인",
                    $"{targets.Count}개 머티리얼을 lilToon 셰이더로 변환합니다.\n실행 취소(Undo) 가능합니다.",
                    "변환", "취소"))
                return;

            foreach (var mat in targets)
                ConvertMaterial(mat);

            AssetDatabase.SaveAssets();
        }

        private void ConvertMaterial(Material material)
        {
            if (material == null) return;

            Shader lilShader = Shader.Find(LilToonShaderName);
            if (lilShader == null)
            {
                EditorUtility.DisplayDialog("실패",
                    $"'{LilToonShaderName}' 셰이더를 찾을 수 없습니다. lilToon 패키지가 설치되어 있는지 확인하세요.",
                    "확인");
                return;
            }

            if (IsLilToon(material))
            {
                Debug.Log($"[lilToon변환기] {material.name}: 이미 lilToon 셰이더입니다. 건너뜀");
                return;
            }

            Undo.RecordObject(material, $"Convert {material.name} to lilToon");

            if (!originalShaders.ContainsKey(material))
                originalShaders[material] = material.shader;

            // 변환 전 메인 텍스처/컬러/기타 값을 백업
            var backup = BackupMaterial(material);

            material.shader = lilShader;
            convertedMaterials.Add(material);

            RestoreToLilToon(material, backup);

            if (autoMapRenderingMode)
                ApplyLilToonRenderingMode(material, backup.renderingMode);

            EditorUtility.SetDirty(material);
            Debug.Log($"[lilToon변환기] {material.name}: {backup.sourceShaderName} → {LilToonShaderName} ({backup.renderingMode})");
        }

        private void RevertAll()
        {
            if (!EditorUtility.DisplayDialog("확인", "이 세션에서 변환한 머티리얼을 모두 되돌리시겠습니까?", "되돌리기", "취소"))
                return;

            foreach (var mat in new List<Material>(convertedMaterials))
                RevertMaterial(mat);

            AssetDatabase.SaveAssets();
        }

        private void RevertMaterial(Material material)
        {
            if (material == null) return;
            if (!originalShaders.TryGetValue(material, out var original) || original == null) return;

            Undo.RecordObject(material, $"Revert {material.name}");
            material.shader = original;
            convertedMaterials.Remove(material);

            EditorUtility.SetDirty(material);
            Debug.Log($"[lilToon변환기] {material.name} 되돌리기 완료");
        }

        // ─── 프로퍼티 백업 / lilToon 복원 ─────────────────────────────

        private struct MaterialBackup
        {
            public string sourceShaderName;
            public LilRenderingMode renderingMode;
            public Texture mainTex;
            public Vector2 mainScale;
            public Vector2 mainOffset;
            public bool hasColor;
            public Color color;
            public Texture bumpMap;
            public Texture emissionMap;
            public bool hasEmissionColor;
            public Color emissionColor;
        }

        // built-in / URP 양쪽 이름을 순서대로 시도한다.
        private static readonly string[] MainTexNames = { "_MainTex", "_BaseMap" };
        private static readonly string[] MainColorNames = { "_Color", "_BaseColor" };

        private static MaterialBackup BackupMaterial(Material material)
        {
            var b = new MaterialBackup
            {
                sourceShaderName = material.shader ? material.shader.name : "없음",
                renderingMode = DetectRenderingMode(material),
                mainScale = Vector2.one,
                mainOffset = Vector2.zero,
            };

            string texProp = FirstExistingProperty(material, MainTexNames);
            if (texProp != null)
            {
                b.mainTex = material.GetTexture(texProp);
                b.mainScale = material.GetTextureScale(texProp);
                b.mainOffset = material.GetTextureOffset(texProp);
            }
            else if (material.mainTexture != null)
            {
                b.mainTex = material.mainTexture;
            }

            string colorProp = FirstExistingProperty(material, MainColorNames);
            if (colorProp != null)
            {
                b.hasColor = true;
                b.color = material.GetColor(colorProp);
            }

            if (material.HasProperty("_BumpMap"))
                b.bumpMap = material.GetTexture("_BumpMap");

            if (material.HasProperty("_EmissionMap"))
                b.emissionMap = material.GetTexture("_EmissionMap");
            if (material.HasProperty("_EmissionColor"))
            {
                b.hasEmissionColor = true;
                b.emissionColor = material.GetColor("_EmissionColor");
            }

            return b;
        }

        private static void RestoreToLilToon(Material material, MaterialBackup b)
        {
            // lilToon 메인: _MainTex / _Color
            if (material.HasProperty("_MainTex"))
            {
                if (b.mainTex != null) material.SetTexture("_MainTex", b.mainTex);
                material.SetTextureScale("_MainTex", b.mainScale);
                material.SetTextureOffset("_MainTex", b.mainOffset);
            }
            if (b.hasColor && material.HasProperty("_Color"))
                material.SetColor("_Color", b.color);

            // 노멀맵: 있으면 활성화
            if (b.bumpMap != null && material.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap", b.bumpMap);
                if (material.HasProperty("_UseBumpMap"))
                    material.SetFloat("_UseBumpMap", 1f);
            }

            // 이멀션: 텍스처나 유의미한 컬러가 있으면 활성화
            bool hasEmission = b.emissionMap != null ||
                               (b.hasEmissionColor && b.emissionColor.maxColorComponent > 0f);
            if (hasEmission)
            {
                if (b.emissionMap != null && material.HasProperty("_EmissionMap"))
                    material.SetTexture("_EmissionMap", b.emissionMap);
                if (b.hasEmissionColor && material.HasProperty("_EmissionColor"))
                    material.SetColor("_EmissionColor", b.emissionColor);
                if (material.HasProperty("_UseEmission"))
                    material.SetFloat("_UseEmission", 1f);
            }
        }

        private static string FirstExistingProperty(Material material, string[] candidates)
        {
            foreach (var name in candidates)
            {
                if (material.HasProperty(name))
                    return name;
            }
            return null;
        }

        // ─── 렌더링 모드 판별 / lilToon 적용 ──────────────────────────

        /// <summary>
        /// 원본 머티리얼(built-in Standard / URP Lit / Unlit 등)의 렌더링 모드를 추정한다.
        /// 컷아웃(알파 클립) > 반투명 > 불투명 순으로 판정한다.
        /// </summary>
        private static LilRenderingMode DetectRenderingMode(Material material)
        {
            if (material == null) return LilRenderingMode.Opaque;

            string shaderName = material.shader ? material.shader.name : string.Empty;

            // 1) 셰이더 이름 힌트
            if (shaderName.IndexOf("Cutout", StringComparison.OrdinalIgnoreCase) >= 0)
                return LilRenderingMode.Cutout;

            // 2) 컷아웃 판정: 알파 클립 키워드/프로퍼티
            if (material.IsKeywordEnabled("_ALPHATEST_ON"))
                return LilRenderingMode.Cutout;
            if (material.HasProperty("_AlphaClip") && material.GetFloat("_AlphaClip") > 0.5f)
                return LilRenderingMode.Cutout;

            // 3) 반투명 판정: 키워드 / _Mode(Standard) / _Surface(URP)
            bool transparent =
                material.IsKeywordEnabled("_ALPHABLEND_ON") ||
                material.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON") ||
                material.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT");

            // Standard: _Mode (0 Opaque, 1 Cutout, 2 Fade, 3 Transparent)
            if (!transparent && material.HasProperty("_Mode"))
            {
                float mode = material.GetFloat("_Mode");
                if (Mathf.Approximately(mode, 1f)) return LilRenderingMode.Cutout;
                if (mode >= 2f) transparent = true;
            }

            // URP: _Surface (0 Opaque, 1 Transparent)
            if (!transparent && material.HasProperty("_Surface") && material.GetFloat("_Surface") > 0.5f)
                transparent = true;

            // 4) 렌더 큐 힌트 (Transparent 이상)
            if (!transparent && material.renderQueue >= (int)UnityEngine.Rendering.RenderQueue.Transparent)
                transparent = true;

            if (shaderName.IndexOf("Transparent", StringComparison.OrdinalIgnoreCase) >= 0)
                transparent = true;

            return transparent ? LilRenderingMode.Transparent : LilRenderingMode.Opaque;
        }

        // lilToon 타입 리플렉션 캐시
        private static Type _lilMaterialUtilsType;
        private static Type _lilRenderingModeType;
        private static Type _lilTransparentModeType;
        private static MethodInfo _setupMethod;
        private static bool _lilReflectionResolved;

        /// <summary>
        /// lilToon.lilMaterialUtils.SetupMaterialWithRenderingMode 를 리플렉션으로 호출해
        /// 블렌드/큐/ZWrite 등 렌더링 모드 관련 세팅을 lilToon 규약대로 일괄 적용한다.
        /// (asmdef 참조를 추가하지 않기 위해 리플렉션 사용 — LilToonDissolveBuildProcessor 와 동일 정책)
        /// </summary>
        private static void ApplyLilToonRenderingMode(Material material, LilRenderingMode mode)
        {
            if (!ResolveLilReflection())
            {
                // lilToon 유틸을 못 찾으면 최소한 렌더 큐만이라도 맞춘다.
                material.renderQueue = mode == LilRenderingMode.Transparent
                    ? (int)UnityEngine.Rendering.RenderQueue.Transparent
                    : mode == LilRenderingMode.Cutout
                        ? (int)UnityEngine.Rendering.RenderQueue.AlphaTest
                        : (int)UnityEngine.Rendering.RenderQueue.Geometry;
                return;
            }

            try
            {
                object renderingModeEnum = Enum.ToObject(_lilRenderingModeType, (int)mode);
                object transparentModeEnum = Enum.ToObject(_lilTransparentModeType, 0); // Normal
                // (material, renderingMode, transparentMode, isoutl, islite, istess, ismulti)
                _setupMethod.Invoke(null, new object[]
                {
                    material, renderingModeEnum, transparentModeEnum, false, false, false, false
                });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[lilToon변환기] 렌더링 모드 적용 실패 ({material.name}): {e.Message}");
            }
        }

        private static bool ResolveLilReflection()
        {
            if (_lilReflectionResolved)
                return _setupMethod != null;

            _lilReflectionResolved = true;

            _lilMaterialUtilsType = FindType("lilToon.lilMaterialUtils");
            _lilRenderingModeType = FindType("lilToon.RenderingMode");
            _lilTransparentModeType = FindType("lilToon.TransparentMode");

            if (_lilMaterialUtilsType == null || _lilRenderingModeType == null || _lilTransparentModeType == null)
            {
                Debug.LogWarning("[lilToon변환기] lilToon 머티리얼 유틸 타입을 찾을 수 없어 렌더링 모드 자동 매핑을 건너뜁니다. (렌더 큐만 설정)");
                return false;
            }

            // internal static 이므로 NonPublic 포함해 7개 인자 오버로드를 찾는다.
            _setupMethod = _lilMaterialUtilsType
                .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m => m.Name == "SetupMaterialWithRenderingMode" && m.GetParameters().Length == 7);

            if (_setupMethod == null)
            {
                Debug.LogWarning("[lilToon변환기] SetupMaterialWithRenderingMode(7-arg) 를 찾을 수 없어 렌더링 모드 자동 매핑을 건너뜁니다.");
                return false;
            }

            return true;
        }

        private static Type FindType(string typeName)
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType(typeName))
                .FirstOrDefault(type => type != null);
        }
    }
}