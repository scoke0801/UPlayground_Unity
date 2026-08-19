using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Dialogue;

namespace UPlayGround.Dialogue.Editor
{
    /// <summary>
    /// IMGUI 기반 노드 그래프 에디터.
    /// Window > Dialogue Graph Editor 또는 DialogueGraphSO 더블클릭으로 열 수 있습니다.
    /// </summary>
    public class DialogueGraphEditor : EditorWindow
    {
        // ── 레이아웃 상수 ─────────────────────────────────────────────────
        private const float NodeWidth       = 210f;
        private const float NodeHeaderH     = 26f;
        private const float NodeBodyPadding = 8f;
        private const float NodeBodyInset   = 10f;   // 노드 좌우 안쪽 여백
        private const float NodeTextIndent  = 6f;    // 강조 바(2px) 다음 텍스트 시작 위치
        /// <summary>노드 본문 대사 미리보기가 쓰는 폭. 높이 계산과 실제 그리기가 같은 값을 써야 글자가 잘리지 않는다.</summary>
        private const float NodeTextWidth   = NodeWidth - NodeBodyInset * 2f - NodeTextIndent;
        private const float GridSize        = 20f;
        private const float PortRadius      = 6f;
        private const float InspectorWidth  = 270f;
        private const float InspectorPadding      = 10f;
        private const float InspectorHeaderHeight = 28f;
        private const float InspectorLabelRatio   = 0.45f;

        // ── 툴바 레이아웃 상수 ────────────────────────────────────────────
        // 창이 좁으면 오른쪽 버튼이 잘리므로, 요구 폭을 미리 계산해 두 줄로 접는다.
        private const float ToolbarRowHeight   = 28f;
        private const float ToolbarEdgePadding = 6f;
        private const float ToolbarItemGap     = 2f;   // GUILayout 기본 요소 간격
        private const float ToolbarSepWidth    = 9f;   // Space(4) + 구분선 1px + Space(4)
        private const float ToolbarGraphFieldW = 200f;
        private const float ToolbarAddLabelW   = 28f;
        private const float ToolbarNodeBtnW    = 62f;
        private const float ToolbarAutoLayoutW = 80f;
        private const float ToolbarFitViewW    = 60f;
        private const float ToolbarZoomBtnW    = 20f;
        private const float ToolbarZoomLabelW  = 38f;
        private const float ToolbarJsonBtnW    = 90f;
        private const float ToolbarSaveBtnW    = 80f;

        private const float FitViewMargin = 80f;
        private const float MinZoom = 0.2f;
        private const float MaxZoom = 2f;

        // ── 상태 ─────────────────────────────────────────────────────────
        private DialogueGraphSO _graph;
        private Vector2         _scroll;
        private float           _zoom = 1f;
        private string          _selectedNodeId;

        // 연결선 드래그
        private bool    _isDraggingConnection;
        private string  _connectionSourceId;
        private string  _connectionSourcePort;
        private Vector2 _connectionDragPos;

        // 단일 노드 드래그
        private string  _draggingNodeId;
        private Vector2 _draggingNodePos;

        // ── 다중 선택 ─────────────────────────────────────────────────────
        private readonly HashSet<string>             _selectedNodeIds  = new();
        private readonly Dictionary<string, Vector2> _multiDragOrigins = new();
        private bool    _isMultiDragging;
        private Vector2 _multiDragStartMouse;
        private Vector2 _multiDragCurrentMouse;

        // 마퀴(rubber-band) 셀렉션
        private bool    _isMarqueeSelecting;
        private Vector2 _marqueeStart;
        private Vector2 _marqueeEnd;

        private Vector2 _inspectorScroll;

        /// <summary>창 폭이 좁아 툴바를 두 줄로 접었는지 여부. 캔버스/인스펙터 상단 오프셋에 반영된다.</summary>
        private bool _isToolbarWrapped;

        // ── 노드 높이 캐시 ────────────────────────────────────────────────
        private readonly Dictionary<string, float> _heightCache = new();

        // ── GUIStyle 캐시 ─────────────────────────────────────────────────
        // 스타일은 EnsureStyles에서 한 번만 구성하고, 그리기 시점에는 normal.textColor만 바꾼다.
        // fontSize/alignment/clipping을 그리면서 덮어쓰면 다음 사용처가 다른 크기로 그려지고,
        // 높이 계산(CalcHeight)과 실제 렌더 크기가 어긋나 노드 안에서 글자가 잘린다.
        private static GUIStyle _styleToolbarLabel;
        private static GUIStyle _styleToolbarCenter;
        private static GUIStyle _styleCanvasHint;
        private static GUIStyle _styleNodeTitle;
        private static GUIStyle _styleNodeStartTag;
        private static GUIStyle _styleNodeIdTag;
        private static GUIStyle _styleNodeChannelTag;
        private static GUIStyle _styleNodeText;
        private static GUIStyle _styleChoiceArrow;
        private static GUIStyle _styleChoiceText;
        private static GUIStyle _styleConditionBox;
        private static GUIStyle _styleBadge;
        private static GUIStyle _styleEventTag;
        private static GUIStyle _styleEmptyHint;
        private static GUIStyle _styleInspectorHeader;
        private static GUIStyle _styleInspectorBadge;
        private static GUIStyle _styleInspectorChannel;
        private static GUIStyle _styleInspectorMini;
        private static GUIStyle _styleInspectorHint;

        /// <summary>줄바꿈·높이 계산 전용. 줌과 무관한 기준 크기라 노드 레이아웃이 줌에 흔들리지 않는다.</summary>
        private static GUIStyle _styleNodeTextBase;

        /// <summary>캔버스 스타일이 마지막으로 만들어진 줌 배율.</summary>
        private static float _canvasStyleZoom = -1f;

        // 캔버스 스타일의 기준(줌 1배) 글자 크기.
        private const int NodeTitleFontSize   = 10;
        private const int NodeTagFontSize     = 8;
        private const int NodeIdFontSize      = 9;
        private const int NodeTextFontSize    = 10;
        private const int NodeChoiceFontSize  = 10;
        private const int NodeBadgeFontSize   = 9;

        private static GUIStyle Mini(int size, TextAnchor anchor, bool clip = true) =>
            new(EditorStyles.miniLabel)
            {
                fontSize  = size,
                alignment = anchor,
                wordWrap  = false,
                clipping  = clip ? TextClipping.Clip : TextClipping.Overflow,
            };

        /// <summary>패딩이 0인 스타일. CalcSize가 순수 글자 폭을 돌려주도록 한다.</summary>
        private static GUIStyle PlainText(int size)
        {
            var style = Mini(size, TextAnchor.UpperLeft);
            style.padding       = new RectOffset(0, 0, 0, 0);
            style.margin        = new RectOffset(0, 0, 0, 0);
            style.contentOffset = Vector2.zero;
            return style;
        }

        /// <summary>창 크롬(툴바·인스펙터) 스타일. 줌의 영향을 받지 않으므로 한 번만 만든다.</summary>
        private static void EnsureStyles()
        {
            if (_styleToolbarLabel != null) return;

            _styleToolbarLabel     = Mini(9,  TextAnchor.MiddleLeft);
            _styleToolbarCenter    = Mini(9,  TextAnchor.MiddleCenter);
            _styleCanvasHint       = Mini(10, TextAnchor.MiddleCenter, clip: false);
            _styleNodeTextBase     = PlainText(NodeTextFontSize);
            _styleInspectorHeader  = new GUIStyle(EditorStyles.boldLabel)
                { fontSize = 10, alignment = TextAnchor.MiddleLeft };
            _styleInspectorBadge   = new GUIStyle(Mini(10, TextAnchor.MiddleCenter)) { fontStyle = FontStyle.Bold };
            _styleInspectorChannel = Mini(9,  TextAnchor.MiddleCenter);
            _styleInspectorMini    = Mini(9,  TextAnchor.UpperLeft);
            _styleInspectorHint    = Mini(10, TextAnchor.MiddleCenter, clip: false);
        }

