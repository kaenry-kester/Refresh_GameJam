using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Painel "vaso painel 1": abre ao clicar numa planta (Vaso chama PainelVaso.Abrir).
// Mostra as necessidades da planta (Água, CO2, Luz), as barras, a descrição, o habitat,
// o vaso + planta em tamanho grande e os botões Fechar (X), Vender e Coletar.
// Funciona em QUALQUER cena: monte o painel num Canvas próprio, transforme esse Canvas num prefab
// chamado "PainelVaso" dentro de uma pasta "Resources". O jogo cria ele sozinho na primeira cena
// e o mantém vivo entre as trocas de cena (não precisa colocar em cada cena).
// Coloque este script num objeto SEMPRE ativo do prefab, e aponte "Raiz" para o painel (é ele que liga/desliga).
public class PainelVaso : MonoBehaviour
{
    const string PREFAB = "PainelVaso"; // Assets/.../Resources/PainelVaso.prefab

    // Cria o painel automaticamente ao iniciar o jogo (em qualquer cena)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CriarAoIniciar()
    {
        if (Instancia) return;
        var prefab = Resources.Load<GameObject>(PREFAB);
        if (prefab) Instantiate(prefab).name = PREFAB;
        else Debug.LogWarning("PainelVaso: prefab 'Resources/PainelVaso' não encontrado.");
    }

    public static PainelVaso Instancia { get; private set; }
    public static bool Aberto => Instancia && Instancia.raiz && Instancia.raiz.activeSelf;

    [Tooltip("O que liga/desliga ao abrir/fechar. Vazio = o próprio objeto deste script (ex.: o Canvas do painel).")]
    [SerializeField] GameObject raiz;

    [Header("Textos (TMP)")]
    [SerializeField] TMP_Text titulo;         // nome da planta (placa "vaso")
    [SerializeField] TMP_Text textoAgua;      // ao lado de "H2O:"
    [SerializeField] TMP_Text textoCO2;       // ao lado de "CO2:"
    [SerializeField] TMP_Text textoLuz;       // ao lado de "Luz:"
    [SerializeField] TMP_Text textoDescricao;
    [SerializeField] TMP_Text textoHabitat;
    [SerializeField] TMP_Text textoVender;    // texto do botão Vender
    [SerializeField] string formatoVender = "Vender por R$ {0},00";

    [Header("Barras (Image azul dentro de cada tubo)")]
    [SerializeField] Image barraAgua;
    [SerializeField] Image barraCO2;
    [SerializeField] Image barraLuz;
    [Tooltip("Quão rápido a barra e o medidor sobem/descem até a água atual (% por segundo)")]
    [SerializeField] float velocidadeNivel = 50f;
    [Tooltip("Cor da barra de água enquanto ainda falta água")]
    [SerializeField] Color corAgua = new Color(0.27f, 0.55f, 0.86f, 1f);
    [Tooltip("Cor da barra quando chegou na quantidade que a planta precisa (ganha o bônus)")]
    [SerializeField] Color corCorreta = new Color(0.3f, 0.75f, 0.35f, 1f);
    [Tooltip("Cor da barra quando passou do limite (muita água: perde o bônus)")]
    [SerializeField] Color corEncharcada = new Color(0.12f, 0.2f, 0.45f, 1f);

    [Header("Medidores (começam em 0% e sobem com a água)")]
    [SerializeField] RectTransform marcaAgua;
    [SerializeField] RectTransform marcaCO2;
    [SerializeField] RectTransform marcaLuz;

    [Tooltip("Pos Y do medidor (Rect Transform) para os níveis 25, 50, 75 e 100%. Vale para os 3 tubos.")]
    [SerializeField] float[] yPorNivel = { 55f, 113f, 178.6f, 238f };
    [Tooltip("Pos Y do medidor no 0% (fundo do tubo)")]
    [SerializeField] float yZero = -3f;
    [SerializeField] float xMedidorAgua = -410f;
    [SerializeField] float xMedidorCO2 = 71f;
    [SerializeField] float xMedidorLuz = -175.8f;

    [Header("Botões")]
    [SerializeField] Button botaoFechar;   // Sair (X)
    [SerializeField] Button botaoVender;   // "Vender por R$ X,00"
    [Tooltip("\"Manter na coleção\": só com a planta pronta; a versão final dela vai para o inventário")]
    [SerializeField] Button botaoColetar;
    [Tooltip("Botões ainda bloqueados (ex.: ButtonCO2, ButtonCO2-, ButtonLUZ, ButtonLUZ-)")]
    [SerializeField] Button[] botoesBloqueados;
    [Tooltip("Luz + e - (ButtonLUZ / ButtonLUZ-): só funcionam no vaso com controle de luz")]
    [SerializeField] Button botaoLuzMais;
    [SerializeField] Button botaoLuzMenos;

