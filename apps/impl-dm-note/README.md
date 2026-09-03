# ImplDmNote

ImplDmNote는 리듬 게임용 키 입력 시각화 데스크톱 앱입니다. DM Note v1.6.1을 기반으로 하며 ImplResourcePack과 localhost ADOFAI-IPC로 현재 키 리미터 프로필을 동기화합니다.

## 주요 기능

- 손·발 오버레이와 키 입력 횟수 표시
- 프리셋, 키음, 커스텀 CSS, 신뢰된 JavaScript 플러그인
- OBS 브라우저 소스 연결
- 노래 제목, 녹화 모드, 판정 표시 설정
- Windows ASIO 출력과 macOS Dock helper

## 개발

저장소 루트에서 Node.js 22와 stable Rust를 준비한 뒤 실행합니다.

```bash
npm ci
npm run desktop:dev
npm run desktop:check
cd apps/impl-dm-note/src-tauri
cargo test --locked
cargo clippy --locked --all-targets -- -D warnings
```

현재 운영체제의 릴리스 빌드는 `npm run desktop:build`로 만들 수 있습니다. 명시적인 플랫폼 명령은 `desktop:build:mac`과 `desktop:build:windows`입니다. Windows 공식 산출물은 `ImplDmNote-v<version>-windows-x64-portable.zip`이며 WebView2 Fixed Runtime과 법적 고지를 포함합니다.

## 데이터와 호환성

- Windows: `%APPDATA%/io.github.kgh1113.impldmnote/store.json`
- macOS: `~/Library/Application Support/io.github.kgh1113.impldmnote/store.json`

기존 프리셋을 위한 `dmn` 플러그인 API와 `dmnote-local-*` URI는 공개 호환 인터페이스로 유지됩니다. 이전 설정 파일의 알 수 없는 필드는 무시됩니다.

## 원본 DM Note와의 차이

기준 원본과 ImplDmNote 사이의 제품·배포·보안 차이는 [`DIFFERENCES.md`](DIFFERENCES.md)에 한곳으로 정리되어 있습니다. 정확한 원본 태그와 커밋, 저작권 정보는 [`UPSTREAM.md`](UPSTREAM.md)를 확인하세요.

## 보안

외부 링크는 `https`, `http`, `mailto` 스킴만 열 수 있습니다. 커스텀 JavaScript 플러그인은 샌드박스되지 않고 현재 사용자 권한으로 실행되므로, 내용을 검토한 신뢰할 수 있는 플러그인만 설치하세요. OBS 연결은 사용자가 시작한 로컬 브리지에 한정하고 세션 토큰을 사용합니다.

## 라이선스와 원본

ImplDmNote는 저장소 루트의 독점 라이선스 범위에서 제외된 독립적인 GPL-3.0-only 구성요소입니다. GPL 전문은 [`LICENSE`](LICENSE), 포함 자산과 SDK 고지는 [`THIRD_PARTY_NOTICES.txt`](THIRD_PARTY_NOTICES.txt) 및 [`THIRD_PARTY_LICENSES.txt`](THIRD_PARTY_LICENSES.txt)를 확인하세요.

Copyright (C) 2024 lee-sihun and DM Note contributors. 2026 ImplDmNote modifications Copyright (C) 2026 KGH1113.
