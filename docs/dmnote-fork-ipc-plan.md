# DM Note 1.6.1 포크 + ADOFAI-IPC 키 리미터 설계

## 결정

DM Note 1.6.1을 장기 유지 포크하고, 현재 선택된 손·발 프로필의 유효 키 합집합을 ADOFAI-IPC를 통해 `ImplResourcePack`으로 푸시하는 방식을 권장한다.

이 방식이 파일 감시보다 나은 이유는 다음과 같다.

- 게임 모드가 DM Note의 `store.json` 경로와 전체 저장 포맷을 알 필요가 없다.
- DM Note가 키 추가, 삭제, 프로필 변경, 프리셋 로드, ghost 지정의 의미를 직접 해석한다.
- IPC 요청 결과로 게임이 설정을 수락했는지 즉시 알 수 있다.
- 향후 포크에 2.x 기능을 선택적으로 이식해도 게임 쪽 계약은 유지할 수 있다.
- 앱 UI에 연결 상태와 마지막 동기화 결과를 표시할 수 있다.

기존 파일 감시안은 IPC 장애 시 진단 또는 수동 복구 수단으로만 남기고, 두 입력원을 동시에 활성화하지 않는다.

## 확인한 기반

### DM Note

- 로컬 macOS 앱 버전은 1.6.1이다.
- 기준 소스는 [DmNote 1.6.1 태그](https://github.com/DmNote-App/DmNote/tree/1.6.1)다.
- 라이선스는 GPL-3.0-only이므로 포크 배포물도 해당 의무를 유지해야 한다.
- 손·발 프로필은 `selectedViewerTabs.hand`와 `selectedViewerTabs.foot`, 키 배열은 `keys[mode]`, 키별 표시/클래스 정보는 `keyPositions[mode]`에 저장된다.
- 키와 위치 배열은 같은 모드에서 인덱스로 대응한다.
- 키 변경은 백엔드 `keys_update`를 거쳐 내부 `keys:changed` 이벤트를 발생시킨다.
- 프로필 변경은 `keys:mode-changed`, 위치와 클래스 변경은 `positions:changed`로 관찰할 수 있다.

관련 소스:

- [키 명령](https://github.com/DmNote-App/DmNote/blob/1.6.1/src-tauri/src/commands/keys/keys.rs)
- [키 API](https://github.com/DmNote-App/DmNote/blob/1.6.1/src/renderer/api/modules/keysApi.ts)
- [키 모델](https://github.com/DmNote-App/DmNote/blob/1.6.1/src/types/key/keys.ts)
- [키 렌더링](https://github.com/DmNote-App/DmNote/blob/1.6.1/src/renderer/components/shared/Key.tsx)

### ADOFAI-IPC

- 기준 저장소는 [KGH1113/adofai-ipc](https://github.com/KGH1113/adofai-ipc)다.
- 로컬 설치 버전과 npm 클라이언트 버전은 모두 0.3.0이다.
- 서버는 기본 `127.0.0.1:32145`부터 `32155`까지 fallback 포트를 사용한다.
- 외부 클라이언트는 `POST /ipc`로 모드 namespace와 method를 호출한다.
- `@adofai-ipc/client` 0.3.0이 npm에 공개되어 있으므로 DM Note 포크에서 직접 고정 의존성으로 사용할 수 있다.
- 클라이언트는 포트 탐색, 요청 timeout, namespace ready 대기를 이미 제공한다.

관련 문서:

- [HTTP 프로토콜](https://github.com/KGH1113/adofai-ipc/blob/main/docs/kor/003-HttpProtocol.md)
- [JavaScript/TypeScript 클라이언트](https://github.com/KGH1113/adofai-ipc/blob/main/docs/kor/005-JsClient.md)
- [모드 API](https://github.com/KGH1113/adofai-ipc/blob/main/docs/kor/002-ModApi.md)
- [npm 클라이언트](https://www.npmjs.com/package/@adofai-ipc/client)

## `.ghost` 규칙

DM Note 1.6.1에는 임의의 inline CSS 텍스트를 키마다 저장하는 필드가 없다. 이 요구사항에 대응하는 실제 필드는 `keyPositions[mode][index].className`이다.

규칙은 다음처럼 정의한다.

- 사용자 입력: 키의 클래스 이름에 `ghost` 토큰을 추가한다.
- CSS 선택자: `.ghost`를 사용한다.
- 판정: `className`을 공백 기준 CSS 클래스 토큰으로 나눈 뒤 정확히 `ghost`인 토큰이 있는지 검사한다.
- 호환성: 실수로 `.ghost`를 입력한 기존 데이터가 있다면 포크에서 앞의 점을 제거해 `ghost`로 정규화할 수 있다.
- `ghosted`, `my-ghost` 같은 부분 문자열은 ghost로 취급하지 않는다.
- `hidden` 속성은 ghost와 별개다. 숨김 키를 자동 제외하면 기존 프리셋 의미가 바뀔 수 있으므로 제외하지 않는다.

같은 물리 키가 한 프로필에 여러 번 들어갈 수 있으므로 인덱스별로 먼저 필터링한 뒤 중복을 제거한다.

```text
effectiveKeys = distinct(
  keys[currentMode][i]
  where keyPositions[currentMode][i].className does not contain token "ghost"
)
```

예를 들어 `A` 배치가 두 개이고 하나만 ghost라면 `A`는 허용한다. 두 배치가 모두 ghost일 때만 `A`를 허용 목록에서 제거한다.

배열 길이가 다르면 위치 정보가 없는 키는 ghost가 아닌 것으로 취급한다. 저장 중간 상태나 구형 프리셋 때문에 키가 뜻하지 않게 차단되는 것을 막기 위한 fail-open 규칙이다.

## IPC 계약

`ImplResourcePack`이 다음 namespace를 소유한다.

```text
namespace: impl-resourcepack
```

### `key-limiter.sync`

DM Note가 현재 유효 상태 전체를 보낸다. 변경분만 보내지 않고 완전한 스냅샷을 보내야 재접속과 순서 뒤바뀜을 안전하게 처리할 수 있다.

요청 예시:

```json
{
  "schemaVersion": 1,
  "source": "impl-dm-note",
  "clientVersion": "1.6.1-kgh.1",
  "sessionId": "3e889705-2bd7-48e0-90a9-b1dc7dc473a1",
  "revision": 42,
  "enabled": true,
  "profile": {
    "id": "dual:hand-main:foot-2",
    "name": "Hand Main + Foot 2"
  },
  "keys": ["TAB", "1", "2", "E", "LEFT SHIFT", "SPACE"],
  "excludedGhostKeys": ["SEMICOLON"]
}
```

필드 의미:

- `schemaVersion`: IPC payload 스키마 버전이다. 앱 버전과 분리한다.
- `sessionId`: DM Note 실행마다 새 UUID를 만든다.
- `revision`: 같은 세션 안에서 상태가 바뀔 때 증가한다.
- `enabled`: 포크 설정에서 ADOFAI 키 리미터 동기화를 켰는지 나타낸다.
- `profile.id`: 손·발 선택 탭 ID를 인코딩한 안정적인 합성 ID다.
- `profile.name`: 손·발 탭 이름을 합친 UI와 로그용 문자열이며 판정에는 사용하지 않는다.
- `keys`: 선택된 손 탭 뒤 발 탭 순서로 ghost를 제외하고 중복 제거한 완전한 허용 키 합집합이다. 한쪽에서 ghost여도 다른 쪽에서 일반 키로 사용하면 허용한다.
- `excludedGhostKeys`: 진단용이다. 서버는 이 값으로 허용 목록을 다시 계산하지 않는다.

응답 예시:

```json
{
  "accepted": true,
  "serverInstanceId": "b4cf6a90-febe-49af-bfa1-0de1583e92ae",
  "sessionId": "3e889705-2bd7-48e0-90a9-b1dc7dc473a1",
  "revision": 42,
  "enabled": true,
  "profileId": "custom-1786698912802",
  "acceptedKeyCount": 6,
  "unknownKeys": []
}
```

알 수 없는 키 라벨이 있으면 알려진 키는 적용하되 `unknownKeys`로 반환한다. 키 목록이 비어 있는 정상 프로필은 유효한 “모든 일반 키 차단” 상태가 될 수 있으므로 오류로 간주하지 않는다.

### `key-limiter.status`

DM Note가 게임 또는 AdofaiIpc 재시작을 감지하고 재동기화할 때 사용한다.

응답에 다음을 포함한다.

```json
{
  "serverInstanceId": "b4cf6a90-febe-49af-bfa1-0de1583e92ae",
  "hasSnapshot": true,
  "enabled": true,
  "source": "dmnote",
  "sessionId": "3e889705-2bd7-48e0-90a9-b1dc7dc473a1",
  "revision": 42,
  "profileId": "custom-1786698912802",
  "acceptedKeyCount": 6
}
```

`serverInstanceId`는 `ImplResourcePack` 런타임 초기화마다 바뀐다. DM Note가 이전과 다른 값을 보면 최신 스냅샷을 다시 보낸다.

별도의 `disable` method는 만들지 않는다. `sync`에 `enabled: false`와 빈 키 목록을 보내 상태 전환을 하나의 계약으로 유지한다.

## DM Note 포크 구현

### 의존성

`package.json`에 정확한 버전을 고정한다.

```json
"@adofai-ipc/client": "0.3.0"
```

ADOFAI-IPC 클라이언트는 서버 제품 버전과의 불일치를 검사하므로 임의의 caret 범위를 사용하지 않는다. 서버를 올릴 때 포크도 함께 검증한 뒤 버전을 갱신한다.

### 동기화 coordinator

예상 파일:

- `src/renderer/integrations/adofaiIpc/client.ts`
- `src/renderer/integrations/adofaiIpc/keyLimiterSync.ts`
- `src/renderer/integrations/adofaiIpc/types.ts`

coordinator는 항상 “보내야 할 최신 스냅샷” 하나만 보유한다.

트리거:

- 앱 bootstrap 완료
- `keys:changed`
- `keys:mode-changed`
- `positions:changed`
- 프리셋 로드와 전체/모드 초기화
- 커스텀 탭 생성, 선택, 삭제
- ADOFAI 동기화 설정 on/off

키 배열과 위치 배열이 별도 이벤트로 갱신될 수 있으므로 50~100ms debounce 후 한 번 계산한다. debounce는 네트워크 전송만 합치며 UI 저장 자체를 지연시키지 않는다.

연결 흐름:

1. `tryConnect()`로 32145~32155 포트를 탐색한다.
2. `waitForNamespace("impl-resourcepack", { status: "ready" })`로 게임 모드 준비를 기다린다.
3. 최신 스냅샷을 `key-limiter.sync`로 보낸다.
4. 실패하면 지수 backoff로 재시도하되 최신 스냅샷만 유지한다.
5. 연결 후에는 3~5초마다 `key-limiter.status`를 확인한다.
6. 서버 instance가 바뀌었거나 서버가 가진 session/revision이 다르면 즉시 전체 스냅샷을 다시 보낸다.

단순히 변경 때만 한 번 보내면 다음 상황을 복구하지 못한다.

- DM Note가 게임보다 먼저 실행됨
- 게임만 재시작됨
- AdofaiIpc가 재설치 또는 재시작됨
- 첫 요청 순간 namespace가 아직 `initializing` 상태였음

상태 polling은 키 입력을 보내는 것이 아니라 작은 상태 확인 요청이므로 오버헤드가 미미하다.

### Origin

Tauri WebView의 `fetch`에는 Origin이 붙을 수 있다. `ImplResourcePack` namespace의 `AllowedOrigins`에는 최소한 다음 값을 등록한다.

```text
tauri://localhost
http://tauri.localhost
https://tauri.localhost
http://localhost
http://127.0.0.1
```

실제 release 앱의 Origin은 macOS 빌드에서 한 번 로그로 확인한다. ADOFAI-IPC는 Origin이 없는 native 요청을 허용하고, Origin이 있는 요청은 namespace 설정과 대조한다.

### UI 설정

포크 설정에 다음 항목을 추가한다.

- `ADOFAI 키 리미터 동기화` 토글
- 연결 상태: 연결 안 됨 / namespace 대기 / 동기화됨 / 오류
- 현재 전송 프로필과 유효 키 개수
- 마지막 오류와 재시도 버튼

연결이 안 된다는 이유로 DM Note 자체 키뷰어 기능을 막지 않는다.

## ImplResourcePack 구현

AdofaiIpc 의존성 통합의 구체적인 기준 구현과 이 프로젝트에 적용할 차이는
[`adofai-ipc-tuf-replay-reference.md`](adofai-ipc-tuf-replay-reference.md)에 정리했다.

### ADOFAI-IPC namespace

예상 파일:

- `Infrastructure/Ipc/ImplResourcePackIpcFeature.cs`
- `Application/KeyLimiting/KeyLimiterService.cs`
- `Application/KeyLimiting/DmNoteLimiterPayload.cs`

초기화 흐름:

1. `impl-resourcepack` namespace 등록
2. `key-limiter.sync`와 `key-limiter.status` 등록
3. 키 라벨 mapper 및 limiter 상태 준비
4. `MarkReady()`
5. 종료 시 namespace unregister

`sync`는 Unity와 SkyHook 타입을 사용해 키 라벨을 변환하므로 `RegisterMainThread`를 사용하는 편이 안전하다. `status`는 immutable snapshot만 읽으므로 일반 `Register`로 충분하다.

### 상태 적용

- payload 전체를 검증하고 변환한 뒤 immutable snapshot 참조를 한 번에 교체한다.
- 같은 `sessionId`에서는 현재보다 작은 revision을 거부한다.
- 새 `sessionId`는 DM Note 재시작으로 보고 받아들인다.
- `enabled: false`면 limiter를 fail-open 상태로 전환한다.
- 아직 성공한 sync가 없을 때도 fail-open이다.
- IPC 연결이 잠시 끊겨도 마지막 정상 snapshot은 유지한다.
- 현재 적용 프로필과 unknown key는 UMM 로그에 요약한다.

키 차단 Harmony 패치는 기존 조사 문서의 KeyboardChatterBlocker 동작을 기준으로 한다. IPC는 허용 목록의 공급 방식만 바꾸며 실제 입력 차단 방식은 동일하다.

### 의존성 배포

`ImplResourcePack`은 AdofaiIpc 0.3.0의 dependency bootstrap을 포함하는 것이 좋다. 사용자가 별도로 모드를 찾아 설치하지 않아도 되고, 구버전이나 비활성화 상태를 공용 안내창으로 보여줄 수 있다.

## 포크 유지 전략

### 기준선

- upstream `1.6.1` 태그에서 유지 브랜치를 만든다.
- 포크 버전은 `1.6.1-kgh.1`, `1.6.1-kgh.2`처럼 upstream과 구분한다.
- upstream 2.x를 그대로 merge하지 않고 필요한 기능을 기능 단위로 cherry-pick 또는 재구현한다.
- IPC payload의 `schemaVersion`은 앱 버전과 독립적으로 유지한다.

### 앱 식별과 업데이트

장기 포크라면 upstream 자동 업데이트가 포크를 덮어쓰지 않도록 반드시 분리한다.

- upstream updater를 비활성화하거나 포크 GitHub Releases로 변경
- 제품명을 예: `DM NOTE Legacy` 또는 `DM NOTE KGH`로 변경
- bundle identifier를 분리하는 것을 권장
- 기존 `com.dmnote.desktop/store.json`을 첫 실행 때 가져오는 migration 제공
- macOS 접근성/입력 모니터링 권한을 새 앱에 다시 부여하도록 안내

동일 bundle identifier를 유지하면 설정 재사용은 쉽지만 upstream 앱과 병행 설치, 권한, updater, 서명 식별이 뒤섞인다. 장기 관리 목적에는 별도 identifier와 명시적 migration이 더 안전하다.

### 2.x 기능 이식 기준

2.x 기능은 다음 순서로 선별한다.

1. 데이터 모델 변경이 작은 독립 기능
2. 성능/입력 안정성 수정
3. 다중 키 매핑처럼 IPC 스키마 확장이 필요한 기능
4. 대규모 UI 개편과 편집 히스토리 구조는 마지막에 검토

다중 키 매핑을 나중에 넣을 때도 게임 IPC에는 최종 물리 키 허용 집합만 보내면 기존 `schemaVersion: 1` 계약을 대부분 유지할 수 있다. 동시 입력 의미까지 게임 리미터가 알아야 하는 요구가 생길 때만 새 schema를 만든다.

## 테스트

### DM Note 단위 테스트

- `ghost` 단일 클래스 제외
- 여러 클래스 중 `ghost` 토큰 제외
- `.ghost` 입력 정규화
- `ghosted` 부분 문자열은 유지
- 중복 키 중 하나만 ghost면 키 유지
- 중복 키가 모두 ghost면 제외
- positions 배열이 짧으면 누락 위치의 키 유지
- 프로필 변경 시 완전한 새 snapshot 생성
- 빠른 keys/positions 연속 변경을 한 요청으로 debounce

### IPC 통합 테스트

- 게임 먼저 실행, DM Note 나중 실행
- DM Note 먼저 실행, 게임 나중 실행
- 게임만 재시작했을 때 `serverInstanceId` 변경 후 재동기화
- namespace initializing 동안 대기 후 성공
- 32145 포트가 사용 중일 때 fallback 포트 연결
- 같은 세션의 오래된 revision 거부
- 새 세션의 낮은 revision 수락
- unknown key 부분 적용과 응답 표시
- 동기화 off 시 fail-open

### 게임 입력 테스트

- 현재 선택된 손·발 프로필의 non-ghost 키 합집합만 판정
- ghost 키는 DM Note에는 표시되지만 ADOFAI 판정에는 사용되지 않음
- Escape, 메뉴, 에디터, 일시정지 입력은 유지
- SkyHook 활성/비활성 경로 확인
- 기존 KeyboardChatterBlocker 키 리미터를 끈 상태에서 검증

## 구현 순서

1. `ImplResourcePack`에 namespace와 payload 계약, status method 추가
2. limiter의 키 라벨 변환 및 Harmony 패치 구현
3. DM Note 1.6.1 포크 생성, 앱 identity/updater 정책 결정
4. `@adofai-ipc/client@0.3.0`과 sync coordinator 추가
5. ghost 필터와 단위 테스트 추가
6. 연결 상태 UI 추가
7. 양쪽 통합 테스트와 macOS release 빌드
8. 포크 migration 및 배포 문서 정리
