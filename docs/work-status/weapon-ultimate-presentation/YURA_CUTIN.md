# 유라 컷인 제작 (2026-11-14)

## 산출물과 출처

- 사용자 요청: 기존 SkillCutIn 일러스트와 같은 스타일로 유라 컷인 제작.
- [최종 PNG](../../../Assets/04.Images/SkillCutIn/CutIn_Yura.png), [meta](../../../Assets/04.Images/SkillCutIn/CutIn_Yura.png.meta).
- 내장 ImageGen 사용. 외형 참조는 [기존 모델 전신 초상화](../../../Assets/04.Images/UI/Portrait/Player_Yura_Full.png), 스타일 참조는 [아린](../../../Assets/04.Images/SkillCutIn/CutIn_Arin.png)·[묘령](../../../Assets/04.Images/SkillCutIn/CutIn_MyoRyeong.png). 실제 모델의 새 뷰포트 촬영은 하지 않았다.
- 보라색 긴 머리·양쪽 머리 묶음·자홍색 눈·흰 프릴·분홍 스트랩을 유지하고 공격 직전 집중과 머리카락 동세를 표현했다.
- 생성 원본: `C:/Users/scoke/.codex/generated_images/01a1da29-607c-7d63-a683-d9c146bccb12/exec-6c6cce97-1b17-4182-849b-9a6ef4c925a8.png`. 원본과 최종 PNG는 보존한다. 다른 기존 아트·사용자 변경은 보존했다.
- 1536×1024, 32비트 RGBA, 2,624,405 bytes. SHA256: `88BE081A92C5B3D69A63941DEBD03F5BE6D782B2B5D36F88822BD947F19A69A6`.
- Unity Sprite 임포트 완료. GUID `8520972187e20b04dafb6384cdc7f70a`, Single, alphaIsTransparency=true, mipmap=false, Clamp, NPOT=None, 최대2048. 기존 캐릭터 정의·궁극기 실행 연결은 수정하지 않았다.

## 검증과 남은 확인

- AI가 외형 참조·스타일 참조·생성 이미지를 열어 비교했다. PNG 크기·알파 확인: 8픽셀 간격 24,576점 중 alpha0=5,449, partial=19,127, alpha255=0, 좌상단 alpha0. 알파 채널이 있다는 사실이 외곽 광무리 제거·실제 합성 품질 인수를 뜻하지 않는다.
- 열린 Unity Editor MCP에서 강제 임포트 후 Sprite 로드·1536×1024·원본 알파·mipmap 꺼짐 확인. 씬·캐릭터·궁극기 연결 변경 없음. 이미지·문서 작업이라 C# 테스트와 빌드는 실행하지 않았다.
- YU01 사용자 테스트 대기: 최종 PNG와 유라 모델을 비교해 얼굴·머리·의상 일치를 확인한다. 다른 인물로 보이거나 핵심 장식·색이 다르면 실패다.
- YU02 사용자 테스트 대기: 후속 패널 연결 시 얼굴/양눈 식별·머리 장식 잘림·외곽 광무리·배경 혼합을 실제 컷인 크기에서 확인한다. 현재는 실행 연결이 없어 실제 전투 검수는 미수행이다.
- 회신 형식: `ID / 통과·실패·미수행 / 실제 결과 / 필요 시 화면`.
- 신규 임시 프로젝트·빌드·도구 설치 없음. 제작 원본·PNG/meta·준비표만 보존하며 삭제할 부산물은 없다.

## 최종 생성 프롬프트

```text
Use case: stylized-concept. Generate ONE new Yura ultimate skill cut-in illustration for an anime action game. Reference roles: Player_Yura_Full.png is the SOLE character identity and outfit reference; CutIn_Arin.png and CutIn_MyoRyeong.png are STYLE/FINISH/action-energy references only. Do not borrow their identity, clothing, ears, or weapons.
Match the existing cut-in collection's refined anime illustration: thin dark linework, soft precise cel shading, richly modeled flowing hair, clear luminous eyes, restrained warm skin highlights, high-quality detailed clothing. Landscape 1536x1024 head-to-upper-chest closeup. Adult woman Yura: pale skin, vivid magenta-pink eyes, deep violet very long hair with straight layered bangs, two small rounded high side buns with long twin tails falling from them, small narrow dark purple pointed accessories above the buns, small muted green ties. Preserve the model's recognizable face and purple palette. Outfit faithfully follows reference: dark plum gothic dress, white upright ruffled shoulder trims and white collar, small black neck bow, bright magenta harness straps across the upper chest with small metallic fasteners, dark detached puff sleeves, black armbands, magenta lacing. Do not invent text or logos.
Pose: dynamic diagonal shoulders and slight torso twist, face in slight three-quarter turn looking toward the viewer, eyes sharply focused with determined lowered eyebrows, lips slightly parted in battle concentration; retain a soft attractive face shape, no smile. Flow long violet hair toward screen right, preserving clear readable eyes and complete buns/accessories with a little top margin. Face is large and central, shoulders fill bottom frame, chest modestly framed; no hands or weapons. The composition and rendering should look like a natural twelfth member of this exact illustration set, not a 3D render or full-body character sheet.
Genuinely transparent alpha background, isolated character only, natural clean contour antialiasing. No scenery, black backdrop, gray backdrop, panel, lettering, border, watermark, smoke, aura, bloom, or exterior glow. Keep lighting ON the character.
```

[전투 위키](../../wiki/combat.md) · [기존 제작 상태](STATUS.md)

