import fs from 'node:fs';
const plan=JSON.parse(fs.readFileSync('Tools/WorldThemes/A04/redforest-study.json','utf8'));
const baseline=JSON.parse(fs.readFileSync(plan.baselineContract,'utf8'));
if(plan.maskAuthorized||plan.corridorConfirmed){if(!plan.approval)throw new Error('명시 승인 없이 잠금 해제');const approval=JSON.parse(fs.readFileSync(plan.approval,'utf8'));if(!approval.coreAlphaWriteAllowed||JSON.stringify(approval.corePolygon)!==JSON.stringify(plan.corePolygon)||plan.placements.length)throw new Error('첫 표면 승인 범위 위반');}
if(plan.baselineSceneHash!==baseline.sourceSceneHash||plan.baselineTerrainHash!==baseline.sourceTerrainHash||plan.baselineNavMeshHash!==baseline.navMeshHash)throw new Error('v2 기준선 불일치');
const builder=fs.readFileSync('Tools/WorldThemes/A04/build-redforest-study.csx','utf8');
const capture=fs.readFileSync('Tools/WorldThemes/A04/capture-redforest.csx','utf8');
if(builder.includes('preview.AddComponent(renderingType)'))throw new Error('전역 작성자 복제');
if(!capture.includes('temporaryTerrain.SetAlphamaps')||!capture.includes('poses.Length!=6')||!capture.includes('skyThickness!=1'))throw new Error('전후 캡처 계약 누락');
console.log('A04 static inputs: PASS; 승인된 표면만, 기존 baseline 증거 보존, 실제 검증은 별도 기록 참조');
