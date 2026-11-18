using UnityEngine;

public class IMG_Hover_MudarCor : MonoBehaviour
{
    [SerializeField] private Color corHover;
    [SerializeField] private Color corOriginal;
    private UnityEngine.UI.Image img;

    void Start()
    {
        img = GetComponent<UnityEngine.UI.Image>();
        corOriginal = img.color;
    }

    void OnMouseEnter()
    {
        img.color = corHover;
    }

    void OnMouseExit()
    {
        img.color = corOriginal;
    }
}
