using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Clique na porta para abrir/fechar (troca de sprite).
[RequireComponent(typeof(AudioSource))]
public class IMGHover_Door_BackgroundRight : MonoBehaviour, IPointerClickHandler
{
    [Header("Imagens da Porta")]
    [Tooltip("Sprite da porta fechada")]
    public Sprite IMG_Default;
    [Tooltip("Sprite da porta aberta")]
    public Sprite IMG_Hover;
    public bool estaAberta = false;

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
    }
}
