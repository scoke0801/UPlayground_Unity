# RiverBasin4km 평지 기복·B3 협곡 — 상태

[설계](../../Assets/docs/design/world-art/open-world/RIVER_BASIN_GORGE_RELIEF_DESIGN.md) · [월드와 아트 위키](../wiki/world-art.md) · [강 유역 제작 상태](river-basin-world.md)

## 현재 상태 (2026-11-22)

- **설계 작성 완료. 구현 0%.** 씬·지형·에셋은 바꾸지 않았다.
- **수행한 것:** 저장 RAW(`Tools/RiverBasinWorld/Rebuild/RiverBasin_4097_Joined_R16LE_SouthFirst.raw`)의 읽기 전용 정적 측정. 호수 마을 → B3 평지의 6m 이상 단절 0%, 협곡 회랑 림–하상 13~19m, 남서 지류 낙차 27m.
- **한계:** 측정 RAW는 B1 높이 패치·북부 폭포 Sculpt·호수 섬 Sculpt 이전 상태다. 수치는 시작값이며 G0에서 열린 Editor로 다시 잰다.
- **측정 스크립트:** [Tools/RiverBasinWorld/GorgeRelief/](../../Tools/RiverBasinWorld/GorgeRelief/)

## 남은 단계

G0 Editor 실측·확정 → G1 회색 블록아웃(사용자 TPS 확인 후 진행) → G2 물·현수교 → G3 암반·식생·길 → G4 평지 확장·NavMesh·미니맵. 단계 정의와 통과 조건은 설계 7장.

## 결정 대기

파일럿 S1(B3 협곡) 선정, B3를 현수교로 변경, 백색 화강암 재질 복제 3건. 권고안은 설계 10장.

## 사용자 테스트 대기·미수행

실제 플레이 확인 전부(첫인상·횡단·바닥 탐험·소리·평지 단차·성능). 설계 11장 표.
