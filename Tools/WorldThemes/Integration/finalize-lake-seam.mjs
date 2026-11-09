import fs from 'node:fs';
import crypto from 'node:crypto';
const folder='output/WorldThemeProduction/LakeShrine/SeamV3_1';
const read=p=>JSON.parse(fs.readFileSync(p,'utf8'));
const applied=read(folder+'/applied.json');
const working=read('output/WorldThemeProduction/A00/WorkingCopies/LakeShrineSeamV3.json');
const parent=read('output/WorldThemeProduction/LakeShrine/handoff.json');
const before=read(folder+'/captures/before/poses.json'),after=read(folder+'/captures/after/poses.json');
if(before.length!==6||JSON.stringify(before)!==JSON.stringify(after))throw new Error('전후 여섯 pose/실효 룩 불일치');
if(applied.outsideChangedChannels||applied.hostOutsideChangedChannels||!applied.heightEqual||!applied.detailEqual)throw new Error('보존검사 실패');
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex').toUpperCase();
const historical=read('output/WorldThemeProduction/LakeShrine/artifact-hashes.json').artifacts;
const paths=[...new Set([...historical.map(a=>a.path).filter(p=>p!==working.sourceScene&&p!==working.sourceScene+'.meta'&&p!==working.sourceTerrain&&p!==working.sourceTerrain+'.meta'),working.scene,working.scene+'.meta',working.terrain,working.terrain+'.meta'])];
const artifacts=paths.map(path=>({path,sha256:hash(path),bytes:fs.statSync(path).size}));
const handoff={...parent,version:'LakeShrine-SeamV3_1',parentVersion:'LakeShrine-final-v2',owner:'A00',status:'독립 경계 보완·사용자 미술 인수 대기',scene:working.scene,terrain:working.terrain,terrainGuid:working.terrainGuid,applied,artifactManifest:'artifact-hashes.json',capturePoses:after,generatedAssets:[],generatedApprovalRecords:[],replaceObjects:[],deleteObjects:[],nextOwner:'A12 / 사용자 / A00 HostReview',knownIssues:[
 'A12 사진 검토: 동쪽 수중 대각 초록 띠와 출구·서쪽 전경의 초록이 국소 완화됐다. 동쪽 얕은 초록 띠·육지 잔디·원경 호안은 남아 이음새 완성이나 호수 전체 완성으로 인수하지 않는다.',
 '정지 Editor 미술 비교이며 실제 TPS·입력·락온·회피·성능·긴 진행은 미수행이다.',
 'A01-v1 절차적 하늘 비교 룩이다. 현재 Host DaySky와의 최종 조화는 미검증이다.',
 '원래 아트 씬은 신규 기본 직렬화11줄이 외부 저장됐다. 저장 주체와 dirty 변경 원인은 미확정이며 원복하지 않았다.',
 '물 재질은 부모 v2 읽기 공유다. 전체 호수 물에 적용할 권한이나 전역 품질 검증으로 확대하지 않는다.',
 '본편 또는 HostReview에 통합하지 않았다. 높이·풀·기존 콘텐츠를 보존한 제한된 경계 표면 보완이다.'
]};
Object.assign(handoff,{
 baselineSceneHash:applied.sourceSceneHash,sourceSceneHash:applied.sourceSceneHash,
 sourceTerrainHash:applied.sourceTerrainHash,assets:artifacts,
 paintedCells:applied.changedCells,paintedCellsMeaning:'부모 v2 대비 이번 경계 보완의 변경 셀 수',
 seamProposalMask:{owner:'A00',outerWidth:15,innerCollar:4,waterAndProtectionFiltered:true,polygon:applied.coreMask},
 unchangedOutsideCoreCheck:{scope:'부모 v2 대비 collar+seam 밖 / Host 대비 core+seam 밖',alphaChanged:0,heightHashIdentical:true,detailHashesIdentical:true,sourceSceneHashUnchanged:true,sourceTerrainHashUnchanged:true,internalAlphaTextureSharing:false},
 validation:[{id:'IN-01',result:'통과',evidence:'적용 코드의 허용 밖/Host 영역 밖0·height/detail/tree/scatter 보존. 저장후 독립 검사는 별도 A12 증거.'},{id:'TH-01',result:'사용자 테스트 대기',evidence:'같은 조건의 전후6쌍 확보. 실제TPS·미술 인수 대기.'},{id:'IN-02',result:'미수행',evidence:'HostReview 통합·양방향 경로·진행 확인은 남음'},{id:'PF-01',result:'미수행',evidence:'LOD/GPU/프레임 실측 없음'}]
});
if(fs.existsSync(folder+'/a12-readonly-audit.json')){
 const audit=read(folder+'/a12-readonly-audit.json');
 if(!audit.pass)throw new Error('A12 후검사 실패');
 handoff.independentAudit={path:'a12-readonly-audit.json',pass:true,reviewer:'A12 코드 / A00 실행',ids:audit.ids.expected,idMismatch:audit.ids.idMismatch,maxPositionDelta:audit.ids.maxPositionDelta,maxGroundDelta:audit.ids.maxGroundDelta,limitations:audit.notPerformed};
 handoff.validation[0].evidence='적용 코드와 저장후 A12 읽기 재조회 모두 pass. 허용 밖/Host 영역 밖0·height/detail/tree/scatter 보존,317ID 좌표·접지 차이0. 강제 디스크 재로드는 미수행.';
}
fs.writeFileSync(folder+'/handoff.json',JSON.stringify(handoff,null,2));
fs.writeFileSync(folder+'/artifact-hashes.json',JSON.stringify({version:handoff.version,artifacts},null,2));
fs.writeFileSync(folder+'/handoff.md',`# 호수 경계 보완 ${handoff.version}\n\n독립 작업 사본의 물가 경계 표면을 보완했다. 본편 통합·전체 테마 완성·실제 TPS 통과가 아니다.\n\n- 씬: \`${working.scene}\`\n- 전용 지형: \`${working.terrain}\`\n- 환경 prefab·물 재질·기존6개 환경 배치는 부모 v2를 그대로 사용했다. 생성형 제작물0, 교체·삭제0.\n- 범위: core 안쪽4m와 바깥15m 중 실제 수면 아래·보호/이동로 밖. 소유권은 A00.\n- 실제 변경 ${applied.changedCells}셀. 작업전 대비 허용 밖 채널0, Host대비 전체core+seam 밖 채널0. 높이·Detail·tree·산포모드 동일.\n- 새 도색 셀은 합255의 UNorm8로 배분했다. 기존 지형의 비정규화 셀은 범위 밖에서 보존한다.\n- 전후6쌍은 카메라·FOV·안개·후처리·하늘 두께가 JSON 전체 동일하다.\n- 저장후 독립 검사: ${handoff.independentAudit?'통과.317ID·좌표·접지 차이0. 디스크SHA와현재clean로드메모리검사를분리했으며강제재로드는미수행.':'대기'}\n\n[비교 사진](comparison.html) · [적용 검사](applied.json) · [가중치 검사](alpha-check.json) · [A12 후검사](a12-readonly-audit.json) · [파일 SHA](artifact-hashes.json)\n\n${handoff.knownIssues.map(i=>'- '+i).join('\n')}\n\n첫 V3 시도는 검증 실패로 복구했으며 \`../SeamV3/rollback.json\`에 복구 SHA가 있다. 성공 검토본은 V3_1로 구분한다. raw applied.version은 실행 코드의 SeamV3를 보존하고, 최종 handoff.version으로 revision을 구분한다.\n`);
fs.writeFileSync(folder+'/user-test-spec.md',`# 사용자 직접 확인\n\n1. 원래 미저장 씬을 먼저 별도로 보존한 뒤 검토 씬을 연다: \`${working.scene}\`. 현재 열린 씬을 자동 저장하거나 폐기하지 않는다.\n2. 비교 사진의 입구·대표·활동·출구·양쪽 경계를 확인한다. 물 아래 초록 띠, 표면 얼룩·반복, 호안 재료의 끊김을 판정한다.\n3. HostReview 합성이 끝난 뒤 실제 TPS에서 양방향 이동, 락온·공격 예고·회피 방향·상호작용 시야를 확인한다. 현재 아트 씬에는 진행 액터/SceneContext/NavMesh를 복제하지 않았다.\n4. 통합 전후 같은 저장으로 퀘스트·보상·NPC·몬스터·접지·재입장 상태를 비교한다.\n\n현재 상태: 정지 미술 사진만 확보. 사용자 미술 인수 대기, TPS·진행·성능 미수행. 빌드하지 않는다.\n`);
const names={entry:'입구',representative:'대표 풍경',activity:'활동 공간',exit:'출구',boundary_west:'서쪽 경계',boundary_east:'동쪽 경계'};
fs.writeFileSync(folder+'/comparison.html',`<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>호수 경계 V3_1 전후</title><style>body{max-width:1400px;margin:32px auto;padding:0 20px;font:16px/1.7 "Malgun Gothic",sans-serif;color:#23352b;background:#faf9f5}.pair{display:grid;grid-template-columns:1fr 1fr;gap:16px}img{width:100%}figure{margin:0}h2{margin-top:36px}@media(max-width:760px){.pair{grid-template-columns:1fr}}</style><h1>호수 경계 V3_1</h1><p>v2 표면의 초록 경계를 잇는 제한된 독립 사본 보완. 좌측 전 · 우측 후. 같은 카메라·안개·후처리·A01-v1 하늘 조건. 실제 TPS 검증은 남아 있다.</p><p><a href="handoff.md">인계</a> · <a href="user-test-spec.md">직접 확인</a></p>${before.map(p=>`<h2>${names[p.key]}</h2><div class="pair"><figure><img src="captures/before/${p.key}.png"><figcaption>전</figcaption></figure><figure><img src="captures/after/${p.key}.png"><figcaption>후</figcaption></figure></div>`).join('')}</html>`);
console.log(JSON.stringify({version:handoff.version,artifacts:artifacts.length,changed:applied.changedCells,posesEqual:true}));
