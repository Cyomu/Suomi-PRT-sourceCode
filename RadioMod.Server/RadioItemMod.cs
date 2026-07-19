using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Server;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Mod;
using SPTarkov.Server.Core.Utils.Json;

namespace RadioMod.Server
{

    [Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
    public class RadioItemMod : IOnLoad
    {
        internal const string DisplayVersion = "0.9.7";

        private const string CompassTplId = "5f4f9eb969cdc30ff33f09db";

        private const string ValuableCloneBaseTplId = "573474f924597738002c6174";
        private const string MultitoolsCategoryId = "66abb0743f4d8b145b1612c1";

        private const string BarterValuablesCategoryId = "57864a3d24597754843f8721";

        private const string BarterHandbookParentId = "5b47574386f77428ca22b2f1";

        private const string HallOfFameContainerLvl1TplId = "63dbd45917fff4dee40fe16e";
        private const string HallOfFameContainerLvl2TplId = "65424185a57eea37ed6562e9";
        private const string HallOfFameContainerLvl3TplId = "6542435ea57eea37ed6562f0";
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

            bool fikaInstalled = IsFikaServerInstalled();
            string radioParentId = fikaInstalled ? MultitoolsCategoryId : BarterValuablesCategoryId;
            string radioCloneBase = fikaInstalled ? CompassTplId : ValuableCloneBaseTplId;
            string radioHandbookParentId = fikaInstalled ? SpecialEquipmentHandbookParentId : BarterHandbookParentId;

            string radioBackgroundColor = fikaInstalled ? "orange" : "violet";

            string radioRarityPvE = "Superrare";

            _customItemService.CreateItemFromClone(new NewItemFromCloneDetails
            {
                ItemTplToClone = radioCloneBase,
                ParentId = radioParentId,
                NewId = BaofengTplId,
                HandbookParentId = radioHandbookParentId,
                HandbookPriceRoubles = 25500,

                FleaPriceRoubles = 42690,
                OverrideProperties = new TemplateItemProperties
                {
                    Width = 1,
                    Height = 2,

                    Weight = 0.25,

                    BackgroundColor = radioBackgroundColor,
                    RarityPvE = radioRarityPvE,
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
                    },
                    ["ge"] = new LocaleDetails
                    {
                        Name = "Baofeng UV-5R Tragbares Funkgerät",
                        ShortName = "UV-5R",
                        Description = "Reichweite: 50 m klar, Verschlechterung bis 275 m, nur Rauschen bis 385 m\n"
                            + "Duplex-Modus: Halbduplex\n"
                            + "Signalqualität: Niedrig\n\n"
                            + "Ein Zweiband-Funkgerät (VHF/UHF), das dank seines extrem niedrigen Preises und seiner einfachen Bedienung weltweit zum Kult wurde. Es erlaubt die manuelle Frequenzeinstellung direkt über die Tastatur, weshalb es bei Wanderern, Jägern und Wochenendausflüglern beliebt ist. Allerdings hat es eine schwache Störfestigkeit und überhaupt keine Verschlüsselung, was es für ernsthafte Einsätze ungeeignet macht. Es ist der ideale, billige „Verbrauchsartikel\" für einfache Kurzstreckenkommunikation."
                    },
                    ["es"] = new LocaleDetails
                    {
                        Name = "Radio portátil Baofeng UV-5R",
                        ShortName = "UV-5R",
                        Description = "Alcance: 50 m claro, degradándose hasta 275 m, solo estática hasta 385 m\n"
                            + "Modo dúplex: Semidúplex\n"
                            + "Calidad de señal: Baja\n\n"
                            + "Una radio de doble banda (VHF/UHF) que se convirtió en un éxito popular mundial gracias a su precio ultrabajo y su sencillez de uso. Permite la sintonización manual de frecuencias directamente desde el teclado, lo que la hace popular entre excursionistas, cazadores y visitantes de cabañas de fin de semana. Sin embargo, tiene una resistencia a interferencias débil y carece por completo de cifrado, lo que la hace inadecuada para tareas serias. Es el \"consumible\" barato ideal para comunicaciones básicas de corto alcance."
                    },
                    ["fr"] = new LocaleDetails
                    {
                        Name = "Radio portable Baofeng UV-5R",
                        ShortName = "UV-5R",
                        Description = "Portée : 50 m clair, dégradation jusqu'à 275 m, uniquement de la friture jusqu'à 385 m\n"
                            + "Mode duplex : Semi-duplex\n"
                            + "Qualité du signal : Faible\n\n"
                            + "Une radio bibande (VHF/UHF) devenue un succès populaire mondial grâce à son prix ultra-bas et sa simplicité d'utilisation. Elle permet le réglage manuel des fréquences directement depuis le clavier, ce qui la rend populaire auprès des randonneurs, chasseurs et vacanciers du week-end. Cependant, elle offre une faible résistance aux interférences et aucun chiffrement, ce qui la rend inadaptée aux tâches sérieuses. C'est le \"consommable\" bon marché idéal pour les communications basiques à courte portée."
                    },
                    ["pl"] = new LocaleDetails
                    {
                        Name = "Radiotelefon przenośny Baofeng UV-5R",
                        ShortName = "UV-5R",
                        Description = "Zasięg: 50 m czysty odbiór, pogorszenie do 275 m, tylko szum do 385 m\n"
                            + "Tryb dupleksu: Półdupleks\n"
                            + "Jakość sygnału: Niska\n\n"
                            + "Dwuzakresowy (VHF/UHF) radiotelefon, który dzięki bardzo niskiej cenie i prostocie obsługi stał się światowym hitem wśród zwykłych użytkowników. Umożliwia ręczne strojenie częstotliwości bezpośrednio z klawiatury, przez co jest popularny wśród turystów, myśliwych i bywalców weekendowych wypadów. Ma jednak słabą odporność na zakłócenia i zupełny brak szyfrowania, co czyni go nieodpowiednim do poważnych zadań. To idealny, tani \"materiał eksploatacyjny\" do podstawowej łączności na krótkim dystansie."
                    },
                    ["it"] = new LocaleDetails
                    {
                        Name = "Radio portatile Baofeng UV-5R",
                        ShortName = "UV-5R",
                        Description = "Portata: 50 m chiaro, degrado fino a 275 m, solo statica fino a 385 m\n"
                            + "Modalità duplex: Semiduplex\n"
                            + "Qualità del segnale: Bassa\n\n"
                            + "Una radio a doppia banda (VHF/UHF) diventata un successo mondiale grazie al suo prezzo ultra basso e alla semplicità d'uso. Consente la sintonizzazione manuale delle frequenze direttamente dal tastierino, il che la rende popolare tra escursionisti, cacciatori e frequentatori di baite nel weekend. Tuttavia ha una scarsa resistenza alle interferenze e nessuna cifratura, il che la rende inadatta a compiti seri. È il \"materiale di consumo\" economico ideale per le comunicazioni di base a corto raggio."
                    },
                    ["cz"] = new LocaleDetails
                    {
                        Name = "Přenosná vysílačka Baofeng UV-5R",
                        ShortName = "UV-5R",
                        Description = "Dosah: 50 m čistý příjem, zhoršení do 275 m, pouze šum do 385 m\n"
                            + "Duplexní režim: Poloduplexní\n"
                            + "Kvalita signálu: Nízká\n\n"
                            + "Dvoupásmová (VHF/UHF) vysílačka, která se díky extrémně nízké ceně a jednoduchosti použití stala celosvětovým hitem mezi běžnými uživateli. Umožňuje ruční ladění frekvence přímo z klávesnice, díky čemuž je oblíbená mezi turisty, lovci a chataři. Má však slabou odolnost proti rušení a naprosto žádné šifrování, což ji činí nevhodnou pro vážné úkoly. Je to ideální levný \"spotřební materiál\" pro základní komunikaci na krátkou vzdálenost."
                    }
                }
            });

            _customItemService.CreateItemFromClone(new NewItemFromCloneDetails
            {
                ItemTplToClone = radioCloneBase,
                ParentId = radioParentId,
                NewId = AzartTplId,
                HandbookParentId = radioHandbookParentId,
                HandbookPriceRoubles = 150000,
                FleaPriceRoubles = 67000,
                OverrideProperties = new TemplateItemProperties
                {
                    Width = 1,
                    Height = 2,

                    Weight = 0.65,
                    BackgroundColor = radioBackgroundColor,
                    RarityPvE = radioRarityPvE,
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
                            + "Duplex mode: Half-duplex | Duplex\n"
                            + "Signal quality: Medium-high\n\n"
                            + "A modern digital tactical radio of military design (SDR), created for reliable unit coordination in combat conditions. It provides the highest resistance to electronic warfare (EW) thanks to ultra-fast frequency hopping (FHSS) and secure encryption protocols. However, its closed communication standards make it practically incompatible with civilian radio equipment, and its considerable size and weight take some getting used to when worn on gear. It's a specialized, tactical-grade tool built for stable operation under active electronic countermeasures."
                    },
                    ["ru"] = new LocaleDetails
                    {
                        Name = "Радиостанция Р-187П1 «Азарт»",
                        ShortName = "Азарт",
                        Description = "Дальность: 700м чистого приёма, деградация до 850м, шум до 1000м\n"
                            + "Режим связи: Полудуплекс | Дуплекс\n"
                            + "Качество сигнала: среднее-высокое\n\n"
                            + "Современная цифровая тактическая радиостанция военного образца (SDR), созданная для надежной координации подразделений в боевых условиях. Она обеспечивает высочайшую устойчивость к радиоэлектронной борьбе (РЭБ) благодаря сверхбыстрой псевдослучайной перестройке рабочей частоты (ППРЧ) и защищенным протоколам шифрования. Однако закрытые стандарты связи делают её практически несовместимой с гражданским радиооборудованием, а внушительные габариты и вес требуют привыкания при ношении на экипировке. Это специализированный инструмент тактического уровня, созданный для стабильной работы в условиях активного радиоэлектронного противодействия."
                    },
                    ["ge"] = new LocaleDetails
                    {
                        Name = "R-187P1 „Asart\" Funkgerät",
                        ShortName = "Asart",
                        Description = "Reichweite: 700 m klar, Verschlechterung bis 850 m, nur Rauschen bis 1000 m\n"
                            + "Duplex-Modus: Halbduplex | Duplex\n"
                            + "Signalqualität: Mittel-hoch\n\n"
                            + "Eine moderne digitale taktische Funkstation militärischer Bauart (SDR), entwickelt für die zuverlässige Koordination von Einheiten unter Gefechtsbedingungen. Sie bietet dank ultraschnellem Frequenzsprungverfahren (FHSS) und sicheren Verschlüsselungsprotokollen höchste Widerstandsfähigkeit gegen elektronische Kampfführung (EloKa). Allerdings machen ihre geschlossenen Kommunikationsstandards sie praktisch inkompatibel mit ziviler Funktechnik, und ihre beträchtliche Größe und ihr Gewicht erfordern etwas Gewöhnung beim Tragen an der Ausrüstung. Es ist ein spezialisiertes Werkzeug auf taktischer Ebene, gebaut für stabilen Betrieb unter aktiver elektronischer Gegenmaßnahme."
                    },
                    ["es"] = new LocaleDetails
                    {
                        Name = "Radio R-187P1 «Azart»",
                        ShortName = "Azart",
                        Description = "Alcance: 700 m claro, degradándose hasta 850 m, solo estática hasta 1000 m\n"
                            + "Modo dúplex: Semidúplex | Dúplex\n"
                            + "Calidad de señal: Media-alta\n\n"
                            + "Una radio táctica digital moderna de diseño militar (SDR), creada para la coordinación fiable de unidades en condiciones de combate. Ofrece la máxima resistencia a la guerra electrónica (EW) gracias al salto de frecuencia ultrarrápido (FHSS) y a protocolos de cifrado seguros. Sin embargo, sus estándares de comunicación cerrados la hacen prácticamente incompatible con equipos de radio civiles, y su considerable tamaño y peso requieren cierta adaptación al llevarla en el equipo. Es una herramienta especializada de nivel táctico, construida para un funcionamiento estable bajo contramedidas electrónicas activas."
                    },
                    ["fr"] = new LocaleDetails
                    {
                        Name = "Radio R-187P1 « Azart »",
                        ShortName = "Azart",
                        Description = "Portée : 700 m clair, dégradation jusqu'à 850 m, uniquement de la friture jusqu'à 1000 m\n"
                            + "Mode duplex : Semi-duplex | Duplex\n"
                            + "Qualité du signal : Moyenne-élevée\n\n"
                            + "Une radio tactique numérique moderne de conception militaire (SDR), créée pour une coordination fiable des unités en conditions de combat. Elle offre la plus haute résistance à la guerre électronique (EW) grâce au saut de fréquence ultra-rapide (FHSS) et à des protocoles de chiffrement sécurisés. Cependant, ses normes de communication fermées la rendent pratiquement incompatible avec les équipements radio civils, et sa taille et son poids considérables demandent une certaine adaptation lorsqu'elle est portée sur l'équipement. C'est un outil spécialisé de niveau tactique, conçu pour un fonctionnement stable sous contre-mesures électroniques actives."
                    },
                    ["pl"] = new LocaleDetails
                    {
                        Name = "Radiostacja R-187P1 „Azart\"",
                        ShortName = "Azart",
                        Description = "Zasięg: 700 m czysty odbiór, pogorszenie do 850 m, tylko szum do 1000 m\n"
                            + "Tryb dupleksu: Półdupleks | Dupleks\n"
                            + "Jakość sygnału: Średnia-wysoka\n\n"
                            + "Nowoczesna cyfrowa radiostacja taktyczna wojskowej konstrukcji (SDR), stworzona do niezawodnej koordynacji oddziałów w warunkach bojowych. Zapewnia najwyższą odporność na walkę radioelektroniczną (WRE) dzięki ultraszybkiemu przeskakiwaniu częstotliwości (FHSS) i bezpiecznym protokołom szyfrowania. Jednak zamknięte standardy łączności czynią ją praktycznie niekompatybilną ze sprzętem cywilnym, a spory rozmiar i waga wymagają przyzwyczajenia przy noszeniu na ekwipunku. To wyspecjalizowane narzędzie klasy taktycznej, zbudowane do stabilnej pracy w warunkach aktywnego przeciwdziałania elektronicznego."
                    },
                    ["it"] = new LocaleDetails
                    {
                        Name = "Radio R-187P1 \"Azart\"",
                        ShortName = "Azart",
                        Description = "Portata: 700 m chiaro, degrado fino a 850 m, solo statica fino a 1000 m\n"
                            + "Modalità duplex: Semiduplex | Duplex\n"
                            + "Qualità del segnale: Media-alta\n\n"
                            + "Una moderna radio tattica digitale di concezione militare (SDR), creata per il coordinamento affidabile delle unità in condizioni di combattimento. Offre la massima resistenza alla guerra elettronica (EW) grazie al salto di frequenza ultra rapido (FHSS) e a protocolli di cifratura sicuri. Tuttavia, i suoi standard di comunicazione chiusi la rendono praticamente incompatibile con le apparecchiature radio civili, e le sue notevoli dimensioni e il peso richiedono un po' di abitudine quando indossata sull'equipaggiamento. È uno strumento specializzato di livello tattico, costruito per un funzionamento stabile sotto contromisure elettroniche attive."
                    },
                    ["cz"] = new LocaleDetails
                    {
                        Name = "Vysílačka R-187P1 „Azart\"",
                        ShortName = "Azart",
                        Description = "Dosah: 700 m čistý příjem, zhoršení do 850 m, pouze šum do 1000 m\n"
                            + "Duplexní režim: Poloduplexní | Duplexní\n"
                            + "Kvalita signálu: Střední-vysoká\n\n"
                            + "Moderní digitální taktická vysílačka vojenské konstrukce (SDR), vytvořená pro spolehlivou koordinaci jednotek v bojových podmínkách. Poskytuje nejvyšší odolnost vůči elektronickému boji (EW) díky ultrarychlému skákání kmitočtu (FHSS) a zabezpečeným šifrovacím protokolům. Uzavřené komunikační standardy ji však činí prakticky nekompatibilní s civilním rádiovým vybavením a značná velikost a hmotnost vyžadují určité zvykání při nošení na výstroji. Jde o specializovaný nástroj taktické úrovně, postavený pro stabilní provoz při aktivním elektronickém rušení."
                    }
                }
            });

