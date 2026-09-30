// Игровые неймспейсы на IL2CPP имеют префикс Il2Cpp (Il2CppScheduleOne.Money),
// на Mono — нет (ScheduleOne.Money). Алиас S1 скрывает разницу:
//   S1.Money.MoneyManager, S1.UI.NotificationsManager, S1.DevUtilities.Singleton<T> ...
#if IL2CPP
global using S1 = Il2CppScheduleOne;
#else
global using S1 = ScheduleOne;
#endif
