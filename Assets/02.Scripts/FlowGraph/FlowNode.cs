using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.FlowGraph
{
    public enum FlowPortDirection
    {
        Input,
        Output,
    }

    /// <summary>포트가 실행 펄스인지 타입이 있는 데이터인지 구분한다.</summary>
    public enum FlowPortKind
    {
        Execution,
        Data,
    }

    /// <summary>한 포트가 허용하는 연결 수. 기존 실행 포트는 호환성을 위해 Multi가 기본이다.</summary>
    public enum FlowPortCapacity
    {
        Single,
        Multi,
    }

    /// <summary>
    /// 노드가 스스로 선언하는 포트 스키마.
    /// Id는 에셋에 저장되는 안정 식별자이고 DisplayName은 에디터 표시용이므로 서로 분리한다.
    /// </summary>
    public readonly struct FlowPortDef
    {
        public FlowPortDef(
            string id,
            FlowPortDirection direction,
            string displayName = null,
            FlowPortKind kind = FlowPortKind.Execution,
            FlowPortCapacity capacity = FlowPortCapacity.Multi,
            Type valueType = null,
            bool optional = false)
        {
            Id = id;
            Direction = direction;
            DisplayName = string.IsNullOrEmpty(displayName) ? id : displayName;
            Kind = kind;
            Capacity = capacity;
            ValueType = kind == FlowPortKind.Data ? valueType ?? typeof(object) : null;
            Optional = optional;
        }

        /// <summary>연결 데이터에 저장되는 안정 식별자.</summary>
        public string Id { get; }

        public string DisplayName { get; }
        public FlowPortDirection Direction { get; }
        public FlowPortKind Kind { get; }
        public FlowPortCapacity Capacity { get; }
        public Type ValueType { get; }

        /// <summary>
        /// 비워 두는 것이 정상인 출력인지 여부. 조건 분기의 반대편이나 진단용 종단처럼
        /// 저작 의도상 끊길 수 있는 포트에만 지정한다. 검증기는 이 포트의 미연결을 경고하지 않는다.
        /// </summary>
        public bool Optional { get; }

        public static FlowPortDef Input(
            string id = FlowPort.In,
            FlowPortCapacity capacity = FlowPortCapacity.Multi,
            string displayName = null) =>
            new(id, FlowPortDirection.Input, displayName, FlowPortKind.Execution, capacity);

        public static FlowPortDef Output(
            string id = FlowPort.Out,
            FlowPortCapacity capacity = FlowPortCapacity.Multi,
            string displayName = null,
            bool optional = false) =>
            new(id, FlowPortDirection.Output, displayName, FlowPortKind.Execution, capacity,
                valueType: null, optional: optional);

        public static FlowPortDef DataInput<T>(
            string id,
            FlowPortCapacity capacity = FlowPortCapacity.Single,
            string displayName = null) =>
            new(id, FlowPortDirection.Input, displayName, FlowPortKind.Data, capacity, typeof(T));

        public static FlowPortDef DataOutput<T>(
            string id,
            FlowPortCapacity capacity = FlowPortCapacity.Multi,
            string displayName = null) =>
            new(id, FlowPortDirection.Output, displayName, FlowPortKind.Data, capacity, typeof(T));

        public static bool AreCompatible(FlowPortDef first, FlowPortDef second)
        {
            if (first.Direction == second.Direction || first.Kind != second.Kind)
                return false;

            FlowPortDef output = first.Direction == FlowPortDirection.Output ? first : second;
            FlowPortDef input = first.Direction == FlowPortDirection.Input ? first : second;
            if (output.Kind == FlowPortKind.Execution)
                return true;

            return input.ValueType != null
                   && output.ValueType != null
                   && input.ValueType.IsAssignableFrom(output.ValueType);
        }
    }

    /// <summary>공용 포트 이름 상수.</summary>
    public static class FlowPort
    {
        public const string In = "In";
        public const string Out = "Out";
        public const string True = "True";
        public const string False = "False";
    }

    /// <summary>
    /// 노드 검색창에 노출할 메뉴 경로. 미지정 시 "기타/{타입명}"으로 노출된다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class FlowNodeMenuAttribute : Attribute
    {
        public FlowNodeMenuAttribute(string path)
        {
            Path = path;
        }

        public string Path { get; }

        /// <summary>노드 검색 결과와 툴팁에 표시할 한 문장 설명.</summary>
        public string Summary { get; set; }

        /// <summary>표시명과 다른 용어로도 찾을 수 있게 하는 검색 별칭.</summary>
        public string[] Keywords { get; set; }
    }

    /// <summary>
    /// 노드 타입의 에디터 표시 스타일. 다른 asmdef의 커스텀 노드도 이 어트리뷰트만으로
    /// 아이콘/헤더 컬러를 제어할 수 있다 (해석은 에디터 FlowNodeCatalog가 담당).
    /// Inherited=true — 베이스 노드에 붙이면 파생 노드 전체에 적용된다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class FlowNodeStyleAttribute : Attribute
    {
        /// <summary>에디터 아이콘. Unity 빌트인 아이콘 이름(예: "PlayButton") 또는 프로젝트 텍스처 경로(Assets/...).</summary>
        public string Icon { get; set; }

        /// <summary>헤더 색 HTML 표기 (예: "#2E6B3A"). 미지정 시 카테고리 팔레트를 따른다.</summary>
        public string HeaderColor { get; set; }
    }

    /// <summary>
    /// 카테고리 단위 스타일을 어셈블리 레벨에서 등록한다. 외부 asmdef가 자기 카테고리의
    /// 색/기본 아이콘을 선언하는 용도 (내장 팔레트보다 우선).
    /// 사용: [assembly: FlowNodeCategoryStyle("내카테고리", "#7A3B5E", Icon = "Favorite")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class FlowNodeCategoryStyleAttribute : Attribute
    {
        public FlowNodeCategoryStyleAttribute(string category, string headerColor)
        {
            Category = category;
            HeaderColor = headerColor;
        }

        public string Category { get; }
        public string HeaderColor { get; }
        public string Icon { get; set; }
    }

    /// <summary>
    /// FlowGraph의 다형 노드 베이스. 단일 에셋 내 [SerializeReference] 리스트로 직렬화된다.
    /// 노드 인스턴스는 그래프 에셋(SO)에 속하므로 필드에 런타임 가변 상태를 두지 않는다 —
    /// 실행 상태는 FlowContext/FlowGraphRunner가 소유한다.
    /// 클래스를 다른 어셈블리로 이동할 때는 [MovedFrom(true, sourceAssembly:...)]를 반드시 유지할 것.
    /// </summary>
    [Serializable]
    public abstract class FlowNode
    {
        [HideInInspector] public string id = Guid.NewGuid().ToString("N");
        [HideInInspector] public Vector2 editorPosition;

        /// <summary>에디터 전용 브레이크포인트 — 토큰 도착 시 에디터를 일시정지한다 (FlowCanvas 참조).</summary>
        [HideInInspector] public bool breakpoint;

        /// <summary>설정을 지우지 않고 브레이크포인트를 일시 비활성화한다.</summary>
        [HideInInspector] public bool breakpointDisabled;

        /// <summary>0이면 매번, 양수면 해당 실행 횟수 이상에서 중단한다.</summary>
        [HideInInspector] public int breakpointAfterHits;

        /// <summary>비어 있지 않으면 Blackboard 값이 expected와 일치할 때만 중단한다.</summary>
        [HideInInspector] public string breakpointVariable;

        [HideInInspector] public FlowVariableValue breakpointExpected;

        [Tooltip("노드 타이틀로 표시할 사용자 라벨. 비우면 타입 기본 이름(DisplayName)을 쓴다.")]
        public string editorLabel;

        [Tooltip("노드 위 말풍선으로 표시되는 저작 메모 (Blueprint comment bubble). 실행에는 영향 없음.")]
        [TextArea] public string editorComment;

        /// <summary>노드가 소유한 실행 포트 목록.</summary>
        public abstract IEnumerable<FlowPortDef> Ports { get; }

        public bool TryGetPort(
            string portId,
            FlowPortDirection direction,
            out FlowPortDef result)
        {
            foreach (FlowPortDef port in Ports)
            {
                if (port.Direction == direction
                    && string.Equals(port.Id, portId, StringComparison.Ordinal))
                {
                    result = port;
                    return true;
                }
            }

            result = default;
            return false;
        }

        /// <summary>에디터 노드 타이틀. 기본은 타입명에서 "Node" 접미사 제거.</summary>
        public virtual string DisplayName
        {
            get
            {
                string name = GetType().Name;
                return name.EndsWith("Node", StringComparison.Ordinal)
                    ? name.Substring(0, name.Length - 4)
                    : name;
            }
        }

        /// <summary>
        /// 토큰이 도착했을 때 실행. 완료 시 token.Emit(포트명)으로 다음 노드에 토큰을 전달한다.
        /// 동기 노드는 Emit 후 즉시 yield break, 대기 노드는 yield로 보류한다.
        /// </summary>
        public abstract IEnumerator Execute(FlowToken token);
    }

    /// <summary>
    /// 실행 토큰 없이 요청 시점에 값을 계산하는 순수 데이터 노드.
    /// 결과 캐시는 두지 않는다. Blackboard나 Context가 바뀌면 다음 소비자가 최신 값을 다시 평가한다.
    /// </summary>
    [Serializable]
    public abstract class FlowDataNode : FlowNode
    {
        public sealed override IEnumerator Execute(FlowToken token)
        {
            yield break;
        }

        public abstract bool TryEvaluate(
            FlowContext context,
            FlowGraphSO graph,
            string outputPortId,
            out object value);
    }
}
