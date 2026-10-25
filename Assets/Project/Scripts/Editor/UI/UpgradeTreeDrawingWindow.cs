using System.Collections.Generic;
using Fabula.Incrementant.UI;
using Fabula.Incrementant.Upgrades;
using UnityEditor;
using UnityEngine;

namespace Fabula.Incrementant.EditorTools.UI
{
    /// <summary>
    /// Redesenho à mão da tela Evolução (documento 48): mostra o formigueiro como no jogo (colunas, faixas dos níveis, galeria,
    /// salões, túneis pelos encontros) e deixa arrastar nós e encontros. Arrastar fixa o item (o gerador não mexe mais nele);
    /// o botão direito fixa/desfixa, põe uma dobra num túnel ou remove um encontro. Salva no <see cref="UpgradeTreeDrawing"/>
    /// ao soltar; <b>Aplicar na cena</b> remonta o HUD (<c>Incrementant/Setup/Montar HUD da partida</c>) com o desenho.
    /// </summary>
    public sealed class UpgradeTreeDrawingWindow : EditorWindow
    {
        const float NodeRadius = 40f, JunctionRadius = 30f, TunnelPick = 10f;
        const float MinZoom = 0.03f, MaxZoom = 1.5f;
        static readonly Color Background = new Color(0.10f, 0.07f, 0.05f);
        static readonly Color TierLine = new Color(0.45f, 0.33f, 0.20f, 0.8f);
        static readonly Color CrossColor = new Color(0.85f, 0.82f, 0.75f, 0.9f);
        static readonly Color SelectedColor = new Color(1f, 0.85f, 0.2f);
        static readonly Color WarningColor = new Color(1f, 0.3f, 0.25f);

        enum Kind { None, Node, Junction }

        UpgradeTreeDrawing _drawing;
        UpgradeLayout _layout;
        List<UpgradeDef> _defs;
        readonly List<string> _warnings = new List<string>();
        readonly HashSet<string> _flagged = new HashSet<string>();
        Vector2 _pan;
        float _zoom = 0.12f;
        Rect _canvas;
        Kind _dragKind, _selKind;
        string _dragId, _selId;
        bool _panning, _framed;
        Vector2 _scroll;

        [MenuItem("Incrementant/Árvore/Editor de desenho")]
        static void Open() => GetWindow<UpgradeTreeDrawingWindow>("Desenho da árvore");

        [MenuItem("Incrementant/Árvore/Gerar desenho (nós automáticos)")]
        public static void GenerateAutomatic() => Generate(true);

        public static UpgradeTreeDrawing LoadOrCreate()
        {
            var drawing = AssetDatabase.LoadAssetAtPath<UpgradeTreeDrawing>(UpgradeTreeDrawing.AssetPath);
            if (drawing != null)
                return drawing;
            drawing = CreateInstance<UpgradeTreeDrawing>();
            AssetDatabase.CreateAsset(drawing, UpgradeTreeDrawing.AssetPath);
            return drawing;
        }

        static void Generate(bool keepPinned)
        {
            UpgradeTreeDrawing drawing = LoadOrCreate();
            Undo.RecordObject(drawing, "Gerar desenho da árvore");
            UpgradeTreeGenerator.Generate(drawing, keepPinned);
            Save(drawing);
            Debug.Log($"[UpgradeTreeDrawing] Desenho gerado ({(keepPinned ? "fixados mantidos" : "tudo refeito")}): " +
                      $"{drawing.Nodes.Count} nós, {drawing.Junctions.Count} encontros.", drawing);
            foreach (UpgradeTreeDrawingWindow window in Resources.FindObjectsOfTypeAll<UpgradeTreeDrawingWindow>())
                window.Rebuild();
        }

        static void Save(UpgradeTreeDrawing drawing)
        {
            EditorUtility.SetDirty(drawing);
            AssetDatabase.SaveAssetIfDirty(drawing);
        }

        void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
            Rebuild();
        }

        void OnDisable() => Undo.undoRedoPerformed -= OnUndoRedo;

