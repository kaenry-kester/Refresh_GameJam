using UnityEngine;
using TMPro;

public class Dinheiro : MonoBehaviour
{

    [SerializeField] private TMP_Text TextoQuantia;
    public const int DinheiroInicial = 1000; // dinheiro com que o jogo começa
    public static int QuantiaAtual = DinheiroInicial; // static: o valor sobrevive à troca de cenas

    // Começa com o dinheiro inicial a cada Play, mesmo sem domain reload.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Resetar() => QuantiaAtual = DinheiroInicial;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        AtualizarTexto();
    }

    public void Adicionar(int valor)
    {
        QuantiaAtual += valor;
        AtualizarTexto();
    }

    public void Remover(int valor)
    {
        QuantiaAtual -= valor;
        AtualizarTexto();
    }

    public void AtualizarTexto()
    {
        TextoQuantia.text = QuantiaAtual.ToString();
    }

    // Soma dinheiro de qualquer script (ex.: venda de planta) e atualiza os textos da cena
    public static void Ganhar(int valor)
    {
        QuantiaAtual += valor;
        AtualizarTodos();
    }

    // Tira dinheiro se tiver o suficiente (ex.: compra na loja). Retorna false se não tiver.
    public static bool Gastar(int valor)
    {
        if (QuantiaAtual < valor) return false;
        QuantiaAtual -= valor;
        AtualizarTodos();
        return true;
    }

    static void AtualizarTodos()
    {
        foreach (var d in FindObjectsByType<Dinheiro>(FindObjectsSortMode.None))
            d.AtualizarTexto();
    }
}
