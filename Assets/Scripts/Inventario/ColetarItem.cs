using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

// Item na tela (UI) que pode ser arrastado para o inventário.
//   - Arraste o item: perto de um slot, o "ímã" puxa ele para o slot.
//   - Soltou dentro do raio do ímã -> voa para dentro do slot e entra no inventário.
//   - Soltou longe -> volta para onde estava.
// Coloque num objeto de UI com Image (ex.: Teste_Semente). Não precisa de Button.
public class ColetarItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] ItemData item;
    [SerializeField] int quantidade = 1;
    [SerializeField] bool coletarComClique = false; // true = comportamento antigo (clicou, foi pro inventário)

    [Header("Ímã")]
    [SerializeField] float raioIma = 90f;                    // distância (pixels de tela) em que o slot começa a puxar
    [SerializeField, Range(0f, 1f)] float forcaIma = 0.6f;   // quanto o item é puxado para o slot enquanto arrasta
    [SerializeField] float duracaoEntrar = 0.18f;            // tempo para "cair" dentro do slot
    [SerializeField] float duracaoVoltar = 0.25f;            // tempo para voltar quando solta longe

    RectTransform rt;
    Canvas canvas;
    Transform paiOriginal;
    int ordemOriginal;
    Vector3 posOriginal;
    bool animando;

    void Awake()
    {
        rt = (RectTransform)transform;
        canvas = GetComponentInParent<Canvas>().rootCanvas;
    }

    // Mantido para quem ainda liga no OnClick de um Button
    public void Coletar()
    {
        if (coletarComClique && Inventario.Adicionar(item, quantidade))
            gameObject.SetActive(false);
    }

    public void OnBeginDrag(PointerEventData ev)
    {
        if (animando) { ev.pointerDrag = null; return; }

        paiOriginal = rt.parent;
        ordemOriginal = rt.GetSiblingIndex();
        posOriginal = rt.position;

        // Vai para o topo do canvas para ficar por cima da barra do inventário
        rt.SetParent(canvas.transform, true);
        rt.SetAsLastSibling();
        OnDrag(ev);
    }

    public void OnDrag(PointerEventData ev)
    {
        if (animando) return;

        Vector3 mouse = TelaParaMundo(ev.position, ev.pressEventCamera);
        var slot = SlotMaisPerto(ev.position, ev.pressEventCamera, out float dist);

        if (slot != null)
        {
            // Quanto mais perto, mais forte o puxão
            float k = forcaIma * (1f - dist / raioIma);
            rt.position = Vector3.Lerp(mouse, slot.IconeRect.position, k);
        }
        else rt.position = mouse;
    }

    public void OnEndDrag(PointerEventData ev)
    {
        if (animando) return;

        var slot = SlotMaisPerto(ev.position, ev.pressEventCamera, out _);
        StartCoroutine(slot != null ? Entrar(slot) : Voltar());
    }

    IEnumerator Entrar(InventorySlot slot)
    {
        animando = true;
        Vector3 p0 = rt.position, e0 = rt.localScale;

        // Escala final = tamanho do ícone do slot (em mundo) / tamanho do item
        Vector2 alvoTam = Vector2.Scale(slot.IconeRect.rect.size, slot.IconeRect.lossyScale);
        Vector2 meuTam = Vector2.Scale(rt.rect.size, rt.lossyScale);
        float fator = Mathf.Min(alvoTam.x / meuTam.x, alvoTam.y / meuTam.y);
        Vector3 e1 = e0 * fator;

        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duracaoEntrar)
        {
            float k = 1f - Mathf.Pow(1f - t, 3f); // ease out
            rt.position = Vector3.Lerp(p0, slot.IconeRect.position, k);
            rt.localScale = Vector3.Lerp(e0, e1, k);
            yield return null;
        }

        if (Inventario.AdicionarNoSlot(item, slot.Indice, quantidade, slot.Local))
        {
            gameObject.SetActive(false);
            rt.localScale = e0;
            Restaurar(posOriginal);
        }
        else
        {
            rt.localScale = e0;
            yield return Voltar(); // slot ocupado no meio do caminho
        }
        animando = false;
    }

    IEnumerator Voltar()
    {
        animando = true;
        Vector3 p0 = rt.position;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duracaoVoltar)
        {
            rt.position = Vector3.Lerp(p0, posOriginal, 1f - Mathf.Pow(1f - t, 3f));
            yield return null;
        }
        Restaurar(posOriginal);
        animando = false;
    }

    void Restaurar(Vector3 pos)
    {
        rt.SetParent(paiOriginal, true);
        rt.SetSiblingIndex(ordemOriginal);
        rt.position = pos;
    }

    // Slot visível mais próximo do mouse que aceita este item, dentro do raio do ímã
    InventorySlot SlotMaisPerto(Vector2 tela, Camera cam, out float menor)
    {
        InventorySlot melhor = null;
        menor = raioIma;

        foreach (var s in FindObjectsByType<InventorySlot>(FindObjectsSortMode.None))
        {
            if (!s.Visivel || !s.PodeReceber(item)) continue;

            Vector2 sTela = RectTransformUtility.WorldToScreenPoint(cam, s.IconeRect.position);
            float d = Vector2.Distance(tela, sTela);
            if (d < menor) { menor = d; melhor = s; }
        }
        return melhor;
    }

    Vector3 TelaParaMundo(Vector2 tela, Camera cam)
    {
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            (RectTransform)canvas.transform, tela, cam, out Vector3 p);
        return p;
    }
}
