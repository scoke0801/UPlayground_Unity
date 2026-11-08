// 일회성 제작표 준비. Unity 에셋 생성은 HumanoidGradeAuthoring의 Preview/Apply가 담당한다.
import fs from 'node:fs';
const root = 'Assets/10.Datas/Ability/Actor';
const ownerRows = fs.readFileSync('Logs/HumanoidGrade/owners.before.txt', 'utf8').split(/\r?\n/);
const paths = [];
function walk(dir) { for (const e of fs.readdirSync(dir, {withFileTypes:true})) { const p = dir+'/'+e.name; if(e.isDirectory()) walk(p); else if(p.endsWith('.asset') && /^  monsterProfile: /m.test(fs.readFileSync(p,'utf8'))) paths.push(p); } }
walk('Assets/10.Datas/Actor');
const actorIds=new Map(paths.map(p=>[p,fs.readFileSync(p,'utf8').match(/^  actorId: (.+)$/m)[1].trim()]));
const recipes = [];
function weapon(key, initial, unlock, signature, late, bossDamage) {
  const folder = `${root}/Humanoid_${key}AttackData`;
  const files = fs.readdirSync(folder).filter(f=>f.startsWith('GA_')&&f.endsWith('.asset'));
  const source = n => folder+'/'+files.find(f=>Number(f.match(/Data_(\d+)_/)[1])===n);
  const attacks = initial.map(n=>({source:source(n),grade:0,level:1}));
  for(const [index,n,roles,category,min] of unlock) attacks.push({source:source(n),grade:0,level:index,roles,category,minimumStartup:min||0,minimumRecovery:min?0.3:0});
  attacks.push({source:source(signature),grade:1,level:1});
  if(bossDamage) attacks.push({source:source(signature),grade:2,level:1,replace:true,damage:bossDamage,minimumStartup:key==='DualBlade'?0.75:0.8,minimumRecovery:0.3});
  if(late) attacks.push({source:source(late[0]),grade:2,level:30,roles:'Signature',category:'Skill',latePhase:true,minimumStartup:late[1],minimumRecovery:0.3,cooldown:14});
  const ids=ownerRows.filter(l=>(l.includes(`| AbilitySet_Humanoid_${key}AttackData |`)&&!(key==='SwordShield'&&l.split(' | ')[2].includes('GreatSword')))||(key==='GreatSword'&&l.split(' | ')[2]?.includes('GreatSword')&&l.includes('SwordShieldAttackData'))).map(l=>l.split(' | ')[0]);
  const definitions=paths.filter(p=>ids.includes(actorIds.get(p))).filter(p=>!['MonsterSera','MonsterNenmir'].includes(actorIds.get(p)));
  recipes.push({key,sourceSet:`${folder}/AbilitySet_Humanoid_${key}AttackData.asset`,attacks,definitions});
}
weapon('GreatSword',[1,6,9,12,14,19,21],[[10,2,'','',0],[20,10,'','',0],[30,17,'Punish','Heavy',0.6]],20,[18,0.8],[24]);
weapon('DoubleAxe',[1,5,6,11,12,16,18],[[10,2,'','',0],[20,7,'','',0],[30,15,'Punish','Heavy',0.6]],17,[14,0.6],null);
weapon('DualBlade',[1,6,8,15,16,21,23],[[10,2,'','',0],[20,20,'','',0],[30,17,'Punish','Heavy',0.75]],22,null,[7,7,8,8,10]);
weapon('SwordShield',[1,5,8,12,14,20,24],[[10,2,'','',0],[20,9,'','',0],[30,16,'Punish','Heavy',0.6]],23,[22,0.8],[30]);
weapon('Bow',[1,3,4,6,9,11,12],[[10,2,'','',0],[20,5,'','',0],[30,10,'','',0.5]],8,[7,0.6],[25]);
// 카타나·채찍은 공용 데이터에 역할이 없다. 원본 대신 역할용 Fork만 등급 Set에 연결한다.
function specialized(key, entries, signature, late) {
  const folder=`${root}/Humanoid_${key}AttackData`;
  const files=fs.readdirSync(folder).filter(f=>f.startsWith('GA_')&&f.endsWith('.asset'));
  const source=n=>folder+'/'+files.find(f=>Number(f.match(/Data_(\d+)_/)[1])===n);
  const attacks=entries.map(([n,roles,category,level=1,min=0])=>({source:source(n),grade:0,level,roles,category,minimumStartup:min,minimumRecovery:min?0.3:0}));
  attacks.push({source:source(signature),grade:1,level:1,roles:'Signature',category:'Skill'});
  if(late) attacks.push({source:source(late),grade:2,level:30,roles:'Signature',category:'Skill',latePhase:true,minimumStartup:0.6,minimumRecovery:0.3,cooldown:14});
  if(key==='Whip') attacks.push({source:source(3),grade:2,level:20,roles:'Finisher',category:'Basic'});
  const ids=ownerRows.filter(l=>l.includes(`| AbilitySet_Humanoid_${key}AttackData |`)).map(l=>l.split(' | ')[0]);
  recipes.push({key,sourceSet:`${folder}/AbilitySet_Humanoid_${key}AttackData.asset`,attacks,definitions:paths.filter(p=>ids.includes(actorIds.get(p)))});
}
specialized('Katana',[[1,'Opener','Basic'],[6,'Finisher','Basic'],[11,'Punish','Heavy'],[33,'Counter','Skill'],[26,'GapCloser','Skill'],[2,'Opener','Basic',10],[9,'Finisher','Basic',20]],27,28);
// 채찍에는 0.6초 준비 구간의 강공격 콘텐츠가 없으므로 Lv30 강공격 추가는 보류한다.
specialized('Whip',[[1,'Opener','Basic'],[4,'Finisher','Basic'],[8,'Punish','Heavy'],[15,'Counter','Skill'],[12,'GapCloser','Skill'],[2,'Opener','Basic',10],[6,'Finisher','Basic',20]],14,null);
const icons={GreatSword:'Assets/04.Images/EquipmentIcons/GreatSword_001.png',DoubleAxe:'Assets/04.Images/UI/Portrait/MonsterCodex/MonsterCodex_Enemy_Random_M_DualAxe_001_2a7247bf.png',DualBlade:'Assets/04.Images/UI/WeaponIcon/dualBladeWeaponIcon.png',SwordShield:'Assets/04.Images/EquipmentIcons/Shield_001.png',Bow:'Assets/04.Images/EquipmentIcons/Bow_001.png',Katana:'Assets/04.Images/UI/WeaponIcon/KatanaWeaponIcon.png',Whip:'Assets/04.Images/UI/WeaponIcon/WhipWeaponIcon.png'};
for(const recipe of recipes)recipe.iconPath=icons[recipe.key];
for(const recipe of recipes)for(const attack of recipe.attacks)if(attack.grade===0&&attack.level===30&&attack.category==='Heavy'){attack.cooldown=6;attack.cooldownGroup=`Humanoid.${recipe.key}.Heavy`;}
fs.writeFileSync(`${root}/HumanoidGradeVariation.json`,JSON.stringify({weapons:recipes},null,2)+'\n');
console.log(recipes.map(w=>`${w.key}: ${w.definitions.length} owners, ${w.attacks.length} entries`).join('\n'));
