import { test } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import { canonical, hash, validate, compile, discover, sourceFile } from './harness.mjs';

// 실제 문안과 혼동하지 않는 합성 테스트. 원문 자료도 이 파일 하나로 한정한다.
function fixture() {
  const source = { source_id: 'test', source_path: 'Tools/NpcWorldHarness/harness.test.mjs',
    authority: 'fixture', sha256: hash(fs.readFileSync(sourceFile('Tools/NpcWorldHarness/harness.test.mjs'))) };
  const condition = { op: 'eq', field: 'quest.test.status', value: 'completed' };
  const slot = { slot_id: 'test.slot', status: 'approved', surface: 'ambient_bark', speaker_id: 'test.npc',
    listener_id: '', intent: 'test', topic_id: 'test', conditions: condition, source_refs: ['test'], fact_refs: ['test.fact'] };
  const snapshot = { schema_version: '1.0.0', fixture_only: true, sources: [source],
    facts: [{ fact_id: 'test.fact', source_refs: ['test'], disclosure: 'public', valid_when: condition }],
    npcs: [{ npc_id: 'test.npc', knowledge_fact_ids: ['test.fact'] }], slots: [slot],
    condition_fields: { 'quest.test.status': ['active', 'completed'] },
    policy: { max_lines: 2, max_characters: 54, max_turns: 6 } };
  const entry = { ...slot, id: 'test.line', lines: ['합성 검증 문장'], review_status: 'draft', locked: false,
    human_edited: false, selection: { weight: 1, cooldown_seconds: 120, repeat_group: 'test' } };
  delete entry.status;
  const pack = { schema_version: '1.0.0', fixture_only: true, pack_id: 'test.pack', locale: 'ko-KR',
    entries: [entry], conversations: [] };
  return { snapshot, pack };
}

test('합성 후보 검증', () => {
  const { snapshot, pack } = fixture();
  assert.equal(validate(snapshot, pack).status, 'PASS');
});

for (const [name, mutate] of [
  ['NPC 지식 누설', (s) => { s.npcs[0].knowledge_fact_ids = []; }],
  ['공개 권한 누설', (s) => { s.facts[0].disclosure = 'restricted'; }],
  ['미등록 퀘스트 상태', (s, p) => { p.entries[0].conditions.value = 'invented'; }],
  ['공개 시점 누락', (s, p) => { s.slots[0].conditions = p.entries[0].conditions = { op: 'true' }; }],
  ['출처 변경', (s) => { s.sources[0].sha256 = '0'.repeat(64); }],
  ['출처 누락', (s, p) => { p.entries[0].source_refs = []; }],
  ['중복 ID', (_s, p) => { p.entries.push(structuredClone(p.entries[0])); }],
  ['슬롯 변조', (_s, p) => { p.entries[0].intent = 'invent'; }],
  ['미승인 슬롯', (s) => { s.slots[0].status = 'draft'; }],
  ['빈 문장', (_s, p) => { p.entries[0].lines = ['']; }],
  ['잘못된 조건 자식', (s, p) => { s.slots[0].conditions = p.entries[0].conditions = { op: 'not', children: [] }; }],
]) test(name + ' 차단', () => {
  const { snapshot, pack } = fixture(); mutate(snapshot, pack);
  assert.throws(() => validate(snapshot, pack));
});

for (const protection of ['locked', 'human_edited', 'approved']) test(protection + ' 변경/삭제 차단', () => {
  const { snapshot, pack } = fixture();
  if (protection === 'approved') pack.entries[0].review_status = 'approved';
  else pack.entries[0][protection] = true;
  const old = structuredClone(pack);
  pack.entries[0].lines = ['다른 합성 문장'];
  assert.throws(() => validate(snapshot, pack, old), /LOCK_CONFLICT/);
  pack.entries = [];
  assert.throws(() => validate(snapshot, pack, old), /LOCK_CONFLICT/);
});

test('픽스처 배포와 승인 해시 불일치 차단', () => {
  const { snapshot, pack } = fixture();
  assert.throws(() => compile(snapshot, pack, {}), /픽스처/);
  snapshot.fixture_only = pack.fixture_only = false;
  snapshot.sources[0].authority = 'canonical';
  pack.entries[0].review_status = 'approved';
  assert.throws(() => compile(snapshot, pack, { decision: 'approved' }), /승인 기록/);
  const report = validate(snapshot, pack);
  const review = { decision: 'approved', reviewer: 'test-only', reviewed_at: '2026-11-11',
    pack_sha256: report.pack_sha256, snapshot_sha256: report.snapshot_sha256 };
  assert.equal(canonical(compile(snapshot, pack, review)), canonical(compile(snapshot, pack, review)));
  pack.entries[0].lines = ['변경된 합성 문장'];
  assert.throws(() => compile(snapshot, pack, review), /승인 기록/);
});

test('실제 허용 파일 조사 및 프로젝트 밖 읽기 차단', () => {
  const config = JSON.parse(fs.readFileSync(sourceFile('Tools/NpcWorldHarness/sources.json'), 'utf8'));
  const report = discover(config);
  assert.equal(report.sources.length, config.sources.length);
  assert.equal(report.status, 'BLOCKED_MISSING_SOURCE');
  assert.throws(() => sourceFile('../outside'));
});

test('대화 참여자·지식·중단 정책 검사', () => {
  const { snapshot, pack } = fixture();
  snapshot.npcs.push({ npc_id: 'test.listener', knowledge_fact_ids: ['test.fact'] });
  const slot = snapshot.slots[0];
  slot.surface = 'npc_conversation'; slot.speaker_ids = ['test.npc', 'test.listener'];
  const entry = { ...pack.entries[0], ...slot, turns: [
    { speaker_id: 'test.npc', speech_act: 'ask', line: '합성 질문' },
    { speaker_id: 'test.listener', speech_act: 'answer', line: '합성 답변' },
  ], interrupt_policy: 'stop_and_reset' };
  pack.entries = []; pack.conversations = [entry];
  assert.equal(validate(snapshot, pack).conversations, 1);
  snapshot.npcs[1].knowledge_fact_ids = [];
  assert.throws(() => validate(snapshot, pack), /지식/);
  snapshot.npcs[1].knowledge_fact_ids = ['test.fact'];
  entry.interrupt_policy = 'unknown';
  assert.throws(() => validate(snapshot, pack), /중단 정책/);
});

test('새 후보가 이전 ID의 의미를 재사용하면 차단', () => {
  const { snapshot, pack } = fixture();
  const previous = structuredClone(pack);
  snapshot.slots[0].slot_id = pack.entries[0].slot_id = 'new.slot';
  assert.throws(() => validate(snapshot, pack, previous), /의미 재사용/);
});
