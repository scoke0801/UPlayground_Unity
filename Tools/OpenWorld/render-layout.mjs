import fs from 'node:fs';
const output='output/OpenWorld/Redesign';
const current=JSON.parse(fs.readFileSync('Tools/OpenWorld/city-layout.json','utf8'));
const before=JSON.parse(fs.readFileSync(output+'/Baseline/city-layout.json','utf8'));
const x=v=>50+(v-680)*.8, y=v=>60+(1150-v)*.65;
function render(r,label){
 let s=`<svg xmlns="http://www.w3.org/2000/svg" width="1000" height="1000" viewBox="0 0 1000 1000"><rect width="1000" height="1000" fill="#f4f0e6"/><style>text{font:15px 'Malgun Gothic',sans-serif;fill:#262a26}.small{font-size:12px}.title{font-size:23px;font-weight:bold}</style><text x="40" y="32" class="title">${label}</text>`;
 s+=`<rect x="50" y="60" width="480" height="552" fill="#aeb99b"/><rect x="50" y="612" width="576" height="247" fill="#bac2a5"/>`;
 s+=`<ellipse cx="${x(1067)}" cy="${y(836)}" rx="108" ry="149" fill="#799da0"/><text x="${x(1000)}" y="${y(850)}">호수 수면 77m</text>`;
 const line=(p,color,w,dash='')=>`<polyline points="${p.map(p=>`${x(p.x)},${y(p.z)}`).join(' ')}" fill="none" stroke="${color}" stroke-width="${w}" stroke-linecap="round" stroke-linejoin="round" ${dash?`stroke-dasharray="${dash}"`:''}/>`;
 if(r.landscape){for(const f of r.landscape.landforms){s+=line(f.points,f.id.includes('water')?'#9c927b':'#939981',f.outerWidth*.65);s+=line(f.points,f.id.includes('water')?'#6f999d':'#737c68',3,f.id.includes('water')?'6 6':'');}}
 else{s+=`<path d="M50 612H626 M338 625V859" stroke="#913e39" stroke-width="8" fill="none"/>`;}
 for(const c of r.cities)s+=line(c.road,'#a36d3f',5);
 for(const route of r.routes??[])s+=line(route.points,route.id==='ridge-loop'?'#736240':route.id==='river-bank'?'#387c84':'#a36d3f',route.id==='trade-loop'?5:3,route.id==='trade-loop'?'':'6 4');
 for(const [label,px,pz]of [['여울마을',958,472],['호수 생활권',1040,450],['돌등성 62m',860,120],['소금바람항 50m',1220,140],['신사 / 붉은 나무',800,1080]]){s+=`<circle cx="${x(px)}" cy="${y(pz)}" r="8" fill="#554a40"/><text x="${x(px)+12}" y="${y(pz)+5}">${label}</text>`;}
 if(r.landscape){for(const [label,px,pz]of [['능선 조망',910,345],['하곡 갈림길',1035,350],['마른 여울',1090,270]])s+=`<circle cx="${x(px)}" cy="${y(pz)}" r="5" fill="#f4eee0" stroke="#333"/><text class="small" x="${x(px)+7}" y="${y(pz)-8}">${label}</text>`;}
 s+=`<rect x="${x(1110)}" y="${y(-15)}" width="232" height="42" fill="#799da0"/><text x="${x(1180)}" y="${y(-45)}">해수 45.9m</text>`;
 s+=`<text x="665" y="100">북 ↑ / X 오른쪽, Z 위쪽</text><text x="665" y="140">실선 갈색: 교역로</text><text x="665" y="168">점선 갈색: 능선 우회</text><text x="665" y="196">점선 청록: 하곡 귀환</text><text x="665" y="224">짙은 띠: 능선 / 시야 차폐</text><text x="665" y="252">옅은 면: 보행 후보 영역</text><text x="665" y="280">서쪽 급사면·수면: 접근 불가</text><text x="665" y="322">기존 북쪽 사건 / 주민 / 작업점</text><text x="665" y="346">위치·진행 ID 유지</text><text x="665" y="386">회색·청록 물길은 지형 설계</text><text x="665" y="410">물의 회복은 기존 스토리 관할</text><text x="665" y="450">이 지도는 설계 구조도이며</text><text x="665" y="474">실제 보행 인수 증거가 아님</text><text x="50" y="920">외곽 지형의 급사면은 보행 영역에 포함하지 않는다. 기존 호수 내부 동선은 원본 콘텐츠를 따른다.</text></svg>`;
 return s;
}
fs.mkdirSync(output,{recursive:true});
fs.writeFileSync(output+'/map-before.svg',render(before,'변경 전 — 지역별 진입로와 지형 절개'));
fs.writeFileSync(output+'/map-after.svg',render(current,'재설계 — 능선·하곡·해안의 순환 구조'));
const lengths=[];for(const r of current.routes??[]){let length=0;for(let i=1;i<r.points.length;i++){const a=r.points[i-1],b=r.points[i];length+=Math.hypot(a.x-b.x,a.y-b.y,a.z-b.z);}lengths.push({id:r.id,lengthMeters:+length.toFixed(1),experience:r.experience});}
fs.writeFileSync(output+'/route-lengths.json',JSON.stringify(lengths,null,2));
