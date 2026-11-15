using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class IMGHover_Door_BackgroundRight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{

    public Sprite IMG_Default;
    public Sprite IMG_Hover;

    private Image IMG;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        IMG = GetComponent<Image>();
    }

    // Update is called once per frame
    public void OnPointerEnter(PointerEventData eventData)
    {
        IMG.sprite = IMG_Hover;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        IMG.sprite = IMG_Default;
    }
}
