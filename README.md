# 스노우 엔젤 · SNOW ANGEL

크리스마스 시즌 인터랙티브 전시. 관객이 팔과 다리를 저으면 눈 위에 자국이 새겨지고, 완성된 천사가 빛을 머금고 떠오른 뒤 눈에 덮여 처음으로 돌아갑니다.

## 흐름 (1회 약 30초, 자동 순환)

1. **눈**: 빈 눈밭 위로 눈이 내립니다.
2. **몸짓**: 지정 위치에 서서 팔다리를 저으면 그만큼 자국이 새겨집니다. 최대 22초 동안 진행되고, 날개와 다리를 충분히 저으면 더 일찍 끝납니다.
3. **비상**: 완성된 천사가 금빛으로 빛나며 떠오릅니다(4.5초).
4. **덮임**: 눈이 흔적을 덮고 처음으로 돌아갑니다(3.5초).

- 동시에 1~2명이 참여합니다(화면 왼쪽과 오른쪽 자리).
- 몸통 길이를 기준으로 크기를 맞추기 때문에 카메라와의 거리와 관계없이 천사가 같은 크기로 그려집니다.
- 영상은 저장하지 않고 브라우저 안에서 실시간으로만 처리합니다.

## 기술

- [MediaPipe Pose Landmarker](https://ai.google.dev/edge/mediapipe/solutions/vision/pose_landmarker) (`@mediapipe/tasks-vision` 0.10.18, `pose_landmarker_lite` 모델)
- 런타임(`vendor/`)과 모델(`models/`)을 레포에 포함해 **인터넷 없이** 동작합니다.
- 빌드 과정 없이 HTML 파일 하나로 실행합니다.

## 공유 링크

https://tami-u.github.io/snow-angel/ (GitHub Pages: Settings → Pages → Branch `main` / root)

## 실행

카메라를 쓰려면 `file://`이 아니라 로컬 서버나 HTTPS에서 열어야 합니다.

```bash
cd snow-angel
python -m http.server 8000
# 브라우저에서 http://localhost:8000
```

### 전시장 무인 운영 (Chrome 키오스크)

```bash
chrome --kiosk --autoplay-policy=no-user-gesture-required --use-fake-ui-for-media-stream http://localhost:8000
```

- `--autoplay-policy=...`: 클릭 없이도 소리(바람, 차임)가 나게 합니다.
- `--use-fake-ui-for-media-stream`: 카메라 권한 팝업을 자동으로 허용합니다.

### 운영자 키

| 키 | 기능 |
| --- | --- |
| `D` | 디버그(카메라 미리보기, 관절 점, 진행도, FPS) |
| `F` | 전체화면 |
| `R` | 처음으로 초기화 |

## 설치 메모

- 75″ 가로형 디스플레이 기준 16:9 화면입니다.
- 웹캠은 화면 위나 아래 가운데에 두고, 관객의 머리부터 발목까지 화면에 들어오게 맞춥니다. 발목이 안 보이면 무릎으로 다리를 계산합니다.
- 바닥 지정 위치 2곳은 카메라 화면의 왼쪽과 오른쪽 절반에 하나씩 오도록 표시합니다.

---

바이브 코딩으로 나만의 인터랙티브 전시 준비하기 · 박타미
