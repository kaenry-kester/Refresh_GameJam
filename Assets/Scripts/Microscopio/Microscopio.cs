using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Coloque na imagem do Microscópio (GMP_Scene_Center).
// - Clicou no microscópio -> abre a Tela Microscópio 1.
// - Arrastou uma planta do inventário e soltou em cima dele -> abre a Tela Microscópio 2,
//   com a planta guardada em Microscopio.PlantaSelecionada (para a tela 2 usar).
public class Microscopio : MonoBehaviour, IPointerClickHandler, IDropHandler
{
    [SerializeField] string cenaMicroscopio1 = "TelaMicroscopio1";
    [SerializeField] string cenaMicroscopio2 = "TelaMicroscopio2";
    [Tooltip("Aceitar também sementes (não só plantas adultas colhidas)")]
    [SerializeField] bool aceitarSementes = false;

    // Planta levada ao microscópio (a tela 2 lê daqui). Ela continua no inventário.
    public static ItemData PlantaSelecionada { get; private set; }

    public void OnPointerClick(PointerEventData ev)
    {
        if (ev.dragging) return;
        PlantaSelecionada = null;
        Abrir(cenaMicroscopio1);
    }

    public void OnDrop(PointerEventData ev)
    {
        if (!ev.pointerDrag || !ev.pointerDrag.TryGetComponent(out InventorySlot origem)) return;
        var item = origem.ItemAtual;
        if (!item) return;

        bool planta = item.EhPlantaColhida || (!item.ehSemente && !item.ehVaso && item.origem);
        if (!planta && !(aceitarSementes && item.ehSemente)) return; // vaso, etc.: ignora

        PlantaSelecionada = item;
        Abrir(cenaMicroscopio2);
    }

    static void Abrir(string cena)
    {
        if (string.IsNullOrEmpty(cena)) return;
        LembrarCena.SalvarTodas();
        SceneManager.LoadScene(cena);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Resetar() => PlantaSelecionada = null;
}
