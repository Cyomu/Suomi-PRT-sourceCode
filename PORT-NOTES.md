# Порт S&M-PRT на SPT 4.1.2 — рабочие заметки

Ветка: `RadioDevDir\v4.1\` (копия исходников, ссылки на `E:\Games\EFT410`).
Оригинал под 4.0.13 в `RadioDevDir\RadioMod.Client` / `RadioMod.Server` не трогаем.

## Установка 4.1.x отличается структурой

| 4.0.13 | 4.1.2 |
|---|---|
| `SPT\user\mods\<mod>` | `SPT_Runtime\user\mods\<mod>` |
| `SPT\SPTarkov.*.dll` | `SPT_Runtime\SPTarkov.*.dll` |

## Сервер: карта изменений API (проверено декомпиляцией fika-server 2.4.0 под 4.1.2)

| Было (4.0.13) | Стало (4.1.2) |
|---|---|
| `net9.0` | **`net10.0`** (сервер собран на .NET 10) |
| `AbstractModMetadata` (abstract class, `override`) | `IModMetadata` (интерфейс, без `override`) |
| `IsBundleMod` в метаданных | удалён из контракта |
| `IOnLoad.OnLoad()` | `IOnLoad.OnLoadAsync(CancellationToken)` |
| `OnLoadOrder.PostDBModLoader` | удалён; порядок задаётся `[Injectable(TypePriority = ...)]` |
| `DatabaseService.GetTables().Templates.Items` | инъекция `TemplateTable` → `.Items` |
| `DatabaseService.GetTables().Traders` | инъекция `TradersTable` |
| `SPTarkov.Server.Core.Models.Utils.ISptLogger<T>` | `SPTarkov.Common.Models.Logging.ISptLogger<T>` |
| `LogTextColor` (enum) | `Spectre.Console.Color` |
| `SPTarkov.Server.Core.Services.Mod.CustomItemService` | `SPTarkov.Server.Core.Services.Modding.Custom.CustomItemService` |

Нужна ссылка на `SPT_Runtime\Spectre.Console.dll`.

## Клиент: переименования типов EFT (новая сборка сильно деобфусцирована)

Уже применено:

| Было | Стало |
|---|---|
| `ItemAttributeClass` | `EFT.InventoryLogic.ItemAttribute` |
| `InfoClass` (профиль) | `EFT.ProfileInfo` |
| `ISession` | `EFT.IClientSession` (`ClientApplication<T> where T : IClientSession`) |

Также применено (все найдены декомпиляцией новой сборки):

| Было | Стало |
|---|---|
| `ItemInfoInteractionsAbstractClass<EItemInfoButton>` | `EFT.UI.ContextInteractions<EItemInfoButton>` (от неё наследуется `BaseItemContextInteractions`, где и живут `IsActive`/`ExecuteInteractionInternal`) |
| `LocaleManagerClass.LocaleManagerClass?.String_1` | `EFT.LocalizationManager.Instance?._currentApplicationCulture` |
| `CurrentScreenSingletonClass.Instance` | `EFT.UI.Screens.EftScreenManager.Instance` |
| `GInterface495<EEftScreenType>` | `EFT.UI.Screens.IBaseScreenController<EEftScreenType>` |

Не менялись и работают: `ItemViewStats`, `EFT.UI.DragAndDrop.GridItemView`,
`EFT.UI.ItemSpecificationPanel`, `EFT.UI.CompactCharacteristicDropdownPanel`.

## Статус

- [x] Создана ветка `v4.1`, ссылки переключены на `E:\Games\EFT410`
- [x] Сервер: `net10.0`, метаданные под `IModMetadata`
- [x] Сервер: DI (`TemplateTable`/`TradersTable`/`LocationTable`), `OnLoadAsync`, `ISptLogger` из `SPTarkov.Common`,
      цвета через `Spectre.Console.Color` (алиас `ConsoleColour`, чтобы не конфликтовать с EFT-шным `Color`),
      обязательное `NewItemName` у всех 13 раций
- [x] **Сервер собирается под 4.1.2: 0 ошибок**
- [x] **Клиент собирается под 4.1.2: 0 ошибок**
- [x] Помечено как экспериментальное: имя мода `S&M-PRT (experimental)`, версия сервера `1.0.0-E`,
      в логах `1.0.0E (experimental, SPT 4.1)`. `[BepInPlugin]` остаётся `1.0.0` — BepInEx парсит
      версию через `System.Version` и букву не примет.
- [x] Развёрнуто в `E:\Games\EFT410` для теста (клиент + сервер + бандлы)
- [ ] Тест в игре: запуск сервера, появление раций у торговцев, связь в рейде
- [ ] Сборка релизного пакета под новую структуру папок (`SPT_Runtime\user\mods\`)

## Что нельзя проверить в 4.1 прямо сейчас

Мод на батарейки под 4.1 ещё не вышел, поэтому вся батареечная ветка кода останется неактивной
(режим «батареек нет»). Это штатное поведение, а не поломка.
