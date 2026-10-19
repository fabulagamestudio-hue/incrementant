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
            Add(l, "queen.mode.auto", "AUTOMÁTICO", "AUTOMATIC");
            Add(l, "queen.mode.specific", "TIPO ESPECÍFICO", "SPECIFIC TYPE");
            Add(l, "queen.mode.paused", "PAUSADO", "PAUSED");
            Add(l, "queen.stance.offensive", "OFENSIVA: ATACA OS CUPINS", "OFFENSIVE: ATTACKS THE TERMITES");
            Add(l, "queen.stance.defensive", "DEFENSIVA: SÓ SE DEFENDE", "DEFENSIVE: ONLY DEFENDS");
            Add(l, "role.worker", "Comum", "Common");
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
            Add(l, "tip.menu.environments", "Salas do formigueiro. Pinte onde ficam a rainha, o cultivo, as crias, o lixo e o orvalho.", "Anthill rooms. Paint where the queen, the farm, the brood, the waste and the dew go.");
            Add(l, "tip.menu.queen", "Rainha: escolha o tipo de formiga que ela põe, como reage aos cupins e acompanhe a criação.", "Queen: choose the type of ant she lays, how she reacts to termites, and follow the breeding.");
            Add(l, "tip.menu.evolution", "Evolução: gaste nutrientes em novas classes de formiga e melhorias.", "Evolution: spend nutrients on new ant classes and upgrades.");
            Add(l, "tip.recall.on", "Volte para o formigueiro: as formigas terminam o que carregam e param de trabalhar.", "Back to the nest: ants finish what they carry and stop working.");
            Add(l, "tip.recall.off", "Volte ao trabalho: as formigas retomam as tarefas.", "Back to work: ants resume their tasks.");
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
            Add(l, "tip.brush.dew", "Gotas de orvalho: as formigas põem terra fofa e folhas no teto da sala, e dela pingam gotas de água para regar os canteiros.", "Dew drops: ants place loose soil and leaves on the room's ceiling, and water drops fall from it to water the beds.");
            Add(l, "tip.card.nutrients", "Nutrientes: a moeda das formigas novas. A rainha consome o que as formigas guardam na sala de nutrientes.", "Nutrients: the currency for new ants. The queen eats what the ants store in the nutrient room.");
            Add(l, "tip.card.nutrients.balance", "Saldo {0} N; a rainha preserva {1} N antes de pôr um ovo.", "Balance {0} N; the queen keeps {1} N in reserve before laying an egg.");
            Add(l, "tip.card.ants", "Formigas: adultas / vagas de adulta (vêm da área escavada do formigueiro) e crias.", "Ants: adults / adult slots (from the dug area of the anthill) and brood.");
            Add(l, "tip.card.ants.capacity", "Agora: {0} de {1} vagas; {2} crias em {3} vagas de berçário.", "Now: {0} of {1} slots; {2} brood in {3} nursery slots.");
            Add(l, "tip.card.queen", "Rainha: precisa da própria sala, de berçário, de vaga e de nutrientes para pôr ovos.", "Queen: needs her own room, a nursery, a free slot and nutrients to lay eggs.");
            Add(l, "tip.role.worker", "Comum: faz de tudo um pouco e corta plantas quando está sem outra tarefa.", "Common: does a bit of everything and cuts plants when it has no other task.");
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
            Add(l, "tip.st.waste.nospot", "Falta: espaço livre. Cave a área, com chão livre para deixar resíduos.", "Missing: free space. Dig the area, with free floor to leave waste on.");
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
            Add(l, "buy.owned", "JÁ COMPRADO", "ALREADY BOUGHT");
            Add(l, "buy.missing", "FALTA UM PRÉ-REQUISITO", "A PREREQUISITE IS MISSING");
            Add(l, "buy.soon", "EM BREVE: O JOGO AINDA NÃO TEM ESSE SISTEMA", "COMING SOON: THE GAME DOESN'T HAVE THIS SYSTEM YET");
            Add(l, "buy.insufficient", "NUTRIENTES INSUFICIENTES", "NOT ENOUGH NUTRIENTS");
            Add(l, "buy.raid", "LIBERA DEPOIS DO PRIMEIRO ATAQUE DE CUPINS", "UNLOCKS AFTER THE FIRST TERMITE ATTACK");
            Add(l, "buy.done", "COMPRADO", "BOUGHT");
            string[] branchPt = { "RAINHA", "COMUM", "CARREGADORA", "CORTADEIRA", "ESCAVADORA", "JARDINEIRA", "CUIDADORA", "SOLDADO" };
            string[] branchEn = { "QUEEN", "COMMON", "CARRIER", "CUTTER", "DIGGER", "GARDENER", "NURSE", "SOLDIER" };
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
            return l;
        }

        // Inglês dos nós da árvore da rainha, na ordem de QueenNode; o português vem de QueenTree (não repete aqui).
        static readonly (string Name, string Effect)[] QueenEn =
        {
            ("Initial laying", "No cycles before the nursery → 1 egg every 18 s"),
            ("Faster laying", "18 s per cycle → 12 s per cycle (−33%)"),
            ("Batch laying", "1 egg per cycle → up to 3 eggs per cycle; each pays its own cost, partial batch if food or space runs out"),
            ("Common worker", "Generalist: keeps up all basic tasks"),
            ("Larger load", "Load of 2 UM or 32 cells → 3 UM or 48 (+50%) for common workers"),
            ("Fast movement", "24 cells/s → 30 cells/s (+25%) for common workers"),
            ("Rescuing nestmates", "No pulling of adults → rescues an adult up to 8 cells from the edge"),
            ("Carrier", "Unlocks the class: 28 cells/s, load 4 UM"),
            ("Bigger loads", "4 UM → 6 UM (+50%)"),
            ("Cooperative transport", "Solo → 2 carriers carry a piece of up to 8 UM"),
            ("Carrier speed", "28 cells/s → 35 cells/s (+25%)"),
            ("Cutter", "Unlocks the class: cuts 2 UM in 1 s (common takes 2 s)"),
            ("Bigger cuts", "2 UM in 1 s → 4 UM in 1.5 s"),
            ("Digger", "Unlocks the class: digs 32 cells/s (common does 16)"),
            ("Wider removal", "32 cells/s → 48 cells/s (+50%) when digging"),
            ("Rock breaking", "Stone blocks → breaks 4 rock cells/s"),
            ("Gardener", "Unlocks the class: tends the crops (serves 6 B, common 4 B)"),
            ("Optimized fungus production", "Growth (nutrient curing) ×1 → ×1.3 in the served area"),
            ("Fungus transplant", "Setup of 8 s per B → 4 s per B"),
            ("Nurse", "Unlocks the class: tends 4 brood in 3 s"),
            ("Batch feeding", "4 brood per trip → 8 brood per trip"),
            ("Brood rescue", "No evacuation → carries 2 brood per trip, with emergency priority"),
            ("Soldier", "Unlocks the class: damage 3/s, health 18 (after the first termite attack)"),
            ("Strong mandibles", "Damage 3/s → 4.5/s (+50%)"),
            ("Tough carapace", "Health 18 → 27 (+50%)"),
            ("Containment", "Combat without knockback → pushes the invader 4 cells every 3 s"),
        };

        // Inglês dos nós da árvore do formigueiro, na ordem de HiveTreeInfo.All.
        static readonly (string Name, string Effect)[] HiveEn =
        {
            ("Initial cultivation", "No production → 0.25 UM/s per B; 3 N per UM of leaf"),
            ("Organized beds", "0.25 UM/s per B → 0.325 UM/s per B (+30%)"),
            ("Preparation area", "Direct leaves → prepares 2 UM in 2 s; reserve of 12 UM per B"),
            ("Productive cultivation", "3 N per UM of leaf → 3.6 N (+20%); fruit 6 → 7.2 N"),
            ("Integrated cultivation", "Redistributes the prepared reserve among crops; target of 6 UM per B"),
            ("Initial nursery", "No new brood → 2 brood slots per B"),
            ("Rearing beds", "Larva 14 s → 10.5 s (−25%)"),
            ("Maturation area", "Pupa 12 s → 9 s (−25%)"),
            ("Organized nursery", "2 slots per B → 3 slots per B (+50%)"),
            ("Initial storage", "6 UM per B in cultivation → 20 UM per B in storage"),
            ("Organized piles", "20 UM per B → 30 UM per B (+50%)"),
            ("Room for big loads", "Loose loads of up to 4 UM → stores whole pieces of up to 8 UM"),
            ("Waste area", "Waste takes up its source → 12 UR per B; decomposition of 120 s per UR"),
            ("Accelerated decomposition", "120 s per UR → 60 s per UR (−50%)"),
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

        /// <summary>Chave de um campo (<c>name</c> ou <c>effect</c>) de um nó da árvore da rainha.</summary>
        public static string QueenKey(QueenNode node, string field) => "queen." + node + "." + field;
    }
}