    [Header("Imagens")]
    [SerializeField] Image imagemVaso;   // vaso grande (lado direito)
    [SerializeField] Image imagemPlanta; // planta grande em cima do vaso (lado direito)
    [SerializeField] Image fotoPlanta;   // opcional: foto na moldura do painel
    [Tooltip("Ajuste da base da planta grande em relação ao vaso (x: + direita / - esquerda, y: + cima)")]
    [SerializeField] Vector2 deslocamentoPlanta = new Vector2(-9f, 17f);
    [Tooltip("Ajuste extra de altura por fase: Semente, Broto, Crescendo, Final (negativo = desce).\n" +
             "Crescendo e Final não têm terra no desenho, então descem para dentro do vaso.")]
    [SerializeField] float[] ajusteYPorFase = { 0f, 0f, -15f, -15f };

    // Posição, tamanho, pivot e escala do vaso e da planta grandes EXATAMENTE como estão no prefab.
    // São guardados ao iniciar e reaplicados sempre que o painel mostra uma planta.
    struct Layout { public Vector2 pos, tamanho, pivot; public Vector3 escala; }
    Layout layoutVaso, layoutPlanta;
    Vector2 basePlanta;
    float nivelMostrado; // água que a barra/medidor mostram agora (anima até a água real)
    float luzMostrada;   // luz que a barra/medidor de luz mostram agora

    static Layout Guardar(Image img)
    {
        if (!img) return default;
        var rt = img.rectTransform;
        return new Layout { pos = rt.anchoredPosition, tamanho = rt.sizeDelta, pivot = rt.pivot, escala = rt.localScale };
    }

    public Vaso VasoAtual { get; private set; }

    void Awake()
    {
        // Só existe um painel no jogo: se já tem um (ex.: um colocado na cena por engano), este some
        if (Instancia && Instancia != this)
        {
            Destroy(transform.root.gameObject);
            return;
        }
        Instancia = this;
        DontDestroyOnLoad(transform.root.gameObject); // o Canvas do painel sobrevive à troca de cena

        // O que está no prefab é a posição/tamanho/escala oficial do vaso e da planta grandes
        layoutVaso = Guardar(imagemVaso);
        layoutPlanta = Guardar(imagemPlanta);

        // Ponto onde a base da planta encosta no vaso grande = embaixo-no-meio da caixa da Planta no prefab.
        // A caixa do prefab vale como o tamanho da planta na fase FINAL.
        basePlanta = layoutPlanta.pos + new Vector2(
            (0.5f - layoutPlanta.pivot.x) * layoutPlanta.tamanho.x * layoutPlanta.escala.x,
            -layoutPlanta.pivot.y * layoutPlanta.tamanho.y * layoutPlanta.escala.y);
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += AoTrocarCena;

        if (!raiz) raiz = gameObject;

        foreach (var b in new[] { barraAgua, barraCO2, barraLuz })
        {
            if (!b) continue;
            // Image sem sprite ignora o "Filled" (sempre desenha cheia): usa um sprite branco
            if (!b.sprite) b.sprite = SpriteBranco();
            b.type = Image.Type.Filled; // cilindro que sobe de baixo para cima
            b.fillMethod = Image.FillMethod.Vertical;
            b.fillOrigin = (int)Image.OriginVertical.Bottom;
            b.fillAmount = 0f;
        }

        if (botaoFechar) botaoFechar.onClick.AddListener(Fechar);
        if (botaoVender) botaoVender.onClick.AddListener(Vender);
        if (botaoColetar) botaoColetar.onClick.AddListener(Coletar);
        if (botaoLuzMais) botaoLuzMais.onClick.AddListener(() => { if (VasoAtual) VasoAtual.AjustarLuz(+1); });
        if (botaoLuzMenos) botaoLuzMenos.onClick.AddListener(() => { if (VasoAtual) VasoAtual.AjustarLuz(-1); });

        // CO2 e Luz ainda não podem ser ajustados
        if (botoesBloqueados != null)
            foreach (var b in botoesBloqueados)
                if (b) b.interactable = false;

        raiz.SetActive(false); // começa fechado (por último: pode desligar este próprio objeto)
    }

    void OnDestroy()
    {
        if (Instancia != this) return;
        Instancia = null;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= AoTrocarCena;
    }

    // Trocou de cena: fecha o painel
    void AoTrocarCena(UnityEngine.SceneManagement.Scene c, UnityEngine.SceneManagement.LoadSceneMode m) => Fechar();