        // O desfazer volta o asset na memória; grava para o disco acompanhar.
        void OnUndoRedo()
        {
            if (_drawing != null && EditorUtility.IsDirty(_drawing))
                Save(_drawing);
            Rebuild();
        }

        void Rebuild()
        {
            _drawing = AssetDatabase.LoadAssetAtPath<UpgradeTreeDrawing>(UpgradeTreeDrawing.AssetPath);
            _defs = SkillTreeScreen.NodesOf();
            _layout = new UpgradeLayout(_drawing);
            CheckWarnings();
            Repaint();
        }

        /// <summary>Os mesmos limites dos testes: nó no desenho, dentro da caixa do domínio no nível e a 125 px dos outros.</summary>
        void CheckWarnings()
        {
            _warnings.Clear();
            _flagged.Clear();
            if (_drawing == null)
                return;
            foreach (string id in _layout.Missing)
            {
                _warnings.Add($"{id} sem posição no desenho (fica na grade).");
                _flagged.Add(id);
            }
            for (int i = 0; i < _defs.Count; i++)
            {
                UpgradeDef def = _defs[i];
                Vector2 p = _layout.Positions[def.Id];
                Rect box = UpgradeTreeGenerator.Box(_layout, def.Domain, def.Tier);
                if (Mathf.Abs(p.x - box.center.x) > UpgradeLayout.ColumnWidth * 0.5f)
                    Flag(def.Id, $"{def.Id} fora da coluna de {def.Domain}.");
                if (p.y > _layout.TierTop(def.Tier) || p.y < _layout.TierBottom(def.Tier))
                    Flag(def.Id, $"{def.Id} fora da faixa do nível {def.Tier}.");
                for (int j = i + 1; j < _defs.Count; j++)
                    if (Vector2.Distance(p, _layout.Positions[_defs[j].Id]) < UpgradeTreeGenerator.MinSpacing)
                    {
                        Flag(def.Id, $"{def.Id} e {_defs[j].Id} a menos de {UpgradeTreeGenerator.MinSpacing} px.");
                        _flagged.Add(_defs[j].Id);
                    }
            }
        }

        void Flag(string id, string message)
        {
            _warnings.Add(message);
            _flagged.Add(id);
        }

        // ---------------------------------------------------------------- interface

