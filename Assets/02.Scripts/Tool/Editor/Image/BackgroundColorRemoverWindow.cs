#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Tool.Editor.Image
{
    /// <summary>
    /// 단색/스튜디오 배경 이미지에서 배경색을 알파로 제거하는 에디터 툴.
    /// AI 세그멘테이션 대신 테두리 연결 영역 기반 크로마키를 사용한다.
    /// </summary>
    public sealed class BackgroundColorRemoverWindow : EditorWindow
    {
        private enum SampleMode
        {
            Corners,
            Edges,
            Manual
        }

        private Texture2D _sourceTexture;
        private Texture2D _previewTexture;
        private Texture2D _checkerboardTexture;
        private Vector2 _scroll;

        private SampleMode _sampleMode = SampleMode.Corners;
        private Color _manualBackgroundColor = Color.white;
        private float _tolerance = 0.16f;
        private float _softness = 0.08f;
        private bool _borderConnectedOnly = true;
        private bool _decontaminateEdges = true;
        private string _saveFolder = "Assets/04.Images/BackgroundRemoved";
        private string _status = "Project 창에서 이미지를 선택하거나 Source Texture를 지정하세요.";

        private const int CheckerSize = 12;
        private const int MaxPreviewSize = 512;

        [MenuItem("UPlayGround/Util/Background Color Remover")]
        private static void Open()
        {
            var window = GetWindow<BackgroundColorRemoverWindow>("BG Remover");
            window.minSize = new Vector2(480f, 560f);
            window.TryUseSelection();
            window.Show();
        }

        [MenuItem("Assets/UPlayGround/Remove Background Color", true)]
        private static bool ValidateOpenFromSelection()
        {
            return Selection.activeObject is Texture2D;
        }

        [MenuItem("Assets/UPlayGround/Remove Background Color")]
        private static void OpenFromSelection()
        {
            Open();
        }

        private void OnEnable()
        {
            TryUseSelection();
        }

        private void OnSelectionChange()
        {
            if (focusedWindow == this && Selection.activeObject is Texture2D texture)
            {
                SetSource(texture);
            }
        }

        private void OnDisable()
        {
            DestroyTexture(ref _previewTexture);
            DestroyTexture(ref _checkerboardTexture);
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField("Background Color Remover", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Photoroom류 배경 제거처럼 결과를 투명 PNG로 저장합니다. 이 툴은 배경이 단색에 가깝거나 이미지 테두리와 연결된 경우에 맞춘 로컬 처리 방식입니다.",
                MessageType.Info);

            DrawInputSection();
            DrawOptionSection();
            DrawActionSection();
            DrawPreviewSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawInputSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("입력", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            var texture = (Texture2D)EditorGUILayout.ObjectField("Source Texture", _sourceTexture, typeof(Texture2D), false);
            if (EditorGUI.EndChangeCheck())
            {
                SetSource(texture);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Use Selected Texture"))
            {
                TryUseSelection();
            }

            using (new EditorGUI.DisabledScope(_sourceTexture == null))
            {
                if (GUILayout.Button("Ping Source"))
                {
                    EditorGUIUtility.PingObject(_sourceTexture);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawOptionSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("제거 옵션", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _sampleMode = (SampleMode)EditorGUILayout.EnumPopup("Background Sample", _sampleMode);
            using (new EditorGUI.DisabledScope(_sampleMode != SampleMode.Manual))
            {
                _manualBackgroundColor = EditorGUILayout.ColorField("Manual Color", _manualBackgroundColor);
            }

            _tolerance = EditorGUILayout.Slider("Tolerance", _tolerance, 0.01f, 0.6f);
            _softness = EditorGUILayout.Slider("Edge Softness", _softness, 0f, 0.35f);
            _borderConnectedOnly = EditorGUILayout.ToggleLeft("테두리와 연결된 배경만 제거", _borderConnectedOnly);
            _decontaminateEdges = EditorGUILayout.ToggleLeft("반투명 가장자리 배경색 보정", _decontaminateEdges);
            _saveFolder = EditorGUILayout.TextField("Save Folder", _saveFolder);

            if (EditorGUI.EndChangeCheck() && _sourceTexture != null)
            {
                GeneratePreview();
            }
        }

        private void DrawActionSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(_status, MessageType.None);

            using (new EditorGUI.DisabledScope(_sourceTexture == null))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Update Preview", GUILayout.Height(30f)))
                {
                    GeneratePreview();
                }

                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Save PNG", GUILayout.Height(30f)))
                {
                    SavePng();
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawPreviewSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            if (_previewTexture == null)
            {
                return;
            }

            float aspect = (float)_previewTexture.width / _previewTexture.height;
            float width = Mathf.Min(position.width - 28f, MaxPreviewSize);
            float height = width / aspect;
            if (height > MaxPreviewSize)
            {
                height = MaxPreviewSize;
                width = height * aspect;
            }

            Rect rect = GUILayoutUtility.GetRect(width, height);
            DrawCheckerboard(rect);
            EditorGUI.DrawPreviewTexture(rect, _previewTexture, null, ScaleMode.ScaleToFit);
        }

        private void TryUseSelection()
        {
            if (Selection.activeObject is Texture2D texture)
            {
                SetSource(texture);
            }
        }

        private void SetSource(Texture2D texture)
        {
            _sourceTexture = texture;
            DestroyTexture(ref _previewTexture);

            if (_sourceTexture == null)
            {
                _status = "Project 창에서 이미지를 선택하거나 Source Texture를 지정하세요.";
                Repaint();
                return;
            }

            GeneratePreview();
        }

        private void GeneratePreview()
        {
            if (_sourceTexture == null)
            {
                return;
            }

            var readable = LoadReadableTexture(_sourceTexture);
            if (readable == null)
            {
                _status = "이미지를 읽을 수 없습니다. 프로젝트 에셋 PNG/JPG/TGA 텍스처를 지정하세요.";
                return;
            }

            DestroyTexture(ref _previewTexture);
            _previewTexture = RemoveBackground(readable);
            DestroyImmediate(readable);

            _status = $"프리뷰 갱신 완료: {_sourceTexture.width}x{_sourceTexture.height}";
            Repaint();
        }

        private void SavePng()
        {
            if (_sourceTexture == null)
            {
                return;
            }

            var readable = LoadReadableTexture(_sourceTexture);
            if (readable == null)
            {
                EditorUtility.DisplayDialog("저장 실패", "이미지를 읽을 수 없습니다.", "확인");
                return;
            }

            var result = RemoveBackground(readable);
            DestroyImmediate(readable);

            if (!Directory.Exists(_saveFolder))
            {
                Directory.CreateDirectory(_saveFolder);
            }

            string fileName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(_sourceTexture));
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = _sourceTexture.name;
            }

            string path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(_saveFolder, $"{fileName}_NoBg.png").Replace('\\', '/'));
            File.WriteAllBytes(path, result.EncodeToPNG());
            DestroyImmediate(result);

            AssetDatabase.Refresh();
            var savedAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            EditorGUIUtility.PingObject(savedAsset);
            _status = $"저장 완료: {path}";
        }

        private Texture2D RemoveBackground(Texture2D source)
        {
            int width = source.width;
            int height = source.height;
            Color32[] sourcePixels = source.GetPixels32();
            Color backgroundColor = GetBackgroundColor(sourcePixels, width, height);
            bool[] targetMask = _borderConnectedOnly
                ? BuildBorderConnectedMask(sourcePixels, width, height, backgroundColor)
                : BuildColorMask(sourcePixels, backgroundColor);

            var outputPixels = new Color32[sourcePixels.Length];
            float hardThreshold = _tolerance;
            float softThreshold = Mathf.Max(hardThreshold + _softness, hardThreshold + 0.0001f);

            for (int i = 0; i < sourcePixels.Length; i++)
            {
                Color color = sourcePixels[i];
                float alpha = color.a;

                if (targetMask[i])
                {
                    float distance = GetColorDistance(color, backgroundColor);
                    alpha *= Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(hardThreshold, softThreshold, distance));
                }

                if (_decontaminateEdges && alpha > 0.001f && alpha < 0.999f)
                {
                    color.r = Mathf.Clamp01((color.r - backgroundColor.r * (1f - alpha)) / alpha);
                    color.g = Mathf.Clamp01((color.g - backgroundColor.g * (1f - alpha)) / alpha);
                    color.b = Mathf.Clamp01((color.b - backgroundColor.b * (1f - alpha)) / alpha);
                }

                color.a = alpha;
                outputPixels[i] = color;
            }

            var result = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = source.name + "_NoBg",
                filterMode = source.filterMode,
                wrapMode = source.wrapMode
            };
            result.SetPixels32(outputPixels);
            result.Apply();
            return result;
        }

        private Color GetBackgroundColor(Color32[] pixels, int width, int height)
        {
            if (_sampleMode == SampleMode.Manual)
            {
                return _manualBackgroundColor;
            }

            var samples = new List<Color>(Mathf.Max(width, height) * 4);
            if (_sampleMode == SampleMode.Corners)
            {
                int radius = Mathf.Clamp(Mathf.Min(width, height) / 20, 2, 24);
                AddSampleRect(samples, pixels, width, 0, 0, radius, radius);
                AddSampleRect(samples, pixels, width, width - radius, 0, radius, radius);
                AddSampleRect(samples, pixels, width, 0, height - radius, radius, radius);
                AddSampleRect(samples, pixels, width, width - radius, height - radius, radius, radius);
            }
            else
            {
                for (int x = 0; x < width; x++)
                {
                    samples.Add(pixels[x]);
                    samples.Add(pixels[(height - 1) * width + x]);
                }

                for (int y = 0; y < height; y++)
                {
                    samples.Add(pixels[y * width]);
                    samples.Add(pixels[y * width + width - 1]);
                }
            }

            return Average(samples);
        }

        private static void AddSampleRect(List<Color> samples, Color32[] pixels, int width, int x, int y, int sampleWidth, int sampleHeight)
        {
            for (int sy = 0; sy < sampleHeight; sy++)
            {
                for (int sx = 0; sx < sampleWidth; sx++)
                {
                    samples.Add(pixels[(y + sy) * width + x + sx]);
                }
            }
        }

        private static Color Average(List<Color> samples)
        {
            if (samples.Count == 0)
            {
                return Color.clear;
            }

            Color total = Color.clear;
            for (int i = 0; i < samples.Count; i++)
            {
                total += samples[i];
            }

            total /= samples.Count;
            total.a = 1f;
            return total;
        }

        private bool[] BuildBorderConnectedMask(Color32[] pixels, int width, int height, Color backgroundColor)
        {
            var mask = new bool[pixels.Length];
            var queue = new Queue<int>();

            for (int x = 0; x < width; x++)
            {
                TryEnqueue(x, pixels, mask, queue, backgroundColor);
                TryEnqueue((height - 1) * width + x, pixels, mask, queue, backgroundColor);
            }

            for (int y = 1; y < height - 1; y++)
            {
                TryEnqueue(y * width, pixels, mask, queue, backgroundColor);
                TryEnqueue(y * width + width - 1, pixels, mask, queue, backgroundColor);
            }

            float softThreshold = _tolerance + _softness;
            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                int x = index % width;
                int y = index / width;

                TryEnqueueNeighbor(x - 1, y, width, height, pixels, mask, queue, backgroundColor, softThreshold);
                TryEnqueueNeighbor(x + 1, y, width, height, pixels, mask, queue, backgroundColor, softThreshold);
                TryEnqueueNeighbor(x, y - 1, width, height, pixels, mask, queue, backgroundColor, softThreshold);
                TryEnqueueNeighbor(x, y + 1, width, height, pixels, mask, queue, backgroundColor, softThreshold);
            }

            return mask;
        }

        private bool[] BuildColorMask(Color32[] pixels, Color backgroundColor)
        {
            var mask = new bool[pixels.Length];
            float softThreshold = _tolerance + _softness;
            for (int i = 0; i < pixels.Length; i++)
            {
                mask[i] = GetColorDistance(pixels[i], backgroundColor) <= softThreshold;
            }

            return mask;
        }

        private void TryEnqueue(int index, Color32[] pixels, bool[] mask, Queue<int> queue, Color backgroundColor)
        {
            if (mask[index] || GetColorDistance(pixels[index], backgroundColor) > _tolerance + _softness)
            {
                return;
            }

            mask[index] = true;
            queue.Enqueue(index);
        }

        private static void TryEnqueueNeighbor(
            int x,
            int y,
            int width,
            int height,
            Color32[] pixels,
            bool[] mask,
            Queue<int> queue,
            Color backgroundColor,
            float threshold)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return;
            }

            int index = y * width + x;
            if (mask[index] || GetColorDistance(pixels[index], backgroundColor) > threshold)
            {
                return;
            }

            mask[index] = true;
            queue.Enqueue(index);
        }

        private static float GetColorDistance(Color a, Color b)
        {
            float dr = a.r - b.r;
            float dg = a.g - b.g;
            float db = a.b - b.b;
            return Mathf.Sqrt(dr * dr + dg * dg + db * db);
        }

        private static Texture2D LoadReadableTexture(Texture2D source)
        {
            string assetPath = AssetDatabase.GetAssetPath(source);
            if (!string.IsNullOrEmpty(assetPath) && File.Exists(assetPath))
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                return ImageConversion.LoadImage(texture, File.ReadAllBytes(assetPath)) ? texture : null;
            }

            try
            {
                var texture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                texture.SetPixels32(source.GetPixels32());
                texture.Apply();
                return texture;
            }
            catch (UnityException)
            {
                return null;
            }
        }

        private void DrawCheckerboard(Rect rect)
        {
            if (_checkerboardTexture == null)
            {
                _checkerboardTexture = CreateCheckerboardTexture();
            }

            GUI.DrawTextureWithTexCoords(
                rect,
                _checkerboardTexture,
                new Rect(0f, 0f, rect.width / CheckerSize, rect.height / CheckerSize));
        }

        private static Texture2D CreateCheckerboardTexture()
        {
            int size = CheckerSize * 2;
            var texture = new Texture2D(size, size, TextureFormat.RGB24, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point
            };

            Color light = new Color(0.74f, 0.74f, 0.74f);
            Color dark = new Color(0.48f, 0.48f, 0.48f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, ((x / CheckerSize + y / CheckerSize) % 2 == 0) ? light : dark);
                }
            }

            texture.Apply();
            return texture;
        }

        private static void DestroyTexture(ref Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            DestroyImmediate(texture);
            texture = null;
        }
    }
}
#endif