    public static void Abrir(Vaso vaso)
    {
        if (Instancia && vaso && vaso.Semente) Instancia.Mostrar(vaso);
    }

    // Abre o painel para uma planta adulta guardada no inventário (clique no slot)
    public static void AbrirItem(ItemData item, int indice, Inventario.Local local)
    {
        if (Instancia && item && item.EhPlantaColhida) Instancia.MostrarItem(item, indice, local);
    }

    // Planta do inventário aberta no painel (null = o painel está mostrando um vaso)
    ItemData itemAberto;
    int itemIndice;
    Inventario.Local itemLocal;
    bool ModoItem => itemAberto != null;
    Sprite ultimoSpriteVaso; // vaso mostrado atrás da planta do inventário
    RegadorUI regadorUI;     // escondido quando a planta vem do inventário (não há o que regar)

    void Mostrar(Vaso vaso)
    {
        VasoAtual = vaso;
        itemAberto = null;
        if (vaso.SpriteVaso) ultimoSpriteVaso = vaso.SpriteVaso;
        MostrarDados(vaso.Semente);

        // Água: barra e medidor abrem já no nível atual
        nivelMostrado = vaso.Agua;
        MostrarAgua();
        AtualizarDinamico();
    }

    void MostrarItem(ItemData item, int indice, Inventario.Local local)
    {
        VasoAtual = null;
        itemAberto = item;
        itemIndice = indice;
        itemLocal = local;
        MostrarDados(item.Especie);

        nivelMostrado = 0f; // planta fora do vaso: sem água
        MostrarAgua();
        AtualizarDinamico();
    }

    // Textos e medidores fixos da espécie
    void MostrarDados(ItemData p)
    {
        raiz.SetActive(true);
        if (!regadorUI) regadorUI = GetComponentInChildren<RegadorUI>(true);
        if (regadorUI) regadorUI.gameObject.SetActive(!ModoItem);

        Escrever(titulo, string.IsNullOrEmpty(p.nome) ? p.name : p.nome.Trim());
        Escrever(textoAgua, $"{p.aguaNecessaria:0}%");
        Escrever(textoCO2, $"{p.co2Necessario:0}%");
        Escrever(textoLuz, $"{p.luzNecessaria:0}%");
        Escrever(textoDescricao, p.descricao);
        Escrever(textoHabitat, p.habitat);

        // CO2 ainda não está desbloqueado: barra desligada e medidor parado no 0%
        if (barraCO2) barraCO2.gameObject.SetActive(false);
        Marcar(marcaCO2, xMedidorCO2, 0f);

        // Luz: só no vaso com controle de luz (nos outros, barra desligada e medidor no 0%)
        bool temLuz = TemLuz;
        if (barraLuz) barraLuz.gameObject.SetActive(temLuz);
        luzMostrada = temLuz ? VasoAtual.Luz : 0f;
        MostrarLuz();
    }

    bool TemLuz => !ModoItem && VasoAtual && VasoAtual.ControleLuz;

    // Medidor de luz no nível mostrado e barra até ele: verde quando bate com o que a planta precisa
    void MostrarLuz()
    {
        Marcar(marcaLuz, xMedidorLuz, luzMostrada);
        if (!barraLuz || !TemLuz) return;

        barraLuz.fillAmount = Preenchimento(barraLuz, marcaLuz, xMedidorLuz, luzMostrada);
        bool certa = Mathf.Abs(luzMostrada - VasoAtual.Semente.luzNecessaria) < 0.01f;
        barraLuz.color = certa ? corCorreta : corAgua;
    }

    public void Fechar()
    {
        VasoAtual = null;
        itemAberto = null;
        if (raiz) raiz.SetActive(false);
    }

    // O item do inventário ainda está no slot que foi clicado?
    bool ItemAindaNoSlot()
    {
        var dados = Inventario.De(itemLocal);
        return itemIndice >= 0 && itemIndice < dados.Count && dados[itemIndice] != null && dados[itemIndice].item == itemAberto;
    }

    void Update()
    {
        if (!Aberto) return;

        if (ModoItem)
        {
            if (!ItemAindaNoSlot()) { Fechar(); return; } // vendeu o último / moveu de slot
            nivelMostrado = 0f;
        }
        else
        {
            if (!VasoAtual || !VasoAtual.Semente) { Fechar(); return; } // vaso esvaziou
            // Barra e medidor sobem/descem juntos, suave, até a água atual
            nivelMostrado = Mathf.MoveTowards(nivelMostrado, VasoAtual.Agua, velocidadeNivel * Time.deltaTime);
            if (TemLuz) luzMostrada = Mathf.MoveTowards(luzMostrada, VasoAtual.Luz, velocidadeNivel * Time.deltaTime);
        }
        MostrarAgua();
        MostrarLuz();
        AtualizarDinamico();
    }

