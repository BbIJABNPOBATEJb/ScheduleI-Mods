// Game namespaces are prefixed with "Il2Cpp" on IL2CPP (Il2CppScheduleOne.Money) and plain on Mono
// (ScheduleOne.Money). The S1 alias hides the difference: S1.Money.MoneyManager, S1.UI.HUD, ...
// Same for TextMeshPro: Il2CppTMPro vs TMPro.
#if IL2CPP
global using S1 = Il2CppScheduleOne;
global using Il2CppTMPro;
#else
global using S1 = ScheduleOne;
global using TMPro;
#endif
