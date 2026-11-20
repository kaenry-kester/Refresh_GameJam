
using UnityEngine;

public class TelaDoacoes : MonoBehaviour
{
    [Header("Placas")]
    public GameObject doacao1;
    public GameObject doacao2;
    public GameObject doacao3;

    [Header("Telas Completas")]
    public GameObject telaDesmatamento;
    public GameObject telaEnchentes;
    public GameObject telaONG;

    [Header("Botao de Voltar")]
    public GameObject botaoVoltarTela;

    void Start()
    {
        FecharTodas();
    }

    public void AbrirDesmatamento()
    {
        FecharTodas();
        telaDesmatamento.SetActive(true);
    }

    public void AbrirEnchentes()
    {
        FecharTodas();
        telaEnchentes.SetActive(true);
    }

    public void AbrirONG()
    {
        FecharTodas();
        telaONG.SetActive(true);
    }

    public void Voltar()
    {
        FecharTodas();
    }

    private void FecharTodas()
    {
        telaDesmatamento.SetActive(false);
        telaEnchentes.SetActive(false);
        telaONG.SetActive(false);
    }
}
