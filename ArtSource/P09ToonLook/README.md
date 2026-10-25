# P09 얼굴·셰이딩 개선

셰이딩 단계에서는 Daon의 얼굴 전용 재질을 다듬고 실제 사용 P09 NPC 27개로 확대했다. 후속 Blender 조형과 얼굴 재질 단계는 Daon 한 명만 대상으로 한다. 원본 ExternalAssets, 장비, 프리팹 GUID는 보존했다. 최신 얼굴 재질 단계에서는 Daon 전용 눈 텍스처 사본을 사용한다. 빌더 전체 재생성과 커밋은 하지 않았다.

## 최신 — Daon 얼굴 인상 재설계

사용자가 기존 외형 보존보다 Bokusei에 가까운 미형을 우선하도록 승인한 후, 정규 Daon의 **얼굴 비례와 이목구비 배치**를 변경했다. 머리카락·의상·다른 NPC는 유지했다. 아래 DaonHead는 직전 버전이다.

- 눈 주변을 위로 이동하고, 눈 아래에서 입·턱까지의 구간을 더 짧게 만들었다. 눈썹은 얇고 완만하게, 입은 조금 넓게 조정했다. 제어 정점의 최대 이동은 약 18.03mm다. 본의 위치·순서·바인드 포즈는 변경하지 않았다.
- 피부는 코의 붉은 점·흰 하이라이트와 입술 광택을 제거한 애니메이션풍으로 다시 그렸다. 귀·목·작은 UV 섬은 이전 원본 베이크로 연결하고 눈썹·속눈썹의 보라색은 짙은 갈색으로 정리했다.
- 눈은 금빛 계열을 유지하면서 동공과 위쪽 음영을 선명하게 다시 그렸다. 별도 흰 반사 조각은 출력에서 제외하고 새 눈 텍스처의 하이라이트를 사용한다. 생성 도구는 내장 ImageGen이며 [피부 프롬프트](DaonPortrait/SkinPrompt.txt), [눈 프롬프트](DaonPortrait/EyePrompt.txt)에 기록했다.
- 무표정에서 치아가 점처럼 보이던 입술 안쪽 틈을 약 0.4mm 보정했다. 목의 검은 경계선은 이전 조형의 작은 위치 차이와 세분화 경계 수축에서 발생했다. 목의 16개 접합 정점을 원본으로 복원하고 정점 crease를 고정하여 몸통과의 틈을 제거했다. 피부색·법선의 미세한 차이까지 모든 조명에서 해결했다고 보지는 않는다.
- **폴리곤을 더 늘리지 않았다.** 정점 8,477 유지, 삼각형 14,456 → 14,400, 표정 36개 유지. 피부 텍스처 2K BC7, 눈 512 BC7로 직전 버전과 같은 해상도다.

[직전 정면](../../output/P09DaonPortrait/Before_0_Soft.png) / [현재 적용 정면](../../output/P09DaonPortrait/Applied_0_Soft.png) / [현재 반측면](../../output/P09DaonPortrait/Applied_35_Soft.png) / [측광](../../output/P09DaonPortrait/After_35_SideLight.png). 현재 적용본과 검수본의 정면 PNG 해시는 동일하다. 모든 캡처는 Editor PreviewRenderUtility 결과이며, Bokusei와 동등한 미적 완성도에 대한 사용자 판정을 대신하지 않는다.

제작 원본은 [DaonPortrait/Daon_Portrait.blend](DaonPortrait/Daon_Portrait.blend)다. 피부·눈 이미지를 패킹하고 원본 리그·37개 Shape Key(기준 포함)를 보관한다. Unity 출력은 `Assets/08.Models/Characters/DaonPortrait/`, `Assets/04.Images/Characters/DaonPortrait/`, `ToonLook/DaonPortrait/`에 있으며 검수 프리팹은 `EquipmentReview/DaonPortrait/Npc_Daon_Portrait.prefab`이다.

재출력 순서는 `DaonPortrait/sculpt_portrait.py` → `export_portrait.py` → Unity MCP 메서드 본문 `import_portrait.csx` → 검수 → **`DaonPortrait/apply_portrait.ps1`**이다. 직접 편집한 Blender 메시를 출력할 때는 첫 sculpt 단계를 생략한다. 텍스처 합성은 `bake_portrait.py`다. `capture_portrait.csx`의 이전 조건은 DaonHead 에셋을 명시하므로 정규 프리팹 적용 후에도 비교 기준이 바뀌지 않는다. 빌더 전체 재생성 이후에는 헤어 복원 후 마지막에 최신 `apply_portrait.ps1`을 적용해야 한다. 전용 UV 재질을 기존 톤 프로파일에 단독 등록하지 않는다.

**Unity MCP 확인:** 36개 표정 단독 100% BakeMesh 유한 좌표 검사 통과, 눈감기·입벌림·미소와 좌우 시선 ±12도 고정 포즈 확인, Missing Script 0, 최종 Console 오류 0. 27개 NPC / 1,589개 렌더러를 재로드해 Daon 얼굴의 메시·피부·눈만 변경됨을 확인했다. 프리팹 파일도 이 세 GUID 외 내용이 동일하다. 헤어 SHA256 보존, 반복 적용 변경 0, 톤 프로파일 Apply 0 / 0. 근거는 `output/P09DaonPortrait/validation_result.txt`, `scope_result.txt`, 전후 참조 목록이다.

