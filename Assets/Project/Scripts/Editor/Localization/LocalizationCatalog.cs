using System.Collections.Generic;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.UI;

namespace Fabula.Incrementant.EditorTools.Localization
{
    /// <summary>
    /// Todos os textos do jogo com o português (idioma de origem, como o jogo foi escrito) e o inglês. É a fonte da tabela
    /// de strings <c>Game</c>: <see cref="LocalizationSetup"/> grava estas entradas nos Locales. Para um texto novo, acrescente a
    /// linha aqui, use a chave com <c>Loc.T</c> e rode <c>Incrementant/Setup/Configurar localização</c>.
    /// Textos de UI estáticos (rótulos montados pelo setup do HUD) são achados pelo português: o mesmo texto repetido em
    /// telas diferentes usa a mesma chave. Modelos com <c>{0}</c> são preenchidos por <c>Loc.T(chave, args)</c>.
    /// </summary>
    public static partial class LocalizationCatalog
    {
        public readonly struct Entry
        {
            public readonly string Key, Pt, En;

            public Entry(string key, string pt, string en)
            {
                Key = key;
                Pt = pt;
                En = en;
            }
        }

        static List<Entry> _all;

        public static IReadOnlyList<Entry> All => _all ?? (_all = Build());

        /// <summary>Texto em português de uma chave (a fonte do jogo), para o setup montar os rótulos estáticos.</summary>
        public static string Pt(string key)
        {
            foreach (Entry e in All)
            {
                if (e.Key == key)
                    return e.Pt;
            }
            throw new System.ArgumentException($"Chave '{key}' não existe em LocalizationCatalog.", nameof(key));
        }

        static void Add(List<Entry> list, string key, string pt, string en) => list.Add(new Entry(key, pt, en));

