import fs from 'node:fs';
const read=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const out='output/QuarryPassR1';
const baseline=read(`${out}/baseline.json`);
Object.assign(baseline,{project:'C:/UsingProject/UnityProject/UPlayground',snapshotFiles:read('output/WorldThemesSharedBaseline/files.json'),sourceLocalChanges:fs.readFileSync('output/WorldThemesSharedBaseline/source-local-changes.txt','utf8'),contentContract:'content-ids.tsv: 기준 씬 직접 직렬화 GUID 322개 보존. 검토 씬 owner만 재매핑. 상속 prefab ID 변경 없음.',limitation:'전체 코드·프리팹의 제작 전 내용 사본은 확보하지 않음. 초기 로컬 변경 목록과 핵심 공유 자원 스냅샷 보존. 최종 의존성 해시는 별도 기록.'});
fs.writeFileSync(`${out}/baseline.json`,JSON.stringify(baseline,null,2));
fs.writeFileSync(`${out}/storage-status.json`,JSON.stringify({method:'공통 프로젝트 내부 개별 검토 씬',oldStorageBlock:'사용자 변경 지시로 해제',fullSnapshotCreated:false,unusedDirectories:'unused-project-folders.json',deleted:false},null,2));
