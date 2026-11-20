using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Tela de Reconstrução (cena M1). Coloque no Canvas.
// - Pinça da DIREITA = cursor do mouse. Botão esquerdo segurado = fechada; soltou = aberta.
// - Pinça da ESQUERDA = W A S D. Tecla E fecha / abre (alterna).
// - Fechar a pinça em cima de uma peça (DNA quebrado ou pedaço) pega a peça; abrir solta.
// - Encaixou o pedaço no buraco do DNA -> o DNA sai da tela sozinho, a semente "pré" vai para o
//   inventário (depois: geladeira 8s -> pronta para plantar) e volta para a GMP_Scene_Center.
// Montagem: deixe o PEDAÇO encaixado certinho no DNA na cena. O script grava esse encaixe
// e, ao começar, separa o pedaço (para o jogador montar).
public class PainelReconstrucao : MonoBehaviour
{
    [Header("Círculo do microscópio")]
    [Tooltip("Objeto do tamanho do círculo, com as pinças e as peças DENTRO dele. Vira máscara redonda e as pinças não saem dele.")]
    [SerializeField] RectTransform circulo;

    [Header("Pinças (Images)")]
    [SerializeField] RectTransform pincaMouse;   // direita
    [SerializeField] RectTransform pincaTeclado; // esquerda (fica espelhada)
    [SerializeField] Sprite spriteAberta;        // "Pinças com Hélice de DNA 1_3"
    [SerializeField] Sprite spriteFechada;       // "Pinças com Hélice de DNA 1_0"
    [Tooltip("Ponta da pinça dentro do sprite (0..1). É o ponto que pega as peças.")]
    [SerializeField] Vector2 pontaAberta = new Vector2(0.124f, 0.2f);
    [SerializeField] Vector2 pontaFechada = new Vector2(0.117f, 0.18f);
    [Tooltip("Comprimento da pinça em relação ao diâmetro do círculo (0.3 = 30% do círculo)")]
    [SerializeField, Range(0.1f, 0.6f)] float comprimentoPinca = 0.3f;
    [Tooltip("Velocidade máxima da pinça do teclado (W A S D)")]
    [SerializeField] float velocidadeMaxTeclado = 350f;
    [Tooltip("Quão rápido ela acelera e freia (maior = mais firme)")]
    [SerializeField] float aceleracaoTeclado = 2500f;
    [Tooltip("Segurando Shift: velocidade multiplicada por isto (movimento fino)")]
    [SerializeField, Range(0.1f, 1f)] float fatorPreciso = 0.35f;
    Vector2 velTeclado;
    [SerializeField] KeyCode teclaFechar = KeyCode.E;

    [Header("Peças (o script monta sozinho: tamanho, posição e encaixe)")]
    [SerializeField] RectTransform dnaQuebrado; // Image com o sprite "dna quebrado"
    [SerializeField] RectTransform pedaco;      // Image com o sprite "dna pedacinho res"
    [Tooltip("Altura do DNA em relação ao diâmetro do círculo (0.5 = metade do círculo)")]
    [SerializeField, Range(0.2f, 0.9f)] float alturaDnaNoCirculo = 0.5f;
    [Tooltip("Onde fica o buraco do DNA, em relação ao centro do círculo")]
    [SerializeField] Vector2 posicaoBuraco = new Vector2(-60f, 40f);
    [Tooltip("Onde o pedaço começa, em relação ao centro do círculo")]
    [SerializeField] Vector2 posicaoInicialPedaco = new Vector2(170f, -170f);
    [Tooltip("Onde a pinça do teclado começa, em relação ao centro do círculo")]
    [SerializeField] Vector2 posicaoInicialPincaTeclado = new Vector2(-200f, -150f);
    [Tooltip("Centro do pedaço encaixado, em pixels do desenho do DNA (a partir do canto de cima, à esquerda). Calculado das imagens.")]
    [SerializeField] Vector2 encaixeEmPixels = new Vector2(596f, 2636f);
    [Tooltip("Quão perto o pedaço precisa chegar do buraco para encaixar")]
    [SerializeField] float distanciaEncaixe = 15f;

