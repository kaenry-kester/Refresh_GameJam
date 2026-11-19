
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SistemaDiaNoite : MonoBehaviour
{
    [Header("Backgrounds")]
    public Image backgroundDia;
    public Image backgroundNoite;

    [Header("Transicao")]
    public float tempoTransicao = 3f;

    private Coroutine transicaoAtual;

    void Start()
    {
        backgroundDia.gameObject.SetActive(true);
        backgroundNoite.gameObject.SetActive(true);

        // Aplica o horario atual ao entrar na cena
        AplicarPeriodoAtual();

        // Escuta mudancas de horario
        GerenciadorTempo.AoMudarPeriodo += AtualizarBackground;
    }

    void OnDestroy()
    {
        GerenciadorTempo.AoMudarPeriodo -= AtualizarBackground;
    }

    void AplicarPeriodoAtual()
    {
        float alpha = GerenciadorTempo.estaDeNoite ? 1f : 0f;

        backgroundNoite.color = new Color(
            1f, 1f, 1f, alpha
        );

        Debug.Log(
            "Cena carregada. Esta de noite: " +
            GerenciadorTempo.estaDeNoite
        );
    }

    void AtualizarBackground()
    {
        if (transicaoAtual != null)
            StopCoroutine(transicaoAtual);

        float destino =
            GerenciadorTempo.estaDeNoite ? 1f : 0f;

        transicaoAtual = StartCoroutine(
            Transicao(destino)
        );
    }

    IEnumerator Transicao(float destino)
    {
        float inicio = backgroundNoite.color.a;
        float tempo = 0f;

        while (tempo < tempoTransicao)
        {
            tempo += Time.deltaTime;

            float progresso = Mathf.Clamp01(
                tempo / Mathf.Max(tempoTransicao, 0.01f)
            );

            float alpha = Mathf.Lerp(
                inicio, destino, progresso
            );

            backgroundNoite.color = new Color(
                1f, 1f, 1f, alpha
            );

            yield return null;
        }

        backgroundNoite.color = new Color(
            1f, 1f, 1f, destino
        );

        transicaoAtual = null;
    }
}
