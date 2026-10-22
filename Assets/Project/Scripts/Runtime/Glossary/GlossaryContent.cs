namespace Fabula.Incrementant.Glossary
{
    /// <summary>
    /// Conteúdo do glossário: categorias, na ordem da lista, e as entradas de cada uma. Os textos ficam na tabela de idiomas
    /// (chaves <see cref="CategoryKey"/>, <see cref="TitleKey"/> e <see cref="TextKey"/>); aqui só a ordem e os ids.
    /// Entrada nova: id aqui e as três linhas no catálogo (<c>LocalizationCatalog.Glossary.cs</c>). Guia: <c>GlossaryGuide.md</c>.
    /// </summary>
    public static class GlossaryContent
    {
        public readonly struct Category
        {
            public readonly string Id;
            public readonly string[] Entries;

            public Category(string id, params string[] entries)
            {
                Id = id;
                Entries = entries;
            }
        }

        public static readonly Category[] Categories =
        {
            new Category("basics", "goal", "camera", "pause", "save", "gameover"),
            new Category("resources", "nutrients", "slots", "soil", "water", "rain", "plants", "light", "rubble"),
            new Category("tools", "dig", "build", "glowbrush", "finger"),
            new Category("rooms", "queenroom", "nutrientroom", "nursery", "waste", "dew", "farm", "depots", "area"),
            new Category("ants", "queen", "brood", "worker", "carrier", "cutter", "digger", "gardener", "nurse", "soldier", "spitter",
                         "midden", "farmer"),
            new Category("queenmenu", "mode", "stance"),
            new Category("evolution", "tree", "unlocks"),
            new Category("threats", "raids", "raidtargets", "blackaphid"),
            new Category("interface", "cards", "colonypanel", "bell", "showrooms", "tips"),
        };

        public static string CategoryKey(string category) => "glossary.cat." + category;

        public static string TitleKey(string entry) => "glossary." + entry + ".title";

        public static string TextKey(string entry) => "glossary." + entry;
    }
}
