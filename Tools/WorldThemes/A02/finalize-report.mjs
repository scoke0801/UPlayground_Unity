import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';

const folder='output/WorldThemeProduction/LakeShrine';
const plan=JSON.parse(fs.readFileSync('Tools/WorldThemes/A02/lake-study.json','utf8'));
const report=JSON.parse(fs.readFileSync(`${folder}/handoff.json`,'utf8'));
const validation=JSON.parse(fs.readFileSync(`${folder}/verification-final.json`,'utf8'));
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex').toUpperCase();
const sourceScene='Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity';
const sourceTerrain='Assets/10.Datas/World/OpenWorld/Terrain.asset';
if(hash(sourceScene)!==plan.baselineSceneHash || hash(sourceTerrain)!==plan.baselineTerrainHash)throw Error('원본 바이트 hash 불일치');
const files=[];
function walk(p){if(!fs.existsSync(p))throw Error(`납품 경로 없음: ${p}`);if(fs.statSync(p).isDirectory())for(const n of fs.readdirSync(p))walk(`${p}/${n}`);else files.push(p);}
walk('Assets/10.Datas/World/ThemeStudies/LakeShrine');
walk('Assets/01.Scenes/ArtReview/WorldThemes/Theme_LakeShrine.unity');
walk('Assets/01.Scenes/ArtReview/WorldThemes/Theme_LakeShrine.unity.meta');
walk('Assets/03.Prefabs/Environment/WorldThemes/LakeShrine');
const artifactHashes=files.map(p=>({path:p,sha256:hash(p),bytes:fs.statSync(p).size}));
for(const p of files.filter(p=>!p.endsWith('.meta')))if(!fs.existsSync(`${p}.meta`))throw Error(`meta 누락: ${p}`);
const pngs=fs.readdirSync(`${folder}/captures`).filter(p=>p.endsWith('.png'));
const photographHashes=pngs.map(name=>({path:`captures/${name}`,sha256:hash(`${folder}/captures/${name}`),bytes:fs.statSync(`${folder}/captures/${name}`).size}));
const finalPoses=JSON.parse(fs.readFileSync(`${folder}/captures/poses-Final.json`,'utf8'));
if(finalPoses.length!==6||finalPoses.some(p=>p.saturation!==15||p.contrast!==2||Math.abs(p.bloom-.08)>.0001||!p.fog||!p.postProcessing))throw Error('최종 6구도/실효 PP 조건 미충족');
const detailMap=JSON.parse(fs.readFileSync('output/WorldThemeProduction/common-detail-prototypes.json','utf8'));
Object.assign(report,{
  version:'A02-final-v2',
  status:'독립 대표 구간 제작·자동 보존 검사 통과 / 사용자 미술·실제 TPS 인수 대기 / 본편 미통합',
  selectedSurface:'RiverBed', selectedWaterVersion:'WaterV2', finalPrefix:'Final_RiverBed_WaterV2_',
  referenceBoundsOrder:['minX','maxX','minZ','maxZ'], referenceBoundsNamed:{minX:920,maxX:1215,minZ:690,maxZ:850},
  referenceBoundsXZ:[920,690,1215,850],
  detailPrototypeMap:detailMap,
  candidateCellCount:649, paintedCells:validation.alphaChangedCells,
  protectedNonIdAnchors:plan.protectedAnchors,
  privateMaterials:['Assets/10.Datas/World/ThemeStudies/LakeShrine/LakeWater.mat','Assets/10.Datas/World/ThemeStudies/LakeShrine/Reeds_Muted.mat'],
  waterOverrides:plan.waterOverrides,
  waterIntegrationScope:'독립 씬의 수면 전체에 적용한 후보. 본편 전체 LakeWater renderer 재질 변경은 승인되지 않았으며 core Terrain 합성과 별도 검수 필요.',
  finalVerification:'verification-final.json',
  colliderFollowup:{owner:'A00',reason:'최초 감사 Collider.bounds가 Transform 갱신 전 scale1.15 캐시였다. A00 Physics.SyncTransforms 후 재조회로 실제 scale0.38과 Renderer 일치 확인. 최초 원기록은 보존.',rockRootScale:[.38,.38,.38],correctedColliderMin:[970.8806,76.5962,758.8214],correctedColliderMax:[973.2440,78.5779,761.0700],evidence:'../A00/lake-collider-sync-evidence.json'},
  capturePoses:finalPoses,
  photographHashes, artifactHashes,
  trialConditions:[
    {prefix:'Before_',surface:'원본 alpha',water:'원본 재질 사본',additions:false,notes:'최종6구도와 같은 카메라/안개/실효PP'},
    {prefix:'Ground6_',surface:'Ground6 모래',water:'원본 재질 사본',additions:true,rockScale:'초기1.15/1.0/1.15',notes:'RiverBed_와 동일 조건 표면 비교'},
    {prefix:'RiverBed_',surface:'RiverBed',water:'원본 재질 사본',additions:true,rockScale:'초기1.15/1.0/1.15',notes:'Ground6_와 동일 조건 표면 비교'},
    {prefix:'Mixed_',surface:'수심별 Ground6/RiverBed 혼합',water:'WaterV2',additions:true,rockScale:'.38 uniform',notes:'초기 Mixed+원본물 사진은 덮어써 미보관. 표면 단독 비교에 포함하지 않음'},
    {prefix:'Final_RiverBed_WaterV2_',surface:'RiverBed',water:'WaterV2',additions:true,rockScale:'.38 uniform',notes:'갈대 전용 muted 재질 포함. 최종 검토판이며 사용자 승인 아님'}
  ],
  validation:[
    {id:'IN-01',result:'통과',evidence:'verification-final.json: height/detail 전체 동일, core밖alpha0, 원본파일 hash 동일'},
    {id:'AS-01',result:'통과(정적 범위)',evidence:'Prefab Missing/금지컴포넌트/nullMaterial/미지원shader0, 신규생성물0'},
    {id:'FOOTPRINT',result:'통과',evidence:'15개 Renderer/Collider AABB core포함·보호반경 침범0. Collider 원기록의 캐시 크기는 A00 동기화 재조회로 정정.'},
    {id:'TH-02',result:'통과(Editor 정지 렌더 조건)',evidence:'captures/poses-Final.json: 동일6구도·fogON·FOV60·실효PP15/2/.08'},
    {id:'TH-01',result:'사용자 테스트 대기',evidence:'comparison.html / user-test-spec.md LS-A01~05'},
    {id:'IN-02',result:'미수행',evidence:'본편 미통합, NavMesh/양방향 실제보행 미수행'},
    {id:'PL-01',result:'사용자 테스트 대기',evidence:'Host 검토 사본 통합 뒤 LS-P01~02'},
    {id:'PL-02',result:'사용자 테스트 대기',evidence:'Host 검토 사본 통합 뒤 LS-P03~04'},
    {id:'PF-01',result:'미수행',evidence:'목표기기·예산 미확정, 성능 측정 없음'}
  ],
  knownIssues:[
    '전체 테마 완성 아님. 첫 대표 수중 포켓과 작은 물가 군락만 제작했다.',
    '물재질 WaterV2는 전체 호수에 영향이 있는 후보이므로 본편 연결 전 물가 전역 검수가 필요하다.',
    'ReferenceOnly는 native 정적 시각 스냅샷이다. 진행 스크립트·기존 충돌·발소리·상태전환·식생 바람을 재현하지 않는다.',
    '입구/활동/경계 사진은 Editor 정지 렌더다. 실제 TPS/Play/LOD 이동 전이/성능은 미검증이다.',
    '초기 Mixed와 원본 물재질 사진은 미보관이다. Mixed WaterV2를 동일 조건의 표면 비교로 해석하면 안 된다.',
    '649개 시험 셀 중4개는 알파 양자화 후 원본과 같아 최종실제변경645셀이다.',
    'Final boundary_east 전경에는 core 밖 기존 초록 수중 띠가 남는다. A00 소유 seam 후속이며 경계 미술 합격으로 보지 않는다.',
    'Player/Addressables 빌드와 본편 통합·NavMesh 베이크는 수행하지 않았다.'
  ],
  generatedAssets:[],generatedAssetApprovalRecords:[],artAcceptance:'사용자 테스트 대기',playAcceptance:'사용자 테스트 대기',nextOwner:'A00/A12'
});
for(const item of report.assets){const p=plan.placements.find(p=>p.name===item.instance);if(p)item.scale=p.scale;item.renderStatus='Editor 정지 사진에서 실제 렌더 확인';item.lodTransitionStatus='미검증';item.collisionWalkStatus='미검증';item.performanceStatus='미검증';}
fs.writeFileSync(`${folder}/handoff.json`,JSON.stringify(report,null,2));
fs.writeFileSync(`${folder}/artifact-hashes.json`,JSON.stringify({artifacts:artifactHashes,photographs:photographHashes},null,2));