중간 MCP 연결이 끊겼으나 복구 후 최종 비례·정규 프리팹을 다시 검수했다. UI 조작은 하지 않았다. **실제 대화 Play Mode·자연 야간/실내·Windows Player·성능 실측은 미수행**이며 아래 DS-1~3은 최신 얼굴을 대상으로 사용자 테스트 대기다. 기존 저장·씬·설정은 변경하지 않았고 커밋하지 않았다.

## 이전 — Daon 얼굴 텍스처 재제작과 곡면 보강

정규 Daon 한 명의 얼굴을 전용 UV·텍스처와 고밀도 메시로 교체했다. 아래 얼굴 재질 단계는 이전 기록이다. 사용자가 수용한 헤어는 이번에 변경하지 않았다.

- **새 얼굴 텍스처:** 기존 전신 아틀라스에서 머리 UV를 분리하고 2048×2048 전용 Albedo를 제작했다. ImageGen으로 얼굴 피부·볼·입술 표현을 다시 그린 뒤 Blender 베이크에서 경계를 원본과 합성했다. 귀·목·입안·속눈썹은 원본 색을 옮겨 연결을 보존했으며, 이 부분까지 새 그림으로 전면 재제작했다고 보지 않는다.
- **형태:** 볼·콧등·입술을 추가로 최대 약 1.73mm 조정하고 얼굴·귀 곡면을 한 단계 세분했다. 입술 접합부·눈 부속 메시·열린 경계는 고정했다. 코 아래의 별도 짙은 선 장식 32삼각형은 출력에서 제외했다. 새 리토폴로지나 고유한 얼굴 디자인의 전면 교체는 아니다.
- **Unity 메시:** 정점 **2,354 → 8,477**, 삼각형 **3,460 → 14,456**. 원래 본 인덱스·바인드 포즈와 표정 **36개**의 이름·순서·프레임 가중치를 보존했다. 볼과 턱의 각짐이 줄어든 것을 정면·반측면에서 확인했다.
- **재질:** 새 UV에 맞지 않는 전신 Normal·ShadowMask·RimMask 등의 참조를 제거했다. 새 Normal 베이크는 눈가·입 주변의 투사 자국 때문에 채택하지 않았다. 추가 노멀맵 없이 메시 법선으로 표현한다. 기존 금빛 눈·헤어·의상은 유지했다.
- **비용:** Albedo 로드 2048 / BC7 / mip 12 / ReadWrite Off, 압축 mip 기준 추가 약 **5.33MiB**. 메시 Editor Profiler 메모리는 978,652 → 3,953,245바이트다. Player 메모리·프레임 비용 실측은 아니다. 다른 NPC가 공유 4K 피부를 계속 쓰므로 그 메모리가 제거된 것으로 계산하지 않는다.

[직전 얼굴](../../output/P09DaonHead/Before_0_Soft.png) / [새 얼굴 정면](../../output/P09DaonHead/After_0_Soft.png) / [반측면·측광](../../output/P09DaonHead/After_35_SideLight.png) / [눈감기](../../output/P09DaonHead/Pose_Eye_Blink.png) / [입벌림](../../output/P09DaonHead/Pose_Lip_aa.png). 같은 카메라·조명의 Editor PreviewRenderUtility 비교이며 실제 월드 캡처가 아니다. 미적 완성도가 Bokusei와 동등해졌다고 단정하지 않는다. 기존 얼굴과 몸 사이 목 경계선은 반측면에서 일부 남아 있다.

### 현재 에셋과 재적용

- Blender 저작 원본: [Daon_Head.blend](DaonHead/Daon_Head.blend), 약 31MiB. 37개 Shape Key(기준 포함)·곡면 Modifier·별도 고밀도 검수 메시와 새 피부 이미지를 보관한다. 새 피부는 blend에 패킹했다.
- Unity 메시: `Assets/08.Models/Characters/DaonHead/Female_Face_02.asset`.
- 피부: `Assets/04.Images/Characters/DaonHead/Daon_Head_Albedo.png`, `Assets/10.Datas/Actor/ScenarioResidents/ToonLook/DaonHead/Daon_Head_Skin.mat`.
- 검수 프리팹: `Assets/03.Prefabs/Characters/EquipmentReview/DaonHead/Npc_Daon_Head.prefab`. 정규 프리팹은 기존 `ScenarioResidents/Npc/Npc_Daon/Npc_Daon.prefab`을 유지한다.
- [TexturePrompt.txt](DaonHead/TexturePrompt.txt)에 도구·입력·프롬프트를 기록했다. `DaonHead/prepare_head.py`, `bake_head_transfer.py`, `bake_head_finish.py`는 제작 단계 기록이다. 자동 반복 호출 시 임시 베이크 사본이 추가될 수 있으므로 완성 에셋의 재적용 용도로 실행하지 않는다.
- 메시 재출력은 Blender의 `DaonHead/export_head.py` → Unity 메서드 본문 `DaonHead/import_head.csx` → 검수 순서다. 정규 프리팹에 적용할 때는 **`DaonHead/apply_head.ps1`**을 쓴다. 메시와 피부를 한 파일 쓰기에서 함께 교체하며 예상 외 GUID·렌더러 ID에서는 중단한다.
- **빌더 전체 재생성은 이 메시를 자동 복원하지 않는다.** 이전 헤어 적용까지 복원한 뒤 마지막에 `DaonHead/apply_head.ps1`을 실행하고 검수한다. 얼굴 전용 새 재질만 톤 프로파일에 넣으면 원본 UV에 잘못 적용되므로 넣지 않았다. 현재 적용본에 기존 프로파일을 반복 적용한 슬롯 변경은 0 / 0이다. 이전 `apply_daon_face.ps1`이나 `apply_daon_sculpt.ps1`만 실행한 상태를 최신 결과로 보지 않는다.