        static List<Entry> Build()
        {
            var l = new List<Entry>();

            // ---------------------------------------------------------------- menu principal e títulos
            Add(l, "menu.title", "IncrementAnt", "IncrementAnt");
            Add(l, "menu.tagline", "Uma colônia em constante transformação", "A colony in constant transformation");
            Add(l, "menu.tagline.dot", "Uma colônia em constante transformação.", "A colony in constant transformation.");
            Add(l, "menu.new", "INICIAR NOVA COLÔNIA", "START NEW COLONY");
            Add(l, "menu.continue", "CONTINUAR", "CONTINUE");
            Add(l, "menu.settings", "CONFIGURAÇÕES", "SETTINGS");
            Add(l, "menu.about", "SOBRE O JOGO", "ABOUT THE GAME");
            Add(l, "menu.quit", "SAIR", "QUIT");
            Add(l, "menu.name", "INCREMENTANT", "INCREMENTANT");
            Add(l, "menu.about.body",
                "Cave, cultive e cuide da colônia.\nCada formiga tem o seu papel.\n\nUM JOGO DA FÁBULA GAME STUDIO\n\nOs nomes da equipe e os créditos\ndos assets serão incluídos aqui.",
                "Dig, cultivate and look after the colony.\nEvery ant has its role.\n\nA GAME BY FÁBULA GAME STUDIO\n\nThe team's names and the asset\ncredits will be added here.");
            Add(l, "menu.nosave", "Nenhuma colônia salva para continuar.", "No saved colony to continue.");
            Add(l, "ui.back", "[ESC] VOLTAR", "[ESC] BACK");
            Add(l, "version.title", "VERSÕES", "VERSIONS");
            Add(l, "version.current", "ATUAL", "CURRENT");
            Add(l, "version.added", "NOVIDADES", "NEW");
            Add(l, "version.modified", "AJUSTES E CORREÇÕES", "CHANGES AND FIXES");
            Add(l, "version.removed", "REMOVIDO", "REMOVED");
            Add(l, "version.bugs", "BUGS CONHECIDOS", "KNOWN BUGS");
            Add(l, "version.nonotes", "Esta versão não tem notas.", "This version has no notes.");
            Add(l, "version.empty", "Nenhuma versão publicada ainda.", "No version released yet.");

            // ---------------------------------------------------------------- HUD: cartões e menus
            Add(l, "ui.nutrients", "NUTRIENTES", "NUTRIENTS");
            Add(l, "ui.ants", "FORMIGAS", "ANTS");
            Add(l, "ui.queen", "RAINHA", "QUEEN");
            Add(l, "ui.colony", "COLÔNIA", "COLONY");
            Add(l, "ui.paint", "FERRAMENTA", "TOOL");
            Add(l, "ui.evolution", "EVOLUÇÃO", "EVOLUTION");
            Add(l, "ui.environments", "AMBIENTES", "ENVIRONMENTS");
            Add(l, "ui.alerts", "AVISOS", "ALERTS");
            Add(l, "brush.dig", "Cavar", "Dig");
            Add(l, "brush.build", "Construir", "Build");
            Add(l, "brush.queenroom", "Sala da rainha", "Queen room");
            Add(l, "brush.nutrientroom", "Sala de nutrientes", "Nutrient room");
            Add(l, "brush.nursery", "Berçário", "Nursery");
            Add(l, "brush.waste", "Resíduos", "Waste");
            Add(l, "brush.finger", "Dedo", "Finger");
            Add(l, "brush.dew", "Gotas de orvalho", "Dew drops");
            Add(l, "brush.foodstore", "Depósito de alimentos", "Food storage");
            Add(l, "brush.blockstore", "Depósito de construção", "Building storage");
            Add(l, "brush.glowmushroom", "Cogumelo luminoso", "Glowing mushroom");
            Add(l, "brush.waterstore", "Depósito de água", "Water storage");
            Add(l, "brush.farm", "Fazenda de pulgões", "Aphid farm");
            Add(l, "brush.sandstore", "Depósito de areia", "Sand storage");
            Add(l, "brush.mushroomstore", "Depósito de cogumelos", "Mushroom storage");
            Add(l, "brush.litterstore", "Depósito de serapilheira", "Leaf litter storage");
            Add(l, "brush.nectarstore", "Depósito de néctar", "Nectar storage");
            Add(l, "brush.orestore", "Depósito de minério", "Ore storage");
            Add(l, "brush.smelter", "Fundição", "Smelter");
            Add(l, "brush.forge", "Forja", "Forge");
            Add(l, "brush.armory", "Armazém de ferramentas e armas", "Tools and weapons storage");
            Add(l, "brush.wardrobe", "Armazém de roupas e armaduras", "Clothing and armor storage");
            Add(l, "ui.depots", "DEPÓSITO", "STORAGE");
            Add(l, "queen.mode.auto", "AUTOMÁTICO", "AUTOMATIC");
            Add(l, "queen.mode.specific", "TIPO ESPECÍFICO", "SPECIFIC TYPE");
            Add(l, "queen.mode.paused", "PAUSADO", "PAUSED");
            Add(l, "queen.stance.offensive", "OFENSIVA: ATACA OS CUPINS", "OFFENSIVE: ATTACKS THE TERMITES");
            Add(l, "queen.stance.defensive", "DEFENSIVA: SÓ SE DEFENDE", "DEFENSIVE: ONLY DEFENDS");
            Add(l, "role.worker", "Operária", "Worker");
            Add(l, "role.carrier", "Carregadora", "Carrier");
            Add(l, "role.cutter", "Cortadeira", "Cutter");
            Add(l, "role.digger", "Escavadora", "Digger");
            Add(l, "role.gardener", "Jardineira", "Gardener");
            Add(l, "role.nurse", "Cuidadora", "Nurse");
            Add(l, "role.soldier", "Soldado", "Soldier");
            Add(l, "role.spitter", "Cuspideira", "Spitter");
            Add(l, "role.midden", "Lixeira", "Midden worker");
            Add(l, "role.farmer", "Fazendeira", "Farmer");

            // ---------------------------------------------------------------- HUD: textos que mudam durante o jogo
            Add(l, "hud.nutrients.value", "{0} N", "{0} N");
            Add(l, "hud.nutrients.consumed", "CONSUMIDOS: {0}", "CONSUMED: {0}");
            Add(l, "hud.ants.adults", "ADULTAS {0} / {1}", "ADULTS {0} / {1}");
            Add(l, "hud.brood", "OVOS {0} · LARVAS {1} · PUPAS {2}", "EGGS {0} · LARVAE {1} · PUPAE {2}");
            Add(l, "hud.queen.room", "SALA: {0} PX", "ROOM: {0} PX");
            Add(l, "hud.queen.room.none", "SALA: —", "ROOM: —");
            Add(l, "hud.queen.next", "PRÓXIMO OVO: {0} · {1} N", "NEXT EGG: {0} · {1} N");
            Add(l, "hud.queen.next.none", "PRÓXIMO OVO: —", "NEXT EGG: —");
            Add(l, "hud.queen.space", "ADULTAS {0} / {1} · BERÇÁRIO {2} / {3}", "ADULTS {0} / {1} · NURSERY {2} / {3}");
            Add(l, "hud.type.locked", "{0} · BLOQ. ({1})", "{0} · LOCKED ({1})");
            Add(l, "hud.type.count", "{0} {1} · {2} N", "{0} {1} · {2} N");
            Add(l, "hud.hint", "WASD / BOTÃO DO MEIO: MOVER CÂMERA · ESC: MENU", "WASD / MIDDLE BUTTON: MOVE CAMERA · ESC: MENU");
            Add(l, "hud.stats", "FPS {0}\nTEMPO {1}", "FPS {0}\nTIME {1}");
            Add(l, "queen.status.noroom", "SEM SALA", "NO ROOM");
            Add(l, "queen.status.inroom", "NA SALA", "IN ROOM");
            Add(l, "queen.status.going", "INDO À SALA", "GOING TO ROOM");
            Add(l, "queen.status.nopath", "SEM CAMINHO", "NO PATH");
            Add(l, "breed.paused", "PAUSADA: SEM OVOS NOVOS", "PAUSED: NO NEW EGGS");
            Add(l, "breed.noqueenroom", "SEM SALA DA RAINHA: PINTE E ESCAVE", "NO QUEEN ROOM: PAINT AND DIG");
            Add(l, "breed.queenout", "RAINHA FORA DA SALA", "QUEEN OUTSIDE HER ROOM");
            Add(l, "breed.nonursery", "SEM BERÇÁRIO: PINTE E ESCAVE", "NO NURSERY: PAINT AND DIG");
            Add(l, "breed.nurseryfull", "BERÇÁRIO CHEIO", "NURSERY FULL");
            Add(l, "breed.expand", "SEM VAGA ADULTA: AMPLIE", "NO ADULT SLOT: EXPAND");
            Add(l, "breed.resources", "AGUARDANDO RECURSOS (RES. {0} N)", "WAITING FOR RESOURCES (RES. {0} N)");
            Add(l, "breed.next", "PRÓXIMO OVO EM {0} S", "NEXT EGG IN {0} S");
            Add(l, "raid.warning", "ATAQUE DE CUPINS IMINENTE! CHEGAM EM {0} S", "TERMITE ATTACK INCOMING! ARRIVING IN {0} S");
            Add(l, "raid.storm", "ATAQUE DE CUPINS IMINENTE! ESPERANDO A CHUVA PASSAR", "TERMITE ATTACK INCOMING! WAITING OUT THE RAIN");
            Add(l, "raid.active", "CUPINS ATACANDO: {0} · {1}", "TERMITES ATTACKING: {0} · {1}");
            Add(l, "raid.over.first", "ATAQUE REPELIDO! SOLDADO E ÁREA DE RESÍDUOS LIBERADOS", "ATTACK REPELLED! SOLDIER AND WASTE AREA UNLOCKED");
            Add(l, "raid.over", "ATAQUE DE CUPINS TERMINOU", "TERMITE ATTACK OVER");
            Add(l, "raid.obj.brood", "ATRÁS DAS CRIAS", "AFTER THE BROOD");
            Add(l, "raid.obj.aphids", "ATRÁS DOS PULGÕES", "AFTER THE APHIDS");
            Add(l, "raid.obj.farms", "DESTRUINDO CANTEIROS", "DESTROYING BEDS");
            Add(l, "raid.obj.sectors", "DESTRUINDO OS SETORES", "DESTROYING THE SECTORS");
            Add(l, "raid.obj.walls", "DERRUBANDO PAREDES", "BREAKING WALLS");
            Add(l, "raid.obj.queen", "ATRÁS DA RAINHA", "AFTER THE QUEEN");
            Add(l, "raid.obj.leave", "INDO EMBORA", "LEAVING");

            // ---------------------------------------------------------------- HUD: dicas (tooltips) dos botões e das seções
            Add(l, "tip.menu.paint", "Ferramentas do mapa: cavar terra, marcar obras e o dedo, que fere cupins.", "Map tools: dig soil, mark building work, and the finger, which hurts termites.");
            Add(l, "tip.menu.environments", "Salas do formigueiro. Pinte onde ficam a rainha, o cultivo, as crias, o lixo, o orvalho, os pulgões e os depósitos.", "Anthill rooms. Paint where the queen, the farm, the brood, the waste, the dew, the aphids and the storage go.");
            Add(l, "tip.menu.depots", "Depósitos de alimentos, de construção, de água, de areia e de cogumelos. O que as formigas trazem de fora fica guardado ali, sem fechar a passagem, e é usado de lá primeiro.", "Food, building, water, sand and mushroom storage. What the ants bring from outside is kept there, without blocking the way, and is used from there first.");
            Add(l, "tip.menu.queen", "Rainha: escolha o tipo de formiga que ela põe, como reage aos cupins e acompanhe a criação.", "Queen: choose the type of ant she lays, how she reacts to termites, and follow the breeding.");
            Add(l, "tip.menu.evolution", "Evolução: gaste nutrientes em novas classes de formiga e melhorias.", "Evolution: spend nutrients on new ant classes and upgrades.");
            Add(l, "tip.recall.on", "Volte para o formigueiro: as formigas terminam o que carregam e param de trabalhar.", "Back to the nest: ants finish what they carry and stop working.");
            Add(l, "tip.recall.off", "Volte ao trabalho: as formigas retomam as tarefas.", "Back to work: ants resume their tasks.");
            Add(l, "tip.menu.colony", "Painel da colônia: estatísticas das formigas e da produção, e quais trabalhos ficam ativos.", "Colony panel: ant and production statistics, and which jobs stay active.");
            Add(l, "tip.rooms.hide", "Esconder ambientes: a pintura das salas e dos depósitos some do terreno. Com um pincel de ambiente na mão, ela volta a aparecer.", "Hide rooms: the room and storage paint disappears from the ground. With a room brush in hand, it shows again.");
            Add(l, "tip.rooms.show", "Mostrar ambientes: a pintura das salas e dos depósitos volta a aparecer no terreno.", "Show rooms: the room and storage paint shows on the ground again.");

            // ---------------------------------------------------------------- painel da colônia
            Add(l, "colony.title", "PAINEL DA COLÔNIA", "COLONY PANEL");
            Add(l, "colony.tab.stats", "ESTATÍSTICAS", "STATISTICS");
            Add(l, "colony.col.section", "SEÇÃO DE TRABALHO", "WORK SECTION");
            Add(l, "colony.col.now", "AGORA", "NOW");
            Add(l, "colony.col.limit", "LIMITE DE FORMIGAS", "ANT LIMIT");
            Add(l, "colony.queen.title", "COMPORTAMENTO DA RAINHA", "QUEEN'S BEHAVIOR");
            Add(l, "colony.queen.nest", "BOTAR OVOS", "LAY EGGS");
            Add(l, "colony.reset", "TUDO ATIVO", "ALL ACTIVE");
            Add(l, "colony.stats.ants", "FORMIGAS", "ANTS");
            Add(l, "colony.stats.adults", "Adultas", "Adults");
            Add(l, "colony.stats.brood", "Crias", "Brood");
            Add(l, "colony.stats.idle", "Sem trabalho", "Without a job");
            Add(l, "colony.stats.fighting", "Lutando", "Fighting");
            Add(l, "colony.stats.waterStock", "Enchendo dep. água", "Filling water depot");
            Add(l, "colony.stats.roles", "POR TIPO", "BY TYPE");
            Add(l, "colony.stats.work", "TRABALHANDO EM", "WORKING ON");
            Add(l, "colony.stats.queen", "RAINHA", "QUEEN");
            Add(l, "colony.stats.recalled", "Sino tocado: recolhidas", "Bell rung: called back");
            Add(l, "colony.stats.places", "ONDE ESTÃO", "WHERE THEY ARE");
            Add(l, "colony.stats.rates", "PRODUÇÃO POR SEGUNDO", "PRODUCTION PER SECOND");
            Add(l, "colony.section.dig", "Escavação", "Digging");
            Add(l, "colony.section.build", "Construção", "Building");
            Add(l, "colony.section.harvest", "Coleta de plantas", "Plant gathering");
            Add(l, "colony.section.queenfeed", "Alimentar a rainha", "Feeding the queen");
            Add(l, "colony.section.nursery", "Berçário", "Nursery");
            Add(l, "colony.section.garden", "Sala de nutrientes", "Nutrient room");
            Add(l, "colony.section.dew", "Gotas de orvalho", "Dew drops");
            Add(l, "colony.section.waste", "Resíduos", "Waste");
            Add(l, "colony.place.outdoor", "Exterior", "Outdoors");
            Add(l, "colony.place.tunnels", "Túneis", "Tunnels");
            Add(l, "colony.place.queenroom", "Sala da rainha", "Queen's room");
            Add(l, "colony.place.nursery", "Berçário", "Nursery");
            Add(l, "colony.place.nutrientroom", "Sala de nutrientes", "Nutrient room");
            Add(l, "colony.place.dewroom", "Gotas de orvalho", "Dew drops");
            Add(l, "colony.place.farmroom", "Fazenda", "Farm");
            Add(l, "colony.place.wasteroom", "Resíduos", "Waste");
            Add(l, "colony.rate.toqueen", "Nutriente à rainha", "Nutrient to queen");
            Add(l, "colony.rate.grown", "Nutriente brotado", "Nutrient sprouted");
            Add(l, "colony.rate.harvest", "Planta colhida", "Plant harvested");
            Add(l, "colony.rate.soil", "Terra escavada", "Soil dug out");
            Add(l, "colony.stopped", "PARADA", "STOPPED");
            Add(l, "colony.limit.none", "SEM LIMITE", "NO LIMIT");
            Add(l, "colony.limit.max", "MÁX {0}", "MAX {0}");
            Add(l, "colony.queen.nesting", "No ninho, pondo ovos", "In the nest, laying eggs");
            Add(l, "colony.queen.working", "Trabalhando como {0}", "Working as a {0}");
            Add(l, "colony.queen.state.nest", "A rainha fica na sala dela pondo ovos. Agora: {0}.", "The queen stays in her room laying eggs. Now: {0}.");
            Add(l, "colony.queen.state.role", "A rainha trabalha como {0} e não põe ovos fora da sala. Agora: {1}.", "The queen works as a {0} and lays no eggs outside her room. Now: {1}.");
            Add(l, "tip.colony.toggle", "Clique para ligar ou parar. Parada, nenhuma formiga nova entra; quem está nela termina o que carrega.", "Click to turn on or stop. Stopped, no new ant joins; those in it finish what they carry.");
            Add(l, "tip.colony.minus", "Diminui o limite de formigas nesta seção.", "Lowers the ant limit for this section.");
            Add(l, "tip.colony.plus", "Aumenta o limite; acima do total de formigas, fica sem limite.", "Raises the limit; above the total number of ants, there is no limit.");
            Add(l, "tip.colony.slider", "Arraste para escolher o limite; no fim do trilho, fica sem limite.", "Drag to pick the limit; at the end of the track, there is no limit.");
            Add(l, "tip.colony.reset", "Liga todas as seções e tira todos os limites.", "Turns every section on and removes every limit.");
            Add(l, "tip.colony.queen.nest", "A rainha fica na sala dela e põe ovos.", "The queen stays in her room and lays eggs.");
            Add(l, "tip.colony.queen.choose", "Escolha o que a rainha faz: botar ovos ou trabalhar como uma das classes já liberadas.", "Choose what the queen does: lay eggs or work as one of the classes already unlocked.");
            Add(l, "tip.colony.queen.role", "A rainha trabalha como {0}, com o mesmo comportamento dessa classe (sem trabalho dela, faz o da operária). Respeita as seções paradas, mas não os limites. Fora da sala, não põe ovos.", "The queen works as a {0}, with the same behavior as that class (with nothing of its own to do, she does the worker's job). She respects stopped sections, but not limits. Outside her room, she lays no eggs.");
            Add(l, "tip.colony.section.dig", "Escavação: cavar a terra marcada e levá-la para fora.", "Digging: dig the marked soil and carry it outside.");
            Add(l, "tip.colony.section.build", "Construção: buscar terra na pilha de fora e montar as obras.", "Building: fetch soil from the outside pile and raise the structures.");
            Add(l, "tip.colony.section.harvest", "Coleta de plantas: cortar plantas de fora e levar os pedaços aos canteiros.", "Plant gathering: cut outside plants and carry the pieces to the beds.");
            Add(l, "tip.colony.section.queenfeed", "Alimentar a rainha: levar o nutriente guardado até ela.", "Feeding the queen: carry stored nutrient to her.");
            Add(l, "tip.colony.section.nursery", "Berçário: levar os ovos e cuidar das crias.", "Nursery: carry the eggs and tend the brood.");
            Add(l, "tip.colony.section.garden", "Sala de nutrientes: montar canteiros, regar e cuidar do que brota.", "Nutrient room: build beds, water them and tend what sprouts.");
            Add(l, "tip.colony.section.dew", "Gotas de orvalho: terra fofa no teto e cuidado das gotas.", "Dew drops: loose soil on the ceiling and care of the drops.");
            Add(l, "tip.colony.section.waste", "Resíduos: desmembrar cupins mortos e levar os restos.", "Waste: dismember dead termites and carry the remains away.");
            Add(l, "tip.pause", "Pausa ou retoma a simulação (tecla P).", "Pauses or resumes the simulation (P key).");
            Add(l, "tip.brush.dig", "Cavar: marque a terra que as escavadoras devem tirar. Botão esquerdo marca, direito desmarca.", "Dig: mark the soil the diggers must remove. Left button marks, right button unmarks.");
            Add(l, "tip.brush.build", "Construir: marque células vazias para as formigas levantarem terra, até acima do solo. Botão direito desmarca.", "Build: mark empty cells for the ants to fill with soil, even above the ground. Right button unmarks.");
            Add(l, "tip.brush.glowmushroom", "Cogumelo luminoso: clique numa célula do túnel encostada na terra. Uma formiga leva até lá um cogumelo colhido dos pulgões e o planta; ele ilumina os túneis em volta. Botão direito desmarca; para tirar um cogumelo, cave-o.", "Glowing mushroom: click a tunnel cell next to the soil. An ant brings a mushroom harvested from the aphids and plants it; it lights the tunnels around it. Right button unmarks; to remove a mushroom, dig it.");
            Add(l, "tip.st.mushroom.stock", "Cogumelos guardados: {0}. Eles vêm da ordenha dos pulgões na fazenda e ficam no depósito de cogumelos.", "Mushrooms stored: {0}. They come from milking the aphids on the farm and are kept in mushroom storage.");
            Add(l, "tip.brush.finger", "Dedo: clique num cupim para feri-lo. Clicar na rainha força um ovo.", "Finger: click a termite to hurt it. Clicking the queen forces an egg.");
            Add(l, "tip.room.paint", "Esquerdo pinta, direito apaga.", "Left paints, right erases.");
            Add(l, "tip.room.area", "Área: {0}/{1} células · salas: {2}/{3}.", "Area: {0}/{1} cells · rooms: {2}/{3}.");
            Add(l, "tip.room.area.full", "Sem área livre: apague uma parte ou compre mais área na Evolução.",
                "No free area: erase some or buy more area in Evolution.");
            Add(l, "tip.brush.queenroom", "Sala da rainha: onde ela fica e põe os ovos.", "Queen room: where she stays and lays eggs.");
            Add(l, "tip.brush.nutrientroom", "Sala de nutrientes: o cultivo. As formigas montam canteiros (terra fofa, planta e água) e deles brotam os nutrientes.", "Nutrient room: the farm. Ants build beds (loose soil, plant and water) and nutrients sprout from them.");
            Add(l, "tip.brush.nursery", "Berçário: onde ovos, larvas e pupas se desenvolvem. As vagas de cria vêm das células livres dele.", "Nursery: where eggs, larvae and pupae develop. Brood slots come from its free cells.");
            Add(l, "tip.brush.waste", "Resíduos: onde as formigas deixam entulho e restos para se decomporem.", "Waste: where ants leave rubble and scraps to decompose.");
            Add(l, "tip.brush.waste.locked", "Resíduos: bloqueado até você repelir o primeiro ataque de cupins.", "Waste: locked until you repel the first termite attack.");
            Add(l, "tip.brush.foodstore", "Depósito de alimentos: guarda os pedaços de planta cortados. Com ele, os canteiros e a sala de orvalho só recebem planta daqui.", "Food storage: keeps the cut plant pieces. With it, the beds and the dew room only get plants from here.");
            Add(l, "tip.brush.blockstore", "Depósito de construção: guarda a terra cavada. Com ele, obras, canteiros e a sala de orvalho só usam a terra daqui.", "Building storage: keeps the dug soil. With it, builds, beds and the dew room only use the soil from here.");
            Add(l, "tip.brush.waterstore", "Depósito de água: guarda a água tirada do mapa. Com ele, os canteiros só são regados com a água daqui.", "Water storage: keeps the water taken from the map. With it, the beds are only watered with the water from here.");
            Add(l, "tip.brush.sandstore", "Depósito de areia: guarda a areia cavada e a que sai da fazenda e dos resíduos. É a comida dos pulgões.", "Sand storage: keeps the dug sand and the sand from the farm and the waste area. It is the aphids' food.");
            Add(l, "tip.brush.mushroomstore", "Depósito de cogumelos: guarda os cogumelos luminosos que os pulgões dão na ordenha. O pincel Cogumelo luminoso busca os cogumelos daqui.", "Mushroom storage: keeps the glowing mushrooms the aphids give when milked. The Glowing mushroom brush takes them from here.");
            Add(l, "tip.brush.litterstore", "Depósito de serapilheira: guarda as folhas secas e os gravetos miúdos cavados no bioma Serapilheira, à direita do formigueiro.", "Leaf litter storage: keeps the dry leaves and small twigs dug in the Leaf Litter biome, to the right of the nest.");
            Add(l, "tip.brush.nectarstore", "Depósito de néctar: guarda as gotas que as flores do Canteiro de flores, à esquerda do formigueiro, soltam no chão. Sem ele, as gotas ficam onde caíram.", "Nectar storage: keeps the drops that the flowers of the Flower Bed, to the left of the nest, let fall. Without it, the drops stay where they fell.");
            Add(l, "tip.brush.orestore", "Depósito de minério: guarda o cobre, o ferro e o ouro cavados dos veios. Sem ele, o minério fica onde foi cavado.", "Ore storage: keeps the copper, iron and gold dug from the veins. Without it, the ore stays where it was dug.");
            Add(l, "tip.brush.smelter", "Fundição: as formigas trazem minério e serapilheira; cada minério com uma serapilheira vira uma barra do mesmo metal em 20 s.", "Smelter: ants bring ore and leaf litter; each ore with one leaf litter becomes a bar of the same metal in 20 s.");
            Add(l, "tip.brush.forge", "Forja: as formigas trazem as barras; duas barras do mesmo metal viram, em 30 s, a ferramenta, a arma ou a armadura que mais falta.", "Forge: ants bring the bars; two bars of the same metal become, in 30 s, the tool, weapon or armor most needed.");
            Add(l, "tip.brush.armory", "Armazém de ferramentas e armas: a escavadora e a operária buscam aqui a ferramenta; soldado e cuspideira, a arma.", "Tools and weapons storage: diggers and workers fetch their tool here; soldiers and spitters, their weapon.");
            Add(l, "tip.brush.wardrobe", "Armazém de roupas e armaduras: soldado e cuspideira buscam aqui a armadura.", "Clothing and armor storage: soldiers and spitters fetch their armor here.");
            Add(l, "tip.brush.farm", "Fazenda de pulgões: os pulgões nascem nas baias do chão, cada uma com um cocho de areia e um de água. Sozinhos, só pingam água; ordenhados pela fazendeira, comem e dão areia, folhas, água e às vezes cogumelos luminosos.", "Aphid farm: aphids are born in the stalls on the floor, each with a sand trough and a water trough. On their own they only drip water; milked by the farmer, they eat and give sand, leaves, water and sometimes glowing mushrooms.");
            Add(l, "tip.brush.farm.locked", "Fazenda de pulgões: bloqueada até você comprar a câmara Fazenda de pulgões em Evolução.", "Aphid farm: locked until you buy the Aphid farm chamber in Evolution.");
            Add(l, "tip.st.farm.paint", "Falta: pintar a fazenda (Ambientes > Fazenda de pulgões).", "Missing: paint the farm (Environments > Aphid farm).");
            Add(l, "tip.st.farm.nospot", "Falta: espaço para baias. Cave a fazenda com chão reto de pelo menos {0} células.", "Missing: room for stalls. Dig the farm with a flat floor at least {0} cells wide.");
            Add(l, "tip.st.farm.breeding", "O primeiro pulgão nasce em {0} s.", "The first aphid is born in {0} s.");
            Add(l, "tip.st.farm.ok", "Pulgões: {0}/{1} em {2} baias; {3} cochos vazios.", "Aphids: {0}/{1} in {2} stalls; {3} empty troughs.");
            Add(l, "tip.st.farm.nofarmer", "Sem fazendeira, os pulgões só pingam água.", "Without a farmer, the aphids only drip water.");
            Add(l, "tip.st.depot.paint", "Falta: pintar o depósito (Ambientes > Depósito).", "Missing: paint the storage (Environments > Storage).");
            Add(l, "tip.st.depot.nospot", "Falta: espaço livre. Cave o depósito; qualquer célula vazia guarda um item.", "Missing: free space. Dig the storage; any empty cell holds one item.");
            Add(l, "tip.st.depot.ok", "Funcionando: {0} guardados, {1} vagas livres.", "Working: {0} stored, {1} free slots.");
            Add(l, "tip.brush.dew", "Gotas de orvalho: as formigas põem terra fofa e folhas no teto da sala, e dela pingam gotas de água para regar os canteiros.", "Dew drops: ants place loose soil and leaves on the room's ceiling, and water drops fall from it to water the beds.");
            Add(l, "tip.card.nutrients", "Nutrientes: a moeda das formigas novas. A rainha consome o que as formigas guardam na sala de nutrientes.", "Nutrients: the currency for new ants. The queen eats what the ants store in the nutrient room.");
            Add(l, "tip.card.nutrients.balance", "Saldo {0} N; a rainha preserva {1} N antes de pôr um ovo.", "Balance {0} N; the queen keeps {1} N in reserve before laying an egg.");
            Add(l, "tip.card.ants", "Formigas: adultas / vagas de adulta (vêm da área escavada do formigueiro) e crias.", "Ants: adults / adult slots (from the dug area of the anthill) and brood.");
            Add(l, "tip.card.ants.capacity", "Agora: {0} de {1} vagas; {2} crias em {3} vagas de berçário.", "Now: {0} of {1} slots; {2} brood in {3} nursery slots.");
            Add(l, "tip.card.queen", "Rainha: precisa da própria sala, de berçário, de vaga e de nutrientes para pôr ovos.", "Queen: needs her own room, a nursery, a free slot and nutrients to lay eggs.");
            Add(l, "tip.role.worker", "Operária: faz de tudo um pouco e corta plantas quando está sem outra tarefa.", "Worker: does a bit of everything and cuts plants when it has no other task.");
            Add(l, "tip.role.carrier", "Carregadora: a mais rápida; leva o dobro por viagem, enche os depósitos e entrega o que está neles.", "Carrier: the fastest; hauls twice as much per trip, fills the depots and delivers from them.");
            Add(l, "tip.role.cutter", "Cortadeira: corta as plantas da superfície na metade do tempo e as leva à sala de nutrientes.", "Cutter: cuts surface plants in half the time and takes them to the nutrient room.");
            Add(l, "tip.role.digger", "Escavadora: especialista em cavar a terra marcada e despejá-la fora.", "Digger: specialist in digging the marked soil and dumping it outside.");
            Add(l, "tip.role.gardener", "Jardineira: leva terra e água aos canteiros e cuida do cultivo.", "Gardener: brings soil and water to the beds and tends the farm.");
            Add(l, "tip.role.nurse", "Cuidadora: cuida das crias no berçário e leva os ovos para lá.", "Nurse: looks after the brood in the nursery and carries eggs there.");
            Add(l, "tip.role.soldier", "Soldado: combate os cupins e recolhe os corpos.", "Soldier: fights the termites and clears the bodies.");
            Add(l, "tip.role.spitter", "Cuspideira: cospe ácido nos cupins de longe, sem chegar perto da mordida deles.", "Spitter: spits acid at termites from afar, out of reach of their bite.");
            Add(l, "tip.role.midden", "Lixeira: tira os corpos dos cupins, leva os itens soltos e abastece a área de resíduos.", "Midden worker: clears termite bodies, hauls loose items and supplies the waste area.");
            Add(l, "tip.role.farmer", "Fazendeira: enche os cochos dos pulgões com areia e água e os ordenha na fazenda.", "Farmer: fills the aphids' troughs with sand and water and milks them on the farm.");
            Add(l, "tip.role.locked", "Bloqueada: libere o nó {0} em Evolução.", "Locked: unlock node {0} in Evolution.");
            Add(l, "tip.role.choose", "Clique para a rainha pôr só este tipo.", "Click to make the queen lay only this type.");
            Add(l, "tip.mode.automatic", "Automático: a rainha escolhe o tipo do próximo ovo para equilibrar a colônia.", "Automatic: the queen picks the next egg's type to balance the colony.");
            Add(l, "tip.mode.specific", "Tipo específico: a rainha só põe o tipo escolhido abaixo.", "Specific type: the queen lays only the type chosen below.");
            Add(l, "tip.mode.paused", "Pausado: a rainha não põe ovos novos (as crias existentes seguem crescendo).", "Paused: the queen lays no new eggs (existing brood keeps growing).");
            Add(l, "tip.stance.defensive", "Defensiva: a rainha só se defende dos cupins que chegam perto.", "Defensive: the queen only defends herself from termites that come close.");
            Add(l, "tip.stance.offensive", "Ofensiva: a rainha vai atrás dos cupins para atacá-los.", "Offensive: the queen goes after the termites to attack them.");
            Add(l, "tip.st.queen.paint", "Falta: pintar a sala da rainha (menu Ambientes).", "Missing: paint the queen room (Environments menu).");
            Add(l, "tip.st.queen.dig", "Falta: cavar a sala pintada, que ainda é terra maciça.", "Missing: dig out the painted room, which is still solid soil.");
            Add(l, "tip.st.queen.path", "Falta: ligar a sala da rainha ao resto do formigueiro por um túnel.", "Missing: connect the queen room to the rest of the anthill with a tunnel.");
            Add(l, "tip.st.queen.ok", "Sala pronta: a rainha está nela ({0} células).", "Room ready: the queen is in it ({0} cells).");
            Add(l, "tip.st.queen.going", "Sala pronta ({0} células): a rainha está indo para lá.", "Room ready ({0} cells): the queen is heading there.");
            Add(l, "tip.st.nutrient.paint", "Falta: pintar a sala de nutrientes (menu Ambientes).", "Missing: paint the nutrient room (Environments menu).");
            Add(l, "tip.st.nutrient.nobeds", "Sem canteiros ainda: as formigas montam um quando trazem terra fofa ao cavar. Nutrientes prontos: {0}.", "No beds yet: ants build one when they bring loose soil while digging. Nutrients ready: {0}.");
            Add(l, "tip.st.nutrient.nospot", "Falta: espaço para canteiro. Cave a sala com chão firme e 2 células de altura. Nutrientes prontos: {0}.", "Missing: room for a bed. Dig the room with solid floor and 2 cells of height. Nutrients ready: {0}.");
            Add(l, "tip.st.nutrient.ok", "Produzindo: {0} canteiros ({1} esperam planta, {2} esperam água, {3} brotando). Nutrientes prontos: {4}.", "Producing: {0} beds ({1} awaiting a plant, {2} awaiting water, {3} sprouting). Nutrients ready: {4}.");
            Add(l, "tip.st.nutrient.paused", "Colheita pausada: nenhum canteiro pede planta.", "Harvest paused: no bed needs a plant.");
            Add(l, "tip.st.nursery.paint", "Falta: pintar o berçário (menu Ambientes).", "Missing: paint the nursery (Environments menu).");
            Add(l, "tip.st.nursery.dig", "Falta: cavar o berçário; ainda não há vaga de cria.", "Missing: dig the nursery; there is no brood slot yet.");
            Add(l, "tip.st.nursery.ok", "Funcionando: {0} crias em {1} vagas.", "Working: {0} brood in {1} slots.");
            Add(l, "tip.st.waste.paint", "Falta: pintar a área de resíduos (menu Ambientes).", "Missing: paint the waste area (Environments menu).");
            Add(l, "tip.st.waste.nospot", "Falta: espaço livre. Cave a área; qualquer célula vazia guarda um resíduo.", "Missing: free space. Dig the area; any empty cell holds one piece of waste.");
            Add(l, "tip.st.waste.ok", "Funcionando: {0} lugares livres, {1} resíduos se decompondo, {2} já depositados.", "Working: {0} free spots, {1} waste pieces decomposing, {2} already deposited.");
            Add(l, "tip.st.waste.mode", "O pincel pinta: {0}. Cada área guarda o próprio modo.", "The brush paints: {0}. Each area keeps its own mode.");
            Add(l, "waste.mode.remains", "Restos em folhas", "Scraps to leaves");
            Add(l, "waste.mode.leaves", "Folhas em água", "Leaves to water");
            Add(l, "waste.mode.dirt", "Terra em areia e água", "Soil to sand and water");
            Add(l, "waste.mode.remains.tip", "Pinta uma área para entulho e restos de cupim. O entulho some; os restos viram folhas, que as formigas levam ao depósito de alimentos ou aos canteiros.", "Paints an area for rubble and termite scraps. Rubble disappears; scraps become leaves, which the ants carry to food storage or to the beds.");
            Add(l, "waste.mode.leaves.tip", "Pinta uma área só para restos de cupim. As folhas deles ficam na área e viram água (1 folha, 1 água). A água vai ao depósito de água ou aos canteiros.", "Paints an area only for termite scraps. Their leaves stay in the area and turn into water (1 leaf, 1 water). The water goes to water storage or to the beds.");
            Add(l, "waste.mode.dirt.tip", "Pinta uma área só para terra: as formigas trazem a terra solta de fora e a cavada que iria ao despejo, e ela vira areia e água (1 terra, 1 areia e 1 água). A areia vai ao depósito de areia (ou ao despejo); a água, ao depósito de água ou aos canteiros.", "Paints an area only for soil: ants bring loose soil from outside and dug soil that would go to the dump, and it turns into sand and water (1 soil, 1 sand and 1 water). The sand goes to sand storage (or the dump); the water to water storage or to the beds.");
            Add(l, "tip.st.dew.paint", "Falta: pintar a sala (menu Ambientes).", "Missing: paint the room (Environments menu).");
            Add(l, "tip.st.dew.soil", "Etapa 1 de 3: levar terra fofa ({0} pontos). As formigas a tiram da pilha de fora ou do que cavam.", "Step 1 of 3: bringing loose soil ({0} spots). Ants take it from the pile outside or from what they dig.");
            Add(l, "tip.st.dew.leaf", "Etapa 2 de 3: levar folhas ({0} pontos). Cortadeiras e jardineiras as cortam; é preciso haver plantas na superfície.", "Step 2 of 3: bringing leaves ({0} spots). Cutters and gardeners cut them; there must be plants on the surface.");
            Add(l, "tip.st.dew.ok", "Produzindo: etapa 3 de 3, a sala pinga água. Gotas até agora: {0}.", "Producing: step 3 of 3, the room drips water. Drops so far: {0}.");
            Add(l, "tip.st.dew.nospot", "Falta: espaço para a terra fofa. Cave a sala com pelo menos 4 células de altura.", "Missing: room for the loose soil. Dig the room at least 4 cells high.");
            Add(l, "tip.st.breed.paused", "Rainha pausada: nenhum ovo novo.", "Queen paused: no new eggs.");
            Add(l, "tip.st.breed.noqueenroom", "Falta: pintar e cavar a sala da rainha.", "Missing: paint and dig the queen room.");
            Add(l, "tip.st.breed.queenout", "A rainha está fora da sala dela.", "The queen is outside her room.");
            Add(l, "tip.st.breed.nonursery", "Falta: pintar e cavar um berçário.", "Missing: paint and dig a nursery.");
            Add(l, "tip.st.breed.nurseryfull", "Berçário cheio: amplie-o.", "Nursery full: expand it.");
            Add(l, "tip.st.breed.expand", "Sem vaga de adulta: cave mais formigueiro.", "No adult slot: dig more anthill.");
            Add(l, "tip.st.breed.resources", "Aguardando nutrientes (reserva mínima de {0} N).", "Waiting for nutrients (minimum reserve of {0} N).");
            Add(l, "tip.st.breed.next", "Produzindo: próximo ovo em {0} s.", "Producing: next egg in {0} s.");

            // ---------------------------------------------------------------- menu do ESC e configurações
            Add(l, "pause.title", "MENU", "MENU");
            Add(l, "pause.resume", "CONTINUAR", "CONTINUE");
            Add(l, "pause.settings", "CONFIGURAÇÕES", "SETTINGS");
            Add(l, "pause.save", "SALVAR PARTIDA", "SAVE GAME");
            Add(l, "pause.load", "CARREGAR PARTIDA", "LOAD GAME");
            Add(l, "pause.mainmenu", "MENU PRINCIPAL", "MAIN MENU");
            Add(l, "pause.quit", "SAIR DO JOGO", "QUIT GAME");
            Add(l, "pause.quitnote", "SAIR SÓ FECHA O JOGO COMPILADO", "QUIT ONLY CLOSES THE BUILT GAME");
            Add(l, "settings.master", "VOLUME GERAL", "MASTER VOLUME");
            Add(l, "settings.music", "MÚSICA", "MUSIC");
            Add(l, "settings.sfx", "EFEITOS SONOROS", "SOUND EFFECTS");
            Add(l, "settings.vfx", "EFEITOS VISUAIS", "VISUAL EFFECTS");
            Add(l, "settings.bloom", "BRILHO (BLOOM)", "BLOOM");
            Add(l, "settings.fullscreen", "TELA CHEIA", "FULLSCREEN");
            Add(l, "settings.resolution", "RESOLUÇÃO", "RESOLUTION");
            Add(l, "settings.vsync", "VSYNC", "VSYNC");
            Add(l, "settings.fps", "LIMITE DE FPS", "FPS LIMIT");
            Add(l, "settings.stats", "MOSTRAR FPS E TEMPO", "SHOW FPS AND TIME");
            Add(l, "settings.language", "IDIOMA", "LANGUAGE");
            Add(l, "settings.blockbuild", "BLOQUEAR OBRA EM SALAS", "BLOCK BUILDING ON ROOMS");
            Add(l, "settings.title", "CONFIGURAÇÕES", "SETTINGS");
            Add(l, "settings.defaults", "PADRÃO", "DEFAULT");
            Add(l, "settings.native", "NATIVA", "NATIVE");
            Add(l, "settings.nolimit", "SEM LIMITE", "NO LIMIT");
            Add(l, "settings.vfx.full", "COMPLETO", "FULL");
            Add(l, "settings.vfx.reduced", "REDUZIDO", "REDUCED");
            Add(l, "settings.vfx.off", "DESLIGADO", "OFF");

            // ---------------------------------------------------------------- tela de evolução
            Add(l, "tree.title", "EVOLUÇÃO DA COLÔNIA", "COLONY EVOLUTION");
            Add(l, "tree.tab.hive", "FORMIGUEIRO", "ANTHILL");
            Add(l, "tree.tab.queen", "RAINHA E CLASSES", "QUEEN AND CLASSES");
            Add(l, "tree.unlock", "DESBLOQUEAR", "UNLOCK");
            Add(l, "tree.unlock.cost", "DESBLOQUEAR · {0} N", "UNLOCK · {0} N");
            Add(l, "tree.acquired", "ADQUIRIDO", "ACQUIRED");
            Add(l, "tree.available", "DISPONÍVEL", "AVAILABLE");
            Add(l, "tree.selected", "SELECIONADO", "SELECTED");
            Add(l, "tree.locked", "BLOQUEADO", "LOCKED");
            Add(l, "tree.legend.met", "TEM O REQUISITO", "REQUIREMENT MET");
            Add(l, "tree.legend.missing", "FALTA O REQUISITO", "REQUIREMENT MISSING");
            Add(l, "tree.legend.unlocks", "O QUE ELA LIBERA", "WHAT IT UNLOCKS");
            Add(l, "tree.soon", "EM BREVE", "COMING SOON");
            Add(l, "tree.requires", "REQUISITO: {0}", "REQUIREMENT: {0}");
            Add(l, "tree.requires.cost", "REQUISITO: {0}  ·  CUSTO: {1} N", "REQUIREMENT: {0}  ·  COST: {1} N");
            Add(l, "tree.none", "NENHUM", "NONE");
            Add(l, "tree.and", "E", "AND");
            Add(l, "tree.firstraid", "PRIMEIRO ATAQUE DE CUPINS", "FIRST TERMITE ATTACK");
            Add(l, "tree.branch.hive.cultivation", "CULTIVO", "CULTIVATION");
            Add(l, "tree.branch.hive.nursery", "BERÇÁRIO", "NURSERY");
            Add(l, "tree.branch.hive.storage", "DEPÓSITO", "STORAGE");
            Add(l, "tree.branch.hive.waste", "RESÍDUOS", "WASTE");
            Add(l, "tree.branch.hive.dew", "ORVALHO", "DEW");
            Add(l, "tree.branch.hive.works", "OBRA", "WORKS");
            Add(l, "tree.branch.hive.outside", "EXTERIOR", "OUTSIDE");
            Add(l, "tree.branch.hive.defense", "DEFESA", "DEFENSE");
            Add(l, "tree.root.hive", "CÂMARA REAL", "ROYAL CHAMBER");
            Add(l, "tree.controls", "ARRASTE PARA MOVER  ·  RODA DO MOUSE PARA ZOOM", "DRAG TO MOVE  ·  MOUSE WHEEL TO ZOOM");
            Add(l, "tree.max", "MÁX", "MAX");
            Add(l, "tree.depth", "PROFUNDIDADE {0}", "DEPTH {0}");
            Add(l, "tree.ring.closed", "TERRA NÃO ESCAVADA · ABRE COM PROFUNDIDADE {0}", "UNDUG SOIL · OPENS AT DEPTH {0}");
            Add(l, "tree.kind.chamber", "CÂMARA", "CHAMBER");
            Add(l, "tree.kind.gallery", "GALERIA", "GALLERY");
            Add(l, "tree.level", "NV {0}/{1}", "LV {0}/{1}");
            Add(l, "tree.level.open", "NV {0}", "LV {0}");
            Add(l, "tree.value", "TOTAL {0} → {1}", "TOTAL {0} → {1}");
            Add(l, "tree.value.full", "TOTAL {0} (COMPLETA)", "TOTAL {0} (COMPLETE)");
            Add(l, "tree.milestone", "MARCO NO NV {0}", "MILESTONE AT LV {0}");
            Add(l, "tree.cost", "CUSTO: {0} N", "COST: {0} N");
            Add(l, "tree.cost.levels", "+{0} NV: {1} N", "+{0} LV: {1} N");
            Add(l, "tree.fiveraids", "5 ATAQUES VENCIDOS", "5 ATTACKS SURVIVED");
            Add(l, "tree.fiftyadults", "50 ADULTAS", "50 ADULTS");
            Add(l, "buy.owned", "JÁ COMPRADO", "ALREADY BOUGHT");
            Add(l, "buy.missing", "FALTA UM PRÉ-REQUISITO", "A PREREQUISITE IS MISSING");
            Add(l, "buy.soon", "EM BREVE: O JOGO AINDA NÃO TEM ESSE SISTEMA", "COMING SOON: THE GAME DOESN'T HAVE THIS SYSTEM YET");
            Add(l, "buy.insufficient", "NUTRIENTES INSUFICIENTES", "NOT ENOUGH NUTRIENTS");
            Add(l, "buy.raid", "LIBERA DEPOIS DO PRIMEIRO ATAQUE DE CUPINS", "UNLOCKS AFTER THE FIRST TERMITE ATTACK");
            Add(l, "buy.done", "COMPRADO", "BOUGHT");
            string[] branchPt = { "RAINHA", "OPERÁRIA", "CARREGADORA", "CORTADEIRA", "ESCAVADORA", "JARDINEIRA", "CUIDADORA", "SOLDADO" };
            string[] branchEn = { "QUEEN", "WORKER", "CARRIER", "CUTTER", "DIGGER", "GARDENER", "NURSE", "SOLDIER" };
            for (int i = 0; i < branchPt.Length; i++)
                Add(l, "tree.branch." + ((QueenBranch)i).ToString().ToLowerInvariant(), branchPt[i], branchEn[i]);

            // ---------------------------------------------------------------- partida: salvar, carregar, fim de jogo
            Add(l, "save.loaded", "Partida carregada ({0}).", "Game loaded ({0}).");
            Add(l, "save.saved", "Partida salva ({0}).", "Game saved ({0}).");
            Add(l, "save.failsave", "Falha ao salvar: {0}", "Failed to save: {0}");
            Add(l, "save.failload", "Falha ao carregar: {0}", "Failed to load: {0}");
            Add(l, "save.none", "Nenhum save encontrado.", "No save found.");
            Add(l, "save.othermode", "O save é de outro modo de mapa (Procedural/Image).", "The save is from another map mode (Procedural/Image).");
            Add(l, "save.otherworld", "O save é de um mundo {0}x{1}; o atual é {2}x{3}.", "The save is from a {0}x{1} world; the current one is {2}x{3}.");
            Add(l, "gameover.title", "GAME OVER", "GAME OVER");
            Add(l, "gameover.body", "Os cupins mataram a rainha.\nTempo de partida: {0}", "The termites killed the queen.\nMatch time: {0}");
            Add(l, "gameover.load", "Carregar o último save  [F9]", "Load the last save  [F9]");
            Add(l, "gameover.new", "Novo mapa  [R]", "New map  [R]");

            // ---------------------------------------------------------------- tutorial (tour da interface)
            Add(l, "tutorial.title.welcome", "Bem-vindo ao formigueiro", "Welcome to the anthill");
            Add(l, "tutorial.title.farewell", "Agora é com você!", "Now it's up to you!");
            Add(l, "tutorial.start", "Começar", "Start");
            Add(l, "tutorial.next", "Próximo", "Next");
            Add(l, "tutorial.back", "Voltar", "Back");
            Add(l, "tutorial.explore", "Explorar", "Explore");
            Add(l, "tutorial.skip", "Pular tutorial", "Skip tutorial");
            Add(l, "tutorial.welcome", "Aqui você não controla cada formiga: você marca onde cavar e construir, decide para que serve cada sala e as formigas fazem o resto. Vamos conhecer a tela. O jogo fica pausado durante o tour e você pode clicar em tudo enquanto isso.",
                "Here you don't control each ant: you mark where to dig and build, decide what each room is for, and the ants do the rest. Let's get to know the screen. The game stays paused during the tour, and you can click anything along the way.");
            Add(l, "tutorial.tour.camera.title", "Câmera", "Camera");
            Add(l, "tutorial.tour.camera", "Mova a vista com WASD, as setas ou arrastando com o botão do meio do mouse. Role o mouse (ou Q/E) para aproximar e afastar. O formigueiro é a terra embaixo; a rainha está no abrigo.",
                "Move the view with WASD, the arrow keys or by dragging with the middle mouse button. Scroll (or Q/E) to zoom in and out. The anthill is the soil below; the queen is in the shelter.");
            Add(l, "tutorial.tour.nutrients.title", "Nutrientes", "Nutrients");
            Add(l, "tutorial.tour.nutrients", "A moeda da colônia. As formigas cultivam canteiros na sala de nutrientes; a rainha come nutrientes para pôr ovos e você os gasta na Evolução.",
                "The colony's currency. Ants grow beds in the nutrient room; the queen eats nutrients to lay eggs, and you spend them on Evolution.");
            Add(l, "tutorial.tour.ants.title", "Formigas", "Ants");
            Add(l, "tutorial.tour.ants", "Adultas / vagas de adulta e as crias (ovos, larvas e pupas). As vagas vêm da área escavada: cavar mais abre espaço para mais formigas.",
                "Adults / adult slots, and the brood (eggs, larvae and pupae). Slots come from the dug area: digging more makes room for more ants.");
            Add(l, "tutorial.tour.queencard.title", "Rainha", "Queen");
            Add(l, "tutorial.tour.queencard", "O estado da rainha. Para pôr ovos ela precisa da própria sala, de um berçário, de vaga e de nutrientes; o cartão diz o que está faltando.",
                "The queen's state. To lay eggs she needs her own room, a nursery, a free slot and nutrients; the card says what is missing.");
            Add(l, "tutorial.tour.tools.title", "Ferramenta", "Tools");
            Add(l, "tutorial.tour.tools", "Pincéis do mapa. Cavar marca a terra que as formigas tiram; Construir marca onde elas põem terra; Cogumelo luminoso ilumina os túneis; o Dedo fere cupins. Botão esquerdo marca, direito desmarca.",
                "Map brushes. Dig marks the soil the ants remove; Build marks where they place soil; Glowing mushroom lights the tunnels; the Finger hurts termites. Left button marks, right button unmarks.");
            Add(l, "tutorial.tour.rooms.title", "Ambientes", "Rooms");
            Add(l, "tutorial.tour.rooms", "Pinte o espaço escavado para dar uma função a ele: sala da rainha, de nutrientes, berçário, resíduos, orvalho, pulgões e depósitos. A barra embaixo de cada botão mostra a área que ainda dá para pintar. Botões apagados ainda não foram liberados.",
                "Paint the dug space to give it a purpose: queen's room, nutrient room, nursery, waste, dew, aphids and storage. The bar under each button shows how much area you can still paint. Greyed-out buttons are not unlocked yet.");
            Add(l, "tutorial.tour.queen.title", "Menu da rainha", "Queen menu");
            Add(l, "tutorial.tour.queen", "Escolha o tipo da próxima formiga (ou deixe no automático), pause a postura de ovos e decida se a rainha se defende ou ataca os cupins.",
                "Choose the type of the next ant (or leave it on automatic), pause egg laying, and decide whether the queen defends or attacks the termites.");
            Add(l, "tutorial.tour.evolution.title", "Evolução", "Evolution");
            Add(l, "tutorial.tour.evolution", "Gaste nutrientes em novas classes de formiga e em melhorias. Arraste para mover a árvore, role para aproximar e clique num nó para ver efeito, custo e requisitos.",
                "Spend nutrients on new ant classes and upgrades. Drag to move the tree, scroll to zoom, and click a node to see its effect, cost and requirements.");
            Add(l, "tutorial.tour.pause.title", "Pausa", "Pause");
            Add(l, "tutorial.tour.pause", "Pausa e retoma o jogo (tecla P). Pausado, você ainda pode marcar e pintar: as formigas começam quando o jogo voltar.",
                "Pauses and resumes the game (P key). While paused you can still mark and paint: the ants start once the game resumes.");
            Add(l, "tutorial.tour.colony.title", "Painel da colônia", "Colony panel");
            Add(l, "tutorial.tour.colony", "Estatísticas das formigas e da produção, e quais trabalhos ficam ativos. Ali você para uma seção ou limita quantas formigas trabalham nela.",
                "Ant and production statistics, and which jobs stay active. There you can stop a section or limit how many ants work in it.");
            Add(l, "tutorial.tour.bell.title", "Sino", "Bell");
            Add(l, "tutorial.tour.bell", "Chama as formigas de volta ao formigueiro: elas terminam o que carregam e param. Toque de novo para mandá-las de volta ao trabalho.",
                "Calls the ants back to the nest: they finish what they carry and stop. Ring again to send them back to work.");
            Add(l, "tutorial.tour.showrooms.title", "Mostrar ambientes", "Show rooms");
            Add(l, "tutorial.tour.showrooms", "Esconde ou mostra a pintura das salas no terreno. As salas continuam funcionando; é só para ver melhor a terra e as formigas.",
                "Hides or shows the room paint on the ground. The rooms keep working; it is just to see the soil and the ants better.");
            Add(l, "tutorial.tour.threats.title", "Cupins", "Termites");
            Add(l, "tutorial.tour.threats", "De tempos em tempos os cupins atacam. Um alerta aparece no alto da tela e cada cupim ganha um círculo vermelho (ou uma seta na borda). Se matarem a rainha, a partida acaba.",
                "From time to time termites attack. An alert shows at the top of the screen and each termite gets a red circle (or an arrow on the edge). If they kill the queen, the game is over.");
            Add(l, "tutorial.tour.tips.title", "Dicas e glossário", "Tips and glossary");
            Add(l, "tutorial.tour.tips", "Pare o mouse sobre qualquer botão ou cartão para ver o que ele faz e o que está faltando. Este botão abre o glossário, com a explicação de cada parte do jogo; ele também está no menu do ESC (salvar, carregar e configurações).",
                "Rest the mouse over any button or card to see what it does and what is missing. This button opens the glossary, which explains every part of the game; it is also in the ESC menu (save, load and settings).");
            Add(l, "tutorial.farewell", "O jogo volta a correr quando você fechar esta janela. Uma boa primeira jogada: cave uma câmara ligada ao abrigo e pinte nela uma sala de nutrientes. Na dúvida, o glossário (botão ? embaixo) explica tudo. Explore à vontade!",
                "The game resumes when you close this window. A good first move: dig a chamber connected to the shelter and paint a nutrient room in it. When in doubt, the glossary (? button below) explains everything. Explore freely!");

            AddQueenNodes(l);
            AddHiveNodes(l);
            AddUpgradeNodes(l);
            AddGlossary(l);
            return l;
        }

