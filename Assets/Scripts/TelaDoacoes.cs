using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    // Botão "DOAR": só aparece com uma doação aberta. Custa 1 semente qualquer do inventário e dá dinheiro.
    // Depois de aceita, aquela doação fica com o botão desativado para sempre.
    [Header("Botão de Doar")]
    public Button botaoDoar;

    [Header("Recompensa em dinheiro")]
    public int recompensaDesmatamento = 300;
    public int recompensaEnchentes = 500;
    public int recompensaONG = 700;

    [Header("Aviso (opcional)")]
    public TMP_Text aviso;
    public string textoSemSemente = "Você precisa de 1 semente para doar";

    // Doações já feitas. Static: sobrevive à troca de cenas.
    static readonly HashSet<string> feitas = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Resetar() => feitas.Clear();

    string aberta;      // doação aberta agora ("Desmatamento", "Enchentes", "ONG" ou null)
    int recompensaAberta;

    void Start()
    {
        if (botaoDoar) botaoDoar.onClick.AddListener(Doar);
        FecharTodas();
    }

    void Abrir(GameObject tela, string id, int recompensa)
    {
        FecharTodas();
        tela.SetActive(true);
        aberta = id;
        recompensaAberta = recompensa;
        AtualizarBotao();
    }

    void AtualizarBotao()
    {
        if (!botaoDoar) return;
        botaoDoar.gameObject.SetActive(aberta != null);
        botaoDoar.interactable = aberta != null && !feitas.Contains(aberta);
    }

    void Doar()
    {
        if (aberta == null || feitas.Contains(aberta)) return;

        if (!RemoverUmaSemente())
        {
            if (aviso) aviso.text = textoSemSemente;
            return;
        }

        Dinheiro.Ganhar(recompensaAberta);
        feitas.Add(aberta);
        AtualizarBotao();
        if (aviso) aviso.text = "";
    }

    // Tira 1 semente (qualquer uma) do inventário. Retorna false se não tiver nenhuma.
    static bool RemoverUmaSemente()
    {
        var slots = Inventario.Slots;
        for (int i = 0; i < slots.Count; i++)
            if (slots[i] != null && slots[i].item && slots[i].item.ehSemente)
                return Inventario.RemoverDoSlot(i, 1);
        return false;
    }

    public void AbrirDesmatamento() => Abrir(telaDesmatamento, "Desmatamento", recompensaDesmatamento);

    public void AbrirEnchentes() => Abrir(telaEnchentes, "Enchentes", recompensaEnchentes);

    public void AbrirONG() => Abrir(telaONG, "ONG", recompensaONG);

    public void Voltar()
    {
        FecharTodas();
    }

    private void FecharTodas()
    {
        telaDesmatamento.SetActive(false);
        telaEnchentes.SetActive(false);
        telaONG.SetActive(false);
        aberta = null;
        AtualizarBotao();
        if (aviso) aviso.text = "";
    }
}