        /// <summary>
        /// 캔버스 안에서 쓰는 스타일. 줌은 GUI.matrix가 아니라 글자 크기로 반영한다.
        /// GUI.matrix에 배율을 걸면 사각형은 배율대로 커지지만 IMGUI 텍스트 래스터화는 따라오지 않아,
        /// 박스 크기 계산과 실제 글자 크기가 어긋나 노드 안에서 글자가 잘린다.
        /// </summary>
        private void EnsureCanvasStyles()
        {
            if (_styleNodeText != null && Mathf.Approximately(_canvasStyleZoom, _zoom)) return;
            _canvasStyleZoom = _zoom;

            // 내림으로 잰다. 올림이면 그려지는 글자가 계산 폭보다 넓어져 다시 잘릴 수 있다.
            int Scaled(int baseSize) => Mathf.Max(1, Mathf.FloorToInt(baseSize * _zoom));

            _styleNodeTitle      = new GUIStyle(EditorStyles.boldLabel)
                { fontSize = Scaled(NodeTitleFontSize), alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Clip };
            _styleNodeStartTag   = Mini(Scaled(NodeTagFontSize),    TextAnchor.MiddleRight);
            _styleNodeIdTag      = Mini(Scaled(NodeIdFontSize),     TextAnchor.MiddleRight);
            _styleNodeChannelTag = Mini(Scaled(NodeTagFontSize),    TextAnchor.MiddleCenter);
            _styleNodeText       = PlainText(Scaled(NodeTextFontSize));
            _styleChoiceArrow    = Mini(Scaled(NodeTagFontSize),    TextAnchor.MiddleCenter);
            _styleChoiceText     = Mini(Scaled(NodeChoiceFontSize), TextAnchor.UpperLeft);
            _styleConditionBox   = Mini(Scaled(NodeChoiceFontSize), TextAnchor.MiddleCenter);
            _styleBadge          = Mini(Scaled(NodeBadgeFontSize),  TextAnchor.MiddleCenter);
            _styleEventTag       = Mini(Scaled(NodeIdFontSize),     TextAnchor.UpperLeft);
            _styleEmptyHint      = Mini(Scaled(NodeChoiceFontSize), TextAnchor.UpperLeft);
        }

        // ── 색상 팔레트 ──────────────────────────────────────────────────
        private static readonly Color BgCanvas      = new(0.07f, 0.08f, 0.09f);
        private static readonly Color BgNode        = new(0.10f, 0.11f, 0.14f);
        private static readonly Color BgInspector   = new(0.08f, 0.09f, 0.10f);
        private static readonly Color BorderNormal  = new(0.18f, 0.20f, 0.26f);
        private static readonly Color BorderSelect  = new(0.31f, 0.62f, 1.00f);
        private static readonly Color GridColor     = new(1f, 1f, 1f, 0.04f);
        private static readonly Color TextSecond    = new(0.53f, 0.54f, 0.64f);
        private static readonly Color TextMuted     = new(0.31f, 0.35f, 0.42f);
        private static readonly Color TagBg         = new(0f, 0f, 0f, 0.35f);
        private static readonly Color MarqueeColor  = new(0.31f, 0.62f, 1.00f, 0.15f);
        private static readonly Color MarqueeBorder = new(0.31f, 0.62f, 1.00f, 0.6f);

        private static Color NodeColor(NodeType t) => t switch
        {
            NodeType.Talk      => new Color(0.31f, 0.62f, 1.00f),
            NodeType.Choice    => new Color(0.24f, 0.86f, 0.52f),
            NodeType.Condition => new Color(0.96f, 0.65f, 0.14f),
            NodeType.Event     => new Color(0.65f, 0.55f, 0.98f),
            NodeType.End       => new Color(1.00f, 0.42f, 0.42f),
            _                  => new Color(0.34f, 0.83f, 0.93f),
        };

        // ── 메뉴 / 오픈 에셋 ─────────────────────────────────────────────

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/내러티브/대화/대화 그래프 에디터", priority = UPlayGround.Tool.Editor.UPlaygroundMenuPriority.NarrativeDialogue)]
        public static void Open() => GetWindow<DialogueGraphEditor>("Dialogue Editor");

        [UnityEditor.Callbacks.OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int _)
        {
            if (EditorUtility.InstanceIDToObject(instanceId) is DialogueGraphSO graph)
            {
                var win = GetWindow<DialogueGraphEditor>("Dialogue Editor");
                win.LoadGraph(graph);
                return true;
            }
            return false;
        }

        // ── 라이프사이클 ─────────────────────────────────────────────────

        private void OnEnable()  => Undo.undoRedoPerformed += OnUndoRedo;
        private void OnDisable() => Undo.undoRedoPerformed -= OnUndoRedo;

        private void OnUndoRedo()
        {
            _heightCache.Clear();
            if (_graph != null) _graph.InvalidateCache();
            Repaint();
        }

        private void LoadGraph(DialogueGraphSO graph)
        {
            _graph = graph;
            _selectedNodeId = null;
            _selectedNodeIds.Clear();
            _heightCache.Clear();
            Repaint();
        }

        // ── OnGUI ─────────────────────────────────────────────────────────

        private void OnGUI()
        {
            EnsureStyles();
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), BgCanvas);
            DrawToolbar();

            if (_graph == null)
            {
                _styleCanvasHint.normal.textColor = TextMuted;
                GUI.Label(new Rect(0, ToolbarHeight, position.width, position.height - ToolbarHeight),
                    "DialogueGraphSO를 선택하거나 더블클릭해서 열어주세요.", _styleCanvasHint);
                return;
            }

            float top        = ToolbarHeight;
            float canvasW    = position.width - InspectorWidth;
            var   canvasRect = new Rect(0, top, canvasW, position.height - top);

