// sample-encounter-ai.csx가 남긴 Play Mode 표본을 조우별 지표로 요약한다.
// 사용: node Tools/OpenWorld/summarize-encounter-sample.mjs [입력 tsv]
import fs from 'node:fs';
const input=process.argv[2]??'output/OpenWorld/MonsterPopulation/Improvement/play-sample.tsv';
const rows=fs.readFileSync(input,'utf8').split(/\r?\n/).filter(l=>l&&!l.startsWith('#')&&!l.startsWith('scenario\t')).map(l=>{
  const [scenario,t,member,id,state,tree,intent,slot,ability,ring,dist,drift]=l.split('\t');
  return {scenario,t:+t,member:+member,id,state,tree,intent,slot:slot==='True',ability,ring:ring==='True',dist:+dist,drift:+drift};
});
const pct=(n,d)=>d?Math.round(n*100/d)+'%':'-';
const out=[];
for(const scenario of [...new Set(rows.map(r=>r.scenario))]){
  const s=rows.filter(r=>r.scenario===scenario);
  const members=[...new Set(s.map(r=>r.member))];
  const times=[...new Set(s.map(r=>r.t))];
  out.push(`## ${scenario} (${members.length}명, 표본 ${times.length}회)`);
  out.push(`BT: ${[...new Set(s.map(r=>r.tree))].join(', ')}`);
  const states={};for(const r of s)states[r.state]=(states[r.state]??0)+1;
  out.push('상태 비율: '+Object.entries(states).sort((a,b)=>b[1]-a[1]).map(([k,v])=>`${k} ${pct(v,s.length)}`).join(' · '));
  // 공격 시작 = 같은 멤버가 Attack이 아니었다가 Attack이 된 표본
  const starts=[];
  for(const m of members){let prev='';for(const r of s.filter(x=>x.member===m)){if(r.state==='Attack'&&prev!=='Attack')starts.push(r);prev=r.state;}}
  const byAbility={};for(const r of starts){const k=(r.ability||'(미상)').replace(/^Actor\./,'');byAbility[k]=(byAbility[k]??0)+1;}
  out.push(`공격 시작 ${starts.length}회 / 위험 링 ${starts.filter(r=>r.ring).length}회: `+Object.entries(byAbility).map(([k,v])=>`${k}×${v}`).join(', '));
  const concurrent=times.filter(t=>s.filter(r=>r.t===t&&r.state==='Attack').length>=2).length;
  if(members.length>1)out.push(`동시 공격(2명 이상) 표본: ${pct(concurrent,times.length)}`);
  for(const m of members){
    const ms=s.filter(r=>r.member===m);
    const idle=ms.filter(r=>r.state==='Idle').length;
    const maxDrift=Math.max(...ms.map(r=>r.drift));
    const meanDist=ms.reduce((a,r)=>a+r.dist,0)/ms.length;
    out.push(`  #${m} ${ms[0].id}: 공격 ${pct(ms.filter(r=>r.state==='Attack').length,ms.length)} · 대기(Idle) ${pct(idle,ms.length)} · 평균 거리 ${meanDist.toFixed(1)}m · 스폰 이탈 최대 ${maxDrift.toFixed(1)}m · 의도 ${[...new Set(ms.map(r=>r.intent).filter(Boolean))].join('/')}`);
  }
  out.push('');
}
const text=out.join('\n');
fs.writeFileSync(input.replace(/\.tsv$/,'-summary.txt'),text);
console.log(text);