const labels={entry:'입구',representative:'대표 풍경',activity:'활동 공간',exit:'출구',boundary_west:'서쪽 경계',boundary_east:'동쪽 경계'};
const figures=Object.entries(labels).map(([key,label])=>`<section><h2>${label}</h2><div class="pair"><figure><a href="captures/Before_${key}.png"><img src="captures/Before_${key}.png" alt="${label} 전"></a><figcaption>전: 원본 표면·물 / 추가물 없음</figcaption></figure><figure><a href="captures/Final_RiverBed_WaterV2_${key}.png"><img src="captures/Final_RiverBed_WaterV2_${key}.png" alt="${label} 최종 검토판"></a><figcaption>후: RiverBed + WaterV2 / 바위 .38·갈대 muted</figcaption></figure></div></section>`).join('');
const candidates=['Ground6','RiverBed','Mixed'].map(key=>`<figure><a href="captures/${key}_representative.png"><img src="captures/${key}_representative.png" alt="${key} 시험"></a><figcaption>${key}${key==='Mixed'?' · WaterV2/축소바위/갈대 조정':' · 원본 물/초기 바위'}</figcaption></figure>`).join('');
const html=`<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>A02 호수와 사당 대표 구간 비교</title><style>body{margin:0 auto;max-width:1500px;padding:28px;background:#f2f0e9;color:#20261f;font:16px/1.6 system-ui,sans-serif}h1{font-size:30px}h2{font-size:21px;margin-top:32px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:16px}.candidates{display:grid;grid-template-columns:repeat(3,1fr);gap:14px}figure{margin:0}img{width:100%;height:auto;display:block}figcaption{font-size:14px;padding:6px 0}a{color:#365948}p{max-width:1050px}@media(max-width:800px){.pair,.candidates{grid-template-columns:1fr}}</style><h1>호수와 사당 · 첫 대표 구간</h1><p>최종 검토판 A02-final-v2. 기존 연속 수중 포켓의 표면 645셀과 작은 물가 군락 6개를 제작했다. height·Detail6·물수위·진행 좌표는 보존했다. 전체 테마 완성이나 사용자 미술 인수·본편 통합 완료는 아니다.</p><p>동일 6구도·FOV60·안개 ON·고정 낮·실효 채도15/대비2/Bloom0.08의 Editor 정지 렌더다. 실제 TPS·애니메이션·성능 합격 사진은 아니다. <a href="handoff.md">납품 기록</a> · <a href="user-test-spec.md">사용자 인수 명세</a> · <a href="verification-final.json">보존·footprint 검사</a> · <a href="artifact-hashes.json">파일·사진 hash</a></p>${figures}<section><h2>후보 비교 조건</h2><p>Ground6와 RiverBed는 원본 물재질·초기 바위 크기가 같아 표면을 비교할 수 있다. Mixed는 WaterV2·바위 축소·갈대 조정 후 사진으로 조건이 다르다. 초기 Mixed+원본 물재질 사진은 덮어써 미보관이다. 세 안을 동일 조건의 재질 단독 비교로 해석하지 않는다.</p><div class="candidates">${candidates}</div><p>선택 근거: Ground6의 넓은 갈색 판 느낌을 줄이는 RiverBed를 선택하고, WaterV2에서 반사/파문을 낮춰 얕은 바닥과 중심 물색을 구분했다. WaterV2는 독립 시험 제안이며 본편 전체 호수에 연결하려면 물가 전역 검수가 필요하다.</p></section></html>`;
fs.writeFileSync(`${folder}/comparison.html`,html);
for(const p of [...files,...pngs.map(p=>`${folder}/captures/${p}`)])if(!fs.existsSync(p))throw Error(`납품파일 누락 ${p}`);
console.log(`A02 final: artifacts=${artifactHashes.length}, captures=${photographHashes.length}, final poses=${finalPoses.length}, changed=${validation.alphaChangedCells}; source bytes unchanged; art/play pending.`);