### 검증과 남은 확인

36개 표정 단독 100% BakeMesh 유한 좌표 검사, 본·바인드 포즈 보존, Missing Script 0을 확인했다. 얼굴 웨이트 합 최대 오차는 `1.192093E-07`이다. 눈감기·입벌림·미소를 이미지로 확인했다. 고개 회전 캡처는 헤어에 같은 회전을 수동 주입하므로 런타임 추적 검증이 아니다.

사용 P09 27개 / 1,589개 렌더러의 메시·재질 참조를 재로드 비교해 **Daon의 얼굴 메시와 피부 슬롯 한 개만 변경**됨을 확인했다. 다른 26개와 눈 슬롯은 동일하며 프리팹 GUID `dc565f06dcbb1904099feee3bba83d1c`를 보존했다. 헤어 파일 SHA256은 `88C8F7B469A4E529EA86ECFFAA4A24CE154AEADFCB2B0DCA92D0B58249A34C1D`로 동일하다. 재적용은 변경 0이다. 근거는 `output/P09DaonHead/validation_result.txt`, `before_references.txt`, `after_references.txt`, `scope_result.txt`다.

임포트 중 Default 플랫폼에 BC7을 직접 지정해 발생한 포맷 오류는 Default=Automatic, Standalone=BC7로 수정했다. 이전 Console 로그를 전체 오류 0으로 보고하지 않는다. 실행 씬·사용자 데이터·Player 설정은 변경하지 않았으며 커밋하지 않았다.

**실제 대화 Play Mode·야간/실내·Player·성능은 미수행**이다. 아래 DS-1~3은 최신 얼굴 에셋을 대상으로 계속 사용자 테스트 대기다. DS-2에서는 눈감기와 발음 중 입술·치아 관통을, DS-3에서는 얼굴과 목의 색·경계선을 함께 확인한다.

## 이전 얼굴 재질·눈 표현 개선

헤어의 각을 줄인 결과를 사용자가 수용한 뒤 얼굴 개선을 요청했다. 현재 헤어와 얼굴 메시를 그대로 두고 Daon의 `Female_Face_02` 재질 2개만 교체했다. 얼굴 메시 SHA256은 `CCC68DB4084B39597967C055619533EBC26D512C23E5C9C273439FEACC3B1D6E`로 전후 동일하다.

- 피부: 약한 따뜻한 색조 `(1, .97, .94)`, 그림자색 `(.90, .75, .73)`, 경계 `.46` / 블러 `.5`, Normal 강도 `.5`. 반사·Rim·RimShade를 꺼 과한 흰 가장자리와 광택을 줄였다. 기존 4K 피부 텍스처는 유지한다.
- 눈: 내장 ImageGen으로 원본 금빛 홍채의 반사 무늬와 동공을 정리한 사본을 만들었다. 최종 재질의 채도는 `.78`, 발광은 꺼져 있다. 생성 결과의 아틀라스 배경은 원본과 달라 픽셀 단위 UV 동일성을 주장하지 않는다. 실제 메시에서 사용되는 눈 영역은 정면·측광·어두운 조건 캡처로 확인했다.
- 눈 에셋: `Assets/04.Images/Characters/DaonFace/Daon_Eye_Hazel.png`. Unity 로드 **512×512 / BC7 / mip 10 / ReadWrite Off**. 압축 mip 기준 추가 GPU 메모리는 약 0.33MiB이며 Player 실측은 아니다.
- 전용 재질 2개: `ToonLook/DaonFace/Daon_Face_Skin.mat`, `Daon_Eye_Hazel.mat`. 기존 공유 재질을 수정하지 않았다.
- `P09_Daon_ToonLook.asset`에 `Female_Face_02` 전용 피부·눈 연결을 추가하여 빌더의 외형 적용에서도 복원한다. 프로파일 검증 통과, 현재 Daon에 반복 적용한 변경 슬롯은 0 / 0이었다.
- 보라·유나·FarmWorker_0에는 원래 피부·눈 재질 override를 넣어 상속 영향을 차단했다. **27개 NPC의 재질 참조를 재로드 비교하여 Daon의 두 슬롯만 변경됨을 확인했다.** 슬롯 수와 나머지 26개 NPC의 재질 참조는 동일하다.

