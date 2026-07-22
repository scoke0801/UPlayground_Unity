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

    /// <summary>노드가 스스로 선언하는 실행(흐름) 포트 정의. 데이터 포트는 1차 범위에서 지원하지 않는다.</summary>
    public readonly struct FlowPortDef
    {
        public FlowPortDef(string name, FlowPortDirection direction)
        {
            Name = name;
            Direction = direction;
        }

        public string Name { get; }
        public FlowPortDirection Direction { get; }

        public static FlowPortDef Input(string name = FlowPort.In) => new(name, FlowPortDirection.Input);
        public static FlowPortDef Output(string name = FlowPort.Out) => new(name, FlowPortDirection.Output);
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

        [Tooltip("노드 타이틀로 표시할 사용자 라벨. 비우면 타입 기본 이름(DisplayName)을 쓴다.")]
        public string editorLabel;

        [Tooltip("노드 위 말풍선으로 표시되는 저작 메모 (Blueprint comment bubble). 실행에는 영향 없음.")]
        [TextArea] public string editorComment;

        /// <summary>노드가 소유한 실행 포트 목록.</summary>
        public abstract IEnumerable<FlowPortDef> Ports { get; }

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
}
