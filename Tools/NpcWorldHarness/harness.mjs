import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';

export const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
export const hash = value => crypto.createHash('sha256').update(value).digest('hex');
export const canonical = value => JSON.stringify(value, (_key, item) => item && typeof item === 'object' && !Array.isArray(item)
  ? Object.fromEntries(Object.keys(item).sort().map(key => [key, item[key]])) : item);
const requireValue = (condition, message) => { if (!condition) throw new Error(message); };
const isText = value => typeof value === 'string' && value.trim().length > 0;

/** 허용한 프로젝트 파일만 읽고 링크를 통한 경계 이탈도 차단한다. */
export function sourceFile(relative) {
  requireValue(isText(relative) && !path.isAbsolute(relative), '상대 경로가 필요합니다.');
  const target = fs.realpathSync(path.resolve(root, relative));
  const inside = path.relative(fs.realpathSync(root), target);
  requireValue(inside && !inside.startsWith('..') && !path.isAbsolute(inside), `프로젝트 밖 경로: ${relative}`);
  requireValue(fs.statSync(target).isFile(), `파일이 아닙니다: ${relative}`);
  return target;
}

/** 별도 허용 목록을 조사하며 원본 데이터의 의미를 추측하지 않는다. */
export function discover(config) {
  return { schema_version: '1.0.0', sources: config.sources.map(source => ({ ...source,
    sha256: hash(fs.readFileSync(sourceFile(source.source_path))) })),
    blockers: config.blockers, status: config.blockers.length ? 'BLOCKED_MISSING_SOURCE' : 'ready' };
}

function index(items, key) {
  requireValue(Array.isArray(items), `${key} 목록이 필요합니다.`);
  const result = new Map();
  for (const item of items) {
    requireValue(item && isText(item[key]) && !result.has(item[key]), `누락 또는 중복 ID: ${item?.[key]}`);
    result.set(item[key], item);
  }
  return result;
}

/** 조건은 허용 값 레지스트리만 사용하며 동적 코드 실행을 허용하지 않는다. */
export function validateCondition(node, fields, depth = 0) {
  requireValue(node && typeof node === 'object' && depth < 24, '잘못되거나 너무 깊은 조건 AST');
  const keys = allowed => requireValue(Object.keys(node).every(key => allowed.includes(key)), '알 수 없는 조건 속성');
  if (['all', 'any', 'not'].includes(node.op)) {
    keys(['op', 'children']);
    requireValue(Array.isArray(node.children) && node.children.length > 0
      && (node.op !== 'not' || node.children.length === 1), '잘못된 조건 자식');
    for (const child of node.children) validateCondition(child, fields, depth + 1);
  } else if (node.op === 'true') keys(['op']);
  else {
    requireValue(['eq', 'in'].includes(node.op), `허용되지 않은 조건: ${node.op}`);
    keys(node.op === 'eq' ? ['op', 'field', 'value'] : ['op', 'field', 'values']);
    const values = node.op === 'eq' ? [node.value] : node.values;
    requireValue(Object.hasOwn(fields, node.field) && Array.isArray(values) && values.length > 0
      && values.every(value => fields[node.field].includes(value)), `미등록 조건 필드/값: ${node.field}`);
  }
}

const same = (a, b) => canonical(a) === canonical(b);
const includesAll = (a, b) => Array.isArray(a) && b.every(value => a.includes(value));