[이전 얼굴](../../output/P09DaonFace/Before_Soft.png) / [현재 적용 얼굴](../../output/P09DaonFace/Applied_Soft.png) / [측광](../../output/P09DaonFace/Applied_SideLight.png) / [어두운 통제 조건](../../output/P09DaonFace/Applied_Dark.png). 모두 Editor PreviewRenderUtility 캡처다. 어두운 조건은 조명·환경광을 기본의 20%로 낮춘 실험이며 실제 야간·실내 검증이 아니다.

제작 기록: `create_face_materials.csx`, `apply_daon_face.ps1`, `capture_face_refine.csx`. 적용 스크립트는 이 단계 직전 바이트를 `output/P09DaonFace/BeforeAssets/`에 보관하고, 실패 시 호출 직전 상태로 복구한다. 반복 적용은 파일 바이트가 동일하다. 생성 도구·입력·최종 프롬프트는 [EyePrompt.txt](DaonSculpt/EyePrompt.txt)에 있다. 검사 근거는 `output/P09DaonFace/scope_result.txt`와 전후 재질 목록이다.

얼굴 형태와 표정 36개, 헤어 형상은 바꾸지 않았다. Missing Script 0. 실제 대화 Play Mode·자연 야간·실내·Player 검증은 미수행이며 아래 DS-1~3은 계속 사용자 테스트 대기다. 이번 결과는 눈의 선명도와 피부 재질 개선이며 코·입 조형이나 얼굴 화풍의 전면 재제작으로 보고하지 않는다.

## 후속 Blender 조형 — Daon 한 명

사용자가 Bokusei를 비교 대상으로 지정하여 형태 개선을 요청했다. 현재 정규 `Npc_Daon.prefab`에 적용했다. 다른 26개 NPC로 확대하지 않았다. 보라·유나·FarmWorker_0는 Daon을 상속하므로 각 Variant에 원본 메시 2개의 override를 추가하여 기존 외형을 유지했다.

- 헤어: **사용자 피드백에 따라 가닥 추가와 형태 변형을 모두 철회했다.** 원래 가르마·길이·전체 실루엣을 기준으로 각진 곡면만 부드럽게 한다. 원본 제어 정점 위치를 바꾸지 않고 연결 가능한 삼각형을 사각형으로 정리한 뒤 한 단계 세분한다. 같은 UV·법선의 코너는 공유한다. 세분화로 모서리와 끝부분이 조금 둥글어지는 것은 포함한다.
- 얼굴: 턱 끝과 아래 볼을 약 2.7mm 이내에서 조정하고, 눈 주변을 소폭 세로 확장했다. 눈썹 메시의 두께를 줄였다. 얼굴 토폴로지와 표정 이름·순서 36개를 유지했다.
- 재질·눈 그림·의상은 이번 조형에서 변경하지 않았다. 얼굴과 장비의 표현 밀도 차이는 여전히 남는다. Bokusei와 동등한 미적 완성도를 달성했다고 판단하지 않는다.
- Unity 렌더 정점: 얼굴 **2,354 유지**, 헤어 **3,596 → 11,170**. 헤어 삼각형 **3,148 → 14,100**. 전 NPC 일괄 적용이나 성능 검증 완료를 의미하지 않는다. 이번 헤어 정정 전후 얼굴 메시 파일의 SHA256은 동일하다.

처음 추가했던 앞머리는 뿌리가 접힌 띠처럼 솟고 끝이 큰 삼각형으로 떨어져 사용자 검수에서 거부되었다. 그 결과를 미적 개선으로 채택했던 판단은 철회한다. 해당 캡처와 제작 스크립트는 `output/P09DaonSculpt/Rejected_Fringe/`에 실패 근거로 보관한다. 현재 정규 프리팹에는 이 가닥이 없다.

### 산출물과 재적용

- Blender 원본: `DaonSculpt/Daon_Sculpt.blend` — 약 29MB, 원본 파츠·리그와 수정본을 함께 보관하는 저작 파일이다.
- Unity 메시: `Assets/08.Models/Characters/DaonSculpt/Female_Face_02.asset`, `Hair_03.asset`.
- 독립 검수 프리팹: `Assets/03.Prefabs/Characters/EquipmentReview/DaonSculpt/Npc_Daon_Sculpt.prefab`.
- `sculpt_daon.py`: 검수 씬의 원본 사본에서 조형. `export_sculpt.py`: Armature 변형을 제외한 로컬 메시·UV·법선·가중치·표정을 `DaonSculpt/sculpt.xml`로 내보낸다. FBX 재임포트로 기존 본 순서를 바꾸지 않는다.
- `import_sculpt.csx`: 원본 FBX 얼굴 정점과 오차 0으로 대응시키고 원본 Unity 본 인덱스·바인드 포즈에 연결한다. 검수 프리팹과 비교 캡처를 갱신한다. 헤어의 4개 초과 본 가중치도 보존한다.
- `apply_daon_sculpt.ps1`: 정규 프리팹의 메시 참조 2개와 상속 Variant의 고정 override만 변경한다. 두 번째 실행은 파일 내용이 같으며, 저장 실패 시 이 호출 직전 바이트를 복구한다. 기존 백업은 `output/P09DaonSculpt/BeforeAssets/`에 보존한다.
- **빌더 전체 재생성은 메시 교체를 자동 복원하지 않는다.** 재생성 후 위 적용 스크립트와 `validate_sculpt.csx`를 다시 실행해야 한다. 기존 renderer fileID가 달라졌으면 스크립트의 참조를 검토해야 하며 임의 폴백하지 않는다.

