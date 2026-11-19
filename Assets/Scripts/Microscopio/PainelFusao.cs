using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Tela de Fusão (cena M2). Coloque no Canvas.
// - Botões da esquerda e da direita (feitos na cena). Cada um é uma planta (ReceitasFusao).
//   Só dá para escolher se o jogador tiver a planta ADULTA no inventário.
// - Escolheu uma planta de um lado -> a mesma fica bloqueada do outro lado e o DNA daquele lado aparece.
// - Com as duas escolhidas, "Fundir": os DNAs se juntam, brilho forte, viram o DNA verde
//   e o botão vira "Coletar". Coletar -> a semente nova (versão "pré") vai para o inventário.
//   As plantas usadas NÃO são gastas.
public class PainelFusao : MonoBehaviour
{
    [System.Serializable]
    public class Opcao
    {
        public Button botao;
        [Tooltip("Semente da espécie. Vazio = descobre pelo texto do botão (ex.: \"Maracujá-nativo\").")]
        public ItemData especie;
    }

    [Header("Botões das plantas")]
    [SerializeField] Opcao[] esquerda = new Opcao[3];
    [SerializeField] Opcao[] direita = new Opcao[3];
    [SerializeField] Color corSelecionada = new Color(0.6f, 1f, 0.55f, 1f);

    [Header("Custo")]
    [SerializeField] int precoFusao = 100;
    [SerializeField] string textoSemDinheiro = "Dinheiro insuficiente! Cada fusão custa R$ {0},00.";

    [Header("DNA")]
    [Tooltip("Objeto do tamanho do círculo, com os DNAs dentro. Vira uma máscara redonda: nada aparece fora dele.")]
    [SerializeField] RectTransform mascaraCirculo;
    [SerializeField] RectTransform dnaEsquerda;  // aparece ao escolher a planta da esquerda
    [SerializeField] RectTransform dnaDireita;
    [SerializeField] RectTransform dnaResultado; // DNA verde (aparece depois da fusão)
    [SerializeField] float duracaoJuntar = 1.2f;
    [SerializeField] float duracaoBrilho = 0.8f;
    [Tooltip("Opcional: imagem do brilho. Vazio = cria um brilho redondo sozinho.")]
    [SerializeField] Image brilho;
    [SerializeField] Color corBrilho = new Color(1f, 1f, 0.85f, 1f);
    [SerializeField] float tamanhoBrilho = 600f;

    [Header("Botões")]
    [SerializeField] Button botaoFundir;
    [SerializeField] string textoFundir = "Fundir";
    [SerializeField] string textoColetar = "Coletar";
    [SerializeField] Button botaoVoltar;
    [SerializeField] string cenaVoltar = "GMP_Scene_Center";

    [Header("Mensagem (opcional)")]
    [SerializeField] TMP_Text texto;
    [SerializeField] string textoInicial = "Selecione duas plantas para realizar a fusão e gerar uma nova espécie.";
    [SerializeField] string formatoResultado = "Nova espécie: {0}!\nLeve a semente à geladeira até ela ficar pronta.";
    [SerializeField] string textoNaoFunde = "Essas plantas não podem se fundir.";
    [SerializeField] string textoInventarioCheio = "Inventário cheio! Libere um espaço.";

    [Header("Sons (opcional)")]
    [SerializeField] AudioClip somFusao;
    [SerializeField] AudioClip somColetar;
    [SerializeField, Range(0f, 1f)] float volume = 1f;

    [Header("Teste")]
    [Tooltip("Liga para testar sem ter as plantas adultas no inventário")]
    [SerializeField] bool ignorarInventario = false;

    enum Estado { Escolhendo, Fundindo, Coletar }
    Estado estado = Estado.Escolhendo;
    ItemData selE, selD, pronta;
    Vector2 posE, posD;
    Vector3 escE, escD, escResultado;
    Quaternion rotE, rotD;
    TMP_Text textoBotaoFundir;
    AudioSource fonte;

    void Start()
    {
        var receitas = ReceitasFusao.Atual;
        Preparar(esquerda, true, receitas);
        Preparar(direita, false, receitas);

        if (dnaEsquerda) { posE = dnaEsquerda.anchoredPosition; escE = dnaEsquerda.localScale; rotE = dnaEsquerda.localRotation; }
        if (dnaDireita) { posD = dnaDireita.anchoredPosition; escD = dnaDireita.localScale; rotD = dnaDireita.localRotation; }
        if (dnaResultado) escResultado = dnaResultado.localScale;
        if (brilho) { brilho.raycastTarget = false; brilho.gameObject.SetActive(false); }
        CriarMascara();

        if (botaoFundir)
        {
            textoBotaoFundir = botaoFundir.GetComponentInChildren<TMP_Text>(true);
            botaoFundir.onClick.AddListener(CliqueFundir);
        }
        if (botaoVoltar) botaoVoltar.onClick.AddListener(() => SceneManager.LoadScene(cenaVoltar));

        fonte = gameObject.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.spatialBlend = 0f;

        Inventario.Mudou += Atualizar;
        Reiniciar();
    }

