using System.Collections.Generic;

namespace Fabula.Incrementant.EditorTools.Localization
{
    /// <summary>
    /// Parcial do catálogo: textos do glossário (ordem e ids em <c>GlossaryContent</c>). Cada entrada tem título e texto; o texto
    /// cabe em umas 49 letras por linha na coluna da direita e rola se passar da altura.
    /// </summary>
    public static partial class LocalizationCatalog
    {
        static void AddGlossary(List<Entry> l)
        {
            Add(l, "glossary.title", "GLOSSÁRIO", "GLOSSARY");
            Add(l, "tip.glossary", "Glossário: a explicação de cada parte do jogo.", "Glossary: an explanation of every part of the game.");

            Add(l, "glossary.cat.basics", "O básico", "Basics");
            Add(l, "glossary.cat.resources", "Recursos e mundo", "Resources and world");
            Add(l, "glossary.cat.tools", "Ferramentas", "Tools");
            Add(l, "glossary.cat.rooms", "Ambientes", "Rooms");
            Add(l, "glossary.cat.ants", "Formigas", "Ants");
            Add(l, "glossary.cat.queenmenu", "Menu da rainha", "Queen menu");
            Add(l, "glossary.cat.evolution", "Evolução", "Evolution");
            Add(l, "glossary.cat.threats", "Ameaças", "Threats");
            Add(l, "glossary.cat.interface", "Interface", "Interface");

            // --- o básico
            AddGlossaryEntry(l, "goal", "Como se joga", "How to play",
                   "Você não controla cada formiga. Você marca onde cavar e construir, pinta para que serve cada sala e decide que formigas a rainha cria; as formigas escolhem o trabalho sozinhas. A colônia cresce com nutrientes e com o espaço escavado.",
                   "You don't control each ant. You mark where to dig and build, paint what each room is for and decide which ants the queen raises; the ants pick their work on their own. The colony grows with nutrients and dug space.");
            AddGlossaryEntry(l, "camera", "Câmera", "Camera",
                   "WASD, as setas ou arrastar com o botão do meio do mouse movem a vista. A rolagem do mouse (ou Q e E) aproxima e afasta. O formigueiro é a terra embaixo; a superfície fica em cima.",
                   "WASD, the arrow keys or dragging with the middle mouse button move the view. The mouse wheel (or Q and E) zooms in and out. The anthill is the soil below; the surface is on top.");
            AddGlossaryEntry(l, "pause", "Pausa", "Pause",
                   "O botão de pausa, no canto inferior esquerdo, ou a tecla P congelam o jogo. Pausado, você ainda pode marcar e pintar; as formigas começam quando o jogo voltar. O menu do ESC e o glossário também pausam enquanto estão abertos.",
                   "The pause button, in the bottom left corner, or the P key freeze the game. While paused you can still mark and paint; the ants start when the game resumes. The ESC menu and the glossary also pause while they are open.");
            AddGlossaryEntry(l, "save", "Salvar e carregar", "Save and load",
                   "O menu do ESC tem Salvar partida e Carregar partida. Atalhos: F5 salva e F9 carrega o último save. Continuar, no menu principal, abre a partida salva.",
                   "The ESC menu has Save game and Load game. Shortcuts: F5 saves and F9 loads the last save. Continue, in the main menu, opens the saved game.");
            AddGlossaryEntry(l, "gameover", "Fim de partida", "Game over",
                   "Se os cupins matarem a rainha, a partida acaba. Na tela de fim de jogo dá para carregar o último save ou começar um mapa novo.",
                   "If the termites kill the queen, the game is over. The game over screen lets you load the last save or start a new map.");

            // --- recursos e mundo
            AddGlossaryEntry(l, "nutrients", "Nutrientes (N)", "Nutrients (N)",
                   "A moeda da colônia. Brotam dos canteiros da sala de nutrientes. A rainha come nutrientes para pôr cada ovo (e guarda uma reserva antes) e você os gasta em Evolução.",
                   "The colony's currency. They sprout from the beds in the nutrient room. The queen eats nutrients to lay each egg (keeping a reserve first) and you spend them in Evolution.");
            AddGlossaryEntry(l, "slots", "Vagas", "Slots",
                   "Vagas de adulta vêm da área escavada do formigueiro: cavar mais abre espaço para mais formigas. Vagas de cria vêm das células livres do berçário. Sem vaga, a rainha não põe ovos.",
                   "Adult slots come from the dug area of the anthill: digging more makes room for more ants. Brood slots come from the free cells of the nursery. Without a free slot, the queen lays no eggs.");
            AddGlossaryEntry(l, "soil", "Terra, areia e pedra", "Soil, sand and stone",
                   "A terra cavada vira obra, canteiro e orvalho, ou é despejada fora. A areia solta escorre e cai; cavada, vai para o depósito de areia e alimenta os pulgões. A pedra demora mais para cavar.",
                   "Dug soil becomes building work, beds and dew, or is dumped outside. Loose sand slides and falls; once dug it goes to the sand storage and feeds the aphids. Stone takes longer to dig.");
            AddGlossaryEntry(l, "water", "Água", "Water",
                   "Fica em laguinhos, escorre e vem da chuva. As formigas levam água aos canteiros. A sala de orvalho e os pulgões também dão água. A água amolece a terra e uma formiga presa nela pode se afogar.",
                   "It sits in ponds, flows and comes from the rain. Ants carry water to the beds. The dew room and the aphids also give water. Water erodes the soil, and an ant trapped in it can drown.");
            AddGlossaryEntry(l, "rain", "Chuva", "Rain",
                   "A chuva leve dissolve a terra despejada; a forte compacta o que está solto. As duas trazem água para o mapa. Um ataque de cupins espera a chuva passar.",
                   "Light rain dissolves dumped soil; heavy rain packs what is loose. Both bring water to the map. A termite attack waits for the rain to stop.");
            AddGlossaryEntry(l, "plants", "Plantas e folhas", "Plants and leaves",
                   "As plantas da superfície crescem sozinhas. Operárias e cortadeiras cortam pedaços e os levam aos canteiros, à sala de orvalho ou ao depósito de alimentos. Folhas também saem dos resíduos e dos pulgões.",
                   "Surface plants grow on their own. Workers and cutters cut pieces and take them to the beds, the dew room or the food storage. Leaves also come from the waste area and the aphids.");
            AddGlossaryEntry(l, "light", "Luz", "Light",
                   "A luz vem de fora: a superfície é clara e o formigueiro fica na penumbra. Cogumelos luminosos plantados nos túneis iluminam em volta.",
                   "Light comes from outside: the surface is bright and the anthill stays dim. Glowing mushrooms planted in the tunnels light up the area around them.");
            AddGlossaryEntry(l, "rubble", "Entulho", "Rubble",
                   "O que sobra de canteiros, salas e paredes que os cupins destroem. Marque o entulho com Cavar para as formigas o recolherem; com área de resíduos, ele vai para lá.",
                   "What is left of beds, rooms and walls the termites destroy. Mark the rubble with Dig so the ants clear it; with a waste area, it goes there.");

            // --- ferramentas
            AddGlossaryEntry(l, "dig", "Cavar", "Dig",
                   "Menu Ferramenta. Marque a terra que as formigas devem tirar. Botão esquerdo marca, direito desmarca. Escavadoras são as especialistas, mas outras formigas também cavam.",
                   "Tools menu. Mark the soil the ants must remove. Left button marks, right button unmarks. Diggers are the specialists, but other ants dig too.");
            AddGlossaryEntry(l, "build", "Construir", "Build",
                   "Menu Ferramenta. Marque células vazias para as formigas preencherem com terra, até acima do solo. Botão direito desmarca.",
                   "Tools menu. Mark empty cells for the ants to fill with soil, even above the ground. Right button unmarks.");
            AddGlossaryEntry(l, "glowbrush", "Cogumelo luminoso", "Glowing mushroom",
                   "Menu Ferramenta. Clique numa célula do túnel encostada na terra: uma formiga leva até lá um cogumelo dos pulgões e o planta, e ele ilumina os túneis em volta. Para tirar um cogumelo, cave-o.",
                   "Tools menu. Click a tunnel cell next to the soil: an ant brings a mushroom from the aphids and plants it, and it lights the tunnels around it. To remove a mushroom, dig it.");
            AddGlossaryEntry(l, "finger", "Dedo", "Finger",
                   "Menu Ferramenta. Clique num cupim para feri-lo; não funciona com o jogo pausado. Clicar na rainha força um ovo.",
                   "Tools menu. Click a termite to hurt it; it does not work while the game is paused. Clicking the queen forces an egg.");

            // --- ambientes
            AddGlossaryEntry(l, "queenroom", "Sala da rainha", "Queen room",
                   "Menu Ambientes. Onde a rainha fica e põe os ovos. Sem ela, a rainha não põe.",
                   "Rooms menu. Where the queen stays and lays eggs. Without it, the queen does not lay.");
            AddGlossaryEntry(l, "nutrientroom", "Sala de nutrientes", "Nutrient room",
                   "Menu Ambientes. O cultivo: as formigas montam canteiros de terra fofa, planta e água, e deles brotam os nutrientes.",
                   "Rooms menu. The farm: ants build beds of loose soil, plant and water, and nutrients sprout from them.");
            AddGlossaryEntry(l, "nursery", "Berçário", "Nursery",
                   "Menu Ambientes. Onde ovos, larvas e pupas se desenvolvem. Cada célula livre dele é uma vaga de cria; a rainha precisa de berçário para pôr ovos.",
                   "Rooms menu. Where eggs, larvae and pupae develop. Each free cell is a brood slot; the queen needs a nursery to lay eggs.");
            AddGlossaryEntry(l, "waste", "Resíduos", "Waste",
                   "Menu Ambientes, liberado ao repelir o primeiro ataque de cupins. As formigas deixam ali entulho e restos, que se transformam. Com o pincel escolhido, o submenu RESÍDUOS define o modo da área: restos em folhas, folhas em água, ou terra em areia e água.",
                   "Rooms menu, unlocked when you repel the first termite attack. Ants leave rubble and scraps there, which turn into something else. With the brush chosen, the WASTE submenu sets the area's mode: scraps to leaves, leaves to water, or soil to sand and water.");
            AddGlossaryEntry(l, "dew", "Gotas de orvalho", "Dew drops",
                   "Menu Ambientes. As formigas põem terra fofa e folhas no teto da sala, e dela pingam gotas de água para regar os canteiros.",
                   "Rooms menu. Ants place loose soil and leaves on the room's ceiling, and water drops fall from it to water the beds.");
            AddGlossaryEntry(l, "farm", "Fazenda de pulgões", "Aphid farm",
                   "Menu Ambientes, liberada pela câmara Fazenda de pulgões em Evolução. Os pulgões nascem nas baias, cada uma com um cocho de areia e um de água. Ordenhados pela fazendeira, comem e dão areia, folhas, água e às vezes cogumelos luminosos.",
                   "Rooms menu, unlocked by the Aphid farm chamber in Evolution. Aphids are born in stalls, each with a sand trough and a water trough. Milked by the farmer, they eat and give sand, leaves, water and sometimes glowing mushrooms.");
            AddGlossaryEntry(l, "depots", "Depósitos", "Storage",
                   "Botão DEPÓSITO, no fim do menu Ambientes. Há depósitos de alimentos, de construção, de água, de areia e de cogumelos. O que as formigas trazem fica guardado ali e é usado de lá primeiro; as carregadoras os enchem e entregam o que está neles.",
                   "STORAGE button, at the end of the Rooms menu. There are food, building, water, sand and mushroom storages. What the ants bring is kept there and used from there first; carriers fill them and deliver from them.");
            AddGlossaryEntry(l, "area", "Limite de área", "Area limit",
                   "Cada ambiente tem um limite de células que dá para pintar. A barra embaixo do botão mostra o que ainda resta: laranja quando está acabando, vermelha quando acabou. Melhorias em Evolução aumentam o limite.",
                   "Each room has a limit of cells you can paint. The bar under the button shows what is left: orange when running low, red when used up. Upgrades in Evolution raise the limit.");

            // --- formigas
            AddGlossaryEntry(l, "queen", "Rainha", "Queen",
                   "Põe os ovos de onde nascem todas as formigas. Precisa da própria sala, de berçário, de vaga e de nutrientes; o cartão Rainha diz o que está faltando. Se ela morrer, a partida acaba.",
                   "Lays the eggs every ant hatches from. She needs her own room, a nursery, a free slot and nutrients; the Queen card says what is missing. If she dies, the game is over.");
            AddGlossaryEntry(l, "brood", "Ovo, larva e pupa", "Egg, larva and pupa",
                   "As crias. A rainha põe o ovo, uma formiga o leva ao berçário, e ele passa a larva, a pupa e por fim a formiga adulta. Cuidadoras cuidam delas no caminho.",
                   "The brood. The queen lays the egg, an ant carries it to the nursery, and it becomes a larva, a pupa and finally an adult ant. Nurses look after them along the way.");
            AddGlossaryEntry(l, "worker", "Operária", "Worker",
                   "Faz de tudo um pouco e corta plantas quando está sem outra tarefa.",
                   "Does a bit of everything and cuts plants when it has no other task.");
            AddGlossaryEntry(l, "carrier", "Carregadora", "Carrier",
                   "A mais rápida: leva o dobro por viagem, enche os depósitos e entrega o que está neles.",
                   "The fastest: hauls twice as much per trip, fills the storages and delivers from them.");
            AddGlossaryEntry(l, "cutter", "Cortadeira", "Cutter",
                   "Corta as plantas da superfície na metade do tempo e as leva à sala de nutrientes.",
                   "Cuts surface plants in half the time and takes them to the nutrient room.");
            AddGlossaryEntry(l, "digger", "Escavadora", "Digger",
                   "Especialista em cavar a terra marcada e despejá-la fora.",
                   "Specialist in digging the marked soil and dumping it outside.");
            AddGlossaryEntry(l, "gardener", "Jardineira", "Gardener",
                   "Leva terra e água aos canteiros e cuida do cultivo.",
                   "Brings soil and water to the beds and tends the farm.");
            AddGlossaryEntry(l, "nurse", "Cuidadora", "Nurse",
                   "Cuida das crias no berçário e leva os ovos para lá.",
                   "Looks after the brood in the nursery and carries eggs there.");
            AddGlossaryEntry(l, "soldier", "Soldado", "Soldier",
                   "Combate os cupins e recolhe os corpos. Liberado ao repelir o primeiro ataque.",
                   "Fights the termites and clears the bodies. Unlocked when you repel the first attack.");
            AddGlossaryEntry(l, "spitter", "Cuspideira", "Spitter",
                   "Cospe ácido nos cupins de longe, sem chegar perto da mordida deles.",
                   "Spits acid at termites from afar, out of reach of their bite.");
            AddGlossaryEntry(l, "midden", "Lixeira", "Midden worker",
                   "Tira os corpos dos cupins, leva os itens soltos e abastece a área de resíduos.",
                   "Clears termite bodies, hauls loose items and supplies the waste area.");
            AddGlossaryEntry(l, "farmer", "Fazendeira", "Farmer",
                   "Enche os cochos dos pulgões com areia e água e os ordenha na fazenda.",
                   "Fills the aphids' troughs with sand and water and milks them on the farm.");

            // --- menu da rainha
            AddGlossaryEntry(l, "mode", "Modo de postura", "Laying mode",
                   "Automático: a rainha escolhe o tipo do próximo ovo para equilibrar a colônia. Tipo específico: ela só põe o tipo escolhido na lista. Pausado: ela não põe ovos novos, mas as crias seguem crescendo.",
                   "Automatic: the queen picks the next egg's type to balance the colony. Specific type: she lays only the type chosen in the list. Paused: she lays no new eggs, but the brood keeps growing.");
            AddGlossaryEntry(l, "stance", "Postura diante dos cupins", "Stance against termites",
                   "Defensiva: a rainha só se defende de quem a morde. Ofensiva: com cupim dentro do formigueiro, ela sai da sala atrás dele e depois volta.",
                   "Defensive: the queen only fights back against whoever bites her. Offensive: with a termite inside the anthill, she leaves her room to chase it and then comes back.");

            // --- evolução
            AddGlossaryEntry(l, "tree", "Árvore de evolução", "Evolution tree",
                   "Botão EVOLUÇÃO, no menu Colônia. Gaste nutrientes em novas classes de formiga, câmaras e melhorias. Arraste para mover, role para aproximar e clique num nó para ver efeito, custo e requisitos.",
                   "EVOLUTION button, in the Colony menu. Spend nutrients on new ant classes, chambers and upgrades. Drag to move, scroll to zoom and click a node to see its effect, cost and requirements.");
            AddGlossaryEntry(l, "unlocks", "Bloqueado e liberado", "Locked and unlocked",
                   "Botões apagados ainda não foram liberados; a dica do botão diz o que falta. Classes e câmaras vêm da Evolução; o Soldado e a área de resíduos, do primeiro ataque de cupins repelido.",
                   "Greyed-out buttons are not unlocked yet; the button's tip says what is missing. Classes and chambers come from Evolution; the Soldier and the waste area, from the first termite attack repelled.");

            // --- ameaças
            AddGlossaryEntry(l, "raids", "Ataques de cupins", "Termite attacks",
                   "Começam quando a colônia chega a 15 formigas adultas e voltam de tempos em tempos. Um alerta no alto da tela avisa alguns segundos antes; cada cupim ganha um círculo vermelho, ou uma seta na borda quando está fora da vista.",
                   "They begin once the colony reaches 15 adult ants and come back from time to time. An alert at the top of the screen warns a few seconds before; each termite gets a red circle, or an arrow on the edge when it is out of view.");
            AddGlossaryEntry(l, "raidtargets", "O que os cupins atacam", "What termites attack",
                   "Em ordem: as crias, os pulgões, os canteiros, o que está dentro do berçário, da sala de nutrientes e dos resíduos, e as paredes em volta deles. O alerta mostra o objetivo atual. Soldados, cuspideiras, a rainha e o Dedo os combatem.",
                   "In order: the brood, the aphids, the beds, what is inside the nursery, the nutrient room and the waste area, and the walls around them. The alert shows the current goal. Soldiers, spitters, the queen and the Finger fight them.");
            AddGlossaryEntry(l, "blackaphid", "Pulgão preto", "Black aphid",
                   "Um pulgão mordido por um cupim fica preto: sai da fazenda e luta do lado dos cupins até morrer. As formigas o caçam como a um cupim.",
                   "An aphid bitten by a termite turns black: it leaves the farm and fights on the termites' side until it dies. The ants hunt it like a termite.");

            // --- interface
            AddGlossaryEntry(l, "cards", "Cartões do topo", "Top cards",
                   "Nutrientes: saldo e quanto foi consumido. Formigas: adultas sobre vagas, e ovos, larvas e pupas. Rainha: o estado dela e o que falta para pôr ovos. Pare o mouse sobre um cartão para ver os detalhes.",
                   "Nutrients: balance and how much was consumed. Ants: adults over slots, and eggs, larvae and pupae. Queen: her state and what she needs to lay eggs. Rest the mouse over a card for details.");
            AddGlossaryEntry(l, "colonypanel", "Painel da colônia", "Colony panel",
                   "Segundo botão da barra inferior. Estatísticas das formigas e da produção, e quais trabalhos ficam ativos: dá para parar uma seção ou limitar quantas formigas trabalham nela.",
                   "Second button of the bottom bar. Ant and production statistics, and which jobs stay active: you can stop a section or limit how many ants work in it.");
            AddGlossaryEntry(l, "bell", "Sino", "Bell",
                   "Chama as formigas de volta ao formigueiro: elas terminam o que carregam e param. Toque de novo para mandá-las de volta ao trabalho.",
                   "Calls the ants back to the anthill: they finish what they carry and stop. Ring again to send them back to work.");
            AddGlossaryEntry(l, "showrooms", "Mostrar ambientes", "Show rooms",
                   "Esconde ou mostra a pintura das salas no terreno. As salas continuam funcionando; é só para ver melhor a terra e as formigas.",
                   "Hides or shows the room paint on the ground. The rooms keep working; it is just to see the soil and the ants better.");
            AddGlossaryEntry(l, "tips", "Dicas e menu", "Tips and menu",
                   "Pare o mouse sobre qualquer botão ou cartão para ver o que ele faz e o que falta. O menu Colônia, à esquerda, se recolhe e volta quando o mouse chega perto. O ESC abre o menu do jogo e fecha janelas.",
                   "Rest the mouse over any button or card to see what it does and what is missing. The Colony menu, on the left, tucks away and comes back when the mouse gets close. ESC opens the game menu and closes windows.");
        }

        /// <summary>Título e texto de uma entrada do glossário (chaves de <c>GlossaryContent</c>).</summary>
        static void AddGlossaryEntry(List<Entry> l, string id, string titlePt, string titleEn, string textPt, string textEn)
        {
            Add(l, "glossary." + id + ".title", titlePt, titleEn);
            Add(l, "glossary." + id, textPt, textEn);
        }
    }
}
