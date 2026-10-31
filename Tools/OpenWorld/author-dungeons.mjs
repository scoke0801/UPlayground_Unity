import fs from 'node:fs';
import assert from 'node:assert/strict';

// 원본 조립 공간의 좌표를 유지한다. 회전·스케일 변형으로 장소 차이를 만들지 않는다.
export function authorDungeons(r) {
  const L=r.landscape, vec=(x,y,z)=>({x,y,z});
  const sites=[
    {id:'SunkenHall',name:'묻힌 등불전',position:vec(330,105,565),experience:'비탈의 석문 → 낮은 전실 → 기둥 대청과 양쪽 단 → 뒤쪽 유물 → 등불이 있는 입구로 귀환',points:[[330,104,520],[330,104,535],[330,104,548],[325,105,560],[318,107,570],[330,104,585]]},
    {id:'ChainTemple',name:'사슬 제단',position:vec(600,140,860),experience:'산허리의 사슬 기둥 → 석교 → 원형 계단 양쪽 선택 → 상부 조망 → 서쪽 석교 보상 → 남쪽 교량 귀환',points:[[590,140,834],[590,140,850],[585,140,860],[591.2,144.45,859.9],[590,140,870],[580,140,860]]},
    {id:'BriarRing',name:'가시울 옛터',position:vec(1900,102,868),experience:'숲 사이 목책 → 서쪽 틈 → 낮은 외곽과 갈라진 경사로 → 중앙 돌원 → 동쪽 작업 흔적 → 서쪽 숲길 귀환',points:[[1850,110,912],[1870,110,912],[1900,110,883],[1900,110.5,898],[1930,110,910],[1900,110,928]]}
  ];
  r.dungeons=sites;
  const cliff=L.props.find(p=>p.id==='Expansion_cliff_768');assert(cliff);cliff.position=vec(265,0,580);
  L.props=L.props.filter(p=>!p.id.startsWith('Dungeon_'));
  // 조립 맵의 원래 수목과 월드 군락이 겹쳐 길을 막지 않도록 이식 footprint 안의 장식 식생만 걷는다.
  L.props=L.props.filter(p=>!(p.position.x>=1836&&p.position.x<=1964&&p.position.z>=836&&p.position.z<=964&&/Foliage/.test(p.prefab)));
  for(const site of sites)L.props.push({id:'Dungeon_'+site.id,prefab:`Assets/03.Prefabs/Environment/OpenWorld/Dungeons/${site.id}.prefab`,position:site.position,scale:vec(1,1,1),yaw:0,useAbsoluteHeight:true});
  L.dungeonGround=[
    {id:'hall-earth-roof',bounds:{x:305,y:537,width:50,height:61},height:125.6,transition:22},
    {id:'hall-entry-cut',bounds:{x:316,y:513,width:28,height:25},height:103.8,transition:8},
    {id:'hall-door-opening',bounds:{x:324,y:534,width:12,height:14},isHole:true,transition:0},
    {id:'temple-footing',bounds:{x:559,y:829,width:63,height:63},height:139.6,transition:18},
    {id:'ring-original-relief',bounds:{x:1836,y:836,width:128,height:128},height:102,transition:28,sourceTerrain:'Assets/ExternalAssets/Environment/LowPolyFantasyArena2/Terrain/LowPolyFantasyArena2.asset'},
    {id:'ring-approach-grade',bounds:{x:1780,y:906,width:55,height:34},height:94.5,endHeight:110,hasSlope:true,transition:16},
    {id:'ring-gate-grade',bounds:{x:1835,y:906,width:39,height:12},height:110,transition:24}
  ];
  const routes=[
    {id:'dungeon-hall-approach',points:[[420,111,580],[420,105,520],[390,103,490],[355,104,490],[330,104,500],[330,104,520]],experience:'고갯길에서 남쪽 비탈을 돌아 접근 → 마지막 굽이에서 석문 정면 발견'},
    {id:'dungeon-temple-approach',points:[[550,145,840],[565,142,820],[590,140,824],[590,140,840]],experience:'고갯길 굽이에서 사슬 기둥을 보고 석교로 접근'},
    {id:'dungeon-ring-approach',points:[[1770,94,910],[1805,99,932],[1820,102,928],[1835,106,918],[1850,110,912],[1870,110,912]],experience:'그늘숲 북쪽 길에서 낮은 목책 틈으로 접근'}
  ];
  r.routes=r.routes.filter(q=>!q.id.startsWith('dungeon-'));
  for(const q of routes)r.routes.push({...q,points:q.points.map(p=>vec(...p)),halfWidth:2.8,followTerrain:true});
  const distance=(p,q)=>{let result=Infinity;for(let i=1;i<q.points.length;i++){const a=q.points[i-1],b=q.points[i],dx=b.x-a.x,dz=b.z-a.z,t=Math.max(0,Math.min(1,((p.x-a.x)*dx+(p.z-a.z)*dz)/(dx*dx+dz*dz)));result=Math.min(result,Math.hypot(p.x-a.x-t*dx,p.z-a.z-t*dz));}return result;};
  L.props=L.props.filter(p=>!/Foliage/.test(p.prefab)||r.routes.filter(q=>q.id.startsWith('dungeon-')).every(q=>distance(p.position,q)>8));
  L.pickups=L.pickups.filter(p=>!['c42bb9f480ec4f3cacdf786ab0a59e31','c42bb9f480ec4f3cacdf786ab0a59e32','c42bb9f480ec4f3cacdf786ab0a59e33'].includes(p.id));
  const original=L.pickups.find(p=>p.id==='c42bb9f480ec4f3cacdf786ab0a59e02')||L.pickups[0];assert(original);
  for(const [i,p] of [[0,[330,104.1,585]],[1,[580,140.1,860]],[2,[1930,110.1,910]]])L.pickups.push({...original,id:`c42bb9f480ec4f3cacdf786ab0a59e3${i+1}`,position:vec(...p),count:1,useAbsoluteHeight:true});
  return r;
}
if(process.argv[1]?.endsWith('author-dungeons.mjs')){const path=process.argv[2]||'Tools/OpenWorld/city-layout.json';const r=JSON.parse(fs.readFileSync(path,'utf8'));authorDungeons(r);fs.writeFileSync(path,JSON.stringify(r,null,2)+'\n');}
