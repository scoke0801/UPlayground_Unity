# 프로젝트 검증 도구

`Validate-Project.ps1`은 Unity 프로젝트의 컴파일과 자동 테스트를 같은 순서로 반복 실행하는 Windows용 진입점이다. 결과 XML과 Unity 로그는 Git 비추적 경로인 `Temp/ProjectValidation/`에 저장한다.

```powershell
# 빠른 회귀 검증: Core/Contracts/GAS/상태 머신 관련 빌드 + 핵심 EditMode 테스트
powershell -ExecutionPolicy Bypass -File Tools/Validate-Project.ps1

# 전체 EditMode 검증
powershell -ExecutionPolicy Bypass -File Tools/Validate-Project.ps1 -Profile EditMode

# 솔루션 컴파일 + 전체 EditMode + 전체 PlayMode 검증
powershell -ExecutionPolicy Bypass -File Tools/Validate-Project.ps1 -Profile Full
```

스크립트는 `ProjectSettings/ProjectVersion.txt`의 버전과 Unity Hub 기본 설치 경로를 사용한다. 다른 위치의 에디터는 `-UnityPath`로 지정할 수 있다. Unity가 같은 프로젝트를 열고 있으면 에셋 동시 접근을 막기 위해 실행을 중단한다.

`dotnet build`는 Unity 컴파일의 보조 검증이며 `.csproj`가 최신이어야 한다. 이미 별도로 컴파일했다면 `-SkipDotnetBuild`를 사용할 수 있다.
