# 구조 개선 검토·구현 기록 — 2026-09-15

## 기준과 범위

- 실제 기본 브랜치는 `kg`: 로컬 HEAD와 원격 `Temporal_fix/HEAD`가 모두 `b9249101fad35a0dcdaca1e02112a1aaf0cc1210`이었다. 새 브랜치/worktree 없이 작업했다.
- 시작 당시 이미 1.9.16 버전 대응·ResourceMap·제작 툴팁 수정과 테스트/의존성 자료가 미커밋 상태였다. **검증 기준은 그 작업 트리 전체이며, 깨끗한 b924910 체크아웃이 아니다.** 기존 수정/미추적 파일 56개의 SHA-256을 기록하고 보존했다. 이번 코드 커밋에는 시작 당시 깨끗했던 두 파일만 포함했다.
- `C:/Users/blizz/.codex/AGENTS.md`, BUILDING.md, Patches/README.md, Tests/StructuralRegressionChecks.md, Tests/Valheim107.md, ServerSync provenance와 실제 코드/빌드 설정을 대조했다. 저장소와 상위 RiderProjects 경로에 추가 AGENTS.md는 발견하지 못했다.
- 요청에 적힌 `C:/Users/blizz.codex/...`는 실제 존재하는 `C:/Users/blizz/.codex/references/valheim/INDEX.md`로 확인했다. 1.0.7 대응 자료와 1.0.12 클라이언트/데디케이트 자료 중 해당 계약만 재사용했다. 새 수집·추출·지원 범위 변경은 하지 않았다.
- 현재 소스/manifest는 1.9.16. 이전 포팅 기준은 1.0.7이며, 설치 원본과 제공 실행 로그는 **Valheim 1.0.12**다. client build 25253764, dedicated build 25253791. 문서의 1.0.7 표기만으로 현재 실행 버전을 판단하지 않았다. 1.0.12 정적 검사 통과는 전체 게임 실행 지원 보증과 다르다.

실제 산출물은 `ValheimEnchantmentSystem.csproj`의 net48 `kg.ValheimEnchantmentSystem.dll` 하나다. `ValheimEnchantmentSystem : BaseUnityPlugin`의 Awake/Update/OnDestroy가 진입점이고 프리로더 패처가 아니다. Awake의 명시적 모듈 초기화와 실패 의존성 차단 후 `Startup/PatchRegistry`가 Harmony 클래스를 찾으며, ItemData/Localization/Skill/Piece 관리자 자체 등록 경로도 존재한다. 이 간접 진입 경로를 참조 검색과 구분했다.

컴파일 참조는 설치된 원본 Managed DLL이다. assembly_valheim SHA-256은 `27A766A8D23A7BD8B6A54FB9AD0452A96C305FB3629B39C40527C09A1C393A84`. publicize를 적용하지 않았고, 이번 변경은 게임 private 멤버 접근을 추가하지 않는다. `Platform/GameAccess.cs`의 기존 캐시된 접근자와 원본 메타데이터를 검사하는 기존 테스트를 유지했다.

ILRepack은 주 DLL에 ServerSync, fastJSON, YamlDotNet, JewelcraftingAPI를 병합한다. ServerSync는 기존 `valheim-1.0.7-r1`, SHA-256 `B4DD786997F4E90D770F09EF3E9D64154754FE7E8EDFB4841795751895B35846` 그대로다. 후처리 `pdb2mdb` 이후 Debug 타깃이 Steam BepInEx/plugins에 최종 DLL을 복사한다. Release에만 실행되는 ZIP 패키징은 호출하지 않았다.

설정 검토는 Configs의 공개 facade/직렬화, 개별 저장소, 권한에 따른 reload·poll·refresh와 변경 캐시를 포함했다. 리소스는 포함 목록과 소비 진입점을 확인했다: `Resources/kg_enchantment`, 런타임 로드되는 `VES_Scripts.dll`, 영어/한국어 번역, 아이콘. manifest의 필수 의존성은 BepInExPack 5.4.2350이며 현재 Jotunn 직접 참조/필수 선언은 없다. soft dependency인 Jewelcrafting, ArcaneWard, Blueprint, ExpandWorldData, AzuCraftyBoxes 및 별도 탐지되는 Auga/ZenUI 경계를 유지했다.

