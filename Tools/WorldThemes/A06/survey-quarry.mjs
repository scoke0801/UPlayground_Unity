import fs from 'node:fs';
import crypto from 'node:crypto';
const output='output/WorldThemeProduction/QuarryPass';fs.mkdirSync(output,{recursive:true});
const live=JSON.parse(fs.readFileSync('output/WorldThemeProduction/A00/live-baseline.json','utf8'));
const form=JSON.parse(fs.readFileSync('Tools/OpenWorld/environment-form-plan.json','utf8'));
const snapshots=fs.readFileSync('output/OpenWorld/EnvironmentForm/placements-after.txt','utf8').trim().split(/\r?\n/);
const named=[...form.rocks.map(r=>r[0]),'채석장_하부파편','채석장_파편편석','채석장_고립전석'];
const objects=named.map(name=>{
 const row=live.branches.find(r=>r.path.endsWith('/'+name));
 const text=snapshots.find(r=>r.split('|')[0].endsWith('/'+name));
 const p=text?.split('|')[1]?.match(/[-+\d.]+/g)?.map(Number);
 return {name,path:row?.path,position:row?.position,scale:row?.scale,formerSnapshot:text,positionMatchesFinalSnapshot:!!row&&!!p&&row.position.every((n,i)=>Math.abs(n-p[i])<.001)};
});
const candidates=[
'Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Prefabs/Stones/Cliffs/PF_EastLands_Cliff_01.prefab',
'Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Prefabs/Stones/Cliffs/PF_EastLands_Cliff_02.prefab',
'Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Prefabs/Stones/Cliffs/PF_EastLands_Cliff_03.prefab',
'Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Prefabs/Props/StoneStairsWalls/PF_EastLands_StoneStairs_Rocks_03_Terrain.prefab',
'Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Rocks/TGV_Cliff_01A.prefab',
'Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Rocks/TGV_Cliff_02A.prefab',
'Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Rocks/TGV_Rock_Platform_01A.prefab',
'Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Background/TGV_BG_Mountain_01.prefab'
].map(path=>({path,exists:fs.existsSync(path),guid:fs.existsSync(path+'.meta')?fs.readFileSync(path+'.meta','utf8').match(/^guid: (\w+)/m)?.[1]:null,sha256:fs.existsSync(path)?crypto.createHash('sha256').update(fs.readFileSync(path)).digest('hex'):null,status:'파일 존재·GUID·hash만 조사. 실척/실제 렌더/URP/충돌/LOD/성능 미검증'}));
const protectedIds=live.ids.filter(r=>r.position[0]>300&&r.position[0]<650&&r.position[2]>1000&&r.position[2]<1450).map(r=>({path:r.path,id:r.id,position:r.position,ground:r.ground}));
const result={status:'A06 사전조사, Editor 미호출·독립 씬 미제작',objects,currentNavMeshSnapshot:live.nav,alreadyAddressed:['EnvironmentForm: 기존 Terrain 절개단·침식홈·능선의 큰 형태 저장','기존 PoorVillage 암반8개 Terrain 실제 정점 교차 검사 통과 기록','채석장 기존 파편3개 접합 저장','EnvironmentPolish: 두 동굴 터널 UV 파생 메시/원본 형상·충돌 보존','기존 진행ID·좌표·접지·길 보존 및 당시132방향 정적 경로 검사 기록'],remaining:['절개 단 반복성과 급경사 Terrain 텍스처 늘어짐','확대한 둥근 바위의 작은 균열이 절벽 규모와 맞지 않는 문제','작업 단·폐석·층상 절개면의 중간 규모 연결 및 실제 TPS 시야 검수'],referencePictures:['output/OpenWorld/EnvironmentForm/after/quarry-face.png','output/OpenWorld/EnvironmentForm/after/old-pass.png','output/OpenWorld/EnvironmentForm/after/quarry-yard.png'],pictureLimits:'과거 Editor 구조 사진이며 안개 off·FOV65. 현재 A01 공통 룩/실제 TPS와 같다고 보지 않는다.',proposedCore:[[330,1160],[400,1160],[400,1235],[330,1235]],coreStatus:'현재 후속 암반 위치를 근거로 고른 탐색 후보. 실제 bounds/보행·콘텐츠/지형·층 방향 조사와 A00 승인 전 편집금지.',referenceBounds:[300,620,1040,1280],referenceBoundsOrder:['minX','maxX','minZ','maxZ'],protectedIds,preserveRoutes:[{name:'채석장 진입·작업장·배후관찰',xz:[[542,1040],[539,1084],[558,1120]]},{name:'갱구·측면보상·귀환',xz:[[539,1084],[524,1110],[519,1107],[564,1105],[539,1084]]},{name:'옛 고개 정·역방향',xz:[[548,1240],[560,1330],[580,1435]]}],candidates,reexecutionAllowed:false,generatedAssets:[],limits:['당시589ID/132방향은 이번 검증 실적이 아니다. 현재 새 기준선으로 재확인해야 한다.','현재 A00 NavMesh snapshot과 과거 EnvironmentForm 최종참조를 구분하며 NavMesh를 되돌리거나 재베이크하지 않는다.','WorldExpansionActors 채석장 그룹의 루트0좌표는 콘텐츠의 실제 위치를 뜻하지 않아 자식/스폰/실제bounds를 조사해야 한다.']};
fs.writeFileSync(`${output}/survey.json`,JSON.stringify(result,null,2));
console.log(JSON.stringify({objects:objects.length,snapshotMatch:objects.filter(r=>r.positionMatchesFinalSnapshot).length,candidateFiles:candidates.filter(r=>r.exists).length,protectedIds:protectedIds.length,currentNav:live.nav,proposedCore:result.proposedCore,status:result.status},null,2));
