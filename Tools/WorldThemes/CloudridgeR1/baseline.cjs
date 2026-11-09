const fs=require('fs'),crypto=require('crypto');
const read=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const baseline=read('Tools/WorldThemes/AgentCoordination/baseline-ready.json');const clone=read('output/CloudridgeR1/clone.json');
const original=fs.readFileSync('output/WorldThemesSharedBaseline/Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity','utf8');
const ids=[...original.matchAll(/_guid: ([^\r\n]+)/g),...original.matchAll(/propertyPath: _guid\s+value: ([^\r\n]+)/g)].map(m=>m[1].trim().replace(/^"|"$/g,""));const actual=clone.ids.map(o=>o.guid);
const missing=ids.filter(id=>!actual.includes(id));if(missing.length)throw new Error('진행 GUID 불일치 '+missing.length);
fs.writeFileSync('output/CloudridgeR1/baseline.json',JSON.stringify({owner:'B',baselineId:baseline.id,sharedBaseline:baseline,files:read('output/WorldThemesSharedBaseline/files.json'),hashCheck:'baseline-hash-check.json',project:'C:/UsingProject/UnityProject/UPlayground',reviewScene:clone.review,sourceSceneGuid:clone.sourceSceneGuid,reviewSceneGuid:clone.reviewSceneGuid,contentGuidCheck:{sourceSerializedCount:ids.length,reviewCount:actual.length,missing},ownerMapping:clone.ownerPolicy,editor:'복제 종료 Edit Mode, dirty=false, Play/컴파일 비실행',localChanges:'../WorldThemesSharedBaseline/source-local-changes.txt',unsavedUserChanges:'기준선에서 제외. 저장·폐기하지 않음',sourceLaterChanges:'최종 통합 시 별도 3방향 비교; 덮어쓰기 금지'},null,2));
console.log(JSON.stringify({sourceIds:ids.length,reviewIds:actual.length,missing:missing.length}));


