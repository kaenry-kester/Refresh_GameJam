using UnityEngine;
using TMPro;

public class Dinheiro : MonoBehaviour
{

    [SerializeField] private TMP_Text TextoQuantia;
    public static int QuantiaAtual = 0; // static: o valor sobrevive à troca de cenas
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
        foreach (var d in FindObjectsByType<Dinheiro>(FindObjectsSortMode.None))
            d.AtualizarTexto();
    }
}
