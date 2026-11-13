# LakeVillageWorld 동물 관찰 근거와 행동 반영

Animal Pack Deluxe 17종의 77개체를 대상으로 한다. 공통 이동은 WildlifeCreature와 WildlifeHabitat가, 종별 생활 리듬과 접근 반응은 WildlifeProfileSO가 소유한다. 이 문서의 **관찰 근거**와 **게임 반영**은 구분한다. m/s, 거리, 시간, 무리 간격은 모두 게임 조정값이며 야생 개체의 실측 통계가 아니다. 생태 자료만으로 애니메이션의 적정 재생 속도나 사람을 피하는 고정 거리를 산출하지 않았다.

## 에셋 식별과 적용 범위

[제작자의 원본 소개](https://discussions.unity.com/t/animal-pack-deluxe/682814)는 asp viper, iron age pig, swan goose, fire salamander를 명시한다. 로컬 원본은 `Assets/ExternalAssets/Animals/Animal pack deluxe/`, 프리팹·클립의 정확한 경로는 `Tools/Wildlife/AnimalPackDeluxe.catalog.json`에 있다. 과거 패키지 소개는 현재 로컬의 클립 유무를 보장하지 않으므로 로컬 목록과 실제 로드 검사를 우선한다.

| 게임 분류 | 원본으로 확인한 범위 | 해석의 경계 |
|---|---|---|
| 소 | Cattle, male 변형 | 가축 역할. 품종과 Bos taurus/Bos indicus는 확정하지 않음 |
| 닭 | Chicken, 색 변형 | 마을 가금 역할. 품종·성별 미확정 |
| 염소 | Goat | 가축 역할. 품종 미확정 |
| 아이벡스 | Ibex | 야생 산양류. Capra ibex는 비교 자료이며 원본의 학명 확정이 아님 |
| 사슴 | Deer, male | 야생 사슴류. 붉은사슴·노루·꽃사슴 등 특정 종으로 단정하지 않음 |
| 토끼 | Wild rabbit, 색 변형 | 야생 역할. 흰 변형을 가축·알비노·겨울털로 단정하지 않음 |
| 멧돼지 | Wild boar | 야생 역할. 성별·연령 미확정 |
| 돼지 | Iron age pig | 일반 흰 가축돼지와 구분. 해당 명칭은 멧돼지와 Tamworth 교잡형을 가리킴. [실제 사육기관 설명](https://cotswoldfarmpark.co.uk/farm-park/blog/an-important-day-preparing-for-piglets/) |
| 개구리 | Common frog | Rana temporaria를 비교 자료로 사용. 색 변형만으로 별개 종을 만들지 않음 |
| 거위 | Swan goose | Anser cygnoides 계열 자료를 사용. 원본의 야생·가축 형태는 확정하지 않음. 항구·마을 근처 비전투 역할로 저작 |
| 쥐 | Rat | 종 미확정. Rattus norvegicus 자료를 조건부 비교로 사용 |
| 달팽이 | Snail | 종 미확정. 육상 유각 복족류 범위의 환경 조건만 적용 |
| 도롱뇽 | Fire salamander | Salamandra salamandra 자료와 대응. 지역 아종 미확정 |
| 독사 | Asp viper | Vipera aspis 자료를 우선. Vipera berus의 활동 시간을 대신 적용하지 않음 |
| 곰 | Bear | 종 미확정. 불곰 자료는 비교 근거이며 원본이 불곰임을 뜻하지 않음 |
| 늑대 | Wolf | 야생 늑대 역할. 아종·가족 관계 미확정 |
| 나비 | Butterfly | 종·성별 미확정. 모든 나비가 동일한 섭식·집단 행동을 한다고 가정하지 않음 |

## 실제 행동과 게임 반영의 대응

아래의 실제 행동은 링크의 서술 범위 안에서 요약했다. 자료가 확정하지 않는 출발 가속도, 정지 시간, 방향 전환 각속도, 개체 사이 거리, 행동 확률은 생태 사실로 기재하지 않는다. 게임 속 전환은 생태 전체를 복제하기보다 가까이서 관찰할 때 구분되는 특징에 집중한다.

| 대상 | 관찰 근거 | 기존 구현의 문제 | 이번 게임 반영과 사용 클립 | 한계 |
|---|---|---|---|---|
| 소 | 무리 생활과 함께 먹고 쉬는 경향. 사람이 바깥 압박 영역에 들어오면 돌아보며, 더 가까워지면 거리를 벌림. [수의행동 자료](https://www.msdvetmanual.com/behavior/behavior-of-production-animals/behavior-of-cattle), [미네소타대 관찰·취급 지침](https://extension.umn.edu/agriculture/animals-and-livestock/livestock-operations/basic-stockmanship) | 정지하면 항상 Eat, 가까우면 바로 Run | Cattle Idle/Walk/Run/Eat를 휴식·섭식·경계로 구분. 느린 회전·가속, 같은 목초지의 느슨한 무리. 떨어진 수컷 변형 1개체의 생활권을 가까이 조정 | 반추·누워 쉬기 클립 없음. 실제 무리 동조를 완전히 제거하지는 않되 정확히 같은 모션 위상은 피함 |
| 닭 | 바닥을 긁고 쪼며 먹이를 찾고, 깃털 손질·모래목욕·횃대 사용 등 다양한 활동. [대학 Extension 행동 자료](https://poultry.extension.org/articles/poultry-behavior/normal-behaviors-of-chickens-in-small-and-backyard-poultry-flocks/) | 긴 직선 배회 뒤 같은 Eat 반복 | Chicken Idle/Walk/Run/Eat, 작은 배회 반경과 짧은 체류, 느슨한 군집·개체 간격 | Eat를 모래목욕이나 발로 긁기로 바꾸어 부르지 않음. 둥지·횃대·모래목욕 미구현 |
| 염소 | 풀뿐 아니라 잎·어린 가지를 먹고, 무리가 합쳐지거나 갈라짐. 높은 식물에 접근할 때 자세를 바꾸기도 함. [수의행동 자료](https://www.msdvetmanual.com/behavior/behavior-of-production-animals/behavior-of-goats) | 풀 먹기와 무작위 보행만 같은 주기로 반복 | Goat Idle/Idle02/Walk/Run/Eat. 추가 대기 변주와 중간 길이 탐색, 느슨한 무리 | Idle02는 추가 대기 원본이며 수목 섭식 전용으로 주장하지 않음. 두 발로 서서 잎 먹기·등반 없음 |
| 아이벡스 | 산지·암석 지형, 계절과 성별에 따라 달라지는 무리. [Mammalian Species 논문](https://academic.oup.com/mspecies/article/doi/10.1644/830.1/2600872) | 마을 가축과 같은 움직임·대기 값 | Ibex Idle/Walk/Run/Eat. 기존 고지·서쪽 산기슭 유지, 무리 반경과 여유 간격, 사슴보다 작은 탐색 범위 | 학명·성별이 미확정이므로 번식 무리·수컷 경쟁을 만들지 않음. 절벽 등반·점프 미구현 |
| 사슴 | 비교종인 붉은사슴류는 사회적이며 이른 아침·늦은 저녁 섭식과 반추 휴식을 반복. 계절·성별에 따라 무리 구성이 바뀜. [미시간대 ADW](https://animaldiversity.org/accounts/Cervus_elaphus/) | 가까워지자마자 최고속 Run, 쉬는 시간이 이동 중 소진 | Deer Idle/Walk/Run/Eat. 먼 거리에서 관찰, 중간 거리 보행 회피, 근거리 달리기. 여유 간격의 작은 생활권 | 사슴 종별 반응 차이와 번식기 경계는 미구현. 상시 암수 쌍을 실제 가족으로 해석하지 않음 |
| 토끼 | 비교종 유럽굴토끼는 조건이 맞으면 굴 집단을 형성. [미시간대 ADW](https://animaldiversity.org/accounts/Oryctolagus_cuniculus/) | 넓은 배회와 일정한 휴식이 작은 먹이활동을 가림 | WildRabbit Idle/Walk/Run/Eat. 짧은 탐색·섭식 사이 관찰, 빠른 근거리 회피. 군집 강제 추종은 끔 | 원본이 굴토끼라고 확정하지 않아 굴 식민지·가족 무리·굴 속 소멸 없음. 도약의 월드 이동 일치는 사용자 검수 필요 |
| 멧돼지 | 암컷과 새끼의 무리, 성체 수컷의 단독성이 다르며 더울 때 낮 활동이 줄기도 함. [미시간대 ADW](https://animaldiversity.org/accounts/Sus_scrofa/) | 초식동물과 같은 즉시 도주 | WildBoar Idle/Walk/Run/Eat. 긴 먹이활동·관찰과 느린 몸 회전, 충분히 접근할 때 이탈 | 성별 미확정이라 현재 개체를 암컷 가족무리로 강제하지 않음. 궁지 방어·돌진·피해 판정은 별도 전투 범위 |
| 돼지 | 야생형 행동은 가축에도 남아 있으며 코로 땅을 뒤지기·후각 탐색·사회적 상호작용이 중요. [수의행동 자료](https://www.merckvetmanual.com/behavior/behavior-of-production-animals/behavior-of-swine) | Walk를 복제한 Run을 실제 달리기처럼 빠르게 이동 | iron_age_pig idle/walk/eat. 섭식 체류와 느슨한 무리. 빠른 이탈도 보행 클립을 적당히 빠르게 재생할 수 있는 범위로 제한 | 별도 달리기 없음. 땅을 실제 파거나 진흙 목욕을 구현하지 않음 |
| 개구리 | 낮에는 축축한 곳에 숨고 밤·비 오는 때 먹이를 찾으며 번식기 외에는 물에서 멀리 나오기도 함. [미시간대 ADW](https://animaldiversity.org/accounts/Rana_temporaria/) | Idle을 Feed로 복제해 가짜 섭식. 물가 이름과 실제 수면이 불일치 | common_frog idle/walk/run. 긴 정지와 작은 이동 범위, 근거리 이탈. 호수권 3개체를 실제 호숫가로 이동 | 섭식·수영·입수 없음. Walk/Run의 도약 착지와 평행이동은 Play Mode 검수 대상 |
| 거위 | 비교대상 Swan goose는 작은 집단으로 풀을 먹고 쉬며, 물가의 생활권을 이용. 가족의 경계 반응은 번식 상황에 따라 다름. [미시간대 ADW](https://animaldiversity.org/accounts/Anser_cygnoides/) | 마른 경사에 배치하고 가축과 같은 공포 도주 | swan_goose idle/walk/run/eat. 무리 간격·섭식·관찰 후 회피, 호수권 2개체를 실제 수면 가까이 이동 | 가축 여부 미확정. 수영·날기·새끼 방어 없음 |
| 쥐 | 비교종 집쥐는 해질녘·밤의 탐색, 후각과 익숙한 경로 이용, 군집 생활을 보임. [미시간대 ADW](https://animaldiversity.org/accounts/Rattus_norvegicus/) | Idle을 Feed로 표시하고 Walk로 고속 도주 | rat idle/walk. 작은 범위·짧은 멈춤과 제한된 빠른 보행. 항구 주변 기존 배치 유지 | 종 미확정으로 공개된 지역 전체를 무리로 묶지 않음. 벽 타기·통로 학습·둥지 없음 |
| 달팽이 | 육상 달팽이는 대체로 습하고 서늘한 곳과 은신처를 이용하며 밤·흐림·안개 때 활동. [UC IPM](https://ipm.ucanr.edu/home-and-landscape/snails-and-slugs/) | 보행 최솟값 0.1과 공통 도주로 작은 생물이 빠르게 미끄러짐 | snail idle/walk. 0.015m/s의 게임용 느린 이동, 작은 생활권, 접근하면 정지. 호수권 개체를 나무 가까운 육상으로 이동 | 정지는 위협 반응의 대체 표현. 껍데기 속 수축·더듬이 수축 모션은 없음 |
| 도롱뇽 | Fire salamander는 축축한 통나무 아래·틈 등에서 숨어 지내고 온화한 밤에 활동. [미시간대 ADW](https://animaldiversity.org/accounts/Salamandra_salamandra/) | Idle을 섭식으로 사용하고 개구리처럼 계속 이동 | FireSalamander Idle/Walk/Run. 긴 정지·작은 생활권·완만한 가속, 호수권 개체를 나무 주변으로 이동 | 축축함은 현재 식생·수면 근접으로 저작한 환경 해석. 실제 습도·밤낮 활성과 통나무 속 진입은 미구현 |
| 독사 | Asp viper는 햇볕·돌·관목의 은폐가 있는 곳을 이용하며 주로 매복 포식. 눈에 띄는 움직임에 은신처로 피하는 반응. [스위스 info fauna](https://www.infofauna.ch/de/beratungsstellen/reptilien-karch/die-reptilien/arten/aspisviper), [프랑스 자연사박물관](https://www.mnhn.fr/fr/vipere-aspic) | Sleep를 Feed로 표시 | Viper Idle/Glide/FastGlide/Sleep/Hissing. 긴 정지·휴식, 경계 표현과 완만한 활주 회피. Sleep는 Rest 슬롯으로 분리 | Hissing은 경계의 게임 표현이며 사람을 보면 항상 위협한다는 생태 주장이 아님. 독·물기·먹이 추적 없음 |
| 곰 | 비교대상 불곰은 대체로 단독이며 천천히 걷지만 빠르게 움직일 능력도 있음. 식량·사람·새끼 상황에 따라 반응이 다름. [미시간대 ADW](https://animaldiversity.org/accounts/Ursus_arctos/), [공공기관 조우 구분](https://www.fws.gov/bear-safety) | 경계 거리 안이면 즉시 도망가며 다른 종과 같은 Feed 반복 | Bear Idle/Walk/Run. 긴 관찰·휴식, 느린 회전과 보행 회피, 근접 압박 시 빠른 이탈. 무리 추종 없음 | 현재 먹이 오브젝트가 없어 공중 Eat는 해제. 채식 곰 전체를 부정하는 의미가 아님. 위협·공격·새끼 방어는 별도 전투 범위 |
| 늑대 | 늑대는 사회적이며 사람을 보면 물러날 수도, 멈춰 관찰할 수도 있음. [미시간대 ADW](https://animaldiversity.org/accounts/Canis_lupus/), [International Wolf Center](https://wolf.org/wolf-info/basic-wolf-info/wolves-and-humans/living-with-wolves-2/) | 동료와 멀고 항상 즉시 달려 피함 | Wolf IdleA/IdleB/IdleC/Walk/Run. 관찰·거리 벌리기·이탈, 두 개체의 생활권을 가깝게 조정하여 느슨한 동행 | 가족관계·계급·사냥·집단 포위는 구현하지 않음. 사체가 없어 Eat를 반복하지 않음. Howl은 현재 타이밍·음향 연결이 없어 미사용 |
| 나비 | 성체는 꽃꿀 이용, 일광욕, 종·성별에 따른 순찰·앉아 기다리기·젖은 흙 이용 등이 있음. [Smithsonian Gardens](https://gardens.si.edu/learn/for-educators/cultivating-habitat/butterfly-gardening/about-butterflies/) | 단일 Fly를 Idle·Feed에도 복제하여 진짜 휴식·섭식과 혼동 | butterfly_fly_cycle만 사용. 작은 비행 범위·완만한 가감속과 회전, 개체별 위상 분산. 군집 추종 없음 | 착지·흡밀·날개 접은 휴식은 원본에 없어 미구현. 제자리 날갯짓은 휴식·흡밀로 보고하지 않음 |

## 공통 코드 변경

- 휴식 타이머는 도착한 뒤에만 진행한다. 이동 시간이 길어도 도착 후 머무는 시간이 사라지지 않는다.
- 휴식·섭식·주변 관찰은 별도 구간이다. 직전 구간의 반복을 피하면서 선택하고 각 구간의 길이와 시작 구간·모션 위상을 개체마다 분산한다. 이는 생태 확률 모델이 아니다.
- 지상 APD는 Watching → Avoiding → Fleeing을 거리와 관찰 시간으로 구분한다. 갑작스러운 근접은 바로 이탈할 수 있다. 압박이 없어진 뒤 CalmSeconds가 지나고 귀환 지점도 안전할 때 귀환한다. 달팽이는 정지 반응을 쓴다.
- APD 이동에는 프로필 가속·각속도를 사용하고, 큰 방향 전환 때 속도를 낮춘다. 실제 NavMesh 속도로 보행 클립 재생률을 맞추되 원본이 보행만 가진 종은 가짜 Run으로 취급하지 않는다. 1배속 기준값은 초기 조정값이며 발 접지 영상 검수로 보정해야 한다.
- 목적지와 경로 샘플에 지면 높이·경사·수면·서식 반경을 검사한다. 완전한 NavMesh 경로만 사용한다. 개체 간 목적지 간격과 NavMeshAgent의 기존 회피를 함께 사용한다.
- 무리 결속은 소·닭·염소·아이벡스·사슴·돼지·거위·늑대에만 켠다. 같은 프로필이면서 최초 생활권이 가까운 개체끼리만 약하게 결속한다. 종 이름을 런타임에서 분기하지 않는다.
- WildlifeHabitat의 기존 일괄 Update를 유지한다. 개체별 Update·핫패스 LINQ·프레임별 컬렉션 생성은 추가하지 않았다. 원거리 정지 중 시간과 모션을 함께 멈추고 재개 시 현재 플레이어 위치로 판단한다.

## 데이터와 재생성

실제 조정은 `Assets/10.Datas/World/Wildlife/APD_*.asset`에서 한다. `Tools/Wildlife/behavior-tuning.json`은 새 프로필 생성의 초기 기준이며 `BuildGroundSpecies`가 새 프로필에만 적용한다. 기존 프로필 수동 편집을 평소 재생성으로 덮어쓰지 않는다. `Wildlife/관찰 행동 데이터 적용` 도구는 기준 수치와 역할별 클립을 **명시적으로 다시 적용**하므로 기존 수치 튜닝을 되돌리고 싶을 때만 실행한다.

`Tools/Wildlife/LakeVillageWorld.animals.json`은 77개체의 재생성 기준이다. 기존 ID·프리팹·스케일·회전은 유지한다. 이번 위치 조정은 이전 좌표도 함께 저장했으며, 배치 명령은 그 이전 좌표에 그대로 있는 동일 프리팹만 옮긴다. 다른 위치로 수동 수정한 개체는 건너뛴다. 수면 렌더러 참조는 빈 서식지에만 초기 연결하므로 수동 설정을 덮어쓰지 않는다. `OpenWorldSliceAuthoring`의 기존 월드 재생성 호출이 같은 배치 명령을 사용한다.

실제 수면은 LakeWater 높이 77, X 범위 932~1202, Z 범위 606~1066이다. 기존 lake-bank 그룹은 이름과 달리 그 남쪽의 마른 경사에 있었다. 7개체를 실제 물가의 육상 NavMesh로 옮겼다. 새 위치는 수면까지 지형 샘플상 약 2~11m, 검사한 여덟 방향 중 5~8방향이 도달 가능했다. 소 1개체는 기존 동료 가까이, 늑대 1개체는 기존 동료와 생활권이 겹치도록 조정했다. 개체 수 77은 검사 후 유지한 결과이며 고정된 설계 상수는 아니다.

9개체의 이전·이후 좌표, 동선·충돌·경로 검사 결과는 `output/WildlifeObservation/relocation.txt`, 실제 적용 기록은 `placement-application.txt`에 있다. 나무까지의 거리는 은신처의 참고값이며 실제 그늘·수풀 밀도나 습도를 측정한 값이 아니다. 포식자와 사슴·가축은 기존 서로 다른 생활권을 유지한다. 서로를 감지하거나 사냥하는 생태 시뮬레이션은 추가하지 않았다.

## 검증과 남은 한계

AI 자동 검증과 사용자 화면 판단을 구분한다. 수행 결과는 `output/WildlifeObservation/`과 `Logs/WildlifeObservation-*.log`를 참조한다. Play Mode의 보행·경계·도주·귀환·발 미끄러짐·무리 간격은 사용자 검수 명세(문서 삭제됨)에 따라 확인한다.

현재 한계는 낮밤·날씨·번식·개체 성별에 따른 행동 전환, 실제 은신처 진입, 수영·입수, 나비 착지·흡밀, 굴·목욕·반추·공격 클립 부재다. 공통 NavMeshAgent 타입을 쓰므로 모델의 체형별 통행 폭은 별도 NavMesh 베이크로 증명되지 않았다. 수면 회피는 렌더러의 XZ 경계와 수면 높이를 사용하는 평면 수면용 검사다. 복잡한 다층 수로·동굴 수면은 별도 공간 데이터가 필요하다.

## 관찰 영상과 참고 구간

공개 영상은 애니메이션을 비교하기 위한 참고자료다. 영상 속 종·상황이 원본 에셋의 종·상황과 같다고 가정하지 않는다. 프레임 표본은 `output/WildlifeObservation/references/`에 보존한다. 표의 구간은 원본 영상 기준이며, 표본만으로 반응 원인이나 행동 확률을 추론하지 않는다.

| 영상 | 확인한 구간 | 관찰·적용 범위 |
|---|---|---|
| [NPS Grizzly Bear, 50초 영상](https://www.nps.gov/yell/learn/photosmultimedia/vl_00451mts.htm) | 00:00~00:10 보행, 00:10~00:35 정지와 머리 방향 변화, 00:40~00:45 다시 이동 | 곰의 느린 보행과 정지 구간 대비. 눈 위 영상이며 게임 원본 곰의 종 확정·사람 감지 증거는 아님 |
| [NPS Wolf, 첫 17초 영상](https://www.nps.gov/yell/learn/photosmultimedia/vl_wolf.htm) | 00:00~00:15 자세·머리 방향·짧은 이동 비교 | 늑대가 항상 달리지만은 않는 모습. 촬영 거리가 멀어 발 접지 속도의 정밀 보정에는 부적합 |
| [NPS Elk in Autumn](https://home.nps.gov/media/video/view.htm?id=06DAF9AF-EC8D-4EC5-9037-A5B3984846BA) | 00:00~00:09 정지·고개 돌리기, 00:15~00:24 머리를 낮춘 섭식과 고개 들기, 00:51~00:57 여러 개체의 달리기 | 정지와 달리기·개체 간 위상 차이 참고. 번식기 수컷의 울음·경쟁을 일반 경계로 옮기지 않음. 달리기의 원인을 사람으로 단정하지 않음 |
| [NPS Elk Herd in Valle Grande](https://home.nps.gov/media/video/view.htm?id=ACBF67D5-E253-41E4-AF97-C6371457DE31) | 00:00~00:40 무리의 빠른 이동, 01:10~02:00 길 주변의 작은 집단 이동 | 달리기의 몸통·다리와 개체 간 간격 비교. 이 영상만으로 사람에게 쫓겨 달린다고 단정하지 않음 |
| [Florida State University Houpt Lab의 Sham Exposure](https://houptlab.org/resources/movies/) | 00:06~00:21 벽 주변 탐색, 약 00:24 머리를 들고 서기, 00:30~00:42 다시 이동 | 실험실 쥐의 보행·정지·탐색 자세만 참고. 앞선 30분 구속과 실험장 조건이 있으므로 야생 쥐의 자연 빈도·일과·사람 반응 근거로 사용하지 않음 |
| [Smithsonian 나비 흡밀, 15초](https://collections.si.edu/search/detail/edanmdm:yt_ochDIN5k4tQ) | 기관 설명상 전체 00:00~00:15가 흡밀 대상. 재생 장면 직접 검수 대기 | Pipevine swallowtail의 특정 영상. 현재 단일 Fly 클립만으로 흡밀을 구현했다고 할 수 없는 이유를 보여주는 추가 참고 |
| [Harvard 연구 소개와 Fire salamander 보행 영상](https://iobopen.com/2020/07/16/ahh-to-walk-like-a-salamander/) | 연결된 [실제 연구 영상](https://www.youtube.com/watch?v=vUpImA84bsk)의 세부 타임스탬프 검수 대기 | 네 발·몸통의 보행 결합 참고. 연구 소개가 연결한 영상이며 현재 재생 화면까지 확인하지 못함 |
| [Texas Tech 돼지 행동 영상](https://www.depts.ttu.edu/animalwelfare/research/Behavior-Videos/pig-behavior-videos.php) | Normal Locomotion의 세부 구간 검수 대기 | 정상 보행 항목만 참고. Painful Locomotion·Sham Chewing·Fighting을 평상 행동 레퍼런스로 섞지 않음 |
| [UF IFAS 닭의 긁기·쪼기](https://blogs.ifas.ufl.edu/collierco/2021/07/15/why-do-chickens-scratch-and-peck-the-ground/) | 연결 영상의 세부 구간 검수 대기 | 원본 Eat가 긁기·모래목욕까지 포함한다고 주장하지 않음 |
| [Smithsonian National Zoo 염소 소개](https://www.nationalzoo.si.edu/education/resources/meet-goats) | 연결 영상의 세부 구간 검수 대기 | Nigerian Dwarf goat의 먹기·놀기·일광욕 소개. 에셋 염소의 품종과 일치하는 자료는 아님 |
| [Parks Canada 곰의 민들레 섭식](https://parks.canada.ca/pn-np/mb/riding/nature/video/ours-bears-dandelions?wbdisable=true) | 기관 설명은 식물 섭식. 직접 재생 구간 검수 대기 | 곰을 육식 행동 하나로만 단순화하지 않는 근거. 현재 씬에는 해당 식물 먹이 상호작용이 없음 |

소·아이벡스·토끼·멧돼지·개구리·거위·쥐·달팽이·독사의 종에 맞는 보행/섭식/휴식/경계/도주 영상 세트와 정확한 구간은 아직 완성하지 못했다. 접근할 수 없는 YouTube·기관 플레이어는 검수했다고 표시하지 않았다. 따라서 **17종 모두에 대한 영상 레퍼런스 검증은 미완료**이며, 구현은 위 생태 문헌의 정성적 근거와 실제 에셋에서 사용할 수 있는 동작에 한정한다. 원본 없는 동작을 영상 근거만으로 만들거나 기존 클립의 이름을 바꾸어 구현했다고 보고하지 않는다.

## 활동 시간과 반응을 해석할 때의 조건

닭의 야간 횃대 휴식은 [Extension의 횃대 행동 설명](https://poultry.extension.org/articles/poultry-behavior/normal-behaviors-of-chickens-in-small-and-backyard-poultry-flocks/)을, 토끼 비교종의 야행·박명 활동과 위험 때 뒷발 두드리기는 [ADW](https://animaldiversity.org/accounts/Oryctolagus_cuniculus/)를 참고한다. 현재 두 행동 모두 전용 모션과 장소 연결이 없어 게임에는 추가하지 않았다. 늑대의 낮·밤 활동과 긴 이동은 [ADW](https://animaldiversity.org/accounts/Canis_lupus/)의 개체군 수준 설명이며 작은 관찰 생활권과 동일시하지 않는다.

개체 간 고정된 자연 거리나 사람이 나타난 뒤 반드시 관찰→회피→달리기를 거친다는 보편 법칙은 설정하지 않는다. 가축의 취급 이력, 야생동물의 사람 적응, 성별·새끼·먹이·계절·피난 경로에 따라 달라진다. 이번 단계 반응은 비전투 월드에서 플레이어가 읽을 수 있는 공통 제어 방식이며, 즉시 근접 시 단계 생략과 달팽이의 정지 예외를 데이터로 둔다. 개구리·도롱뇽·달팽이를 매 순간 물 위에 두거나, 모든 포유류에 무리를 강제하는 해석은 하지 않는다.

## 수행한 검증과 보관 자료

- Unity 6000.3.21f1에서 스크립트를 임포트·컴파일하고 Console 컴파일 오류를 확인했다. CLI 보조 컴파일 로그는 `Logs/WildlifeObservation-editor-compile.log`다. 기존 외부 에셋과 프로젝트의 경고는 별도로 남아 있다.
- EditMode는 시간 소진 회귀, 단계 반응·정지 예외, 느린 이동 속도, 점유·개인 간격, 실제 지면 투영, 기존 새의 착지·클립 연결을 대상으로 선택했다. 실제 NavMeshAgent 이동은 EditMode에서 실행되지 않아 귀환 경로 실행을 단위 테스트 통과로 표시하지 않는다. 최종 개수와 통과 여부는 `output/WildlifeObservation/editmode.json`에 기록한다.
- 실제 로드한 프로필 17개·프리팹 25개·APD 개체 77개의 필수 클립·루프·이벤트 제거·본 연결·Missing Script·NavMesh 접지·수면 아래 배치·반복 적용·수동 위치 수정 보존을 검사했다. 결과는 `asset-audit.txt`다. 선택형 Feed/Observe/Rest가 없는 것은 오류로 만들지 않았다.
- 실제 화면·입력을 통한 Play Mode 품질 검수는 사용자 대기다. Windows Player와 Addressables 빌드, 커밋은 하지 않았다.
- `output/WildlifeObservation/BeforeBehavior.unity` 약 146MiB는 작업 시작 당시 저장되지 않은 사용자 씬 편집을 보존한 사본이다. 커밋 전 사용자 편집이 포함되어 자동 삭제하지 않았다. 에디터 임포트 중 다른 작업이 병행되었으므로 이 사본을 일괄 덮어쓰기 복구에 사용하지 않는다.
- 관찰 근거로 인용한 프레임 표본·공개 플레이어 HTML·텍스트 로그는 보관한다. 임시 영상 디코더와 실패한 프레임 추출물, 재다운로드할 수 있는 MP4 원본은 정리한다. 프로젝트에 디코더 의존성을 추가하지 않았다.
- 월드 씬과 Wildlife SO는 기존 `.gitignore` 대상이다. 이번 반영은 실제 로컬 씬·에셋에 저장했으며, 코드·행동 기준 JSON·이전 좌표를 포함한 배치표도 함께 갱신했다. `.gitignore` 정책이나 기존 에셋 GUID는 변경하지 않았다.

## 먹이 탐색 묶음과 동료 경계 반응 보강

실제 행동의 종류를 참고하고, 횟수·거리·초 단위 리듬은 관찰 가능한 길이로 압축한 게임 조정값으로 둔다. 생태 자료가 현재 모델의 정확한 종이나 클립의 의미를 모두 확정하지는 않는다.

| 대상·행동 | 이번에 확인한 웹 근거 | 구현과 해석의 한계 |
|---|---|---|
| 소의 풀 뜯기 | [Merck 수의행동 자료](https://www.merckvetmanual.com/behavior/behavior-of-production-animals/behavior-of-cattle): 풀과 낮은 식물을 먹고 무리와 생활 | 기존 Eat 사이에 0.5~1.2m 보행을 넣는 3~5회 탐색 묶음. 반추·무리의 동시 섭식 구현은 아님 |
| 닭의 탐색·쪼기 | [Merck 가금 행동 자료](https://www.merckvetmanual.com/behavior/behavior-of-poultry/behavior-of-poultry): 먹이 찾기는 바닥을 뒤져 먹이를 찾는 행동 | Eat와 0.15~0.5m 보행을 4~7회 연결. 새 긁기 클립은 만들지 않음 |
| 염소의 선택적 채식 | [Merck 영양·채식 자료](https://www.merckvetmanual.com/management-and-nutrition/nutrition-goats/herbage-and-browse-usage-in-goats): 먹이의 일부를 선택하며 채식 | 2~4회, 0.4~1m 이동. 실제 관목·높이 선택은 없음 |
| 아이벡스의 채식 | [ADW Capra ibex](https://animaldiversity.org/accounts/Capra_ibex/): 식물성 먹이 이용 | 비교 종 근거로 3~5회, 0.4~1.1m 이동. 종 확정·등반 구현은 아님 |
| 사슴의 섭식과 경계 | [흰꼬리사슴 관찰 연구](https://pmc.ncbi.nlm.nih.gov/articles/PMC3945222/), [붉은사슴 방해 반응 연구](https://esajournals.onlinelibrary.wiley.com/doi/10.1002/ecs2.4281) | 3~6회, 0.4~1.2m 이동. 먹은 뒤 Observe로 전환. 주변 10m 동료 도주에 경계하는 규칙은 집단 위험 감지를 단순화한 게임적 추론이며 연구의 실측 전환 규칙이 아님 |
| 토끼의 채식·위험 관찰 | [스코틀랜드 정부 지침](https://www.gov.scot/publications/pet-rabbit-welfare-guidance/pages/2/): 풀을 먹고, 위협에 멈춘 뒤 가까우면 도주하며 동료에게 위험을 알림 | 3~5회, 0.15~0.55m 이동. 기존 접근 단계 유지. 주변 6m 동료 도주를 경계 신호로 쓰는 것은 발 구르기·소리를 대체한 게임적 단순화 |
| 멧돼지·Iron age pig의 먹이 탐색 | [ADW Sus scrofa](https://animaldiversity.org/accounts/Sus_scrofa/), [교잡형 확인](https://cotswoldfarmpark.co.uk/farm-park/blog/an-important-day-preparing-for-piglets/) | 멧돼지 0.3~0.9m, 돼지 0.25~0.75m를 3~5회 이동. 땅 파기·흙 변형 구현은 아님 |
| 거위의 채식 | [ADW Anser cygnoides](https://animaldiversity.org/accounts/Anser_cygnoides/) | 3~5회, 0.25~0.8m 지상 이동. 수영·수초 채집은 없음 |

ForageSteps와 ForageStepDistance를 둘 다 설정하고 Feed 클립이 있는 지상 생물에만 묶음을 시작한다. 섭식 → 관찰 → 짧은 이동 → 섭식을 반복하고 마지막 관찰 뒤 일반 배회로 복귀한다. 도착 전에는 시간이 줄지 않으며 위협 시 묶음을 중단한다. 지면·완전 경로·서식 반경·개인 간격 검사는 기존 경로를 유지한다. 짧은 이동 후보가 안전하지 않으면 다음 판단 주기에 다시 선택한다.

SocialAlertDistance는 사슴·토끼에만 적용했다. 살아 있고 활성화된 같은 프로필의 가까운 도주 개체만 반응 원인이 된다. Watching은 전파하지 않아 상호 경계가 영구 유지되는 순환을 차단한다. 직접 근접 위협이 우선하며, 동료 반응이 끝나고 CalmSeconds가 지나면 생활을 재개한다. 현재 거리 기반이며 시야 차폐·경고음 전달은 구현하지 않았다.

기존 프로필의 GUID·클립·사냥 데이터와 다른 동물의 설정은 보존했다. 재생성 기준 JSON과 BehaviorSettings도 같은 필드를 갖는다. 먹이 또는 대응 클립의 근거가 없는 곰·늑대·쥐·양서류·독사·달팽이·나비에는 새 탐색 묶음을 켜지 않았다.