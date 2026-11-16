using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class IMG_Hover_Geral : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Outline outline;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        outline = GetComponent<Outline>();
        outline.enabled = false;
    }

    // Update is called once per frame
    public void OnPointerEnter(PointerEventData eventData)
    {
        outline.enabled = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        outline.enabled = false;
    }
}
