using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Mod;
namespace RadioMod.Server
{
    [Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
    public class RadioItemMod : IOnLoad
    {
        internal const string DisplayVersion = "0.9.6";
        private const string CompassTplId = "5f4f9eb969cdc30ff33f09db";
        private const string MultitoolsCategoryId = "66abb0743f4d8b145b1612c1";
        private const string SpecialEquipmentHandbookParentId = "5b47574386f77428ca22b345";
        internal const string BaofengTplId = "6d6f645f726164696f303031";
        internal const string AzartTplId = "6d6f645f726164696f303032";
        internal const string KenwoodTplId = "6d6f645f726164696f303033";
        internal const string T460TplId = "6d6f645f726164696f303034";
        internal const string YaesuTplId = "6d6f645f726164696f303035";
        internal const string Dp4800TplId = "6d6f645f726164696f303036";
        internal const string Dp4601eTplId = "6d6f645f726164696f303037";
        internal const string Xts5000TplId = "6d6f645f726164696f303038";
        internal const string HarrisTplId = "6d6f645f726164696f303039";
        internal const string Trc83TplId = "6d6f645f726164696f303130";
        internal const string AlincoTplId = "6d6f645f726164696f303131";
        internal const string KenwoodProTalkTplId = "6d6f645f726164696f303132";
        internal const string Mth800TplId = "6d6f645f726164696f303133";
        private const string MilitaryCircuitBoardTplId = "5d0376a486f7747d8050965c";
        private const string VirtexProcessorTplId = "5c05308086f7746b2101e90b";
        private const string RechargeableBatteryTplId = "590a358486f77429692b2790";
        private const string ElectronicComponentsTplId = "6389c70ca33d8c4cdf4932c6";
        private const string WireBundleTplId = "5c06779c86f77426e00dd782";
        private const string SkierTraderId = "58330581ace78e27b8b10cee";
        private const string PraporTraderId = "54cb50c76803fa8b248b4571";
        private const string MechanicTraderId = "5a7c2eca46aef81a7ca2145d";
        private const string JaegerTraderId = "5c0647fdd443bc2504c2d371";
        private const string PeacekeeperTraderId = "5935c25fb3acc3127c3d8cd9";
        private const string FenceTraderId = "579dc571d53a0658a154fbec";
        private const string RoublesTplId = "5449016a4bdc2d6f028b456f";
        private const string DollarsTplId = "5696686a4bdc2da3298b456a";
        private const string EurosTplId = "569668774bdc2da2298b4568";
        private const string MilitaryCableTplId = "5d0375ff86f774186372f685";
        private const string CofdmTransmitterTplId = "5c052f6886f7746b1e3db148";
        private const string GreenBatBatteryTplId = "5e2aedd986f7746d404f3aa4";
        private const string HuntingMatchesTplId = "5e2af2bc86f7746d3f3c33fc";
        private const string DryFuelTplId = "590a373286f774287540368b";
        private const string SecuredContainerCategoryId = "5448bf274bdc2dfc2f8b456a";
        private readonly CustomItemService _customItemService;
        private readonly DatabaseService _databaseService;
        private readonly ISptLogger<RadioItemMod> _logger;
        public RadioItemMod(CustomItemService customItemService, DatabaseService databaseService, ISptLogger<RadioItemMod> logger)
        {
            _customItemService = customItemService;
            _databaseService = databaseService;
            _logger = logger;
        }
        public Task OnLoad()
        {
            _customItemService.CreateItemFromClone(new NewItemFromCloneDetails
            {
                ItemTplToClone = CompassTplId,
                ParentId = MultitoolsCategoryId,
                NewId = BaofengTplId,
                HandbookParentId = SpecialEquipmentHandbookParentId,
                HandbookPriceRoubles = 8000,
                FleaPriceRoubles = 42690,
                OverrideProperties = new TemplateItemProperties
                {
                    Width = 1,
                    Height = 2,
                    Weight = 0.25,
                    Unlootable = false,
                    IsUndiscardable = false,
                    DiscardingBlock = false,
                    IsUngivable = false,
                    DiscardLimit = -1,
                    Prefab = new Prefab
                    {
                        Path = "prt-fika/baofeng",
                        Rcid = ""
                    },
                },
                Locales = new Dictionary<string, LocaleDetails>
                {
                    ["en"] = new LocaleDetails
                    {
                        Name = "Baofeng UV-5R portable radio",
                        ShortName = "UV-5R",
                        Description = "Range: 50m clear, degrading up to 275m, static-only up to 385m\n"
                            + "Duplex mode: Half-duplex\n"
                            + "Signal quality: Low\n\n"
                            + "A dual-band (VHF/UHF) radio that became a worldwide grassroots hit thanks to its ultra-low price and simplicity of use. It allows manual frequency tuning directly from the keypad, which makes it popular among hikers, hunters, and weekend cabin-goers. However, it has weak interference resistance and no encryption whatsoever, making it unsuitable for serious tasks. It's the ideal, cheap \"consumable\" for basic short-range communication."
                    },
                    ["ru"] = new LocaleDetails
                    {
                        Name = "Портативная рация Baofeng UV-5R",
                        ShortName = "UV-5R",
                        Description = "Дальность: 50м чистого приёма, деградация до 275м, шум до 385м\n"
                            + "Режим связи: полудуплекс\n"
                            + "Качество сигнала: низкое\n\n"
                            + "Двухдиапазонная рация (VHF/UHF), ставшая мировым народным хитом благодаря ультрабюджетной цене и простоте использования. Она позволяет настраивать частоты вручную прямо с клавиатуры, из-за чего популярна среди туристов, охотников и дачников. Однако у нее слабая защита от помех и полностью отсутствует шифрование, поэтому для серьезных задач она не подходит. Это идеальный и дешевый \"расходник\" для базовой связи на небольших расстояниях."
                    }
                }
            });
            _customItemService.CreateItemFromClone(new NewItemFromCloneDetails
            {
                ItemTplToClone = CompassTplId,
                ParentId = MultitoolsCategoryId,
                NewId = AzartTplId,
                HandbookParentId = SpecialEquipmentHandbookParentId,
                HandbookPriceRoubles = 45000,
                FleaPriceRoubles = 67000,
                OverrideProperties = new TemplateItemProperties
                {
                    Width = 1,
                    Height = 2,
                    Weight = 0.65,
                    Unlootable = false,
                    IsUndiscardable = false,
                    DiscardingBlock = false,
                    IsUngivable = false,
                    DiscardLimit = -1,
                    CanSellOnRagfair = false,
                    Prefab = new Prefab
                    {
                        Path = "prt-fika/azart",
                        Rcid = ""
                    },
                },
                Locales = new Dictionary<string, LocaleDetails>
                {
                    ["en"] = new LocaleDetails
                    {
                        Name = "R-187P1 \"Azart\" radio",
                        ShortName = "Azart",
                        Description = "Range: 700m clear, degrading up to 850m, static-only up to 1000m\n"
                            + "Duplex mode: Half-duplex | Simplex\n"
                            + "Signal quality: Medium-high\n\n"
                            + "A modern digital tactical radio of military design (SDR), created for reliable unit coordination in combat conditions. It provides the highest resistance to electronic warfare (EW) thanks to ultra-fast frequency hopping (FHSS) and secure encryption protocols. However, its closed communication standards make it practically incompatible with civilian radio equipment, and its considerable size and weight take some getting used to when worn on gear. It's a specialized, tactical-grade tool built for stable operation under active electronic countermeasures."
                    },
                    ["ru"] = new LocaleDetails
                    {
                        Name = "Радиостанция Р-187П1 «Азарт»",
                        ShortName = "Азарт",
                        Description = "Дальность: 700м чистого приёма, деградация до 850м, шум до 1000м\n"
                            + "Режим связи: Полудуплекс | Симплекс\n"
                            + "Качество сигнала: среднее-высокое\n\n"
                            + "Современная цифровая тактическая радиостанция военного образца (SDR), созданная для надежной координации подразделений в боевых условиях. Она обеспечивает высочайшую устойчивость к радиоэлектронной борьбе (РЭБ) благодаря сверхбыстрой псевдослучайной перестройке рабочей частоты (ППРЧ) и защищенным протоколам шифрования. Однако закрытые стандарты связи делают её практически несовместимой с гражданским радиооборудованием, а внушительные габариты и вес требуют привыкания при ношении на экипировке. Это специализированный инструмент тактического уровня, созданный для стабильной работы в условиях активного радиоэлектронного противодействия."
                    }
                }
            });
            _customItemService.CreateItemFromClone(new NewItemFromCloneDetails
            {
                ItemTplToClone = CompassTplId,
                ParentId = MultitoolsCategoryId,
                NewId = KenwoodTplId,
                HandbookParentId = SpecialEquipmentHandbookParentId,
                HandbookPriceRoubles = 3000,
                FleaPriceRoubles = 300000,
                OverrideProperties = new TemplateItemProperties
                {
                    Width = 1,
                    Height = 2,
                    Weight = 0.25,
                    Unlootable = false,
                    IsUndiscardable = false,
                    DiscardingBlock = false,
                    IsUngivable = false,
                    DiscardLimit = -1,
                    CanSellOnRagfair = false,
                    Prefab = new Prefab
                    {
                        Path = "prt-fika/kenwood",
                        Rcid = ""
                    },
                },
                Locales = new Dictionary<string, LocaleDetails>
                {
                    ["en"] = new LocaleDetails
                    {
                        Name = "Kenwood TH-21BT portable radio",
                        ShortName = "TH-21BT",
                        Description = "Range: 25m clear, degrading up to 175m, static-only up to 275m\n"
                            + "Duplex mode: Half-duplex\n"
                            + "Signal quality: Low\n\n"
                            + "This is an iconic Japanese radio from the mid-1980s for the 2-meter (VHF) band. In its time it stood out for its ultra-compact housing and its unusual mechanical frequency-selector dials. Today the model holds purely collector's value for retro-tech enthusiasts. For actual field use it is hopelessly outdated, falling far behind even the cheapest modern radios in both power and features."
                    },
                    ["ru"] = new LocaleDetails
                    {
                        Name = "Портативная рация Kenwood TH-21BT",
                        ShortName = "TH-21BT",
                        Description = "Дальность: 25м чистого приёма, деградация до 175м, шум до 275м\n"
                            + "Режим связи: полудуплекс\n"
                            + "Качество сигнала: низкое\n\n"
                            + "Это культовая японская рация середины 1980-х годов для 2-метрового диапазона (VHF). В свое время она поражала ультракомпактным корпусом и запоминалась необычным механическим выбором частоты с помощью колесиков. Сегодня модель представляет исключительно коллекционную ценность для любителей ретро-техники. Для реальной работы она безнадежно устарела, сильно уступая по мощности и функциям даже самым дешевым современным рациям."
                    }
                }
            });
            _customItemService.CreateItemFromClone(new NewItemFromCloneDetails
            {
                ItemTplToClone = CompassTplId,
                ParentId = MultitoolsCategoryId,
                NewId = T460TplId,
                HandbookParentId = SpecialEquipmentHandbookParentId,
                HandbookPriceRoubles = 10000,
                FleaPriceRoubles = 66900,
                OverrideProperties = new TemplateItemProperties
                {
                    Width = 1,
                    Height = 1,
                    Weight = 0.19,
                    Unlootable = false,
                    IsUndiscardable = false,
                    DiscardingBlock = false,
                    IsUngivable = false,
                    DiscardLimit = -1,
                    Prefab = new Prefab
                    {
                        Path = "prt-fika/t460",
                        Rcid = ""
                    },
                },
                Locales = new Dictionary<string, LocaleDetails>
                {
                    ["en"] = new LocaleDetails
                    {
                        Name = "Motorola Talkabout T460 portable radio",
                        ShortName = "T460",
                        Description = "Range: 100m clear, degrading up to 325m, static-only up to 475m\n"
                            + "Duplex mode: Half-duplex\n"
                            + "Signal quality: Low-medium\n\n"
                            + "A solid license-free FRS/GMRS radio for active hiking, fishing, and hunting. It features a splash-resistant housing (IP54), a built-in flashlight, and support for NOAA weather channels, making it a great choice for family trips. However, due to its fixed antenna and fixed low power output, its range and functionality are strictly capped by consumer-grade limits. It's a simple, reliable \"turn it on and use it\" device that requires no license or complex setup."
                    },
                    ["ru"] = new LocaleDetails
                    {
                        Name = "Портативная рация Motorola Talkabout T460",
                        ShortName = "T460",
                        Description = "Дальность: 100м чистого приёма, деградация до 325м, шум до 475м\n"
                            + "Режим связи: полудуплекс\n"
                            + "Качество сигнала: низкое-среднее\n\n"
                            + "Добротная безлицензионная рация стандарта FRS/GMRS для активного туризма, рыбалки и охоты. Она имеет влагозащищенный корпус (IP54), встроенный фонарик и поддержку погодных каналов NOAA, что делает ее отличным выбором для семейных походов. Однако из-за несъемной антенны и фиксированной малой мощности дальность связи и функционал жестко ограничены бытовыми рамками. Это простой и надежный прибор формата «включил и пользуйся», который не требует лицензий и сложных настроек."
                    }
                }
            });
            _customItemService.CreateItemFromClone(new NewItemFromCloneDetails
            {
                ItemTplToClone = CompassTplId,
                ParentId = MultitoolsCategoryId,
                NewId = YaesuTplId,
                HandbookParentId = SpecialEquipmentHandbookParentId,
                HandbookPriceRoubles = 20000,
                FleaPriceRoubles = 110537,
                OverrideProperties = new TemplateItemProperties
                {
                    Width = 1,
                    Height = 2,
                    Weight = 0.24,
                    Unlootable = false,
                    IsUndiscardable = false,
                    DiscardingBlock = false,
                    IsUngivable = false,
                    DiscardLimit = -1,
                    Prefab = new Prefab
                    {
                        Path = "prt-fika/yaesu",
                        Rcid = ""
                    },
                },
                Locales = new Dictionary<string, LocaleDetails>
                {
                    ["en"] = new LocaleDetails
                    {
                        Name = "Yaesu VX-8DR portable radio",
                        ShortName = "VX-8DR",
                        Description = "Range: 150m clear, degrading up to 400m, static-only up to 515m\n"
                            + "Duplex mode: Half-duplex | Simplex\n"
                            + "Signal quality: Medium\n\n"
                            + "A feature-packed, premium-class quad-band transceiver for survivalists, extreme sports enthusiasts, and advanced ham radio operators. It stands out with a rugged, dust- and water-resistant housing (IPX7), a built-in barometer, and support for APRS/GPS navigation functions for transmitting coordinates. However, its abundance of features makes the menu extremely difficult for an unprepared user to configure, and its high price is limited to an analog-only communication format. It's the ultimate, reliable device for those who value maximum self-sufficiency and durability in the wilderness."
                    },
                    ["ru"] = new LocaleDetails
                    {
                        Name = "Портативная рация Yaesu VX-8DR",
                        ShortName = "VX-8DR",
                        Description = "Дальность: 150м чистого приёма, деградация до 400м, шум до 515м\n"
                            + "Режим связи: полудуплекс | симплекс\n"
                            + "Качество сигнала: среднее\n\n"
                            + "Навороченный четырехдиапазонный трансивер премиум-класса для выживальщиков, экстремалов и продвинутых радиолюбителей. Он выделяется сверхпрочным пылевлагозащищенным корпусом (IPX7), встроенным барометром и поддержкой навигационных функций APRS/GPS для передачи координат. Однако обилие функций делает меню крайне сложным для настройки неподготовленным пользователем, а высокая цена ограничивается исключительно аналоговым форматом связи. Это ультимативный и надежный прибор для тех, кому важна максимальная автономность и живучесть в дикой природе."
                    }
                }
            });
            _customItemService.CreateItemFromClone(new NewItemFromCloneDetails
            {
                ItemTplToClone = CompassTplId,
                ParentId = MultitoolsCategoryId,
                NewId = Dp4800TplId,
                HandbookParentId = SpecialEquipmentHandbookParentId,
                HandbookPriceRoubles = 35000,
                FleaPriceRoubles = 40000,
                OverrideProperties = new TemplateItemProperties
                {
                    Width = 1,
                    Height = 2,
                    Weight = 0.355,
                    Unlootable = false,
                    IsUndiscardable = false,
                    DiscardingBlock = false,
                    IsUngivable = false,
                    DiscardLimit = -1,
                    CanSellOnRagfair = false,
                    Prefab = new Prefab
                    {
                        Path = "prt-fika/dp4800",
                        Rcid = ""
                    },
                },
                Locales = new Dictionary<string, LocaleDetails>
                {
                    ["en"] = new LocaleDetails
                    {
                        Name = "Motorola DP4800 portable radio",
                        ShortName = "DP4800",
                        Description = "Range: 350m clear, degrading up to 525m, static-only up to 650m\n"
                            + "Duplex mode: Half-duplex\n"
                            + "Signal quality: Medium-high\n\n"
                            + "The gold standard of professional digital DMR communication for the commercial sector, security services, and heavy-duty operating conditions. It's equipped with crystal-clear audio backed by intelligent noise suppression, a full keypad, a color display, and the highest level of housing protection under the IP68 standard. However, operating and configuring it requires complex professional PC programming, and its high price and size make it excessive for simple everyday use. It's an uncompromising, ultra-reliable working tool for building secure communication networks within an enterprise."
                    },
                    ["ru"] = new LocaleDetails
                    {
                        Name = "Портативная рация Motorola DP4800",
                        ShortName = "DP4800",
                        Description = "Дальность: 350м чистого приёма, деградация до 525м, шум до 650м\n"
                            + "Режим связи: полудуплекс\n"
                            + "Качество сигнала: среднее-высокое\n\n"
                            + "Золотой стандарт профессиональной цифровой связи DMR для коммерческого сектора, служб безопасности и тяжелых условий эксплуатации. Она оснащена кристально чистым звуком с интеллектуальной системой шумоподавления, полноценной клавиатурой, цветным дисплеем и имеет максимальную защиту корпуса по стандарту IP68. Однако для ее работы и настройки требуется сложное профессиональное программирование через ПК, а высокая цена и габариты делают ее избыточной для простого бытового использования. Это бескомпромиссный и сверхнадежный рабочий инструмент для построения защищенных сетей связи на предприятиях."
                    }
                }
            });
            _customItemService.CreateItemFromClone(new NewItemFromCloneDetails
            {
                ItemTplToClone = CompassTplId,
                ParentId = MultitoolsCategoryId,
                NewId = Dp4601eTplId,
                HandbookParentId = SpecialEquipmentHandbookParentId,
                HandbookPriceRoubles = 42000,
                FleaPriceRoubles = 152400,
                OverrideProperties = new TemplateItemProperties
                {
                    Width = 1,
                    Height = 2,
                    Weight = 0.36,
                    Unlootable = false,
                    IsUndiscardable = false,
                    DiscardingBlock = false,
                    IsUngivable = false,
                    DiscardLimit = -1,
                    CanSellOnRagfair = false,
                    Prefab = new Prefab
                    {
                        Path = "prt-fika/dp4601e",
                        Rcid = ""
                    },
                },
                Locales = new Dictionary<string, LocaleDetails>
                {
                    ["en"] = new LocaleDetails
                    {
                        Name = "Motorola DP4601e portable radio",
                        ShortName = "DP4601e",
                        Description = "Range: 300m clear, degrading up to 500m, static-only up to 625m\n"
                            + "Duplex mode: Half-duplex | Simplex\n"
                            + "Signal quality: High\n\n"
                            + "An advanced version of the DP4800, a professional digital DMR radio with built-in GPS and Bluetooth modules. It delivers crystal-clear audio, robust AES-256 encryption, full IP68 water resistance, and precise field-position tracking. However, like the entire MotoTRBO lineup, it requires complex computer-based configuration and is too expensive for casual everyday use. It's a reliable, technologically advanced tool for team coordination and security operations in the harshest conditions."
                    },
                    ["ru"] = new LocaleDetails
                    {
                        Name = "Портативная рация Motorola DP4601e",
                        ShortName = "DP4601e",
                        Description = "Дальность: 300м чистого приёма, деградация до 500м, шум до 625м\n"
                            + "Режим связи: полудуплекс | симплекс\n"
                            + "Качество сигнала: высокое\n\n"
                            + "Продвинутая версия модели DP4800, представляющая собой профессиональную цифровую DMR-рацию со встроенными модулями GPS и Bluetooth. Она обеспечивает кристально чистый звук, стойкое шифрование AES-256, полную влагозащиту корпуса по стандарту IP68 и возможность точного позиционирования на местности. Однако, как и вся линейка MotoTRBO, она требует сложной настройки через компьютер и является слишком дорогой для простого бытового использования. Это надежный и технологичный инструмент для координации команд и работы служб безопасности в самых тяжелых условиях."
                    }
                }
            });
            _customItemService.CreateItemFromClone(new NewItemFromCloneDetails
            {
                ItemTplToClone = CompassTplId,
                ParentId = MultitoolsCategoryId,
                NewId = Xts5000TplId,
                HandbookParentId = SpecialEquipmentHandbookParentId,
                HandbookPriceRoubles = 55000,
                FleaPriceRoubles = 62000,
                OverrideProperties = new TemplateItemProperties
                {
                    Width = 1,
                    Height = 2,
                    Weight = 0.54,
                    Unlootable = false,
                    IsUndiscardable = false,
                    DiscardingBlock = false,
                    IsUngivable = false,
                    DiscardLimit = -1,
                    CanSellOnRagfair = false,
                    Prefab = new Prefab
                    {
                        Path = "prt-fika/xts5000",
                        Rcid = ""
                    },
                },
                Locales = new Dictionary<string, LocaleDetails>
                {
                    ["en"] = new LocaleDetails
                    {
                        Name = "Motorola XTS5000 portable radio",
                        ShortName = "XTS5000",
                        Description = "Range: 450m clear, degrading up to 625m, static-only up to 750m\n"
                            + "Duplex mode: Half-duplex | Simplex\n"
                            + "Signal quality: High-very high\n\n"
                            + "A professional radio using the APCO P25 digital standard, developed for police, special services, and military agencies. It stands out for its phenomenal military-grade physical durability, excellent audio quality under heavy interference, and support for reliable hardware encryption. However, its substantial size, considerable weight, and archaic programming software make it a challenging device for an average user to master. It's a nearly indestructible, uncompromising \"brick\" for those who need maximum equipment durability and conversation privacy."
                    },
                    ["ru"] = new LocaleDetails
                    {
                        Name = "Портативная рация Motorola XTS5000",
                        ShortName = "XTS5000",
                        Description = "Дальность: 450м чистого приёма, деградация до 625м, шум до 750м\n"
                            + "Режим связи: полудуплекс | симплекс\n"
                            + "Качество сигнала: высокое-оч.высокое\n\n"
                            + "Профессиональная радиостанция цифрового стандарта APCO P25, разработанная для полиции, спецслужб и военных ведомств. Она отличается феноменальной физической прочностью по военным стандартам, отличным качеством звука в тяжелых условиях помех и поддержкой надежного аппаратного шифрования. Однако внушительные габариты, приличный вес и архаичный софт для программирования делают ее освоение сложным испытанием для обычного пользователя. Это практически неуничтожимый и бескомпромиссный «кирпич» для тех, кому критически важна максимальная живучесть оборудования и конфиденциальность переговоров."
                    }
                }
            });
            _customItemService.CreateItemFromClone(new NewItemFromCloneDetails
            {
                ItemTplToClone = CompassTplId,
                ParentId = MultitoolsCategoryId,
                NewId = HarrisTplId,
                HandbookParentId = SpecialEquipmentHandbookParentId,
                HandbookPriceRoubles = 90000,
                FleaPriceRoubles = 100000,
                OverrideProperties = new TemplateItemProperties
                {
                    Width = 1,
                    Height = 2,
                    Weight = 1.18,
                    Unlootable = false,
                    IsUndiscardable = false,
                    DiscardingBlock = false,
                    IsUngivable = false,
                    DiscardLimit = -1,
                    CanSellOnRagfair = false,
                    Prefab = new Prefab
                    {
                        Path = "prt-fika/harris",
                        Rcid = ""
                    },
                },
                Locales = new Dictionary<string, LocaleDetails>
                {
                    ["en"] = new LocaleDetails
                    {
                        Name = "Harris AN/PRC-152 portable radio",
                        ShortName = "AN/PRC-152",
                        Description = "Range: 600m clear, degrading up to 775m, static-only up to 900m\n"
                            + "Duplex mode: Half-duplex | Simplex\n"
                            + "Signal quality: Perfect\n\n"
                            + "A legendary military-standard tactical radio, developed for special forces and armed forces units. It stands out for its enormous frequency range, satellite communication (SATCOM) support, extremely powerful military-grade encryption, and a nearly indestructible sealed housing. However, its exorbitant cost, strict legal ownership restrictions, and extreme setup complexity make it practically unattainable for ordinary users. It's the ultimate, uncompromising communication tool, built exclusively for operation in the harshest combat conditions."
                    },
                    ["ru"] = new LocaleDetails
                    {
                        Name = "Портативная рация Harris AN/PRC-152",
                        ShortName = "AN/PRC-152",
                        Description = "Дальность: 600м чистого приёма, деградация до 775м, шум до 900м\n"
                            + "Режим связи: полудуплекс | симплекс\n"
                            + "Качество сигнала: идеальное\n\n"
                            + "Легендарная тактическая радиостанция военного стандарта, разработанная для специальных подразделений и вооруженных сил. Она отличается огромным диапазоном частот, поддержкой спутниковой связи (SATCOM), мощнейшим шифрованием военного уровня и практически неубиваемым герметичным корпусом. Однако запредельная стоимость, строгие юридические ограничения на владение и крайняя сложность в настройке делают ее практически недосягаемой для обычных пользователей. Это ультимативный и бескомпромиссный инструмент связи, созданный исключительно для работы в самых суровых боевых условиях."
                    }
                }
            });
            _customItemService.CreateItemFromClone(new NewItemFromCloneDetails
            {
                ItemTplToClone = CompassTplId,
                ParentId = MultitoolsCategoryId,
                NewId = Trc83TplId,
                HandbookParentId = SpecialEquipmentHandbookParentId,
                HandbookPriceRoubles = 189456,
                FleaPriceRoubles = 227347,
                OverrideProperties = new TemplateItemProperties
                {
                    Width = 1,
                    Height = 2,
                    Weight = 0.6,
                    Unlootable = false,
                    IsUndiscardable = false,
                    DiscardingBlock = false,
                    IsUngivable = false,
                    DiscardLimit = -1,
                    CanSellOnRagfair = false,
                    Prefab = new Prefab
                    {
                        Path = "prt-fika/trc83",
                        Rcid = ""
                    },
                },
                Locales = new Dictionary<string, LocaleDetails>
                {
                    ["en"] = new LocaleDetails
                    {
                        Name = "Realistic TRC-83 portable radio",
                        ShortName = "TRC-83",
                        Description = "Range: 30m clear, degrading up to 200m, static-only up to 300m\n"
                            + "Duplex mode: Half-duplex\n"
                            + "Signal quality: Very low\n\n"
                            + "A vintage three-channel Citizens Band (CB) radio from the 1980s. Bulky, simple, and reliable as a brick. It features a long telescoping antenna and runs on AA batteries. Due to its outdated analog circuitry and limited channel selection it's unsuitable for serious tactical operations, but as a cheap communication tool for beginners it works flawlessly."
                    },
                    ["ru"] = new LocaleDetails
                    {
                        Name = "Портативная рация Realistic TRC-83",
                        ShortName = "TRC-83",
                        Description = "Дальность: 30м чистого приёма, деградация до 200м, шум до 300м\n"
                            + "Режим связи: полудуплекс\n"
                            + "Качество сигнала: оч. низкое\n\n"
                            + "Винтажная трехканальная радиостанция гражданского диапазона (CB) родом из 1980-х. Массивная, простая и надежная, как утюг. Она оборудована длинной телескопической антенной и питается от батареек типа АА. Из-за устаревшей аналоговой схемы и ограниченного выбора каналов не подходит для серьезных тактических операций, но как дешевое средство связи для новичков — работает безотказно."
                    }
                }
            });
            _customItemService.CreateItemFromClone(new NewItemFromCloneDetails
            {
                ItemTplToClone = CompassTplId,
                ParentId = MultitoolsCategoryId,
                NewId = AlincoTplId,
                HandbookParentId = SpecialEquipmentHandbookParentId,
                HandbookPriceRoubles = 42700,
                FleaPriceRoubles = 51240,
                OverrideProperties = new TemplateItemProperties
                {
                    Width = 1,
                    Height = 2,
                    Weight = 0.17,
                    Unlootable = false,
                    IsUndiscardable = false,
                    DiscardingBlock = false,
                    IsUngivable = false,
                    DiscardLimit = -1,
                    Prefab = new Prefab
                    {
                        Path = "prt-fika/alinco",
                        Rcid = ""
                    },
                },
                Locales = new Dictionary<string, LocaleDetails>
                {
                    ["en"] = new LocaleDetails
                    {
                        Name = "Alinco (Fake) portable radio",
                        ShortName = "Alinco",
                        Description = "Range: 75m clear, degrading up to 300m, static-only up to 400m\n"
                            + "Duplex mode: Half-duplex\n"
                            + "Signal quality: Very low - Low\n\n"
                            + "A cheap Chinese radio with a hastily slapped-on logo of the Japanese brand Alinco. The specific model can't be identified, since the original manufacturer never made anything like it. Despite its dubious origin and rough plastic build, this no-name device works reliably enough on civilian frequencies. A simple, affordable communication option you won't feel bad about losing."
                    },
                    ["ru"] = new LocaleDetails
                    {
                        Name = "Портативная рация Alinco (Fake)",
                        ShortName = "Alinco",
                        Description = "Дальность: 75м чистого приёма, деградация до 300м, шум до 400м\n"
                            + "Режим связи: полудуплекс\n"
                            + "Качество сигнала: оч.низкое - низкое\n\n"
                            + "Дешевая китайская рация с наспех наклеенным логотипом японского бренда Alinco. Определить конкретную модель невозможно, так как оригинальный производитель никогда не выпускал подобных устройств. Несмотря на сомнительное происхождение и грубый пластик, этот безымянный прибор вполне исправно работает на гражданских частотах. Простой и доступный вариант для связи, который не жалко потерять."
                    }
                }
            });
            _customItemService.CreateItemFromClone(new NewItemFromCloneDetails
            {
                ItemTplToClone = CompassTplId,
                ParentId = MultitoolsCategoryId,
                NewId = KenwoodProTalkTplId,
                HandbookParentId = SpecialEquipmentHandbookParentId,
                HandbookPriceRoubles = 66675,
                FleaPriceRoubles = 80010,
                OverrideProperties = new TemplateItemProperties
                {
                    Width = 1,
                    Height = 1,
                    Weight = 0.16,
                    Unlootable = false,
                    IsUndiscardable = false,
                    DiscardingBlock = false,
                    IsUngivable = false,
                    DiscardLimit = -1,
                    Prefab = new Prefab
                    {
                        Path = "prt-fika/kenwoodprotalk",
                        Rcid = ""
                    },
                },
                Locales = new Dictionary<string, LocaleDetails>
                {
                    ["en"] = new LocaleDetails
                    {
                        Name = "Kenwood ProTalk XLS portable radio",
                        ShortName = "ProTalk XLS",
                        Description = "Range: 90m clear, degrading up to 295m, static-only up to 425m\n"
                            + "Duplex mode: Half-duplex | Simplex\n"
                            + "Signal quality: Medium\n\n"
                            + "A lightweight, compact UHF-band radio, originally designed for business and service industries. It has a convenient LCD screen, simple controls, and a built-in vibration alert. Due to its low transmitter power and fully plastic housing it isn't built for serious combat, but it's a great affordable radio for short-range work."
                    },
                    ["ru"] = new LocaleDetails
                    {
                        Name = "Портативная рация Kenwood ProTalk XLS",
                        ShortName = "ProTalk XLS",
                        Description = "Дальность: 90м чистого приёма, деградация до 295м, шум до 425м\n"
                            + "Режим связи: полудуплекс | симплекс\n"
                            + "Качество сигнала: среднее\n\n"
                            + "Легкая и компактная рация дециметрового диапазона (UHF), изначально созданная для бизнеса и сферы услуг. Имеет удобный ЖК-экран, простое управление и встроенный вибровызов. Из-за невысокой мощности передатчика и полностью пластикового корпуса не рассчитана на серьезный бой, но отлично подходит как доступная рация для работы на коротких дистанциях."
                    }
                }
            });
            _customItemService.CreateItemFromClone(new NewItemFromCloneDetails
            {
                ItemTplToClone = CompassTplId,
                ParentId = MultitoolsCategoryId,
                NewId = Mth800TplId,
                HandbookParentId = SpecialEquipmentHandbookParentId,
                HandbookPriceRoubles = 74350,
                FleaPriceRoubles = 89220,
                OverrideProperties = new TemplateItemProperties
                {
                    Width = 1,
                    Height = 1,
                    Weight = 0.2,
                    Unlootable = false,
                    IsUndiscardable = false,
                    DiscardingBlock = false,
                    IsUngivable = false,
                    DiscardLimit = -1,
                    Prefab = new Prefab
                    {
                        Path = "prt-fika/mth800",
                        Rcid = ""
                    },
                },
                Locales = new Dictionary<string, LocaleDetails>
                {
                    ["en"] = new LocaleDetails
                    {
                        Name = "Motorola MTH800 portable radio",
                        ShortName = "MTH800",
                        Description = "Range: 175m clear, degrading up to 425m, static-only up to 490m\n"
                            + "Duplex mode: Half-duplex | Simplex\n"
                            + "Signal quality: Low-medium\n\n"
                            + "A digital TETRA-standard radio, built for emergency services and police. Rugged, dust- and water-resistant, with a color display and encryption support. In the reality of Tarkov, it's a great transitional step up from simple civilian \"chatterboxes\" toward closed professional communication. Its secure digital signal is practically impossible to intercept with an ordinary frequency scanner."
                    },
                    ["ru"] = new LocaleDetails
                    {
                        Name = "Портативная рация Motorola MTH800",
                        ShortName = "MTH800",
                        Description = "Дальность: 175м чистого приёма, деградация до 425м, шум до 490м\n"
                            + "Режим связи: полудуплекс | симплекс\n"
                            + "Качество сигнала: низкое-среднее\n\n"
                            + "Цифровая радиостанция стандарта TETRA, созданная для экстренных служб и полиции. Прочная, пылевлагозащищенная, с цветным дисплеем и поддержкой шифрования. В реалиях Таркова — это отличный переходный шаг от простых гражданских \"болталок\" к закрытой профессиональной связи. Её защищенный цифровой сигнал практически невозможно перехватить обычным сканером частот."
                    }
                }
            });
            AddRadiosToTraders();
            int patchedContainers = ExcludeFromSecuredContainers();
            LogStartupBanner(patchedContainers);
            return Task.CompletedTask;
        }
        private void LogStartupBanner(int patchedContainers)
        {
            const int width = 48;
            string?[] lines =
            {
                "PRT " + DisplayVersion,
                null,
                "Radios loaded: 13 (C -> S tier)",
                "Flea-market banned: 7 radios",
                "Excluded from secured containers: " + patchedContainers,
            };
            _logger.LogWithColor("╔" + new string('═', width) + "╗", LogTextColor.Cyan);
            foreach (string? line in lines)
            {
                if (line == null)
                {
                    _logger.LogWithColor("╠" + new string('═', width) + "╣", LogTextColor.Cyan);
                    continue;
                }
                _logger.LogWithColor("║ " + line.PadRight(width - 1) + "║", LogTextColor.White);
            }
            _logger.LogWithColor("╚" + new string('═', width) + "╝", LogTextColor.Cyan);
            _logger.Success("[PRT] Load completed successfully");
        }
        private int ExcludeFromSecuredContainers()
        {
            var items = _databaseService.GetTables().Templates.Items;
            var radioIds = new HashSet<MongoId> { new MongoId(BaofengTplId), new MongoId(AzartTplId), new MongoId(KenwoodTplId), new MongoId(T460TplId), new MongoId(YaesuTplId), new MongoId(Dp4800TplId), new MongoId(Dp4601eTplId), new MongoId(Xts5000TplId), new MongoId(HarrisTplId), new MongoId(Trc83TplId), new MongoId(AlincoTplId), new MongoId(KenwoodProTalkTplId), new MongoId(Mth800TplId) };
            int patched = 0;
            foreach (TemplateItem item in items.Values)
            {
                if (item.Parent != SecuredContainerCategoryId || item.Properties?.Grids == null)
                {
                    continue;
                }
                foreach (Grid grid in item.Properties.Grids)
                {
                    foreach (GridFilter filter in grid.Properties.Filters)
                    {
                        filter.ExcludedFilter ??= new HashSet<MongoId>();
                        foreach (MongoId radioId in radioIds)
                        {
                            filter.ExcludedFilter.Add(radioId);
                        }
                    }
                }
                patched++;
            }
            return patched;
        }
        private void AddRadiosToTraders()
        {
            TraderAssort skierAssort = _databaseService.GetTables().Traders[new MongoId(SkierTraderId)].Assort;
            AddAssortEntry(skierAssort, "6d6f645f7261646961737369", BaofengTplId, 35575, 500, 800, 4, 1);
            TraderAssort praporAssort = _databaseService.GetTables().Traders[new MongoId(PraporTraderId)].Assort;
            AddAssortEntry(praporAssort, "6d6f645f72616469617a6172", AzartTplId, 284275, 60, 86, 1, 4);
            AddBarterAssortEntry(praporAssort, "6d6f645f617a617274626172", AzartTplId, 4, 70, 85, 2,
                new List<List<(string TplId, int Count)>>
                {
                    new List<(string, int)>
                    {
                        (MilitaryCableTplId, 2),
                        (MilitaryCircuitBoardTplId, 1),
                        (CofdmTransmitterTplId, 1),
                        (GreenBatBatteryTplId, 2)
                    }
                });
            TraderAssort mechanicAssort = _databaseService.GetTables().Traders[new MongoId(MechanicTraderId)].Assort;
            AddAssortEntry(mechanicAssort, "6d6f645f726164696b656e77", KenwoodTplId, 250000, 1, 13, 1, 1);
            TraderAssort jaegerAssort = _databaseService.GetTables().Traders[new MongoId(JaegerTraderId)].Assort;
            AddAssortEntry(jaegerAssort, "6d6f645f7261646974343630", T460TplId, 55750, 400, 600, 3, 1);
            AddAssortEntry(skierAssort, "6d6f645f7261646979616573", YaesuTplId, 583, 300, 500, 3, 2, EurosTplId);
            AddAssortEntry(skierAssort, "6d6f645f7261646964703438", Dp4800TplId, 908, 100, 200, 2, 3, EurosTplId);
            TraderAssort peacekeeperAssort = _databaseService.GetTables().Traders[new MongoId(PeacekeeperTraderId)].Assort;
            AddAssortEntry(peacekeeperAssort, "6d6f645f7261646964703436", Dp4601eTplId, 1000, 150, 175, 2, 3, DollarsTplId);
            AddAssortEntry(peacekeeperAssort, "6d6f645f7261646978747335", Xts5000TplId, 1350, 70, 100, 2, 4, DollarsTplId);
            AddBarterAssortEntry(peacekeeperAssort, "6d6f645f7261646968617272", HarrisTplId, 4, 25, 50, 1,
                new List<List<(string TplId, int Count)>>
                {
                    new List<(string, int)>
                    {
                        (MilitaryCircuitBoardTplId, 1),
                        (VirtexProcessorTplId, 2),
                        (RechargeableBatteryTplId, 1),
                        (ElectronicComponentsTplId, 5),
                        (WireBundleTplId, 2)
                    }
                });
            AddBarterAssortEntry(peacekeeperAssort, "6d6f645f686172726973617a", HarrisTplId, 4, 35, 90, 2,
                new List<List<(string TplId, int Count)>>
                {
                    new List<(string, int)>
                    {
                        (AzartTplId, 2)
                    }
                });
            AddAssortEntry(mechanicAssort, "6d6f645f7261646974726338", Trc83TplId, 189456, 1, 7, 1, 2);
            TraderAssort fenceAssort = _databaseService.GetTables().Traders[new MongoId(FenceTraderId)].Assort;
            AddAssortEntry(fenceAssort, "6d6f645f72616469696c696e", AlincoTplId, 42700, 40, 100, 4, 1);
            AddBarterAssortEntry(fenceAssort, "6d6f645f616c696e63627472", AlincoTplId, 1, 20, 50, 3,
                new List<List<(string TplId, int Count)>>
                {
                    new List<(string, int)>
                    {
                        (BaofengTplId, 1)
                    }
                });
            AddAssortEntry(peacekeeperAssort, "6d6f645f7261646970726f74", KenwoodProTalkTplId, 525, 100, 200, 3, 2, DollarsTplId);
            AddAssortEntry(praporAssort, "6d6f645f726164696d746838", Mth800TplId, 74350, 100, 175, 3, 2);
            AddBarterAssortEntry(jaegerAssort, "6d6f645f6a6d746838627472", Mth800TplId, 2, 10, 20, 2,
                new List<List<(string TplId, int Count)>>
                {
                    new List<(string, int)>
                    {
                        (HuntingMatchesTplId, 1),
                        (DryFuelTplId, 1)
                    }
                });
        }
        private static readonly System.Random StockRng = new System.Random();
        private static void AddAssortEntry(TraderAssort assort, string assortEntryId, string radioTplId, int price, int stockMin, int stockMax, int buyRestrictionMax, int loyaltyLevel, string currencyTplId = RoublesTplId)
        {
            var entryId = new MongoId(assortEntryId);
            assort.Items.Add(new Item
            {
                Id = entryId,
                Template = new MongoId(radioTplId),
                ParentId = "hideout",
                SlotId = "hideout",
                Upd = new Upd
                {
                    UnlimitedCount = false,
                    StackObjectsCount = StockRng.Next(stockMin, stockMax + 1),
                    BuyRestrictionMax = buyRestrictionMax,
                    BuyRestrictionCurrent = 0
                }
            });
            assort.BarterScheme[entryId] = new List<List<BarterScheme>>
            {
                new List<BarterScheme>
                {
                    new BarterScheme
                    {
                        Count = price,
                        Template = new MongoId(currencyTplId)
                    }
                }
            };
            assort.LoyalLevelItems[entryId] = loyaltyLevel;
        }
        private static void AddBarterAssortEntry(TraderAssort assort, string assortEntryId, string radioTplId, int loyaltyLevel, int stockMin, int stockMax, int buyRestrictionMax, List<List<(string TplId, int Count)>> barterOptions)
        {
            var entryId = new MongoId(assortEntryId);
            assort.Items.Add(new Item
            {
                Id = entryId,
                Template = new MongoId(radioTplId),
                ParentId = "hideout",
                SlotId = "hideout",
                Upd = new Upd
                {
                    UnlimitedCount = false,
                    StackObjectsCount = StockRng.Next(stockMin, stockMax + 1),
                    BuyRestrictionMax = buyRestrictionMax,
                    BuyRestrictionCurrent = 0
                }
            });
            assort.BarterScheme[entryId] = barterOptions
                .Select(option => option
                    .Select(component => new BarterScheme
                    {
                        Count = component.Count,
                        Template = new MongoId(component.TplId)
                    })
                    .ToList())
                .ToList();
            assort.LoyalLevelItems[entryId] = loyaltyLevel;
        }
    }
}