### 확인한 결과

Unity Editor의 PreviewRenderUtility 고정 포즈 캡처이며 실제 월드 Play Mode가 아니다.

- [이전 정면](../../output/P09DaonSculpt/Before_0.png), [최종 정면](../../output/P09DaonSculpt/Final_Reload_0.png), [최종 반측면](../../output/P09DaonSculpt/Final_Reload_35.png), [Bokusei 기준](../../output/P09DaonSculpt/Bokusei_0.png). 기준 캡처의 카메라 FOV·거리·조명은 동일하며 모델의 얼굴 크기 차이는 남는다.
- [눈감기](../../output/P09DaonSculpt/Pose_Eye_Blink.png), [입벌림](../../output/P09DaonSculpt/Pose_Lip_aa.png), [미소](../../output/P09DaonSculpt/Pose_Mouth_Smile.png).
- 36개 표정의 이름·순서와 100% 단독 BakeMesh 유한 좌표 검사 통과. 재질·본 순서·바인드 포즈 동일, Missing Script 0, 가중치 합 최대 오차 `1.788139E-07`.
- 27개 NPC / 1,589개 렌더러 메시 참조를 재로드 비교하여 Daon의 두 메시만 변경됨을 확인했다.
- 중간 반복 생성에서 `CopySerialized` 이후 스킨 메시의 GPU 버퍼가 갱신되지 않아 헤어가 사라지고 vertex stride 오류가 발생했다. 가중치 버퍼를 명시적으로 갱신하도록 수정했다. 최종 강제 재임포트와 정면·반측면 캡처 전후 Console 개수는 동일했다. 이전 오류 로그를 지우거나 전체 Console 오류 0으로 보고하지 않는다. `error CS` 검색은 0건이다.
- 고개 회전 이미지는 Preview가 ParentConstraint 플레이 루프를 진행하지 않아 머리와 헤어에 같은 회전을 수동 주입한 검사다. **런타임 머리 추적 검증으로 간주하지 않는다.**

검증 근거: `output/P09DaonSculpt/validation_result.txt`, `scope_result.txt`, `final_reload_log.txt`. 초기 `Hair_*Check`와 `After_Clay` 등은 제작 진단이며 최종 판정 이미지는 `Final_Reload_*`다.

### 사용자 테스트 대기

기존 저장을 덮어쓰지 않는 새 게임에서 호수 Daon을 확인한다. 이번 변경의 Windows Player 빌드·실제 대화 Play Mode·야간·실내·프레임 비용 측정은 미수행이다.

| ID | 절차 | 기대 결과 / 실패 기준 |
|---|---|---|
| DS-1 | Boot → 호수 → 게임패드로 Daon 대화 진입·종료 | 대화 거리에서 이전보다 얼굴 인상이 좋아야 한다. 눈이 가려지거나 헤어가 사라지면 실패. 대화 진행·포커스 복귀도 확인한다. |
| DS-2 | 대화·시선 추적 중 좌우와 위아래 고개 움직임, 눈감기·입 모양 확인 | 머리와 헤어가 같이 움직이고, 두피 노출·가닥 관통·눈꺼풀 틈이 없어야 한다. |
| DS-3 | 밝은 곳과 실제 야간/실내에서 정면·반측면 확인 | 앞머리 뿌리의 이음과 볼·턱 음영이 거슬리지 않아야 한다. 미적 개선 여부는 사용자 판단으로 기록한다. |

회신 형식: `DS 항목 / 통과·실패·미수행 / 실제 결과 / 캡처·로그`.

## 이전 셰이딩 단계의 최종 적용

- 얼굴: 그림자 경계 0.3 → 0.4, 블러 0.6 → 0.45, 최대 조명 1 → 0.9, 그림자색 `(1, .856, .825)` → `(.92, .78, .76)`. 밝은 월드에서 흰 느낌을 조금 줄이고 코·볼의 기존 명암을 유지한다. 얼굴 그림이나 인상을 새로 그리지 않았다.
- 얼굴 해상도: 여성 Body Diff의 **원본 4096px 파일을 그대로 복사**한 프로젝트 소유 사본만 얼굴 피부에 연결했다. 몸은 기존 2048px를 사용한다. 눈과 Normal은 원본 참조를 유지한다.
- 표준 피부·의상: 기존 Daon의 그림자색과 절반 윤곽선을 대응 재질에 적용했다. 베이스색·금속 반사·원본 텍스처를 유지한다.
- 표준 헤어: 그림자 블러 0.1, 기존 Daon 백라이트, 절반 윤곽선을 공유한다.
- 전용 재질: Sodam/Elder 피부의 저작된 색·그림자를 유지하며 윤곽선과 얼굴 조명 상한만 조정했다. Jun/Elder/Sodam 전용 헤어·수염은 윤곽선만 줄였다. `Assets/08.Models/` 전용 의상 재질은 교체하지 않았다.
- **FakeShadow와 4K Normal은 채택하지 않았다.** 실험용 Normal 에셋과 임시 검수 씬도 제거했다.

