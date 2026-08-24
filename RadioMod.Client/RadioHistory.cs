using System.Collections.Generic;

namespace RadioMod.Client
{
    /// <summary>
    /// Short histories for the thirteen radios, shown in a collapsed block on the RADIOS tab.
    ///
    /// Written as prose rather than as a spec sheet — the passport above already carries the numbers.
    /// This is the part that explains why a radio is the way it is, which is what makes carrying a
    /// 1980s two-metre handheld feel like a choice instead of a worse stat line.
    ///
    /// Russian and English only. The remaining six languages fall back to English rather than being
    /// machine-translated: thirteen paragraphs of invented-sounding history in a language nobody
    /// checked would undermine the accuracy the rest of the mod is built on.
    /// </summary>
    public partial class Plugin
    {
        private struct RadioHistory
        {
            public string Ru;
            public string En;
        }

        private static readonly Dictionary<string, RadioHistory> RadioHistories = new Dictionary<string, RadioHistory>
        {
            [KenwoodTplId] = new RadioHistory
            {
                Ru = "Начало восьмидесятых — время, когда носимую станцию носили во внутреннем кармане, а не на разгрузке. "
                   + "TH-21BT был одним из самых миниатюрных приёмопередатчиков своего поколения: размером с пачку сигарет, "
                   + "один диапазон, минимум органов управления. Дальности от него никто и не ждал — ценили за то, что его "
                   + "можно было взять с собой не задумываясь.",
                En = "The early eighties, when a handheld lived in an inside pocket rather than on a chest rig. The TH-21BT "
                   + "was among the smallest transceivers of its generation: the size of a cigarette pack, one band, almost "
                   + "no controls. Nobody expected reach from it — it was valued for being the radio you took along without "
                   + "thinking about it.",
            },

            [Trc83TplId] = new RadioHistory
            {
                Ru = "Realistic — собственная марка американской сети Radio Shack, где рации лежали на полке рядом с батарейками "
                   + "и паяльниками. TRC-83 застал расцвет гражданского эфира: в семидесятых и восьмидесятых на CB переговаривались "
                   + "дальнобойщики, соседи и подростки, которых родители гнали от телефона. Аппарат прост до предела, с амплитудной "
                   + "модуляцией и тем самым потрескиванием, по которому эпоху узнают до сих пор.",
                En = "Realistic was the house brand of Radio Shack, where radios sat on the shelf next to batteries and soldering "
                   + "irons. The TRC-83 caught the height of the civilian airwaves: through the seventies and eighties CB carried "
                   + "truckers, neighbours and teenagers chased off the family telephone. The set is plain to a fault — amplitude "
                   + "modulation and the particular crackle that still dates the era instantly.",
            },

            [BaofengTplId] = new RadioHistory
            {
                Ru = "Появилась в 2012 году и перевернула рынок одной-единственной характеристикой — ценой. Стоя дешевле ужина "
                   + "в кафе, UV-5R сделала любительскую связь доступной буквально каждому и разошлась миллионными тиражами по "
                   + "всему миру. Радиолюбители до сих пор спорят о чистоте её эфира, но именно она чаще любой другой оказывается "
                   + "в рюкзаке у того, кому связь понадобилась впервые.",
                En = "Released in 2012, it upended the market on a single specification: price. Cheaper than dinner out, the UV-5R "
                   + "put amateur radio within reach of anyone and shipped in the millions worldwide. Hams still argue about how "
                   + "clean it is on air, yet it is the set that most often ends up in the pack of someone who needed a radio for "
                   + "the first time.",
            },

            [AlincoTplId] = new RadioHistory
            {
                Ru = "Данных нет. Корпус повторяет очертания серийных моделей Alinco, но ни один каталог совпадения не даёт — "
                   + "ни по компоновке органов управления, ни по разъёмам, ни по маркировке платы. Серийный номер спилен до "
                   + "основания. Тот, кто её собирал, либо очень старался остаться неизвестным, либо не собирал её вовсе.",
                En = "No data. The housing follows the outline of production Alinco models, yet no catalogue returns a match — not "
                   + "by control layout, not by connectors, not by the markings on the board. The serial number has been filed down "
                   + "to bare metal. Whoever built it either worked hard to stay anonymous, or did not build it at all.",
            },

            [KenwoodProTalkTplId] = new RadioHistory
            {
                Ru = "Рабочая лошадь бизнес-диапазона: склады, стройплощадки, охрана торговых центров. Такие станции покупают не "
                   + "поштучно, а комплектами на смену, и ценят в них не дальность, а то, что они переживают падение с лесов и "
                   + "работают весь день без подзарядки. Никакой романтики эфира — чистый инструмент.",
                En = "A business-band workhorse: warehouses, building sites, shopping-centre security. Radios like this are bought "
                   + "by the crate rather than one at a time, and what matters is not range but surviving a fall from scaffolding "
                   + "and lasting a full shift on one charge. No romance of the airwaves — a tool, and nothing more.",
            },

            [T460TplId] = new RadioHistory
            {
                Ru = "Потребительская серия Talkabout — та самая, что продаётся в туристических магазинах в блистере по две штуки. "
                   + "Влагозащита, заметный корпус, расчёт на лыжников, охотников и родителей, потерявших ребёнка из виду в парке. "
                   + "В тактической обстановке смотрится нелепо ровно до того момента, когда оказывается единственным, что вообще работает.",
                En = "The consumer Talkabout line — the pair-in-a-blister-pack you find in outdoor shops. Water resistance, a "
                   + "high-visibility shell, aimed at skiers, hunters and parents who lost sight of a child in the park. It looks "
                   + "absurd in a tactical setting right up until it turns out to be the only thing still working.",
            },

            [YaesuTplId] = new RadioHistory
            {
                Ru = "Аппарат для тех, кто относится к связи как к серьёзному хобби: несколько диапазонов сразу, поддержка APRS для "
                   + "передачи координат и погодных данных, внешний Bluetooth-модуль — редкость для носимой техники того поколения. "
                   + "Насыщен функциями настолько, что инструкция к нему толщиной с книгу, и добрая половина владельцев так и не "
                   + "дочитала её до конца.",
                En = "A set for people who take radio seriously as a hobby: several bands at once, APRS support for position and "
                   + "weather data, an optional Bluetooth module — unusual for a portable of its generation. So densely featured "
                   + "that the manual reads like a book, and a good half of its owners never finished it.",
            },

            [Mth800TplId] = new RadioHistory
            {
                Ru = "Терминал стандарта TETRA — европейского цифрового стандарта для экстренных служб. Групповые вызовы, шифрование "
                   + "и приоритеты заложены в сам стандарт, а не навешены сверху: такие станции работают там, где на одном канале "
                   + "одновременно полиция, пожарные и медики. Голос звучит узнаваемо цифровым, с характерной обработкой, которую ни "
                   + "с чем не спутаешь.",
                En = "A TETRA terminal — the European digital standard for emergency services. Group calls, encryption and priorities "
                   + "are built into the standard rather than bolted on: these radios work where police, fire and paramedics share one "
                   + "channel. The voice is unmistakably digital, carrying processing artefacts you learn to recognise instantly.",
            },

            [Dp4601eTplId] = new RadioHistory
            {
                Ru = "Из линейки MOTOTRBO, работающей в цифровом стандарте DMR. Вариант с экраном и приёмником спутниковой навигации: "
                   + "диспетчер видит на карте, где находится каждая станция. Цифровой канал держит разборчивость до самой границы "
                   + "зоны, а затем обрывается разом — без долгого сползания в шум, каким заканчивается аналог.",
                En = "From the MOTOTRBO line running the digital DMR standard. This is the variant with a display and satellite "
                   + "navigation, so a dispatcher can see every radio on a map. A digital channel holds intelligibility right to the "
                   + "edge of coverage and then stops dead — none of the long slide into noise that ends an analogue link.",
            },

            [Dp4800TplId] = new RadioHistory
            {
                Ru = "Профессиональная DMR-станция того же семейства, но с упором на голос: шумоподавление, усиление разборчивости, "
                   + "работа вплотную к грохочущей технике. Там, где аналоговая станция отдаёт кашу, эта отдаёт слова.",
                En = "A professional DMR set from the same family, tuned around the voice: noise suppression, intelligibility "
                   + "processing, working right next to machinery that drowns everything out. Where an analogue radio hands you mush, "
                   + "this one hands you words.",
            },

            [Xts5000TplId] = new RadioHistory
            {
                Ru = "Стандарт P25 — американский ответ на вопрос, как заставить полицию, пожарных и спасателей разных штатов слышать "
                   + "друг друга. XTS5000 стала одной из самых массовых станций этого стандарта: сотни тысяч экземпляров у служб "
                   + "общественной безопасности США. Тяжёлая, дорогая и почти неубиваемая.",
                En = "P25 is the American answer to a blunt question: how do you make police, fire and rescue from different states "
                   + "hear each other. The XTS5000 became one of the most widely fielded radios on that standard, with hundreds of "
                   + "thousands in US public-safety service. Heavy, expensive and very close to indestructible.",
            },

            [AzartTplId] = new RadioHistory
            {
                Ru = "Российская носимая радиостанция шестого поколения, принятая на снабжение в 2010-х. Программно определяемая "
                   + "архитектура — режимы задаются прошивкой, а не схемой — и псевдослучайная перестройка рабочей частоты, при "
                   + "которой станция скачет по эфиру сотни раз в секунду. Перехватить такой сигнал, не зная закона перестройки, "
                   + "практически невозможно: в чужом приёмнике он выглядит ровным шумом.",
                En = "A Russian sixth-generation portable radio, taken into service in the 2010s. A software-defined architecture — "
                   + "modes come from firmware rather than circuitry — combined with frequency hopping that moves the set across the "
                   + "band hundreds of times a second. Without knowing the hopping pattern, intercepting it is close to impossible: "
                   + "on a hostile receiver it reads as flat noise.",
            },

            [HarrisTplId] = new RadioHistory
            {
                Ru = "Многодиапазонная военная носимая станция вооружённых сил США, ставшая фактическим эталоном тактической связи и "
                   + "разошедшаяся по армиям союзников. Работает от коротких волн до дециметровых, шифрует канал, умеет ретранслировать "
                   + "через себя чужие переговоры. То, что для остальных в этом списке считается характеристикой, здесь считается минимумом.",
                En = "A multiband military handheld of the US armed forces that became the de facto benchmark for tactical comms and "
                   + "spread through allied armies. It covers HF up through UHF, encrypts the channel, and will relay other stations "
                   + "through itself. What counts as a specification for everything else on this list counts as the baseline here.",
            },
        };

        /// <summary>History for a radio in the player's language, falling back to English.</summary>
        private string GetRadioHistory(string tplId)
        {
            if (tplId == null || !RadioHistories.TryGetValue(tplId, out RadioHistory h))
            {
                return null;
            }

            // GetLanguageCode already resolves the override and the game's own culture, so the
            // fallback rule lives in one place instead of being restated here.
            return GetLanguageCode() == "ru" ? h.Ru : h.En;
        }
    }
}
