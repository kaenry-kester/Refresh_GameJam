using UnityEngine;

// Mostra o conteúdo do Inventario nos slots. Use um em cada versão (horizontal e vertical).
public class InventoryView : MonoBehaviour
{
    [SerializeField] InventorySlot[] slots;

    void OnEnable()  { Inventario.Mudou += Atualizar; Atualizar(); }
    void OnDisable() { Inventario.Mudou -= Atualizar; }

    void Atualizar()
    {
        var itens = Inventario.Itens;
        for (int i = 0; i < slots.Length; i++)
        {
            if (i < itens.Count) slots[i].Definir(itens[i].item.icone, itens[i].qtd);
            else slots[i].Limpar();
        }
    }
}