검토한 자사 코드 영역은 Startup/Platform의 등록·검증 경계, Configs, Services의 강화/재료/미리보기/경험치와 VFX 상태, Items_Structures의 등록/조합/스킬 스크롤, UI의 controller/view/query/입력 및 Integrations다. Managers와 Enchantment_Core는 관련 API·초기화·Harmony 진입 및 호출을 중심으로 검토했다. 모든 메서드의 의미를 전수 감사한 것은 아니다.

**제외/미검토:** bin/obj/packages/.tmp/배포 ZIP과 디컴파일 생성물은 구조 수정 대상에서 제외했다. 병합 외부 라이브러리·vendor 관리자 전체 내부 구현, assetbundle 내부 프리팹/셰이더 전체, VES_Scripts 전체 구현, 게임 DLL 전체 의미 분석, 모든 외부 모드의 리플렉션/후킹, Linux 및 실제 Unity·멀티플레이 실행은 전수 검토하지 않았다. 기존 자료와 빌드 결과로 확인한 범위를 실제 실행 검증으로 확대하지 않았다.

## 영역별 판단과 유지 결정

| 영역 | 판단과 근거 |
| --- | --- |
| 초기화·패치 등록 | 대체로 균형. 모듈 순서와 실패 차단은 주 플러그인, 탐색·역할/선택적 플러그인 필터는 PatchRegistry가 맡는다. 명시적 순서를 또 다른 일반 프레임워크로 옮길 실익이 작다. |
| 설정·요구사항 | 대체로 균형, 통계 조회에 국소 중복. SyncedData는 공개 설정/직렬화 facade이며 개별 저장소가 정책과 캐시를 소유한다. requirements의 수동 우선/잘못된 파일 건너뛰기와 stats/chance의 읽기 완료 후 게시 정책은 달라 범용 저장소로 합치지 않았다. |
| 강화 실행·재료·경험치 | 분리 유지. 표시, 확률 계산, 실제 소비, 경험치 계산은 검증 환경이 다르다. `9a251fd`의 AzuCraftyBoxes 추가에서 미리보기 변경이 재료 서비스 호출 한 줄에 머문 것은 경계가 변경 전파를 줄인 실제 사례다. |
| 조합·스킬 스크롤 | 일부 집중돼 있으나 유지. ScrollCombineService의 grid 색인/dirty 프레임/overlay 상태와 SkillScrollService의 요청 검증/소비 표식/보상/만료 수명은 서로 연결된다. 파일을 더 나누면 상태 전달과 정리 지점이 증가한다. |
| UI | 대체로 균형. controller의 입력/상태, view의 Unity 바인딩, query의 목록 생성 경계를 유지했다. `2ae164a`에서 무상태 Query 인스턴스와 중계가 이미 정리됐고 `9a251fd`에서 게임패드/드래그 변경은 관련 입력 경계에 배치됐다. |
| VFX·선택적 연동 | 분리 유지. ArmorStand의 owner metadata 쓰기, EquipmentWorldVfx의 읽기/표시, registry의 살아 있는 객체 관리가 다르다. Auga 행 예약, EWD 바이옴 이벤트, Jewelcrafting 데이터 처리도 같은 정책이 아니다. |

과도한 분리를 추가로 없앨 강한 근거는 발견하지 못했다. `Info_UI._view` 지역화나 레시피 상수 중계 정리는 가능하지만 실익이 작아 수행하지 않았다. 미리보기에서 요구사항을 재조회하는 부분도 조회 사이 외부 호출과 실제 UI 검증 비용을 고려해 유지했다. 공개 no-op IntegrationRegistry API와 VES_Autoload 특성은 외부 호출·Harmony·바이너리 계약이 될 수 있어 삭제하지 않았다.

## 구현한 두 단계