    void OnDestroy() => Inventario.Mudou -= Atualizar;

    void Preparar(Opcao[] lado, bool ladoEsquerdo, ReceitasFusao receitas)
    {
        foreach (var o in lado)
        {
            if (o == null || !o.botao) continue;
            if (!o.especie && receitas) o.especie = PeloTexto(o.botao, receitas);
            if (!o.especie) Debug.LogWarning($"PainelFusao: não achei a planta do botão '{o.botao.name}'. Preencha o campo Especie.");
            var opcao = o;
            o.botao.onClick.AddListener(() => Escolher(opcao.especie, ladoEsquerdo));
        }
    }

    // "Maracujá-\nnativo" == "Maracujá nativo" (compara só as letras)
    static ItemData PeloTexto(Button b, ReceitasFusao receitas)
    {
        var t = b.GetComponentInChildren<TMP_Text>(true);
        if (!t) return null;
        string alvo = SoLetras(t.text);
        foreach (var p in receitas.Plantas())
            if (SoLetras(string.IsNullOrEmpty(p.nome) ? p.name : p.nome) == alvo) return p;
        return null;
    }

    static string SoLetras(string s)
    {
        var sb = new System.Text.StringBuilder();
        foreach (char c in s) if (char.IsLetter(c)) sb.Append(char.ToLowerInvariant(c));
        return sb.ToString();
    }

    // ---- Seleção ----

    void Escolher(ItemData especie, bool ladoEsquerdo)
    {
        if (estado != Estado.Escolhendo || !especie) return;
        if (ladoEsquerdo) selE = selE == especie ? null : especie; // clicar de novo desmarca
        else selD = selD == especie ? null : especie;
        Mensagem(textoInicial);
        Atualizar();
    }

    void Atualizar()
    {
        if (estado == Estado.Escolhendo)
        {
            if (selE && !TemAdulta(selE)) selE = null;
            if (selD && !TemAdulta(selD)) selD = null;
            if (dnaEsquerda) dnaEsquerda.gameObject.SetActive(selE);
            if (dnaDireita) dnaDireita.gameObject.SetActive(selD);
        }

        AtualizarLado(esquerda, selE, selD);
        AtualizarLado(direita, selD, selE);

        if (botaoFundir)
            botaoFundir.interactable = estado == Estado.Coletar || (estado == Estado.Escolhendo && selE && selD);
        if (textoBotaoFundir) textoBotaoFundir.text = estado == Estado.Coletar ? textoColetar : textoFundir;
    }

    void AtualizarLado(Opcao[] lado, ItemData selecionada, ItemData doOutroLado)
    {
        foreach (var o in lado)
        {
            if (o == null || !o.botao) continue;
            bool sel = o.especie && o.especie == selecionada;
            o.botao.interactable = sel || (estado == Estado.Escolhendo && o.especie &&
                                           TemAdulta(o.especie) && o.especie != doOutroLado);
            if (o.botao.image) o.botao.image.color = sel ? corSelecionada : Color.white;
        }
    }

    bool TemAdulta(ItemData especie)
    {
        if (ignorarInventario) return true;
        foreach (var e in Inventario.Slots)
            if (e != null && e.item && e.item.EhPlantaColhida && e.item.Especie == especie) return true;
        return false;
    }

    // ---- Fundir / Coletar ----

    void CliqueFundir()
    {
        if (estado == Estado.Coletar) { Coletar(); return; }
        if (estado != Estado.Escolhendo || !selE || !selD) return;

        var receitas = ReceitasFusao.Atual;
        pronta = receitas ? receitas.Resultado(selE, selD) : null;
        if (!pronta) { Mensagem(textoNaoFunde); return; }
        if (!CabeNoInventario(pronta.SementePre())) { Mensagem(textoInventarioCheio); return; }
        if (!Dinheiro.Gastar(precoFusao)) { Mensagem(string.Format(textoSemDinheiro, precoFusao)); return; }

        StartCoroutine(Animacao());
    }

    IEnumerator Animacao()
    {
        estado = Estado.Fundindo;
        Atualizar();
        Tocar(somFusao);

        // 1) Os dois DNAs vão para o meio girando e diminuindo
        Vector2 meio = (posE + posD) * 0.5f;
        if (dnaResultado) meio = PosicaoEm(dnaResultado, dnaEsquerda ? dnaEsquerda : dnaResultado);
        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(duracaoJuntar, 0.01f))
        {
            float k = t * t * (3f - 2f * t); // suave
            Mover(dnaEsquerda, posE, meio, escE, rotE, k, 1f);
            Mover(dnaDireita, posD, meio, escD, rotD, k, -1f);
            yield return null;
        }

