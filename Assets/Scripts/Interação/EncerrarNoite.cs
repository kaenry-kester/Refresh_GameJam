using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Coloque na porta (Door_EndDay). À noite, clicar na porta abre "Deseja encerrar a noite?".
//   Não -> fecha a pergunta.
//   Sim -> tela escura "Dia X concluído..." por alguns segundos e começa o próximo dia.
// Ligue o OnClick do botão Sim em Sim() e do botão Não em Nao().
public class EncerrarNoite : MonoBehaviour, IPointerClickHandler
{
    [Header("Telas (comece com as duas desativadas)")]
    [SerializeField] GameObject painelPergunta; // "Deseja encerrar a noite?" + Sim / Não
    [SerializeField] GameObject telaConcluido;  // tela escura com a mensagem

    [Header("Mensagem da tela escura (use um dos dois)")]
    [SerializeField] TMP_Text textoTMP;
    [SerializeField] Text textoLegacy;
    [Tooltip("{0} vira o número do dia que acabou")]
    [SerializeField] string mensagem = "Dia {0} concluído, avançando para o próximo dia!";
    [SerializeField] Color corTexto = Color.white;

    [SerializeField] float duracaoTela = 3f;     // segundos que a tela escura fica
    [SerializeField] bool somenteANoite = true; // de dia a porta não pergunta nada

    bool avancando;

    void Start()
    {
        if (painelPergunta) painelPergunta.SetActive(false);
        if (telaConcluido) telaConcluido.SetActive(false);
    }

    public void OnPointerClick(PointerEventData ev)
    {
        if (avancando) return;
        if (somenteANoite && !GerenciadorTempo.estaDeNoite) return;
        painelPergunta.SetActive(true);
    }

    public void Nao() => painelPergunta.SetActive(false);

    public void Sim()
    {
        if (!avancando) StartCoroutine(Avancar());
    }

    IEnumerator Avancar()
    {
        avancando = true;
        painelPergunta.SetActive(false);

        string texto = string.Format(mensagem, GerenciadorTempo.Dia);
        if (textoTMP) { textoTMP.text = texto; textoTMP.color = corTexto; }
        if (textoLegacy) { textoLegacy.text = texto; textoLegacy.color = corTexto; }
        telaConcluido.SetActive(true);

        // Avança já com a tela escura cobrindo (a troca noite -> dia acontece por trás)
        GerenciadorTempo.AvancarDia();

        yield return new WaitForSeconds(duracaoTela);

        telaConcluido.SetActive(false);
        avancando = false;
    }
}
