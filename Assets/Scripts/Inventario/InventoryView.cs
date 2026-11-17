using UnityEngine;

// Mostra o conteúdo do Inventario nos slots. Use um em cada versão (horizontal e vertical).
// A ordem do array 'slots' define o índice de cada slot (0, 1, 2...).
public class InventoryView : MonoBehaviour
{
    [SerializeField] InventorySlot[] slots;

    void Awake()
    {
        for (int i = 0; i < slots.Length; i++) slots[i].Indice = i;
    }

    void OnEnable()  { Inventario.Mudou += Atualizar; Atualizar(); }
    void OnDisable() { Inventario.Mudou -= Atualizar; }

    void Atualizar()
    {
        var dados = Inventario.Slots;
        for (int i = 0; i < slots.Length; i++)
        {
            var e = i < dados.Count ? dados[i] : null;
            if (e != null) slots[i].Definir(e.item.icone, e.qtd);
            else slots[i].Limpar();
        }
    }
}
