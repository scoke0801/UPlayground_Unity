# ADR 0001 — v1 범위와 패치 백엔드

- 상태: 채택 (2026-11-12)
- 설계 정본: [DESIGN_SPEC.md](../../../전달용/Unity_HotReload_Agent_Handoff/DESIGN_SPEC.md) · 상태: [unity-hot-reload.md](../../unity-hot-reload.md)

## 결정

1. **백엔드:** 외부 Worker(Roslyn 4.3.0 + Mono.Cecil 0.11.5)가 변경 어셈블리 전체를 컴파일하고 IL을 의미 기반 IR로 추출한다. Editor는 IR 피연산자를 로드된 원본(A0) `Type`/`MemberInfo`로 해석하고 Harmony 2.3.6 transpiler로 기존 메서드 본문 전체를 교체한다. 새 게임 어셈블리를 `Assembly.Load`하지 않는다.
2. **컴파일 컨텍스트:** Unity 6000.3 Bee가 실제 csc에 넘긴 응답 파일(`Library/Bee/artifacts/*EDbg.dag/<asm>.rsp`, Release면 `*E.dag`)을 Roslyn `CSharpCommandLineParser`로 그대로 해석한다. 참조·define·옵션·Unity 내장 source generator·additional file이 rsp에서 오므로 Worker 호스트 BCL이 섞이지 않는다. 소스 목록은 `CompilationPipeline.GetAssemblies`와 대조하고 다르면 `ContextChanged`. 어댑터 ID `Unity6000.3-BeeRsp`는 버전 전용이다.
3. **source generator:** Unity 설치 경로(`EditorApplication.applicationContentsPath`) 아래 generator만 실행한다. 그 밖의 analyzer/generator가 rsp에 있으면 `UnsupportedGeneratedCode`로 거부한다.
4. **기준선 parity:** W0(Worker 컴파일)와 로드된 A0(ILPP 후처리 포함) 전체를 표면·본문 지문으로 비교한다. 차이가 하나라도 있으면 세션 시작을 거부한다(`BaselineParityFailed`). MVID 일치는 요구하지 않는다(아래 근거).
5. **v1 대상:** allowlist + 사전 컴파일된 `[MethodImpl(NoInlining)]` + 메인 스레드 비제네릭 클래스 메서드. 설계 지원 매트릭스의 거부 항목은 축소 없이 유지한다.
6. **Worker 취소:** 요청별 취소 토큰 대신 Editor가 시간 초과 시 자신이 시작한 Worker 프로세스만 종료하고, revision/requestId가 다른 늦은 결과는 버린다. 다음 분석 때 같은 기준선 스냅샷으로 재시작하고 컨텍스트 해시를 재확인한다.

## 근거

- Unity 내장 Roslyn: `Editor/Data/DotNetSdkRoslyn/Microsoft.CodeAnalysis.CSharp.dll` 4.3.0.0 (파일 4.300.122.52613). Worker는 NuGet 4.3.0으로 고정.
- W0 MVID는 Unity 산출물과 다르다(Fixtures `a1afa0a0…` vs `f628ceb5…`). 같은 버전 번호라도 Roslyn 빌드가 달라 PDB의 컴파일러 메타데이터가 달라지는 것으로 추정한다(미검증). 대신 Camera·Actor(558파일)·fixture 전부 표면 차이 0, 본문 차이 0을 실측했다.
- 병합본 `Lib.Harmony` 2.3.6(0Harmony 2.2MB)은 Burst 엔트리포인트 해싱에서 `BadImageFormatException: Read out of bounds`를 낸다(Editor.log). 무시하지 않고 `Lib.Harmony.Thin` 2.3.6 + MonoMod 원본 DLL로 교체했다. 상세: [ADR 0002](0002-backend-proof.md).

## 대안과 기각 사유

- 변경 파일 하나만 컴파일: partial·같은 어셈블리 내부 멤버를 놓친다(설계 금지).
- Unity 공식 MethodReload: 별도 실행 모델·제약. 비교 실험은 하지 않았다.
- JIT 포인터 직접 덮어쓰기: 도입하지 않음.