    [Header("Final")]
    [SerializeField] float duracaoSaida = 1.2f;
    [SerializeField] bool gastarFossil = true;
    [SerializeField] string cenaVoltar = "GMP_Scene_Center";
    [SerializeField] float esperaAntesDeVoltar = 2f;
    [SerializeField] TMP_Text texto; // opcional
    [SerializeField] string formatoResultado = "DNA reconstruído!\nA semente de {0} foi para o inventário. Leve-a à geladeira.";
    [SerializeField] string textoInventarioCheio = "Inventário cheio! Libere um espaço.";

    [Header("Sons (opcional)")]
    [SerializeField] AudioClip somPegar;
    [SerializeField] AudioClip somEncaixe;
    [SerializeField, Range(0f, 1f)] float volume = 1f;

    [Header("Teste (opcional)")]
    [Tooltip("Só para testar dando Play direto nesta cena. Vazio = sem fóssil a tela volta na hora para a GMP_Scene_Center.")]
    [SerializeField] ItemData fossilTeste;

    class Pinca
    {
        public RectTransform rt;
        public Image img;
        public bool fechada;
        public RectTransform segurando;
        public Vector3 offset;
    }

    Pinca direita, esquerda;
    ItemData fossil; // fóssil usado nesta reconstrução
    Canvas canvas;
    Vector3 encaixeLocal; // posição do pedaço encaixado, relativa ao DNA
    bool terminou;
    AudioSource fonte;

    Camera Cam => canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
    RectTransform CanvasRT => (RectTransform)canvas.transform;

    void Start()
    {
        canvas = GetComponentInParent<Canvas>().rootCanvas;

        // Só entra com um fóssil (arrastado do inventário até o microscópio). Sem fóssil: volta.
        fossil = Microscopio.FossilSelecionado ? Microscopio.FossilSelecionado : fossilTeste;
        bool veioDoMicroscopio = Microscopio.FossilSelecionado;
        if (!fossil || !fossil.ehFossil || !fossil.sementeReconstruida ||
            (veioDoMicroscopio && Inventario.Quantidade(fossil) <= 0))
        {
            if (fossil && !fossil.sementeReconstruida)
                Debug.LogWarning($"PainelReconstrucao: o fóssil '{fossil.name}' está sem 'Semente Reconstruida'.");
            terminou = true;
            Voltar();
            return;
        }
        fonte = gameObject.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.spatialBlend = 0f;

        CriarMascara();
        MontarPecas();
        direita = Criar(pincaMouse, false);
        esquerda = Criar(pincaTeclado, true);
        if (esquerda != null) { esquerda.rt.position = NoCirculo(posicaoInicialPincaTeclado); ManterDentro(esquerda.rt); }
        if (direita != null) { direita.rt.position = NoCirculo(Vector2.zero); ManterDentro(direita.rt); }
        ManterDentro(dnaQuebrado);
        ManterDentro(pedaco);
        Cursor.visible = false; // a pinça da direita é o cursor
    }

    // Posiciona DNA e pedaço sozinho, no tamanho certo um do outro, e grava o ponto de encaixe
    void MontarPecas()
    {
        if (!dnaQuebrado || !pedaco) { Debug.LogError("PainelReconstrucao: ligue Dna Quebrado e Pedaco."); return; }
        var imgDna = dnaQuebrado.GetComponent<Image>();
        var imgPed = pedaco.GetComponent<Image>();
        if (!imgDna || !imgDna.sprite || !imgPed || !imgPed.sprite)
        {
            Debug.LogError("PainelReconstrucao: coloque o sprite 'dna quebrado' no DNA e 'dna pedacinho res' no pedaço (Source Image).");
            return;
        }

        Vector2 tamDna = imgDna.sprite.rect.size;
        Vector2 tamPed = imgPed.sprite.rect.size;
        float k = alturaDnaNoCirculo * Diametro / tamDna.y; // pixels do desenho -> unidades (mesma escala para os dois)

        foreach (var (rt, img, tam) in new[] { (dnaQuebrado, imgDna, tamDna), (pedaco, imgPed, tamPed) })
        {
            img.preserveAspect = false;
            img.raycastTarget = false;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.sizeDelta = tam * k;
        }

        // Ponto do encaixe dentro do DNA (pivô no centro)
        encaixeLocal = new Vector3((encaixeEmPixels.x - tamDna.x / 2f) * k, (tamDna.y / 2f - encaixeEmPixels.y) * k, 0f);

        // DNA posicionado com o buraco no lugar escolhido; pedaço separado
        dnaQuebrado.position = NoCirculo(posicaoBuraco) - dnaQuebrado.TransformVector(encaixeLocal);
        pedaco.position = NoCirculo(posicaoInicialPedaco);
        dnaQuebrado.SetAsFirstSibling(); // DNA atrás do pedaço e das pinças
    }