`Assets/10.Datas/Actor/ScenarioResidents/ToonLook/`의 공유 재질 23개와 프로파일이 원본이다. 이번 확대에서 재질 19개와 공용 프로파일 1개를 추가했으며 NPC별 복제는 하지 않았다.

`P09_Daon_ToonLook.asset`은 기존 Daon 빌더 연결을 유지한다. `P09_Residents_ToonLook.asset`은 원본/교체 재질과 얼굴 접두사 조건 27개를 보관한다. Jun/Shop/Sodam BuildPreset에는 공용 프로파일을 연결했다. 프리셋이 없는 수작업 Variant는 프리팹 공유 재질 참조에 적용했다. 빌더의 **외형 → 셰이딩 → 톤 프로파일**에서 같은 프로파일을 선택할 수 있다.

`Resolve`는 이미 교체된 재질도 원본으로 역해석한 뒤 얼굴 조건을 다시 평가한다. 같은 교체 재질이 서로 다른 원본을 가리키거나 연결이 연쇄되는 모호한 프로파일은 사전에 거부한다. 런타임 컴포넌트나 프레임 처리는 추가하지 않았다.

## 적용 대상

정확한 경로는 [APPLIED_NPCS.txt](APPLIED_NPCS.txt)에 있다. 시나리오 씬의 GUID 참조를 프리팹·데이터까지 추적하고 실제 활성 렌더러의 재질을 조사했다. 적용은 비활성 외형 슬롯까지 포함한다.

| 구분 | 대상 | 수 |
|---|---|---:|
| ScenarioResidents | Daon, Jun, Shop, Sodam, Yeonmok | 5 |
| HarborResidents | Bora, Dongjin, Goeun, Haejin, Minsu, Seongho, Taesu, Yuna | 8 |
| StonegateCitadel | Buyer, CargoWorker, Carpenter, ClothSeller, Dam, GateEast, GateWest, Haru, MarketWalker, RestVisitor, SquareWalker, VegetableSeller | 12 |
| Farming | FarmWorker_0, FarmWorker_1 | 2 |

FarmWorker 두 개는 기반 프리팹에서 상속하므로 파일을 수정하지 않았다. 나머지 25개는 재질 참조만 갱신했다. EquipmentReview/NpcVariants, AppearanceReview, 외부 패키지 데모·장비 프리팹은 제외했다. 검수 프리팹을 기반으로 하는 사용 Variant도 사용 Variant에만 override를 기록했다.

사용 씬 근거는 `output/P09ToonLook/scene-usage.json`, 원본 재질 조사는 `npc-usage.txt`, 최종 GUID·슬롯·재로드 결과는 `final-validation.txt`에 있다.

## 실제 Play Mode 확인

Unity 6000.3.21f1 / URP에서 Boot 초기화를 거쳐 LakeOfLifeScenario에 진입했다. 실제 Daon과 Bokusei를 배치하고 NPC 대화 스테이지와 프로젝트 CameraManager의 Dialogue Closeup 요청을 사용했다. 애니메이션이 있는 실제 월드에서 일시정지하여 전후 포즈를 고정했다.

기본 대화 카메라의 최소 거리 2.2m에서는 상반신 구도가 나왔다. 세부 검수에는 **런타임 설정 사본만** 최소 거리 0.5m, 요청 거리 0.85m로 바꾸었다. FOV 39, 카메라 위치 `(1005.22, 63.73, 438.82)`, 해상도 1024×1280을 전후 동일하게 사용했다. 저장된 카메라 SO는 수정하지 않았다. 입력부터 대화 그래프를 진행한 검증과는 구분한다.

| 캡처 | 조건과 판정 |
|---|---|
| [밝은 월드 이전](../../output/P09ToonLook/WorldPlay/World_Before.png) / [이후](../../output/P09ToonLook/WorldPlay/World_After.png) | 같은 월드 조명·포즈. 얼굴의 흰 느낌을 완화했다. 개선 폭은 절제된 수준이며 전면광에서 극적인 입체감을 주장하지 않는다. 눈 그림·헤어 실루엣 유지. |
| [어두운 조건 이전](../../output/P09ToonLook/WorldPlay/Dark_Before.png) / [이후](../../output/P09ToonLook/WorldPlay/Dark_After.png) | 같은 월드에서 방향광 12%, 환경광·SH 18%로 낮춘 통제 실험. 얼굴도 어두워지며 눈·입 식별 가능. 자연 야간/실내 위치 검증은 아님. |
| `WorldPlay/Dialogue_Initial.png`, `Dialogue_Closeup.png` | 기본 거리 대화 카메라의 실제 월드 구도. 확대 진단 캡처와 구분. |
| `PlayReview/`, `PlayRefine/`, `FakeSmallOffset/` | 월드 조명을 복사한 격리 Play Mode 고정 포즈 비교. 통합 월드 검증과 구분. |
| `Residents/Before/`, `Residents/After/` | 27개 NPC 동일 조건 얼굴·전신 PreviewRenderUtility 캡처 108장. 실제 Play Mode는 아님. |

