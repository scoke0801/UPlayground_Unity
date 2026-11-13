import fs from 'node:fs';
const path='Assets/docs/design/world-art/terrain-theme/WORLD_THEME_TERRAIN_PRODUCTION_DESIGN.html';
const text=fs.readFileSync(path,'utf8').replace(/<style[\s\S]*?<\/style>/g,'').replace(/<script[\s\S]*?<\/script>/g,'').replace(/<img[^>]*>/g,' [reference image] ').replace(/<[^>]*>/g,' ').replace(/&[^;]+;/g,' ').replace(/\s+/g,' ');
const start=Number(process.argv[2]??0),length=Number(process.argv[3]??12000);
console.log(`Design total characters=${text.length}, range=${start}..${start+length}`);
console.log(text.slice(start,start+length));