    // Ponto (em relação ao centro do círculo) -> posição no mundo
    Vector3 NoCirculo(Vector2 p)
    {
        if (circulo) return circulo.TransformPoint((Vector3)(circulo.rect.center + p));
        return CanvasRT.TransformPoint((Vector3)p);
    }

    // O círculo vira máscara redonda: o que passar da borda não aparece
    void CriarMascara()
    {
        if (!circulo) return;
        if (!circulo.TryGetComponent(out Image img)) img = circulo.gameObject.AddComponent<Image>();
        const int n = 256;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[n * n];
        float r = n / 2f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(r - d));
            }
        tex.SetPixels(px);
        tex.Apply();
        img.sprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        img.color = Color.white;
        img.raycastTarget = false;
        if (!circulo.TryGetComponent(out Mask m)) m = circulo.gameObject.AddComponent<Mask>();
        m.showMaskGraphic = false;
    }

    // Mantém um ponto (mundo) dentro do círculo. Sem círculo: dentro da tela.
    Vector3 Limitar(Vector3 mundo)
    {
        if (circulo)
        {
            Vector3 local = circulo.InverseTransformPoint(mundo);
            Vector2 centro = circulo.rect.center;
            float raio = Mathf.Min(circulo.rect.width, circulo.rect.height) / 2f;
            Vector2 d = (Vector2)local - centro;
            if (d.magnitude > raio) d = d.normalized * raio;
            local = new Vector3(centro.x + d.x, centro.y + d.y, local.z);
            return circulo.TransformPoint(local);
        }
        Vector3 l = CanvasRT.InverseTransformPoint(mundo);
        Rect r = CanvasRT.rect;
        l.x = Mathf.Clamp(l.x, r.xMin, r.xMax);
        l.y = Mathf.Clamp(l.y, r.yMin, r.yMax);
        return CanvasRT.TransformPoint(l);
    }

    // Diâmetro do círculo (unidades de dentro dele). Sem círculo: 700.
    float Diametro => circulo ? Mathf.Min(circulo.rect.width, circulo.rect.height) : 700f;

    // Coloca o objeto o mais perto possível de "desejado" (mundo) com ele INTEIRO dentro do círculo.
    // Desliza suave pela borda (sem trancos): cada canto do objeto precisa ficar dentro do círculo,
    // então a posição permitida é uma região convexa e projetamos o ponto desejado nela.
    readonly Vector3[] cantos = new Vector3[4];
    readonly Vector2[] rel = new Vector2[4];

    void ColocarDentro(RectTransform rt, Vector3 desejado)
    {
        if (!rt) return;
        if (!circulo) { rt.position = desejado; return; }

        float raio = Diametro / 2f - 0.5f;
        Vector2 centro = circulo.rect.center;

        // Cantos em relação ao pivô (não mudam ao mover)
        rt.GetWorldCorners(cantos);
        Vector2 pivo = circulo.InverseTransformPoint(rt.position);
        Vector2 media = Vector2.zero;
        for (int i = 0; i < 4; i++) { rel[i] = (Vector2)circulo.InverseTransformPoint(cantos[i]) - pivo; media += rel[i] / 4f; }

        Vector3 local3 = circulo.InverseTransformPoint(desejado);
        Vector2 p = local3;
        if (!Cabe(p, centro, raio))
        {
            // Projeções alternadas nos 4 círculos permitidos (um por canto)
            for (int volta = 0; volta < 12; volta++)
                for (int i = 0; i < 4; i++)
                {
                    Vector2 c = centro - rel[i];
                    Vector2 d = p - c;
                    if (d.magnitude > raio) p = c + d.normalized * raio;
                }

            // Segurança: se ainda não coube, procura no caminho a partir do centro
            if (!Cabe(p, centro, raio))
            {
                Vector2 seguro = centro - media, alvo = local3;
                float a = 0f, b = 1f;
                for (int k = 0; k < 14; k++) { float m = (a + b) / 2f; if (Cabe(Vector2.Lerp(seguro, alvo, m), centro, raio)) a = m; else b = m; }
                p = Vector2.Lerp(seguro, alvo, a);
            }
        }
        rt.position = circulo.TransformPoint(new Vector3(p.x, p.y, local3.z));
    }

    bool Cabe(Vector2 p, Vector2 centro, float raio)
    {
        for (int i = 0; i < 4; i++)
            if ((p + rel[i] - centro).sqrMagnitude > (raio + 0.01f) * (raio + 0.01f)) return false;
        return true;
    }

    void ManterDentro(RectTransform rt) { if (rt) ColocarDentro(rt, rt.position); }

    void OnDisable() => Cursor.visible = true;

    Pinca Criar(RectTransform rt, bool espelhada)
    {
        if (!rt) return null;
        var p = new Pinca { rt = rt, img = rt.GetComponent<Image>() };
        if (p.img) { p.img.raycastTarget = false; p.img.preserveAspect = true; }
        var s = rt.localScale;
        rt.localScale = new Vector3(Mathf.Abs(s.x) * (espelhada ? -1f : 1f), s.y, s.z);
        rt.SetAsLastSibling(); // pinças por cima das peças
        Visual(p);
        return p;
    }

    void Visual(Pinca p)
    {
        var spr = p.fechada ? spriteFechada : spriteAberta;
        if (!spr || !p.img) return;
        p.img.sprite = spr;
        p.rt.pivot = p.fechada ? pontaFechada : pontaAberta; // a ponta fica sempre no mesmo lugar
        p.rt.sizeDelta = spr.rect.size * (comprimentoPinca * Diametro / spr.rect.width);
        ManterDentro(p.rt);
    }

    void Update()
    {
        if (terminou) return;

        // Pinça direita: segue o mouse; botão esquerdo segurado = fechada
        if (direita != null)
        {
            // Ponta da pinça exatamente no mouse (sem atraso); na borda, desliza
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(CanvasRT, Input.mousePosition, Cam, out Vector3 m))
                ColocarDentro(direita.rt, m);
            Fechar(direita, Input.GetMouseButton(0));
        }

        // Pinça esquerda: WASD; E alterna
        if (esquerda != null)
        {
            Vector2 dir = Vector2.zero;
            if (Input.GetKey(KeyCode.W)) dir.y += 1f;
            if (Input.GetKey(KeyCode.S)) dir.y -= 1f;
            if (Input.GetKey(KeyCode.D)) dir.x += 1f;
            if (Input.GetKey(KeyCode.A)) dir.x -= 1f;
            // Aceleração/freio suaves; Shift = movimento lento e preciso
            bool preciso = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            Vector2 alvoVel = dir.normalized * velocidadeMaxTeclado * (preciso ? fatorPreciso : 1f);
            velTeclado = Vector2.MoveTowards(velTeclado, alvoVel, aceleracaoTeclado * Time.deltaTime);
            if (velTeclado.sqrMagnitude > 0.0001f)
            {
                Vector3 antes = esquerda.rt.position;
                Vector3 local = CanvasRT.InverseTransformPoint(antes);
                local += (Vector3)(velTeclado * Time.deltaTime);
                ColocarDentro(esquerda.rt, CanvasRT.TransformPoint(local));
                if ((esquerda.rt.position - antes).sqrMagnitude < 1e-8f) velTeclado = Vector2.zero; // bateu na borda
            }
            if (Input.GetKeyDown(teclaFechar)) Fechar(esquerda, !esquerda.fechada);
        }

        // Peças seguras acompanham a ponta da pinça
        foreach (var p in new[] { direita, esquerda })
            if (p != null && p.segurando)
                ColocarDentro(p.segurando, p.rt.position + p.offset); // peça também não sai do círculo

        VerificarEncaixe();
    }

    void Fechar(Pinca p, bool fechar)
    {
        if (p.fechada == fechar) return;
        p.fechada = fechar;
        Visual(p);

        if (!fechar) { p.segurando = null; return; } // abriu: solta

        // Fechou: pega a peça que estiver na ponta (o pedaço tem prioridade, é menor)
        Vector2 tela = RectTransformUtility.WorldToScreenPoint(Cam, p.rt.position);
        foreach (var peca in new[] { pedaco, dnaQuebrado })
        {
            if (!peca || Segurada(peca)) continue;
            if (!RectTransformUtility.RectangleContainsScreenPoint(peca, tela, Cam)) continue;
            p.segurando = peca;
            p.offset = peca.position - p.rt.position;
            Tocar(somPegar);
            break;
        }
    }

    bool Segurada(RectTransform peca) =>
        (direita != null && direita.segurando == peca) || (esquerda != null && esquerda.segurando == peca);

    void VerificarEncaixe()
    {
        if (!dnaQuebrado || !pedaco) return;
        Vector3 alvo = dnaQuebrado.TransformPoint(encaixeLocal);
        float dist = CanvasRT.InverseTransformVector(pedaco.position - alvo).magnitude;
        if (dist <= distanciaEncaixe) StartCoroutine(Concluir(alvo));
    }

    IEnumerator Concluir(Vector3 alvo)
    {
        terminou = true;
        if (direita != null) direita.segurando = null;
        if (esquerda != null) esquerda.segurando = null;
        Tocar(somEncaixe);

        // Encaixa suave e prende o pedaço no DNA
        Vector3 de = pedaco.position;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.15f)
        {
            pedaco.position = Vector3.Lerp(de, dnaQuebrado.TransformPoint(encaixeLocal), t);
            yield return null;
        }
        pedaco.SetParent(dnaQuebrado, true);
        pedaco.position = dnaQuebrado.TransformPoint(encaixeLocal);
        yield return new WaitForSeconds(0.4f);

        // DNA completo sai da tela (para cima)
        Vector3 inicio = dnaQuebrado.localPosition;
        float subir = CanvasRT.rect.height + dnaQuebrado.rect.height * Mathf.Abs(dnaQuebrado.localScale.y);
        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(duracaoSaida, 0.01f))
        {
            dnaQuebrado.localPosition = inicio + Vector3.up * subir * (t * t);
            yield return null;
        }
        dnaQuebrado.gameObject.SetActive(false);

        // Recompensa: o fóssil vira a semente "pré" dele, direto no inventário
        var semente = fossil.sementeReconstruida;
        bool gastou = gastarFossil && Microscopio.FossilSelecionado && Inventario.Remover(fossil, 1);
        if (Inventario.Adicionar(semente.SementePre()))
            Mensagem(string.Format(formatoResultado, string.IsNullOrEmpty(semente.nome) ? semente.name : semente.nome.Trim()));
        else
        {
            if (gastou) Inventario.Adicionar(fossil); // não coube: devolve o fóssil
            Mensagem(textoInventarioCheio);
        }
        Microscopio.Limpar();

        yield return new WaitForSeconds(esperaAntesDeVoltar);
        Voltar();
    }

    void Voltar()
    {
        Cursor.visible = true;
        if (!string.IsNullOrEmpty(cenaVoltar)) SceneManager.LoadScene(cenaVoltar);
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
