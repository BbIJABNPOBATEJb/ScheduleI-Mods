# S1StarterMod

Стартовый мод для **Schedule I** на [MelonLoader](https://github.com/LavaGang/MelonLoader).
Собирается под обе ветки игры из одного кода:

| Конфигурация | Ветка Steam | Рантайм | Файл |
|---|---|---|---|
| `Il2Cpp` | main (по умолчанию) | IL2CPP, `net6.0` | `S1StarterMod_Il2Cpp.dll` |
| `Mono` | `alternate` | Mono, `netstandard2.1` | `S1StarterMod_Mono.dll` |

## Что умеет

- **F8** в игровом мире — уведомление с наличными и игровым временем.
- `CashMultiplier` — пример Harmony-патча: множитель для всех положительных изменений наличных (по умолчанию `1` = ванилла).

Настройки: `<игра>/UserData/MelonPreferences.cfg`, секция `[S1StarterMod]` (появляется после первого запуска).

## Настройка окружения

1. .NET SDK 6+ (собирается и на SDK 9).
2. MelonLoader **0.7.3** в папке игры (0.7.1 не использовать — сломан для Schedule I).
   Для IL2CPP игру нужно один раз запустить: MelonLoader сгенерирует `MelonLoader/Il2CppAssemblies`, против которых компилируется мод.
3. Скопировать `local.build.props.example` → `local.build.props` и прописать пути к игре.
   Файл в `.gitignore`, у каждого разработчика свой.

## Сборка

```bash
dotnet build -c Il2Cpp
dotnet build -c Mono
```

В Rider — выбрать конфигурацию `Il2Cpp` или `Mono` в тулбаре. При `AutomateLocalDeployment=true`
dll после сборки копируется в `<игра>/Mods`. Перед сборкой закрой игру: загруженный мод держит файл.

Лог мода: `<игра>/MelonLoader/Latest.log`.

## Структура

```
src/
  Core.cs                  точка входа (MelonMod), хоткей
  ModInfo.cs               имя / версия / автор для MelonLoader
  ModConfig.cs             MelonPreferences
  GlobalUsings.cs          алиас S1 = Il2CppScheduleOne | ScheduleOne
  Patches/                 Harmony-патчи (применяются автоматически)
```

## Кросс-рантайм

- Игровые типы пиши через алиас `S1.`: `S1.Money.MoneyManager`, `S1.UI.NotificationsManager`.
- Различия рантаймов — через `#if IL2CPP` / `#else`.
- На IL2CPP: коллекции игры — `Il2CppSystem.Collections.Generic.*`, приведение типов — `obj.TryCast<T>()`,
  свои `MonoBehaviour` регистрируются через `ClassInjector.RegisterTypeInIl2Cpp<T>()`.

## Полезные инструменты

- **dnSpyEx / ILSpy** — чтение кода игры. В IL2CPP-сборках нет тел методов, поэтому логику читают
  по `Schedule I_Data/Managed/Assembly-CSharp.dll` из ветки `alternate`.
- **UnityExplorer** — инспектор сцены и объектов прямо в игре.
- **S1API** ([ifBars/S1API](https://github.com/ifBars/S1API)) — API для NPC, квестов, предметов, телефона и т.д.
