# 아린 확정·묘령 궁극기 반격 컷인 후속 (2026-11-14)

[전투 위키](../../wiki/combat.md) · [궁극기 상태](STATUS.md)

## 현재 적용본과 사용자 판정

- **아린: 사용자 통과.** `Assets/04.Images/SkillCutIn/CutIn_Arin.png`, SHA256 `EF55FF25FED9A61E80475A2F72FC5E04DEB9E53D579509027002998EF599C555`. 회색감을 줄인 새 작화가 채택됐다. 이 후 묘령 수정에서 아린은 변경하지 않았다.
- **묘령: 재제작 반영·사용자 미술 확인 대기.** `Assets/04.Images/SkillCutIn/CutIn_MyoRyeong.png`, SHA256 `3A8B50BC5528E6A3E84AA1D1EA151936F6AD330E74A55E467E010E130F7B233E`. 모델 유사성만 살린 차분한 자세는 전투 느낌 부족으로 반려됐다. 현재는 방패로 공격을 밀어내면서 기합과 함께 궁극기 반격을 시작하는 장면이다. 양눈·입·상체 회전·앞쪽 방패 원근·머리카락 흐름으로 전투 동세를 표현했다.
- 실제 모델 초상화와 **사용자 통과 아린**을 참조해 내장 ImageGen으로 새로 그렸다. 기존 컷인 위 덧칠/필터를 적용하지 않았다. 다른 컷인과 H09는 이번 수정 범위에서 제외한다.
- PNG 두 장은1536×1024 RGBA. 최종 묘령의 투명 모서리/알파 형식·크기 확인. 아린 포함 다른 PNG 해시 변경0. `.meta`와 실행 SO는 변경하지 않았다. Unity 화면/최종 Sprite 임포트 검증은 미수행이며 이전 기록의 Default importer 문제는 별도 미해결 상태다. 코드·데이터 변경이 없어 C# 테스트/빌드는 실행하지 않았다.

## 출처와 보존

내장 ImageGen 원본 폴더는 `C:/Users/scoke/.codex/generated_images/01a1d999-d952-7843-99cd-386859388205/`다.

- 확정 아린: `exec-9f16c6ba-520e-4c96-a015-4f633acaa417.png`. 원본 아린 전신 초상화·화린 컷인을 참조했다.
- 현재 묘령: `exec-f9148a77-3bd1-49c4-8ac0-0de58474b755.png`. 원본 `Player_MyoRyeong_Full.png`·통과 아린 컷인을 참조했다.
- [직전 묘령](art-before-myoryeong-final/CutIn_MyoRyeong.png)·[모델 유사성 중심 반려본](art-before-myoryeong-final/CutIn_MyoRyeong_model-like.png)·[이전 두 장](FRESH_ARIN_MYORYEONG.md)을 보존한다. 생성 원본도 보존하며 삭제 대상 부산물은 없다.

## 사용자 확인

M01: 묘령의 표정/방패 밀기/머리 흐름이 전투·궁극기 컷인으로 읽히는지, 아린 및 다른 캐릭터와 화풍이 맞는지 확인한다. M02: 실제 패널에서 양눈 식별·방패가 얼굴을 가리지 않는지·귀/머리 잘림과 가장자리 확인. **두 항목 사용자 테스트 대기.** 회신: `ID / 통과·실패·미수행 / 실제 결과 / 화면(필요 시)`.

## 실제 프롬프트

### 확정 아린

