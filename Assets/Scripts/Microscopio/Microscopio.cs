using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Coloque na imagem do Microscópio (GMP_Scene_Center).
// - Clicou no microscópio -> abre a cena de Reconstrução.
// - Arrastou um fóssil do inventário e soltou em cima dele -> abre a cena de Fusão,
//   com o fóssil guardado em Microscopio.FossilSelecionado (para a cena usar).
public class Microscopio : MonoBehaviour, IPointerClickHandler, IDropHandler
{
    [SerializeField] string cenaClique = "M1 (Reconstrucao)";
    [SerializeField] string cenaFossil = "M2 (fusao)";

    // Fóssil levado ao microscópio (a cena de fusão lê daqui). Ele continua no inventário.
    public static ItemData FossilSelecionado { get; private set; }

    public void OnPointerClick(PointerEventData ev)
    {
        if (ev.dragging) return;
        FossilSelecionado = null;
        Abrir(cenaClique);
    }

    public void OnDrop(PointerEventData ev)
    {
        if (!ev.pointerDrag || !ev.pointerDrag.TryGetComponent(out InventorySlot origem)) return;
        var item = origem.ItemAtual;
        if (!item || !item.ehFossil) return; // só fóssil

        FossilSelecionado = item;
        Abrir(cenaFossil);
    }

    static void Abrir(string cena)
    {
        if (string.IsNullOrEmpty(cena)) return;
        LembrarCena.SalvarTodas();
        SceneManager.LoadScene(cena);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Resetar() => FossilSelecionado = null;
}
