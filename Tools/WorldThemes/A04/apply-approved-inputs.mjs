import fs from 'node:fs';
const path='Tools/WorldThemes/A04/redforest-study.json',plan=JSON.parse(fs.readFileSync(path,'utf8'));
const approval=JSON.parse(fs.readFileSync('Tools/WorldThemes/Integration/A04-core-approval-v1.json','utf8'));
const baseline=JSON.parse(fs.readFileSync(approval.sourceBaseline,'utf8'));
if(!approval.coreAlphaWriteAllowed||approval.environmentPlacementAllowed||approval.historicalDecorationReplacementAllowed)throw new Error('첫 표면 승인 계약 필요');
plan.version='A04-surface-v1';plan.baselineContract=approval.sourceBaseline;plan.baselineSceneHash=baseline.sourceSceneHash;plan.baselineTerrainHash=baseline.sourceTerrainHash;plan.baselineNavMeshHash=baseline.navMeshHash;
plan.maskAuthorized=true;plan.corridorConfirmed=true;plan.corePolygon=approval.corePolygon;plan.preserveCorridor=approval.preservePathsXZ[0];plan.preserveCorridors=approval.preservePathsXZ;plan.corridorHalfWidth=approval.eligibleMask.pathHalfWidthMeters;plan.placements=[];
const poses=JSON.parse(fs.readFileSync('output/WorldThemeProduction/RedTreeForest/captures/BaselineV1/poses.json','utf8')).map(p=>p.file==='boundary_west.png'?JSON.parse(fs.readFileSync('output/WorldThemeProduction/RedTreeForest/captures/BaselineV2/poses.json','utf8'))[0]:p);
plan.capturePoses=poses.map(p=>({key:p.file.replace('.png',''),position:p.position,target:p.target,fov:p.fov}));
plan.approval='Tools/WorldThemes/Integration/A04-core-approval-v1.json';plan.notes='A00 첫 표면만 승인. 환경추가0/장식교체0/HeightDetailTree0. 기존 baseline461E 증거와 v3 새독립제작을 분리';
fs.writeFileSync(path,JSON.stringify(plan,null,2));
let builder=fs.readFileSync('Tools/WorldThemes/A04/build-redforest-study.csx','utf8');
if(!builder.includes('finally // A04')){const at=builder.indexOf('string scenePath =');builder=builder.slice(0,at)+'try{\n'+builder.slice(at)+'\n}finally // A04\n{if(sourceScene.isLoaded)UnityEngine.SceneManagement.SceneManager.SetActiveScene(sourceScene);}\n';fs.writeFileSync('Tools/WorldThemes/A04/build-redforest-study.csx',builder);}
console.log('A00 승인 surface 입력만 갱신; 기존 baseline 씬/기록 불변');
