// 저장된 캡처만 나란히 표시한다. 씬과 저작 기준은 변경하지 않는다.
import fs from 'node:fs';
const names=['quarry-face','old-pass','quarry-yard','crypt','fort','waystation','harbor-quay','harbor-lighthouse','citadel-market','citadel-workshop','citadel-gate'];
const labels=['폐채석장 절개면','옛 고갯길','채석장 작업마당','묘역 참배로','보루 전실','역참 숙영 회랑','항구 안벽','등대 방파제','돌등성 장터','돌등성 공방','돌등성 성문'];
const folder='output/OpenWorld/EnvironmentFollowup';
for(const n of names)for(const stage of ['before','after'])if(!fs.existsSync(`${folder}/${stage}/${n}.png`))throw Error(`캡처 없음 ${stage}/${n}`);
fs.writeFileSync(folder+'/compare.html',`<!doctype html><html lang="ko"><meta charset="utf-8"><title>월드 환경 후속 개선 전후</title><style>body{font:16px system-ui;background:#202425;color:#eee;margin:24px}section{margin:36px 0}h1{font-size:24px}h2{font-size:19px}figure{margin:0}img{width:100%;display:block}.pair{display:grid;grid-template-columns:1fr 1fr;gap:12px}figcaption{padding:8px 0}@media(max-width:800px){.pair{grid-template-columns:1fr}}</style><h1>월드 환경 후속 개선 전후</h1><p>동일한 플레이어 눈높이(1.7m), 시점·화각의 Editor 정지 캡처. 실제 이동·전투 검수는 TEST_SPEC.md 참조.</p><p>사면 형태·암반·기존 소품·바닥 접합의 부분 보강이다. 넓은 사면의 반복 무늬와 보루·역참의 장소 구분은 후속 저작이 남았다.</p>${names.map((n,i)=>`<section><h2>${labels[i]}</h2><div class="pair"><figure><img src="before/${n}.png"><figcaption>작업 직전</figcaption></figure><figure><img src="after/${n}.png"><figcaption>저장한 변경</figcaption></figure></div></section>`).join('')}</html>`);
console.log('전후 캡처 11쌍 비교 페이지 생성');
