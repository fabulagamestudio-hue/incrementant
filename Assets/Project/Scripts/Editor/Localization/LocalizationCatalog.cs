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
    public static class LocalizationCatalog
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
            Add(l, "brush.waterstore", "Depósito de água", "Water storage");
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
            Add(l, "raid.obj.farms", "DESTRUINDO CANTEIROS", "DESTROYING BEDS");
            Add(l, "raid.obj.sectors", "DESTRUINDO OS SETORES", "DESTROYING THE SECTORS");
            Add(l, "raid.obj.walls", "DERRUBANDO PAREDES", "BREAKING WALLS");
            Add(l, "raid.obj.queen", "ATRÁS DA RAINHA", "AFTER THE QUEEN");
            Add(l, "raid.obj.leave", "INDO EMBORA", "LEAVING");

            // ---------------------------------------------------------------- HUD: dicas (tooltips) dos botões e das seções
            Add(l, "tip.menu.paint", "Ferramentas do mapa: cavar terra, marcar obras e o dedo, que fere cupins.", "Map tools: dig soil, mark building work, and the finger, which hurts termites.");
            Add(l, "tip.menu.environments", "Salas do formigueiro. Pinte onde ficam a rainha, o cultivo, as crias, o lixo, o orvalho e os depósitos.", "Anthill rooms. Paint where the queen, the farm, the brood, the waste, the dew and the storage go.");
            Add(l, "tip.menu.depots", "Depósitos de alimentos, de construção e de água. O que as formigas trazem de fora fica guardado ali, sem fechar a passagem, e é usado de lá primeiro.", "Food, building and water storage. What the ants bring from outside is kept there, without blocking the way, and is used from there first.");
            Add(l, "tip.menu.queen", "Rainha: escolha o tipo de formiga que ela põe, como reage aos cupins e acompanhe a criação.", "Queen: choose the type of ant she lays, how she reacts to termites, and follow the breeding.");
            Add(l, "tip.menu.evolution", "Evolução: gaste nutrientes em novas classes de formiga e melhorias.", "Evolution: spend nutrients on new ant classes and upgrades.");
            Add(l, "tip.recall.on", "Volte para o formigueiro: as formigas terminam o que carregam e param de trabalhar.", "Back to the nest: ants finish what they carry and stop working.");
            Add(l, "tip.recall.off", "Volte ao trabalho: as formigas retomam as tarefas.", "Back to work: ants resume their tasks.");
            Add(l, "tip.menu.colony", "Painel da colônia: estatísticas das formigas e da produção, e quais trabalhos ficam ativos.", "Colony panel: ant and production statistics, and which jobs stay active.");

            // ---------------------------------------------------------------- painel da colônia
            Add(l, "colony.title", "PAINEL DA COLÔNIA", "COLONY PANEL");
            Add(l, "colony.tab.stats", "ESTATÍSTICAS", "STATISTICS");
            Add(l, "colony.col.section", "SEÇÃO DE TRABALHO", "WORK SECTION");
            Add(l, "colony.col.now", "AGORA", "NOW");
            Add(l, "colony.col.limit", "LIMITE DE FORMIGAS", "ANT LIMIT");
            Add(l, "colony.queen.title", "TRABALHO DA RAINHA", "QUEEN'S JOB");
            Add(l, "colony.queen.nest", "NINHO: PÔR OVOS", "NEST: LAY EGGS");
            Add(l, "colony.queen.forage", "COLETAR RECURSOS", "GATHER RESOURCES");
            Add(l, "colony.queen.nutrients", "COLETAR NUTRIENTE", "GATHER NUTRIENT");
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
            Add(l, "colony.place.wasteroom", "Resíduos", "Waste");
            Add(l, "colony.rate.toqueen", "Nutriente à rainha", "Nutrient to queen");
            Add(l, "colony.rate.grown", "Nutriente brotado", "Nutrient sprouted");
            Add(l, "colony.rate.harvest", "Planta colhida", "Plant harvested");
            Add(l, "colony.rate.soil", "Terra escavada", "Soil dug out");
            Add(l, "colony.stopped", "PARADA", "STOPPED");
            Add(l, "colony.limit.none", "SEM LIMITE", "NO LIMIT");
            Add(l, "colony.limit.max", "MÁX {0}", "MAX {0}");
            Add(l, "colony.queen.nesting", "No ninho, pondo ovos", "In the nest, laying eggs");
            Add(l, "colony.queen.foraging", "Coletando recursos", "Gathering resources");
            Add(l, "colony.queen.eating", "Coletando nutriente", "Gathering nutrient");
            Add(l, "colony.queen.state.nest", "A rainha fica na sala dela pondo ovos. Agora: {0}.", "The queen stays in her room laying eggs. Now: {0}.");
            Add(l, "colony.queen.state.nutrients", "Na sala de nutrientes, comendo cada nutriente pronto (sem ovos). Agora: {0}.", "In the nutrient room, eating each ready nutrient (no eggs). Now: {0}.");
            Add(l, "colony.queen.state.forage", "A rainha coleta e não põe ovos fora da sala. Agora: {0}.", "The queen gathers and lays no eggs outside her room. Now: {0}.");
            Add(l, "tip.colony.toggle", "Clique para ligar ou parar. Parada, nenhuma formiga nova entra; quem está nela termina o que carrega.", "Click to turn on or stop. Stopped, no new ant joins; those in it finish what they carry.");
            Add(l, "tip.colony.minus", "Diminui o limite de formigas nesta seção.", "Lowers the ant limit for this section.");
            Add(l, "tip.colony.plus", "Aumenta o limite; acima do total de formigas, fica sem limite.", "Raises the limit; above the total number of ants, there is no limit.");
            Add(l, "tip.colony.slider", "Arraste para escolher o limite; no fim do trilho, fica sem limite.", "Drag to pick the limit; at the end of the track, there is no limit.");
            Add(l, "tip.colony.reset", "Liga todas as seções e tira todos os limites.", "Turns every section on and removes every limit.");
            Add(l, "tip.colony.queen.nest", "A rainha fica na sala dela e põe ovos.", "The queen stays in her room and lays eggs.");
            Add(l, "tip.colony.queen.nutrients", "A rainha vai à sala de nutrientes, fica lá e come cada nutriente assim que fica pronto, sem cortar plantas. Enquanto isso, não põe ovos.", "The queen goes to the nutrient room, stays there and eats each nutrient as soon as it is ready, without cutting plants. Meanwhile, she lays no eggs.");
            Add(l, "tip.colony.queen.forage", "A rainha sai para coletar: come o nutriente guardado e corta e carrega plantas de fora. Enquanto isso, não põe ovos.", "The queen goes out to gather: she eats stored nutrient and cuts and carries plants from outside. Meanwhile, she lays no eggs.");
            Add(l, "tip.colony.section.dig", "Escavação: cavar a terra marcada e levá-la para fora.", "Digging: dig the marked soil and carry it outside.");
            Add(l, "tip.colony.section.build", "Construção: buscar terra na pilha de fora e montar as obras.", "Building: fetch soil from the outside pile and raise the structures.");
            Add(l, "tip.colony.section.harvest", "Coleta de plantas: cortar plantas de fora e levar os pedaços aos canteiros.", "Plant gathering: cut outside plants and carry the pieces to the beds.");
            Add(l, "tip.colony.section.queenfeed", "Alimentar a rainha: levar o nutriente guardado até ela.", "Feeding the queen: carry stored nutrient to her.");
            Add(l, "tip.colony.section.nursery", "Berçário: levar os ovos e cuidar das crias.", "Nursery: carry the eggs and tend the brood.");
            Add(l, "tip.colony.section.garden", "Sala de nutrientes: montar canteiros, regar e cuidar do que brota.", "Nutrient room: build beds, water them and tend what sprouts.");
            Add(l, "tip.colony.section.dew", "Gotas de orvalho: terra fofa no teto e cuidado das gotas.", "Dew drops: loose soil on the ceiling and care of the drops.");
            Add(l, "tip.colony.section.waste", "Resíduos: desmembrar cupins mortos e levar os restos.", "Waste: dismember dead termites and carry the remains away.");
            Add(l, "tip.pause", "Pausa ou retoma a simulação (tecla P).", "Pauses or resumes the simulation (P key).");
            Add(l, "tip.speed", "Velocidade {0}x (tecla {1}).", "Speed {0}x (key {1}).");
            Add(l, "tip.brush.dig", "Cavar: marque a terra que as escavadoras devem tirar. Botão esquerdo marca, direito desmarca.", "Dig: mark the soil the diggers must remove. Left button marks, right button unmarks.");
            Add(l, "tip.brush.build", "Construir: marque células vazias para as formigas levantarem terra, até acima do solo. Botão direito desmarca.", "Build: mark empty cells for the ants to fill with soil, even above the ground. Right button unmarks.");
            Add(l, "tip.brush.finger", "Dedo: clique num cupim para feri-lo. Clicar na rainha força um ovo.", "Finger: click a termite to hurt it. Clicking the queen forces an egg.");
            Add(l, "tip.room.paint", "Esquerdo pinta, direito apaga.", "Left paints, right erases.");
            Add(l, "tip.brush.queenroom", "Sala da rainha: onde ela fica e põe os ovos.", "Queen room: where she stays and lays eggs.");
            Add(l, "tip.brush.nutrientroom", "Sala de nutrientes: o cultivo. As formigas montam canteiros (terra fofa, planta e água) e deles brotam os nutrientes.", "Nutrient room: the farm. Ants build beds (loose soil, plant and water) and nutrients sprout from them.");
            Add(l, "tip.brush.nursery", "Berçário: onde ovos, larvas e pupas se desenvolvem. As vagas de cria vêm das células livres dele.", "Nursery: where eggs, larvae and pupae develop. Brood slots come from its free cells.");
            Add(l, "tip.brush.waste", "Resíduos: onde as formigas deixam entulho e restos para se decomporem.", "Waste: where ants leave rubble and scraps to decompose.");
            Add(l, "tip.brush.waste.locked", "Resíduos: bloqueado até você repelir o primeiro ataque de cupins.", "Waste: locked until you repel the first termite attack.");
            Add(l, "tip.brush.foodstore", "Depósito de alimentos: guarda os pedaços de planta cortados. Com ele, os canteiros e a sala de orvalho só recebem planta daqui.", "Food storage: keeps the cut plant pieces. With it, the beds and the dew room only get plants from here.");
            Add(l, "tip.brush.blockstore", "Depósito de construção: guarda a terra cavada. Com ele, obras, canteiros e a sala de orvalho só usam a terra daqui.", "Building storage: keeps the dug soil. With it, builds, beds and the dew room only use the soil from here.");
            Add(l, "tip.brush.waterstore", "Depósito de água: guarda a água tirada do mapa. Com ele, os canteiros só são regados com a água daqui.", "Water storage: keeps the water taken from the map. With it, the beds are only watered with the water from here.");
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
            Add(l, "tip.role.carrier", "Carregadora: a mais rápida; leva ovos e carrega mais material por viagem.", "Carrier: the fastest; carries eggs and hauls more material per trip.");
            Add(l, "tip.role.cutter", "Cortadeira: corta as plantas da superfície na metade do tempo e as leva à sala de nutrientes.", "Cutter: cuts surface plants in half the time and takes them to the nutrient room.");
            Add(l, "tip.role.digger", "Escavadora: especialista em cavar a terra marcada e despejá-la fora.", "Digger: specialist in digging the marked soil and dumping it outside.");
            Add(l, "tip.role.gardener", "Jardineira: leva terra e água aos canteiros e cuida do cultivo.", "Gardener: brings soil and water to the beds and tends the farm.");
            Add(l, "tip.role.nurse", "Cuidadora: cuida das crias no berçário e leva os ovos para lá.", "Nurse: looks after the brood in the nursery and carries eggs there.");
            Add(l, "tip.role.soldier", "Soldado: combate os cupins e recolhe os corpos.", "Soldier: fights the termites and clears the bodies.");
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
            Add(l, "settings.fullscreen", "TELA CHEIA", "FULLSCREEN");
            Add(l, "settings.resolution", "RESOLUÇÃO", "RESOLUTION");
            Add(l, "settings.vsync", "VSYNC", "VSYNC");
            Add(l, "settings.fps", "LIMITE DE FPS", "FPS LIMIT");
            Add(l, "settings.stats", "MOSTRAR FPS E TEMPO", "SHOW FPS AND TIME");
            Add(l, "settings.language", "IDIOMA", "LANGUAGE");
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

            // ---------------------------------------------------------------- tutorial
            Add(l, "tutorial.title.intro", "Uma nova colônia", "A new colony");
            Add(l, "tutorial.title.outro", "Agora é com você!", "Now it's up to you!");
            Add(l, "tutorial.start", "Começar", "Start");
            Add(l, "tutorial.gotit", "Entendi", "Got it");
            Add(l, "tutorial.continue", "Continuar", "Continue");
            Add(l, "tutorial.grow", "Vamos crescer!", "Let's grow!");
            Add(l, "tutorial.viewgoal", "Ver objetivo", "Show objective");
            Add(l, "tutorial.skip", "Pular tutorial", "Skip tutorial");
            Add(l, "tutorial.intro", "Uma nova colônia começa aqui. Sua rainha conta com cinco operárias para transformar este pequeno abrigo em um formigueiro. Vamos abrir espaço e preparar sua colônia para crescer!",
                "A new colony begins here. Your queen has five workers to turn this small shelter into an anthill. Let's make room and get your colony ready to grow!");
            Add(l, "tutorial.choosedig", "Selecione o pincel de escavação para abrir novos espaços no formigueiro.",
                "Select the digging brush to open new spaces in the anthill.");
            Add(l, "tutorial.markfirst", "Clique e arraste sobre a terra indicada para marcar onde suas operárias devem cavar.",
                "Click and drag over the indicated soil to mark where your workers should dig.");
            Add(l, "tutorial.waitdig", "Você marca o caminho; suas operárias fazem o trabalho. Aguarde até terminarem de cavar.",
                "You mark the path; your workers do the work. Wait until they finish digging.");
            Add(l, "tutorial.paintnutrients", "Agora vamos dar uma função a esse espaço. Selecione o pincel de nutrientes e pinte o interior da área que você acabou de escavar.",
                "Now let's give this space a purpose. Select the nutrient brush and paint the inside of the area you just dug.");
            Add(l, "tutorial.nutrientsinfo", "Aqui, suas operárias cultivarão fungos para produzir nutrientes. Eles sustentam o crescimento da colônia e permitem investir em novas áreas e melhorias.",
                "Here, your workers will cultivate fungi to produce nutrients. They sustain the colony's growth and let you invest in new areas and upgrades.");
            Add(l, "tutorial.marksecond", "A colônia precisa de novas gerações para crescer. Use o pincel de escavação para abrir mais uma câmara conectada ao formigueiro.",
                "The colony needs new generations to grow. Use the digging brush to open one more chamber connected to the anthill.");
            Add(l, "tutorial.paintnursery", "Selecione o pincel de berçário e pinte o novo espaço. Ali, suas operárias cuidarão das crias.",
                "Select the nursery brush and paint the new space. There, your workers will tend to the brood.");
            Add(l, "tutorial.queen", "Com um berçário funcionando e nutrientes disponíveis, sua rainha se reproduz automaticamente.",
                "With a working nursery and available nutrients, your queen reproduces automatically.");
            Add(l, "tutorial.brood", "Os ovos passam pelas fases de larva e pupa até se tornarem novas formigas. Suas operárias cuidam desse desenvolvimento.",
                "Eggs go through the larva and pupa stages until they become new ants. Your workers look after this development.");
            Add(l, "tutorial.opentree", "Sua colônia pode ir além! Abra a árvore de evolução para conhecer suas possibilidades.",
                "Your colony can go further! Open the evolution tree to see your possibilities.");
            Add(l, "tutorial.treetabs", "Use nutrientes para desbloquear habilidades e melhorias. A árvore do formigueiro desenvolve a colônia; a árvore da rainha aprimora suas capacidades.",
                "Use nutrients to unlock skills and upgrades. The anthill tree develops the colony; the queen tree improves her abilities.");
            Add(l, "tutorial.treedetail", "Confira o efeito, o custo e os requisitos de cada habilidade antes de escolher onde investir.",
                "Check each skill's effect, cost and requirements before choosing where to invest.");
            Add(l, "tutorial.threat", "Mas cuidado: inimigos podem chegar e ameaçar seu formigueiro. Suas formigas precisarão batalhar para defender a colônia.",
                "But be careful: enemies may arrive and threaten your anthill. Your ants will have to fight to defend the colony.");
            Add(l, "tutorial.defense", "Fortaleça sua colônia e prepare suas defesas. Proteja a rainha: a sobrevivência do formigueiro depende dela!",
                "Strengthen your colony and prepare your defenses. Protect the queen: the anthill's survival depends on her!");
            Add(l, "tutorial.outro", "Seu pequeno formigueiro tem um grande futuro pela frente. Expanda os túneis, cuide das novas gerações e fortaleça sua colônia. Agora é com você!",
                "Your small anthill has a great future ahead. Expand the tunnels, care for the new generations and strengthen your colony. Now it's up to you!");

            AddQueenNodes(l);
            AddHiveNodes(l);
            AddUpgradeNodes(l);
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
            ("C5", "Jardim da rainha", "Nutrientes prontos perto da rainha entram direto no saldo, sem transporte", "Queen's garden", "Ready nutrients near the queen go straight to the balance, with no carrying"),
            ("B4", "Ninhada gêmea", "10% dos ovos viram duas larvas pelo preço de uma", "Twin brood", "10% of eggs become two larvae for the price of one"),
            ("D3", "Depósito de construção", "Libera o pincel do depósito de construção", "Building storage", "Unlocks the building storage brush"),
            ("D4", "Depósito de água", "Libera o pincel do depósito de água", "Water storage", "Unlocks the water storage brush"),
            ("D5", "Logística integrada", "Formigas sem tarefa levam os itens para o depósito mais perto de quem vai usar", "Integrated logistics", "Idle ants move items to the storage closest to whoever will use them"),
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