[27개 얼굴 전후 모음](../../output/P09ToonLook/Residents/ContactSheet.png)은 각 쌍의 왼쪽이 이전, 오른쪽이 이후다. 개별 색상·의상·얼굴 인상 보존을 확인했다.

## FakeShadow 시험과 제외 근거

Bokusei 원본 프리팹의 M_Hair_Front에는 투명 헤어와 FakeShadow 재질이 순서대로 연결되어 있다. P09 Hair_03은 앞머리만 분리된 메시가 아니며 UV도 Bokusei 전용 마스크와 다르다.

| 항목 | Bokusei 실제 값 |
|---|---|
| 셰이더 | `_lil/[Optional] lilToonFakeShadow` |
| 얼굴 | queue 2000, stencil ref 51 / Always / Replace |
| FakeShadow | queue 2505, stencil ref 51 / Equal / Keep, read/write mask 255 |
| 깊이·블렌드 | ZWrite 1, ZTest LEqual, Cull Back, Zero / SrcColor 곱셈 |
| 변위 | `_FakeShadowVector = (-.6, .3, 0, .0035)` |
| 마스크 | Bokusei Hair FakeShadowMask, 원본 4096px / 임포트 1024px |

`lil_pass_forward_fakeshadow.hlsl`은 조명 방향으로 정점을 화면 공간에서 이동하고 UV0로 마스크를 읽는다. P09에는 런타임 SkinnedMeshRenderer 사본으로 시험했으며 저장된 메시·슬롯은 늘리지 않았다.

- 원본 마스크와 단색 실루엣 모두 비교했다. 실제 변위 .0035는 앞머리 경계에 얇은 색 띠·긴 가닥 자국을 만들었다. .025 확대 시험은 눈 옆·볼 방향에 뾰족한 그림자를 만들었다. 자연스러운 앞머리 음영 이득이 없었다.
- 단독 장면의 ref 51/52 결과는 동일했다. 실제 월드에서 Bokusei 얼굴을 겹친 장면은 같은 조명·포즈에서 ref만 바꿔도 **원본 마스크 65픽셀, 단색 실루엣 79픽셀**이 달라졌다. 다른 캐릭터 영역으로 효과가 전달되는 것을 확인했다. 영역은 x757~764, y564~579(아래 원점)다.
- 눈을 완전히 가리는 현상은 기본 변위 구도에서 보이지 않았지만 눈 인접부의 가닥 자국은 보였다. 모든 각도의 눈 안전성을 통과했다고 보지 않는다.
- 근거: `WorldFake/Solo_*`, `WorldFake/Overlap_*`, `WorldFake/stencil-comparison.txt`. [공유 51](../../output/P09ToonLook/WorldFake/Overlap_Silhouette51.png) / [분리 52](../../output/P09ToonLook/WorldFake/Overlap_Silhouette52.png). 단색은 결함을 드러내기 위한 진단용 색이다.

최종 P09 재질·프리팹에는 FakeShadow와 신규 스텐실 쓰기를 남기지 않았다. 추후 도입한다면 P09 앞머리 분리/전용 마스크, 눈 수신 제외와 캐릭터별 스텐실 계약부터 설계해야 한다.

## 해상도·메모리

실제 파일 헤더와 TextureImporter를 확인했다. 여성 Body Diff/Normal 원본은 각각 4096×4096, 기존 최대 크기는 2048, Standalone override는 꺼져 있었다. 눈은 **원본 자체가 512×512**이고 로드 결과도 512다. 눈 업스케일은 하지 않았다.

| 텍스처 | 기존 로드 | 4K 시험 | 압축 GPU mip 추정량 | 판정 |
|---|---|---|---|---|
| Female Body Diff | 2048 DXT1 | 4096 DXT1 | 2.67 → 10.67 MiB | 눈썹·입술의 작은 세부 차이를 확인하여 얼굴만 채택 |
| Female Body Normal | 2048 DXT5 | 4096 DXT5 | 5.33 → 21.33 MiB | 비교 영역 차이가 거의 없어 제외 |

원본 2K가 몸에 계속 필요하므로 최종 비용은 교체 차이 8 MiB가 아니라 **공유 4K Diff 추가 약 10.67 MiB**다. NPC별 텍스처 중복은 없다. 압축 mip 계산이며 Windows Player 전체 메모리 실측은 아니다. Editor Profiler 값은 `texture-comparison.txt`에 별도 기록했다.

