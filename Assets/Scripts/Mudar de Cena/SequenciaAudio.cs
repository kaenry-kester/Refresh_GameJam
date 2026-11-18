using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class SequenciaAudio : MonoBehaviour
{
    [Header("Audios")]
    public AudioClip som1;
    public AudioClip som2;
    public AudioClip som3;
    public AudioClip som4;

    [Header("Volumes")]
    [Range(0f, 1f)] public float volume1 = 1f;
    [Range(0f, 1f)] public float volume2 = 1f;
    [Range(0f, 1f)] public float volume3 = 1f;
    [Range(0f, 1f)] public float volume4 = 1f;

    [Header("Velocidades")]
    [Range(0.5f, 2f)] public float velocidade1 = 1f;
    [Range(0.5f, 2f)] public float velocidade2 = 1f;
    [Range(0.5f, 2f)] public float velocidade3 = 1f;
    [Range(0.5f, 2f)] public float velocidade4 = 1f;

    [Header("Troca de Cena")]
    public string nomeCenaDestino;

    private AudioSource audioSource;
    private Coroutine sequencia;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    void OnEnable()
    {
        sequencia = StartCoroutine(TocarSequencia());
    }

    void OnDisable()
    {
        if (sequencia != null)
        {
            StopCoroutine(sequencia);
            sequencia = null;
        }

        if (audioSource != null)
            audioSource.Stop();
    }
    IEnumerator TocarSequencia()
    {
        yield return TocarSom(som1, volume1, velocidade1);
        yield return TocarSom(som2, volume2, velocidade2);
        yield return TocarSom(som3, volume3, velocidade3);
        yield return TocarSom(som4, volume4, velocidade4);

        sequencia = null;

        if (!string.IsNullOrEmpty(nomeCenaDestino))
        {
            SceneManager.LoadScene(nomeCenaDestino);
        }
    }

    IEnumerator TocarSom(AudioClip clip, float volume, float velocidade)
    {
        if (clip == null)
            yield break;

        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.pitch = velocidade;
        audioSource.Play();

        while (audioSource.isPlaying)
        {
            yield return null;
        }
    }
}