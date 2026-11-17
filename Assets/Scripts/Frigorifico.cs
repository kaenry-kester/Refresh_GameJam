using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Clique no frigorífico para abrir/fechar.
public class Frigorifico : MonoBehaviour, IPointerClickHandler
{
    public Sprite spriteFechado;
    public Sprite spriteAberto;
    public bool estaAberto = false;
    public GameObject slots; // objeto que agrupa os slots do frigorífico (só aparece aberto)

    private Image img;

    void Start()
    {
        img = GetComponent<Image>();
        AtualizarSprite();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        estaAberto = !estaAberto;
        AtualizarSprite();
    }

    void AtualizarSprite()
    {
        img.sprite = estaAberto ? spriteAberto : spriteFechado;
        if (slots) slots.SetActive(estaAberto);
    }
}
