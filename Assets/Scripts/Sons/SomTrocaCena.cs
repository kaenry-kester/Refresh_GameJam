
using UnityEngine;
using UnityEngine.EventSystems;

public class SomTrocaCena : MonoBehaviour, IPointerEnterHandler
{
    [Header("Efeitos Sonoros")]
    public AudioClip somHover;
    public AudioClip somTroca;

    [Header("Volume")]
    [Range(0f, 1f)]
    public float volumeHover = 0.5f;

    [Range(0f, 1f)]
    public float volumeTroca = 0.5f;

    [Header("Velocidade")]
    [Range(0.5f, 2f)]
    public float velocidadeHover = 1f;

    [Range(0.5f, 2f)]
    public float velocidadeTroca = 1f;

    public void OnPointerEnter(PointerEventData eventData)
    {
        TocarAudio(somHover, volumeHover, velocidadeHover);
    }

    public void TocarSom()
    {
        TocarAudio(somTroca, volumeTroca, velocidadeTroca);
    }

    private void TocarAudio(AudioClip clip, float volume, float velocidade)
    {
        if (clip == null) return;

        GameObject objetoAudio = new GameObject("Som Temporario");

        AudioSource audioSource = objetoAudio.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.pitch = velocidade;

        DontDestroyOnLoad(objetoAudio);

        audioSource.PlayOneShot(clip, volume);

        Destroy(objetoAudio, clip.length / velocidade + 0.2f);
    }
}