        // Inglês dos nós da árvore da rainha, na ordem de QueenNode; o português vem de QueenTree (não repete aqui).
        static readonly (string Name, string Effect)[] QueenEn =
        {
            ("Initial laying", "With the nursery ready, the queen lays 1 egg every 18 s"),
            ("Faster laying", "The queen lays an egg every 12 s instead of 18 s"),
            ("Batch laying", "Up to 3 eggs per laying, if there is food and nursery space"),
            ("Worker", "Does every basic task in the colony"),
            ("Larger load", "Workers carry 3 plant pieces instead of 2 and 4 soil instead of 3"),
            ("Fast movement", "Workers walk 25% faster"),
            ("Rescuing nestmates", "Workers rescue buried nestmates near the tunnel edge"),
            ("Carrier", "New class: carries 4 plant pieces per trip and walks 17% faster than a worker"),
            ("Bigger loads", "Carriers carry 6 plant pieces instead of 4"),
            ("Cooperative transport", "Two carriers carry a big piece together"),
            ("Carrier speed", "Carriers walk 25% faster"),
            ("Cutter", "New class: cuts plants 2× faster than a worker"),
            ("Bigger cuts", "Cutters cut 33% faster"),
            ("Digger", "New class: digs 2× faster than a worker and carries 6 soil per trip"),
            ("Wider removal", "Diggers dig 50% faster and carry 9 soil instead of 6"),
            ("Rock breaking", "Diggers can break rock"),
            ("Gardener", "New class: tends the beds and covers an area 50% larger than a worker"),
            ("Optimized fungus production", "Beds tended by gardeners are ready 30% faster"),
            ("Fungus transplant", "Gardeners set up new beds in half the time"),
            ("Nurse", "New class: tends the brood 33% faster than a worker"),
            ("Batch feeding", "Nurses tend 8 brood at once instead of 4"),
            ("Brood rescue", "In danger, nurses carry the brood away, 2 per trip"),
            ("Soldier", "New class, after the first termite attack: fights with 3× a worker's damage and health"),
            ("Strong mandibles", "Soldiers deal 50% more damage"),
            ("Tough carapace", "Soldiers have 50% more health"),
            ("Containment", "Soldiers push termites back every 3 s"),
            ("Spitter", "New class, after the first termite attack: spits acid at termites up to 8 cells away"),
            ("Midden worker", "New class, after the first termite attack: clears bodies, hauls loose items and supplies the waste area before anything else"),
            ("Farmer", "New class, with the aphid farm: fills the sand and water troughs and milks the aphids, which then give sand, leaves and water"),
            ("Careful milking", "Farmers milk the aphids 50% faster"),
        };

