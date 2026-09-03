# DM Note 연동 키 리미터 조사 및 구현안

> 이 문서는 최초의 파일 감시안을 보존한다. 현재 권장안은
> [`dmnote-fork-ipc-plan.md`](dmnote-fork-ipc-plan.md)의 DM Note 1.6.1 포크 + ADOFAI-IPC 푸시 방식이다.
> 현재 포크는 `selectedViewerTabs.hand`와 `selectedViewerTabs.foot`의 유효 키 합집합을 하나의 합성 프로필로 전송하며, 아래의 `selectedKeyType` 기반 설명은 최초 조사 당시의 단일 키뷰어 설계다.

## 결론

DM Note 1.6.1의 내부 IPC에 연결하지 않고, macOS 앱 데이터의 `store.json`을 감시하는 방식으로 구현하는 것이 가장 단순하고 안정적이다.

- DM Note 설정 파일: `~/Library/Application Support/com.dmnote.desktop/store.json`
- 현재 선택한 키뷰어 모드: 루트의 `selectedKeyType`
- 모드별 허용 키 목록: 루트의 `keys[selectedKeyType]`
- 키를 추가하거나 삭제하면 DM Note의 `keys_update` 명령이 전체 매핑을 저장하고 `keys:changed`를 앱 내부에 발생시킨다.
- 파일은 DM Note가 실행 중이지 않아도 남아 있으므로, 게임 모드는 별도 프로세스 통신 없이 마지막 키뷰어 설정을 사용할 수 있다.

따라서 `ImplResourcePack` 안에 독립적인 DM Note 설정 리더와 키 리미터를 추가하고, 파일 변경 때마다 현재 모드의 키 목록을 원자적으로 교체하면 된다.

## 확인한 버전과 근거

