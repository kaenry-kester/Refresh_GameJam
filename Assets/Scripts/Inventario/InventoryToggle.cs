using System;
using System.Collections;
using UnityEngine;

// Abre/fecha a barra do inventário usando princípios de animação:
//   Anticipation     -> um pequeno movimento contrário antes da ação principal
//   Squash & Stretch -> estica (comprida e fina) quando rápida, amassa ao parar/colidir
//   Timing           -> duração de cada fase ajustável no Inspector
//   (bônus) Follow-through -> passa um pouco do ponto final e volta com elasticidade
//
// Coloque no botão da mochila e ligue Alternar() no OnClick do próprio botão.
// A barra pode ser filha do botão: o script compensa a escala da mochila.
public class InventoryToggle : MonoBehaviour
{
    public enum Direcao { DireitaHorizontal, BaixoVertical }

    [Header("Referências")]
    [SerializeField] Direcao direcao = Direcao.DireitaHorizontal;
    [SerializeField] RectTransform painel;              // a barra com os slots
    [SerializeField] RectTransform mochila;             // vazio = usa o próprio objeto
    [SerializeField] RectTransform orelha;              // opcional: ponta da barra que fica sempre visível
    [SerializeField, Min(0.1f)] float escalaOrelhaFechada = 1.5f; // tamanho da orelha com a barra recolhida
    [SerializeField] bool comecaAberto = false;

    [Header("Timing - Abrir (segundos)")]
    [SerializeField] float abrirAntecipacao = 0.12f;    // mochila se encolhe, "juntando força"
    [SerializeField] float abrirAcao = 0.16f;           // barra dispara para fora
    [SerializeField] float abrirAssentar = 0.30f;       // barra se acomoda com elasticidade

    [Header("Timing - Fechar (segundos)")]
    [SerializeField] float fecharAntecipacao = 0.10f;   // barra estica um pouco para fora
    [SerializeField] float fecharAcao = 0.14f;          // barra é puxada para dentro
    [SerializeField] float fecharImpacto = 0.30f;       // mochila amassa e volta

    [Header("Intensidade")]
    [SerializeField, Range(0f, 0.4f)] float squashMochila = 0.14f;  // quanto a mochila amassa
    [SerializeField, Range(0f, 0.4f)] float stretchBarra = 0.20f;   // quanto a barra afina em movimento
    [SerializeField, Range(0f, 0.3f)] float overshootBarra = 0.12f; // quanto a barra passa do tamanho final
    [SerializeField, Range(1f, 5f)] float elasticidade = 3f;        // força do "vai e volta" ao assentar

    // Escalas em (principal, cruzado): principal = eixo em que a barra abre; cruzado = perpendicular.
    Vector2 barra = Vector2.one;
    Vector2 bolsa = Vector2.one;
    bool aberto;
    Coroutine anim;

    public bool Aberto => aberto;

    void Awake()
    {
        if (mochila == null) mochila = (RectTransform)transform;

        // Pivô na borda que encosta na mochila, para a barra esticar a partir dela.
        var pivo = direcao == Direcao.DireitaHorizontal ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 1f);
        DefinirPivoMantendoPosicao(painel, pivo);

        // A orelha fica com o pivô na borda que encosta na barra e com as mesmas âncoras da barra,
        // para poder ser posicionada exatamente na ponta dela.
        if (orelha)
        {
            orelha.anchorMin = painel.anchorMin;
            orelha.anchorMax = painel.anchorMax;
            orelha.pivot = pivo;
        }

