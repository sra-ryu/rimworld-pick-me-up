# Pick Me Up

A RimWorld 1.7 mod that marks hostile pawns who have the skills you are looking for with an arrow above their heads, to make recruiting easier.

## Demo

![Arrow markers above matching hostile pawns](docs/demo.png)

The condition window, opened with your own shortcut key:

![Condition window](docs/conditions.png)

## Features

- Pick any skills (modded skills included) and a minimum level for each one.
- Combine the conditions with **AND** ("Medicine and Intellectual at level 8 or higher") or **OR** ("Medicine or Intellectual at level 8 or higher").
- If a matching hostile pawn is on the map you are viewing, an arrow appears above their head.
- A condition window you can open while playing, plus a switch to hide the arrows without losing your conditions.
- No keyboard shortcut is assigned by default. You choose your own keys.
- Pawns who can never do a skill (for example, incapable of violence) do not match that skill. Prisoners and slaves of your colony are never marked.
- Languages: English, Korean.

## Tech Stack

- C# on .NET 10 (RimWorld 1.7 runs on .NET 10)
- RimWorld 1.7 game assemblies (`Assembly-CSharp`, `UnityStubs`), referenced only
- No Harmony or other mod dependencies
- Written with [Claude Code](https://claude.com/claude-code)

## Getting Started

Prerequisites: RimWorld 1.7 and the [.NET 10 SDK](https://dotnet.microsoft.com/download). The project file looks for the game in the default macOS Steam location. On other setups, pass the path with `-p:RimWorldDir=...`.

```bash
# 1. Clone into the game's Mods folder (or clone anywhere and symlink it there)
cd "$HOME/Library/Application Support/Steam/steamapps/common/RimWorld/Mods"
git clone https://github.com/<your-account>/rimworld-pick-me-up.git PickMeUp

# 2. Build. The DLL is written to Assemblies/
cd PickMeUp/Source/PickMeUp
dotnet build -c Release

# Different game location:
# dotnet build -c Release -p:RimWorldDir="/path/to/RimWorld"
```

Then start the game, enable **Pick Me Up** in the mod list and restart. The log should show `[Pick Me Up] Loaded.`.

The built `Assemblies/*.dll` and `*.pdb` files are not committed, so every fresh clone has to be built once.

## Usage

1. Open `Options > Keyboard config`, find **Pick Me Up** and assign a key to **Open/close condition window** (and optionally **Show/hide arrow markers**).
2. Press that key during play, click **Add condition**, choose a skill and drag the slider to the minimum level. Repeat for more skills.
3. Choose **Match all conditions (AND)** or **Match any condition (OR)**.
4. Matching hostile pawns get an arrow right away. The same editor is also available under `Options > Mod settings > Pick Me Up`.

For example, with `Shooting 10` and `Melee 10` set to OR, any raider who is level 10 or higher in either skill is marked. Switch to AND and only raiders who reach both are marked.

To check the matching without looking at the map, enable Development mode and run `Debug actions > Pick Me Up > Log matching hostile pawns`.

## Project Structure

```
About/About.xml                 Mod metadata (name, packageId, supported version)
Defs/KeyBindings.xml            Keyboard shortcut definitions
Languages/                      English (default) and Korean text
Textures/PickMeUp/Arrow.png     The arrow marker
Source/PickMeUp/
  PickMeUp.csproj               Builds into ../../Assemblies/
  PickMeUpMod.cs                Mod entry point and mod settings page
  PickMeUpSettings.cs           Saved settings (conditions, AND/OR, on/off)
  SkillCondition.cs             One "skill + minimum level" rule
  PawnMatcher.cs                Decides which hostile pawns match
  ArrowMarkerMapComponent.cs    Draws the arrows on the map
  ConditionEditor.cs            Condition UI shared by settings and window
  Window_Conditions.cs          The in-game condition window
  PickMeUpGameComponent.cs      Keyboard shortcut handling
  PickMeUpDebug.cs              Development-mode logging action
```

---

# 픽 미 업

## 소개

RimWorld 1.7용 모드입니다. 원하는 스킬을 가진 적대 림의 머리 위에 화살표 마크를 표시해서, 영입을 쉽게 할 수 있도록 합니다.

## 데모

스크린샷은 위 Demo 섹션을 참고하세요. 첫 번째는 조건에 맞는 적대 림 위에 뜬 화살표 마크이고, 두 번째는 조건 창입니다.

## 주요 기능

- 원하는 스킬(모드 스킬 포함)과 스킬별 최소 레벨을 지정합니다.
- 조건을 **AND**("의학과 연구가 모두 8레벨 이상") 또는 **OR**("의학 또는 연구가 8레벨 이상")로 묶을 수 있습니다.
- 지금 보고 있는 맵에 조건에 맞는 적대 림이 있다면 머리 위에 화살표가 표시됩니다.
- 플레이 중에 열 수 있는 조건 창과, 조건은 유지한 채 화살표만 끄는 스위치가 있습니다.
- 기본 단축키는 지정되어 있지 않으며, 직접 키를 정합니다.
- 해당 스킬을 아예 못 하는 림(예: 폭력 불가)은 그 스킬 조건에 맞지 않는 것으로 처리합니다. 정착지의 죄수와 노예는 표시하지 않습니다.
- 지원 언어: 영어, 한국어

## 기술 스택

- .NET 10 기반 C# (RimWorld 1.7은 .NET 10으로 실행됩니다)
- RimWorld 1.7 게임 어셈블리(`Assembly-CSharp`, `UnityStubs`) 참조 전용
- Harmony 등 다른 모드 의존성 없음
- [Claude Code](https://claude.com/claude-code)로 작성했습니다

## 시작하기

필요한 것은 RimWorld 1.7과 .NET 10 SDK입니다. 프로젝트 파일은 macOS 기본 Steam 경로에서 게임을 찾습니다. 다른 환경에서는 `-p:RimWorldDir=...`로 경로를 지정하세요. 명령어는 위 Getting Started와 동일합니다.

빌드한 뒤 게임에서 모드 목록의 **Pick Me Up**을 켜고 재시작하면 됩니다. `Assemblies/`의 `.dll`과 `.pdb`는 커밋하지 않으므로, 저장소를 새로 받으면 한 번 빌드해야 합니다.

## 사용 예시

1. `옵션 → 키보드 설정`에서 **Pick Me Up** 항목을 찾아 **조건 창 열기/닫기**에 키를 지정합니다. 화살표 마크 켜기/끄기 키도 원하면 지정합니다.
2. 플레이 중 그 키로 조건 창을 열고, **조건 추가**를 눌러 스킬을 고른 뒤 슬라이더로 최소 레벨을 정합니다. 스킬을 더 넣을 수도 있습니다.
3. **모든 조건 만족 (AND)** 또는 **하나라도 만족 (OR)** 중에서 고릅니다.
4. 조건에 맞는 적대 림에 바로 화살표가 표시됩니다. 같은 설정 화면이 `옵션 → 모드 설정 → Pick Me Up`에도 있습니다.

예를 들어 `사격 10`과 `격투 10`을 OR로 두면 둘 중 하나라도 10레벨 이상인 습격자가 표시됩니다. AND로 바꾸면 둘 다 10레벨 이상인 습격자만 표시됩니다.

## 프로젝트 구조

파일 구성은 위 Project Structure와 동일합니다.
