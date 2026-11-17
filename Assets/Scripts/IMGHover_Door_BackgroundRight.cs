
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class IMGHover_Door_BackgroundRight : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("Imagens da Porta")]
    public Sprite IMG_Default;
    public Sprite IMG_Hover;

    [Header("Efeitos Sonoros")]
    public AudioClip somAbrir;
    public AudioClip somFechar;

    [Header("Volume dos Sons")]
    [Range(0f, 1f)]
    public float volumeAbrir = 0.5f;

    [Range(0f, 1f)]
    public float volumeFechar = 0.5f;

    private Image IMG;
    private AudioSource audioSource;

    void Start()
    {
        IMG = GetComponent<Image>();
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        IMG.sprite = IMG_Hover;

        if (somAbrir != null)
        {
            audioSource.PlayOneShot(somAbrir, volumeAbrir);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        IMG.sprite = IMG_Default;

        if (somFechar != null)
        {
            audioSource.PlayOneShot(somFechar, volumeFechar);
        }
    }
}