        // Inglês dos nós da árvore do formigueiro, na ordem de HiveTreeInfo.All.
        static readonly (string Name, string Effect)[] HiveEn =
        {
            ("Initial cultivation", "Unlocks the nutrient room, where beds turn plants into nutrients"),
            ("Organized beds", "Beds produce 30% faster"),
            ("Preparation area", "Unlocks an area where ants chop plants and keep a reserve for the beds"),
            ("Productive cultivation", "Each plant yields 20% more nutrients"),
            ("Integrated cultivation", "Ants bring the prepared reserve to beds that are running low"),
            ("Initial nursery", "Unlocks the nursery: without it, the queen lays no eggs"),
            ("Rearing beds", "Larvae grow 25% faster"),
            ("Maturation area", "Pupae become adults 25% faster"),
            ("Organized nursery", "The nursery holds 50% more brood in the same space"),
            ("Initial storage", "Unlocks the food storage to keep plants"),
            ("Organized piles", "Storages hold 50% more in the same space"),
            ("Room for big loads", "Storages keep big pieces whole"),
            ("Waste area", "Unlocks the waste area, where scraps and bodies go"),
            ("Accelerated decomposition", "Waste disappears 2× faster"),
        };

        static void AddQueenNodes(List<Entry> l)
        {
            for (int i = 0; i < QueenTree.All.Count; i++)
            {
                QueenNodeDef def = QueenTree.All[i];
                Add(l, QueenKey(def.Node, "name"), def.Name, QueenEn[i].Name);
                Add(l, QueenKey(def.Node, "effect"), def.Effect, QueenEn[i].Effect);
            }
        }