        // 2) Brilho forte; no auge os DNAs viram o DNA verde
        var luz = PegarBrilho();
        bool trocou = false;
        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(duracaoBrilho, 0.01f))
        {
            float a = t < 0.4f ? t / 0.4f : 1f - (t - 0.4f) / 0.6f; // acende rápido, apaga devagar
            var c = corBrilho; c.a = a; luz.color = c;
            luz.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.2f, t);

            if (!trocou && t >= 0.4f)
            {
                trocou = true;
                if (dnaEsquerda) dnaEsquerda.gameObject.SetActive(false);
                if (dnaDireita) dnaDireita.gameObject.SetActive(false);
                if (dnaResultado) dnaResultado.gameObject.SetActive(true);
            }
            if (dnaResultado && trocou)
                dnaResultado.localScale = escResultado * (1f + 0.25f * Mathf.Sin(Mathf.Clamp01((t - 0.4f) / 0.6f) * Mathf.PI));
            yield return null;
        }
        luz.gameObject.SetActive(false);
        if (dnaResultado) dnaResultado.localScale = escResultado;

        // 3) Botão vira "Coletar"
        estado = Estado.Coletar;
        Mensagem(string.Format(formatoResultado, string.IsNullOrEmpty(pronta.nome) ? pronta.name : pronta.nome.Trim()));
        Atualizar();
    }

    void Coletar()
    {
        var pre = pronta.SementePre();
        if (!Inventario.Adicionar(pre)) { Mensagem(textoInventarioCheio); return; }
        Tocar(somColetar);
        Reiniciar();
    }

    // Volta tudo ao começo: nada no círculo, nenhuma planta escolhida
    void Reiniciar()
    {
        estado = Estado.Escolhendo;
        selE = selD = pronta = null;
        if (dnaEsquerda) { Mover(dnaEsquerda, posE, posE, escE, rotE, 0f, 0f); dnaEsquerda.gameObject.SetActive(false); }
        if (dnaDireita) { Mover(dnaDireita, posD, posD, escD, rotD, 0f, 0f); dnaDireita.gameObject.SetActive(false); }
        if (dnaResultado) { dnaResultado.localScale = escResultado; dnaResultado.gameObject.SetActive(false); }
        Mensagem(textoInicial);
        Atualizar();
    }

    static void Mover(RectTransform rt, Vector2 de, Vector2 para, Vector3 esc, Quaternion rot, float k, float giro)
    {
        if (!rt) return;
        rt.anchoredPosition = Vector2.LerpUnclamped(de, para, k);
        rt.localScale = esc * Mathf.Lerp(1f, 0.6f, k);
        rt.localRotation = rot * Quaternion.Euler(0f, 0f, 360f * k * giro);
    }

    // Posição de "alvo" no mesmo espaço de anchoredPosition de "referencia"
    static Vector2 PosicaoEm(RectTransform alvo, RectTransform referencia)
    {
        var pai = referencia.parent as RectTransform;
        if (!pai) return referencia.anchoredPosition;
        Vector2 local = pai.InverseTransformPoint(alvo.position);
        // anchoredPosition = local - ponto da âncora
        Vector2 ancora = Vector2.Scale(pai.rect.size, (referencia.anchorMin + referencia.anchorMax) * 0.5f) + pai.rect.min;
        return local - ancora;
    }

    Image PegarBrilho()
    {
        if (!brilho)
        {
            var go = new GameObject("Brilho", typeof(RectTransform), typeof(Image));
            brilho = go.GetComponent<Image>();
            brilho.raycastTarget = false;
            brilho.sprite = CriarCirculoSuave();
            var alvo = dnaResultado ? dnaResultado : (dnaEsquerda ? dnaEsquerda : (RectTransform)transform);
            brilho.rectTransform.SetParent(alvo.parent, false);
            brilho.rectTransform.position = alvo.position;
            brilho.rectTransform.sizeDelta = Vector2.one * tamanhoBrilho;
            brilho.rectTransform.SetAsLastSibling(); // por cima dos DNAs
        }
        brilho.gameObject.SetActive(true);
        return brilho;
    }

    // Transforma o objeto do círculo numa máscara redonda (a forma não aparece, só corta o que passar dela)
    void CriarMascara()
    {
        if (!mascaraCirculo) return;
        if (!mascaraCirculo.TryGetComponent(out Image img)) img = mascaraCirculo.gameObject.AddComponent<Image>();
        img.sprite = CriarCirculo();
        img.color = Color.white;
        img.raycastTarget = false;
        if (!mascaraCirculo.TryGetComponent(out Mask m)) m = mascaraCirculo.gameObject.AddComponent<Mask>();
        m.showMaskGraphic = false;
    }

    static Sprite CriarCirculo()
    {
        const int n = 256;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[n * n];
        float r = n / 2f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(r - d)); // borda lisa
            }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
    }

    static Sprite CriarCirculoSuave()
    {
        const int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                float a = Mathf.Clamp01(1f - d);
                px[y * n + x] = new Color(1f, 1f, 1f, a * a);
            }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
    }

    static bool CabeNoInventario(ItemData item)
    {
        if (Inventario.Quantidade(item) > 0) return true;
        foreach (var e in Inventario.Slots) if (e == null) return true;
        return false;
    }

    void Mensagem(string t)
    {
        if (texto) texto.text = t;
    }

    void Tocar(AudioClip clip)
    {
        if (clip && fonte) fonte.PlayOneShot(clip, volume);
    }
}
