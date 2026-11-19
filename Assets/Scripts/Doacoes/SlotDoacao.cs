using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Espaço da janela da Doação 1 onde o jogador solta uma semente arrastada do inventário.
// Clicar no espaço tira a semente de volta. A semente só sai do inventário quando a doação é confirmada.
[RequireComponent(typeof(Image))]
public class SlotDoacao : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [SerializeField] Image icone; // imagem (filha) que mostra a semente colocada

    public ItemData Item { get; private set; }
    public System.Action Mudou;

    void Awake() => Limpar();

    public void OnDrop(PointerEventData ev)
    {
        if (!ev.pointerDrag || !ev.pointerDrag.TryGetComponent(out InventorySlot origem)) return;
        var item = origem.ItemAtual;
        if (!item || !item.ehSemente) return;          // só sementes
        if (!PainelDoacoes.TemSementeSobrando(item, this)) return; // não dá para usar a mesma semente duas vezes

        Item = item;
        if (icone) { icone.sprite = item.icone; icone.enabled = true; icone.preserveAspect = true; }
        Mudou?.Invoke();
    }

    public void OnPointerClick(PointerEventData ev)
    {
        Limpar();
        Mudou?.Invoke();
    }

    public void Limpar()
    {
        Item = null;
        if (icone) { icone.sprite = null; icone.enabled = false; }
    }
}
