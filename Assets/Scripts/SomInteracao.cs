
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(AudioSource))]
public class SomInteracao : MonoBehaviour,
    IPointerEnterHandler,
    IPointerClickHandler
{
    [Header("Efeitos Sonoros")]
    public AudioClip somHover;
    public AudioClip somClick;

    [Header("Volume")]
    [Range(0f, 1f)]
    public float volume = 1f;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (somHover != null)
        {
            audioSource.PlayOneShot(somHover, volume);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left
            && somClick != null)
        {
            audioSource.PlayOneShot(somClick, volume);
        }
    }
}
