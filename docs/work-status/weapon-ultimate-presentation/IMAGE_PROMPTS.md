# P0 얼굴 이미지 제작 기록

## 현재 적용본: 부드러운 인상 재제작 (2026-11-14)

사용자가 기존 컷인의 날카로운 인상을 지적해 두 PNG를 내장 ImageGen으로 재제작했다. **현재 적용본은 아래 재제작본이며, 뒤의 최초 제작 기록·해시는 이전 버전이다.**

- 원본 모델 기반 `Player_Raon_Full.png`·`Player_Hwarin_Full.png`, 라온 `Player_Raon_SmallIcon.png`, 화린 `dialogue/Portraits/Hwarin/Hwarin_Serious.png`를 직접 열어 참조했다. 구형 백발·붉은 피부 화린 초상화는 제외했다. 새 모델 뷰포트 촬영은 수행하지 않았다.
- 둥근 눈과 짧고 부드러운 얼굴 윤곽, 완만한 눈썹, 낮은 명암 대비·머리카락 광택으로 조정했다. 의상 색·옥 장신구·뿔·초커·곡옥은 원본 외형을 따랐다. AI가 원본/결과 이미지를 열어 비교했다. 최종 미술 인수는 사용자 테스트 대기다.
- 경로: 기존 `Assets/04.Images/SkillCutIn/CutIn_Raon.png`, `CutIn_Hwarin.png`를 교체하고 `.meta`/GUID를 유지했다. 원본 PNG와 생성 원본은 보존했다.
- Raon: 1536×1024 RGBA, 1,419,943 bytes, SHA256 `6E256C1B971710B0C3B3EEDF47AA9E541E763257151653A1C9C3F58573587E53`.
- Hwarin: 1536×1024 RGBA, 1,516,085 bytes, SHA256 `589FEB6B1B35C12F9A65594BAF87B47540598F10F5A68C5B19B8BDC7A9CDA52E`.
- 생성 원본: `C:/Users/scoke/.codex/generated_images/01a1d999-d952-7843-99cd-386859388205/`. Raon=`exec-5a632c83-5c66-49db-b356-aa1609cd556f.png`, Hwarin=`exec-48602b42-c3fd-4aed-b394-91a4565a85d6.png`. [최종 프롬프트](SOFT_REMAKE_PROMPTS.md).
- 이전 PNG 보존: [Raon](art-before-soft-remake/CutIn_Raon.png), [Hwarin](art-before-soft-remake/CutIn_Hwarin.png), 합계 3,631,492 bytes. Git 추적 밖 아트이므로 임시 파일로 삭제하지 않는다.
- 검증: PNG 두 장의 32비트 알파·투명 모서리 확인. 열린 Unity에서 강제 재임포트 후 Sprite 로드2/2, 크기1536×1024, alphaIsTransparency=true, mipmap=false, PlayerCharacterDefinition의 ultimateFaceSprite 일치2/2 확인. 코드 변경 없이 이미지 교체만 수행해 C# 테스트·빌드는 실행하지 않았다.
- 사용자 확인 SR01: 라온/화린 초상화와 새 컷인을 비교해 눈매·얼굴·의상 일치를 확인한다. SR02: 기존 플레이 환경에서 각 캐릭터 궁극기를 실행해 얼굴 식별, 뿔/머리 잘림, 배경 테두리, 반복 시 인상을 확인한다. 둘 다 사용자 테스트 대기. 회신: `ID / 통과·실패·미수행 / 실제 결과 / 화면(필요 시)`.

## 최초 제작 기록 (이전 버전)

2026-11-14 기본 내장 ImageGen으로 신규 생성 후 배경 알파 추출 편집을 각 1회 수행했다. 요청서의 Codex CLI ImageGen은 이미지 생성 수단으로 해석했고 직접 API/키는 사용하지 않았다. 상태는 **잠정 채택, 사용자 스타일 확인 대기**다.

| 캐릭터 | 파일 | 크기/용량 | SHA256 |
|---|---|---|---|
| Raon | [PNG](../../../Assets/04.Images/SkillCutIn/CutIn_Raon.png) | 1536×1024 RGBA / 1,801,451 bytes | 26E7332BEE6BCF1C7EE54388737CDC70954995D27D84D77B9FF99872F6DA6A87 |
| Hwarin | [PNG](../../../Assets/04.Images/SkillCutIn/CutIn_Hwarin.png) | 1536×1024 RGBA / 1,830,041 bytes | A9358ADCF212D63781484EE97DE8DC852E8F4FEA54337D708328558638D76476 |

