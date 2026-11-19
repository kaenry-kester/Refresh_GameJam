using UnityEngine;

// Um tipo de item. Crie em: Create > Inventario > Item
[CreateAssetMenu(menuName = "Inventario/Item")]
public class ItemData : ScriptableObject
{
    public string nome;
    public Sprite icone;
    [UnityEngine.Serialization.FormerlySerializedAs("iconeFrigorifico")]
    public Sprite iconeFrigorificoPre; // versão clara, usada nos slots do frigorífico (vazio = usa o icone)
    public Sprite iconeFrigorificoPos;
    public bool ehSemente;      // só sementes podem ser plantadas em vasos

    [Header("Vaso (item que vira um vaso novo no jardim)")]
    public bool ehVaso;           // soltar este item num slot livre da GMP_Scene_Top cria um vaso
    public bool ehFossil;         // recompensa de expedição (fica no inventário; pode ir ao microscópio)
    public GameObject prefabVaso; // o prefab do vaso que será criado

    // "Pacote" de crescimento: o que aparece em cima do vaso em cada fase.
    // Plantou -> Semente. Cada dia regado avança uma fase até a Final.
    [Header("Crescimento no vaso (só sementes)")]
    [UnityEngine.Serialization.FormerlySerializedAs("spritePlantado")]
    public Sprite spriteSemente;   // logo depois de plantar (vazio = usa o icone)
    public Sprite spriteBroto;
    public Sprite spriteCrescendo;
    public Sprite spriteFinal;

    public const int FaseFinal = 3; // 0 = Semente, 1 = Broto, 2 = Crescendo, 3 = Final

    [Header("Painel do vaso: necessidades da planta (25, 50, 75 ou 100%)")]
    public int precoBase = 100;                    // valor de venda sem bônus
    [Range(25, 100)] public float aguaNecessaria = 50f;
    [Range(25, 100)] public float co2Necessario = 50f;
    [Range(25, 100)] public float luzNecessaria = 50f;
    [Tooltip("Imagem da moldura do painel do vaso. Vazio = usa o desenho da planta na fase atual.")]
    public Sprite fotoPainel;
    [Tooltip("Como a planta foi extinta e por quê")]
    [TextArea(3, 8)] public string descricao;
    [Tooltip("Onde ela costumava nascer / de onde se originou")]
    [TextArea(3, 8)] public string habitat;

    // Arredonda as necessidades para 25, 50, 75 ou 100 sempre que algo muda no Inspector
    void OnValidate()
    {
        aguaNecessaria = Arredondar(aguaNecessaria);
        co2Necessario = Arredondar(co2Necessario);
        luzNecessaria = Arredondar(luzNecessaria);
    }

    public static float Arredondar(float v) => Mathf.Clamp(Mathf.Round(v / 25f) * 25f, 25f, 100f);

    public const float BonusPorRequisito = 0.5f; // cada requisito cumprido soma +50% do preço base

    // Preço de venda: proporcional ao tamanho (semente 25%, broto 50%, crescendo 75%, final 100%)
    // + 50% por requisito cumprido (Água, CO2, Luz)
    public int PrecoVenda(int requisitosCumpridos, int fase = FaseFinal)
    {
        float tamanho = (Mathf.Clamp(fase, 0, FaseFinal) + 1f) / (FaseFinal + 1f);
        return Mathf.RoundToInt(precoBase * tamanho * (1f + BonusPorRequisito * requisitosCumpridos));
    }

    // Texto curto para o nível de uma necessidade
    public static string Nivel(float v) => v <= 25f ? "baixa" : v <= 50f ? "média" : v <= 75f ? "alta" : "muito alta";

    [Header("Colheita")]
    [Tooltip("Opcional: item que vai para o inventário ao colher. Vazio = cria sozinho um item com o sprite Final.")]
    public ItemData itemColhido;

    [System.NonSerialized] ItemData[] colhidosGerados;

    // Planta colhida: de qual semente/espécie ela veio (para o painel mostrar água, CO2, luz, textos...)
    [System.NonSerialized] public ItemData origem;
    [System.NonSerialized] public int faseColhida = -1;
    public ItemData Especie => origem ? origem : this;
    public bool EhPlantaColhida => origem != null && faseColhida == FaseFinal;
    static readonly string[] nomesFase = { "semente", "broto", "crescendo", "adulta" };

