import fs from 'node:fs';
import assert from 'node:assert/strict';

const survey = JSON.parse(fs.readFileSync('output/RegionalSky/Followup/scene-survey.json', 'utf8'));
const sky = fs.readFileSync('Assets/02.Scripts/Gameplay/World/SceneEnvironmentRendering.Sky.cs', 'utf8');
const base = fs.readFileSync('Assets/02.Scripts/Gameplay/World/SceneEnvironmentRendering.cs', 'utf8');
const shader = fs.readFileSync('Assets/06.Shaders/DaySky.shader', 'utf8');
const apply = fs.readFileSync('Tools/RegionalSky/apply-lake-regions.csx', 'utf8');
for (const match of apply.matchAll(/data\.FindProperty\("([^"]+)"\)/g))
    assert.ok((sky + base).includes(` ${match[1]}`), `적용 스크립트 필드 불일치: ${match[1]}`);
for (const match of sky.matchAll(/Shader\.PropertyToID\("([^"]+)"\)/g))
    assert.ok(shader.includes(match[1] + '('), `셰이더 프로퍼티 누락: ${match[1]}`);
assert.equal(survey.rows.filter(r => r.text.includes('aa71b2344a03951498765f2edbe96bf2')).length, 1);
const water = survey.rows.find(r => r.type === '23' && r.name === 'LakeWater');
assert.ok(water.text.includes('5e3df2ad06fdf8044ac6f6bd290582d5'));
assert.ok(survey.rows.find(r => r.type === '104').text.includes('m_CustomReflection: {fileID: 0}'));
let integration = '적용 전';
if (fs.existsSync('output/RegionalSky/Followup/integration-applied.json')) {
    const diff = JSON.parse(fs.readFileSync('output/RegionalSky/Followup/integration-scene-diff.json', 'utf8'));
    assert.deepEqual(diff.removed, []);
    assert.equal(diff.added.length, 6);
    const expected = new Set(['453226482', '874872977', '879054241', '1474497645', '9223372036854775807']);
    assert.equal(diff.changed.length, expected.size);
    for (const doc of diff.changed) assert.ok(expected.has(doc.id), `범위 밖 씬 변경: ${doc.id}`);
    integration = '기존 문서5개 변경·신규6개·삭제0';
} else assert.equal(survey.rows.filter(r => r.type === '215').length, 0);
const result = { status: '통과', scope: '씬 직렬화·적용 필드·셰이더 프로퍼티 정적 보조 검사', integration,
    unityAssetLoad: 'unity-binding-check.txt 별도 확인', baselineSceneSha256: survey.sha256 };
fs.writeFileSync('output/RegionalSky/Followup/static-check.json', JSON.stringify(result, null, 2));
console.log(JSON.stringify(result));