        aberto = comecaAberto;
        barra = aberto ? Vector2.one : new Vector2(0f, 1f);
        bolsa = Vector2.one;
        Aplicar();
    }

    public void Alternar()
    {
        aberto = !aberto;
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(aberto ? Abrir() : Fechar());
    }

    IEnumerator Abrir()
    {
        float s = squashMochila, st = stretchBarra, o = overshootBarra;

        // 1. ANTECIPAÇÃO: a mochila se comprime no eixo do movimento (junta força), a barra ainda fechada.
        yield return Fase(abrirAntecipacao, new Vector2(0f, 1f), new Vector2(1f - s, 1f + s), EaseInOutSine);

        // 2. AÇÃO + STRETCH: a barra dispara, passa do tamanho final e fica mais fina por estar rápida.
        //    A mochila "solta" o impulso e estica levemente no sentido contrário.
        yield return Fase(abrirAcao, new Vector2(1f + o, 1f - st), new Vector2(1f + s * 0.5f, 1f - s * 0.5f), EaseOutCubic);

        // 3. SQUASH + FOLLOW-THROUGH: a barra volta ao tamanho normal passando um pouco
        //    (fica levemente mais curta e mais grossa) e a mochila se acomoda.
        yield return Fase(abrirAssentar, Vector2.one, Vector2.one, t => EaseOutBack(t, elasticidade));

        anim = null;
    }

    IEnumerator Fechar()
    {
        float s = squashMochila, st = stretchBarra, o = overshootBarra;

        // 1. ANTECIPAÇÃO: antes de ser puxada para dentro, a barra estica um pouco para fora.
        yield return Fase(fecharAntecipacao, new Vector2(1f + o * 0.6f, 1f - st * 0.3f), Vector2.one, EaseOutSine);

        // 2. AÇÃO + STRETCH: a barra é puxada acelerando e afina pela velocidade.
        yield return Fase(fecharAcao, new Vector2(0f, 1f - st), Vector2.one, EaseInCubic);

        // 3. SQUASH (impacto): a mochila "engole" a barra, amassa e volta com elasticidade.
        barra = new Vector2(0f, 1f);
        yield return Fase(fecharImpacto * 0.25f, barra, new Vector2(1f - s, 1f + s), EaseOutQuad);
        yield return Fase(fecharImpacto * 0.75f, barra, Vector2.one, t => EaseOutBack(t, elasticidade));

        anim = null;
    }

    // Interpola barra e mochila do estado atual até os alvos, durante 'duracao' segundos.
    IEnumerator Fase(float duracao, Vector2 alvoBarra, Vector2 alvoBolsa, Func<float, float> ease)
    {
        Vector2 b0 = barra, m0 = bolsa;
        float t = duracao <= 0f ? 1f : 0f;
        while (true)
        {
            float k = ease(t);
            barra = Vector2.LerpUnclamped(b0, alvoBarra, k);
            bolsa = Vector2.LerpUnclamped(m0, alvoBolsa, k);
            Aplicar();
            if (t >= 1f) yield break;
            yield return null;
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime / duracao);
        }
    }

    void Aplicar()
    {
        Vector3 escBolsa = ParaEscala(bolsa);
        mochila.localScale = escBolsa;

        Vector3 escBarra = ParaEscala(new Vector2(Mathf.Max(0f, barra.x), barra.y));
        // Se a barra é filha da mochila, desfaz a escala herdada para os dois animarem de forma independente.
        if (painel.IsChildOf(mochila))
            escBarra = new Vector3(escBarra.x / escBolsa.x, escBarra.y / escBolsa.y, 1f);
        painel.localScale = escBarra;

        AplicarOrelha(escBolsa);
    }

    // Coloca a orelha na ponta da barra. Ela acompanha a espessura da barra (squash & stretch),
    // mas não estica no comprimento, para não deformar.
    // Conforme a barra recolhe, a orelha cresce até 'escalaOrelhaFechada' (e volta a 1 ao abrir).
    void AplicarOrelha(Vector3 escBolsa)
    {
        if (!orelha) return;

        float aberta = Mathf.Clamp01(barra.x); // 0 = recolhida, 1 = aberta
        float tamanho = Mathf.Lerp(escalaOrelhaFechada, 1f, aberta);

        if (direcao == Direcao.DireitaHorizontal)
        {
            float ponta = painel.anchoredPosition.x + painel.rect.width * painel.localScale.x;
            orelha.anchoredPosition = new Vector2(ponta, painel.anchoredPosition.y);
            orelha.localScale = new Vector3(tamanho / escBolsa.x, painel.localScale.y * tamanho, 1f);
        }
        else
        {
            float ponta = painel.anchoredPosition.y - painel.rect.height * painel.localScale.y;
            orelha.anchoredPosition = new Vector2(painel.anchoredPosition.x, ponta);
            orelha.localScale = new Vector3(painel.localScale.x * tamanho, tamanho / escBolsa.y, 1f);
        }
    }

    Vector3 ParaEscala(Vector2 v) =>
        direcao == Direcao.DireitaHorizontal ? new Vector3(v.x, v.y, 1f) : new Vector3(v.y, v.x, 1f);

    static void DefinirPivoMantendoPosicao(RectTransform rt, Vector2 pivo)
    {
        Vector2 delta = pivo - rt.pivot;
        rt.pivot = pivo;
        rt.anchoredPosition += new Vector2(delta.x * rt.rect.width * rt.localScale.x,
                                           delta.y * rt.rect.height * rt.localScale.y);
    }

    // Curvas de easing (t de 0 a 1)
    static float EaseInOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
    static float EaseOutSine(float t)   => Mathf.Sin(t * Mathf.PI / 2f);
    static float EaseOutQuad(float t)   => 1f - (1f - t) * (1f - t);
    static float EaseOutCubic(float t)  => 1f - Mathf.Pow(1f - t, 3f);
    static float EaseInCubic(float t)   => t * t * t;
    static float EaseOutBack(float t, float s)
    {
        float c = s + 1f;
        return 1f + c * Mathf.Pow(t - 1f, 3f) + s * Mathf.Pow(t - 1f, 2f);
    }
}
