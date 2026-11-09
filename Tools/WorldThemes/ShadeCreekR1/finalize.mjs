import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
const out='output/ShadeCreekR1',data='Assets/10.Datas/World/ThemeStudies/ShadeCreekR1',scene='Assets/01.Scenes/ArtReview/WorldThemes/ShadeCreekR1.unity';
const read=p=>fs.readFileSync(p,'utf8').replace(/^\uFEFF/,'');
const load=p=>JSON.parse(read(p));
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
assert.equal(read(`${out}/before/poses.txt`),read(`${out}/after/poses.txt`),'전후 구도 불일치');
const validation=load(`${out}/validation.json`),preservation=load(`${out}/preservation.json`);
assert.equal(validation.idErrors.length+validation.ownerErrors.length+validation.protectedGroundChanges.length+validation.missingScripts+validation.heightOutside+validation.surfaceOutside+validation.detailOutside,0);
assert.equal(validation.paths.filter(p=>p.status==='PathComplete').length,10);
assert.equal(preservation.sourceChanges.length+preservation.missing.length+preservation.unapproved.length,0);
const links=[];
for(const name of ['RESULT.html','TEST_SPEC.html'])for(const m of read(`${out}/${name}`).matchAll(/(?:src|href)="([^"]+)"/g)){const p=path.resolve(out,m[1]);assert.ok(fs.existsSync(p),`없는 링크 ${name}: ${m[1]}`);links.push(m[1]);}
const files=fs.readdirSync(data,{recursive:true}).map(p=>path.join(data,p)).filter(p=>fs.statSync(p).isFile());
const handoff={version:'ShadeCreekR1',status:'독립 검토본 실제 제작·결함 보정·동일 조건 전후·검사·테스트 명세 작성',scene,sceneGuid:read(scene+'.meta').match(/^guid: (\w+)/m)[1],sceneHash:hash(scene),data,regularIntegration:'미승인·미수행',userArtAcceptance:'사용자 판단 대기',actualTPS:'사용자 테스트 대기',performance:'미측정·마지막 LOD의 LOD2 재사용 비용 확인 필요',saveRestore:'사용자 테스트 대기·검토본/정규 논리 지도 라우팅 구분',newAssetCandidates:[{path:data+'/CreekSurface.asset',kind:'검토본 전용 저작 수면 메시',approvedForRegular:false},{path:data+'/CreekWater.mat',kind:'기존 셰이더의 검토 수면 변형',approvedForRegular:false}],narrativeWater:'DroughtVillageRegion의 CreekArt·수위·진행 조건 유지. 동쪽 검토 수로는 비서사 환경 후보',sourcePreserved:preservation.sourceChanges.length===0,validation,files:files.map(p=>({path:p,size:fs.statSync(p).size,sha256:hash(p)})),captures:['entry','clearing','exit','return'].map(k=>({before:`before/${k}.png`,after:`after/${k}.png`})),report:'RESULT.html',userTests:'TEST_SPEC.html',forbiddenNotRun:['전체 월드 생성기','과거 적용 도구','정규 통합','Player 빌드','Addressables 빌드','커밋'],retainedSourceBackup:{path:out+'/Source.before.unity',bytes:fs.statSync(out+'/Source.before.unity').size,reason:'미커밋 최신 정규 씬의 원본 기준선 보존. 자동 삭제하지 않음'}};
fs.writeFileSync(`${out}/handoff.json`,JSON.stringify(handoff,null,2));
console.log(JSON.stringify({matchedPoses:true,linkedFiles:links.length,sceneMB:fs.statSync(scene).size/1048576,dataMB:files.reduce((n,p)=>n+fs.statSync(p).size,0)/1048576,artAcceptance:handoff.userArtAcceptance,TPS:handoff.actualTPS}));
