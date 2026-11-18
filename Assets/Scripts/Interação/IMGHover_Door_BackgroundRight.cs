
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class IMGHover_Door_BackgroundRight : MonoBehaviour, IPointerClickHandler
{
    [Header("Imagens da Porta")]
    public Sprite IMG_Default;
    public Sprite IMG_Hover;
    public bool estaAberta = false;

    [Header("Cores da Porta")]
    public Color corDefault = Color.white;
    public Color corHover = new Color(0.6f, 0.6f, 0.6f, 1f);

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

        AtualizarSprite();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        estaAberta = !estaAberta;
        AtualizarSprite();

        AudioClip som = estaAberta ? somAbrir : somFechar;
        float volume = estaAberta ? volumeAbrir : volumeFechar;

        if (som != null)
        {
            audioSource.PlayOneShot(som, volume);
        }
    }

    void AtualizarSprite()
    {
        IMG.sprite = estaAberta ? IMG_Hover : IMG_Default;
        IMG.color = estaAberta ? corHover : corDefault;
    }
}