/** 후보의 구조·출처·지식·계획 불변성을 검사한다. 문장 의미와 자연스러움은 사람 검수 대상이다. */
export function validate(snapshot, pack, previous = null) {
  requireValue(snapshot.schema_version === '1.0.0' && pack.schema_version === '1.0.0', '지원하지 않는 스키마');
  requireValue(typeof snapshot.fixture_only === 'boolean' && pack.fixture_only === snapshot.fixture_only, '픽스처 경계 불일치');
  requireValue(isText(pack.pack_id) && isText(pack.locale), '팩 ID/로케일 누락');
  const sources = index(snapshot.sources, 'source_id');
  const facts = index(snapshot.facts, 'fact_id');
  const npcs = index(snapshot.npcs, 'npc_id');
  const slots = index(snapshot.slots, 'slot_id');
  const fields = snapshot.condition_fields;
  requireValue(fields && Object.values(fields).every(Array.isArray), '조건 값 레지스트리 누락');
  requireValue(snapshot.policy && ['max_lines', 'max_characters', 'max_turns'].every(key =>
    Number.isInteger(snapshot.policy[key]) && snapshot.policy[key] > 0), '저작 정책 누락/범위 오류');
  for (const source of sources.values()) {
    requireValue(/^[a-f0-9]{64}$/.test(source.sha256), '잘못된 출처 해시');
    requireValue(hash(fs.readFileSync(sourceFile(source.source_path))) === source.sha256, `출처 변경: ${source.source_id}`);
    requireValue(snapshot.fixture_only || source.authority === 'canonical', '정식 자료에 픽스처/미확정 출처 사용');
  }
  for (const fact of facts.values()) {
    requireValue(Array.isArray(fact.source_refs) && fact.source_refs.length > 0
      && fact.source_refs.every(id => sources.has(id)), `팩트 출처 누락: ${fact.fact_id}`);
    requireValue(['public', 'restricted'].includes(fact.disclosure), '공개 범위 미확정');
    validateCondition(fact.valid_when, fields);
  }
  requireValue(Array.isArray(pack.entries) && Array.isArray(pack.conversations), '엔트리/대화 목록 누락');
  const records = [...pack.entries, ...pack.conversations];
  index(records, 'id');
  for (const entry of records) {
    const slot = slots.get(entry.slot_id);
    requireValue(slot?.status === 'approved', `승인된 슬롯 없음: ${entry.slot_id}`);
    for (const key of ['surface', 'intent', 'topic_id', 'conditions', 'source_refs', 'fact_refs'])
      requireValue(same(entry[key], slot[key]), `슬롯 변조: ${entry.id}/${key}`);
    validateCondition(entry.conditions, fields);
    requireValue(Array.isArray(entry.fact_refs) && entry.fact_refs.length > 0, '발화 근거 누락');
    requireValue(Array.isArray(entry.source_refs) && entry.source_refs.every(id => sources.has(id)), '알 수 없는 출처');
    requireValue(['draft', 'needs_review', 'approved', 'rejected'].includes(entry.review_status)
      && typeof entry.locked === 'boolean' && typeof entry.human_edited === 'boolean', '검수/보호 상태 누락');
    let speakers;
    let lines;
    if (pack.entries.includes(entry)) {
      requireValue(['ambient_bark', 'greeting'].includes(entry.surface), '발화 형태 오류');
      requireValue(entry.speaker_id === slot.speaker_id && entry.listener_id === slot.listener_id, '화자/청자 변조');
      requireValue(entry.listener_id === '' || npcs.has(entry.listener_id), '알 수 없는 청자');
      speakers = [entry.speaker_id]; lines = entry.lines;
      requireValue(lines?.length >= 1 && lines.length <= snapshot.policy.max_lines, '발화 줄 수 오류');
      requireValue(entry.selection && Number.isFinite(entry.selection.weight) && entry.selection.weight > 0
        && Number.isFinite(entry.selection.cooldown_seconds) && entry.selection.cooldown_seconds >= 0
        && isText(entry.selection.repeat_group), '선택 정책 오류');
    } else {
      requireValue(entry.surface === 'npc_conversation' && Array.isArray(entry.turns)
        && entry.turns.length >= 2 && entry.turns.length <= snapshot.policy.max_turns, '대화 턴 수 오류');
      requireValue(['stop_and_reset', 'resume'].includes(entry.interrupt_policy), '중단 정책 누락');
      speakers = [...new Set(entry.turns.map(turn => turn.speaker_id))];
      requireValue(speakers.length >= 2 && same([...speakers].sort(), [...slot.speaker_ids].sort()), '대화 참여자 변조');
      requireValue(entry.turns.every(turn => isText(turn.speech_act)), '발화 동기 누락');
      lines = entry.turns.map(turn => turn.line);
    }
    requireValue(Array.isArray(lines) && lines.every(line => isText(line)
      && [...line].length <= snapshot.policy.max_characters), '문장 길이/형식 오류');
    for (const speaker of speakers) {
      const npc = npcs.get(speaker);
      requireValue(npc && includesAll(npc.knowledge_fact_ids, entry.fact_refs), `NPC 지식 누설: ${speaker}`);
      for (const id of entry.fact_refs) {
        const fact = facts.get(id);
        requireValue(fact && includesAll(entry.source_refs, fact.source_refs), `팩트/출처 불일치: ${id}`);
        requireValue(fact.disclosure === 'public' || fact.allowed_speakers?.includes(speaker), `공개 권한 없음: ${id}`);
        // 임의 AST의 함의를 추측하지 않고 필수 공개 조건을 all의 직접 항으로 보존한다.
        requireValue(fact.valid_when.op === 'true' || same(entry.conditions, fact.valid_when)
          || (entry.conditions.op === 'all' && entry.conditions.children.some(child => same(child, fact.valid_when))),
        `공개 시점 조건 누락: ${id}`);
      }
    }
  }
  if (previous) {
    requireValue(previous.pack_id === pack.pack_id && previous.locale === pack.locale, '이전 팩 식별 불일치');
    const current = index(records, 'id');
    for (const old of [...previous.entries, ...previous.conversations]) {
      const next = current.get(old.id);
      if (old.locked || old.human_edited || old.review_status === 'approved')
        requireValue(same(old, next), `LOCK_CONFLICT: ${old.id}`);
      if (next) requireValue(old.slot_id === next.slot_id, `ID 의미 재사용: ${old.id}`);
    }
  }
  return { status: 'PASS', entries: pack.entries.length, conversations: pack.conversations.length,
    snapshot_sha256: hash(canonical(snapshot)), pack_sha256: hash(canonical(pack)),
    editorial_review: '사람 검수 필요: 의미·말투·이미지·빈도' };
}

