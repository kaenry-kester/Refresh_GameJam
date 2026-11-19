using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Um slot: ícone + quantidade. O quadrado do slot já vem desenhado na arte da barra.
// Arrastar e soltar:
//   - soltar em outro slot  -> move / troca / junta pilhas
//   - soltar num Vaso (mundo) -> planta a semente (Vaso.Plantar)
//   - soltar em qualquer outro lugar -> o item volta para o slot
// Clique rápido (sem arrastar e sem segurar) numa planta adulta -> abre o painel do vaso.
public class InventorySlot : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler, IPointerDownHandler
{
    [Tooltip("Distância (unidades do mundo) para um item Vaso encaixar num slot livre do jardim")]
    [SerializeField] float raioSlotVaso = 1.5f;
    [Tooltip("Segurou mais que isso (segundos) = não é clique (é para pegar/arrastar)")]
    [SerializeField] float tempoMaxClique = 0.3f;
    float inicioToque;
    [SerializeField] Inventario.Local local = Inventario.Local.Inventario; // de quem é este slot
    [SerializeField] Image icone;
    [SerializeField] TMP_Text qtd;
    [SerializeField, Range(0f, 1f)] float alphaDuranteArraste = 0.35f;

    [Header("Timer (só no frigorífico)")]
    [SerializeField] GameObject indicador; // objeto que agrupa o círculo (some com o slot vazio)
    [SerializeField] Image circulo;        // a parte que vai se fechando
    [SerializeField] GameObject concluido; // opcional: aparece quando completa (ex.: o ✔)

    // -1 = automático: usa a ordem do slot entre os irmãos (1º slot = 0, 2º = 1...)
    [SerializeField] int indice = -1;
    public int Indice { get => indice; set => indice = value; }

    RectTransform fantasma; // cópia do ícone que segue o mouse
    Canvas canvas;

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>().rootCanvas;

        icone.preserveAspect = true;
        icone.raycastTarget = true; // dá para pegar a semente clicando em qualquer parte do ícone

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

        // Círculo que se fecha no sentido horário, começando no topo
        if (circulo)
        {
            circulo.type = Image.Type.Filled;
            circulo.fillMethod = Image.FillMethod.Radial360;
            circulo.fillOrigin = (int)Image.Origin360.Top;
            circulo.fillClockwise = true;
            circulo.raycastTarget = false;
        }
    }

    bool mostrandoPos;

    void Update()
    {
        if (local != Inventario.Local.Frigorifico) return;

        var e = Entrada;
        if (e == null) return;

        if (circulo) circulo.fillAmount = e.Progresso;
        if (e.Pronta != mostrandoPos) Atualizar(); // completou -> troca para o sprite "pós"
    }

    Inventario.Entrada Entrada
    {
        get
        {
            var dados = Inventario.De(local);
            return indice >= 0 && indice < dados.Count ? dados[indice] : null;
        }
    }

    // Cada slot se atualiza sozinho: vazio = invisível (só aparece a arte da barra)
    void OnEnable()  { Inventario.Mudou += Atualizar; Atualizar(); }
    void OnDisable() { Inventario.Mudou -= Atualizar; }

    void Atualizar()
    {
        var e = Entrada;
        bool noFrigorifico = local == Inventario.Local.Frigorifico;
        mostrandoPos = e != null && e.Pronta;

        if (e != null)
        {
            var sprite = e.item.icone;
            if (noFrigorifico && mostrandoPos && e.item.iconeFrigorificoPos) sprite = e.item.iconeFrigorificoPos;
            else if (noFrigorifico && e.item.iconeFrigorificoPre) sprite = e.item.iconeFrigorificoPre;
            Definir(sprite, e.qtd);
        }
        else Limpar();

        if (indicador) indicador.SetActive(noFrigorifico && e != null);
        if (circulo) circulo.fillAmount = e != null ? e.Progresso : 0f;
        if (concluido) concluido.SetActive(mostrandoPos);
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

    // Item que está neste slot (null = vazio). Usado por quem recebe um item arrastado (doação, microscópio...)
    public ItemData ItemAtual => Entrada != null ? Entrada.item : null;

    // Usado pelo ímã do ColetarItem
    public RectTransform IconeRect => icone.rectTransform;
    public Inventario.Local Local => local;
    public bool PodeReceber(ItemData item) => Inventario.PodeAdicionarNoSlot(item, indice, local);
    public bool Visivel => isActiveAndEnabled &&
        transform.lossyScale.x > canvas.transform.lossyScale.x * 0.05f; // barra recolhida = escala ~0

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

        // Arrastando um vaso novo: destaca os slots livres do jardim
        if (Entrada != null && Entrada.item && Entrada.item.ehVaso) Vaso_Holder.Destacar(true);
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
        Vaso_Holder.Destacar(false); // soltou: tira o destaque dos slots

        // Soltou fora da UI? Tenta entregar para algo no mundo (ex.: um vaso).
        if (ev.pointerCurrentRaycast.gameObject == null && TemItem)
            SoltarNoMundo(ev.position);
    }

    // Chamado no slot DESTINO quando outro slot é solto em cima dele
    public void OnDrop(PointerEventData ev)
    {
        if (ev.pointerDrag && ev.pointerDrag.TryGetComponent(out InventorySlot origem) && origem != this)
            Inventario.Mover(origem.local, origem.Indice, local, Indice, 1); // arrastar leva só 1 da pilha
    }

    public void OnPointerDown(PointerEventData ev) => inicioToque = Time.unscaledTime;

    // Também impede que clicar num slot "atravesse" e feche o frigorífico.
    // A Unity só chama o clique se NÃO houve arraste; aqui também ignoramos quando segurou demais.
    public void OnPointerClick(PointerEventData ev)
    {
        if (ev.button != PointerEventData.InputButton.Left || ev.dragging) return;
        if (Time.unscaledTime - inicioToque > tempoMaxClique) return; // pressionou/segurou: não abre

        var e = Entrada;
        if (e != null && e.item && e.item.EhPlantaColhida)
            PainelVaso.AbrirItem(e.item, indice, local);
    }

    void SoltarNoMundo(Vector2 telaPos)
    {
        var cam = Camera.main;
        if (!cam) return;

        Vector2 mundo = cam.ScreenToWorldPoint(telaPos);
        var item = Inventario.De(local)[Indice].item;

        // Item "Vaso": soltou perto de um slot livre do jardim -> cria um vaso novo ali
        if (item.ehVaso)
        {
            var slot = Vaso.SlotLivreMaisPerto(mundo, raioSlotVaso);
            if (slot && Vaso.CriarNoSlot(item.prefabVaso, slot))
                Inventario.RemoverDoSlot(Indice, 1, local);
            return;
        }

        // Semente: basta soltar perto do vaso (raioPlantio de cada vaso)
        var vaso = Vaso.MaisPerto(mundo);
        if (vaso == null) return;

        if (vaso.Plantar(item))
            Inventario.RemoverDoSlot(Indice, 1, local);
    }

    void Seguir(PointerEventData ev)
    {
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            (RectTransform)canvas.transform, ev.position, ev.pressEventCamera, out Vector3 pos);
        fantasma.position = pos;
    }
}