        static void AddHiveNodes(List<Entry> l)
        {
            for (int i = 0; i < HiveTreeInfo.All.Count; i++)
            {
                HiveTreeInfo.Node node = HiveTreeInfo.All[i];
                Add(l, "hive." + node.Id + ".name", node.Name, HiveEn[i].Name);
                Add(l, "hive." + node.Id + ".effect", node.Effect, HiveEn[i].Effect);
            }
        }

        // Nós novos do documento 39 (câmaras e galerias que não existiam nas árvores da rainha e do formigueiro).
        static readonly (string Id, string NamePt, string EffectPt, string NameEn, string EffectEn)[] UpgradeTexts =
        {
            // Câmara central do formigueiro (documento 39): sem texto, os requisitos "P0"/"T0" apareciam como chave crua.
            ("P0", "Postura", "A sala da rainha, onde ela põe os ovos", "Laying", "The queen room, where she lays her eggs"),
            ("T0", "Túnel comum", "Os túneis do formigueiro", "Common tunnel", "The anthill tunnels"),
            ("C5", "Jardim da rainha", "Nutrientes prontos perto da rainha entram direto no saldo, sem transporte", "Queen's garden", "Ready nutrients near the queen go straight to the balance, with no carrying"),
            ("B4", "Ninhada gêmea", "10% dos ovos viram duas larvas pelo preço de uma", "Twin brood", "10% of eggs become two larvae for the price of one"),
            ("D3", "Depósito de construção", "Libera o pincel do depósito de construção", "Building storage", "Unlocks the building storage brush"),
            ("D4", "Depósito de água", "Libera o pincel do depósito de água", "Water storage", "Unlocks the water storage brush"),
            ("D5", "Logística integrada", "Formigas sem tarefa levam os itens para o depósito mais perto de quem vai usar", "Integrated logistics", "Idle ants move items to the storage closest to whoever will use them"),
            ("R2", "Filtragem da terra", "A terra vira areia e água 2× mais rápido na área de resíduos", "Soil filtering", "Soil turns into sand and water 2× faster in the waste area"),
            ("G0", "Sala Gotas de orvalho", "Libera o pincel da sala de orvalho", "Dew drop room", "Unlocks the dew room brush"),
            ("G1", "Folhas coletoras", "Cada gota de orvalho rende o dobro de água", "Collecting leaves", "Each dew drop yields twice the water"),
            ("G2", "Neblina do amanhecer", "As gotas também regam os canteiros vizinhos da sala", "Dawn mist", "Drops also water the beds next to the room"),
            ("T1", "Despejo organizado", "Mais 2 saídas para despejar terra (4 → 6)", "Organized dump", "2 more exits to dump soil (4 → 6)"),
            ("T2", "Rotas batidas", "Túneis muito usados viram trilhas: +10% de velocidade dentro do formigueiro", "Beaten paths", "Busy tunnels become trails: +10% speed inside the anthill"),
            ("X0", "Trilha de forrageio", "Formigas andam 10% mais rápido fora do formigueiro", "Foraging trail", "Ants walk 10% faster outside the anthill"),
            ("X1", "Jardim cultivado", "Plantas cortadas perto da entrada voltam a crescer no mesmo lugar", "Tended garden", "Plants cut near the entrance grow back in the same spot"),
            ("M0", "Entradas vigiadas", "O aviso de ataque chega 4 s mais cedo (8 → 12 s)", "Guarded entrances", "The attack warning comes 4 s earlier (8 → 12 s)"),
            ("M1", "Câmara de refúgio", "Libera o pincel de refúgio: no aviso de ataque, crias e cuidadoras vão para lá", "Refuge chamber", "Unlocks the refuge brush: on the attack warning, brood and nurses go there"),
            ("M2", "Postos de guarda", "No ataque, soldados guardam as entradas até um cupim chegar", "Guard posts", "On attack, soldiers guard the entrances until a termite arrives"),
            ("Q3", "Postura em campo", "A rainha também põe ovos quando está coletando ou na sala de nutrientes", "Field laying", "The queen also lays eggs while foraging or in the nutrient room"),
            ("O4", "Operária versátil", "Sem especialista por perto, a operária faz a tarefa dela com metade do bônus", "Versatile worker", "With no specialist nearby, a worker does its task with half the bonus"),
            ("L4", "Comboio", "Grupos de carregadoras levam peças grandes inteiras direto ao depósito", "Convoy", "Groups of carriers bring big pieces whole straight to storage"),
            ("F2", "Poda seletiva", "Plantas cortadas por cortadeiras voltam a crescer 50% mais rápido", "Selective pruning", "Plants cut by cutters grow back 50% faster"),
            ("E3", "Frente larga", "Escavadoras cavam 2 células vizinhas de cada vez", "Wide front", "Diggers dig 2 neighboring cells at a time"),
            ("J3", "Simbiose", "Canteiros cuidados por jardineiras não deixam resíduo de fungo velho", "Symbiosis", "Beds tended by gardeners leave no old fungus waste"),
            ("N3", "Ama de leite", "Cada cria custa 10% menos alimento (com Geleia real, no máximo 40% menos)", "Wet nurse", "Each brood costs 10% less food (with Royal jelly, at most 40% less)"),
            ("S4", "Guarda real", "No aviso de ataque, até 4 soldados cercam a rainha", "Royal guard", "On the attack warning, up to 4 soldiers surround the queen"),
            ("H+1", "Câmara real ampliada", "Cada adulta ocupa 4% menos espaço por nível: cabem mais formigas", "Enlarged royal chamber", "Each adult takes 4% less space per level: more ants fit"),
            ("C+1", "Fungo vigoroso", "Canteiros ficam prontos 8% mais rápido por nível", "Vigorous fungus", "Beds are ready 8% faster per level"),
            ("C+2", "Colheita nutritiva", "Cada entrega de nutriente vale 5% mais por nível", "Nutritious harvest", "Each nutrient delivery is worth 5% more per level"),
            ("C+3", "Cura rápida", "Nutrientes ficam prontos para levar 4% mais rápido por nível", "Quick curing", "Nutrients are ready to carry 4% faster per level"),
            ("B+1", "Leitos aquecidos", "Larvas e pupas crescem 3% mais rápido por nível", "Warm beds", "Larvae and pupae grow 3% faster per level"),
            ("B+2", "Vagas extras", "Cada cria ocupa 5% menos espaço no berçário por nível", "Extra slots", "Each brood takes 5% less nursery space per level"),
            ("D+1", "Empilhamento", "Depósitos guardam 25% mais por nível", "Stacking", "Storages hold 25% more per level"),
            ("D+2", "Conservação", "Alimentos estragam 5% mais devagar no depósito por nível", "Preservation", "Food spoils 5% slower in storage per level"),
            ("G+1", "Orvalho farto", "Gotas de orvalho caem 5% mais rápido por nível", "Plentiful dew", "Dew drops fall 5% faster per level"),
            ("G+2", "Reservatório", "A sala de orvalho guarda +4 de água por nível", "Reservoir", "The dew room holds +4 water per level"),
            ("G+3", "Sala de orvalho ampla", "Pode pintar +20 células de sala de orvalho por nível", "Wide dew room", "You can paint +20 more dew room cells per level"),
            ("C+4", "Sala de nutrientes ampla", "Pode pintar +30 células de sala de nutrientes por nível", "Wide nutrient room", "You can paint +30 more nutrient room cells per level"),
            ("B+3", "Berçário amplo", "Pode pintar +40 células de berçário por nível", "Wide nursery", "You can paint +40 more nursery cells per level"),
            ("D+3", "Depósitos amplos", "Pode pintar +30 células de cada depósito por nível", "Wide storages", "You can paint +30 more cells of each storage per level"),
            ("R+1", "Área de resíduos ampla", "Pode pintar +24 células de área de resíduos por nível", "Wide waste area", "You can paint +24 more waste area cells per level"),
            ("T+3", "Salas extras", "Cada ambiente pode ter +1 sala separada por nível", "Extra rooms", "Each room type can have +1 separate room per level"),
            ("D6", "Depósito de areia", "Libera o pincel do depósito de areia, a comida dos pulgões", "Sand storage", "Unlocks the sand storage brush, the aphids' food"),
            ("D7", "Depósito de cogumelos", "Libera o pincel do depósito de cogumelos, onde ficam os cogumelos luminosos dos pulgões", "Mushroom storage", "Unlocks the mushroom storage brush, where the aphids' glowing mushrooms are kept"),
            ("A0", "Fazenda de pulgões", "Libera o pincel da fazenda: os pulgões nascem nas baias e, ordenhados, dão areia, folhas, água e cogumelos luminosos", "Aphid farm", "Unlocks the farm brush: aphids are born in the stalls and, when milked, give sand, leaves, water and glowing mushrooms"),
            ("A1", "Ninhada de pulgões", "Os pulgões nascem 2× mais rápido", "Aphid brood", "Aphids are born 2× faster"),
            ("A+1", "Rebanho", "A fazenda cria +1 pulgão por nível", "Herd", "The farm raises +1 aphid per level"),
            ("A+2", "Ordenha", "A ordenha dos pulgões fica 5% mais rápida por nível", "Milking", "Milking the aphids is 5% faster per level"),
            ("A+3", "Fazenda ampla", "Pode pintar +20 células de fazenda por nível", "Wide farm", "You can paint +20 more farm cells per level"),
            ("Z+1", "Passo de pastora", "Fazendeiras andam 4% mais rápido por nível", "Herder's stride", "Farmers walk 4% faster per level"),
            ("Q+4", "Sala real ampla", "Pode pintar +40 células de sala da rainha por nível", "Wide royal room", "You can paint +40 more queen room cells per level"),
            ("T+1", "Obra barata", "Pintar ambientes custa 3% menos por nível", "Cheap works", "Painting rooms costs 3% less per level"),
            ("T+2", "Frentes de obra", "Todas as formigas cavam 3% mais rápido por nível", "Work fronts", "Every ant digs 3% faster per level"),
            ("X+1", "Brotação", "Plantas de fora crescem 6% mais rápido por nível", "Sprouting", "Outdoor plants grow 6% faster per level"),
            ("X+2", "Feromônio de trilha", "Formigas andam 3% mais rápido fora do formigueiro por nível", "Trail pheromone", "Ants walk 3% faster outside the anthill per level"),
            ("X+3", "Faro", "Formigas acham plantas 5% mais longe por nível", "Scent", "Ants find plants 5% farther away per level"),
            ("M+1", "Paredes reforçadas", "Cupins levam 10% mais tempo para quebrar paredes por nível", "Reinforced walls", "Termites take 10% longer to break walls per level"),
            ("M+2", "Vigias", "O aviso de ataque chega 1 s mais cedo por nível", "Lookouts", "The attack warning comes 1 s earlier per level"),
            ("Q+1", "Fertilidade", "A rainha põe ovos 3% mais rápido por nível", "Fertility", "The queen lays eggs 3% faster per level"),
            ("Q+2", "Geleia real", "Cada cria custa 2% menos alimento por nível", "Royal jelly", "Each brood costs 2% less food per level"),
            ("Q+3", "Vigor real", "A rainha tem 10% mais vida por nível", "Royal vigor", "The queen has 10% more health per level"),
            ("O+1", "Passo firme", "Operárias andam 4% mais rápido por nível", "Steady stride", "Workers walk 4% faster per level"),
            ("O+2", "Vitalidade", "Todas as adultas têm 10% mais vida em combate por nível", "Vitality", "Every adult has 10% more health in combat per level"),
            ("O+3", "Mandíbula de trabalho", "Operárias cortam plantas 3% mais rápido por nível", "Work mandible", "Workers cut plants 3% faster per level"),
            ("L+1", "Costas largas", "Carregadoras levam +1 pedaço de planta por nível", "Broad back", "Carriers carry +1 plant piece per level"),
            ("L+2", "Passo de carga", "Carregadoras andam 4% mais rápido por nível", "Hauling stride", "Carriers walk 4% faster per level"),
            ("L+3", "Corrida com carga", "Carregadoras com carga na mão andam 4% mais rápido por nível", "Loaded sprint", "Carriers walk 4% faster while carrying a load, per level"),
            ("F+1", "Serrilha", "Cortadeiras cortam 4% mais rápido por nível", "Serration", "Cutters cut 4% faster per level"),
            ("E+1", "Mandíbulas de pá", "Escavadoras cavam 4% mais rápido por nível", "Shovel mandibles", "Diggers dig 4% faster per level"),
            ("E+2", "Carga de terra", "Escavadoras levam +1 de terra por viagem por nível", "Soil load", "Diggers carry +1 soil per trip per level"),
            ("E+3", "Britador", "Escavadoras quebram rocha 6% mais rápido por nível", "Crusher", "Diggers break rock 6% faster per level"),
            ("J+1", "Mãos verdes", "Jardineiras deixam os canteiros 5% mais rápidos por nível", "Green thumb", "Gardeners make beds 5% faster per level"),
            ("J+2", "Orvalheira", "Jardineiras fazem as gotas de orvalho cair 10% mais rápido por nível", "Dew keeper", "Gardeners make dew drops fall 10% faster per level"),
            ("N+1", "Ninhada atenta", "Cuidadoras param 5% menos entre um cuidado e outro por nível", "Attentive care", "Nurses pause 5% less between tasks per level"),
            ("N+2", "Turno dobrado", "Cada cuidadora atende +1 parte do berçário por nível", "Double shift", "Each nurse covers +1 nursery section per level"),
            ("S+1", "Mandíbulas afiadas", "Soldados causam 8% mais dano por nível", "Sharp mandibles", "Soldiers deal 8% more damage per level"),
            ("S+2", "Carapaça grossa", "Soldados têm 8% mais vida por nível", "Thick carapace", "Soldiers have 8% more health per level"),
            ("S+3", "Recuperação", "Soldados feridos começam a se curar 5% mais cedo por nível", "Recovery", "Wounded soldiers start healing 5% sooner per level"),
            ("S+4", "Fúria de batalha", "Soldados andam 5% mais rápido enquanto estão em batalha, por nível", "Battle fury", "Soldiers walk 5% faster while in battle, per level"),
        };

        static void AddUpgradeNodes(List<Entry> l)
        {
            foreach ((string id, string namePt, string effectPt, string nameEn, string effectEn) in UpgradeTexts)
            {
                Add(l, "upgrade." + id + ".name", namePt, nameEn);
                Add(l, "upgrade." + id + ".effect", effectPt, effectEn);
            }
        }

        /// <summary>Chave de um campo (<c>name</c> ou <c>effect</c>) de um nó da árvore da rainha.</summary>
        public static string QueenKey(QueenNode node, string field) => "queen." + node + "." + field;
    }
}