- 로컬 앱: `/Applications/DM NOTE.app`
- `CFBundleShortVersionString`: `1.6.1`
- 앱 식별자: `com.dmnote.desktop`
- 로컬 설정 파일이 실제로 존재하며, `selectedKeyType`, `keys`, `keyPositions` 구조를 확인했다.
- 공식 소스 태그: [DmNote 1.6.1](https://github.com/DmNote-App/DmNote/tree/1.6.1)
- 저장 구현: [src-tauri/src/state/store.rs](https://github.com/DmNote-App/DmNote/blob/1.6.1/src-tauri/src/state/store.rs)
- 키 변경 명령: [src-tauri/src/commands/keys/keys.rs](https://github.com/DmNote-App/DmNote/blob/1.6.1/src-tauri/src/commands/keys/keys.rs)
- macOS 키 라벨 생성: [src-tauri/src/keyboard/daemon/macos.rs](https://github.com/DmNote-App/DmNote/blob/1.6.1/src-tauri/src/keyboard/daemon/macos.rs)

DM Note 1.6.1의 `AppStore::persist_locked`는 `serde_json::to_string_pretty` 후 `fs::write`로 `store.json`을 갱신한다. `keys_update`는 저장소의 `keys`를 갱신한 뒤 앱 내부 `keys:changed` 이벤트를 발생시킨다. 외부 게임 모드는 이 Tauri 이벤트를 직접 받을 수 없으므로 파일 감시가 적절하다.

## KeyboardChatterBlocker에서 확인한 동작

로컬 설치본:

`~/Library/Application Support/Steam/steamapps/common/A Dance of Fire and Ice/Mods/KeyboardChatterBlocker.v0.1.0 (2)/KeyboardChatterBlocker.dll`

`ilspycmd 10.0.1.8346`으로 다음 타입을 확인했다.

- `KeyboardChatterBlocker.KeyLimiterProfile`
  - `allowedAsyncKeys: List<ushort>`
  - `allowedKeys: List<UnityEngine.KeyCode>`
- `KeyboardChatterBlocker.Patch.SkyHookManager_HookCallback_Patch`
  - SkyHook 입력이 허용 목록에 없으면 실제 게임 플레이 중 입력 콜백을 차단한다.
  - 키 릴리스와 Escape 계열 입력은 통과시킨다.
- `KeyboardChatterBlocker.Patch.scrController_CountValidKeysPressed_Patch`
  - `scrPlayer.CountValidKeysPressed`의 결과를 자체 계산으로 대체한다.
  - Unity `KeyCode` 입력은 허용 목록에 없는 경우 유효 입력 수에서 제외한다.
  - 허용된 키만 `keyFrequency`, `keyTotal` 및 판정 입력에 반영한다.
- 차단 조건
  - 컨트롤러가 존재한다.
  - 일시정지 상태가 아니다.
  - 게임 월드다.
  - 상태 머신이 실제 플레이 상태다.

채터 방지용 `inputInterval` 로직은 이번 기능의 범위가 아니다. 가져와야 하는 것은 허용 목록 기반의 키 리미터 부분뿐이다.

## 권장 구조

### 1. DM Note 설정 모델과 리더

예상 파일:

- `Infrastructure/DmNote/DmNoteStoreReader.cs`
- `Infrastructure/DmNote/DmNoteStoreSnapshot.cs`

필요한 JSON 필드만 역직렬화한다.

```text
selectedKeyType: string
keys: Dictionary<string, List<string>>
```

Newtonsoft.Json은 프로젝트에 이미 참조되어 있으므로 추가 패키지는 필요 없다.

경로는 사용자 이름을 하드코딩하지 않고 다음과 같이 만든다.

```text
Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
  + /com.dmnote.desktop/store.json
```

macOS Unity/Mono에서 `ApplicationData`는 일반적으로 `~/Library/Application Support`를 가리킨다. 초기화 때 최종 경로를 로그에 한 번 남겨 현장 확인이 가능하게 한다.

### 2. 파일 변경 감시

예상 파일:

- `Infrastructure/DmNote/DmNoteStoreWatcher.cs`

권장 방식은 Unity 메인 스레드에서 250~500ms마다 `LastWriteTimeUtc`와 파일 길이를 확인하는 폴링이다. `FileSystemWatcher`보다 구현이 단순하고, 게임 수명 주기와 함께 확실히 해제할 수 있다.

중요한 예외 처리:

- DM Note는 `fs::write`로 파일을 직접 다시 쓰므로 변경 순간에 비어 있거나 JSON이 덜 써진 상태를 볼 수 있다.
- 파싱 실패 시 현재 허용 목록을 즉시 비우면 모든 키가 차단될 수 있다.
- 마지막으로 성공한 스냅샷을 유지하고 다음 폴링에서 재시도한다.
- 시작할 때 파일이 없거나 한 번도 정상적으로 읽지 못했다면 리미터를 비활성화하는 fail-open 정책을 쓴다.
- 같은 내용이면 허용 목록을 재생성하지 않는다.
- `keys[selectedKeyType]`가 없으면 fail-open으로 처리하고 경고 로그를 남긴다.

### 3. DM Note 라벨 변환

예상 파일:

- `Infrastructure/Input/DmNoteKeyMapper.cs`

DM Note의 문자열을 두 가지 허용 집합으로 변환한다.

- `HashSet<KeyCode>`: Unity 입력 경로용
- `HashSet<ushort>`: SkyHook 입력 경로용

SkyHook 네이티브 키 코드는 직접 표를 하드코딩하지 않는다. 게임에 포함된 `SkyHook.Unity.dll`의 다음 API를 사용한다.

```text
DM Note label
  -> UnityEngine.KeyCode
  -> SkyHookKeyMapper.UnityKeyToSkyHookKey(KeyCode)
  -> SkyHookKeyMapper.KeyLabelToNativeKeyCode(KeyLabel)
```

이 방식이면 플랫폼별 SkyHook 네이티브 코드 차이를 게임 DLL에 맡길 수 있다. 프로젝트 파일에 `SkyHook.Unity.dll` 참조를 추가해야 한다.

DM Note 1.6.1 macOS의 주요 별칭도 처리해야 한다.

| DM Note 라벨 | Unity/SkyHook 의미 |
|---|---|
| `RETURN` | Enter |
| `EQUALS` | Equal |
| `FORWARD SLASH` | Slash |
| `SQUARE BRACKET OPEN` / `SQUARE BRACKET CLOSE` | Left/Right bracket |
| `LEFT CTRL`, `RIGHT CTRL`, `25` | 좌/우 Control |
| `LEFT ALT`, `RIGHT ALT`, `21` | 좌/우 Alt/Option |
| `LEFT SHIFT`, `RIGHT SHIFT` | 좌/우 Shift |
| `NUMPAD DELETE` | Keypad period/delete 위치 키 |
| `NUMPAD DIVIDE`, `NUMPAD 0` ... `NUMPAD 9` | 숫자 패드 키 |
| `MOUSE1` ... `MOUSE5` | 마우스 버튼 |

알 수 없는 라벨은 전체 리미터를 실패시키지 말고 해당 키만 건너뛰며 한 번만 경고한다. 현재 로컬에서 선택된 모드의 24개 라벨은 모두 이 변환 범위에 들어간다.

### 4. 키 리미터 상태 서비스

예상 파일:

- `Application/KeyLimiting/KeyLimiterService.cs`
- `Application/KeyLimiting/IKeyLimiter.cs`

서비스가 보유할 상태:

- 리미터 준비 여부
- 현재 DM Note 모드 ID
- 현재 DM Note 원본 라벨 집합
- Unity `KeyCode` 허용 집합
- SkyHook 네이티브 코드 허용 집합

새 스냅샷을 완전히 파싱하고 변환한 뒤 잠금 안에서 참조를 한 번에 교체한다. Harmony 패치가 입력 스레드에서 읽을 수 있으므로 수정 중인 `HashSet`을 직접 노출하지 않는다.

### 5. Harmony 패치

예상 파일:

- `Infrastructure/Game/Input/KeyLimiterPatches.cs`

KeyboardChatterBlocker와 같은 두 입력 경로를 처리한다.

1. `SkyHookManager.HookCallback` Prefix
   - 키 릴리스는 항상 통과시킨다.
   - Escape는 `ev.Label == KeyLabel.Escape`로 통과시킨다. 원본 모드처럼 숫자 리터럴에 의존하지 않는다.
   - 실제 플레이 상태에서만 허용 목록에 없는 키 다운을 차단한다.

2. `scrPlayer.CountValidKeysPressed`
   - 비동기 키는 SkyHook 단계에서 이미 걸러진다.
   - Unity `KeyCode` 경로는 KeyboardChatterBlocker의 유효 입력 계산과 같은 의미를 유지해야 한다.
   - 게임 버전 의존성이 큰 메서드 전체 복제는 최소화한다. 가능한 경우 `RDInput.GetMainPressKeys()` 결과를 해당 호출 범위에서만 필터링하는 보조 패치를 먼저 검토한다.
   - 전체 복제가 불가피하면 현재 게임 DLL의 메서드 시그니처와 필드를 기준으로 테스트를 고정한다.

공통 조건은 별도 함수로 둔다.

```text
controller != null
!controller.paused
controller.gameworld
state == States.Playing에 해당하는 실제 값
limiter has a valid snapshot
```

### 6. 런타임 연결

`ModRuntime.Initialize()`에서 다음 순서로 초기화한다.

1. DM Note 경로 결정
2. 최초 스냅샷 읽기
3. 리미터 서비스 생성
4. 감시 ticker 시작
5. Harmony 패치 적용

`Dispose()`에서는 ticker를 중지한 뒤 Harmony 패치를 해제한다. 폰트 로딩 실패와 키 리미터는 서로 독립이어야 한다. 현재 `Initialize()`는 폰트 로딩 실패 시 전체 런타임 초기화를 중단하므로, 키 리미터를 추가할 때는 오버레이 초기화 실패가 입력 기능까지 끄지 않도록 런타임 구성을 분리하는 편이 좋다.

## KeyboardChatterBlocker와의 공존

동일한 `SkyHookManager.HookCallback`과 `scrPlayer.CountValidKeysPressed`를 두 모드가 동시에 패치하면 패치 순서에 따라 결과가 달라질 수 있다.

권장 운영 방식:

- 새 기능을 `ImplResourcePack`에 구현한 뒤 기존 KeyboardChatterBlocker의 키 리미터는 끈다.
- 채터 차단 기능이 필요하면 KeyboardChatterBlocker는 유지하되 `enableKeyLimiter=false`로 둔다.
- 또는 이번 기능이 KeyboardChatterBlocker의 설정을 리플렉션으로 수정하는 방식도 가능하지만, 설치 여부와 내부 타입명에 의존하므로 권장하지 않는다.

현재 로컬 KeyboardChatterBlocker 설정은 `enableKeyLimiter=true`이므로 실제 구현 후에는 중복 리미터를 반드시 정리해야 한다.

## 테스트 항목

1. 게임과 DM Note를 실행하고 현재 DM Note 모드의 키만 판정되는지 확인
2. DM Note에서 키 하나를 추가한 뒤 500ms 이내에 게임에서 허용되는지 확인
3. 키를 제거하면 이후 입력이 차단되는지 확인
4. DM Note 모드를 바꾸면 `selectedKeyType`의 새 키 집합으로 교체되는지 확인
5. DM Note를 종료해도 마지막 정상 설정이 유지되는지 확인
6. `store.json`을 일시적으로 잘못된 JSON으로 만들어도 모든 키가 차단되지 않는지 확인
7. `store.json`이 없는 계정에서는 리미터가 비활성화되고 게임 입력이 정상인지 확인
8. Escape, 일시정지 화면, 에디터와 메뉴 입력이 차단되지 않는지 확인
9. 좌우 Shift/Ctrl/Alt, 숫자 패드, 방향키와 특수문자 라벨 확인
10. SkyHook 활성/비활성 입력 경로 모두 확인
11. KeyboardChatterBlocker 키 리미터를 켠 상태의 충돌을 재현하고 운영 안내대로 한쪽을 끈 뒤 정상인지 확인

## 구현 순서

1. `DmNoteStoreReader`와 라벨 변환기 단위 테스트
2. 폴링 watcher와 마지막 정상 스냅샷 정책 구현
3. SkyHook 입력 패치 구현 및 실제 macOS 게임 테스트
4. Unity `KeyCode` 입력 경로 구현 및 테스트
5. `ModRuntime` 수명 주기에 연결
6. 로그와 README의 의존성/공존 안내 추가

이 설계는 DM Note 1.6.1의 저장 포맷을 기준으로 한다. DM Note 2.0.0부터는 한 슬롯에 여러 키를 넣는 구조가 추가되었으므로, 이후 2.x도 지원하려면 `keys` 항목의 문자열 외에 다중 키 객체 형식을 추가로 파싱해야 한다.
