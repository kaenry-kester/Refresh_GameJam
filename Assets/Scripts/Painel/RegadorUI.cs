using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

// Regador do painel do vaso (uma Image de UI ao lado do vaso grande).
// Arraste até em cima do vaso grande: a água da planta aberta no painel sobe, com o som tocando.
// Tirou de cima ou encheu -> o som para na hora. Soltou -> volta para o lugar.
public class RegadorUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] RectTransform alvo;              // área do vaso grande (ex.: a Image do vaso ou da planta)
    [SerializeField] AudioClip somRegar;              // Assets/Som/regador
    [SerializeField, Range(0f, 1f)] float volume = 1f;
    [SerializeField] float duracaoVoltar = 0.25f;

    RectTransform rt;
    Canvas canvas;
    Vector2 origem; // anchoredPosition original (não depende do tamanho da tela)
    Vector2 mouse;
    bool arrastando;
    AudioSource fonte;

    Camera Cam => canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

    void Awake()
    {
        rt = (RectTransform)transform;
        canvas = GetComponentInParent<Canvas>().rootCanvas;
        origem = rt.anchoredPosition;
    }

    void OnDisable()
    {
        arrastando = false;
        PararSom();
        if (rt) rt.anchoredPosition = origem;
    }

    public void OnBeginDrag(PointerEventData ev)
    {
        StopAllCoroutines();
        arrastando = true;
        rt.SetAsLastSibling(); // por cima do vaso grande
        OnDrag(ev);
    }

    public void OnDrag(PointerEventData ev)
    {
        mouse = ev.position;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            (RectTransform)canvas.transform, ev.position, Cam, out Vector3 p);
        rt.position = p;
    }

    public void OnEndDrag(PointerEventData ev)
    {
        arrastando = false;
        PararSom();
        StartCoroutine(Voltar());
    }

    void Update()
    {
        if (!arrastando) return;

        var vaso = PainelVaso.Instancia ? PainelVaso.Instancia.VasoAtual : null;
        bool emCima = alvo && RectTransformUtility.RectangleContainsScreenPoint(alvo, mouse, Cam);

        if (vaso && emCima && vaso.Regando(Time.deltaTime)) TocarSom();
        else PararSom();
    }

    IEnumerator Voltar()
    {
        Vector2 p0 = rt.anchoredPosition;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duracaoVoltar)
        {
            rt.anchoredPosition = Vector2.Lerp(p0, origem, 1f - Mathf.Pow(1f - t, 3f));
            yield return null;
        }
        rt.anchoredPosition = origem;
    }

    void TocarSom()
    {
        if (!somRegar) return;
        if (!fonte)
        {
            fonte = gameObject.AddComponent<AudioSource>();
            fonte.playOnAwake = false;
            fonte.spatialBlend = 0f;
            fonte.loop = true;
        }
        fonte.clip = somRegar;
        fonte.volume = volume;
        if (!fonte.isPlaying) fonte.Play();
    }

    void PararSom()
    {
        if (fonte && fonte.isPlaying) fonte.Stop();
    }
}