```text
Create one completely NEW anime ultimate-skill cut-in illustration, NOT a repaint or edit. Reference 1 supplies character identity and costume. Reference 2 Hwarin supplies the exact finished game illustration style and COLOR VITALITY; use the same artist's fine linework, clear local colors, warm skin and dimensional soft-cel shading. Do not copy Hwarin's identity, costume, pose, horns or red palette. Match the visual color strength of reference2. COLOR IS CRITICAL: clean warm peach skin with warm rose blush, clear saturated irises, deep rich dark hair with defined strand highlights. Rich character-specific cloth colors that remain colorful in both lights and shadows. Keep BLACK hair truly near-black with small neutral/warm highlights, not a light gray or purple-gray mass. Highlights should be small and coherent; avoid all-over white/gray veiling. NO gray color grading, NO desaturation, NO milky/foggy veil, NO chalky pale overlay, NO lavender-gray wash, NO lifted dark values, NO large hazy rim glow. Maintain a firm black point and natural chromatic contrast, with smooth clean midtones within cel-shaped shadows. Detailed crisp illustrative rendering like reference2, not flat thick-outlined TV anime, and not blurry/muddy painting. Landscape 1536x1024 transparent RGBA, isolated head and upper chest only. Full hair/ears/goggles inside canvas with headroom, no text, watermark, backdrop, vignette, fog, glow, aura or particles. All empty pixels have zero alpha; do not fill transparency with gray. No full body or extra characters. Character posture and facial acting must read as an imminent ultimate attack. ARIN: original young adult swordswoman with long black hair, red-brown eyes, black choker, white and bright TEAL sailor collar, saturated teal necktie with small gold chevrons, black jacket with saturated teal lining and white sleeve marks. Keep her exact face and design from ref1. Torso angled viewer-left, head near front with slightly lowered chin, calm intense resolve before sword draw. Hair flows to viewer-right in broad clean locks, necktie flicks to viewer-left. BOTH pupils must focus naturally on ONE distant target DIRECTLY AT CAMERA, aligned eye axes, matching catchlight direction; no divergent or crossed eyes. Youthful soft face proportions and closed mouth, no friendly smile, no snarl. Teal fabric must be clean rich green-blue, not gray-cyan. Black hair and black jacket keep deep values, skin has peach warmth and dimensional color like Hwarin. No hands or weapons in bust. Draw entirely new composition, never imitate Hwarin's actual pose or features.
```

### 현재 묘령

```text
Create a completely NEW MYORYEONG ULTIMATE COUNTERATTACK CUT-IN for an anime combat game. Not a portrait, not an idle guard: this is the exact impact frame where she forcefully DEFLECTS an enemy attack with her shield and launches her ultimate counter. Reference1 defines her original model identity. Reference2 approved Arin defines the exact artist/style, skin warmth, rich local colors, dark hair values and refined soft-cel rendering. Preserve the original youthful face, gold eyes, black cat ears with white accents, long black hair with side braids, modestly scaled silver aviator goggles on forehead, black ring choker, olive green off-shoulder jacket with ORANGE lining over black inner top. No new armor.
COMBAT ACTING IS THE PRIMARY REQUIREMENT: fierce focused golden eyes locked on enemy immediately beyond shield, eyebrows drawn decisively inward/downward, mouth OPEN in a short forceful battle cry with a small visible upper tooth line, cheek/jaw tension from exertion. She is committing all her strength to a decisive counterattack, NOT smiling, posing, or calmly gazing. Keep the same youthful facial anatomy as original, no mature glamour face. The head leans toward target with chin lowered, ears point alertly forward, neck and shoulder muscles show a controlled brace.
ACTION COMPOSITION: eye-level to slight low camera; torso upright but twisting powerfully toward viewer-left with a strong diagonal shoulder line. Her LEFT shield arm and shoulder on viewer RIGHT surge toward the foreground in a shield bash, shield edge enters LOWER RIGHT foreground as a close foreshortened dark metal shield with simple silver rim and no emblem. Shield must not hide face or eyes. Her RIGHT sword shoulder on viewer LEFT is pulled back for the follow-up slash, sword and hand stay OUTSIDE frame. The opposing shoulder positions create obvious counterattack mechanics. This is a braced athletic sword-and-shield fighter, NOT crouched cat-pounce. Coat collar/lining snaps back, black hair and braids STREAM decisively toward viewer-right from sudden acceleration, a few clean separated locks, no messy spiky explosion. Hair movement and expression communicate an explosive ultimate without background VFX.
Rendering must look drawn by the SAME illustrator as approved Arin reference2: fine clean contour lines, warm peach skin, clear rich gold eyes, deep black hair with restrained defined highlights, olive/orange cloth with nuanced soft-cel volume, proper black point. No gray veil, no desaturation, no glossy overpaint, no thick flat TV-anime shadows, no haze or overlit metal. All details crisp and coherent. Landscape 1536x1024 transparent alpha isolated head/upper chest/shield-edge. Entire ears and goggles inside frame with a clear gap above. No full body, no scenery, text, watermark, frame, aura, sparks, glow or fog. This must read immediately as an ultimate combat cut-in at thumbnail size.
```