PNG는 Git ignore 대상이므로 `.meta`와 함께 추적 밖 아트로 보존한다. 기존 파일은 덮어쓰지 않았다. 생성 원본/편집본은 `C:/Users/scoke/.codex/generated_images/01a1d8a7-3583-7473-84fe-4ba9cbca1202/`에 보존한다.

## 공통 스타일 프롬프트

```text
Use case: stylized-concept
Asset type: transparent PNG ultimate-skill face cut-in for a game UI.
Create ONE new polished anime action-game bust illustration, preserving identity from supplied character references. New artwork, not a portrait crop.
Style: restrained thin dark outlines, crisp cel shading with softly blended secondary shadows, detailed readable eyes, controlled fabric highlights.
Composition: landscape 1536x1024, centered head-to-upper-chest, complete hair silhouette and ornaments, transparent margin. Slight three-quarter turn toward screen right, eyes toward viewer and slightly down, strong determined gaze, closed mouth.
Lighting: neutral soft upper-left key, restrained cool highlights ON character only.
Background: genuinely transparent alpha. No panel, text, border, hands, weapon, watermark, scenery, haze, aura or exterior glow.
UI will scale the bust beyond the short panel; do not bake a panel into the artwork.
```

## 개별 외형 프롬프트

Raon은 `Player_Raon_Full.png` 외형 정본과 `Screenshots/Raon.png` 보조 참조를 사용했다.

```text
Raon: youthful male East Asian fantasy swordsman; pale skin, short tousled charcoal-black hair, long center bangs partly crossing face, small curved ahoge, muted reddish-brown eyes. Long green-jade and brown tassel earring on right ear, dark green jade bead necklace with pale jade teardrop pendant. Black high-collared coat, muted burgundy shoulder lapels, lavender-gray edging, fine gold piping, burgundy inner tunic, small gold knotted frog fasteners. Focused calm combat readiness. Preserve identity, jewelry and clothing colors. No Hwarin features.
```

Hwarin은 `Player_Hwarin_Full.png`, `dialogue/Portraits/Hwarin/Hwarin_Serious.png` 외형 정본과 생성한 Raon의 스타일을 참조했다. 실제 `PlayerModel_Hwarin`의 `Honoka/ho_hair_alt1.mat`, `ho_face_alt4.mat`와 텍스처도 직접 확인했다. 구형 `Screenshots/Hwarin`/`Hwarin_Dialogue_Portrait`의 백발·붉은 피부·청색 외투와 혼용하지 않는다.

```text
Hwarin: young adult female East Asian fantasy oni warrior, light warm skin, ruby-red eyes. Short charcoal-black hair with red tips, straight bangs, small high bun with red accents. Two complete upward tapered warm red-orange forehead horns. Black choker with small gold geometric motif, silver chain and single silver magatama pendant. Black hooded short coat, dark red lining, red cord knots, small circular silver tomoe medallions at shoulders. Red inner kimono lapels over black top with small pale flower detail. Fierce determined gaze. Use current BLACK-HAIRED LIGHT-SKINNED version, never obsolete white-haired red-skinned alternate. Match Raon illustration finish only, never his appearance or clothing.
```

## 알파 보정 프롬프트

```text
Use case: background-extraction
Extract only the supplied finished character onto real transparent alpha. Remove all exterior gray/purple/red glow, smoky haze, ambient halo, bloom and shadow. All pixels outside hair, horns, ear, jewelry, skin and clothing must be alpha zero, retaining natural contour antialiasing. Preserve face, gaze, pose, clothing, ornaments, colors, lighting ON character, composition and exact 1536x1024 crop. No redraw, panel or text.
```

## 점검

아티스트가 외형 참조/최종 이미지를 확인했고 리드도 최종 2종을 열어 확인했다. 8픽셀 간격 24,576점 알파 표본: Raon alpha0=15,705, partial=8,871 / Hwarin alpha0=14,426, partial=10,150. 외부 지정 6점 모두 alpha0, 내부 최고 alpha254다. 썸네일이 투명 영역 RGB 광무리를 보여줄 수 있어 Unity 합성 외곽은 별도 확인한다. 이는 미술 인수나 최종 외형 승인과 다르다. 나머지 캐릭터는 스타일 승인 전 생성하지 않는다.
