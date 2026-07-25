# ImplResourcePack

JALib에 의존하지 않는 A Dance of Fire and Ice용 UnityModManager 오버레이 모드입니다.

현재는 Jipper 스타일의 싱글 플레이 판정 오버레이를 제공합니다. 키뷰어는 범위에서 제외하며, 이후 다음 오버레이 요소를 독립 모듈로 추가합니다.

- 진행도 및 진행도 바
- 정확도와 절대 정확도
- 음악 시간과 맵 시간
- BPM
- 판정
- 콤보
- 타이밍 스케일
- 최고 기록과 시도 횟수

## Runtime dependencies

- A Dance of Fire and Ice
- UnityModManager
- Harmony

JALib과 AdofaiIpc에는 의존하지 않습니다.

판정 오버레이는 JipperResourcePack과 동일한 `MAPLESTORY_OTF_BOLD SDF` 폰트 자산을 플랫폼별 AssetBundle에서 로드합니다.

## Build

빌드 환경 설정은 `tuf-replay`와 같은 `.env` 규약을 사용합니다.

폰트 AssetBundle을 다시 생성하려면 Unity 에디터를 닫은 뒤 다음 명령을 실행합니다.

```bash
./scripts/run.sh assets
```

Unity 프로젝트는 `ImplResourcePack.Unity`에 있으며 Unity `6000.3.10f1`을 사용합니다.

```bash
cp .env.example .env
./scripts/run.sh build
```

`build` 명령은 입력 경로를 검증하고 Debug 빌드를 만든 뒤 기본적으로 ADOFAI의 `Mods/ImplResourcePack`에 설치합니다.

릴리스 패키지는 다음 명령으로 만듭니다.

```bash
./scripts/run.sh package
```

생성물:

- `build/ImplResourcePack.zip`
- `build/ImplResourcePack.version`
- `build/ImplResourcePack.zip.sha256`

셸 스크립트만 검사하려면 다음을 실행합니다.

```bash
./scripts/run.sh check
```

## Formatting

C# 포맷터는 `tuf-replay`와 동일한 CSharpier 1.3.0을 사용합니다.

```bash
dotnet tool restore
dotnet csharpier format ImplResourcePack
dotnet csharpier check ImplResourcePack
```