            _customItemService.CreateItemFromClone(new NewItemFromCloneDetails
            {
                ItemTplToClone = radioCloneBase,
                ParentId = radioParentId,
                NewId = KenwoodTplId,
                HandbookParentId = radioHandbookParentId,
                HandbookPriceRoubles = 215750,

                FleaPriceRoubles = 300000,
                OverrideProperties = new TemplateItemProperties
                {
                    Width = 1,
                    Height = 2,

                    Weight = 0.25,
                    BackgroundColor = radioBackgroundColor,
                    RarityPvE = radioRarityPvE,
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
                    },
                    ["ge"] = new LocaleDetails
                    {
                        Name = "Kenwood TH-21BT Tragbares Funkgerät",
                        ShortName = "TH-21BT",
                        Description = "Reichweite: 25 m klar, Verschlechterung bis 175 m, nur Rauschen bis 275 m\n"
                            + "Duplex-Modus: Halbduplex\n"
                            + "Signalqualität: Niedrig\n\n"
                            + "Dies ist ein legendäres japanisches Funkgerät aus der Mitte der 1980er Jahre für das 2-Meter-Band (VHF). Zu seiner Zeit stach es durch sein ultrakompaktes Gehäuse und seine ungewöhnlichen mechanischen Frequenzwahlräder hervor. Heute besitzt das Modell rein sammlerischen Wert für Retro-Technik-Enthusiasten. Für den tatsächlichen Feldeinsatz ist es hoffnungslos veraltet und fällt sowohl bei Leistung als auch bei Funktionen weit hinter selbst die billigsten modernen Funkgeräte zurück."
                    },
                    ["es"] = new LocaleDetails
                    {
                        Name = "Radio portátil Kenwood TH-21BT",
                        ShortName = "TH-21BT",
                        Description = "Alcance: 25 m claro, degradándose hasta 175 m, solo estática hasta 275 m\n"
                            + "Modo dúplex: Semidúplex\n"
                            + "Calidad de señal: Baja\n\n"
                            + "Esta es una icónica radio japonesa de mediados de los años 80 para la banda de 2 metros (VHF). En su época destacaba por su carcasa ultracompacta y sus inusuales diales mecánicos de selección de frecuencia. Hoy en día el modelo tiene un valor puramente coleccionable para los entusiastas de la tecnología retro. Para uso real sobre el terreno está irremediablemente obsoleta, quedando muy por detrás incluso de las radios modernas más baratas tanto en potencia como en funciones."
                    },
                    ["fr"] = new LocaleDetails
                    {
                        Name = "Radio portable Kenwood TH-21BT",
                        ShortName = "TH-21BT",
                        Description = "Portée : 25 m clair, dégradation jusqu'à 175 m, uniquement de la friture jusqu'à 275 m\n"
                            + "Mode duplex : Semi-duplex\n"
                            + "Qualité du signal : Faible\n\n"
                            + "Il s'agit d'une radio japonaise emblématique du milieu des années 1980 pour la bande des 2 mètres (VHF). À son époque, elle se distinguait par son boîtier ultra-compact et ses curieux cadrans mécaniques de sélection de fréquence. Aujourd'hui, ce modèle n'a plus qu'une valeur de collection pour les amateurs de technologie rétro. Pour un usage réel sur le terrain, elle est irrémédiablement dépassée, loin derrière même les radios modernes les moins chères, tant en puissance qu'en fonctionnalités."
                    },
                    ["pl"] = new LocaleDetails
                    {
                        Name = "Radiotelefon przenośny Kenwood TH-21BT",
                        ShortName = "TH-21BT",
                        Description = "Zasięg: 25 m czysty odbiór, pogorszenie do 175 m, tylko szum do 275 m\n"
                            + "Tryb dupleksu: Półdupleks\n"
                            + "Jakość sygnału: Niska\n\n"
                            + "To kultowy japoński radiotelefon z połowy lat 80. na pasmo 2 metrów (VHF). W swoich czasach wyróżniał się ultrakompaktową obudową i nietypowymi mechanicznymi pokrętłami wyboru częstotliwości. Dziś model ma czysto kolekcjonerską wartość dla entuzjastów retro-techniki. Do faktycznego użytku w terenie jest beznadziejnie przestarzały, ustępując znacznie nawet najtańszym współczesnym radiotelefonom pod względem mocy i funkcji."
                    },
                    ["it"] = new LocaleDetails
                    {
                        Name = "Radio portatile Kenwood TH-21BT",
                        ShortName = "TH-21BT",
                        Description = "Portata: 25 m chiaro, degrado fino a 175 m, solo statica fino a 275 m\n"
                            + "Modalità duplex: Semiduplex\n"
                            + "Qualità del segnale: Bassa\n\n"
                            + "Questa è un'iconica radio giapponese della metà degli anni '80 per la banda dei 2 metri (VHF). Ai suoi tempi si distingueva per la scocca ultracompatta e le insolite manopole meccaniche di selezione della frequenza. Oggi il modello ha un valore puramente da collezione per gli appassionati di retro-tecnologia. Per l'uso reale sul campo è irrimediabilmente superata, restando molto indietro anche rispetto alle radio moderne più economiche sia in potenza che in funzionalità."
                    },
                    ["cz"] = new LocaleDetails
                    {
                        Name = "Přenosná vysílačka Kenwood TH-21BT",
                        ShortName = "TH-21BT",
                        Description = "Dosah: 25 m čistý příjem, zhoršení do 175 m, pouze šum do 275 m\n"
                            + "Duplexní režim: Poloduplexní\n"
                            + "Kvalita signálu: Nízká\n\n"
                            + "Toto je kultovní japonská vysílačka z poloviny 80. let pro pásmo 2 metry (VHF). Ve své době vynikala ultrakompaktním pouzdrem a neobvyklými mechanickými voliči kmitočtu. Dnes má model čistě sběratelskou hodnotu pro nadšence do retro techniky. Pro skutečné použití v terénu je beznadějně zastaralá a výrazně zaostává i za nejlevnějšími moderními vysílačkami jak výkonem, tak funkcemi."
                    }
                }
            });

            _customItemService.CreateItemFromClone(new NewItemFromCloneDetails
            {
                ItemTplToClone = radioCloneBase,
                ParentId = radioParentId,
                NewId = T460TplId,
                HandbookParentId = radioHandbookParentId,
                HandbookPriceRoubles = 40000,

                FleaPriceRoubles = 66900,
                OverrideProperties = new TemplateItemProperties
                {
                    Width = 1,
                    Height = 1,

                    Weight = 0.19,
                    BackgroundColor = radioBackgroundColor,
                    RarityPvE = radioRarityPvE,
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
                    },
                    ["ge"] = new LocaleDetails
                    {
                        Name = "Motorola Talkabout T460 Tragbares Funkgerät",
                        ShortName = "T460",
                        Description = "Reichweite: 100 m klar, Verschlechterung bis 325 m, nur Rauschen bis 475 m\n"
                            + "Duplex-Modus: Halbduplex\n"
                            + "Signalqualität: Niedrig-mittel\n\n"
                            + "Ein solides lizenzfreies FRS/GMRS-Funkgerät für aktives Wandern, Angeln und Jagen. Es verfügt über ein spritzwassergeschütztes Gehäuse (IP54), eine eingebaute Taschenlampe und Unterstützung für NOAA-Wetterkanäle, was es zu einer großartigen Wahl für Familienausflüge macht. Aufgrund der fest montierten Antenne und der festen niedrigen Sendeleistung sind Reichweite und Funktionalität jedoch strikt auf Verbraucherniveau begrenzt. Es ist ein einfaches, zuverlässiges „Einschalten und benutzen\"-Gerät, das keine Lizenz oder komplizierte Einrichtung erfordert."
                    },
                    ["es"] = new LocaleDetails
                    {
                        Name = "Radio portátil Motorola Talkabout T460",
                        ShortName = "T460",
                        Description = "Alcance: 100 m claro, degradándose hasta 325 m, solo estática hasta 475 m\n"
                            + "Modo dúplex: Semidúplex\n"
                            + "Calidad de señal: Baja-media\n\n"
                            + "Una sólida radio FRS/GMRS sin licencia para senderismo activo, pesca y caza. Cuenta con una carcasa resistente a salpicaduras (IP54), una linterna incorporada y soporte para los canales meteorológicos NOAA, lo que la convierte en una gran opción para viajes familiares. Sin embargo, debido a su antena fija y su potencia de salida baja fija, su alcance y funcionalidad están estrictamente limitados a los estándares de consumo. Es un dispositivo simple y fiable de \"enciende y usa\" que no requiere licencia ni configuración compleja."
                    },
                    ["fr"] = new LocaleDetails
                    {
                        Name = "Radio portable Motorola Talkabout T460",
                        ShortName = "T460",
                        Description = "Portée : 100 m clair, dégradation jusqu'à 325 m, uniquement de la friture jusqu'à 475 m\n"
                            + "Mode duplex : Semi-duplex\n"
                            + "Qualité du signal : Faible-moyenne\n\n"
                            + "Une radio FRS/GMRS solide et sans licence pour la randonnée active, la pêche et la chasse. Elle dispose d'un boîtier résistant aux éclaboussures (IP54), d'une lampe torche intégrée et prend en charge les canaux météo NOAA, ce qui en fait un excellent choix pour les sorties en famille. Cependant, en raison de son antenne fixe et de sa puissance de sortie fixe et faible, sa portée et ses fonctionnalités sont strictement limitées par les normes grand public. C'est un appareil simple et fiable, du genre \"on allume et on utilise\", qui ne nécessite ni licence ni configuration complexe."
                    },
                    ["pl"] = new LocaleDetails
                    {
                        Name = "Radiotelefon przenośny Motorola Talkabout T460",
                        ShortName = "T460",
                        Description = "Zasięg: 100 m czysty odbiór, pogorszenie do 325 m, tylko szum do 475 m\n"
                            + "Tryb dupleksu: Półdupleks\n"
                            + "Jakość sygnału: Niska-średnia\n\n"
                            + "Solidny, bezlicencyjny radiotelefon FRS/GMRS do aktywnej turystyki, wędkowania i polowania. Ma obudowę odporną na zachlapanie (IP54), wbudowaną latarkę oraz obsługę kanałów pogodowych NOAA, co czyni go świetnym wyborem na rodzinne wyjazdy. Jednak ze względu na stałą antenę i stałą niską moc nadawania, jego zasięg i funkcjonalność są ściśle ograniczone do poziomu konsumenckiego. To prosty, niezawodny sprzęt typu \"włącz i używaj\", niewymagający licencji ani skomplikowanej konfiguracji."
                    },
                    ["it"] = new LocaleDetails
                    {
                        Name = "Radio portatile Motorola Talkabout T460",
                        ShortName = "T460",
                        Description = "Portata: 100 m chiaro, degrado fino a 325 m, solo statica fino a 475 m\n"
                            + "Modalità duplex: Semiduplex\n"
                            + "Qualità del segnale: Bassa-media\n\n"
                            + "Una solida radio FRS/GMRS senza licenza per escursionismo attivo, pesca e caccia. Dispone di una scocca resistente agli spruzzi (IP54), una torcia integrata e il supporto per i canali meteo NOAA, il che la rende un'ottima scelta per le gite in famiglia. Tuttavia, a causa dell'antenna fissa e della potenza di uscita bassa e fissa, la sua portata e funzionalità sono strettamente limitate agli standard consumer. È un dispositivo semplice e affidabile del tipo \"accendi e usa\", che non richiede licenza né configurazioni complesse."
                    },
                    ["cz"] = new LocaleDetails
                    {
                        Name = "Přenosná vysílačka Motorola Talkabout T460",
                        ShortName = "T460",
                        Description = "Dosah: 100 m čistý příjem, zhoršení do 325 m, pouze šum do 475 m\n"
                            + "Duplexní režim: Poloduplexní\n"
                            + "Kvalita signálu: Nízká-střední\n\n"
                            + "Solidní bezlicenční vysílačka FRS/GMRS pro aktivní turistiku, rybaření a lov. Má pouzdro odolné proti stříkající vodě (IP54), vestavěnou svítilnu a podporu meteorologických kanálů NOAA, díky čemuž je skvělou volbou na rodinné výlety. Kvůli pevné anténě a pevnému nízkému výkonu je ale její dosah a funkčnost přísně omezena na spotřebitelskou úroveň. Je to jednoduché a spolehlivé zařízení typu \"zapni a používej\", které nevyžaduje licenci ani složité nastavení."
                    }
                }
            });

            _customItemService.CreateItemFromClone(new NewItemFromCloneDetails
            {
                ItemTplToClone = radioCloneBase,
                ParentId = radioParentId,
                NewId = YaesuTplId,
                HandbookParentId = radioHandbookParentId,
                HandbookPriceRoubles = 72000,

                FleaPriceRoubles = 110537,
                OverrideProperties = new TemplateItemProperties
                {
                    Width = 1,
                    Height = 2,

                    Weight = 0.24,
                    BackgroundColor = radioBackgroundColor,
                    RarityPvE = radioRarityPvE,
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
                            + "Duplex mode: Half-duplex | Duplex\n"
                            + "Signal quality: Medium\n\n"
                            + "A feature-packed, premium-class quad-band transceiver for survivalists, extreme sports enthusiasts, and advanced ham radio operators. It stands out with a rugged, dust- and water-resistant housing (IPX7), a built-in barometer, and support for APRS/GPS navigation functions for transmitting coordinates. However, its abundance of features makes the menu extremely difficult for an unprepared user to configure, and its high price is limited to an analog-only communication format. It's the ultimate, reliable device for those who value maximum self-sufficiency and durability in the wilderness."
                    },
                    ["ru"] = new LocaleDetails
                    {
                        Name = "Портативная рация Yaesu VX-8DR",
                        ShortName = "VX-8DR",
                        Description = "Дальность: 150м чистого приёма, деградация до 400м, шум до 515м\n"
                            + "Режим связи: полудуплекс | дуплекс\n"
                            + "Качество сигнала: среднее\n\n"
                            + "Навороченный четырехдиапазонный трансивер премиум-класса для выживальщиков, экстремалов и продвинутых радиолюбителей. Он выделяется сверхпрочным пылевлагозащищенным корпусом (IPX7), встроенным барометром и поддержкой навигационных функций APRS/GPS для передачи координат. Однако обилие функций делает меню крайне сложным для настройки неподготовленным пользователем, а высокая цена ограничивается исключительно аналоговым форматом связи. Это ультимативный и надежный прибор для тех, кому важна максимальная автономность и живучесть в дикой природе."
                    },
                    ["ge"] = new LocaleDetails
                    {
                        Name = "Yaesu VX-8DR Tragbares Funkgerät",
                        ShortName = "VX-8DR",
                        Description = "Reichweite: 150 m klar, Verschlechterung bis 400 m, nur Rauschen bis 515 m\n"
                            + "Duplex-Modus: Halbduplex | Duplex\n"
                            + "Signalqualität: Mittel\n\n"
                            + "Ein funktionsreicher Vierband-Transceiver der Premiumklasse für Survival-Experten, Extremsportler und fortgeschrittene Funkamateure. Er zeichnet sich durch ein robustes, staub- und wasserdichtes Gehäuse (IPX7), ein eingebautes Barometer und Unterstützung für APRS/GPS-Navigationsfunktionen zur Übertragung von Koordinaten aus. Die Fülle an Funktionen macht das Menü jedoch für einen unvorbereiteten Nutzer äußerst schwer zu konfigurieren, und der hohe Preis beschränkt sich auf ein rein analoges Kommunikationsformat. Es ist das ultimative, zuverlässige Gerät für alle, die maximale Autarkie und Widerstandsfähigkeit in der Wildnis schätzen."
                    },
                    ["es"] = new LocaleDetails
                    {
                        Name = "Radio portátil Yaesu VX-8DR",
                        ShortName = "VX-8DR",
                        Description = "Alcance: 150 m claro, degradándose hasta 400 m, solo estática hasta 515 m\n"
                            + "Modo dúplex: Semidúplex | Dúplex\n"
                            + "Calidad de señal: Media\n\n"
                            + "Un transceptor de cuatro bandas repleto de funciones, de clase premium, para supervivencialistas, entusiastas de deportes extremos y radioaficionados avanzados. Destaca por su carcasa robusta, resistente al polvo y al agua (IPX7), un barómetro incorporado y soporte para funciones de navegación APRS/GPS para transmitir coordenadas. Sin embargo, la abundancia de funciones hace que el menú sea extremadamente difícil de configurar para un usuario no preparado, y su elevado precio se limita a un formato de comunicación exclusivamente analógico. Es el dispositivo definitivo y fiable para quienes valoran la máxima autosuficiencia y durabilidad en plena naturaleza."
                    },
                    ["fr"] = new LocaleDetails
                    {
                        Name = "Radio portable Yaesu VX-8DR",
                        ShortName = "VX-8DR",
                        Description = "Portée : 150 m clair, dégradation jusqu'à 400 m, uniquement de la friture jusqu'à 515 m\n"
                            + "Mode duplex : Semi-duplex | Duplex\n"
                            + "Qualité du signal : Moyenne\n\n"
                            + "Un émetteur-récepteur quadribande haut de gamme, riche en fonctionnalités, destiné aux survivalistes, aux amateurs de sports extrêmes et aux radioamateurs avancés. Il se distingue par un boîtier robuste résistant à la poussière et à l'eau (IPX7), un baromètre intégré et la prise en charge des fonctions de navigation APRS/GPS pour transmettre des coordonnées. Cependant, l'abondance de fonctionnalités rend le menu extrêmement difficile à configurer pour un utilisateur non averti, et son prix élevé se limite à un format de communication purement analogique. C'est l'appareil ultime et fiable pour ceux qui privilégient une autonomie et une robustesse maximales en pleine nature."
                    },
                    ["pl"] = new LocaleDetails
                    {
                        Name = "Radiotelefon przenośny Yaesu VX-8DR",
                        ShortName = "VX-8DR",
                        Description = "Zasięg: 150 m czysty odbiór, pogorszenie do 400 m, tylko szum do 515 m\n"
                            + "Tryb dupleksu: Półdupleks | Dupleks\n"
                            + "Jakość sygnału: Średnia\n\n"
                            + "Naszpikowany funkcjami czterozakresowy transceiver klasy premium dla survivalowców, entuzjastów sportów ekstremalnych i zaawansowanych radioamatorów. Wyróżnia się wytrzymałą, pyło- i wodoszczelną obudową (IPX7), wbudowanym barometrem oraz obsługą funkcji nawigacyjnych APRS/GPS do przesyłania współrzędnych. Jednak nadmiar funkcji sprawia, że menu jest niezwykle trudne do skonfigurowania dla nieprzygotowanego użytkownika, a wysoka cena ogranicza się do wyłącznie analogowego formatu łączności. To ostateczne, niezawodne urządzenie dla tych, którzy cenią maksymalną samowystarczalność i wytrzymałość w dziczy."
                    },
                    ["it"] = new LocaleDetails
                    {
                        Name = "Radio portatile Yaesu VX-8DR",
                        ShortName = "VX-8DR",
                        Description = "Portata: 150 m chiaro, degrado fino a 400 m, solo statica fino a 515 m\n"
                            + "Modalità duplex: Semiduplex | Duplex\n"
                            + "Qualità del segnale: Media\n\n"
                            + "Un ricetrasmettitore quadribanda di classe premium, ricco di funzioni, per sopravvivenzialisti, appassionati di sport estremi e radioamatori esperti. Si distingue per una scocca robusta, resistente a polvere e acqua (IPX7), un barometro integrato e il supporto per le funzioni di navigazione APRS/GPS per la trasmissione delle coordinate. Tuttavia, l'abbondanza di funzioni rende il menu estremamente difficile da configurare per un utente impreparato, e il prezzo elevato è limitato a un formato di comunicazione solo analogico. È il dispositivo definitivo e affidabile per chi apprezza la massima autosufficienza e resistenza nella natura selvaggia."
                    },
                    ["cz"] = new LocaleDetails
                    {
                        Name = "Přenosná vysílačka Yaesu VX-8DR",
                        ShortName = "VX-8DR",
                        Description = "Dosah: 150 m čistý příjem, zhoršení do 400 m, pouze šum do 515 m\n"
                            + "Duplexní režim: Poloduplexní | Duplexní\n"
                            + "Kvalita signálu: Střední\n\n"
                            + "Prémiový čtyřpásmový transceiver nabitý funkcemi pro nadšence do přežití, extrémních sportů a pokročilé radioamatéry. Vyniká odolným, prachotěsným a vodotěsným pouzdrem (IPX7), vestavěným barometrem a podporou navigačních funkcí APRS/GPS pro přenos souřadnic. Množství funkcí ale činí menu extrémně obtížné na nastavení pro nepřipraveného uživatele a vysoká cena se omezuje pouze na analogový formát komunikace. Je to špičkové, spolehlivé zařízení pro ty, kdo si cení maximální soběstačnosti a odolnosti v divočině."
                    }
                }
            });

            _customItemService.CreateItemFromClone(new NewItemFromCloneDetails
            {
                ItemTplToClone = radioCloneBase,
                ParentId = radioParentId,
                NewId = Dp4800TplId,
                HandbookParentId = radioHandbookParentId,
                HandbookPriceRoubles = 85000,
                FleaPriceRoubles = 40000,
                OverrideProperties = new TemplateItemProperties
                {
                    Width = 1,
                    Height = 2,

                    Weight = 0.355,
                    BackgroundColor = radioBackgroundColor,
                    RarityPvE = radioRarityPvE,
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
                    },
                    ["ge"] = new LocaleDetails
                    {
                        Name = "Motorola DP4800 Tragbares Funkgerät",
                        ShortName = "DP4800",
                        Description = "Reichweite: 350 m klar, Verschlechterung bis 525 m, nur Rauschen bis 650 m\n"
                            + "Duplex-Modus: Halbduplex\n"
                            + "Signalqualität: Mittel-hoch\n\n"
                            + "Der Goldstandard der professionellen digitalen DMR-Kommunikation für den kommerziellen Sektor, Sicherheitsdienste und harte Einsatzbedingungen. Es ist mit kristallklarem Klang dank intelligenter Rauschunterdrückung, einer vollständigen Tastatur, einem Farbdisplay und dem höchsten Gehäuseschutz nach dem IP68-Standard ausgestattet. Der Betrieb und die Konfiguration erfordern jedoch eine komplexe professionelle PC-Programmierung, und der hohe Preis sowie die Größe machen es für den einfachen Alltagsgebrauch übertrieben. Es ist ein kompromissloses, ultrazuverlässiges Arbeitswerkzeug zum Aufbau sicherer Kommunikationsnetze in Unternehmen."
                    },
                    ["es"] = new LocaleDetails
                    {
                        Name = "Radio portátil Motorola DP4800",
                        ShortName = "DP4800",
                        Description = "Alcance: 350 m claro, degradándose hasta 525 m, solo estática hasta 650 m\n"
                            + "Modo dúplex: Semidúplex\n"
                            + "Calidad de señal: Media-alta\n\n"
                            + "El estándar de oro de la comunicación digital DMR profesional para el sector comercial, los servicios de seguridad y condiciones de uso exigentes. Está equipada con un audio cristalino respaldado por supresión inteligente de ruido, un teclado completo, una pantalla a color y el máximo nivel de protección de carcasa según el estándar IP68. Sin embargo, su manejo y configuración requieren una compleja programación profesional por ordenador, y su elevado precio y tamaño la hacen excesiva para el uso cotidiano simple. Es una herramienta de trabajo intransigente y ultrafiable para construir redes de comunicación seguras dentro de una empresa."
                    },
                    ["fr"] = new LocaleDetails
                    {
                        Name = "Radio portable Motorola DP4800",
                        ShortName = "DP4800",
                        Description = "Portée : 350 m clair, dégradation jusqu'à 525 m, uniquement de la friture jusqu'à 650 m\n"
                            + "Mode duplex : Semi-duplex\n"
                            + "Qualité du signal : Moyenne-élevée\n\n"
                            + "La référence absolue en communication numérique DMR professionnelle pour le secteur commercial, les services de sécurité et les conditions d'utilisation intensives. Elle est dotée d'un son cristallin grâce à une suppression intelligente du bruit, d'un clavier complet, d'un écran couleur et du plus haut niveau de protection du boîtier selon la norme IP68. Cependant, son fonctionnement et sa configuration nécessitent une programmation professionnelle complexe sur ordinateur, et son prix élevé ainsi que sa taille la rendent excessive pour un usage quotidien simple. C'est un outil de travail intransigeant et ultra-fiable pour construire des réseaux de communication sécurisés au sein d'une entreprise."
                    },
                    ["pl"] = new LocaleDetails
                    {
                        Name = "Radiotelefon przenośny Motorola DP4800",
                        ShortName = "DP4800",
                        Description = "Zasięg: 350 m czysty odbiór, pogorszenie do 525 m, tylko szum do 650 m\n"
                            + "Tryb dupleksu: Półdupleks\n"
                            + "Jakość sygnału: Średnia-wysoka\n\n"
                            + "Złoty standard profesjonalnej cyfrowej łączności DMR dla sektora komercyjnego, służb ochrony i wymagających warunków pracy. Wyposażony w krystalicznie czysty dźwięk wspierany inteligentną redukcją szumów, pełną klawiaturę, kolorowy wyświetlacz oraz najwyższy poziom ochrony obudowy według normy IP68. Jednak jego obsługa i konfiguracja wymagają skomplikowanego profesjonalnego programowania przez komputer, a wysoka cena i rozmiar czynią go zbędnym do prostego codziennego użytku. To bezkompromisowe, ultraniezawodne narzędzie pracy do budowy bezpiecznych sieci łączności w przedsiębiorstwie."
                    },
                    ["it"] = new LocaleDetails
                    {
                        Name = "Radio portatile Motorola DP4800",
                        ShortName = "DP4800",
                        Description = "Portata: 350 m chiaro, degrado fino a 525 m, solo statica fino a 650 m\n"
                            + "Modalità duplex: Semiduplex\n"
                            + "Qualità del segnale: Media-alta\n\n"
                            + "Lo standard d'oro della comunicazione digitale DMR professionale per il settore commerciale, i servizi di sicurezza e condizioni operative gravose. È dotata di un audio cristallino supportato da una soppressione intelligente del rumore, una tastiera completa, un display a colori e il massimo livello di protezione della scocca secondo lo standard IP68. Tuttavia, il suo funzionamento e la configurazione richiedono una complessa programmazione professionale via PC, e il prezzo elevato e le dimensioni la rendono eccessiva per il semplice uso quotidiano. È uno strumento di lavoro intransigente e ultra-affidabile per costruire reti di comunicazione sicure all'interno di un'azienda."
                    },
                    ["cz"] = new LocaleDetails
                    {
                        Name = "Přenosná vysílačka Motorola DP4800",
                        ShortName = "DP4800",
                        Description = "Dosah: 350 m čistý příjem, zhoršení do 525 m, pouze šum do 650 m\n"
                            + "Duplexní režim: Poloduplexní\n"
                            + "Kvalita signálu: Střední-vysoká\n\n"
                            + "Zlatý standard profesionální digitální komunikace DMR pro komerční sektor, bezpečnostní služby a náročné provozní podmínky. Je vybavena křišťálově čistým zvukem podporovaným inteligentním potlačením šumu, plnou klávesnicí, barevným displejem a nejvyšší úrovní ochrany pouzdra podle normy IP68. Provoz a konfigurace však vyžadují složité profesionální programování přes PC a vysoká cena i velikost ji činí přebytečnou pro jednoduché každodenní použití. Je to nekompromisní, mimořádně spolehlivý pracovní nástroj pro budování zabezpečených komunikačních sítí v rámci podniku."
                    }
                }
            });

            _customItemService.CreateItemFromClone(new NewItemFromCloneDetails
            {
                ItemTplToClone = radioCloneBase,
                ParentId = radioParentId,
                NewId = Dp4601eTplId,
                HandbookParentId = radioHandbookParentId,
                HandbookPriceRoubles = 89990,

                FleaPriceRoubles = 152400,
                OverrideProperties = new TemplateItemProperties
                {
                    Width = 1,
                    Height = 2,

                    Weight = 0.36,
                    BackgroundColor = radioBackgroundColor,
                    RarityPvE = radioRarityPvE,
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
                            + "Duplex mode: Half-duplex | Duplex\n"
                            + "Signal quality: High\n\n"
                            + "An advanced version of the DP4800, a professional digital DMR radio with built-in GPS and Bluetooth modules. It delivers crystal-clear audio, robust AES-256 encryption, full IP68 water resistance, and precise field-position tracking. However, like the entire MotoTRBO lineup, it requires complex computer-based configuration and is too expensive for casual everyday use. It's a reliable, technologically advanced tool for team coordination and security operations in the harshest conditions."
                    },
                    ["ru"] = new LocaleDetails
                    {
                        Name = "Портативная рация Motorola DP4601e",
                        ShortName = "DP4601e",
                        Description = "Дальность: 300м чистого приёма, деградация до 500м, шум до 625м\n"
                            + "Режим связи: полудуплекс | дуплекс\n"
                            + "Качество сигнала: высокое\n\n"
                            + "Продвинутая версия модели DP4800, представляющая собой профессиональную цифровую DMR-рацию со встроенными модулями GPS и Bluetooth. Она обеспечивает кристально чистый звук, стойкое шифрование AES-256, полную влагозащиту корпуса по стандарту IP68 и возможность точного позиционирования на местности. Однако, как и вся линейка MotoTRBO, она требует сложной настройки через компьютер и является слишком дорогой для простого бытового использования. Это надежный и технологичный инструмент для координации команд и работы служб безопасности в самых тяжелых условиях."
                    },
                    ["ge"] = new LocaleDetails
                    {
                        Name = "Motorola DP4601e Tragbares Funkgerät",
                        ShortName = "DP4601e",
                        Description = "Reichweite: 300 m klar, Verschlechterung bis 500 m, nur Rauschen bis 625 m\n"
                            + "Duplex-Modus: Halbduplex | Duplex\n"
                            + "Signalqualität: Hoch\n\n"
                            + "Eine fortschrittliche Version des DP4800, ein professionelles digitales DMR-Funkgerät mit integrierten GPS- und Bluetooth-Modulen. Es liefert kristallklaren Klang, robuste AES-256-Verschlüsselung, volle IP68-Wasserbeständigkeit und präzise Positionsverfolgung im Feld. Wie die gesamte MotoTRBO-Reihe erfordert es jedoch eine komplexe computergestützte Konfiguration und ist für den gelegentlichen Alltagsgebrauch zu teuer. Es ist ein zuverlässiges, technologisch fortschrittliches Werkzeug für die Teamkoordination und Sicherheitseinsätze unter härtesten Bedingungen."
                    },
                    ["es"] = new LocaleDetails
                    {
                        Name = "Radio portátil Motorola DP4601e",
                        ShortName = "DP4601e",
                        Description = "Alcance: 300 m claro, degradándose hasta 500 m, solo estática hasta 625 m\n"
                            + "Modo dúplex: Semidúplex | Dúplex\n"
                            + "Calidad de señal: Alta\n\n"
                            + "Una versión avanzada de la DP4800, una radio digital DMR profesional con módulos GPS y Bluetooth integrados. Ofrece un audio cristalino, un robusto cifrado AES-256, resistencia total al agua según IP68 y un seguimiento preciso de posición en el terreno. Sin embargo, como toda la línea MotoTRBO, requiere una configuración informática compleja y resulta demasiado cara para un uso cotidiano ocasional. Es una herramienta fiable y tecnológicamente avanzada para la coordinación de equipos y operaciones de seguridad en las condiciones más duras."
                    },
                    ["fr"] = new LocaleDetails
                    {
                        Name = "Radio portable Motorola DP4601e",
                        ShortName = "DP4601e",
                        Description = "Portée : 300 m clair, dégradation jusqu'à 500 m, uniquement de la friture jusqu'à 625 m\n"
                            + "Mode duplex : Semi-duplex | Duplex\n"
                            + "Qualité du signal : Élevée\n\n"
                            + "Une version avancée du DP4800, une radio numérique DMR professionnelle dotée de modules GPS et Bluetooth intégrés. Elle offre un son cristallin, un chiffrement AES-256 robuste, une étanchéité totale selon la norme IP68 et un suivi précis de la position sur le terrain. Cependant, comme toute la gamme MotoTRBO, elle nécessite une configuration informatique complexe et est trop chère pour un usage occasionnel. C'est un outil fiable et technologiquement avancé pour la coordination d'équipe et les opérations de sécurité dans les conditions les plus rudes."
                    },
                    ["pl"] = new LocaleDetails
                    {
                        Name = "Radiotelefon przenośny Motorola DP4601e",
                        ShortName = "DP4601e",
                        Description = "Zasięg: 300 m czysty odbiór, pogorszenie do 500 m, tylko szum do 625 m\n"
                            + "Tryb dupleksu: Półdupleks | Dupleks\n"
                            + "Jakość sygnału: Wysoka\n\n"
                            + "Zaawansowana wersja modelu DP4800 — profesjonalny cyfrowy radiotelefon DMR z wbudowanymi modułami GPS i Bluetooth. Zapewnia krystalicznie czysty dźwięk, solidne szyfrowanie AES-256, pełną wodoodporność zgodną z IP68 oraz precyzyjne śledzenie pozycji w terenie. Jednak, podobnie jak cała linia MotoTRBO, wymaga skomplikowanej konfiguracji komputerowej i jest zbyt droga do okazjonalnego codziennego użytku. To niezawodne, zaawansowane technologicznie narzędzie do koordynacji zespołu i operacji bezpieczeństwa w najtrudniejszych warunkach."
                    },
                    ["it"] = new LocaleDetails
                    {
                        Name = "Radio portatile Motorola DP4601e",
                        ShortName = "DP4601e",
                        Description = "Portata: 300 m chiaro, degrado fino a 500 m, solo statica fino a 625 m\n"
                            + "Modalità duplex: Semiduplex | Duplex\n"
                            + "Qualità del segnale: Alta\n\n"
                            + "Una versione avanzata della DP4800, una radio digitale DMR professionale con moduli GPS e Bluetooth integrati. Offre un audio cristallino, una robusta crittografia AES-256, piena resistenza all'acqua secondo lo standard IP68 e un preciso tracciamento della posizione sul campo. Tuttavia, come tutta la gamma MotoTRBO, richiede una configurazione informatica complessa ed è troppo costosa per un uso occasionale quotidiano. È uno strumento affidabile e tecnologicamente avanzato per il coordinamento di squadra e le operazioni di sicurezza nelle condizioni più difficili."
                    },
                    ["cz"] = new LocaleDetails
                    {
                        Name = "Přenosná vysílačka Motorola DP4601e",
                        ShortName = "DP4601e",
                        Description = "Dosah: 300 m čistý příjem, zhoršení do 500 m, pouze šum do 625 m\n"
                            + "Duplexní režim: Poloduplexní | Duplexní\n"
                            + "Kvalita signálu: Vysoká\n\n"
                            + "Pokročilá verze modelu DP4800, profesionální digitální DMR vysílačka s vestavěnými moduly GPS a Bluetooth. Nabízí křišťálově čistý zvuk, odolné šifrování AES-256, plnou vodotěsnost dle normy IP68 a přesné sledování polohy v terénu. Stejně jako celá řada MotoTRBO ale vyžaduje složitou konfiguraci přes počítač a je příliš drahá pro příležitostné každodenní použití. Je to spolehlivý a technologicky vyspělý nástroj pro koordinaci týmu a bezpečnostní operace v těch nejtěžších podmínkách."
                    }
                }
            });

            _customItemService.CreateItemFromClone(new NewItemFromCloneDetails
            {
                ItemTplToClone = radioCloneBase,
                ParentId = radioParentId,
                NewId = Xts5000TplId,
                HandbookParentId = radioHandbookParentId,
                HandbookPriceRoubles = 100000,
                FleaPriceRoubles = 62000,
                OverrideProperties = new TemplateItemProperties
                {
                    Width = 1,
                    Height = 2,

                    Weight = 0.54,
                    BackgroundColor = radioBackgroundColor,
                    RarityPvE = radioRarityPvE,
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
                            + "Duplex mode: Half-duplex | Duplex\n"
                            + "Signal quality: High-very high\n\n"
                            + "A professional radio using the APCO P25 digital standard, developed for police, special services, and military agencies. It stands out for its phenomenal military-grade physical durability, excellent audio quality under heavy interference, and support for reliable hardware encryption. However, its substantial size, considerable weight, and archaic programming software make it a challenging device for an average user to master. It's a nearly indestructible, uncompromising \"brick\" for those who need maximum equipment durability and conversation privacy."
                    },
                    ["ru"] = new LocaleDetails
                    {
                        Name = "Портативная рация Motorola XTS5000",
                        ShortName = "XTS5000",
                        Description = "Дальность: 450м чистого приёма, деградация до 625м, шум до 750м\n"
                            + "Режим связи: полудуплекс | дуплекс\n"
                            + "Качество сигнала: высокое-оч.высокое\n\n"
                            + "Профессиональная радиостанция цифрового стандарта APCO P25, разработанная для полиции, спецслужб и военных ведомств. Она отличается феноменальной физической прочностью по военным стандартам, отличным качеством звука в тяжелых условиях помех и поддержкой надежного аппаратного шифрования. Однако внушительные габариты, приличный вес и архаичный софт для программирования делают ее освоение сложным испытанием для обычного пользователя. Это практически неуничтожимый и бескомпромиссный «кирпич» для тех, кому критически важна максимальная живучесть оборудования и конфиденциальность переговоров."
                    },
                    ["ge"] = new LocaleDetails
                    {
                        Name = "Motorola XTS5000 Tragbares Funkgerät",
                        ShortName = "XTS5000",
                        Description = "Reichweite: 450 m klar, Verschlechterung bis 625 m, nur Rauschen bis 750 m\n"
                            + "Duplex-Modus: Halbduplex | Duplex\n"
                            + "Signalqualität: Hoch-sehr hoch\n\n"
                            + "Ein professionelles Funkgerät nach dem digitalen APCO-P25-Standard, entwickelt für Polizei, Sondereinsatzkräfte und Militärbehörden. Es zeichnet sich durch eine phänomenale, militärtaugliche physische Robustheit, hervorragende Klangqualität unter starken Störungen und Unterstützung für zuverlässige Hardware-Verschlüsselung aus. Die beträchtliche Größe, das erhebliche Gewicht und die veraltete Programmiersoftware machen es jedoch für einen Durchschnittsnutzer schwer zu beherrschen. Es ist ein nahezu unzerstörbarer, kompromissloser \"Ziegelstein\" für alle, die maximale Ausrüstungslebensdauer und Gesprächsvertraulichkeit benötigen."
                    },
                    ["es"] = new LocaleDetails
                    {
                        Name = "Radio portátil Motorola XTS5000",
                        ShortName = "XTS5000",
                        Description = "Alcance: 450 m claro, degradándose hasta 625 m, solo estática hasta 750 m\n"
                            + "Modo dúplex: Semidúplex | Dúplex\n"
                            + "Calidad de señal: Alta-muy alta\n\n"
                            + "Una radio profesional que utiliza el estándar digital APCO P25, desarrollada para la policía, servicios especiales y agencias militares. Destaca por su fenomenal durabilidad física de grado militar, excelente calidad de audio bajo fuertes interferencias y soporte para un cifrado de hardware fiable. Sin embargo, su considerable tamaño, peso notable y su arcaico software de programación la convierten en un dispositivo difícil de dominar para un usuario medio. Es un \"ladrillo\" prácticamente indestructible e intransigente para quienes necesitan la máxima durabilidad del equipo y privacidad en las conversaciones."
                    },
                    ["fr"] = new LocaleDetails
                    {
                        Name = "Radio portable Motorola XTS5000",
                        ShortName = "XTS5000",
                        Description = "Portée : 450 m clair, dégradation jusqu'à 625 m, uniquement de la friture jusqu'à 750 m\n"
                            + "Mode duplex : Semi-duplex | Duplex\n"
                            + "Qualité du signal : Élevée-très élevée\n\n"
                            + "Une radio professionnelle utilisant le standard numérique APCO P25, développée pour la police, les services spéciaux et les agences militaires. Elle se distingue par sa robustesse physique phénoménale de qualité militaire, son excellente qualité audio en cas de fortes interférences et sa prise en charge d'un chiffrement matériel fiable. Cependant, sa taille imposante, son poids considérable et son logiciel de programmation archaïque en font un appareil difficile à maîtriser pour un utilisateur moyen. C'est une \"brique\" pratiquement indestructible et intransigeante pour ceux qui ont besoin d'une durabilité maximale de l'équipement et de la confidentialité des conversations."
                    },
                    ["pl"] = new LocaleDetails
                    {
                        Name = "Radiotelefon przenośny Motorola XTS5000",
                        ShortName = "XTS5000",
                        Description = "Zasięg: 450 m czysty odbiór, pogorszenie do 625 m, tylko szum do 750 m\n"
                            + "Tryb dupleksu: Półdupleks | Dupleks\n"
                            + "Jakość sygnału: Wysoka-bardzo wysoka\n\n"
                            + "Profesjonalny radiotelefon wykorzystujący cyfrowy standard APCO P25, opracowany dla policji, służb specjalnych i agencji wojskowych. Wyróżnia się fenomenalną fizyczną wytrzymałością klasy wojskowej, doskonałą jakością dźwięku przy silnych zakłóceniach oraz obsługą niezawodnego szyfrowania sprzętowego. Jednak spore gabaryty, znaczna waga i archaiczne oprogramowanie do programowania czynią go trudnym do opanowania dla przeciętnego użytkownika. To praktycznie niezniszczalna, bezkompromisowa \"cegła\" dla tych, którzy potrzebują maksymalnej trwałości sprzętu i poufności rozmów."
                    },
                    ["it"] = new LocaleDetails
                    {
                        Name = "Radio portatile Motorola XTS5000",
                        ShortName = "XTS5000",
                        Description = "Portata: 450 m chiaro, degrado fino a 625 m, solo statica fino a 750 m\n"
                            + "Modalità duplex: Semiduplex | Duplex\n"
                            + "Qualità del segnale: Alta-molto alta\n\n"
                            + "Una radio professionale che utilizza lo standard digitale APCO P25, sviluppata per polizia, servizi speciali e agenzie militari. Si distingue per la sua fenomenale robustezza fisica di livello militare, un'eccellente qualità audio in condizioni di forte interferenza e il supporto per una crittografia hardware affidabile. Tuttavia, le sue dimensioni considerevoli, il peso notevole e il software di programmazione arcaico la rendono un dispositivo difficile da padroneggiare per un utente medio. È un \"mattone\" praticamente indistruttibile e intransigente per chi ha bisogno della massima durata dell'attrezzatura e riservatezza delle conversazioni."
                    },
                    ["cz"] = new LocaleDetails
                    {
                        Name = "Přenosná vysílačka Motorola XTS5000",
                        ShortName = "XTS5000",
                        Description = "Dosah: 450 m čistý příjem, zhoršení do 625 m, pouze šum do 750 m\n"
                            + "Duplexní režim: Poloduplexní | Duplexní\n"
                            + "Kvalita signálu: Vysoká-velmi vysoká\n\n"
                            + "Profesionální vysílačka využívající digitální standard APCO P25, vyvinutá pro policii, speciální služby a vojenské agentury. Vyniká fenomenální fyzickou odolností vojenské kvality, vynikající kvalitou zvuku i za silného rušení a podporou spolehlivého hardwarového šifrování. Značné rozměry, nemalá hmotnost a archaický programovací software z ní však dělají zařízení, které je pro běžného uživatele obtížné zvládnout. Je to prakticky nezničitelný, nekompromisní \"cihla\" pro ty, kdo potřebují maximální odolnost vybavení a důvěrnost hovorů."
                    }
                }
            });

            _customItemService.CreateItemFromClone(new NewItemFromCloneDetails
            {
                ItemTplToClone = radioCloneBase,
                ParentId = radioParentId,
                NewId = HarrisTplId,
                HandbookParentId = radioHandbookParentId,
                HandbookPriceRoubles = 200000,
                FleaPriceRoubles = 100000,
                OverrideProperties = new TemplateItemProperties
                {
                    Width = 1,
                    Height = 2,

                    Weight = 1.18,
                    BackgroundColor = radioBackgroundColor,
                    RarityPvE = radioRarityPvE,
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
                            + "Duplex mode: Half-duplex | Duplex\n"
                            + "Signal quality: Perfect\n\n"
                            + "A legendary military-standard tactical radio, developed for special forces and armed forces units. It stands out for its enormous frequency range, satellite communication (SATCOM) support, extremely powerful military-grade encryption, and a nearly indestructible sealed housing. However, its exorbitant cost, strict legal ownership restrictions, and extreme setup complexity make it practically unattainable for ordinary users. It's the ultimate, uncompromising communication tool, built exclusively for operation in the harshest combat conditions."
                    },
                    ["ru"] = new LocaleDetails
                    {
                        Name = "Портативная рация Harris AN/PRC-152",
                        ShortName = "AN/PRC-152",
                        Description = "Дальность: 600м чистого приёма, деградация до 775м, шум до 900м\n"
                            + "Режим связи: полудуплекс | дуплекс\n"
                            + "Качество сигнала: идеальное\n\n"
                            + "Легендарная тактическая радиостанция военного стандарта, разработанная для специальных подразделений и вооруженных сил. Она отличается огромным диапазоном частот, поддержкой спутниковой связи (SATCOM), мощнейшим шифрованием военного уровня и практически неубиваемым герметичным корпусом. Однако запредельная стоимость, строгие юридические ограничения на владение и крайняя сложность в настройке делают ее практически недосягаемой для обычных пользователей. Это ультимативный и бескомпромиссный инструмент связи, созданный исключительно для работы в самых суровых боевых условиях."
                    },
                    ["ge"] = new LocaleDetails
                    {
                        Name = "Harris AN/PRC-152 Tragbares Funkgerät",
                        ShortName = "AN/PRC-152",
                        Description = "Reichweite: 600 m klar, Verschlechterung bis 775 m, nur Rauschen bis 900 m\n"
                            + "Duplex-Modus: Halbduplex | Duplex\n"
                            + "Signalqualität: Perfekt\n\n"
                            + "Ein legendäres taktisches Funkgerät nach Militärstandard, entwickelt für Spezialeinheiten und Streitkräfte. Es zeichnet sich durch einen enormen Frequenzbereich, Unterstützung für Satellitenkommunikation (SATCOM), extrem starke militärische Verschlüsselung und ein nahezu unzerstörbares, versiegeltes Gehäuse aus. Sein exorbitanter Preis, strenge rechtliche Besitzbeschränkungen und die extreme Einrichtungskomplexität machen es jedoch für gewöhnliche Nutzer praktisch unerreichbar. Es ist das ultimative, kompromisslose Kommunikationswerkzeug, ausschließlich für den Einsatz unter den härtesten Kampfbedingungen gebaut."
                    },
                    ["es"] = new LocaleDetails
                    {
                        Name = "Radio portátil Harris AN/PRC-152",
                        ShortName = "AN/PRC-152",
                        Description = "Alcance: 600 m claro, degradándose hasta 775 m, solo estática hasta 900 m\n"
                            + "Modo dúplex: Semidúplex | Dúplex\n"
                            + "Calidad de señal: Perfecta\n\n"
                            + "Una legendaria radio táctica de estándar militar, desarrollada para fuerzas especiales y unidades de las fuerzas armadas. Destaca por su enorme rango de frecuencias, soporte de comunicación satelital (SATCOM), un cifrado militar extremadamente potente y una carcasa sellada prácticamente indestructible. Sin embargo, su coste exorbitante, las estrictas restricciones legales de posesión y la extrema complejidad de configuración la hacen prácticamente inalcanzable para usuarios comunes. Es la herramienta de comunicación definitiva e intransigente, construida exclusivamente para operar en las condiciones de combate más duras."
                    },
                    ["fr"] = new LocaleDetails
                    {
                        Name = "Radio portable Harris AN/PRC-152",
                        ShortName = "AN/PRC-152",
                        Description = "Portée : 600 m clair, dégradation jusqu'à 775 m, uniquement de la friture jusqu'à 900 m\n"
                            + "Mode duplex : Semi-duplex | Duplex\n"
                            + "Qualité du signal : Parfaite\n\n"
                            + "Une radio tactique légendaire de standard militaire, développée pour les forces spéciales et les unités des forces armées. Elle se distingue par son énorme plage de fréquences, sa prise en charge des communications satellites (SATCOM), un chiffrement militaire extrêmement puissant et un boîtier scellé pratiquement indestructible. Cependant, son coût exorbitant, ses strictes restrictions légales de possession et son extrême complexité de configuration la rendent pratiquement inaccessible aux utilisateurs ordinaires. C'est l'outil de communication ultime et intransigeant, conçu exclusivement pour fonctionner dans les conditions de combat les plus rudes."
                    },
                    ["pl"] = new LocaleDetails
                    {
                        Name = "Radiotelefon przenośny Harris AN/PRC-152",
                        ShortName = "AN/PRC-152",
                        Description = "Zasięg: 600 m czysty odbiór, pogorszenie do 775 m, tylko szum do 900 m\n"
                            + "Tryb dupleksu: Półdupleks | Dupleks\n"
                            + "Jakość sygnału: Idealna\n\n"
                            + "Legendarny radiotelefon taktyczny klasy wojskowej, opracowany dla jednostek specjalnych i sił zbrojnych. Wyróżnia się ogromnym zakresem częstotliwości, wsparciem łączności satelitarnej (SATCOM), niezwykle silnym szyfrowaniem klasy wojskowej oraz praktycznie niezniszczalną, hermetyczną obudową. Jednak zaporowa cena, surowe ograniczenia prawne dotyczące posiadania i ekstremalna złożoność konfiguracji czynią go praktycznie nieosiągalnym dla zwykłych użytkowników. To ostateczne, bezkompromisowe narzędzie łączności, zbudowane wyłącznie do działania w najsurowszych warunkach bojowych."
                    },
                    ["it"] = new LocaleDetails
                    {
                        Name = "Radio portatile Harris AN/PRC-152",
                        ShortName = "AN/PRC-152",
                        Description = "Portata: 600 m chiaro, degrado fino a 775 m, solo statica fino a 900 m\n"
                            + "Modalità duplex: Semiduplex | Duplex\n"
                            + "Qualità del segnale: Perfetta\n\n"
                            + "Una leggendaria radio tattica di standard militare, sviluppata per le forze speciali e le unità delle forze armate. Si distingue per l'enorme gamma di frequenze, il supporto per la comunicazione satellitare (SATCOM), una crittografia militare estremamente potente e un involucro sigillato praticamente indistruttibile. Tuttavia, il suo costo esorbitante, le rigide restrizioni legali sul possesso e l'estrema complessità di configurazione la rendono praticamente irraggiungibile per gli utenti comuni. È lo strumento di comunicazione definitivo e intransigente, costruito esclusivamente per operare nelle condizioni di combattimento più dure."
                    },
                    ["cz"] = new LocaleDetails
                    {
                        Name = "Přenosná vysílačka Harris AN/PRC-152",
                        ShortName = "AN/PRC-152",
                        Description = "Dosah: 600 m čistý příjem, zhoršení do 775 m, pouze šum do 900 m\n"
                            + "Duplexní režim: Poloduplexní | Duplexní\n"
                            + "Kvalita signálu: Dokonalá\n\n"
                            + "Legendární taktická vysílačka vojenského standardu, vyvinutá pro speciální jednotky a ozbrojené síly. Vyniká obrovským frekvenčním rozsahem, podporou satelitní komunikace (SATCOM), nesmírně silným šifrováním vojenské úrovně a prakticky nezničitelným hermetickým pouzdrem. Astronomická cena, přísná právní omezení vlastnictví a extrémní složitost nastavení ji však činí prakticky nedosažitelnou pro běžné uživatele. Je to absolutní, nekompromisní komunikační nástroj, postavený výhradně pro provoz v těch nejtvrdších bojových podmínkách."
                    }
                }
            });

            _customItemService.CreateItemFromClone(new NewItemFromCloneDetails
            {
                ItemTplToClone = radioCloneBase,
                ParentId = radioParentId,
                NewId = Trc83TplId,
                HandbookParentId = radioHandbookParentId,
                HandbookPriceRoubles = 300000,

                FleaPriceRoubles = 227347,
                OverrideProperties = new TemplateItemProperties
                {
                    Width = 1,
                    Height = 2,

                    Weight = 0.6,
                    BackgroundColor = radioBackgroundColor,
                    RarityPvE = radioRarityPvE,
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
                            + "A vintage three-channel Citizens Band (CB) radio from the 1980s. Bulky, simple, and reliable as a brick. It features a long telescoping antenna and runs on AA batteries. Due to its outdated analog circuitry and limited channel selection it's unsuitable for serious tactical operations, but as a cheap communication tool for beginners it works flawlessly. One of these was found wedged behind a basement shelf in a house on the outskirts. Christmas lights ran along the wall by the door frame, and a kid's handwriting was scratched into the casing: names, call signs, something about a \"gate\". Well, stranger things have happened, I suppose..."
                    },
                    ["ru"] = new LocaleDetails
                    {
                        Name = "Портативная рация Realistic TRC-83",
                        ShortName = "TRC-83",
                        Description = "Дальность: 30м чистого приёма, деградация до 200м, шум до 300м\n"
                            + "Режим связи: полудуплекс\n"
                            + "Качество сигнала: оч. низкое\n\n"
                            + "Винтажная трехканальная радиостанция гражданского диапазона (CB) родом из 1980-х. Массивная, простая и надежная, как утюг. Она оборудована длинной телескопической антенной и питается от батареек типа АА. Из-за устаревшей аналоговой схемы и ограниченного выбора каналов не подходит для серьезных тактических операций, но как дешевое средство связи для новичков — работает безотказно. Одну из таких нашли за полкой в подвале одного дома на окраине. Вдоль стены у дверного проёма тянулась гирлянда, а на корпусе детским почерком были нацарапаны имена, позывные и что-то про «ворота». Мда уж, очень странные дела, конечно..."
                    },
                    ["ge"] = new LocaleDetails
                    {
                        Name = "Realistic TRC-83 Tragbares Funkgerät",
                        ShortName = "TRC-83",
                        Description = "Reichweite: 30 m klar, Verschlechterung bis 200 m, nur Rauschen bis 300 m\n"
                            + "Duplex-Modus: Halbduplex\n"
                            + "Signalqualität: Sehr niedrig\n\n"
                            + "Ein altmodisches dreikanaliges CB-Funkgerät (Citizens Band) aus den 1980er Jahren. Klobig, einfach und zuverlässig wie ein Ziegelstein. Es verfügt über eine lange ausziehbare Antenne und läuft mit AA-Batterien. Aufgrund seiner veralteten Analogschaltung und der begrenzten Kanalauswahl ist es für ernsthafte taktische Einsätze ungeeignet, funktioniert aber als billiges Kommunikationsmittel für Anfänger tadellos. Eines davon wurde hinter einem Kellerregal in einem Haus am Stadtrand gefunden. Entlang der Wand neben dem Türrahmen verlief eine Lichterkette, und in das Gehäuse war mit Kinderhandschrift eingeritzt: Namen, Rufzeichen, etwas über ein \"Tor\". Nun ja, seltsamere Dinge sind schon passiert, nehme ich an..."
                    },
                    ["es"] = new LocaleDetails
                    {
                        Name = "Radio portátil Realistic TRC-83",
                        ShortName = "TRC-83",
                        Description = "Alcance: 30 m claro, degradándose hasta 200 m, solo estática hasta 300 m\n"
                            + "Modo dúplex: Semidúplex\n"
                            + "Calidad de señal: Muy baja\n\n"
                            + "Una radio vintage de banda ciudadana (CB) de tres canales de los años 80. Voluminosa, simple y fiable como un ladrillo. Cuenta con una larga antena telescópica y funciona con pilas AA. Debido a su anticuado circuito analógico y su selección limitada de canales no es apta para operaciones tácticas serias, pero como herramienta de comunicación barata para principiantes funciona sin fallos. Se encontró una de estas encajada detrás de una estantería en el sótano de una casa en las afueras. Unas luces navideñas recorrían la pared junto al marco de la puerta, y en la carcasa había garabateado, con letra infantil, nombres, indicativos y algo sobre una \"puerta\". Bueno, cosas más extrañas han pasado, supongo..."
                    },
                    ["fr"] = new LocaleDetails
                    {
                        Name = "Radio portable Realistic TRC-83",
                        ShortName = "TRC-83",
                        Description = "Portée : 30 m clair, dégradation jusqu'à 200 m, uniquement de la friture jusqu'à 300 m\n"
                            + "Mode duplex : Semi-duplex\n"
                            + "Qualité du signal : Très faible\n\n"
                            + "Une radio CB (Citizens Band) vintage à trois canaux datant des années 1980. Encombrante, simple et fiable comme une brique. Elle est équipée d'une longue antenne télescopique et fonctionne avec des piles AA. En raison de son circuit analogique dépassé et de son choix de canaux limité, elle ne convient pas aux opérations tactiques sérieuses, mais comme outil de communication bon marché pour débutants, elle fonctionne sans faille. L'une d'elles a été retrouvée coincée derrière une étagère de sous-sol dans une maison en périphérie. Une guirlande de Noël courait le long du mur près de l'embrasure de la porte, et sur le boîtier était gravée, d'une écriture enfantine, des noms, des indicatifs et quelque chose à propos d'une \"porte\". Enfin, on a déjà vu plus étrange, je suppose..."
                    },
                    ["pl"] = new LocaleDetails
                    {
                        Name = "Radiotelefon przenośny Realistic TRC-83",
                        ShortName = "TRC-83",
                        Description = "Zasięg: 30 m czysty odbiór, pogorszenie do 200 m, tylko szum do 300 m\n"
                            + "Tryb dupleksu: Półdupleks\n"
                            + "Jakość sygnału: Bardzo niska\n\n"
                            + "Zabytkowy trzykanałowy radiotelefon CB (Citizens Band) z lat 80. Nieporęczny, prosty i niezawodny jak cegła. Wyposażony w długą teleskopową antenę, zasilany bateriami AA. Ze względu na przestarzały analogowy układ i ograniczony wybór kanałów nie nadaje się do poważnych operacji taktycznych, ale jako tani sprzęt komunikacyjny dla początkujących działa bez zarzutu. Jeden z nich znaleziono zaklinowany za półką w piwnicy domu na obrzeżach miasta. Wzdłuż ściany przy futrynie drzwi ciągnęły się lampki choinkowe, a na obudowie dziecięcym pismem wydrapano imiona, znaki wywoławcze i coś o „bramie”. Cóż, dziwniejsze rzeczy się zdarzały, jak sądzę..."
                    },
                    ["it"] = new LocaleDetails
                    {
                        Name = "Radio portatile Realistic TRC-83",
                        ShortName = "TRC-83",
                        Description = "Portata: 30 m chiaro, degrado fino a 200 m, solo statica fino a 300 m\n"
                            + "Modalità duplex: Semiduplex\n"
                            + "Qualità del segnale: Molto bassa\n\n"
                            + "Una radio CB (Citizens Band) vintage a tre canali degli anni '80. Ingombrante, semplice e affidabile come un mattone. È dotata di una lunga antenna telescopica e funziona a batterie AA. A causa del suo circuito analogico datato e della selezione limitata dei canali non è adatta a operazioni tattiche serie, ma come economico strumento di comunicazione per principianti funziona perfettamente. Una di queste è stata trovata incastrata dietro uno scaffale in cantina in una casa alla periferia. Lungo la parete accanto allo stipite della porta correva una fila di luci natalizie, e sulla scocca era incisa, con una grafia infantile, una serie di nomi, nominativi radio e qualcosa a proposito di un \"cancello\". Beh, cose più strane sono già successe, immagino..."
                    },
                    ["cz"] = new LocaleDetails
                    {
                        Name = "Přenosná vysílačka Realistic TRC-83",
                        ShortName = "TRC-83",
                        Description = "Dosah: 30 m čistý příjem, zhoršení do 200 m, pouze šum do 300 m\n"
                            + "Duplexní režim: Poloduplexní\n"
                            + "Kvalita signálu: Velmi nízká\n\n"
                            + "Retro třikanálová vysílačka občanského pásma (CB) z 80. let. Neskladná, jednoduchá a spolehlivá jako cihla. Je vybavena dlouhou teleskopickou anténou a napájena bateriemi typu AA. Kvůli zastaralému analogovému obvodu a omezenému výběru kanálů se nehodí pro seriózní taktické operace, ale jako levný komunikační prostředek pro začátečníky funguje bezchybně. Jedna z nich byla nalezena zaklíněná za regálem ve sklepě jednoho domu na okraji města. Podél stěny u veřejí dveří se táhla girlanda světýlek a na plášti bylo dětským písmem naškrábáno: jména, volací znaky, něco o „bráně“. No, divnější věci se už staly, předpokládám..."
                    }
                }
            });

            _customItemService.CreateItemFromClone(new NewItemFromCloneDetails
            {
                ItemTplToClone = radioCloneBase,
                ParentId = radioParentId,
                NewId = AlincoTplId,
                HandbookParentId = radioHandbookParentId,
                HandbookPriceRoubles = 42700,

                FleaPriceRoubles = 51240,
                OverrideProperties = new TemplateItemProperties
                {
                    Width = 1,
                    Height = 1,

                    Weight = 0.17,
                    BackgroundColor = radioBackgroundColor,
                    RarityPvE = radioRarityPvE,
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
                    },
                    ["ge"] = new LocaleDetails
                    {
                        Name = "Alinco (Fake) Tragbares Funkgerät",
                        ShortName = "Alinco",
                        Description = "Reichweite: 75 m klar, Verschlechterung bis 300 m, nur Rauschen bis 400 m\n"
                            + "Duplex-Modus: Halbduplex\n"
                            + "Signalqualität: Sehr niedrig - Niedrig\n\n"
                            + "Ein billiges chinesisches Funkgerät mit einem hastig aufgeklebten Logo der japanischen Marke Alinco. Das genaue Modell lässt sich nicht bestimmen, da der Originalhersteller nie etwas Ähnliches produziert hat. Trotz seiner zweifelhaften Herkunft und der groben Plastikverarbeitung funktioniert dieses namenlose Gerät zuverlässig genug auf zivilen Frequenzen. Eine einfache, erschwingliche Kommunikationsoption, deren Verlust einem nicht leid tut."
                    },
                    ["es"] = new LocaleDetails
                    {
                        Name = "Radio portátil Alinco (Fake)",
                        ShortName = "Alinco",
                        Description = "Alcance: 75 m claro, degradándose hasta 300 m, solo estática hasta 400 m\n"
                            + "Modo dúplex: Semidúplex\n"
                            + "Calidad de señal: Muy baja - Baja\n\n"
                            + "Una radio china barata con un logotipo de la marca japonesa Alinco pegado a toda prisa. No se puede identificar el modelo concreto, ya que el fabricante original nunca produjo nada parecido. A pesar de su dudoso origen y su tosco plástico, este dispositivo sin marca funciona con fiabilidad suficiente en frecuencias civiles. Una opción de comunicación simple y asequible que no dolerá perder."
                    },
                    ["fr"] = new LocaleDetails
                    {
                        Name = "Radio portable Alinco (Fake)",
                        ShortName = "Alinco",
                        Description = "Portée : 75 m clair, dégradation jusqu'à 300 m, uniquement de la friture jusqu'à 400 m\n"
                            + "Mode duplex : Semi-duplex\n"
                            + "Qualité du signal : Très faible - Faible\n\n"
                            + "Une radio chinoise bon marché avec un logo de la marque japonaise Alinco collé à la hâte. Le modèle exact ne peut être identifié, car le fabricant d'origine n'a jamais produit quoi que ce soit de semblable. Malgré son origine douteuse et son plastique grossier, cet appareil sans marque fonctionne assez fiablement sur les fréquences civiles. Une option de communication simple et abordable qu'on ne regrette pas de perdre."
                    },
                    ["pl"] = new LocaleDetails
                    {
                        Name = "Radiotelefon przenośny Alinco (Fake)",
                        ShortName = "Alinco",
                        Description = "Zasięg: 75 m czysty odbiór, pogorszenie do 300 m, tylko szum do 400 m\n"
                            + "Tryb dupleksu: Półdupleks\n"
                            + "Jakość sygnału: Bardzo niska - Niska\n\n"
                            + "Tania chińska rejestrowa z pospiesznie naklejonym logo japońskiej marki Alinco. Nie da się zidentyfikować konkretnego modelu, ponieważ oryginalny producent nigdy nie wypuścił niczego podobnego. Mimo wątpliwego pochodzenia i szorstkiego plastiku to bezimienne urządzenie działa wystarczająco niezawodnie na częstotliwościach cywilnych. Prosta i przystępna cenowo opcja łączności, której nie żal stracić."
                    },
                    ["it"] = new LocaleDetails
                    {
                        Name = "Radio portatile Alinco (Fake)",
                        ShortName = "Alinco",
                        Description = "Portata: 75 m chiaro, degrado fino a 300 m, solo statica fino a 400 m\n"
                            + "Modalità duplex: Semiduplex\n"
                            + "Qualità del segnale: Molto bassa - Bassa\n\n"
                            + "Una radio cinese economica con il logo del marchio giapponese Alinco applicato in fretta. Non è possibile identificare il modello specifico, poiché il produttore originale non ha mai realizzato nulla di simile. Nonostante la dubbia provenienza e la plastica grezza, questo dispositivo senza nome funziona in modo abbastanza affidabile sulle frequenze civili. Un'opzione di comunicazione semplice ed economica che non dispiacerà perdere."
                    },
                    ["cz"] = new LocaleDetails
                    {
                        Name = "Přenosná vysílačka Alinco (Fake)",
                        ShortName = "Alinco",
                        Description = "Dosah: 75 m čistý příjem, zhoršení do 300 m, pouze šum do 400 m\n"
                            + "Duplexní režim: Poloduplexní\n"
                            + "Kvalita signálu: Velmi nízká - Nízká\n\n"
                            + "Levná čínská vysílačka s narychlo nalepeným logem japonské značky Alinco. Konkrétní model nelze určit, protože originální výrobce nic podobného nikdy nevyráběl. Navzdory pochybnému původu a hrubému plastu funguje toto bezejmenné zařízení na civilních frekvencích dostatečně spolehlivě. Jednoduchá a dostupná volba komunikace, o kterou není škoda přijít."
                    }
                }
            });

            _customItemService.CreateItemFromClone(new NewItemFromCloneDetails
            {
                ItemTplToClone = radioCloneBase,
                ParentId = radioParentId,
                NewId = KenwoodProTalkTplId,
                HandbookParentId = radioHandbookParentId,
                HandbookPriceRoubles = 67000,

                FleaPriceRoubles = 80010,
                OverrideProperties = new TemplateItemProperties
                {
                    Width = 1,
                    Height = 1,

                    Weight = 0.16,
                    BackgroundColor = radioBackgroundColor,
                    RarityPvE = radioRarityPvE,
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
                            + "Duplex mode: Half-duplex | Duplex\n"
                            + "Signal quality: Medium\n\n"
                            + "A lightweight, compact UHF-band radio, originally designed for business and service industries. It has a convenient LCD screen, simple controls, and a built-in vibration alert. Due to its low transmitter power and fully plastic housing it isn't built for serious combat, but it's a great affordable radio for short-range work."
                    },
                    ["ru"] = new LocaleDetails
                    {
                        Name = "Портативная рация Kenwood ProTalk XLS",
                        ShortName = "ProTalk XLS",
                        Description = "Дальность: 90м чистого приёма, деградация до 295м, шум до 425м\n"
                            + "Режим связи: полудуплекс | дуплекс\n"
                            + "Качество сигнала: среднее\n\n"
                            + "Легкая и компактная рация дециметрового диапазона (UHF), изначально созданная для бизнеса и сферы услуг. Имеет удобный ЖК-экран, простое управление и встроенный вибровызов. Из-за невысокой мощности передатчика и полностью пластикового корпуса не рассчитана на серьезный бой, но отлично подходит как доступная рация для работы на коротких дистанциях."
                    },
                    ["ge"] = new LocaleDetails
                    {
                        Name = "Kenwood ProTalk XLS Tragbares Funkgerät",
                        ShortName = "ProTalk XLS",
                        Description = "Reichweite: 90 m klar, Verschlechterung bis 295 m, nur Rauschen bis 425 m\n"
                            + "Duplex-Modus: Halbduplex | Duplex\n"
                            + "Signalqualität: Mittel\n\n"
                            + "Ein leichtes, kompaktes Funkgerät im UHF-Band, ursprünglich für Geschäfts- und Dienstleistungsbranchen entwickelt. Es verfügt über einen praktischen LCD-Bildschirm, einfache Bedienung und einen eingebauten Vibrationsalarm. Aufgrund seiner geringen Sendeleistung und des vollständig aus Plastik gefertigten Gehäuses ist es nicht für ernsthafte Kampfeinsätze gebaut, aber es ist ein hervorragendes, erschwingliches Funkgerät für Arbeiten auf kurzer Distanz."
                    },
                    ["es"] = new LocaleDetails
                    {
                        Name = "Radio portátil Kenwood ProTalk XLS",
                        ShortName = "ProTalk XLS",
                        Description = "Alcance: 90 m claro, degradándose hasta 295 m, solo estática hasta 425 m\n"
                            + "Modo dúplex: Semidúplex | Dúplex\n"
                            + "Calidad de señal: Media\n\n"
                            + "Una radio ligera y compacta en banda UHF, diseñada originalmente para el sector empresarial y de servicios. Cuenta con una práctica pantalla LCD, controles sencillos y una alerta de vibración incorporada. Debido a su baja potencia de transmisión y su carcasa totalmente de plástico no está pensada para un combate serio, pero es una excelente radio asequible para trabajar a corta distancia."
                    },
                    ["fr"] = new LocaleDetails
                    {
                        Name = "Radio portable Kenwood ProTalk XLS",
                        ShortName = "ProTalk XLS",
                        Description = "Portée : 90 m clair, dégradation jusqu'à 295 m, uniquement de la friture jusqu'à 425 m\n"
                            + "Mode duplex : Semi-duplex | Duplex\n"
                            + "Qualité du signal : Moyenne\n\n"
                            + "Une radio légère et compacte en bande UHF, conçue à l'origine pour les secteurs du commerce et des services. Elle dispose d'un écran LCD pratique, de commandes simples et d'une alerte vibrante intégrée. En raison de sa faible puissance d'émission et de son boîtier entièrement en plastique, elle n'est pas conçue pour un combat sérieux, mais c'est une excellente radio abordable pour un travail à courte distance."
                    },
                    ["pl"] = new LocaleDetails
                    {
                        Name = "Radiotelefon przenośny Kenwood ProTalk XLS",
                        ShortName = "ProTalk XLS",
                        Description = "Zasięg: 90 m czysty odbiór, pogorszenie do 295 m, tylko szum do 425 m\n"
                            + "Tryb dupleksu: Półdupleks | Dupleks\n"
                            + "Jakość sygnału: Średnia\n\n"
                            + "Lekki, kompaktowy radiotelefon pasma UHF, pierwotnie zaprojektowany dla biznesu i branży usługowej. Posiada wygodny wyświetlacz LCD, proste sterowanie oraz wbudowany alarm wibracyjny. Ze względu na niską moc nadajnika i w pełni plastikową obudowę nie jest przystosowany do poważnej walki, ale świetnie sprawdza się jako niedrogi radiotelefon do pracy na krótkim dystansie."
                    },
                    ["it"] = new LocaleDetails
                    {
                        Name = "Radio portatile Kenwood ProTalk XLS",
                        ShortName = "ProTalk XLS",
                        Description = "Portata: 90 m chiaro, degrado fino a 295 m, solo statica fino a 425 m\n"
                            + "Modalità duplex: Semiduplex | Duplex\n"
                            + "Qualità del segnale: Media\n\n"
                            + "Una radio leggera e compatta in banda UHF, originariamente progettata per il settore commerciale e dei servizi. Dispone di un comodo schermo LCD, comandi semplici e un allarme a vibrazione integrato. A causa della bassa potenza del trasmettitore e della scocca interamente in plastica non è adatta a un combattimento serio, ma è un'ottima radio economica per lavorare a corto raggio."
                    },
                    ["cz"] = new LocaleDetails
                    {
                        Name = "Přenosná vysílačka Kenwood ProTalk XLS",
                        ShortName = "ProTalk XLS",
                        Description = "Dosah: 90 m čistý příjem, zhoršení do 295 m, pouze šum do 425 m\n"
                            + "Duplexní režim: Poloduplexní | Duplexní\n"
                            + "Kvalita signálu: Střední\n\n"
                            + "Lehká, kompaktní vysílačka v pásmu UHF, původně navržená pro obchodní a servisní odvětví. Má praktickou LCD obrazovku, jednoduché ovládání a vestavěné vibrační upozornění. Kvůli nízkému výkonu vysílače a celoplastovému pouzdru není určena pro seriózní boj, ale je to skvělá dostupná vysílačka pro práci na krátkou vzdálenost."
                    }
                }
            });

            _customItemService.CreateItemFromClone(new NewItemFromCloneDetails
            {
                ItemTplToClone = radioCloneBase,
                ParentId = radioParentId,
                NewId = Mth800TplId,
                HandbookParentId = radioHandbookParentId,
                HandbookPriceRoubles = 70000,

                FleaPriceRoubles = 89220,
                OverrideProperties = new TemplateItemProperties
                {
                    Width = 1,
                    Height = 1,

                    Weight = 0.2,
                    BackgroundColor = radioBackgroundColor,
                    RarityPvE = radioRarityPvE,
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
                            + "Duplex mode: Half-duplex | Duplex\n"
                            + "Signal quality: Low-medium\n\n"
                            + "A digital TETRA-standard radio, built for emergency services and police. Rugged, dust- and water-resistant, with a color display and encryption support. In the reality of Tarkov, it's a great transitional step up from simple civilian \"chatterboxes\" toward closed professional communication. Its secure digital signal is practically impossible to intercept with an ordinary frequency scanner."
                    },
                    ["ru"] = new LocaleDetails
                    {
                        Name = "Портативная рация Motorola MTH800",
                        ShortName = "MTH800",
                        Description = "Дальность: 175м чистого приёма, деградация до 425м, шум до 490м\n"
                            + "Режим связи: полудуплекс | дуплекс\n"
                            + "Качество сигнала: низкое-среднее\n\n"
                            + "Цифровая радиостанция стандарта TETRA, созданная для экстренных служб и полиции. Прочная, пылевлагозащищенная, с цветным дисплеем и поддержкой шифрования. В реалиях Таркова — это отличный переходный шаг от простых гражданских \"болталок\" к закрытой профессиональной связи. Её защищенный цифровой сигнал практически невозможно перехватить обычным сканером частот."
                    },
                    ["ge"] = new LocaleDetails
                    {
                        Name = "Motorola MTH800 Tragbares Funkgerät",
                        ShortName = "MTH800",
                        Description = "Reichweite: 175 m klar, Verschlechterung bis 425 m, nur Rauschen bis 490 m\n"
                            + "Duplex-Modus: Halbduplex | Duplex\n"
                            + "Signalqualität: Niedrig-mittel\n\n"
                            + "Ein digitales Funkgerät nach dem TETRA-Standard, gebaut für Rettungsdienste und Polizei. Robust, staub- und wassergeschützt, mit Farbdisplay und Verschlüsselungsunterstützung. In der Realität von Tarkov ist es ein hervorragender Übergangsschritt von einfachen zivilen \"Plaudertaschen\" hin zu geschlossener professioneller Kommunikation. Sein gesichertes digitales Signal ist mit einem gewöhnlichen Frequenzscanner praktisch unmöglich abzufangen."
                    },
                    ["es"] = new LocaleDetails
                    {
                        Name = "Radio portátil Motorola MTH800",
                        ShortName = "MTH800",
                        Description = "Alcance: 175 m claro, degradándose hasta 425 m, solo estática hasta 490 m\n"
                            + "Modo dúplex: Semidúplex | Dúplex\n"
                            + "Calidad de señal: Baja-media\n\n"
                            + "Una radio digital de estándar TETRA, construida para servicios de emergencia y policía. Robusta, resistente al polvo y al agua, con pantalla en color y soporte de cifrado. En la realidad de Tarkov es un excelente paso de transición desde las simples \"charlatanas\" civiles hacia una comunicación profesional cerrada. Su señal digital segura es prácticamente imposible de interceptar con un escáner de frecuencias corriente."
                    },
                    ["fr"] = new LocaleDetails
                    {
                        Name = "Radio portable Motorola MTH800",
                        ShortName = "MTH800",
                        Description = "Portée : 175 m clair, dégradation jusqu'à 425 m, uniquement de la friture jusqu'à 490 m\n"
                            + "Mode duplex : Semi-duplex | Duplex\n"
                            + "Qualité du signal : Faible-moyenne\n\n"
                            + "Une radio numérique de standard TETRA, conçue pour les services d'urgence et la police. Robuste, résistante à la poussière et à l'eau, avec un écran couleur et un support de chiffrement. Dans la réalité de Tarkov, c'est une excellente étape de transition entre les simples \"bavardes\" civiles et une communication professionnelle fermée. Son signal numérique sécurisé est pratiquement impossible à intercepter avec un scanner de fréquences ordinaire."
                    },
                    ["pl"] = new LocaleDetails
                    {
                        Name = "Radiotelefon przenośny Motorola MTH800",
                        ShortName = "MTH800",
                        Description = "Zasięg: 175 m czysty odbiór, pogorszenie do 425 m, tylko szum do 490 m\n"
                            + "Tryb dupleksu: Półdupleks | Dupleks\n"
                            + "Jakość sygnału: Niska-średnia\n\n"
                            + "Cyfrowy radiotelefon w standardzie TETRA, zbudowany dla służb ratunkowych i policji. Wytrzymały, odporny na kurz i wodę, z kolorowym wyświetlaczem i obsługą szyfrowania. W realiach Tarkova to świetny krok przejściowy od prostych cywilnych „gadek” do zamkniętej, profesjonalnej łączności. Jego zabezpieczony sygnał cyfrowy jest praktycznie niemożliwy do przechwycenia zwykłym skanerem częstotliwości."
                    },
                    ["it"] = new LocaleDetails
                    {
                        Name = "Radio portatile Motorola MTH800",
                        ShortName = "MTH800",
                        Description = "Portata: 175 m chiaro, degrado fino a 425 m, solo statica fino a 490 m\n"
                            + "Modalità duplex: Semiduplex | Duplex\n"
                            + "Qualità del segnale: Bassa-media\n\n"
                            + "Una radio digitale di standard TETRA, costruita per i servizi di emergenza e la polizia. Robusta, resistente a polvere e acqua, con display a colori e supporto per la crittografia. Nella realtà di Tarkov è un ottimo passaggio di transizione dalle semplici \"chiacchierone\" civili a una comunicazione professionale chiusa. Il suo segnale digitale protetto è praticamente impossibile da intercettare con un normale scanner di frequenze."
                    },
                    ["cz"] = new LocaleDetails
                    {
                        Name = "Přenosná vysílačka Motorola MTH800",
                        ShortName = "MTH800",
                        Description = "Dosah: 175 m čistý příjem, zhoršení do 425 m, pouze šum do 490 m\n"
                            + "Duplexní režim: Poloduplexní | Duplexní\n"
                            + "Kvalita signálu: Nízká-střední\n\n"
                            + "Digitální vysílačka standardu TETRA, postavená pro záchranné složky a policii. Odolná, prachotěsná a voděodolná, s barevným displejem a podporou šifrování. V reáliích Tarkova je to skvělý přechodový krok od jednoduchých civilních „žvanilek“ k uzavřené profesionální komunikaci. Její zabezpečený digitální signál je prakticky nemožné zachytit běžným skenerem frekvencí."
                    }
                }
            });

            AddRadiosToTraders();
            int patchedContainers = ExcludeFromSecuredContainers();
            EnableTraderBuyback();
            AddRadiosToWorldLoot();

            if (!fikaInstalled)
            {
                PatchHallOfFame();
            }

            LogStartupBanner(patchedContainers, fikaInstalled);

            return Task.CompletedTask;
        }

        private static bool IsFikaServerInstalled()
        {
            return System.AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => a.GetName().Name == "FikaServer");
        }

        private void PatchHallOfFame()
        {
            var items = _databaseService.GetTables().Templates.Items;

            var smallRadioIds = new HashSet<MongoId> { new MongoId(T460TplId), new MongoId(AlincoTplId), new MongoId(KenwoodProTalkTplId), new MongoId(Mth800TplId) };

            var bigRadioIds = new HashSet<MongoId> { new MongoId(BaofengTplId), new MongoId(AzartTplId), new MongoId(KenwoodTplId), new MongoId(YaesuTplId), new MongoId(Dp4800TplId), new MongoId(Dp4601eTplId), new MongoId(Xts5000TplId), new MongoId(HarrisTplId), new MongoId(Trc83TplId) };

            foreach (string containerTplId in new[] { HallOfFameContainerLvl1TplId, HallOfFameContainerLvl2TplId, HallOfFameContainerLvl3TplId })
            {
                if (!items.TryGetValue(new MongoId(containerTplId), out TemplateItem? hallOfFame) || hallOfFame.Properties?.Slots == null)
                {
                    continue;
                }

                foreach (Slot slot in hallOfFame.Properties.Slots)
                {
                    bool isSmall = slot.Name != null && slot.Name.StartsWith("smallTrophies");
                    bool isBig = slot.Name != null && slot.Name.StartsWith("bigTrophies");
                    if (!isSmall && !isBig)
                    {
                        continue;
                    }

                    if (slot.Properties?.Filters == null)
                    {
                        continue;
                    }

                    HashSet<MongoId> idsForThisSlot = isSmall ? smallRadioIds : bigRadioIds;

                    foreach (SlotFilter filter in slot.Properties.Filters)
                    {
                        filter.Filter ??= new HashSet<MongoId>();
                        foreach (MongoId id in idsForThisSlot)
                        {
                            filter.Filter.Add(id);
                        }
                    }
                }
            }
        }

        private void EnableTraderBuyback()
        {
            var radioIds = new HashSet<MongoId> { new MongoId(BaofengTplId), new MongoId(AzartTplId), new MongoId(KenwoodTplId), new MongoId(T460TplId), new MongoId(YaesuTplId), new MongoId(Dp4800TplId), new MongoId(Dp4601eTplId), new MongoId(Xts5000TplId), new MongoId(HarrisTplId), new MongoId(Trc83TplId), new MongoId(AlincoTplId), new MongoId(KenwoodProTalkTplId), new MongoId(Mth800TplId) };
            var buybackTraderIds = new[] { PeacekeeperTraderId, MechanicTraderId };
            var traders = _databaseService.GetTables().Traders;

            foreach (string traderId in buybackTraderIds)
            {
                TraderBase traderBase = traders[new MongoId(traderId)].Base;
                traderBase.ItemsBuy.IdList ??= new HashSet<MongoId>();

                foreach (MongoId id in radioIds)
                {
                    traderBase.ItemsBuy.IdList.Add(id);
                }
            }
        }

        private void LogStartupBanner(int patchedContainers, bool fikaInstalled)
        {
            const int width = 48;
            string?[] lines =
            {
                "PRT " + DisplayVersion,
                null,
                "Radios loaded: 13",
                "Fika detected: " + (fikaInstalled ? "yes (full radio functionality)" : "no (collectible-only mode)"),
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

        private static readonly Dictionary<string, int> RadioHandbookPriceByTplId = new Dictionary<string, int>
        {
            [BaofengTplId] = 25500,
            [AzartTplId] = 150000,
            [KenwoodTplId] = 215750,
            [T460TplId] = 40000,
            [YaesuTplId] = 72000,
            [Dp4800TplId] = 85000,
            [Dp4601eTplId] = 89990,
            [Xts5000TplId] = 100000,
            [HarrisTplId] = 200000,
            [Trc83TplId] = 300000,
            [AlincoTplId] = 42700,
            [KenwoodProTalkTplId] = 67000,
            [Mth800TplId] = 70000,
        };

        private static readonly string[] CivilianLootRadioIds =
        {
            BaofengTplId, KenwoodTplId, T460TplId, YaesuTplId, Dp4800TplId, Dp4601eTplId, Trc83TplId, AlincoTplId, KenwoodProTalkTplId
        };

        private static readonly string[] MilitaryLootRadioIds =
        {
            AzartTplId, Xts5000TplId, HarrisTplId, Mth800TplId
        };

        private static int GetLootSpawnWeight(string radioTplId)
        {
            int price = RadioHandbookPriceByTplId.TryGetValue(radioTplId, out int p) ? p : 100000;
            if (price < 50000)
            {
                return 12;
            }
            if (price < 100000)
            {
                return 7;
            }
            if (price < 180000)
            {
                return 4;
            }
            return 2;
        }

        private static uint StableHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in value)
                {
                    hash ^= c;
                    hash *= 16777619;
                }
                return hash;
            }
        }

        private static string PickWeightedRadio(string[] radioPool, uint hash)
        {
            int totalWeight = 0;
            foreach (string tplId in radioPool)
            {
                totalWeight += GetLootSpawnWeight(tplId);
            }

            int roll = (int)(hash % (uint)totalWeight);
            int accumulated = 0;
            foreach (string tplId in radioPool)
            {
                accumulated += GetLootSpawnWeight(tplId);
                if (roll < accumulated)
                {
                    return tplId;
                }
            }

            return radioPool[radioPool.Length - 1];
        }

        private const string LootMarkerPrefix = "prt-radio-";

        private static readonly HashSet<string> ThematicRootCategoryIds = new HashSet<string>
        {
            "5b5f78dc86f77409407a7f8e",
            "5b5f71a686f77447ed5636ab",
            "5b47574386f77428ca22b33f",
            "5b47574386f77428ca22b346",
            "5b47574386f77428ca22b2ef",
            "5b47574386f77428ca22b2ed",
            "5b47574386f77428ca22b2f4",
            "5b47574386f77428ca22b2f0",
            "5b47574386f77428ca22b2ee",
            "5b47574386f77428ca22b2f2",
            "5b47574386f77428ca22b2f6",
        };

        private static bool IsThematicItem(string tplId, Dictionary<string, string> itemCategoryId, Dictionary<string, string?> categoryParentId)
        {
            if (!itemCategoryId.TryGetValue(tplId, out string? categoryId))
            {
                return false;
            }

            string? current = categoryId;
            for (int depth = 0; depth < 10 && current != null; depth++)
            {
                if (ThematicRootCategoryIds.Contains(current))
                {
                    return true;
                }

                categoryParentId.TryGetValue(current, out current);
            }

            return false;
        }

        private void AddRadiosToLooseLoot(Location? location, string[] radioPool, string poolNamespace, uint injectionChancePerMille,
            Dictionary<string, string> itemCategoryId, Dictionary<string, string?> categoryParentId)
        {
            if (location?.LooseLoot == null || radioPool.Length == 0)
            {
                return;
            }

            string markerPrefix = LootMarkerPrefix + poolNamespace + "-";

            location.LooseLoot.AddTransformer(loot =>
            {
                if (loot?.Spawnpoints == null)
                {
                    return loot;
                }

                var spawnpoints = loot.Spawnpoints.ToList();
                foreach (Spawnpoint sp in spawnpoints)
                {
                    if (sp.Template?.Items == null || sp.ItemDistribution == null || sp.Template.IsContainer == true)
                    {
                        continue;
                    }

                    var items = sp.Template.Items.ToList();
                    if (items.Any(i => i.ComposedKey != null && i.ComposedKey.StartsWith(markerPrefix)))
                    {
                        continue;
                    }

                    if (!items.Any(i => IsThematicItem((string)i.Template, itemCategoryId, categoryParentId)))
                    {
                        continue;
                    }

                    uint hash = StableHash(poolNamespace + "|" + (sp.Template.Id ?? ""));
                    if (hash % 1000 >= injectionChancePerMille)
                    {
                        continue;
                    }

                    string radioTplId = PickWeightedRadio(radioPool, hash);
                    string composedKey = markerPrefix + radioTplId;

                    items.Add(new SptLootItem
                    {
                        Id = new MongoId(),
                        Template = radioTplId,
                        ComposedKey = composedKey,
                        Upd = new Upd { StackObjectsCount = 1 }
                    });

                    var distribution = sp.ItemDistribution.ToList();
                    distribution.Add(new LooseLootItemDistribution
                    {
                        ComposedKey = new ComposedKey { Key = composedKey },
                        RelativeProbability = GetLootSpawnWeight(radioTplId)
                    });

                    sp.Template.Items = items;
                    sp.ItemDistribution = distribution;
                }

                loot.Spawnpoints = spawnpoints;
                return loot;
            });
        }

        private static readonly HashSet<string> AllowedRadioContainerTplIds = new HashSet<string>
        {
            "5909e4b686f7747f5b744fa4",
            "6582e6d7b14c3f72eb071420",
            "658420d8085fea07e674cdb6",
            "6582e6bb0c3b9823fe6d1840",
            "6582e6c6edf14c4c6023adf2",
            "5d07b91b86f7745a077a9432",
            "578f8778245977358849a9b5",
            "578f87b7245977356274f2cd",
            "5909d50c86f774659e6aaebe",
            "5d6fd45b86f774317075ed43",
            "578f87a3245977356274f2cb",
            "61aa1e9a32a4743c3453d2cf",
            "5c052cea86f7746b2101e8d8",
            "5909d5ef86f77467974efbd8",
            "5909d76c86f77471e53d2adf",
            "5909d7cf86f77470ee57d75a",
            "5909d89086f77472591234a0",
            "5d6d2bb386f774785b07a77a",
            "5d6d2b5486f774785c2ba8ea",
        };

        private void AddRadiosToStaticLoot(Location? location, string[] radioPool, string poolNamespace, uint injectionChancePerMille)
        {
            if (location?.StaticLoot == null || radioPool.Length == 0)
            {
                return;
            }

            var radioPoolSet = new HashSet<string>(radioPool);

            location.StaticLoot.AddTransformer(staticLoot =>
            {
                if (staticLoot == null)
                {
                    return staticLoot;
                }

                foreach (KeyValuePair<MongoId, StaticLootDetails> entry in staticLoot)
                {
                    if (!AllowedRadioContainerTplIds.Contains((string)entry.Key))
                    {
                        continue;
                    }

                    StaticLootDetails details = entry.Value;
                    if (details?.ItemDistribution == null)
                    {
                        continue;
                    }

                    var distribution = details.ItemDistribution.ToList();
                    if (distribution.Any(d => radioPoolSet.Contains((string)d.Tpl)))
                    {
                        continue;
                    }

                    uint hash = StableHash(poolNamespace + "|static|" + (string)entry.Key);
                    if (hash % 1000 >= injectionChancePerMille)
                    {
                        continue;
                    }

                    string radioTplId = PickWeightedRadio(radioPool, hash);
                    distribution.Add(new ItemDistribution
                    {
                        Tpl = radioTplId,
                        RelativeProbability = GetLootSpawnWeight(radioTplId)
                    });

                    details.ItemDistribution = distribution;
                }

                return staticLoot;
            });
        }

        private void AddRadiosToWorldLoot()
        {
            SPTarkov.Server.Core.Models.Spt.Server.Locations locations = _databaseService.GetTables().Locations;

            Location?[] civilianMaps =
            {
                locations.Factory4Day, locations.Factory4Night, locations.Bigmap, locations.Woods,
                locations.Lighthouse, locations.Shoreline, locations.Interchange, locations.TarkovStreets,
                locations.Sandbox, locations.SandboxHigh
            };
            Location?[] militaryMaps =
            {
                locations.Lighthouse, locations.RezervBase, locations.Sandbox, locations.SandboxHigh, locations.Laboratory
            };

            const uint looseLootChancePerMille = 15;
            const uint staticLootChancePerMille = 60;

            HandbookBase handbook = _databaseService.GetTables().Templates.Handbook;
            var itemCategoryId = new Dictionary<string, string>();
            foreach (HandbookItem hi in handbook.Items)
            {
                itemCategoryId[(string)hi.Id] = (string)hi.ParentId;
            }

            var categoryParentId = new Dictionary<string, string?>();
            foreach (HandbookCategory hc in handbook.Categories)
            {
                categoryParentId[(string)hc.Id] = hc.ParentId.HasValue ? (string)hc.ParentId.Value : null;
            }

            foreach (Location? map in civilianMaps)
            {
                AddRadiosToLooseLoot(map, CivilianLootRadioIds, "civ", looseLootChancePerMille, itemCategoryId, categoryParentId);
                AddRadiosToStaticLoot(map, CivilianLootRadioIds, "civ", staticLootChancePerMille);
            }

            foreach (Location? map in militaryMaps)
            {
                AddRadiosToLooseLoot(map, MilitaryLootRadioIds, "mil", looseLootChancePerMille, itemCategoryId, categoryParentId);
                AddRadiosToStaticLoot(map, MilitaryLootRadioIds, "mil", staticLootChancePerMille);
            }

            const string FlaregunCorpseContainerId = "container_Sandbox_Design_Stuff_00011";
            ForceItemIntoContainer(locations.Sandbox, FlaregunCorpseContainerId, BaofengTplId);
            ForceItemIntoContainer(locations.SandboxHigh, FlaregunCorpseContainerId, BaofengTplId);
        }

        private void ForceItemIntoContainer(Location? location, string containerId, string itemTplId)
        {
            if (location?.StaticContainers == null)
            {
                return;
            }

            location.StaticContainers.AddTransformer(staticContainers =>
            {
                if (staticContainers == null)
                {
                    return staticContainers;
                }

                var forced = (staticContainers.StaticForced ?? Enumerable.Empty<StaticForced>()).ToList();
                if (forced.Any(f => f.ContainerId == containerId && f.ItemTpl == new MongoId(itemTplId)))
                {
                    return staticContainers;
                }

                forced.Add(new StaticForced { ContainerId = containerId, ItemTpl = itemTplId });
                staticContainers.StaticForced = forced;
                return staticContainers;
            });
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

            AddAssortEntry(mechanicAssort, "6d6f645f7261646974726338", Trc83TplId, 314654, 1, 7, 1, 2);

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

