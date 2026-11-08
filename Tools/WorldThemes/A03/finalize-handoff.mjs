import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';

const out = 'output/WorldThemeProduction/VillageFields';
const read = p => JSON.parse(fs.readFileSync(p, 'utf8').replace(/^\uFEFF/, ''));
const sha = p => crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const guid = p => fs.readFileSync(`${p}.meta`, 'utf8').match(/^guid: (\w+)/m)?.[1] ?? null;
const plan = read('Tools/WorldThemes/A03/village-study.json');
const scaffold = read(`${out}/scaffold-report.json`);
const applied = read(`${out}/applied.json`);
const selection = read(`${out}/candidate-selection.json`);
const before = read(`${out}/captures/before/poses.json`);
const after = read(`${out}/captures/after/poses.json`);
const baseline = read('output/WorldThemeProduction/A00/live-baseline.json');
const details = read(`${out}/detail-prototype-map.json`);
const trees = read(`${out}/tree-prototype-map.json`);
const dataAudit = read(`${out}/validation.json`);
assert(selection.reviewedInActualRender, '후보 실제 비교 기록 필요');
assert(applied.placements.length > 0, '배치 0개를 성공 처리하지 않음');
assert.equal(applied.outsideAlphaChannelsChanged, 0);
assert.equal(applied.heightDifferences, 0);
assert.equal(applied.detailDifferences, 0);
assert.equal(applied.hostDirtyBefore, applied.hostDirtyAfter);
assert.equal(sha(scaffold.sourceScene).toUpperCase(), scaffold.sourceSceneHash);
assert.equal(sha(scaffold.sourceTerrain).toUpperCase(), scaffold.sourceTerrainHash);
assert.equal(details.length, 6);
const poses = [];
for (const b of before.filter(p => /^0[1-6]_/.test(p.file))) {
  const a = after.find(p => p.file === b.file);
  assert(a, `후 사진 누락: ${b.file}`);
  for (const key of ['position', 'rotation', 'fov']) assert.deepEqual(a[key], b[key], `카메라 다름: ${b.file} ${key}`);
  for (const item of [b, a]) {
    assert.equal(item.effectiveSaturation, 15);
    assert.equal(item.effectiveContrast, 2);
    assert(Math.abs(item.effectiveBloom - .08) < .001);
    assert(item.fog && item.postProcessing);
    assert.equal(item.skyThickness, 1);
  }
  const files = ['before', 'after'].map(stage => {
    const p = `${out}/captures/${stage}/${b.file}`;
    assert(fs.existsSync(p));
    return { stage, path: p, sha256: sha(p) };
  });
  poses.push({ key: b.file.replace(/\.png$/, ''), position: b.position, rotation: b.rotation, fov: b.fov, time: b.time, weather: b.weather, renderSettings: { fog: true, fogStart: b.fogStart, fogEnd: b.fogEnd, effectiveSaturation: 15, effectiveContrast: 2, effectiveBloom: b.effectiveBloom, skyThickness: b.skyThickness, pipeline: b.pipeline, method: b.mode }, files });
}
assert.equal(poses.length, 6);
const files = [];
for (const root of ['Assets/01.Scenes/ArtReview/WorldThemes/Theme_VillageFields.unity', 'Assets/10.Datas/World/ThemeStudies/VillageFields', 'Assets/03.Prefabs/Environment/WorldThemes/VillageFields']) {
  const visit = p => {
    if (fs.statSync(p).isDirectory()) for (const name of fs.readdirSync(p)) visit(path.posix.join(p, name));
    else files.push({ path: p, bytes: fs.statSync(p).size, sha256: sha(p), guid: p.endsWith('.meta') ? null : fs.existsSync(`${p}.meta`) ? guid(p) : null });
  };
  visit(root);
  if (fs.existsSync(`${root}.meta`)) visit(`${root}.meta`);
}
const bounds = { minX: 980, maxX: 1160, minZ: 325, maxZ: 470 };
const protectedEntities = baseline.ids.filter(i => i.position[0] >= bounds.minX && i.position[0] <= bounds.maxX && i.position[2] >= bounds.minZ && i.position[2] <= bounds.maxZ);
const report = {
  themeId: 'VillageFields', version: plan.version, status: '독립 대표 구간 초안 제작 / 활동 공간 개선 확인·입구 및 마을 조망 개선은 작음 / 사용자 미술·플레이 인수 대기 / 본편 통합 미수행',
  sourceScene: scaffold.sourceScene, sourceTerrainGuid: baseline.terrain.guid, sourceTerrainHash: scaffold.sourceTerrainHash, baselineSceneHash: scaffold.sourceSceneHash,
  coordinateSpace: 'world', rootTransform: 'identity', coreMask: { shape: 'polygonXZ', points: plan.corePolygon, exclusions: ['기존 흙길 alpha', '기존 NavigationPreferences', '콘텐츠 반경14m', '중앙10m 이동 띠', '구조물 충돌', '20도 초과 경사'], ownership: 'A03', evidence: 'A00 승인 및 village-study.json' },
  seamProposalMask: { owner: 'A00', outerWidthMeters: 15, changes: [] }, referenceBounds: [bounds.minX, bounds.maxX, bounds.minZ, bounds.maxZ], referenceBoundsOrder: ['minX', 'maxX', 'minZ', 'maxZ'], referenceBoundsNamed: bounds,
  terrainSampleRect: { position: baseline.terrain.position, size: baseline.terrain.size, mode: '전체 TerrainData 독립 사본; core의 alpha만 변경', heightResolution: 4097, alphaResolution: 1024, detailResolution: 1024 },
  layerMap: scaffold.layerMap, detailPrototypeMap: details, treePrototypeMap: trees, scatterMode: applied.scatter,
  scene: scaffold.scene, terrain: scaffold.terrain, prefabPathsAndGuids: [{ path: applied.prefab, guid: applied.prefabGuid }], waterMaterialPaths: [], protectedEntities, replaceObjects: [], deleteObjects: [],
  unchangedOutsideCoreCheck: { alphaChannelsChanged: dataAudit.alphaOutside, heightSamplesChanged: dataAudit.heightChanges, detailCellsChanged: dataAudit.detailChanges, sourceSceneHashUnchanged: true, sourceTerrainHashUnchanged: true, hostDirtyBefore: applied.hostDirtyBefore, hostDirtyAfter: applied.hostDirtyAfter, meshBoundsExclusionsApplied: true, finalAudit: dataAudit },
  generatedAssets: [], newGeneratedAssetApprovals: [], sourceAssetReview: selection,
  capturePoses: poses,
  validation: [
    { id: 'IN-01', result: '통과', evidence: 'applied.json; height 전표본/detail6종 전셀/alpha core밖/원본hash/ID·발밑 보존' },
    { id: 'TH-02-static', result: '통과', evidence: '동일 6구도 Editor PreviewScene; 실효 PP/안개 기록' },
    { id: 'TH-01', result: '사용자 테스트 대기', evidence: 'comparison.html 및 user-test-spec.md VF-01~02' },
    { id: 'IN-02', result: '미수행', evidence: '본편 통합 전; 새 식생 Collider0. 검토 Host의 양방향 보행 검증 필요' },
    { id: 'PL-01/PL-02', result: '사용자 테스트 대기', evidence: 'user-test-spec.md VF-03~06' },
    { id: 'TH-03/PF-01', result: '미수행', evidence: '이동 중 LOD·바람·팝인·성능 미측정. 정지 사진으로 대체하지 않음' },
    { id: 'Player/Addressables', result: '미수행', evidence: '사용자 별도 지시 없는 검증용 빌드 금지' }
  ],
  knownIssues: ['입구와 마을 조망 01/02의 전후 개선은 작다. 활동 공간 03/출구04/서쪽 경계05의 흰 군락 분리가 개선된 작은 초안이며 마을 대표 풍경 완성이 아님', '정지 Editor 렌더이며 실제 TPS 전투·키보드/패드 플레이가 아님', 'ReferenceOnly의 기존 환경 메시/PreviewRig/후보 비교실/ThemeTerrain은 환경 프리팹에 포함하지 않음', '본편의 미저장 변경을 저장하거나 폐기하지 않음', '목표 프레임 예산·기기 성능은 이번 제작에서 측정하지 않음', '기존 MorganCargo 반복 동선 실패/최종 보정 미확정과 이번 환경 변경 회귀를 구분하며 일과 정상화를 이번 작업의 합격 조건으로 재정의하지 않음'], nextOwner: 'A12 독립 검수 → A00 대표 풍경 보완 판단 → 별도 Host 합성 → 사용자 미술·플레이 인수', ownedFiles: files
};
fs.writeFileSync(`${out}/handoff.json`, JSON.stringify(report, null, 2));
fs.writeFileSync(`${out}/handoff.md`, `# A03 여울마을 대표 구간 인계\n\n${report.status}\n\n- 독립 씬: ${scaffold.scene}\n- 전용 TerrainData: ${scaffold.terrain}\n- 추가 환경 프리팹: ${applied.prefab}\n- 실제 추가 ${applied.placements.length}개, 제외 ${applied.rejected.length}개. 표면 ${applied.paintedCells}셀.\n- core 밖 alpha 0, height/detail 변경 0, 원본 Terrain/본편 파일 hash 보존, 원본 dirty 시작·끝 동일.\n- 기존 NPC/콘텐츠/ID/진행/가뭄 상태/최신 NavMesh를 수정하지 않았다.\n- 신규 생성 자산·생성물 승인 0건. 기존 자원 변형 재질만 사용한다.\n\n동일 6구도 전후는 [comparison.html](comparison.html), 실제 절차는 [user-test-spec.md](user-test-spec.md), 수치/참조/hash는 [handoff.json](handoff.json)을 따른다. Editor 정지 사진을 실제 TPS 전투·LOD 이동·성능 인수로 판정하지 않는다. 다음 담당은 ${report.nextOwner}다.\n`);
const labels = ['입구', '대표 풍경', '활동 공간', '출구', '서쪽 경계', '동쪽 경계'];
const html = `<!doctype html><html lang="ko"><meta charset="utf-8"><title>A03 여울마을 전후 비교</title><style>body{font:16px/1.7 sans-serif;background:#f5f4ef;color:#26342e;margin:32px auto;max-width:1500px}section{margin:32px 0}div{display:grid;grid-template-columns:1fr 1fr;gap:12px}img{width:100%;height:auto}figcaption{color:#536057}figure{margin:0}h1{font-size:28px}</style><h1>A03 여울마을 진입 대표 구간</h1><p>동일 카메라·FOV60·안개ON100~650·실효 PP15/2/.08. Editor 정지 PreviewScene이며 사용자 미술/플레이 인수는 대기다. 새 생성물 0건. 본편 통합 미수행.</p>${poses.map((p, i) => `<section><h2>${labels[i]}</h2><div><figure><img src="captures/before/${p.key}.png"><figcaption>전</figcaption></figure><figure><img src="captures/after/${p.key}.png"><figcaption>후</figcaption></figure></div></section>`).join('')}</html>`;
fs.writeFileSync(`${out}/comparison.html`, html);
console.log(`A03 handoff prepared: ${files.length} files, ${poses.length} before/after views, ${applied.placements.length} placements; art/play acceptance pending`);
