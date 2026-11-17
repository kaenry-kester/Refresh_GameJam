using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Um slot: ícone + quantidade. O quadrado do slot já vem desenhado na arte da barra.
// Arrastar e soltar:
//   - soltar em outro slot  -> move / troca / junta pilhas
//   - soltar num Vaso (mundo) -> planta a semente (Vaso.Plantar)
//   - soltar em qualquer outro lugar -> o item volta para o slot
public class InventorySlot : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    [SerializeField] Inventario.Local local = Inventario.Local.Inventario; // de quem é este slot
    [SerializeField] Image icone;
    [SerializeField] TMP_Text qtd;
    [SerializeField, Range(0f, 1f)] float alphaDuranteArraste = 0.35f;

    // -1 = automático: usa a ordem do slot entre os irmãos (1º slot = 0, 2º = 1...)
    [SerializeField] int indice = -1;
    public int Indice { get => indice; set => indice = value; }

    RectTransform fantasma; // cópia do ícone que segue o mouse
    Canvas canvas;

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>().rootCanvas;

        icone.preserveAspect = true;

        // Índice automático pela posição visual: esquerda -> direita, cima -> baixo
        if (indice < 0 && transform.parent)
        {
            var ordem = System.Linq.Enumerable.ToList(transform.parent.GetComponentsInChildren<InventorySlot>(true));
            ordem.Sort((a, b) =>
            {
                Vector3 pa = a.transform.localPosition, pb = b.transform.localPosition;
                int x = pa.x.CompareTo(pb.x);
                return Mathf.Abs(pa.x - pb.x) > 1f ? x : pb.y.CompareTo(pa.y);
            });
            indice = ordem.IndexOf(this);
        }

        // O slot precisa de um Graphic para receber o drop mesmo vazio (ícone desligado).
        if (!TryGetComponent<Graphic>(out _))
            gameObject.AddComponent<Image>().color = Color.clear;

        if (qtd) qtd.raycastTarget = false;
    }

    // Cada slot se atualiza sozinho: vazio = invisível (só aparece a arte da barra)
    void OnEnable()  { Inventario.Mudou += Atualizar; Atualizar(); }
    void OnDisable() { Inventario.Mudou -= Atualizar; }

    void Atualizar()
    {
        var dados = Inventario.De(local);
        var e = indice >= 0 && indice < dados.Count ? dados[indice] : null;
        if (e != null) Definir(e.item.icone, e.qtd);
        else Limpar();
    }

    public void Definir(Sprite sprite, int quantidade)
    {
        icone.sprite = sprite;
        icone.enabled = true;
        if (qtd) qtd.text = quantidade > 1 ? quantidade.ToString() : ""; // 1 item não mostra número
    }

    public void Limpar()
    {
        icone.sprite = null;
        icone.enabled = false;
        if (qtd) qtd.text = "";
    }

    // Usado pelo ímã do ColetarItem
    public RectTransform IconeRect => icone.rectTransform;
    public Inventario.Local Local => local;
    public bool PodeReceber(ItemData item) => Inventario.PodeAdicionarNoSlot(item, indice, local);
    public bool Visivel => isActiveAndEnabled &&
        transform.lossyScale.x > canvas.transform.lossyScale.x * 0.5f; // barra recolhida = escala ~0

    bool TemItem => indice >= 0 && indice < Inventario.De(local).Count && Inventario.De(local)[indice] != null;

    public void OnBeginDrag(PointerEventData ev)
    {
        if (!TemItem) { ev.pointerDrag = null; return; } // slot vazio não arrasta

        // Cria o fantasma no canvas raiz, por cima de tudo, sem bloquear raycasts
        var go = new GameObject("ItemArrastado", typeof(RectTransform), typeof(Image));
        fantasma = (RectTransform)go.transform;
        fantasma.SetParent(canvas.transform, false);
        fantasma.SetAsLastSibling();

        var img = go.GetComponent<Image>();
        img.sprite = icone.sprite;
        img.preserveAspect = icone.preserveAspect;
        img.raycastTarget = false;

        // Mesmo tamanho visual do ícone, mesmo com a barra escalada pela animação
        Vector3 esc = icone.rectTransform.lossyScale;
        Vector3 escCanvas = canvas.transform.lossyScale;
        fantasma.sizeDelta = Vector2.Scale(icone.rectTransform.rect.size,
                                           new Vector2(esc.x / escCanvas.x, esc.y / escCanvas.y));

        icone.canvasRenderer.SetAlpha(alphaDuranteArraste);
        Seguir(ev);
    }

    public void OnDrag(PointerEventData ev)
    {
        if (fantasma) Seguir(ev);
    }

    public void OnEndDrag(PointerEventData ev)
    {
        if (fantasma) Destroy(fantasma.gameObject);
        fantasma = null;
        icone.canvasRenderer.SetAlpha(1f);

        // Soltou fora da UI? Tenta entregar para algo no mundo (ex.: um vaso).
        if (ev.pointerCurrentRaycast.gameObject == null && TemItem)
            SoltarNoMundo(ev.position);
    }

    // Chamado no slot DESTINO quando outro slot é solto em cima dele
    public void OnDrop(PointerEventData ev)
    {
        if (ev.pointerDrag && ev.pointerDrag.TryGetComponent(out InventorySlot origem) && origem != this)
            Inventario.Mover(origem.local, origem.Indice, local, Indice);
    }

    // Vazio de propósito: impede que clicar num slot "atravesse" e feche o frigorífico
    public void OnPointerClick(PointerEventData ev) { }

    void SoltarNoMundo(Vector2 telaPos)
    {
        var cam = Camera.main;
        if (!cam) return;

        Vector2 mundo = cam.ScreenToWorldPoint(telaPos);
        foreach (var col in Physics2D.OverlapPointAll(mundo))
        {
            if (col.TryGetComponent(out Vaso vaso))
            {
                var item = Inventario.De(local)[Indice].item;
                if (vaso.Plantar(item))
                    Inventario.RemoverDoSlot(Indice, 1, local);
                return;
            }
        }
    }

    void Seguir(PointerEventData ev)
    {
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            (RectTransform)canvas.transform, ev.position, ev.pressEventCamera, out Vector3 pos);
        fantasma.position = pos;
    }
}