1. **`d9cc056` — 통계 조회 정책 공동 배치**
   - 파일/호출: `Configs/EnchantmentStatRepository.cs`, `SyncedData.GetStatIncrease(string,...) → GetStatIncrease`와 `SyncedData.IsLevelEnchantable → HasStatIncrease`.
   - 문제: 두 소비자가 입력 가드와 override→무기/방어구 기본값 조회를 각각 구현했다. 표시와 가능 판정이 따로 바뀔 수 있었다. `2ae164a`는 이미 로딩을 TryReloadAll에 모았으므로 로딩 정책은 건드리지 않았다.
   - 최소 변경: 같은 파일에 private `TryGetDefinedStatIncrease`를 두고 두 호출이 사용한다. 새 파일·인터페이스·캐시가 없다. 추가 private 호출 하나의 간접 비용 대신 조회 규칙의 변경 지점 하나를 줄였다. 성능 개선을 주장하지 않는다.
   - 보존/위험: null 값이어도 키 존재는 true, 없는 override 레벨은 기본값 fallback, null/공백 이름과 level<=0 가드를 유지했다. `GetStatIncrease(Enchanted)`는 level==0만 거부하고 override 성공 시 IsWeapon을 평가하지 않으므로 합치지 않았다. 구독·캐시 갱신과 공개 API를 유지했다.
   - 검증: 기존 규칙 테스트·Debug 빌드·정적 검사 통과. 변경 전후 실제 조회 메서드 본문을 평범한 메모리 저장소 holder에 넣은 격리 실험에서 504개 반환/예외 관찰값이 일치했다: 잘못된 입력, 대소문자, 무기/방어구, null 값/사전, override 레벨 누락, 변경/삭제 후 재조회. **이 실험은 최종 DLL의 ServerSync/Unity 초기화를 실행하는 테스트가 아니다.** 원본 최종 DLL 초기화를 일반 .NET 호스트에서 시도한 검사는 BepInEx/Harmony 환경 부족으로 진행되지 않아 통과 결과에 포함하지 않았다.

2. **`77fad82` — 레이아웃 갱신의 중간 List 제거**
   - 파일/호출: `UI/InfoPanelView.ForceCanvasLayout`, InfoPanelController.Render와 행 펼침/접힘 callback에서 호출한다.
   - 최소 변경: `GetComponentsInChildren<ContentSizeFitter>(true)`가 반환하는 배열을 그대로 두 번 순회한다. 기존 snapshot 수집→Canvas 갱신→전체 disable→전체 enable 순서를 보존한다. 두 순회를 하나로 합치지 않았다.
   - 효과/위험: 중간 List 생성·배열 복사·ForEach callback을 제거했다. 프레임마다의 작업은 아니며 시간/GC 개선량은 측정하지 않았다. 캐시가 없어 무효화·파괴 책임을 추가하지 않는다. 위험 지점은 layout callback 순서이므로 diff와 호출 관계로 대조했고 독립 읽기 전용 리뷰에서도 회귀 문제를 발견하지 못했다.
   - 검증: Debug 빌드, 최종 DLL 정적 검사와 원본/복사본 해시 일치. 실제 Unity layout 실행은 미검증이다.

각 단계는 수정→관련 검증→diff 검토→그 파일만 커밋 순서로 수행했다. 기존 버전 대응/제작 툴팁 수정과 기능 변경을 이번 커밋에 포함하지 않았다.

## 계약·성능 검토와 별도 후보

