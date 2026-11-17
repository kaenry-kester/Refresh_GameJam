using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Um slot: ícone + quantidade. O quadrado do slot já vem desenhado na arte da barra.
public class InventorySlot : MonoBehaviour
{
    [SerializeField] Image icone;
    [SerializeField] TMP_Text qtd;

    public void Definir(Sprite sprite, int quantidade)
    {
        icone.sprite = sprite;
        icone.enabled = true;
        qtd.text = quantidade > 1 ? quantidade.ToString() : ""; // 1 item não mostra número
    }

    public void Limpar()
    {
        icone.sprite = null;
        icone.enabled = false;
        qtd.text = "";
    }
}
