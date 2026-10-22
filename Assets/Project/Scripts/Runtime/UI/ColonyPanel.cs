using System;
using System.Text;
using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.Texts;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Fabula.Incrementant.UI
{
    /// <summary>
    /// Painel da colônia (botão à esquerda do sino), com duas abas: Estatísticas (formigas por papel, seção de trabalho e
    /// lugar, e a produção por segundo) e Configurações (ligar, parar e limitar cada seção de trabalho, por "-"/"+" ou slider, e o comportamento da
    /// rainha: botar ovos ou trabalhar como uma das classes liberadas). Só lê e muda <see cref="AntColony"/>; quem abre e fecha é o <see cref="AntFarmHud"/>. A hierarquia é
    /// montada por <c>Incrementant/Setup/Montar HUD da partida</c>.
    /// </summary>
    public sealed class ColonyPanel : MonoBehaviour
    {
        [SerializeField] AntFarmGame _game;
        [SerializeField] Button _close;

        [Header("Abas")]
        [SerializeField] Button _statsTab;
        [SerializeField] Image _statsTabImage;
        [SerializeField] Button _settingsTab;
        [SerializeField] Image _settingsTabImage;
        [SerializeField] GameObject _statsPage;
        [SerializeField] GameObject _settingsPage;
        [SerializeField] Sprite _tabActive;
        [SerializeField] Sprite _tabInactive;

        [Header("Estatísticas (uma coluna de texto cada)")]
        [SerializeField] Text _antsColumn;
        [SerializeField] Text _workColumn;
        [SerializeField] Text _placesColumn;

        [Header("Configurações: uma linha por seção, na ordem de WorkPolicy.All")]
        [SerializeField] Button[] _toggles;
        [SerializeField] Image[] _toggleImages;
        [SerializeField] Text[] _sectionLabels;
        [SerializeField] Text[] _sectionNow;
        [SerializeField] Button[] _minus;
        [SerializeField] Button[] _plus;
        [Tooltip("De 0 ao total de formigas; a última posição (total + 1) é \"sem limite\".")]
        [SerializeField] Slider[] _limitSliders;
        [SerializeField] Text[] _limits;
        [SerializeField] Button _reset;
        [SerializeField] Sprite _toggleOn;
        [SerializeField] Sprite _toggleOff;

        [Header("Comportamento da rainha: seletor e lista que abre para cima, uma opção por QueenJobs.All")]
        [SerializeField] Button _queenJobToggle;
        [SerializeField] Text _queenJobLabel;
        [SerializeField] Image _queenJobIcon;
        [SerializeField] RectTransform _queenJobList;
        [Tooltip("Fundo transparente atrás da lista: clicar fora dela a fecha.")]
        [SerializeField] Button _queenJobBlocker;
        [SerializeField] Button[] _queenJobs;
        [SerializeField] Image[] _queenJobImages;
        [SerializeField] Text[] _queenJobLabels;
        [SerializeField] Image[] _queenJobIcons;
        [SerializeField] Text _queenJobState;
        [SerializeField] Sprite _buttonIdle;
        [SerializeField] Sprite _buttonActive;

        const float RefreshSeconds = 0.25f;
        // Largura das colunas em letras (a fonte é monoespaçada: 16 px por letra).
        const int LabelWidth = 21;
        const int ValueWidth = 6;

        static readonly Color Ink = new Color(0.23f, 0.13f, 0.07f);
        static readonly Color InkOff = new Color(0.36f, 0.33f, 0.30f);

        readonly ColonyCensus _census = new ColonyCensus();
        readonly StringBuilder _text = new StringBuilder(512);
        UnityAction[] _toggleHandlers, _minusHandlers, _plusHandlers, _jobHandlers;
        UnityAction<float>[] _sliderHandlers;
        bool _valid;
        // RefreshSettings mexe na faixa dos sliders, e mudar maxValue dispara onValueChanged: ignorar enquanto sincroniza.
        bool _syncingSliders;
        bool _settingsOpen;
        float _nextRefresh;

        int Sections => WorkPolicy.Count;

        /// <summary>Opções da lista do comportamento da rainha, uma por valor de <see cref="QueenJobs.All"/>.</summary>
        public static int QueenJobCount => QueenJobs.All.Length;

        /// <summary>Altura de cada opção da lista (botão de 42 px e folga) e a margem em volta; o setup do HUD usa as mesmas medidas.</summary>
        public const float QueenOptionStep = 46f, QueenOptionHeight = 42f, QueenListPadding = 8f;

        void Awake()
        {
            int n = Sections;
            _valid = _game != null && _close != null && _statsTab != null && _settingsTab != null && _statsTabImage != null &&
                     _settingsTabImage != null && _statsPage != null && _settingsPage != null && _antsColumn != null &&
                     _workColumn != null && _placesColumn != null && _reset != null && _queenJobState != null &&
                     _queenJobToggle != null && _queenJobLabel != null && _queenJobIcon != null && _queenJobList != null && _queenJobBlocker != null &&
                     Has(_toggles, n) && Has(_toggleImages, n) && Has(_sectionLabels, n) && Has(_sectionNow, n) &&
                     Has(_minus, n) && Has(_plus, n) && Has(_limitSliders, n) && Has(_limits, n) && Has(_queenJobs, QueenJobCount) && Has(_queenJobImages, QueenJobCount) && Has(_queenJobLabels, QueenJobCount) &&
                     Has(_queenJobIcons, QueenJobCount);
            if (!_valid)
            {
                Debug.LogError($"{name}: ColonyPanel com referências faltando (rode Incrementant/Setup/Montar HUD da partida).", this);
                enabled = false;
                return;
            }
            _toggleHandlers = new UnityAction[n];
            _minusHandlers = new UnityAction[n];
            _plusHandlers = new UnityAction[n];
            _sliderHandlers = new UnityAction<float>[n];
            for (int i = 0; i < n; i++)
            {
                WorkSection section = WorkPolicy.All[i];
                _toggleHandlers[i] = () => Toggle(section);
                _minusHandlers[i] = () => StepLimit(section, -1);
                _plusHandlers[i] = () => StepLimit(section, +1);
                _sliderHandlers[i] = value => SlideLimit(section, value);
            }
            _jobHandlers = new UnityAction[_queenJobs.Length];
            for (int i = 0; i < _jobHandlers.Length; i++)
            {
                QueenJob job = QueenJobs.All[i];
                _jobHandlers[i] = () => SetQueenJob(job);
            }
        }

        static bool Has<T>(T[] array, int length) where T : class
        {
            if (array == null || array.Length != length)
                return false;
            foreach (T item in array)
            {
                if (item == null)
                    return false;
            }
            return true;
        }

        void OnEnable()
        {
            if (!_valid)
                return;
            _close.onClick.AddListener(Close);
            _statsTab.onClick.AddListener(ShowStats);
            _settingsTab.onClick.AddListener(ShowSettings);
            _reset.onClick.AddListener(ResetPolicy);
            for (int i = 0; i < Sections; i++)
            {
                _toggles[i].onClick.AddListener(_toggleHandlers[i]);
                _minus[i].onClick.AddListener(_minusHandlers[i]);
                _plus[i].onClick.AddListener(_plusHandlers[i]);
                _limitSliders[i].onValueChanged.AddListener(_sliderHandlers[i]);
            }
            for (int i = 0; i < _queenJobs.Length; i++)
                _queenJobs[i].onClick.AddListener(_jobHandlers[i]);
            _queenJobToggle.onClick.AddListener(ToggleQueenJobList);
            _queenJobBlocker.onClick.AddListener(CloseQueenJobList);
            ShowPage(_settingsOpen);
        }

        void OnDisable()
        {
            if (!_valid)
                return;
            _close.onClick.RemoveListener(Close);
            _statsTab.onClick.RemoveListener(ShowStats);
            _settingsTab.onClick.RemoveListener(ShowSettings);
            _reset.onClick.RemoveListener(ResetPolicy);
            for (int i = 0; i < Sections; i++)
            {
                _toggles[i].onClick.RemoveListener(_toggleHandlers[i]);
                _minus[i].onClick.RemoveListener(_minusHandlers[i]);
                _plus[i].onClick.RemoveListener(_plusHandlers[i]);
                _limitSliders[i].onValueChanged.RemoveListener(_sliderHandlers[i]);
            }
            for (int i = 0; i < _queenJobs.Length; i++)
                _queenJobs[i].onClick.RemoveListener(_jobHandlers[i]);
            _queenJobToggle.onClick.RemoveListener(ToggleQueenJobList);
            _queenJobBlocker.onClick.RemoveListener(CloseQueenJobList);
        }

        /// <summary>Dicas dos controles da aba Configurações (o <see cref="HudTooltips"/> é do HUD).</summary>
        public void AttachTooltips(HudTooltips tooltips)
        {
            if (!_valid)
                return;
            for (int i = 0; i < Sections; i++)
            {
                WorkSection section = WorkPolicy.All[i];
                tooltips.Attach(_toggles[i], () => Loc.T(SectionTipKey(section)) + "\n" + Loc.T("tip.colony.toggle"));
                tooltips.Attach(_minus[i], () => Loc.T("tip.colony.minus"));
                tooltips.Attach(_plus[i], () => Loc.T("tip.colony.plus"));
                tooltips.Attach(_limitSliders[i], () => Loc.T("tip.colony.slider"));
            }
            tooltips.Attach(_queenJobToggle, () => Loc.T("tip.colony.queen.choose"));
            for (int i = 0; i < _queenJobs.Length; i++)
            {
                QueenJob job = QueenJobs.All[i];
                tooltips.Attach(_queenJobs[i], () => QueenJobTip(job));
            }
            tooltips.Attach(_reset, () => Loc.T("tip.colony.reset"));
        }

        public void Close() => gameObject.SetActive(false);

        void ShowStats() => ShowPage(false);

        void ShowSettings() => ShowPage(true);

        void ShowPage(bool settings)
        {
            CloseQueenJobList();
            _settingsOpen = settings;
            _statsPage.SetActive(!settings);
            _settingsPage.SetActive(settings);
            if (_tabActive != null && _tabInactive != null)
            {
                _statsTabImage.sprite = settings ? _tabInactive : _tabActive;
                _settingsTabImage.sprite = settings ? _tabActive : _tabInactive;
            }
            _nextRefresh = 0f;
        }

        void Toggle(WorkSection section)
        {
            AntColony colony = _game.Colony;
            if (colony == null)
                return;
            colony.Work.SetEnabled(section, !colony.Work.IsEnabled(section));
            _nextRefresh = 0f;
        }

        /// <summary>
        /// Sem limite, o primeiro "-" fixa o limite em quem já está na seção (os cliques seguintes tiram uma por vez); o "+"
        /// passando do total de formigas volta a "sem limite".
        /// </summary>
        void StepLimit(WorkSection section, int step)
        {
            AntColony colony = _game.Colony;
            if (colony == null)
                return;
            int limit = colony.Work.LimitOf(section);
            int total = colony.Ants.Count;
            if (limit == WorkPolicy.Unlimited)
            {
                if (step > 0)
                    return;
                limit = Mathf.Min(colony.WorkersIn(section), Mathf.Max(0, total - 1));
            }
            else
            {
                limit += step;
                if (limit < 0)
                    limit = 0;
                else if (step > 0 && limit > total)
                    limit = WorkPolicy.Unlimited;
            }
            colony.Work.SetLimit(section, limit);
            _nextRefresh = 0f;
        }

        /// <summary>Mesma escala do "-"/"+": de 0 ao total de formigas é o limite; o fim do trilho (total + 1) é "sem limite".</summary>
        void SlideLimit(WorkSection section, float value)
        {
            AntColony colony = _game.Colony;
            if (_syncingSliders || colony == null)
                return;
            int limit = Mathf.RoundToInt(value);
            colony.Work.SetLimit(section, limit > colony.Ants.Count ? WorkPolicy.Unlimited : Mathf.Max(0, limit));
            _nextRefresh = 0f;
        }

        void ResetPolicy()
        {
            AntColony colony = _game.Colony;
            if (colony == null)
                return;
            colony.Work.Reset();
            _nextRefresh = 0f;
        }

        void SetQueenJob(QueenJob job)
        {
            AntColony colony = _game.Colony;
            if (colony == null)
                return;
            colony.QueenJob = job;
            CloseQueenJobList();
            _nextRefresh = 0f;
        }

        void ToggleQueenJobList()
        {
            bool open = !_queenJobList.gameObject.activeSelf;
            AntColony colony = _game.Colony;
            if (open && colony != null)
                LayoutQueenJobList(colony);
            _queenJobList.gameObject.SetActive(open);
            _queenJobBlocker.gameObject.SetActive(open);
            _nextRefresh = 0f;
        }

        void CloseQueenJobList()
        {
            _queenJobList.gameObject.SetActive(false);
            _queenJobBlocker.gameObject.SetActive(false);
        }

        /// <summary>
        /// Só as opções oferecidas, empilhadas de cima para baixo. Roda com a lista fechada, ao abrir: o idle do
        /// <see cref="Juice.ButtonJuice"/> soma deslocamentos ao anchoredPosition, e fechado ele está zerado.
        /// </summary>
        void LayoutQueenJobList(AntColony colony)
        {
            int shown = 0;
            for (int i = 0; i < _queenJobs.Length; i++)
            {
                bool offered = Offers(colony, QueenJobs.All[i]);
                _queenJobs[i].gameObject.SetActive(offered);
                if (!offered)
                    continue;
                var rect = (RectTransform)_queenJobs[i].transform;
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -(QueenListPadding + shown * QueenOptionStep));
                shown++;
            }
            _queenJobList.sizeDelta = new Vector2(_queenJobList.sizeDelta.x, QueenListHeight(shown));
        }

        /// <summary>A opção aparece na lista: o ninho, as classes já liberadas e a que a rainha já faz.</summary>
        static bool Offers(AntColony colony, QueenJob job) =>
            !QueenJobs.TryGetRole(job, out AntRole role) || colony.Tree.IsRoleUnlocked(role) || job == colony.QueenJob;

        void Update()
        {
            AntColony colony = _game.Colony;
            if (colony == null || Time.unscaledTime < _nextRefresh)
                return;
            _nextRefresh = Time.unscaledTime + RefreshSeconds;
            colony.TakeCensus(_census);
            if (_settingsOpen)
                RefreshSettings(colony);
            else
                RefreshStats(colony);
        }

        // ---------------------------------------------------------------- estatísticas

        void RefreshStats(AntColony colony)
        {
            _text.Clear();
            Title(Loc.T("colony.stats.ants"));
            Line(Loc.T("colony.stats.adults"), $"{colony.Ants.Count}/{colony.AdultCapacity}");
            Line(Loc.T("colony.stats.brood"), colony.Brood.Count.ToString());
            Line(Loc.T("colony.stats.idle"), _census.Idle.ToString());
            Line(Loc.T("colony.stats.fighting"), _census.Fighting.ToString());
            Line(Loc.T("colony.stats.waterStock"), _census.WaterStocking.ToString());
            _text.Append('\n');
            Title(Loc.T("colony.stats.roles"));
            foreach (AntRole role in AntRoles.All)
            {
                if (colony.Tree.IsRoleUnlocked(role) || _census.Roles[(int)role] > 0)
                    Line(Loc.T(AntFarmHud.RoleKey(role)), _census.Roles[(int)role].ToString());
            }
            _antsColumn.text = _text.ToString();

            _text.Clear();
            Title(Loc.T("colony.stats.work"));
            for (int i = 0; i < Sections; i++)
            {
                WorkSection section = WorkPolicy.All[i];
                string value = colony.Work.IsEnabled(section) ? _census.Sections[i].ToString() : Loc.T("colony.stopped");
                Line(Loc.T(SectionKey(section)), value);
            }
            _text.Append('\n');
            Title(Loc.T("colony.stats.queen"));
            _text.Append(QueenJobStats(colony.QueenJob)).Append('\n');
            _text.Append(Loc.T(PlaceKey(colony.QueenPlace))).Append('\n');
            if (colony.Recalled)
                _text.Append('\n').Append(Loc.T("colony.stats.recalled")).Append('\n');
            _workColumn.text = _text.ToString();

            _text.Clear();
            Title(Loc.T("colony.stats.places"));
            foreach (ColonyPlace place in Places)
                Line(Loc.T(PlaceKey(place)), _census.Places[(int)place].ToString());
            _text.Append('\n');
            Title(Loc.T("colony.stats.rates"));
            Line(Loc.T("colony.rate.toqueen"), Rate(colony, ColonyRates.Counter.NutrientsToQueen));
            Line(Loc.T("colony.rate.grown"), Rate(colony, ColonyRates.Counter.NutrientsGrown));
            Line(Loc.T("colony.rate.harvest"), Rate(colony, ColonyRates.Counter.PlantsHarvested));
            Line(Loc.T("colony.rate.soil"), Rate(colony, ColonyRates.Counter.SoilDumped));
            _placesColumn.text = _text.ToString();
        }

        static readonly ColonyPlace[] Places = (ColonyPlace[])Enum.GetValues(typeof(ColonyPlace));

        static string Rate(AntColony colony, ColonyRates.Counter counter) =>
            colony.Rates.PerSecond(counter).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);

        void Title(string title) => _text.Append(title).Append('\n');

        /// <summary>Rótulo à esquerda e valor alinhado à direita na mesma linha (fonte monoespaçada).</summary>
        void Line(string label, string value)
        {
            if (label.Length > LabelWidth)
                label = label.Substring(0, LabelWidth);
            _text.Append(label.PadRight(LabelWidth)).Append(value.PadLeft(ValueWidth)).Append('\n');
        }

        // ---------------------------------------------------------------- configurações

        void RefreshSettings(AntColony colony)
        {
            WorkPolicy work = colony.Work;
            int total = colony.Ants.Count;
            _syncingSliders = true;
            for (int i = 0; i < Sections; i++)
            {
                WorkSection section = WorkPolicy.All[i];
                bool on = work.IsEnabled(section);
                _toggleImages[i].sprite = on ? _toggleOn : _toggleOff;
                _sectionLabels[i].text = Loc.T(SectionKey(section)).ToUpperInvariant();
                _sectionLabels[i].color = on ? Ink : InkOff;
                _sectionNow[i].text = on ? _census.Sections[i].ToString() : Loc.T("colony.stopped");
                _sectionNow[i].color = on ? Ink : InkOff;
                int limit = work.LimitOf(section);
                _limits[i].text = limit == WorkPolicy.Unlimited ? Loc.T("colony.limit.none") : Loc.T("colony.limit.max", limit);
                _limits[i].color = on ? Ink : InkOff;
                Slider slider = _limitSliders[i];
                slider.minValue = 0f;
                slider.maxValue = total + 1;
                slider.SetValueWithoutNotify(limit == WorkPolicy.Unlimited ? total + 1 : Mathf.Min(limit, total));
            }
            _syncingSliders = false;
            RefreshQueenJob(colony);
        }

        /// <summary>Seletor com a escolha de agora e, na lista, o destaque e os rótulos de cada opção.</summary>
        void RefreshQueenJob(AntColony colony)
        {
            int current = Array.IndexOf(QueenJobs.All, colony.QueenJob);
            if (current >= 0)
            {
                _queenJobLabel.text = QueenJobLabel(colony.QueenJob);
                _queenJobIcon.sprite = _queenJobIcons[current].sprite;
                _queenJobIcon.color = _queenJobIcons[current].color;
            }
            for (int i = 0; i < _queenJobs.Length; i++)
            {
                QueenJob job = QueenJobs.All[i];
                _queenJobImages[i].sprite = job == colony.QueenJob ? _buttonActive : _buttonIdle;
                _queenJobLabels[i].text = QueenJobLabel(job);
            }
            string place = Loc.T(PlaceKey(colony.QueenPlace));
            _queenJobState.text = QueenJobs.TryGetRole(colony.QueenJob, out AntRole role)
                ? Loc.T("colony.queen.state.role", RoleName(role), place)
                : Loc.T("colony.queen.state.nest", place);
        }

        // ---------------------------------------------------------------- chaves (switch: o teste de chaves lê os literais)

        public static string SectionKey(WorkSection section)
        {
            switch (section)
            {
                case WorkSection.Dig: return "colony.section.dig";
                case WorkSection.Build: return "colony.section.build";
                case WorkSection.Harvest: return "colony.section.harvest";
                case WorkSection.QueenFeed: return "colony.section.queenfeed";
                case WorkSection.Nursery: return "colony.section.nursery";
                case WorkSection.Garden: return "colony.section.garden";
                case WorkSection.Dew: return "colony.section.dew";
                default: return "colony.section.waste";
            }
        }

        static string SectionTipKey(WorkSection section)
        {
            switch (section)
            {
                case WorkSection.Dig: return "tip.colony.section.dig";
                case WorkSection.Build: return "tip.colony.section.build";
                case WorkSection.Harvest: return "tip.colony.section.harvest";
                case WorkSection.QueenFeed: return "tip.colony.section.queenfeed";
                case WorkSection.Nursery: return "tip.colony.section.nursery";
                case WorkSection.Garden: return "tip.colony.section.garden";
                case WorkSection.Dew: return "tip.colony.section.dew";
                default: return "tip.colony.section.waste";
            }
        }

        /// <summary>Altura da lista com <paramref name="options"/> opções.</summary>
        public static float QueenListHeight(int options) =>
            2 * QueenListPadding + Mathf.Max(0, options - 1) * QueenOptionStep + QueenOptionHeight;

        /// <summary>Rótulo da opção: BOTAR OVOS ou o nome da classe, em maiúsculas.</summary>
        static string QueenJobLabel(QueenJob job) =>
            QueenJobs.TryGetRole(job, out AntRole role) ? AntFarmHud.RoleLabel(role) : Loc.T("colony.queen.nest");

        static string QueenJobStats(QueenJob job) =>
            QueenJobs.TryGetRole(job, out AntRole role) ? Loc.T("colony.queen.working", RoleName(role)) : Loc.T("colony.queen.nesting");

        static string QueenJobTip(QueenJob job) =>
            QueenJobs.TryGetRole(job, out AntRole role) ? Loc.T("tip.colony.queen.role", RoleName(role)) : Loc.T("tip.colony.queen.nest");

        static string RoleName(AntRole role) => Loc.T(AntFarmHud.RoleKey(role)).ToLowerInvariant();

        static string PlaceKey(ColonyPlace place)
        {
            switch (place)
            {
                case ColonyPlace.Outdoor: return "colony.place.outdoor";
                case ColonyPlace.QueenRoom: return "colony.place.queenroom";
                case ColonyPlace.Nursery: return "colony.place.nursery";
                case ColonyPlace.NutrientRoom: return "colony.place.nutrientroom";
                case ColonyPlace.DewRoom: return "colony.place.dewroom";
                case ColonyPlace.WasteRoom: return "colony.place.wasteroom";
                case ColonyPlace.FarmRoom: return "colony.place.farmroom";
                default: return "colony.place.tunnels";
            }
        }
    }
}