같은 얼굴 영역에서 Diff만 바꾸면 평균 RGB8 차이 약 0.2645, Normal만 바꾸면 약 0.0000848이었다. 품질 점수가 아닌 변화량 보조 근거다. `texture-pixel-deltas.txt`와 PlayRefine 캡처를 함께 본다. 일반 대화 거리에서 4K 이득은 작다.

유지한 사본은 `ToonLook/Textures/P09_Female_Body_Diff_4K.png` 하나이며 Standalone 4096/DXT1, Read/Write Off다. PNG 바이트 SHA256이 원본과 동일함을 자동 검사했다. 남성·전용 피부는 이득 검증 없이 일괄 4K로 올리지 않았다.

## 구현·자동 검사

- Unity 컴파일 오류 0, 관련 EditMode **5/5 통과**. 얼굴 우선/눈 보존, 반복 적용·원본 동일 텍스처, 교체 후 얼굴 재평가, 모호한 연결 거부, 부분 적용 방지를 검사했다. `editmode-result.txt` 참조.
- 27개 재로드 후 Missing Script 0, 미적용 슬롯 0, P09 FakeShadow 슬롯 0. `final-validation.txt`에 GUID·슬롯 수를 기록했다.
- 두 번째 적용에서 생성 에셋 0, 변경 슬롯 0. 첫 적용 중 저장 직후 실패 주입으로 기존 파일 2개의 바이트 복구와 신규 에셋 제거를 확인했다. `rollback-result.txt`, `expansion-first-result.txt`, `expansion-result.txt` 참조.
- 저장이 추가한 NpcBrain 기본 필드 등 자동 재직렬화만 제거하고, 적용 직전 백업과 재질 이외 내용이 동일함을 27개 모두 검사했다. `preservation.json` 참조. 기존 사용자 변경은 보존했다.
- 임시 씬·4K Normal 제거, 시작 씬과 Enter Play Mode 설정 복구, 원래 LakeOfLifeScenario 편집 씬 복귀. ExternalAssets와 ProjectSettings 변경 diff 없음.

### 별도 문제와 남은 범위

초기 직접 월드 실행의 서비스 초기화 경고는 유효한 통합 검증에서 제외했다. Boot 경유 첫 시도에는 Mono `mono_class_init_internal` / `PlayableMethods::InvokePrepareFrame` 네이티브 크래시가 있었다. 정상 Domain Reload 재시도에서 월드 진입과 위 캡처는 성공했다. 크래시 원인 확정·해결로 보고하지 않는다.

성공한 월드 검수의 관리 코드 오류 로그는 0이었으나 무게 프로필 폴백, 카메라 안정화 지연, Graphics Ring Buffer, PlayableGraph 해제 경고가 관찰됐다. 카메라·애니메이션·로딩 계통은 이번 범위에서 임의 수정하지 않았다. 마지막 추가 Off51/Off52 대조 시도는 편집 모드로 돌아와 실행되지 않았으며 완료된 10장 비교에만 근거했다.

**미수행:** Windows Player 빌드/메모리 실측, 입력부터 자연스럽게 진행하는 대화 그래프, 자연 야간·실내 이동, 나머지 26개 NPC의 개별 실제 월드 Play Mode. 전체 검증 완료로 표시하지 않는다.

후속 사용자 확인은 기존 저장을 덮어쓰지 않는 새 게임에서 진행한다. `P09-W1`: 호수 Daon에게 게임패드로 대화 진입·종료 후 얼굴/눈/헤어와 포커스 복귀 확인. `P09-W2`: 실제 야간/실내에서 얼굴과 목의 이질감·눈 가림 확인. `P09-W3`: 항구·성채 남성/노인/Sodam 계열 각각 색상·장비·얼굴 확인. 흰색 포화, 눈 가림, 색상 유실, 서비스 예외를 실패로 기록한다. 회신 형식은 `항목 ID / 통과·실패·미수행 / 실제 결과 / 캡처·로그`다.

## 제작 기록

`.csx`는 Unity MCP execute_code에 메서드 본문으로 전달한 저작 기록이며 정식 런타임/메뉴 도구가 아니다. `create_materials.csx`, `apply_daon.csx`, `capture.csx`는 최초 Daon 단계 기록이다. `audit_usage.cjs`는 참조 조사, `expand_residents.csx`는 백업·실패 복구를 포함한 재질 참조 갱신, `verify_preservation.cjs`는 비재질 내용/텍스처 보존 검사다. 확대 스크립트는 `output/P09ToonLook/used-npcs.txt` 명시 목록만 사용한다.

`prepare_review.csx`는 실험용 4K Normal과 임시 씬을 다시 생성한다. `capture_play_review.csx`, `capture_refine.csx`, `capture_fake_small.csx`는 당시 조정 단계의 비교 기록이다. 현재 상태에서 무작정 재실행하면 당시 Baseline과 다르므로 이전 재질 상태를 먼저 확인해야 한다. `capture_world.csx`는 이전 얼굴값을 런타임 사본에 명시적으로 복원한다. `capture_world_fake.csx`는 일시정지한 실제 월드의 겹침 시험이다.

백업과 캡처는 `output/P09ToonLook/`에 보존했다. 이 폴더는 로컬 검증 산출물이며 Git 추적 여부와 별개다.
