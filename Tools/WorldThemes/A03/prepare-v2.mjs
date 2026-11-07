import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';

const out = 'output/WorldThemeProduction/VillageFields';
const dest = `${out}/revision-v1.1`;
assert(!fs.existsSync(dest), '기존 v1.1 검수 기록을 덮어쓰지 않는다.');
const read = p => JSON.parse(fs.readFileSync(p, 'utf8').replace(/^\uFEFF/, ''));
const hash = p => crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const handoff = read(`${out}/handoff.json`);
assert.equal(handoff.version, 'VillageFields-v1.1');
fs.mkdirSync(dest, { recursive: true });
const files = [];
const copy = (source, target) => {
  assert(fs.lstatSync(source).isFile(), `일반 파일만 보존: ${source}`);
  fs.mkdirSync(path.dirname(target), { recursive: true });
  fs.copyFileSync(source, target);
  const sha256 = hash(source);
  assert.equal(hash(target), sha256);
  files.push({ source, archived: target, sha256, bytes: fs.statSync(target).size });
};
for (const f of handoff.ownedFiles) {
  assert.equal(hash(f.path), f.sha256, `v1.1 에셋이 인계 후 바뀜: ${f.path}`);
  copy(f.path, `${dest}/assets/${f.path}`);
}
for (const file of ['handoff.json', 'handoff.md', 'comparison.html', 'validation.json', 'applied.json', 'scaffold-report.json', 'candidate-inventory.json', 'candidate-selection.json', 'capture-poses.json', 'asset-review.md', 'user-test-spec.md', 'detail-prototype-map.json', 'tree-prototype-map.json', 'eligible-grid.csv']) {
  copy(`${out}/${file}`, `${dest}/${file}`);
}
for (const stage of ['before', 'after']) for (const file of fs.readdirSync(`${out}/captures/${stage}`)) copy(`${out}/captures/${stage}/${file}`, `${dest}/captures/${stage}/${file}`);
for (const file of fs.readdirSync('Tools/WorldThemes/A03').filter(f => /\.(csx|mjs|json)$/.test(f) && f !== 'prepare-v2.mjs')) copy(`Tools/WorldThemes/A03/${file}`, `${dest}/tools/${file}`);
fs.writeFileSync(`${dest}/archive-manifest.json`, JSON.stringify({ version: handoff.version, reason: '독립 씬 v2 보완 전 실제 v1.1 에셋·사진·인계·재현 입력을 보존', files }, null, 2));
fs.writeFileSync(`${dest}/README.md`, '# VillageFields-v1.1 보존본\n\nA00의 v2 확장 작업 전에 독립 제작 에셋/.meta·실제 사진·인계·해시·재현 입력을 함께 보존했다. `comparison.html`은 이 폴더 안의 v1.1 사진만 참조한다. `assets/Assets/`는 원래 경로 구조의 복구용 사본이며 Unity가 임포트하는 Assets 폴더가 아니다. 본편 원본은 변경하지 않았다.\n');
console.log(`v1.1 archived: ${files.length} files, ${files.reduce((n, f) => n + f.bytes, 0)} bytes; source files preserved`);
