using System;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// 특정 MotionEvent 타입의 offset/rotation 필드를 어떤 좌표 공간으로 그릴지
    /// 프로젝트 쪽에서 타입 안전하게 선언하는 확장점.
    ///
    /// 이 계약이 없으면 MotionSet.Editor가 프로젝트 이벤트 타입을 참조할 수 없어
    /// 타입 이름 문자열 비교로 판별해야 하고, 리네임 시 컴파일 오류 없이 조용히 깨진다.
    /// 구현체는 public 무인자 생성자를 가져야 하며 <see cref="MotionEditorExtensionRegistry"/>가
    /// 자동으로 수집한다.
    /// </summary>
    public interface IMotionEventOffsetFieldProvider
    {
        /// <summary>이 provider가 담당하는 이벤트 타입. 하위 타입도 매칭된다.</summary>
        Type EventType { get; }

        /// <summary>해당 필드를 로컬 위치 오프셋 위젯으로 그려야 하는지.</summary>
        bool IsLocalOffset(object motionEvent, string fieldName);

        /// <summary>해당 필드를 회전 오프셋(Euler) 위젯으로 그려야 하는지.</summary>
        bool IsRotationOffset(object motionEvent, string fieldName);

        /// <summary>위치 오프셋 위젯에 표시할 좌표 공간 라벨.</summary>
        string GetLocalOffsetSpaceLabel(object motionEvent);

        /// <summary>회전 오프셋 위젯에 표시할 좌표 공간 라벨.</summary>
        string GetRotationOffsetSpaceLabel(object motionEvent);
    }
}
