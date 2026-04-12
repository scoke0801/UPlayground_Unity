using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Dialogue;

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
    private const float GridSize        = 20f;
    private const float PortRadius      = 6f;
    private const float InspectorWidth  = 270f;

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

    // ── 노드 높이 캐시 ────────────────────────────────────────────────
    private readonly Dictionary<string, float> _heightCache = new();

    // ── GUIStyle 캐시 ─────────────────────────────────────────────────
    private static GUIStyle _styleMiniLabel;
    private static GUIStyle _styleMiniLabelWrap;
    private static GUIStyle _styleMiniLabelCenter;
    private static GUIStyle _styleBoldLabel;
    private static GUIStyle _styleMiniLabelRight;

    private static void EnsureStyles()
    {
        if (_styleMiniLabel != null) return;
        _styleMiniLabel       = new GUIStyle(EditorStyles.miniLabel);
        _styleMiniLabelWrap   = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true, fontSize = 10 };
        _styleMiniLabelCenter = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
        _styleBoldLabel       = new GUIStyle(EditorStyles.boldLabel) { fontSize = 10, alignment = TextAnchor.MiddleLeft };
        _styleMiniLabelRight  = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };
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

    [MenuItem("UPlayGround/Story/Dialogue Graph Editor")]
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
            _styleMiniLabelCenter.normal.textColor = TextMuted;
            GUI.Label(new Rect(0, 30, position.width, position.height - 30),
                "DialogueGraphSO를 선택하거나 더블클릭해서 열어주세요.", _styleMiniLabelCenter);
            return;
        }

        float canvasW    = position.width - InspectorWidth;
        var   canvasRect = new Rect(0, 28, canvasW, position.height - 28);

        DrawCanvas(canvasRect);
        DrawInspector(new Rect(canvasW, 28, InspectorWidth, position.height - 28));
        HandleShortcuts();
    }

    // ── 툴바 ─────────────────────────────────────────────────────────

    private void DrawToolbar()
    {
        EditorGUI.DrawRect(new Rect(0, 0, position.width, 28), new Color(0.08f, 0.09f, 0.10f));
        EditorGUI.DrawRect(new Rect(0, 27, position.width, 1), BorderNormal);

        EditorGUILayout.BeginHorizontal(GUILayout.Height(28));
        GUILayout.Space(6);

        var prev = _graph;
        _graph = (DialogueGraphSO)EditorGUILayout.ObjectField(
            _graph, typeof(DialogueGraphSO), false, GUILayout.Width(200));
        if (_graph != prev) { _selectedNodeId = null; _selectedNodeIds.Clear(); _heightCache.Clear(); }

        DrawToolbarSep();

        _styleMiniLabel.normal.textColor = TextMuted;
        GUILayout.Label("ADD", _styleMiniLabel, GUILayout.Width(28));

        foreach (NodeType t in Enum.GetValues(typeof(NodeType)))
        {
            if (GUILayout.Button(t.ToString().ToUpper(), MakeToolbarNodeBtn(NodeColor(t)),
                    GUILayout.Width(62), GUILayout.Height(20)))
                AddNode(t);
        }

        DrawToolbarSep();

        if (GUILayout.Button("Auto Layout", EditorStyles.toolbarButton, GUILayout.Width(80))) AutoLayout();
        if (GUILayout.Button("Fit View",    EditorStyles.toolbarButton, GUILayout.Width(60))) FitView();

        DrawToolbarSep();

        if (GUILayout.Button("-", EditorStyles.toolbarButton, GUILayout.Width(20))) ChangeZoom(-0.15f);
        _styleMiniLabelCenter.normal.textColor = TextMuted;
        GUILayout.Label($"{Mathf.RoundToInt(_zoom * 100)}%", _styleMiniLabelCenter, GUILayout.Width(38));
        if (GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.Width(20))) ChangeZoom(0.15f);

        GUILayout.FlexibleSpace();

        // ── JSON IO ──────────────────────────────────────────────────
        DrawToolbarSep();

        var importStyle = new GUIStyle(EditorStyles.toolbarButton)
            { normal = { textColor = new Color(0.34f, 0.83f, 0.93f) } };
        var exportStyle = new GUIStyle(EditorStyles.toolbarButton)
            { normal = { textColor = new Color(0.24f, 0.86f, 0.52f) } };

        if (GUILayout.Button("JSON Import", importStyle, GUILayout.Width(90)))
            DialogueJsonIO.ImportFromJson(_graph);

        GUI.enabled = _graph != null;
        if (GUILayout.Button("JSON Export", exportStyle, GUILayout.Width(90)))
            DialogueJsonIO.ExportToJson(_graph);
        GUI.enabled = true;

        DrawToolbarSep();

        var saveStyle = new GUIStyle(EditorStyles.toolbarButton)
            { normal = { textColor = new Color(0.31f, 0.62f, 1f) } };
        if (GUILayout.Button("Save Graph", saveStyle, GUILayout.Width(80))) SaveGraph();
        GUILayout.Space(6);

        EditorGUILayout.EndHorizontal();
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
        EditorGUI.DrawRect(canvasRect, BgCanvas);
        GUI.BeginClip(canvasRect);

        DrawGrid(new Rect(0, 0, canvasRect.width, canvasRect.height));

        var oldMatrix = GUI.matrix;
        var pivot     = new Vector2(canvasRect.width * 0.5f, canvasRect.height * 0.5f);
        GUIUtility.ScaleAroundPivot(Vector2.one * _zoom, pivot);
        GUI.matrix = GUI.matrix * Matrix4x4.TRS(
            new Vector3(-_scroll.x + pivot.x * (1 - 1f / _zoom),
                        -_scroll.y + pivot.y * (1 - 1f / _zoom), 0),
            Quaternion.identity, Vector3.one);

        DrawAllConnections();
        if (_isDraggingConnection) DrawConnectionPreview();
        foreach (var node in _graph.nodes)
            DrawNode(node);

        if (_isMarqueeSelecting) DrawMarqueeRect();

        GUI.matrix = oldMatrix;

        HandleCanvasInput();
        GUI.EndClip();
    }

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
        EditorGUI.DrawRect(r, MarqueeColor);
        DrawOutline(r, MarqueeBorder, 1f / _zoom);
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

        EditorGUI.DrawRect(new Rect(rect.x + 3, rect.y + 4, rect.width, rect.height), new Color(0, 0, 0, 0.4f));
        EditorGUI.DrawRect(rect, BgNode);
        DrawOutline(rect, sel ? BorderSelect : BorderNormal, 1);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3, rect.height), col);

        var headerRect = new Rect(rect.x, rect.y, rect.width, NodeHeaderH);
        EditorGUI.DrawRect(headerRect, new Color(col.r, col.g, col.b, 0.15f));
        EditorGUI.DrawRect(new Rect(rect.x, rect.y + NodeHeaderH - 1, rect.width, 1), BorderNormal);

        if (isStart)
        {
            _styleMiniLabel.fontSize  = 8;
            _styleMiniLabel.alignment = TextAnchor.MiddleRight;
            _styleMiniLabel.normal.textColor = new Color(0.34f, 0.83f, 0.93f);
            GUI.Label(new Rect(rect.x, rect.y, rect.width - 8, NodeHeaderH), "▶ START", _styleMiniLabel);
        }

        _styleBoldLabel.normal.textColor = col;
        GUI.Label(new Rect(rect.x + 10, rect.y, rect.width - 60, NodeHeaderH),
            $"[{node.nodeType.ToString().ToUpper()}]  {node.speakerId}", _styleBoldLabel);

        // 채널이 Main이 아닐 때 채널 태그 표시
        if (node.channel != DialogueChannel.Main)
        {
            var chCol  = ChannelColor(node.channel);
            string tag = node.channel == DialogueChannel.System ? "SYS" : "MLG";
            var tagRect = new Rect(rect.xMax - 38, rect.y + 5, 30, 14);
            EditorGUI.DrawRect(tagRect, new Color(chCol.r, chCol.g, chCol.b, 0.2f));
            DrawOutline(tagRect, new Color(chCol.r, chCol.g, chCol.b, 0.5f), 1);
            _styleMiniLabelCenter.fontSize         = 8;
            _styleMiniLabelCenter.normal.textColor = chCol;
            GUI.Label(tagRect, tag, _styleMiniLabelCenter);
        }

        string shortId = node.nodeId.Length > 6 ? node.nodeId[..6] : node.nodeId;
        _styleMiniLabelRight.normal.textColor = TextMuted;
        GUI.Label(new Rect(rect.x, rect.y, rect.width - 6, NodeHeaderH), shortId, _styleMiniLabelRight);

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
        float x = nodeRect.x + 10;
        float w = nodeRect.width - 20;

        switch (node.nodeType)
        {
            case NodeType.Talk:
                if (!string.IsNullOrEmpty(node.dialogueText))
                {
                    var   c    = NodeColor(node.nodeType);
                    float txtH = GetCachedTextHeight(node.dialogueText, w - 16);
                    EditorGUI.DrawRect(new Rect(x, y, 2, txtH), new Color(c.r, c.g, c.b, 0.5f));
                    _styleMiniLabelWrap.normal.textColor = TextSecond;
                    GUI.Label(new Rect(x + 6, y, w - 6, txtH), node.dialogueText, _styleMiniLabelWrap);
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
            EditorGUI.DrawRect(row, TagBg);
            DrawOutline(row, BorderNormal, 1);
            var arrowRect = new Rect(x + 4, y + 4, 12, 12);
            EditorGUI.DrawRect(arrowRect, new Color(green.r, green.g, green.b, 0.2f));
            _styleMiniLabelCenter.fontSize         = 8;
            _styleMiniLabelCenter.normal.textColor = green;
            GUI.Label(arrowRect, "→", _styleMiniLabelCenter);
            _styleMiniLabel.fontSize              = 10;
            _styleMiniLabel.alignment             = TextAnchor.UpperLeft;
            _styleMiniLabel.clipping              = TextClipping.Clip;
            _styleMiniLabel.normal.textColor      = TextSecond;
            GUI.Label(new Rect(x + 20, y + 2, w - 22, 16), choice.choiceText, _styleMiniLabel);
            y += 22;
        }
    }

    private void DrawConditionBox(DialogueNodeSO node, float x, float w, ref float y)
    {
        string name  = node.condition != null ? node.condition.name : "— no condition —";
        var    amber = new Color(0.96f, 0.65f, 0.14f);
        var    boxR  = new Rect(x, y, w, 22);
        EditorGUI.DrawRect(boxR, new Color(amber.r, amber.g, amber.b, 0.1f));
        DrawOutline(boxR, new Color(amber.r, amber.g, amber.b, 0.3f), 1);
        _styleMiniLabelCenter.fontSize         = 10;
        _styleMiniLabelCenter.normal.textColor = amber;
        GUI.Label(boxR, name, _styleMiniLabelCenter);
        y += 26;
        DrawBadge(new Rect(x,              y, w * 0.45f, 18), "T →", new Color(0.24f, 0.86f, 0.52f));
        DrawBadge(new Rect(x + w * 0.55f, y, w * 0.45f, 18), "F →", new Color(1.00f, 0.42f, 0.42f));
    }

    private void DrawEventTags(DialogueNodeSO node, float x, float w, ref float y)
    {
        var purple = new Color(0.65f, 0.55f, 0.98f);
        if (node.eventActions == null || node.eventActions.Count == 0)
        {
            _styleMiniLabel.fontSize         = 10;
            _styleMiniLabel.alignment        = TextAnchor.UpperLeft;
            _styleMiniLabel.normal.textColor = TextMuted;
            GUI.Label(new Rect(x, y, w, 18), "— no actions —", _styleMiniLabel);
            return;
        }
        foreach (var action in node.eventActions)
        {
            if (action == null) continue;
            var tr = new Rect(x, y, w, 18);
            EditorGUI.DrawRect(tr, new Color(purple.r, purple.g, purple.b, 0.12f));
            DrawOutline(tr, new Color(purple.r, purple.g, purple.b, 0.3f), 1);
            _styleMiniLabel.fontSize         = 9;
            _styleMiniLabel.alignment        = TextAnchor.UpperLeft;
            _styleMiniLabel.normal.textColor = purple;
            GUI.Label(new Rect(x + 4, y, w - 4, 18), action.name, _styleMiniLabel);
            y += 20;
        }
    }

    private void DrawBadge(Rect rect, string text, Color col)
    {
        EditorGUI.DrawRect(rect, new Color(col.r, col.g, col.b, 0.15f));
        DrawOutline(rect, new Color(col.r, col.g, col.b, 0.4f), 1);
        _styleMiniLabelCenter.fontSize         = 9;
        _styleMiniLabelCenter.normal.textColor = col;
        GUI.Label(rect, text, _styleMiniLabelCenter);
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
                    h += GetCachedTextHeight(node.dialogueText, NodeWidth - 30);
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

    private static readonly Dictionary<string, float> _textHeightCache = new();

    private static float GetCachedTextHeight(string text, float width)
    {
        string key = $"{text.GetHashCode()}_{(int)width}";
        if (_textHeightCache.TryGetValue(key, out float h)) return h;
        var style = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true, fontSize = 10 };
        h = style.CalcHeight(new GUIContent(text), width);
        _textHeightCache[key] = h;
        return h;
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

        EditorGUI.DrawRect(new Rect(drawR.x - 1, drawR.y - 1, drawR.width + 2, drawR.height + 2),
            new Color(0, 0, 0, 0.5f));
        EditorGUI.DrawRect(drawR, color);

        if (!isOut) return;

        var e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0 && hitR.Contains(e.mousePosition))
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

        if (!rect.Contains(e.mousePosition)) return;

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
        var   from = new Vector3(_connectionDragPos.x, _connectionDragPos.y);
        var   to   = new Vector3(Event.current.mousePosition.x, Event.current.mousePosition.y);
        float dy   = Mathf.Abs(to.y - from.y) * 0.5f + 30f;
        Handles.BeginGUI();
        Handles.DrawBezier(from, to, from + Vector3.down * dy, to - Vector3.down * dy,
            new Color(1f, 1f, 1f, 0.6f), null, 2f);
        Handles.EndGUI();
    }

    private static void DrawBezier(Vector2? from, Vector2? to, Color color)
    {
        if (from == null || to == null) return;
        Vector3 f  = new(from.Value.x, from.Value.y, 0);
        Vector3 t2 = new(to.Value.x,   to.Value.y,   0);
        float   dy = Mathf.Abs(t2.y - f.y) * 0.5f + 30f;

        Handles.BeginGUI();
        Handles.DrawBezier(f, t2, f + Vector3.down * dy, t2 - Vector3.down * dy,
            new Color(color.r, color.g, color.b, 0.7f), null, 2f);
        Vector3 dir = (t2 - f).normalized;
        Handles.color = new Color(color.r, color.g, color.b, 0.8f);
        Handles.DrawLine(t2, t2 - 7f * (Quaternion.Euler(0, 0,  30) * dir));
        Handles.DrawLine(t2, t2 - 7f * (Quaternion.Euler(0, 0, -30) * dir));
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
            ChangeZoom(-e.delta.y * 0.05f);
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

        var hdr = new Rect(rect.x, rect.y, rect.width, 28);
        EditorGUI.DrawRect(hdr, new Color(0.06f, 0.07f, 0.08f));
        EditorGUI.DrawRect(new Rect(rect.x, hdr.yMax - 1, rect.width, 1), BorderNormal);
        _styleBoldLabel.fontSize         = 10;
        _styleBoldLabel.normal.textColor = TextSecond;

        string headerText = _selectedNodeIds.Count > 1
            ? $"INSPECTOR  [{_selectedNodeIds.Count} selected]"
            : "INSPECTOR";
        GUI.Label(new Rect(rect.x + 12, rect.y, rect.width, 28), headerText, _styleBoldLabel);

        GUILayout.BeginArea(new Rect(rect.x, rect.y + 28, rect.width, rect.height - 28));
        _inspectorScroll = GUILayout.BeginScrollView(_inspectorScroll, false, false);

        if (_selectedNodeId == null)
        {
            GUILayout.Space(40);
            _styleMiniLabelCenter.normal.textColor = TextMuted;
            GUILayout.Label("노드를 선택하면\n속성이 표시됩니다", _styleMiniLabelCenter);
        }
        else
        {
            var node = _graph.nodes.Find(n => n.nodeId == _selectedNodeId);
            if (node != null) DrawNodeInspector(node);
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
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
        _styleMiniLabelCenter.fontSize         = 10;
        _styleMiniLabelCenter.fontStyle        = FontStyle.Bold;
        _styleMiniLabelCenter.normal.textColor = col;
        GUI.Label(badgeRect, node.nodeType.ToString().ToUpper(), _styleMiniLabelCenter);

        // ── 채널 배지 ────────────────────────────────────────────────
        GUILayout.Space(2);
        var channelColor = ChannelColor(node.channel);
        var channelRect  = GUILayoutUtility.GetRect(0, 18, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(channelRect, new Color(channelColor.r, channelColor.g, channelColor.b, 0.08f));
        DrawOutline(channelRect, new Color(channelColor.r, channelColor.g, channelColor.b, 0.25f), 1);
        _styleMiniLabelCenter.fontSize         = 9;
        _styleMiniLabelCenter.fontStyle        = FontStyle.Normal;
        _styleMiniLabelCenter.normal.textColor = channelColor;
        GUI.Label(channelRect, $"CH: {node.channel.ToString().ToUpper()}", _styleMiniLabelCenter);

        GUILayout.Space(8);
        _styleMiniLabel.fontSize         = 9;
        _styleMiniLabel.alignment        = TextAnchor.UpperLeft;
        _styleMiniLabel.normal.textColor = TextMuted;
        GUILayout.Label("NODE ID", _styleMiniLabel);
        GUILayout.Label(node.nodeId, _styleMiniLabel);

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
            EditorGUILayout.PropertyField(so.FindProperty("portrait"));
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
        _styleMiniLabel.fontSize         = 9;
        _styleMiniLabel.alignment        = TextAnchor.UpperLeft;
        _styleMiniLabel.normal.textColor = color;
        GUILayout.Label(label, _styleMiniLabel);
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
        AddNodeAt(type, SnapToGrid(_scroll + new Vector2(200, 150)));

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
        float cw = position.width - InspectorWidth;
        float ch = position.height - 28;
        _scroll = new Vector2((minX + maxX) * 0.5f - cw * 0.5f, (minY + maxY) * 0.5f - ch * 0.5f);
        _zoom   = Mathf.Min(1f, Mathf.Min(cw / (maxX - minX + 80), ch / (maxY - minY + 80)) * 0.85f);
        Repaint();
    }

    private void ChangeZoom(float delta)
    {
        _zoom = Mathf.Clamp(_zoom + delta, 0.2f, 2f);
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