    // Item da planta guardada na coleção, um por fase (o mesmo item para a mesma fase, então empilha).
    // Na fase final usa "Item Colhido" se estiver preenchido.
    public ItemData ItemColhido(int fase = FaseFinal)
    {
        fase = Mathf.Clamp(fase, 0, FaseFinal);
        // "Item Colhido" só vale se for um item de planta de verdade
        // (não a própria semente nem outra semente, senão o inventário receberia uma semente)
        if (fase == FaseFinal && itemColhido && itemColhido != this && !itemColhido.ehSemente)
        {
            itemColhido.origem = this; // o painel do inventário mostra os dados desta espécie
            itemColhido.faseColhida = FaseFinal;
            return itemColhido;
        }

        if (colhidosGerados == null || colhidosGerados.Length != FaseFinal + 1)
            colhidosGerados = new ItemData[FaseFinal + 1];
        if (colhidosGerados[fase]) return colhidosGerados[fase];

        var item = CreateInstance<ItemData>();
        item.name = item.nome = (string.IsNullOrEmpty(nome) ? name : nome.Trim()) + " (" + nomesFase[fase] + ")";
        item.icone = Recortar(SpriteDaFase(fase));
        item.ehSemente = false;
        item.origem = this;
        item.faseColhida = fase;
        return colhidosGerados[fase] = item;
    }

    // ---- Semente "pré" (vem da fusão) ----
    // Não pode ser plantada. Dentro do frigorífico, quando o tempo acaba, vira a semente de verdade ("pós").
    [System.NonSerialized] public ItemData viraNoFrigorifico;
    [System.NonSerialized] ItemData preGerada;

    public ItemData SementePre()
    {
        if (preGerada) return preGerada;
        var item = CreateInstance<ItemData>();
        item.name = item.nome = (string.IsNullOrEmpty(nome) ? name : nome.Trim()) + " (pré)";
        item.icone = iconeFrigorificoPre ? iconeFrigorificoPre : icone;
        item.iconeFrigorificoPre = item.icone;
        item.iconeFrigorificoPos = iconeFrigorificoPos ? iconeFrigorificoPos : icone;
        item.ehSemente = false; // ainda não dá para plantar
        item.viraNoFrigorifico = this;
        item.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return preGerada = item;
    }

    // Recortes já feitos (sprite original -> recortado), para não criar um novo toda vez
    static readonly System.Collections.Generic.Dictionary<Sprite, Sprite> recortes =
        new System.Collections.Generic.Dictionary<Sprite, Sprite>();

    // Sprite só com a parte desenhada (sem o espaço transparente), para o desenho
    // ficar grande e centralizado em slots e no painel do vaso.
    public static Sprite Recortar(Sprite s)
    {
        if (!s) return null;
        if (recortes.TryGetValue(s, out var pronto) && pronto) return pronto;
        var r = CriarRecorte(s);
        recortes[s] = r;
        return r;
    }

    static Sprite CriarRecorte(Sprite s)
    {
        if (s.vertices.Length == 0) return s;

        Vector2 min = s.vertices[0], max = s.vertices[0];
        foreach (var v in s.vertices) { min = Vector2.Min(min, v); max = Vector2.Max(max, v); }

        float ppu = s.pixelsPerUnit;
        Vector2 inicio = s.rect.position + s.pivot + min * ppu; // vértices são relativos ao pivot
        var r = new Rect(inicio, (max - min) * ppu);
        r = Rect.MinMaxRect(Mathf.Max(r.xMin, s.rect.xMin), Mathf.Max(r.yMin, s.rect.yMin),
                            Mathf.Min(r.xMax, s.rect.xMax), Mathf.Min(r.yMax, s.rect.yMax));
        return Sprite.Create(s.texture, r, new Vector2(0.5f, 0.5f), ppu);
    }

    // Sprite da fase (0..3). Se alguma estiver vazia, usa a fase anterior que tiver.
    public Sprite SpriteDaFase(int fase)
    {
        Sprite[] fases = { spriteSemente ? spriteSemente : icone, spriteBroto, spriteCrescendo, spriteFinal };
        for (int i = Mathf.Clamp(fase, 0, FaseFinal); i >= 0; i--)
            if (fases[i]) return fases[i];
        return icone;
    }
}