        void OnGUI()
        {
            DrawToolbar();
            if (_drawing == null)
            {
                EditorGUILayout.HelpBox($"{UpgradeTreeDrawing.AssetPath} não existe. Use \"Gerar automáticos\" para criar a primeira versão.", MessageType.Info);
                return;
            }
            float warningsHeight = _warnings.Count == 0 ? 0f : Mathf.Min(90f, 18f * _warnings.Count + 8f);
            Rect rest = GUILayoutUtility.GetRect(10, 10, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            _canvas = new Rect(rest.x, rest.y, rest.width, rest.height - warningsHeight);
            if (!_framed && _canvas.width > 1f)
            {
                Frame();
                _framed = true;
            }
            HandleEvents(Event.current);
            if (Event.current.type == EventType.Repaint)
                DrawCanvas();
            if (warningsHeight > 0f)
            {
                GUILayout.BeginArea(new Rect(rest.x, _canvas.yMax, rest.width, warningsHeight));
                _scroll = GUILayout.BeginScrollView(_scroll);
                foreach (string w in _warnings)
                    GUILayout.Label("⚠ " + w, EditorStyles.miniLabel);
                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }
        }

        void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button(new GUIContent("Gerar automáticos", "Refaz só nós, encontros e túneis não fixados."), EditorStyles.toolbarButton))
                Generate(true);
            if (GUILayout.Button(new GUIContent("Gerar tudo", "Desfixa tudo e refaz o desenho do zero."), EditorStyles.toolbarButton) &&
                EditorUtility.DisplayDialog("Gerar tudo", "Desfixar todos os nós, encontros e túneis e refazer o desenho?", "Refazer", "Cancelar"))
                Generate(false);
            GUI.enabled = _drawing != null;
            if (GUILayout.Button(new GUIContent("Aplicar na cena", "Remonta o HUD da partida com este desenho."), EditorStyles.toolbarButton))
                AntFarmHudSetup.CreateOrUpdate();
            if (GUILayout.Button("Enquadrar", EditorStyles.toolbarButton))
                Frame();
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            if (_selKind != Kind.None)
                GUILayout.Label(SelectionLabel(), EditorStyles.miniLabel);
            GUILayout.Label("arraste: mover e fixar · botão direito: opções · roda: zoom · botão do meio: mover a vista", EditorStyles.miniLabel);
            GUILayout.EndHorizontal();
        }

        string SelectionLabel()
        {
            if (_selKind == Kind.Node)
            {
                UpgradeTreeDrawing.Node n = _drawing?.FindNode(_selId);
                return n == null ? _selId : $"{_selId}  ({n.Position.x:0}, {n.Position.y:0}){(n.Pinned ? "  fixado" : "")}";
            }
            UpgradeTreeDrawing.Junction j = _drawing?.FindJunction(_selId);
            return j == null ? _selId : $"encontro {_selId}  ({j.Position.x:0}, {j.Position.y:0}){(j.Pinned ? "  fixado" : "")}";
        }

        void Frame()
        {
            Rect b = _layout.Bounds;
            _zoom = Mathf.Clamp(Mathf.Min(_canvas.width / b.width, _canvas.height / b.height) * 0.95f, MinZoom, MaxZoom);
            _pan = -new Vector2(b.center.x, -b.center.y) * _zoom;
            Repaint();
        }

        Vector2 ToScreen(Vector2 p) => _canvas.center + _pan + new Vector2(p.x, -p.y) * _zoom;
        Vector2 ToWorld(Vector2 s)
        {
            Vector2 d = (s - _canvas.center - _pan) / _zoom;
            return new Vector2(d.x, -d.y);
        }

        // ---------------------------------------------------------------- eventos

        void HandleEvents(Event e)
        {
            if (!_canvas.Contains(e.mousePosition) && e.type != EventType.MouseDrag && e.type != EventType.MouseUp)
                return;
            switch (e.type)
            {
                case EventType.ScrollWheel:
                {
                    Vector2 before = ToWorld(e.mousePosition);
                    _zoom = Mathf.Clamp(_zoom * (e.delta.y > 0 ? 0.9f : 1.1f), MinZoom, MaxZoom);
                    _pan += e.mousePosition - ToScreen(before);
                    e.Use();
                    break;
                }
                case EventType.MouseDown when e.button == 0:
                {
                    (Kind kind, string id) = Pick(e.mousePosition);
                    _selKind = kind;
                    _selId = id;
                    _dragKind = kind;
                    _dragId = id;
                    if (kind != Kind.None)
                        Undo.RecordObject(_drawing, "Mover no desenho da árvore");
                    else
                        _panning = true;
                    e.Use();
                    break;
                }
                case EventType.MouseDown when e.button == 2:
                    _panning = true;
                    e.Use();
                    break;
                case EventType.MouseDown when e.button == 1:
                    ContextMenu(e.mousePosition);
                    e.Use();
                    break;
                case EventType.MouseDrag:
                    if (_dragKind != Kind.None)
                        Drag(e.delta / _zoom);
                    else if (_panning)
                        _pan += e.delta;
                    e.Use();
                    break;
                case EventType.MouseUp:
                    if (_dragKind != Kind.None)
                    {
                        Save(_drawing);
                        Rebuild();
                    }
                    _dragKind = Kind.None;
                    _panning = false;
                    e.Use();
                    break;
                case EventType.KeyDown when e.keyCode == KeyCode.F:
                    Frame();
                    e.Use();
                    break;
            }
            Repaint();
        }

        /// <summary>Move o item arrastado e o fixa; um encontro arrastado fixa também os túneis que passam por ele.</summary>
        void Drag(Vector2 screenDelta)
        {
            var delta = new Vector2(screenDelta.x, -screenDelta.y);
            if (_dragKind == Kind.Node)
            {
                UpgradeTreeDrawing.Node node = _drawing.FindNode(_dragId);
                if (node == null)
                {
                    node = new UpgradeTreeDrawing.Node { Id = _dragId, Position = _layout.Positions[_dragId] };
                    _drawing.Nodes.Add(node);
                }
                node.Position += delta;
                node.Pinned = true;
            }
            else
            {
                UpgradeTreeDrawing.Junction junction = _drawing.FindJunction(_dragId);
                junction.Position += delta;
                junction.Pinned = true;
                foreach (UpgradeTreeDrawing.Tunnel t in _drawing.Tunnels)
                    if (t.Via.Contains(_dragId))
                        t.Pinned = true;
            }
            _layout = new UpgradeLayout(_drawing);
        }

        (Kind, string) Pick(Vector2 mouse)
        {
            foreach (KeyValuePair<string, Vector2> j in _layout.Junctions)
                if (Vector2.Distance(ToScreen(j.Value), mouse) <= Mathf.Max(6f, JunctionRadius * _zoom))
                    return (Kind.Junction, j.Key);
            foreach (UpgradeDef def in _defs)
                if (Vector2.Distance(ToScreen(_layout.Positions[def.Id]), mouse) <= Mathf.Max(6f, NodeRadius * _zoom))
                    return (Kind.Node, def.Id);
            return (Kind.None, null);
        }

        /// <summary>Túnel e trecho (índice do ponto de partida em <see cref="UpgradeLayout.TunnelPoints"/>) sob o mouse.</summary>
        bool PickTunnel(Vector2 mouse, out UpgradeDef from, out UpgradeDef to, out int segment)
        {
            foreach (UpgradeDef def in _defs)
                foreach (UpgradeRequirement r in def.Requires)
                {
                    UpgradeDef target = UpgradeCatalog.Get(r.Id);
                    Vector2[] points = _layout.TunnelPoints(target, def);
                    for (int k = 0; k + 1 < points.Length; k++)
                        if (HandleUtility.DistancePointLine(mouse, ToScreen(points[k]), ToScreen(points[k + 1])) <= TunnelPick)
                        {
                            from = target;
                            to = def;
                            segment = k;
                            return true;
                        }
                }
            from = to = null;
            segment = -1;
            return false;
        }

        void ContextMenu(Vector2 mouse)
        {
            var menu = new GenericMenu();
            (Kind kind, string id) = Pick(mouse);
            if (kind == Kind.Node)
            {
                UpgradeTreeDrawing.Node node = _drawing.FindNode(id);
                bool pinned = node != null && node.Pinned;
                menu.AddItem(new GUIContent(pinned ? $"Desfixar {id}" : $"Fixar {id}"), false, () => Edit("Fixar nó", () =>
                {
                    UpgradeTreeDrawing.Node n = _drawing.FindNode(id);
                    if (n == null)
                        _drawing.Nodes.Add(n = new UpgradeTreeDrawing.Node { Id = id, Position = _layout.Positions[id] });
                    n.Pinned = !pinned;
                }));
            }
            else if (kind == Kind.Junction)
            {
                UpgradeTreeDrawing.Junction junction = _drawing.FindJunction(id);
                menu.AddItem(new GUIContent(junction.Pinned ? $"Desfixar encontro {id}" : $"Fixar encontro {id}"), false,
                             () => Edit("Fixar encontro", () => junction.Pinned = !junction.Pinned));
                menu.AddItem(new GUIContent($"Remover encontro {id}"), false, () => Edit("Remover encontro", () =>
                {
                    // Os túneis que passavam por ele ficam fixados, para o gerador não recolocar o encontro.
                    foreach (UpgradeTreeDrawing.Tunnel t in _drawing.Tunnels)
                        if (t.Via.Remove(id))
                            t.Pinned = true;
                    _drawing.Junctions.Remove(junction);
                }));
            }
            else if (PickTunnel(mouse, out UpgradeDef from, out UpgradeDef to, out int segment))
            {
                Vector2 at = ToWorld(mouse);
                UpgradeTreeDrawing.Tunnel tunnel = _drawing.FindTunnel(from.Id, to.Id);
                menu.AddItem(new GUIContent($"Túnel {from.Id} → {to.Id}/Pôr dobra aqui"), false, () => Edit("Dobra no túnel", () =>
                {
                    UpgradeTreeDrawing.Tunnel t = _drawing.FindTunnel(from.Id, to.Id);
                    if (t == null)
                        _drawing.Tunnels.Add(t = new UpgradeTreeDrawing.Tunnel { From = from.Id, To = to.Id });
                    string jid = _drawing.NewJunctionId();
                    _drawing.Junctions.Add(new UpgradeTreeDrawing.Junction { Id = jid, Position = new Vector2(Mathf.Round(at.x), Mathf.Round(at.y)), Pinned = true });
                    // O trecho k vai do ponto k ao k + 1; o ponto 0 é o requisito, então a dobra entra na posição k de Via.
                    t.Via.Insert(Mathf.Min(segment, t.Via.Count), jid);
                    t.Pinned = true;
                }));
                if (tunnel != null)
                    menu.AddItem(new GUIContent($"Túnel {from.Id} → {to.Id}/{(tunnel.Pinned ? "Desfixar (o gerador refaz)" : "Fixar")}"), false,
                                 () => Edit("Fixar túnel", () => tunnel.Pinned = !tunnel.Pinned));
            }
            else
                menu.AddDisabledItem(new GUIContent("Nada aqui"));
            menu.ShowAsContext();
        }

        void Edit(string undo, System.Action change)
        {
            Undo.RecordObject(_drawing, undo);
            change();
            Save(_drawing);
            Rebuild();
        }

        // ---------------------------------------------------------------- desenho

        void DrawCanvas()
        {
            EditorGUI.DrawRect(_canvas, Background);
            GUI.BeginClip(_canvas);
            Vector2 offset = _canvas.position;
            Rect b = _layout.Bounds;
            int columns = UpgradeCatalog.Domains.Length;

            // Colunas dos domínios e faixas dos níveis.
            for (int c = 0; c < columns; c++)
            {
                Color tint = AntFarmHudSetup.CaveTint(c);
                tint.a = 0.07f;
                float x = UpgradeLayout.ColumnX(c);
                FillWorld(offset, Rect.MinMaxRect(x - UpgradeLayout.ColumnWidth * 0.5f, b.yMin, x + UpgradeLayout.ColumnWidth * 0.5f, UpgradeLayout.GalleryY), tint);
            }
            for (int t = 1; t < UpgradeCatalog.TierCount; t++)
            {
                FillWorld(offset, Rect.MinMaxRect(b.xMin, _layout.TierTop(t) - 2f / _zoom, b.xMax, _layout.TierTop(t)), TierLine);
                Label(offset, new Vector2(b.xMin + 60f, _layout.TierTop(t) - 40f), "Nível " + t, Color.gray, false);
            }

            // Galeria, Câmara real e salões.
            Handles.color = new Color(0.6f, 0.5f, 0.4f);
            Line(offset, new Vector2(UpgradeLayout.ColumnX(0), UpgradeLayout.GalleryY), new Vector2(UpgradeLayout.ColumnX(columns - 1), UpgradeLayout.GalleryY), 3f);
            Line(offset, _layout.Root, new Vector2(_layout.Root.x, UpgradeLayout.GalleryY), 3f);
            Disc(offset, _layout.Root, 60f, new Color(0.9f, 0.75f, 0.3f), false);
            for (int c = 0; c < columns; c++)
            {
                UpgradeDomain domain = UpgradeCatalog.Domains[c].Domain;
                Vector2 hall = _layout.Hall(domain);
                Handles.color = AntFarmHudSetup.CaveTint(c);
                Line(offset, new Vector2(hall.x, UpgradeLayout.GalleryY), hall, 3f);
                Label(offset, hall, domain.ToString(), AntFarmHudSetup.CaveTint(c), true);
            }

            // Túneis: os do domínio no tom da coluna, os entre domínios claros; os do item selecionado em amarelo.
            foreach (UpgradeDef def in _defs)
                foreach (UpgradeRequirement r in def.Requires)
                {
                    UpgradeDef target = UpgradeCatalog.Get(r.Id);
                    bool cross = target.Domain != def.Domain && target.Domain != UpgradeDomain.Root;
                    UpgradeTreeDrawing.Tunnel tunnel = _drawing.FindTunnel(target.Id, def.Id);
                    bool selected = _selKind == Kind.Node && (_selId == def.Id || _selId == target.Id) ||
                                    _selKind == Kind.Junction && tunnel != null && tunnel.Via.Contains(_selId);
                    Handles.color = selected ? SelectedColor : cross ? CrossColor : AntFarmHudSetup.CaveTint(_layout.Column(def.Domain));
                    Vector2[] points = _layout.TunnelPoints(target, def);
                    float width = selected ? 4f : tunnel != null && tunnel.Pinned ? 3f : 2f;
                    for (int k = 0; k + 1 < points.Length; k++)
                        Curve(offset, points[k], points[k + 1], width);
                }

            foreach (KeyValuePair<string, Vector2> j in _layout.Junctions)
            {
                UpgradeTreeDrawing.Junction junction = _drawing.FindJunction(j.Key);
                bool selected = _selKind == Kind.Junction && _selId == j.Key;
                Disc(offset, j.Value, JunctionRadius, selected ? SelectedColor : new Color(0.55f, 0.5f, 0.45f), junction != null && junction.Pinned);
            }
            foreach (UpgradeDef def in _defs)
            {
                Vector2 p = _layout.Positions[def.Id];
                UpgradeTreeDrawing.Node node = _drawing.FindNode(def.Id);
                bool selected = _selKind == Kind.Node && _selId == def.Id;
                Color color = _flagged.Contains(def.Id) ? WarningColor : AntFarmHudSetup.CaveTint(_layout.Column(def.Domain));
                float radius = def.Kind == UpgradeKind.Gallery ? NodeRadius * 0.75f : NodeRadius;
                Disc(offset, p, radius, selected ? SelectedColor : color, node != null && node.Pinned);
                if (_zoom >= 0.08f)
                    Label(offset, p + new Vector2(0f, -radius - 22f), def.Id, Color.white, true);
            }
            GUI.EndClip();
        }

        Vector2 Local(Vector2 offset, Vector2 world) => ToScreen(world) - offset;

        void FillWorld(Vector2 offset, Rect world, Color color)
        {
            Vector2 a = Local(offset, new Vector2(world.xMin, world.yMax)), c = Local(offset, new Vector2(world.xMax, world.yMin));
            EditorGUI.DrawRect(Rect.MinMaxRect(a.x, a.y, c.x, c.y), color);
        }

        void Line(Vector2 offset, Vector2 a, Vector2 b, float width) =>
            Handles.DrawAAPolyLine(width, Local(offset, a), Local(offset, b));

        /// <summary>O mesmo serpentear do túnel no jogo (<see cref="CaveGraphic.TunnelPoint"/>).</summary>
        void Curve(Vector2 offset, Vector2 a, Vector2 b, float width)
        {
            var t = new Vector4(a.x, a.y, b.x, b.y);
            int pieces = Mathf.Min(24, CaveGraphic.TunnelPieces((b - a).magnitude));
            var points = new Vector3[pieces + 1];
            for (int i = 0; i <= pieces; i++)
                points[i] = Local(offset, CaveGraphic.TunnelPoint(t, i / (float)pieces));
            Handles.DrawAAPolyLine(width, points);
        }

        void Disc(Vector2 offset, Vector2 world, float radius, Color color, bool pinned)
        {
            Vector2 center = Local(offset, world);
            float r = Mathf.Max(3f, radius * _zoom);
            Handles.color = color;
            Handles.DrawSolidDisc(center, Vector3.forward, r);
            if (pinned)
            {
                Handles.color = Color.white;
                Handles.DrawWireDisc(center, Vector3.forward, r + 2f, 2f);
            }
        }

        static GUIStyle _labelStyle;

        void Label(Vector2 offset, Vector2 world, string text, Color color, bool centered)
        {
            _labelStyle ??= new GUIStyle(EditorStyles.miniBoldLabel);
            _labelStyle.alignment = centered ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            _labelStyle.normal.textColor = color;
            Vector2 p = Local(offset, world);
            GUI.Label(new Rect(p.x - (centered ? 60f : 0f), p.y - 8f, 120f, 16f), text, _labelStyle);
        }
    }
}