    // Medidor de água no nível mostrado, barra azul até ele, e a cor:
    // falta água = azul, chegou na quantidade certa = cor "correta", passou do limite = "encharcada"
    void MostrarAgua()
    {
        var v = VasoAtual;
        Marcar(marcaAgua, xMedidorAgua, nivelMostrado);
        if (!barraAgua) return;

        barraAgua.fillAmount = Preenchimento(barraAgua, marcaAgua, xMedidorAgua, nivelMostrado);
        if (!v) { barraAgua.color = corAgua; return; } // planta do inventário: sem água
        bool chegou = nivelMostrado >= v.AguaNecessaria - 0.01f;
        bool passou = nivelMostrado > v.AguaMaxIdeal + 0.01f;
        barraAgua.color = passou ? corEncharcada : chegou ? corCorreta : corAgua;
    }

    // O que muda enquanto o painel está aberto (fase da planta, preço, botões)
    void AtualizarDinamico()
    {
        if (ModoItem) { AtualizarItem(); return; }
        var v = VasoAtual;

        // Sempre na mesma posição e tamanho, qualquer que seja o vaso/planta.
        // Usa a imagem recortada (sem espaço transparente) para o desenho preencher a caixa.
        // "Preserve Aspect" de cada Image é o que estiver marcado no Inspector.
        Mostrar(imagemVaso, ItemData.Recortar(v.SpriteVaso));
        Mostrar(imagemPlanta, ItemData.Recortar(v.SpritePlanta));
        // Moldura: a foto própria da semente; sem foto, o desenho da planta na fase atual
        Mostrar(fotoPlanta, v.Semente.fotoPainel ? v.Semente.fotoPainel : ItemData.Recortar(v.SpritePlanta));
        Fixar(imagemVaso, layoutVaso);
        float final = v.TamanhoNaFase(ItemData.FaseFinal);
        PosicionarPlanta(v.Fase, final > 0f ? v.TamanhoNaFase(v.Fase) / final : 1f);

        Escrever(textoVender, string.Format(formatoVender, v.PrecoVenda));
        // Vender funciona em qualquer fase (o preço acompanha o tamanho da planta).
        // Manter na coleção só com a planta pronta (versão final vai para o inventário).
        if (botaoVender) botaoVender.interactable = true;
        if (botaoColetar) botaoColetar.interactable = v.Pronta;
        AtualizarBotoesLuz();
    }

    // + / - de Luz só funcionam no vaso com controle de luz (e não passam de 0% / 100%)
    void AtualizarBotoesLuz()
    {
        bool tem = TemLuz;
        if (botaoLuzMais) botaoLuzMais.interactable = tem && VasoAtual.Luz < 100f;
        if (botaoLuzMenos) botaoLuzMenos.interactable = tem && VasoAtual.Luz > 0f;
    }

    // Planta adulta do inventário: mesma tela, sem regar. Já está na coleção (Manter desativado).
    void AtualizarItem()
    {
        var p = itemAberto.Especie;
        var spriteFinal = ItemData.Recortar(p.SpriteDaFase(ItemData.FaseFinal));

        Mostrar(imagemVaso, ItemData.Recortar(ultimoSpriteVaso));
        Mostrar(imagemPlanta, spriteFinal);
        Mostrar(fotoPlanta, p.fotoPainel ? p.fotoPainel : spriteFinal);
        Fixar(imagemVaso, layoutVaso);
        PosicionarPlanta(ItemData.FaseFinal, 1f);

        Escrever(textoVender, string.Format(formatoVender, PrecoItem));
        if (botaoVender) botaoVender.interactable = true;
        if (botaoColetar) botaoColetar.interactable = false;
        AtualizarBotoesLuz();
    }

    // Planta do inventário vende pelo preço da adulta, sem bônus (fora do vaso não há água/CO2/luz)
    int PrecoItem => itemAberto ? itemAberto.Especie.PrecoVenda(0, ItemData.FaseFinal) : 0;

    void Vender()
    {
        if (ModoItem)
        {
            if (!ItemAindaNoSlot()) { Fechar(); return; }
            int valor = PrecoItem;
            if (Inventario.RemoverDoSlot(itemIndice, 1, itemLocal)) Dinheiro.Ganhar(valor);
            if (!ItemAindaNoSlot()) Fechar(); // vendeu a última da pilha
            return;
        }
        if (VasoAtual && VasoAtual.Vender() > 0) Fechar();
    }