- 최종 DLL과 기준 작업 트리 DLL에서 공개/protected 선언, Harmony/BepInEx/역할 특성, embedded resource 해시 **2,555개 기록이 동일**했다. 서명/특성/리소스 비교이며 모든 외부 후킹과 런타임 동작 동일성의 증명은 아니다.
- 변경한 두 파일에는 RPC, 아이템 저장, Unity lifecycle 메시지, 설정 구독 변경이 없다. Harmony 대상·오버로드·우선순위·반환·__state·예외 처리 코드는 유지했다.
- 재료 소비의 IsUsable/컨테이너 권한 재검사는 미리보기와 다른 시점의 최신 상태 검증이다. owner는 사용자 권한과 다르며 ClaimOwnership은 원자적 잠금이 아니다. 불확실한 소비 후 재시도/환불을 넣지 않았다.
- SkillScrollService는 서버/peer/거리 검증과 소비 ZDO 표식 후 보상 RPC·파괴 순서다. 접속 해제/저장 타이밍의 보상 소실 가능성은 **추정 위험**이며 확인된 재현 결함이 아니다. ACK·재전송·소비 정책 변경은 별도 조사로 남겼다.
- ConfigReloadPoller는 Update에서 호출되지만 실제 파일 조사는 2초 간격이다. ScrollCombine dirty refresh와 InventoryOverlay의 지정 프레임 조건을 유지했다. UI는 표시/입력 변경에 따라 재생성되며 전부 매 프레임 재생성한다고 보지 않았다.
- 표시 중 EnchantmentButtonHint.LateUpdate의 문자열/현지화/크기 계산, overlay 갱신의 Find/GetComponent는 남아 있다. 키·언어·UI 모드 교체·요소 재생성의 무효화 조건을 확인하지 않은 새 캐시는 추가하지 않았다. registry의 ToArray는 삭제/콜백 중 순회 snapshot 역할이 있어 유지했다.
- Formatter.ShouldShow 삭제는 보류했다. 공개 mutable Stat_Data와 GetResistancePairs의 캐시 때문에 모두 0일 때 검사 생략은 캐시 생성 시점과 다음 출력에 영향을 준다. 반복 보호 코드라는 이유로 삭제할 수 없다.
- **조건부 기존 결함:** FormatDamageModifier가 게임 1.0.12의 SlightlyResistant/SlightlyWeak를 Normal로 표시할 수 있다. 원본 HitData enum과 SE_Stats의 별도 처리를 대조했다. 실제 게임 재현은 하지 않았고, 출력 동작 수정이므로 이번 구조 변경에 섞지 않았다.
- plugin OnDestroy는 Config 저장과 poller 종료를 수행한다. static 이벤트와 DontDestroyOnLoad UI의 동적 플러그인 unload/reload 전체 지원은 미검증이며 현재 누수로 확정하지 않았다.

## 검증 결과와 남은 확인

- 기준 작업 트리: Debug 빌드(DeployToGame=true), 기존 net48 규칙 검사, 클라이언트 정적 검사 모두 성공. 이번 변경 전 검사에서 기존 실패는 없었다. 이것이 앞서 보고된 모든 게임 오류가 해결됐다는 뜻은 아니다.
- 1단계: Debug 0 warnings/errors, 규칙 검사 통과, 조회 격리 비교 504개 일치, 정적 검사 통과.
- 2단계 최종 DLL: Debug 0 warnings/errors. client/dedicated **각각** Harmony 79개, member reference 708개, FieldRef 58개, IL 계약 9개, source binding 71개 검사에서 실패 0. 정적 검사는 원본 DLL을 읽으며 게임을 실행하거나 Harmony를 설치하지 않는다.
- 최종 DLL `bin/Debug/kg.ValheimEnchantmentSystem.dll`과 Steam `BepInEx/plugins/kg.ValheimEnchantmentSystem.dll` SHA-256은 모두 `C8776E34603C242AD84C135F8903E8DC16665E8B66A0B264EF6E913E7C5E03F5`.
- Gale 프로필, 데디케이트 설치 DLL, 모드 버전, Release ZIP, 업로더, 원격 push는 변경/실행하지 않았다.
- 기준 스냅샷·해시·조회 실험 스크립트·계약 목록은 로컬 ignored `.tmp/structure-review-20260915/`에 보관했다. 이 임시 실험을 새 production 추상화나 일반 테스트 프레임워크로 만들지 않았다.

실제 게임에서 남은 확인:

1. 정보창 검색/카테고리 변경/행 펼침·접힘/재진입에서 높이·스크롤·툴팁 배치 확인.
2. 무기/방어구의 override 유무, null/누락 레벨 및 YAML live reload 후 가능 판정·미리보기·실제 스탯 비교.
3. 앞선 제작 툴팁 수정의 Hammer/Scroll Station 회귀 확인은 계속 별도 필요하다.
4. 클라이언트·호스트·데디케이트의 설정 동기화, 소비·드롭·저장·재접속 및 선택적 모드 조합은 실제 실행하지 않았다. 기존 StructuralRegressionChecks.md와 Valheim107.md의 체크리스트를 유지한다.

코드 기준 커밋은 `b924910`, 이번 코드 최종 커밋은 `77fad82`이며 이 문서는 후속 기록 커밋이다. 이번 두 파일의 변경은 각각 독립적으로 되돌릴 수 있다. 최종 작업 트리에는 시작 시 존재했던 미커밋 변경을 그대로 남긴다. 테스트한 DLL은 그 기존 변경을 포함한 작업 트리에서 생성됐다.
