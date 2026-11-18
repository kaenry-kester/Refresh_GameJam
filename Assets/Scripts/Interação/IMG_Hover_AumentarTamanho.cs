using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class IMG_Hover_AumentarTamanho : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 Tamanho_Original;
    private Vector3 Tamanho_Hover = 1.03f * Vector3.one;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Tamanho_Original = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = Vector3.Scale(Tamanho_Original, Tamanho_Hover);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = Tamanho_Original;
    }
}
