
using UnityEngine;

public class SomMonitor : MonoBehaviour
{
    [Header("Som do Clique")]
    public AudioClip somClick;

    [Header("Volume")]
    [Range(0f, 1f)]
    public float volume = 1f;

    [Header("Velocidade")]
    [Range(0.5f, 2f)]
    public float velocidade = 1f;

    public void TocarSom()
    {
        if (somClick == null) return;

        GameObject objeto = new GameObject("Som Temporario");

        AudioSource audio = objeto.AddComponent<AudioSource>();

        audio.playOnAwake = false;
        audio.spatialBlend = 0f;
        audio.pitch = velocidade;

        audio.PlayOneShot(somClick, volume);

        Destroy(objeto, somClick.length / velocidade + 0.2f);
    }
}