    void Coletar()
    {
        if (VasoAtual && VasoAtual.Colher()) Fechar();
    }

    static void Escrever(TMP_Text t, string s)
    {
        if (t) t.text = s ?? "";
    }

    static void Mostrar(Image img, Sprite s)
    {
        if (!img) return;
        if (img.sprite != s) img.sprite = s;
        img.enabled = s != null;
    }

    // Mesma lógica do jardim: a base da planta (pivot embaixo, no meio do desenho) fica sempre no
    // mesmo ponto do vaso, e o tamanho segue a fase (proporção do Tamanho Por Fase do vaso).
    // A fase final fica do tamanho da caixa da Planta no prefab; as outras, menores na mesma proporção.
    void PosicionarPlanta(int fase, float proporcao)
    {
        if (!imagemPlanta || !imagemPlanta.sprite) return;
        var rt = imagemPlanta.rectTransform;

        float lado = Mathf.Max(layoutPlanta.tamanho.x, layoutPlanta.tamanho.y) * proporcao; // lado maior

        // Mantém a proporção do desenho (não estica)
        Vector2 px = imagemPlanta.sprite.rect.size;
        Vector2 tamanho = px.x >= px.y ? new Vector2(lado, lado * px.y / px.x)
                                       : new Vector2(lado * px.x / px.y, lado);

        imagemPlanta.preserveAspect = false; // o tamanho já respeita a proporção
        rt.pivot = new Vector2(0.5f, 0f);    // base do desenho (a imagem é recortada: embaixo = base)
        rt.localScale = layoutPlanta.escala;
        rt.sizeDelta = tamanho;
        float ajusteFase = ajusteYPorFase != null && ajusteYPorFase.Length > 0
            ? ajusteYPorFase[Mathf.Clamp(fase, 0, ajusteYPorFase.Length - 1)] : 0f;
        rt.anchoredPosition = basePlanta + deslocamentoPlanta + new Vector2(0f, ajusteFase);
    }

    static Sprite spriteBranco;
    static Sprite SpriteBranco()
    {
        if (!spriteBranco)
        {
            var tex = Texture2D.whiteTexture;
            spriteBranco = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
        return spriteBranco;
    }

    static void Fixar(Image img, Layout l)
    {
        if (!img) return;
        var rt = img.rectTransform;
        rt.pivot = l.pivot;
        rt.anchoredPosition = l.pos;
        rt.sizeDelta = l.tamanho;
        rt.localScale = l.escala;
    }

    // Pos Y (no espaço do medidor) de um nível 0..100%, seguindo a tabela 25/50/75/100.
    // Entre os pontos é linear; o 0% é estimado continuando a distância entre 25 e 50.
    float NivelParaY(float nivel)
    {
        if (yPorNivel == null || yPorNivel.Length < 4) return nivel;
        float[] ys = { yZero, yPorNivel[0], yPorNivel[1], yPorNivel[2], yPorNivel[3] };
        float t = Mathf.Clamp(nivel, 0f, 100f) / 25f; // 0..4
        int i = Mathf.Min((int)t, 3);
        return Mathf.Lerp(ys[i], ys[i + 1], t - i);
    }

    // Coloca o medidor (pivot no centro) na posição do nível necessário da planta
    void Marcar(RectTransform marca, float x, float nivel)
    {
        if (!marca) return;
        marca.pivot = new Vector2(0.5f, 0.5f);
        marca.anchoredPosition = new Vector2(x, NivelParaY(nivel));
    }

    // Quanto a barra precisa estar cheia (0..1) para o topo dela ficar na altura do nível,
    // usando a mesma escala dos medidores. Sem medidor, usa a proporção simples.
    float Preenchimento(Image barra, RectTransform marca, float x, float nivel)
    {
        if (!marca || !(marca.parent is RectTransform pai)) return Mathf.Clamp01(nivel / 100f);

        // Ponto do nível no espaço do pai do medidor (anchoredPosition é relativo à âncora)
        Vector2 ancora = pai.rect.min + Vector2.Scale(marca.anchorMin, pai.rect.size);
        Vector3 mundo = pai.TransformPoint(ancora + new Vector2(x, NivelParaY(nivel)));

        // Converte para o espaço da barra e vê a fração da altura dela
        var rb = barra.rectTransform;
        float yLocal = rb.InverseTransformPoint(mundo).y;
        return Mathf.Clamp01((yLocal - rb.rect.yMin) / rb.rect.height);
    }
}