            DrawCanvas(canvasRect);
            DrawInspector(new Rect(canvasW, top, InspectorWidth, position.height - top));
            HandleShortcuts();
        }

        // ── 툴바 ─────────────────────────────────────────────────────────

        /// <summary>현재 툴바가 차지하는 높이. 접힌 상태면 두 줄이다.</summary>
        private float ToolbarHeight => _isToolbarWrapped ? ToolbarRowHeight * 2f : ToolbarRowHeight;

        /// <summary>왼쪽 그룹(그래프 필드 · 노드 추가 · 뷰 · 줌)이 요구하는 폭.</summary>
        private static float ToolbarLeftGroupWidth
        {
            get
            {
                int   nodeTypeCount = Enum.GetValues(typeof(NodeType)).Length;
                float width = ToolbarEdgePadding
                            + ToolbarGraphFieldW + ToolbarSepWidth
                            + ToolbarAddLabelW + nodeTypeCount * ToolbarNodeBtnW + ToolbarSepWidth
                            + ToolbarAutoLayoutW + ToolbarFitViewW + ToolbarSepWidth
                            + ToolbarZoomBtnW * 2f + ToolbarZoomLabelW;
                return width + ToolbarItemGap * (7 + nodeTypeCount);
            }
        }

        /// <summary>오른쪽 그룹(JSON IO · 저장)이 요구하는 폭.</summary>
        private static float ToolbarRightGroupWidth =>
            ToolbarSepWidth + ToolbarJsonBtnW * 2f + ToolbarSepWidth + ToolbarSaveBtnW
            + ToolbarEdgePadding + ToolbarItemGap * 3;

        private void DrawToolbar()
        {
            _isToolbarWrapped = position.width < ToolbarLeftGroupWidth + ToolbarRightGroupWidth;

            float h = ToolbarHeight;
            EditorGUI.DrawRect(new Rect(0, 0, position.width, h), new Color(0.08f, 0.09f, 0.10f));
            EditorGUI.DrawRect(new Rect(0, h - 1, position.width, 1), BorderNormal);

            EditorGUILayout.BeginHorizontal(GUILayout.Height(ToolbarRowHeight));
            DrawToolbarLeftGroup();
            if (_isToolbarWrapped)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal(GUILayout.Height(ToolbarRowHeight));
            }
            GUILayout.FlexibleSpace();
            DrawToolbarRightGroup();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbarLeftGroup()
        {
            GUILayout.Space(ToolbarEdgePadding);

            var prev = _graph;
            _graph = (DialogueGraphSO)EditorGUILayout.ObjectField(
                _graph, typeof(DialogueGraphSO), false, GUILayout.Width(ToolbarGraphFieldW));
            if (_graph != prev) { _selectedNodeId = null; _selectedNodeIds.Clear(); _heightCache.Clear(); }

            DrawToolbarSep();

            _styleToolbarLabel.normal.textColor = TextMuted;
            GUILayout.Label("ADD", _styleToolbarLabel, GUILayout.Width(ToolbarAddLabelW));

            foreach (NodeType t in Enum.GetValues(typeof(NodeType)))
            {
                if (GUILayout.Button(t.ToString().ToUpper(), MakeToolbarNodeBtn(NodeColor(t)),
                        GUILayout.Width(ToolbarNodeBtnW), GUILayout.Height(20)))
                    AddNode(t);
            }

            DrawToolbarSep();

            if (GUILayout.Button("Auto Layout", EditorStyles.toolbarButton, GUILayout.Width(ToolbarAutoLayoutW))) AutoLayout();
            if (GUILayout.Button("Fit View",    EditorStyles.toolbarButton, GUILayout.Width(ToolbarFitViewW)))    FitView();

            DrawToolbarSep();

            if (GUILayout.Button("-", EditorStyles.toolbarButton, GUILayout.Width(ToolbarZoomBtnW))) ChangeZoom(-0.15f);
            _styleToolbarCenter.normal.textColor = TextMuted;
            GUILayout.Label($"{Mathf.RoundToInt(_zoom * 100)}%", _styleToolbarCenter, GUILayout.Width(ToolbarZoomLabelW));
            if (GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.Width(ToolbarZoomBtnW))) ChangeZoom(0.15f);
        }

        private void DrawToolbarRightGroup()
        {
            DrawToolbarSep();

            var importStyle = new GUIStyle(EditorStyles.toolbarButton)
                { normal = { textColor = new Color(0.34f, 0.83f, 0.93f) } };
            var exportStyle = new GUIStyle(EditorStyles.toolbarButton)
                { normal = { textColor = new Color(0.24f, 0.86f, 0.52f) } };

            if (GUILayout.Button("JSON Import", importStyle, GUILayout.Width(ToolbarJsonBtnW)))
                DialogueJsonIO.ImportFromJson(_graph);

            GUI.enabled = _graph != null;
            if (GUILayout.Button("JSON Export", exportStyle, GUILayout.Width(ToolbarJsonBtnW)))
                DialogueJsonIO.ExportToJson(_graph);
            GUI.enabled = true;

            DrawToolbarSep();

            var saveStyle = new GUIStyle(EditorStyles.toolbarButton)
                { normal = { textColor = new Color(0.31f, 0.62f, 1f) } };
            if (GUILayout.Button("Save Graph", saveStyle, GUILayout.Width(ToolbarSaveBtnW))) SaveGraph();
            GUILayout.Space(ToolbarEdgePadding);
        }

        private static void DrawToolbarSep()
        {
            GUILayout.Space(4);
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(1, 20, GUILayout.Width(1)), BorderNormal);
            GUILayout.Space(4);
        }

        // ── 캔버스 ────────────────────────────────────────────────────────

        private void DrawCanvas(Rect canvasRect)
        {
            EnsureCanvasStyles();

            EditorGUI.DrawRect(canvasRect, BgCanvas);
            GUI.BeginClip(canvasRect);

            DrawGrid(new Rect(0, 0, canvasRect.width, canvasRect.height));

            DrawAllConnections();
            if (_isDraggingConnection) DrawConnectionPreview();
            foreach (var node in _graph.nodes)
                DrawNode(node);

            if (_isMarqueeSelecting) DrawMarqueeRect();

            HandleCanvasInput();
            GUI.EndClip();
        }

        // ── 캔버스 ↔ 화면 좌표 ────────────────────────────────────────────
        // 노드 위치·크기·히트 판정은 전부 캔버스 좌표로 다루고, 그리는 순간에만 화면 좌표로 옮긴다.
        // 화면 좌표 = (캔버스 좌표 - _scroll) * _zoom

        private Vector2 ToScreen(Vector2 canvasPos) => (canvasPos - _scroll) * _zoom;

        private Rect ToScreen(Rect canvasRect) => new(
            (canvasRect.x - _scroll.x) * _zoom, (canvasRect.y - _scroll.y) * _zoom,
            canvasRect.width * _zoom,           canvasRect.height * _zoom);

        /// <summary>현재 마우스 위치를 캔버스 좌표로 옮긴 값.</summary>
        private Vector2 MouseCanvasPos => ScreenToCanvas(Event.current.mousePosition);

        private void FillC(Rect canvasRect, Color color) => EditorGUI.DrawRect(ToScreen(canvasRect), color);

        private void OutlineC(Rect canvasRect, Color color) => DrawOutline(ToScreen(canvasRect), color, 1f);

        private void LabelC(Rect canvasRect, string text, GUIStyle style) =>
            GUI.Label(ToScreen(canvasRect), text, style);

        private void DrawGrid(Rect area)
        {
            Handles.BeginGUI();
            Handles.color = GridColor;
            float step = GridSize * _zoom;
            float offX = -(_scroll.x % GridSize) * _zoom;
            float offY = -(_scroll.y % GridSize) * _zoom;
            for (float x = offX; x < area.width;  x += step)
                Handles.DrawLine(new Vector3(x, 0), new Vector3(x, area.height));
            for (float y = offY; y < area.height; y += step)
                Handles.DrawLine(new Vector3(0, y), new Vector3(area.width, y));
            Handles.color = Color.white;
            Handles.EndGUI();
        }

        private void DrawMarqueeRect()
        {
            var r = GetMarqueeCanvasRect();
            FillC(r, MarqueeColor);
            OutlineC(r, MarqueeBorder);
        }

        private Vector2 ScreenToCanvas(Vector2 clipPos) => clipPos / _zoom + _scroll;

        private Rect GetMarqueeCanvasRect()
        {
            float x = Mathf.Min(_marqueeStart.x, _marqueeEnd.x);
            float y = Mathf.Min(_marqueeStart.y, _marqueeEnd.y);
            float w = Mathf.Abs(_marqueeEnd.x - _marqueeStart.x);
            float h = Mathf.Abs(_marqueeEnd.y - _marqueeStart.y);
            return new Rect(x, y, w, h);
        }

        // ── 노드 그리기 ───────────────────────────────────────────────────

        private void DrawNode(DialogueNodeSO node)
        {
            Vector2 pos = GetNodeDrawPos(node);
            float   h   = GetCachedHeight(node);
            var     rect    = new Rect(pos.x, pos.y, NodeWidth, h);
            bool    sel     = _selectedNodeIds.Contains(node.nodeId) || node.nodeId == _selectedNodeId;
            bool    isStart = node.nodeId == _graph.startNodeId;
            var     col     = NodeColor(node.nodeType);

            FillC(new Rect(rect.x + 3, rect.y + 4, rect.width, rect.height), new Color(0, 0, 0, 0.4f));
            FillC(rect, BgNode);
            OutlineC(rect, sel ? BorderSelect : BorderNormal);
            FillC(new Rect(rect.x, rect.y, 3, rect.height), col);

            var headerRect = new Rect(rect.x, rect.y, rect.width, NodeHeaderH);
            FillC(headerRect, new Color(col.r, col.g, col.b, 0.15f));
            FillC(new Rect(rect.x, rect.y + NodeHeaderH - 1, rect.width, 1), BorderNormal);

            if (isStart)
            {
                _styleNodeStartTag.normal.textColor = new Color(0.34f, 0.83f, 0.93f);
                LabelC(new Rect(rect.x, rect.y, rect.width - 8, NodeHeaderH), "▶ START", _styleNodeStartTag);
            }

            _styleNodeTitle.normal.textColor = col;
            LabelC(new Rect(rect.x + 10, rect.y, rect.width - 60, NodeHeaderH),
                $"[{node.nodeType.ToString().ToUpper()}]  {node.speakerId}", _styleNodeTitle);

            // 채널이 Main이 아닐 때 채널 태그 표시
            if (node.channel != DialogueChannel.Main)
            {
                var chCol  = ChannelColor(node.channel);
                string tag = node.channel == DialogueChannel.System ? "SYS" : "MLG";
                var tagRect = new Rect(rect.xMax - 38, rect.y + 5, 30, 14);
                FillC(tagRect, new Color(chCol.r, chCol.g, chCol.b, 0.2f));
                OutlineC(tagRect, new Color(chCol.r, chCol.g, chCol.b, 0.5f));
                _styleNodeChannelTag.normal.textColor = chCol;
                LabelC(tagRect, tag, _styleNodeChannelTag);
            }

            string shortId = node.nodeId.Length > 6 ? node.nodeId[..6] : node.nodeId;
            _styleNodeIdTag.normal.textColor = TextMuted;
            LabelC(new Rect(rect.x, rect.y, rect.width - 6, NodeHeaderH), shortId, _styleNodeIdTag);

            float bodyY = rect.y + NodeHeaderH + NodeBodyPadding;
            DrawNodeBody(node, rect, ref bodyY);

            DrawAndHandlePorts(node, rect);
            HandleNodeDragSelect(node, rect);
        }

        private Vector2 GetNodeDrawPos(DialogueNodeSO node)
        {
            if (_isMultiDragging && _multiDragOrigins.TryGetValue(node.nodeId, out Vector2 origin))
                return origin + (_multiDragCurrentMouse - _multiDragStartMouse);
            if (_draggingNodeId == node.nodeId)
                return _draggingNodePos;
            return node.editorPosition;
        }

        // ── 노드 본문 ─────────────────────────────────────────────────────

        private void DrawNodeBody(DialogueNodeSO node, Rect nodeRect, ref float y)
        {
            float x = nodeRect.x + NodeBodyInset;
            float w = nodeRect.width - NodeBodyInset * 2f;

            switch (node.nodeType)
            {
                case NodeType.Talk:
                    if (!string.IsNullOrEmpty(node.dialogueText))
                    {
                        var      c     = NodeColor(node.nodeType);
                        string[] lines = GetWrappedNodeText(node.dialogueText);
                        float    lineH = NodeTextLineHeight;
                        FillC(new Rect(x, y, 2, lines.Length * lineH), new Color(c.r, c.g, c.b, 0.5f));
                        _styleNodeText.normal.textColor = TextSecond;
                        for (int i = 0; i < lines.Length; i++)
                        {
                            LabelC(new Rect(x + NodeTextIndent, y + i * lineH, NodeTextWidth, lineH),
                                lines[i], _styleNodeText);
                        }
                    }
                    break;
                case NodeType.Choice:    DrawChoiceList(node, x, w, ref y);   break;
                case NodeType.Condition: DrawConditionBox(node, x, w, ref y); break;
                case NodeType.Event:     DrawEventTags(node, x, w, ref y);    break;
            }
        }

        private void DrawChoiceList(DialogueNodeSO node, float x, float w, ref float y)
        {
            if (node.choices == null || node.choices.Count == 0) return;
            var green = new Color(0.24f, 0.86f, 0.52f);
            foreach (var choice in node.choices)
            {
                var row = new Rect(x, y, w, 20);
                FillC(row, TagBg);
                OutlineC(row, BorderNormal);
                var arrowRect = new Rect(x + 4, y + 4, 12, 12);
                FillC(arrowRect, new Color(green.r, green.g, green.b, 0.2f));
                _styleChoiceArrow.normal.textColor = green;
                LabelC(arrowRect, "→", _styleChoiceArrow);
                _styleChoiceText.normal.textColor = TextSecond;
                LabelC(new Rect(x + 20, y + 2, w - 22, 16), choice.choiceText, _styleChoiceText);
                y += 22;
            }
        }

        private void DrawConditionBox(DialogueNodeSO node, float x, float w, ref float y)
        {
            string name  = node.condition != null ? node.condition.name : "— no condition —";
            var    amber = new Color(0.96f, 0.65f, 0.14f);
            var    boxR  = new Rect(x, y, w, 22);
            FillC(boxR, new Color(amber.r, amber.g, amber.b, 0.1f));
            OutlineC(boxR, new Color(amber.r, amber.g, amber.b, 0.3f));
            _styleConditionBox.normal.textColor = amber;
            LabelC(boxR, name, _styleConditionBox);
            y += 26;
            DrawBadge(new Rect(x,              y, w * 0.45f, 18), "T →", new Color(0.24f, 0.86f, 0.52f));
            DrawBadge(new Rect(x + w * 0.55f, y, w * 0.45f, 18), "F →", new Color(1.00f, 0.42f, 0.42f));
        }

        private void DrawEventTags(DialogueNodeSO node, float x, float w, ref float y)
        {
            var purple = new Color(0.65f, 0.55f, 0.98f);
            if (node.eventActions == null || node.eventActions.Count == 0)
            {
                _styleEmptyHint.normal.textColor = TextMuted;
                LabelC(new Rect(x, y, w, 18), "— no actions —", _styleEmptyHint);
                return;
            }
            foreach (var action in node.eventActions)
            {
                if (action == null) continue;
                var tr = new Rect(x, y, w, 18);
                FillC(tr, new Color(purple.r, purple.g, purple.b, 0.12f));
                OutlineC(tr, new Color(purple.r, purple.g, purple.b, 0.3f));
                _styleEventTag.normal.textColor = purple;
                LabelC(new Rect(x + 4, y, w - 4, 18), action.name, _styleEventTag);
                y += 20;
            }
        }

        private void DrawBadge(Rect canvasRect, string text, Color col)
        {
            FillC(canvasRect, new Color(col.r, col.g, col.b, 0.15f));
            OutlineC(canvasRect, new Color(col.r, col.g, col.b, 0.4f));
            _styleBadge.normal.textColor = col;
            LabelC(canvasRect, text, _styleBadge);
        }

        // ── 노드 높이 캐시 ────────────────────────────────────────────────

        private float GetCachedHeight(DialogueNodeSO node)
        {
            if (_heightCache.TryGetValue(node.nodeId, out float cached)) return cached;
            float h = ComputeNodeHeight(node);
            _heightCache[node.nodeId] = h;
            return h;
        }

        public void InvalidateHeightCache(string nodeId = null)
        {
            if (nodeId == null) _heightCache.Clear();
            else _heightCache.Remove(nodeId);
        }

        private static float ComputeNodeHeight(DialogueNodeSO node)
        {
            float h = NodeHeaderH + NodeBodyPadding * 2;
            switch (node.nodeType)
            {
                case NodeType.Talk:
                    if (!string.IsNullOrEmpty(node.dialogueText))
                        h += GetWrappedNodeText(node.dialogueText).Length * NodeTextLineHeight;
                    break;
                case NodeType.Choice:
                    h += (node.choices?.Count ?? 0) * 22 + 4;
                    break;
                case NodeType.Condition:
                    h += 22 + 26 + 18 + 4;
                    break;
                case NodeType.Event:
                    int cnt = node.eventActions?.Count ?? 0;
                    h += cnt > 0 ? cnt * 20 : 18;
                    break;
            }
            return Mathf.Max(h, NodeHeaderH + 36);
        }

        private static readonly Dictionary<string, string[]> _wrappedTextCache = new();
        private static readonly GUIContent                   _measureContent   = new();

        private static float NodeTextLineHeight => _styleNodeTextBase.lineHeight;

        /// <summary>
        /// 노드 본문 대사를 <see cref="NodeTextWidth"/>(캔버스 좌표) 기준으로 줄바꿈한다.
        /// 줄 수가 곧 노드 높이이므로 줌과 무관한 <see cref="_styleNodeTextBase"/>로 재야
        /// 노드 크기와 히트 판정이 줌에 따라 흔들리지 않는다.
        /// </summary>
        private static string[] GetWrappedNodeText(string text)
        {
            if (_wrappedTextCache.TryGetValue(text, out string[] cached)) return cached;

            var lines = new List<string>();
            foreach (string paragraph in text.Split('\n'))
                WrapParagraph(paragraph, lines);
            if (lines.Count == 0) lines.Add(string.Empty);

            string[] result = lines.ToArray();
            _wrappedTextCache[text] = result;
            return result;
        }

        private static void WrapParagraph(string paragraph, List<string> lines)
        {
            var line = new System.Text.StringBuilder();

            void FlushLine()
            {
                lines.Add(line.ToString().TrimEnd());
                line.Clear();
            }

            foreach (string word in SplitWords(paragraph))
            {
                // 이 단어를 붙이면 넘치면 줄을 먼저 끊는다. 줄 끝 공백은 폭 계산에서 뺀다.
                if (line.Length > 0 && MeasureWidth((line.ToString() + word).TrimEnd()) > NodeTextWidth)
                    FlushLine();

                if (MeasureWidth(word.TrimEnd()) <= NodeTextWidth)
                {
                    line.Append(word);
                    continue;
                }

                // 단어 하나가 한 줄보다 길면(공백 없는 한글 구간 등) 글자 단위로 끊는다.
                foreach (char ch in word)
                {
                    if (line.Length > 0 && MeasureWidth(line.ToString() + ch) > NodeTextWidth)
                        FlushLine();
                    line.Append(ch);
                }
            }

            FlushLine();
        }

        /// <summary>공백을 앞 단어에 붙여서 문단을 단어 단위로 자른다.</summary>
        private static IEnumerable<string> SplitWords(string paragraph)
        {
            int start = 0;
            for (int i = 0; i < paragraph.Length; i++)
            {
                if (paragraph[i] != ' ') continue;
                // 연속 공백까지 한 덩어리로 포함시킨다.
                int end = i;
                while (end + 1 < paragraph.Length && paragraph[end + 1] == ' ') end++;
                yield return paragraph[start..(end + 1)];
                start = end + 1;
                i     = end;
            }
            if (start < paragraph.Length) yield return paragraph[start..];
        }

        /// <summary>기준(줌 1배) 스타일로 문자열이 차지하는 가로 폭. 패딩이 0이라 순수 글자 폭이 나온다.</summary>
        private static float MeasureWidth(string text)
        {
            _measureContent.text = text;
            return _styleNodeTextBase.CalcSize(_measureContent).x;
        }

        // ── 포트 ─────────────────────────────────────────────────────────

        private void DrawAndHandlePorts(DialogueNodeSO node, Rect nodeRect)
        {
            DrawPort(new Vector2(nodeRect.center.x, nodeRect.yMin),
                new Color(0.4f, 0.4f, 0.5f), "in_" + node.nodeId, isOut: false);

            if (node.nodeType == NodeType.End) return;

            switch (node.nodeType)
            {
                case NodeType.Condition:
                    DrawPort(new Vector2(nodeRect.x + nodeRect.width * 0.33f, nodeRect.yMax),
                        new Color(0.24f, 0.86f, 0.52f), "true_"  + node.nodeId, isOut: true);
                    DrawPort(new Vector2(nodeRect.x + nodeRect.width * 0.67f, nodeRect.yMax),
                        new Color(1.00f, 0.42f, 0.42f), "false_" + node.nodeId, isOut: true);
                    break;
                case NodeType.Choice:
                    int cnt = node.choices?.Count ?? 0;
                    if (cnt == 0) goto default;
                    for (int i = 0; i < cnt; i++)
                    {
                        float t = (i + 1f) / (cnt + 1f);
                        DrawPort(new Vector2(nodeRect.x + nodeRect.width * t, nodeRect.yMax),
                            NodeColor(NodeType.Choice), $"choice_{i}_" + node.nodeId, isOut: true);
                    }
                    break;
                default:
                    DrawPort(new Vector2(nodeRect.center.x, nodeRect.yMax),
                        NodeColor(node.nodeType), "out_" + node.nodeId, isOut: true);
                    break;
            }
        }

        private void DrawPort(Vector2 center, Color color, string portKey, bool isOut)
        {
            const float hitRadius = PortRadius + 4f;
            var drawR = new Rect(center.x - PortRadius, center.y - PortRadius, PortRadius * 2, PortRadius * 2);
            var hitR  = new Rect(center.x - hitRadius,  center.y - hitRadius,  hitRadius  * 2, hitRadius  * 2);

            FillC(new Rect(drawR.x - 1, drawR.y - 1, drawR.width + 2, drawR.height + 2),
                new Color(0, 0, 0, 0.5f));
            FillC(drawR, color);

            if (!isOut) return;

            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && hitR.Contains(MouseCanvasPos))
            {
                _isDraggingConnection = true;
                _connectionSourceId   = ExtractNodeId(portKey);
                _connectionSourcePort = ExtractPortType(portKey);
                _connectionDragPos    = center;
                e.Use();
            }
        }

        // ── 노드 드래그 / 선택 ────────────────────────────────────────────

        private void HandleNodeDragSelect(DialogueNodeSO node, Rect rect)
        {
            var e = Event.current;
            if (e.type == EventType.Used) return;

            if (e.type == EventType.MouseDrag && _isMultiDragging)
            {
                _multiDragCurrentMouse = ScreenToCanvas(e.mousePosition);
                Repaint();
                e.Use();
                return;
            }

            if (e.type == EventType.MouseDrag && _draggingNodeId == node.nodeId)
            {
                _draggingNodePos += e.delta / _zoom;
                Repaint();
                e.Use();
                return;
            }

            if (!rect.Contains(MouseCanvasPos)) return;

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                bool alreadySelected = _selectedNodeIds.Contains(node.nodeId);

                if (e.control || e.command)
                {
                    if (alreadySelected) _selectedNodeIds.Remove(node.nodeId);
                    else _selectedNodeIds.Add(node.nodeId);
                    _selectedNodeId = node.nodeId;
                }
                else if (alreadySelected && _selectedNodeIds.Count > 1)
                {
                    _selectedNodeId = node.nodeId;
                    BeginMultiDrag(ScreenToCanvas(e.mousePosition));
                }
                else
                {
                    _selectedNodeIds.Clear();
                    _selectedNodeIds.Add(node.nodeId);
                    _selectedNodeId  = node.nodeId;
                    _draggingNodeId  = node.nodeId;
                    _draggingNodePos = node.editorPosition;
                }

                GUI.FocusControl(null);
                e.Use();
            }
            else if (e.type == EventType.MouseDown && e.button == 1)
            {
                ShowNodeContextMenu(node);
                e.Use();
            }
        }

        private void BeginMultiDrag(Vector2 canvasMousePos)
        {
            _isMultiDragging       = true;
            _multiDragStartMouse   = canvasMousePos;
            _multiDragCurrentMouse = canvasMousePos;
            _multiDragOrigins.Clear();
            foreach (var id in _selectedNodeIds)
            {
                var n = _graph.nodes.Find(x => x.nodeId == id);
                if (n != null) _multiDragOrigins[id] = n.editorPosition;
            }
        }

        // ── 연결선 ────────────────────────────────────────────────────────

        private void DrawAllConnections()
        {
            foreach (var node in _graph.nodes)
            {
                Vector2 pos = GetNodeDrawPos(node);
                float   h   = GetCachedHeight(node);
                var     r   = new Rect(pos.x, pos.y, NodeWidth, h);

                switch (node.nodeType)
                {
                    case NodeType.Talk:
                    case NodeType.Event:
                        DrawBezier(new Vector2(r.center.x, r.yMax),
                            GetTargetInPos(node.nextNodeId), NodeColor(node.nodeType));
                        break;
                    case NodeType.Condition:
                        DrawBezier(new Vector2(r.x + r.width * 0.33f, r.yMax),
                            GetTargetInPos(node.trueNextNodeId),  new Color(0.24f, 0.86f, 0.52f));
                        DrawBezier(new Vector2(r.x + r.width * 0.67f, r.yMax),
                            GetTargetInPos(node.falseNextNodeId), new Color(1.00f, 0.42f, 0.42f));
                        break;
                    case NodeType.Choice:
                        int cnt = node.choices?.Count ?? 0;
                        for (int i = 0; i < cnt; i++)
                        {
                            float t = (i + 1f) / (cnt + 1f);
                            DrawBezier(new Vector2(r.x + r.width * t, r.yMax),
                                GetTargetInPos(node.choices[i].nextNodeId), NodeColor(NodeType.Choice));
                        }
                        break;
                }
            }
        }

        private void DrawConnectionPreview()
        {
            Vector2 from = ToScreen(_connectionDragPos);
            Vector2 to   = Event.current.mousePosition;
            float   dy   = Mathf.Abs(to.y - from.y) * 0.5f + BezierTangent * _zoom;
            Handles.BeginGUI();
            Handles.DrawBezier(from, to, from + Vector2.down * dy, to - Vector2.down * dy,
                new Color(1f, 1f, 1f, 0.6f), null, 2f);
            Handles.EndGUI();
        }

        private const float BezierTangent = 30f;
        private const float ArrowLength   = 7f;

        /// <summary>캔버스 좌표로 받은 두 점을 화면 좌표로 옮겨 연결선을 그린다.</summary>
        private void DrawBezier(Vector2? fromCanvas, Vector2? toCanvas, Color color)
        {
            if (fromCanvas == null || toCanvas == null) return;
            Vector3 f  = ToScreen(fromCanvas.Value);
            Vector3 t2 = ToScreen(toCanvas.Value);
            float   dy = Mathf.Abs(t2.y - f.y) * 0.5f + BezierTangent * _zoom;

            Handles.BeginGUI();
            Handles.DrawBezier(f, t2, f + Vector3.down * dy, t2 - Vector3.down * dy,
                new Color(color.r, color.g, color.b, 0.7f), null, 2f);
            Vector3 dir   = (t2 - f).normalized;
            float   arrow = ArrowLength * _zoom;
            Handles.color = new Color(color.r, color.g, color.b, 0.8f);
            Handles.DrawLine(t2, t2 - arrow * (Quaternion.Euler(0, 0,  30) * dir));
            Handles.DrawLine(t2, t2 - arrow * (Quaternion.Euler(0, 0, -30) * dir));
            Handles.color = Color.white;
            Handles.EndGUI();
        }

        private Vector2? GetTargetInPos(string targetId)
        {
            if (string.IsNullOrEmpty(targetId)) return null;
            var n = _graph.nodes.Find(x => x.nodeId == targetId);
            if (n == null) return null;
            Vector2 pos = GetNodeDrawPos(n);
            return new Vector2(pos.x + NodeWidth * 0.5f, pos.y);
        }

        // ── 캔버스 입력 ───────────────────────────────────────────────────

        private void HandleCanvasInput()
        {
            var e = Event.current;

            if (e.type == EventType.MouseDrag && (e.button == 2 || (e.button == 0 && e.alt)))
            {
                _scroll -= e.delta / _zoom;
                Repaint();
                e.Use();
                return;
            }

            if (e.type == EventType.ScrollWheel)
            {
                SetZoom(_zoom - e.delta.y * 0.05f, e.mousePosition);
                e.Use();
                return;
            }

            if (_isDraggingConnection && e.type == EventType.MouseUp && e.button == 0)
            {
                TryFinishConnection(ScreenToCanvas(e.mousePosition));
                _isDraggingConnection = false;
                Repaint();
                e.Use();
                return;
            }

            if (_isDraggingConnection) { Repaint(); return; }

            if (e.type == EventType.MouseUp && e.button == 0 && _isMultiDragging)
            {
                CommitMultiDrag();
                e.Use();
                return;
            }

            if (e.type == EventType.MouseUp && e.button == 0 && _draggingNodeId != null)
            {
                CommitNodeDrag();
                e.Use();
                return;
            }

            if (e.type == EventType.MouseUp) _draggingNodeId = null;

            if (e.type == EventType.MouseDrag && e.button == 0 && _isMarqueeSelecting)
            {
                _marqueeEnd = ScreenToCanvas(e.mousePosition);
                UpdateMarqueeSelection();
                Repaint();
                e.Use();
                return;
            }

            if (e.type == EventType.MouseUp && e.button == 0 && _isMarqueeSelecting)
            {
                _isMarqueeSelecting = false;
                Repaint();
                e.Use();
                return;
            }

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                _selectedNodeId = null;
                _selectedNodeIds.Clear();
                _isMarqueeSelecting = true;
                _marqueeStart       = ScreenToCanvas(e.mousePosition);
                _marqueeEnd         = _marqueeStart;
                e.Use();
                return;
            }

            if (e.type == EventType.ContextClick)
            {
                ShowCanvasContextMenu(SnapToGrid(ScreenToCanvas(e.mousePosition)));
                e.Use();
            }
        }

        private void UpdateMarqueeSelection()
        {
            var marquee = GetMarqueeCanvasRect();
            _selectedNodeIds.Clear();
            foreach (var node in _graph.nodes)
            {
                float h        = GetCachedHeight(node);
                var   nodeRect = new Rect(node.editorPosition.x, node.editorPosition.y, NodeWidth, h);
                if (marquee.Overlaps(nodeRect))
                    _selectedNodeIds.Add(node.nodeId);
            }
            _selectedNodeId = _selectedNodeIds.Count > 0
                ? _graph.nodes.Find(n => _selectedNodeIds.Contains(n.nodeId))?.nodeId
                : null;
        }

        private void CommitNodeDrag()
        {
            var node = _graph.nodes.Find(n => n.nodeId == _draggingNodeId);
            if (node != null)
            {
                Undo.RecordObject(node, "Move Node");
                node.editorPosition = SnapToGrid(_draggingNodePos);
                EditorUtility.SetDirty(node);
            }
            _draggingNodeId = null;
            Repaint();
        }

        private void CommitMultiDrag()
        {
            Vector2 delta = _multiDragCurrentMouse - _multiDragStartMouse;
            if (delta.sqrMagnitude > 0.01f)
            {
                Undo.IncrementCurrentGroup();
                foreach (var (id, origin) in _multiDragOrigins)
                {
                    var node = _graph.nodes.Find(n => n.nodeId == id);
                    if (node == null) continue;
                    Undo.RecordObject(node, "Move Nodes");
                    node.editorPosition = SnapToGrid(origin + delta);
                    EditorUtility.SetDirty(node);
                }
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            }
            _isMultiDragging = false;
            _multiDragOrigins.Clear();
            Repaint();
        }

        private void TryFinishConnection(Vector2 canvasPos)
        {
            const float snapDist = 40f;
            DialogueNodeSO best  = null;
            float bestD          = float.MaxValue;

            foreach (var node in _graph.nodes)
            {
                if (node.nodeId == _connectionSourceId) continue;
                var   inPos   = new Vector2(node.editorPosition.x + NodeWidth * 0.5f, node.editorPosition.y);
                float d       = Vector2.Distance(canvasPos, inPos);
                var   hdrRect = new Rect(node.editorPosition.x, node.editorPosition.y,
                                         NodeWidth, NodeHeaderH + PortRadius * 2);
                if ((d < snapDist || hdrRect.Contains(canvasPos)) && d < bestD)
                {
                    bestD = d;
                    best  = node;
                }
            }

            if (best != null)
                ApplyConnection(_connectionSourceId, _connectionSourcePort, best.nodeId);
        }

        private void ApplyConnection(string fromId, string fromPort, string toId)
        {
            var fromNode = _graph.nodes.Find(n => n.nodeId == fromId);
            if (fromNode == null) return;

            Undo.RecordObject(fromNode, "Connect Node");
            var so = new SerializedObject(fromNode);
            so.Update();

            switch (fromPort)
            {
                case "out":   so.FindProperty("nextNodeId").stringValue      = toId; break;
                case "true":  so.FindProperty("trueNextNodeId").stringValue  = toId; break;
                case "false": so.FindProperty("falseNextNodeId").stringValue = toId; break;
                default:
                    if (fromPort.StartsWith("choice_") &&
                        int.TryParse(fromPort.Split('_')[1], out int idx))
                    {
                        var choices = so.FindProperty("choices");
                        if (idx < choices.arraySize)
                            choices.GetArrayElementAtIndex(idx)
                                   .FindPropertyRelative("nextNodeId").stringValue = toId;
                    }
                    break;
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(fromNode);
            _graph.InvalidateCache();
            Repaint();
        }

        // ── Inspector ─────────────────────────────────────────────────────

        private void DrawInspector(Rect rect)
        {
            EditorGUI.DrawRect(rect, BgInspector);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), BorderNormal);

            var hdr = new Rect(rect.x, rect.y, rect.width, InspectorHeaderHeight);
            EditorGUI.DrawRect(hdr, new Color(0.06f, 0.07f, 0.08f));
            EditorGUI.DrawRect(new Rect(rect.x, hdr.yMax - 1, rect.width, 1), BorderNormal);
            _styleInspectorHeader.normal.textColor = TextSecond;

            string headerText = _selectedNodeIds.Count > 1
                ? $"INSPECTOR  [{_selectedNodeIds.Count} selected]"
                : "INSPECTOR";
            GUI.Label(new Rect(rect.x + 12, rect.y, rect.width - 12, InspectorHeaderHeight),
                headerText, _styleInspectorHeader);

            // 좌우 여백을 준 본문 영역. 여백이 없으면 프로퍼티 필드가 패널 경계 밖으로 잘린다.
            float contentW = rect.width - InspectorPadding * 2f;
            GUILayout.BeginArea(new Rect(rect.x + InspectorPadding, rect.y + InspectorHeaderHeight,
                contentW, rect.height - InspectorHeaderHeight));
            _inspectorScroll = GUILayout.BeginScrollView(_inspectorScroll, false, false);

            // labelWidth 기본값은 창 전체 폭 기준이라 좁은 패널에서 값 필드를 밀어낸다.
            float prevLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = contentW * InspectorLabelRatio;

            if (_selectedNodeId == null)
            {
                DrawGraphInspector();
                GUILayout.Space(16);
                _styleInspectorHint.normal.textColor = TextMuted;
                GUILayout.Label("노드를 선택하면\n속성이 표시됩니다", _styleInspectorHint);
            }
            else
            {
                var node = _graph.nodes.Find(n => n.nodeId == _selectedNodeId);
                if (node != null) DrawNodeInspector(node);
            }

            EditorGUIUtility.labelWidth = prevLabelWidth;
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        /// <summary>
        /// 노드 선택이 없을 때 그래프 단위 설정을 편집한다.
        /// 무언 참여자는 어떤 노드에도 등장하지 않아 노드 인스펙터에는 둘 자리가 없다.
        /// </summary>
        private void DrawGraphInspector()
        {
            var so = new SerializedObject(_graph);
            so.Update();

            GUILayout.Space(8);
            EditorGUILayout.LabelField("그래프 설정", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                so.FindProperty("silentParticipantSpeakerIds"),
                new GUIContent("무언 참여자", "대사는 없지만 이 대화 동안 함께 멈춰 설 인물의 화자 ID"),
                true);

            so.ApplyModifiedProperties();
        }

        private void DrawNodeInspector(DialogueNodeSO node)
        {
            var so = new SerializedObject(node);
            so.Update();

            GUILayout.Space(10);

            // ── 노드 타입 배지 ───────────────────────────────────────────
            var col       = NodeColor(node.nodeType);
            var badgeRect = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(badgeRect, new Color(col.r, col.g, col.b, 0.1f));
            DrawOutline(badgeRect, new Color(col.r, col.g, col.b, 0.3f), 1);
            _styleInspectorBadge.normal.textColor = col;
            GUI.Label(badgeRect, node.nodeType.ToString().ToUpper(), _styleInspectorBadge);

            // ── 채널 배지 ────────────────────────────────────────────────
            GUILayout.Space(2);
            var channelColor = ChannelColor(node.channel);
            var channelRect  = GUILayoutUtility.GetRect(0, 18, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(channelRect, new Color(channelColor.r, channelColor.g, channelColor.b, 0.08f));
            DrawOutline(channelRect, new Color(channelColor.r, channelColor.g, channelColor.b, 0.25f), 1);
            _styleInspectorChannel.normal.textColor = channelColor;
            GUI.Label(channelRect, $"CH: {node.channel.ToString().ToUpper()}", _styleInspectorChannel);

            GUILayout.Space(8);
            _styleInspectorMini.normal.textColor = TextMuted;
            GUILayout.Label("NODE ID", _styleInspectorMini);
            GUILayout.Label(node.nodeId, _styleInspectorMini);

            GUILayout.Space(6);
            InspectorDivider();

            EditorGUI.BeginChangeCheck();

            // ── 기본 속성 ────────────────────────────────────────────────
            EditorGUILayout.PropertyField(so.FindProperty("nodeType"));
            GUILayout.Space(2);
            EditorGUILayout.PropertyField(so.FindProperty("channel"));

            GUILayout.Space(4);
            InspectorDivider();

            // ── Talk / Choice 속성 ──────────────────────────────────────
            if (node.nodeType == NodeType.Talk || node.nodeType == NodeType.Choice)
            {
                InspectorSectionLabel("TALK / CHOICE", col);
                EditorGUILayout.PropertyField(so.FindProperty("speakerId"));
                EditorGUILayout.PropertyField(so.FindProperty("dialogueText"));
                // 기본 초상화는 SpeakerPortraitTable이 소유한다. 여기는 이 대사만 다르게 보일 때 쓰는 오버라이드다.
                EditorGUILayout.PropertyField(so.FindProperty("portrait"),
                    new GUIContent("Portrait (Override)",
                        "비워두면 SpeakerPortraitTable에 등록된 화자 기본 초상화를 사용합니다."));
                EditorGUILayout.PropertyField(so.FindProperty("typingSpeed"));

                // autoAdvanceDuration — Main 이외 채널에서만 의미있지만 모든 Talk에서 편집 허용
                EditorGUILayout.PropertyField(so.FindProperty("autoAdvanceDuration"),
                    new GUIContent("Auto Advance (sec)", "0 = 입력 대기 / 0 초과 = N초 후 자동 진행"));

                GUILayout.Space(4);
                InspectorDivider();
            }

            // ── Routing ──────────────────────────────────────────────────
            InspectorSectionLabel("ROUTING", TextSecond);
            switch (node.nodeType)
            {
                case NodeType.Talk:
                case NodeType.Event:
                    EditorGUILayout.PropertyField(so.FindProperty("nextNodeId"));
                    break;
                case NodeType.Condition:
                    EditorGUILayout.PropertyField(so.FindProperty("condition"));
                    EditorGUILayout.PropertyField(so.FindProperty("trueNextNodeId"));
                    EditorGUILayout.PropertyField(so.FindProperty("falseNextNodeId"));
                    break;
                case NodeType.Choice:
                    EditorGUILayout.PropertyField(so.FindProperty("choices"), true);
                    break;
            }

            GUILayout.Space(4);
            InspectorDivider();

            // ── Camera ───────────────────────────────────────────────────
            if (node.nodeType == NodeType.Talk || node.nodeType == NodeType.Choice)
            {
                InspectorSectionLabel("CAMERA", new Color(0.45f, 0.8f, 0.95f));
                EditorGUILayout.PropertyField(so.FindProperty("shotType"),
                    new GUIContent("Shot", "Auto = 자동 디렉터(화자 OTS / 화자 전환 시 리버스 샷)"));
                EditorGUILayout.PropertyField(so.FindProperty("shotTransition"),
                    new GUIContent("Transition", "Auto = 대상 변경 Cut / 동일 대상 Blend / 진입 Establish"));
                EditorGUILayout.PropertyField(so.FindProperty("listenerSpeakerId"),
                    new GUIContent("Listener Speaker", "비우면 자동(플레이어 또는 마지막 비플레이어 화자). 채우면 이 인물과의 가상선으로 구도를 잡는다"));
                EditorGUILayout.PropertyField(so.FindProperty("reactionSpeakerId"),
                    new GUIContent("Reaction Speaker", "비우지 않으면 이 인물의 반응을 잡는 리액션 샷"));
                EditorGUILayout.PropertyField(so.FindProperty("shotDistanceOverride"),
                    new GUIContent("Distance Override", "0 = 프리셋 거리 사용"));
                EditorGUILayout.PropertyField(so.FindProperty("cameraRecording"),
                    new GUIContent("Recording", "지정 시 자동 구도 대신 사전 녹화 카메라를 화자 기준으로 재생"));

                GUILayout.Space(4);
                InspectorDivider();
            }

            // ── Events ───────────────────────────────────────────────────
            InspectorSectionLabel("EVENTS", new Color(0.65f, 0.55f, 0.98f));
            EditorGUILayout.PropertyField(so.FindProperty("eventActions"), true);

            if (EditorGUI.EndChangeCheck())
            {
                so.ApplyModifiedProperties();
                InvalidateHeightCache(node.nodeId);
            }
            else
            {
                so.ApplyModifiedProperties();
            }
        }

        private static Color ChannelColor(DialogueChannel ch) => ch switch
        {
            DialogueChannel.Main      => new Color(0.31f, 0.62f, 1.00f),
            DialogueChannel.System    => new Color(0.96f, 0.65f, 0.14f),
            DialogueChannel.Monologue => new Color(0.65f, 0.55f, 0.98f),
            _                         => Color.white,
        };

        private void InspectorSectionLabel(string label, Color color)
        {
            _styleInspectorMini.normal.textColor = color;
            GUILayout.Label(label, _styleInspectorMini);
            GUILayout.Space(2);
        }

        private static void InspectorDivider()
        {
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true)), BorderNormal);
            GUILayout.Space(4);
        }

        // ── 컨텍스트 메뉴 ─────────────────────────────────────────────────

        private void ShowNodeContextMenu(DialogueNodeSO node)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Set as Start Node"), false, () =>
            {
                Undo.RecordObject(_graph, "Set Start Node");
                _graph.startNodeId = node.nodeId;
                EditorUtility.SetDirty(_graph);
            });
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Duplicate"), false, () => DuplicateNode(node));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delete Node"), false, () => DeleteNode(node));
            menu.ShowAsContext();
        }

        private void ShowCanvasContextMenu(Vector2 canvasPos)
        {
            var menu = new GenericMenu();
            foreach (NodeType t in Enum.GetValues(typeof(NodeType)))
            {
                var ct = t;
                var cp = canvasPos;
                menu.AddItem(new GUIContent($"Add {t} Node"), false, () => AddNodeAt(ct, cp));
            }
            menu.ShowAsContext();
        }

        // ── 노드 CRUD ─────────────────────────────────────────────────────

        private void AddNode(NodeType type) =>
            AddNodeAt(type, SnapToGrid(ScreenToCanvas(NewNodeScreenOffset)));

        /// <summary>툴바 ADD로 만든 노드가 놓일 화면상 위치. 캔버스 좌표로 환산해 줌과 무관하게 보이는 곳에 둔다.</summary>
        private static readonly Vector2 NewNodeScreenOffset = new(200, 150);

        private void AddNodeAt(NodeType type, Vector2 pos)
        {
            Undo.IncrementCurrentGroup();

            var node = CreateInstance<DialogueNodeSO>();
            node.name           = $"Node_{type}";
            node.nodeType       = type;
            node.editorPosition = pos;
            node.AssignNewId();

            Undo.RegisterCreatedObjectUndo(node, "Add Node");
            AssetDatabase.AddObjectToAsset(node, _graph);

            Undo.RecordObject(_graph, "Add Node");
            _graph.nodes.Add(node);
            if (string.IsNullOrEmpty(_graph.startNodeId)) _graph.startNodeId = node.nodeId;
            _graph.InvalidateCache();

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            _selectedNodeId = node.nodeId;
            _selectedNodeIds.Clear();
            _selectedNodeIds.Add(node.nodeId);
            EditorUtility.SetDirty(_graph);
            AssetDatabase.SaveAssets();
            Repaint();
        }

        private void DuplicateNode(DialogueNodeSO source)
        {
            var node = Instantiate(source);
            node.name           = source.name + "_copy";
            node.editorPosition = source.editorPosition + new Vector2(30, 30);
            node.AssignNewId();
            AssetDatabase.AddObjectToAsset(node, _graph);
            _graph.nodes.Add(node);
            _graph.InvalidateCache();
            _selectedNodeId = node.nodeId;
            _selectedNodeIds.Clear();
            _selectedNodeIds.Add(node.nodeId);
            EditorUtility.SetDirty(_graph);
            AssetDatabase.SaveAssets();
            Repaint();
        }

        private void DeleteNode(DialogueNodeSO node)
        {
            Undo.IncrementCurrentGroup();

            foreach (var n in _graph.nodes)
            {
                if (n == node) continue;
                ClearReferencesTo(n, node.nodeId);
            }

            Undo.RecordObject(_graph, "Delete Node");
            _graph.nodes.Remove(node);
            _graph.InvalidateCache();
            _heightCache.Remove(node.nodeId);
            Undo.DestroyObjectImmediate(node);

            if (_selectedNodeId == node.nodeId)
            {
                _selectedNodeId = null;
                _selectedNodeIds.Remove(node.nodeId);
            }
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            EditorUtility.SetDirty(_graph);
            AssetDatabase.SaveAssets();
            Repaint();
        }

        private static void ClearReferencesTo(DialogueNodeSO node, string targetId)
        {
            var so = new SerializedObject(node);
            so.Update();
            ClearPropIfMatch(so.FindProperty("nextNodeId"),      targetId);
            ClearPropIfMatch(so.FindProperty("trueNextNodeId"),  targetId);
            ClearPropIfMatch(so.FindProperty("falseNextNodeId"), targetId);
            var choices = so.FindProperty("choices");
            for (int i = 0; i < choices.arraySize; i++)
                ClearPropIfMatch(choices.GetArrayElementAtIndex(i).FindPropertyRelative("nextNodeId"), targetId);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ClearPropIfMatch(SerializedProperty prop, string id)
        {
            if (prop != null && prop.stringValue == id) prop.stringValue = string.Empty;
        }

        // ── 뷰 유틸리티 ───────────────────────────────────────────────────

        private void AutoLayout()
        {
            if (_graph.nodes.Count == 0) return;
            int cols = Mathf.CeilToInt(Mathf.Sqrt(_graph.nodes.Count));
            for (int i = 0; i < _graph.nodes.Count; i++)
            {
                _graph.nodes[i].editorPosition =
                    new Vector2((i % cols) * (NodeWidth + 40) + 60, (i / cols) * 200 + 60);
                EditorUtility.SetDirty(_graph.nodes[i]);
            }
            Repaint();
        }

        private void FitView()
        {
            if (_graph == null || _graph.nodes.Count == 0) return;
            float minX = float.MaxValue, minY = float.MaxValue,
                  maxX = float.MinValue, maxY = float.MinValue;
            foreach (var n in _graph.nodes)
            {
                float h = GetCachedHeight(n);
                minX = Mathf.Min(minX, n.editorPosition.x);
                minY = Mathf.Min(minY, n.editorPosition.y);
                maxX = Mathf.Max(maxX, n.editorPosition.x + NodeWidth);
                maxY = Mathf.Max(maxY, n.editorPosition.y + h);
            }
            Vector2 canvasSize = CanvasSize;
            // 화면 좌표 = _zoom * (캔버스 좌표 - _scroll) 이므로, 중앙 정렬 오프셋도 _zoom으로 나눠야 한다.
            _zoom   = Mathf.Clamp(Mathf.Min(1f, Mathf.Min(canvasSize.x / (maxX - minX + FitViewMargin),
                                                          canvasSize.y / (maxY - minY + FitViewMargin)) * 0.85f),
                                  MinZoom, MaxZoom);
            _scroll = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f) - canvasSize * 0.5f / _zoom;
            Repaint();
        }

        /// <summary>캔버스 영역 크기(클립 좌표 기준).</summary>
        private Vector2 CanvasSize =>
            new(position.width - InspectorWidth, position.height - ToolbarHeight);

        private void ChangeZoom(float delta) => SetZoom(_zoom + delta, CanvasSize * 0.5f);

        /// <summary>
        /// 캔버스 클립 좌표 <paramref name="anchor"/>가 가리키는 지점을 고정한 채 줌을 바꾼다.
        /// 고정하지 않으면 캔버스 원점(좌상단) 기준으로 확대돼 보던 위치를 잃는다.
        /// </summary>
        private void SetZoom(float newZoom, Vector2 anchor)
        {
            newZoom = Mathf.Clamp(newZoom, MinZoom, MaxZoom);
            if (Mathf.Approximately(newZoom, _zoom)) return;

            _scroll += anchor * (1f / _zoom - 1f / newZoom);
            _zoom    = newZoom;
            Repaint();
        }

        private void SaveGraph()
        {
            if (_graph == null) return;
            EditorUtility.SetDirty(_graph);
            foreach (var node in _graph.nodes)
                EditorUtility.SetDirty(node);
            AssetDatabase.SaveAssets();
        }

        private void HandleShortcuts()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown || _graph == null) return;

            if (e.keyCode is KeyCode.Delete or KeyCode.Backspace)
            {
                if (_selectedNodeIds.Count > 1)
                {
                    var toDelete = new List<string>(_selectedNodeIds);
                    foreach (var id in toDelete)
                    {
                        var n = _graph.nodes.Find(x => x.nodeId == id);
                        if (n != null) DeleteNode(n);
                    }
                }
                else if (_selectedNodeId != null)
                {
                    var n = _graph.nodes.Find(x => x.nodeId == _selectedNodeId);
                    if (n != null) DeleteNode(n);
                }
                e.Use();
            }

            if ((e.control || e.command) && e.keyCode == KeyCode.A)
            {
                _selectedNodeIds.Clear();
                foreach (var n in _graph.nodes) _selectedNodeIds.Add(n.nodeId);
                _selectedNodeId = _graph.nodes.Count > 0 ? _graph.nodes[0].nodeId : null;
                Repaint();
                e.Use();
            }
        }

        // ── 그리기 헬퍼 ───────────────────────────────────────────────────

        private static void DrawOutline(Rect r, Color col, float t)
        {
            EditorGUI.DrawRect(new Rect(r.x,        r.y,        r.width, t),  col);
            EditorGUI.DrawRect(new Rect(r.x,        r.yMax - t, r.width, t),  col);
            EditorGUI.DrawRect(new Rect(r.x,        r.y,        t, r.height), col);
            EditorGUI.DrawRect(new Rect(r.xMax - t, r.y,        t, r.height), col);
        }

        private static GUIStyle MakeToolbarNodeBtn(Color col) =>
            new(EditorStyles.miniButton)
                { fontSize = 9, fontStyle = FontStyle.Bold, normal = { textColor = col } };

        // ── 포트 키 파싱 ──────────────────────────────────────────────────

        private static string ExtractPortType(string key)
        {
            if (key.StartsWith("choice_"))
            {
                int s = key.IndexOf('_', "choice_".Length);
                return s >= 0 ? key[..s] : key;
            }
            int f = key.IndexOf('_');
            return f >= 0 ? key[..f] : key;
        }

        private static string ExtractNodeId(string key)
        {
            if (key.StartsWith("choice_"))
            {
                int s = key.IndexOf('_', "choice_".Length);
                return s >= 0 ? key[(s + 1)..] : string.Empty;
            }
            int f = key.IndexOf('_');
            return f >= 0 ? key[(f + 1)..] : string.Empty;
        }

        private static Vector2 SnapToGrid(Vector2 pos) =>
            new(Mathf.Round(pos.x / GridSize) * GridSize,
                Mathf.Round(pos.y / GridSize) * GridSize);
    }
}
