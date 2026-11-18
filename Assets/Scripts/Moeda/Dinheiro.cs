using UnityEngine;
using TMPro;

public class Dinheiro : MonoBehaviour
{

    [SerializeField] private TMP_Text TextoQuantia;
    public int QuantiaAtual = 0;
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
}
