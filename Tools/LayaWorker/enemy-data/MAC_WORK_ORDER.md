# 실제 몬스터 데이터 후속 확인

`Tools/LayaWorker/enemy-data/README.md`를 먼저 읽는다. 기존 40개 합성 플레이어 평가와 이번 몬스터 데이터는 별개다. 중간 사용자 확인 없이 아래를 수행하고 결과를 한 폴더로 반환한다.

1. `enemy_decision_request.py --verify`와 전체 단위 검사를 실행한다. 원본 에셋이나 기존 평가 결과는 변경하지 않는다.
2. 골렘·엔트의 Behavior·가중치·BT·공격 연결을 실제 파일에서 확인한다. `native-score-fixtures.json`과 `examples/`의 Context가 일치하고 상위 후보가 이어지는지 확인한다.
3. 실제 점수 코어의 원시 점수/최종 점수/난수 후보와 최종 BT 행동은 다른 층이라는 점을 보고한다. native fixture의 중립 보정 범위를 벗어나 동등성을 주장하지 않는다.
4. 기존 캐시 모델로 README의 12개 합성 몬스터 요청을 `--offline` 실행한다. 원시 응답·토큰 잘림·추론 시간·선택 Intent를 남긴다. 입력/후보/프롬프트를 결과에 맞춰 고치지 않는다.
5. Laya가 최고 점수를 골랐는지 하나로 합격을 판정하지 않는다. 기존 AI는 상위 3개를 확률 선택한다. 후보 밖 선택·잘린 입력의 성공 반환·관측과 명백히 모순되는 판단을 조사하고 그 근거를 적는다.
6. 실제 런타임 관측이 없으면 가정을 유지한 채 합성 적합성만 보고한다. Unity 실행·전투 자동 조작·Windows 연결·새 모델 다운로드·학습을 하지 않는다.

반환 경로는 `Tools/LayaWorker/output/enemy-data-review/<run-id>/`다. 최소 `REPORT.md`, `environment.json`, `unit-tests.txt`, `requests.jsonl`, `predictions.jsonl`, `analysis.json`, `SHA256SUMS`를 담는다. 각 결과에 입력 manifest 해시·코드 커밋·모델 revision을 기록한다. 오류가 있으면 부분 결과와 미수행 사유를 함께 남긴다.

이번 output과 필요한 최소 수정만 한국어 메시지로 커밋한다. ignored output은 해당 경로만 명시해 추가하고 모델·venv·원본 게임 파일은 포함하지 않는다. 마지막에는 커밋 해시·output 경로·실행 수·미검증 범위를 보고한다.