/** 승인 기록은 정확한 입력 해시에 묶는다. 이 함수는 승인 기록을 만들지 않는다. */
export function compile(snapshot, pack, review, previous = null) {
  const report = validate(snapshot, pack, previous);
  requireValue(!pack.fixture_only, '픽스처는 배포할 수 없습니다.');
  requireValue(review.decision === 'approved' && isText(review.reviewer) && isText(review.reviewed_at)
    && review.pack_sha256 === report.pack_sha256 && review.snapshot_sha256 === report.snapshot_sha256,
  '정확한 입력에 대한 사용자 승인 기록이 필요합니다.');
  const all = [...pack.entries, ...pack.conversations];
  requireValue(all.length > 0 && all.every(item => item.review_status === 'approved'), '미승인 또는 빈 팩');
  return { schema_version: '1.0.0', pack_id: pack.pack_id, locale: pack.locale,
    entries: [...pack.entries].sort((a, b) => a.id.localeCompare(b.id, 'en')),
    conversations: [...pack.conversations].sort((a, b) => a.id.localeCompare(b.id, 'en')),
    manifest: { ...report, review } };
}

/** CLI 출력은 신규 staging/reports 파일로만 작성해 기존 결과와 원본을 보존한다. */
function writeOutput(relative, value) {
  const target = path.resolve(root, relative);
  const base = path.join(root, 'work/dialogue-bake');
  requireValue(['staging', 'reports'].some(folder => target.startsWith(path.join(base, folder) + path.sep)), '출력 경계 위반');
  let ancestor = path.dirname(target);
  while (!fs.existsSync(ancestor)) ancestor = path.dirname(ancestor);
  requireValue(fs.realpathSync(ancestor) === ancestor, '링크 출력 경로 금지');
  fs.mkdirSync(path.dirname(target), { recursive: true });
  fs.writeFileSync(target, JSON.stringify(value, null, 2) + '\n', { flag: 'wx' });
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  try {
    const [command, input, packPath, ...args] = process.argv.slice(2);
    const read = file => JSON.parse(fs.readFileSync(sourceFile(file), 'utf8').replace(/^\uFEFF/, ''));
    if (command === 'discover') writeOutput(packPath, discover(read(input)));
    else if (command === 'validate') {
      const [previousPath] = args;
      console.log(JSON.stringify(validate(read(input), read(packPath), previousPath ? read(previousPath) : null), null, 2));
    } else if (command === 'compile') {
      const [reviewPath, output, previousPath] = args;
      writeOutput(output, compile(read(input), read(packPath), read(reviewPath), previousPath ? read(previousPath) : null));
    } else throw new Error('사용법: discover config output | validate snapshot pack [previous] | compile snapshot pack review output [previous]');
  } catch (error) { console.error(`FAIL: ${error.message}`); process.exitCode = 1; }
}
