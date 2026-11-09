import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';

const base = 'output/WorldThemeProduction/VillageFields';
const out = `${base}/v2`;
const read = p => JSON.parse(fs.readFileSync(p, 'utf8').replace(/^\uFEFF/, ''));
const hash = p => crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const guid = p => fs.readFileSync(`${p}.meta`, 'utf8').match(/^guid: (\w+)/m)?.[1] ?? null;
const plan = read('Tools/WorldThemes/A03/v2-production-plan.json');
const applied = read(`${out}/applied-v2.json`);
const audit = read(`${out}/validation-v2.json`);
const previous = read(`${base}/revision-v1.1/handoff.json`);
const before = read(`${out}/before/poses.json`);
const after = read(`${out}/after/poses.json`);
const editorState = read(`${out}/final-editor-state.json`);
const exportReview = read(`${out}/export-v2.json`);
assert(plan.approvedByA00);
assert.equal(audit.heightChanges, 0);
assert.equal(audit.detailChanges, 0);
assert.equal(audit.alphaOutside, 0);
assert.equal(audit.footprintOutside, 0);
assert.equal(audit.centralCorridorOverlap, 0);
assert.equal(audit.identityMismatch, 0);
assert.equal(audit.missingScripts, 0);
assert.equal(audit.environmentColliders, 0);
assert.equal(audit.environmentScripts, 0);
assert(applied.placements.length > 0);
assert(applied.previousEnvironmentPoseUnchanged);
assert.equal(applied.previousAlphaCoreChanges, 0);
assert.equal(hash(previous.sourceScene).toUpperCase(), plan.expectedSceneHash);
assert.equal(hash('Assets/10.Datas/World/OpenWorld/Terrain.asset').toUpperCase(), plan.expectedTerrainHash);
assert.equal(hash(applied.sourceNavPath).toUpperCase(), applied.sourceNavHash);
assert.equal(before.length, 6);
assert.equal(after.length, 6);
const captures = before.map(b => {
  const a = after.find(r => r.file === b.file);
  assert(a, `후 사진 누락: ${b.file}`);
  for (const key of ['position', 'rotation', 'fov']) assert.deepEqual(a[key], b[key], `구도 불일치: ${b.file} ${key}`);
  for (const row of [b, a]) {
    assert(row.fog && row.postProcessing);
    assert.equal(row.skyThickness, 1);
    assert.equal(row.effectiveSaturation, 15);
    assert.equal(row.effectiveContrast, 2);
    assert(Math.abs(row.effectiveBloom - .08) < .001);
  }
  return { key: b.file.replace(/\.png$/, ''), position: b.position, rotation: b.rotation, fov: b.fov, time: '정지 낮', weather: 'A01-v1 고정 시험', renderSettings: { fog: true, fogStart: b.fogStart, fogEnd: b.fogEnd, saturation: 15, contrast: 2, bloom: .08, skyThickness: 1, pipeline: 'LakeVillage_RPAsset', method: 'Editor 정지 PreviewScene' }, files: ['before', 'after'].map(stage => { const p = `${out}/${stage}/${b.file}`; return { path: p, sha256: hash(p) }; }) };
});
const ownedFiles = [];
for (const target of [plan.targetScene, path.posix.dirname(plan.targetTerrain), path.posix.dirname(plan.targetPrefab)]) {
  const scan = p => { if (fs.statSync(p).isDirectory()) for (const file of fs.readdirSync(p)) scan(path.posix.join(p, file)); else ownedFiles.push({ path: p, bytes: fs.statSync(p).size, sha256: hash(p), guid: !p.endsWith('.meta') && fs.existsSync(`${p}.meta`) ? guid(p) : null }); };
  scan(target);
  if (fs.existsSync(`${target}.meta`)) scan(`${target}.meta`);
}
const report = {
  ...previous,
  version: 'VillageFields-v2',
  scene: plan.targetScene,
  terrain: plan.targetTerrain,
  terrainGuid: plan.targetTerrainGuid,
  prefabPathsAndGuids: [{ path: applied.prefab, guid: applied.prefabGuid }],
  workingCopyEvidence: plan.workingCopyEvidence,
  sourceBaselineEvidence: plan.sourceBaselineEvidence,
  originalArtPreservation: { sceneHash: applied.originalArtSceneHash, terrainHash: applied.originalArtTerrainHash, observedDirtyBefore: applied.originalArtDirtyBefore, observedDirtyAfter: applied.originalArtDirtyAfter, applyLoadedValidityNotCaptured: true, finalObservedScenes: editorState.scenes, status: '디스크 해시 보존 확인. 준비 당시 dirty=true에서 이후 false로 전환된 원인은 미확정이며 미저장 상태 보존 완료로 소급 판정하지 않음. 최종 관측은 IsValid/IsLoaded=true, dirty=false' },
  finalEditorState: editorState,
  prefabExportReview: exportReview,
  userTestSpecification: `${out}/user-test-spec.md`,
  artAssessment: '동쪽 집 옆의 흰 꽃 띠와 우물 울타리 바깥 군락이 실제 주거 화면에서 읽히며 길과 기초를 비운다. 개선은 두 포켓에 국한되고 넓은 풍경의 변화는 작다. 마을 전체 완성 또는 TPS 인수로 판정하지 않는다.',
  artLookVersion: plan.artLookVersion,
  sourceRevision: 'VillageFields-v1.1',
  priorRevisionArchive: `${base}/revision-v1.1/archive-manifest.json`,
  status: '실제 주거 구도의 독립 v2 시험 제작 / 사용자 미술·플레이 인수 대기 / 본편 통합 미수행',
  baselineSceneHash: plan.expectedSceneHash,
  sourceTerrainHash: plan.expectedTerrainHash,
  baselineStatus: plan.baselineStatus,
  coreMask: { shape: 'polygonXZ', points: plan.corePolygon, owner: 'A03', exclusions: ['실제 회전된 경로 우선 OBB', '흙길 alpha>.4', '진행 ID14m', '건물 Mesh footprint+.75m', '물리 충돌', '20도 초과 경사', '기존 중앙10m 통로'] },
  seamProposalMask: { owner: 'A00', shape: '확장 core polygon 바깥 15m 띠', scope: 'A03 변경 없음. 정확 소유권은 A00 ownership.json 기준' },
  navigationPlacementExemption: plan.navigationPlacementExemption,
  sourceNavPath: applied.sourceNavPath,
  sourceNavHash: applied.sourceNavHash,
  sourceNavMetadataUnchanged: applied.sourceNavMetadataUnchanged,
  unchangedOutsideCoreCheck: { ...audit, previous36EnvironmentPoseUnchanged: applied.previousEnvironmentPoseUnchanged, previousAlphaCoreChanges: applied.previousAlphaCoreChanges },
  capturePoses: captures,
  additions: { root: applied.addedRoot, count: applied.placements.length, placements: applied.placements, rejected: applied.rejected, newPaintedCells: applied.newChangedCells },
  generatedAssets: [],
  newGeneratedAssetApprovals: [],
  validation: [
    { id: 'IN-01', result: '통과', evidence: 'validation-v2.json; 원본/높이/detail/core밖/이전36개/ID/발밑/Nav 보존' },
    { id: 'TH-02-static', result: '통과', evidence: '동일 주거 6구도·실효 안개/PP/하늘 조건; Editor 정지 시험' },
    { id: 'TH-01', result: '사용자 테스트 대기', evidence: 'comparison.html; A00/A12 판정과 사용자 미술 인수 구분' },
    { id: 'IN-02/PL-01/PL-02', result: '사용자 테스트 대기', evidence: '검토 Host 통합 후 실제 양방향 보행·전투·저장/로드·가뭄 상태 확인' },
    { id: 'PF-01/TH-03', result: '미수행', evidence: '실제 이동 중 팝인·LOD·바람·성능 미측정' },
    { id: 'Player/Addressables', result: '미수행', evidence: '별도 지시 없는 검증용 빌드 금지' }
  ],
  knownIssues: ['기존 공동 Prop_Well과 Bakery Mesh AABB 교차는 기존 문제이며 실제 관통 확정·수정 아님', '본편 외부 변경은 A00가 재기준선 검토하며 자동 원복하지 않음', '정지 아트 시험으로 실제 TPS 조작/전투 인수를 대신하지 않음', '기존 MorganCargo 미해결과 이번 환경 회귀를 구분함'],
  nextOwner: 'A12 독립 검토 → A00 실제 주거 사진 검토/Host 합성 → 사용자 미술·플레이 인수',
  ownedFiles
};
fs.writeFileSync(`${out}/handoff.json`, JSON.stringify(report, null, 2));
fs.writeFileSync(`${out}/handoff.md`, `# VillageFields-v2 인계\n\n${report.status}\n\n실제 집·마당·우물 두 포켓에 추가 ${applied.placements.length}개, 표면 ${applied.newChangedCells}셀을 시험했다. 이전36개/기존46셀은 보존했다. sourceScene/Terrain/Nav 해시와 ID317·좌표·발밑, 높이/detail/core밖 변경 검사는 validation-v2.json을 따른다. 실제 TPS·긴 플레이·성능·사용자 미술 인수는 대기다.\n\n[전후 비교](comparison.html), [정확 참조·해시](handoff.json), [제작 의도/선행 사진](production-proposal.md)를 함께 확인한다. v1.1은 revision-v1.1에 실제 에셋/.meta와 함께 보존했다.\n`);
const labels = ['동쪽 주거 진입', '기존 우물과 집 기초', '활동 마당', '출구', '서쪽 골목 경계', '동쪽 경계'];
fs.writeFileSync(`${out}/comparison.html`, `<!doctype html><html lang="ko"><meta charset="utf-8"><title>VillageFields-v2 전후 비교</title><style>body{font:16px/1.7 sans-serif;color:#26342e;background:#f5f4ef;margin:32px auto;max-width:1500px}section{margin:32px 0}div{display:grid;grid-template-columns:1fr 1fr;gap:12px}figure{margin:0}img{width:100%;height:auto}figcaption{color:#536057}</style><h1>실제 주거 구간 v2</h1><p>같은 카메라·안개ON100~650·실효 PP15/2/.08·하늘두께1. 기존 집/나무/진행 콘텐츠는 이동하거나 삭제하지 않았다. Editor 정지 시험이며 본편 통합·사용자 미술/플레이 인수는 대기다.</p>${captures.map((c, i) => `<section><h2>${labels[i]}</h2><div><figure><img src="before/${c.key}.png"><figcaption>전</figcaption></figure><figure><img src="after/${c.key}.png"><figcaption>후</figcaption></figure></div></section>`).join('')}</html>`);
console.log(`v2 handoff: ${applied.placements.length} additions, ${captures.length} comparisons, ${ownedFiles.length} artifact files; user acceptance pending`);
