using UnityEngine;
using UnityEngine.UI;

// Coloque em cada BotaoExpedicao (todos com a tag "Orelha"). Ao clicar:
//   Elemento 0          -> desativa
//   Elementos 1 a 4     -> ativa
//   Elementos 5 em diante (dados desta expedição) -> ativa,
//   e os dados (5 em diante) dos outros botões "Orelha" -> desativa
[RequireComponent(typeof(Button))]
public class MudarDeExpedição : MonoBehaviour
{
    const int InicioDados = 5;

    [SerializeField] private GameObject[] Elementos;

    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Clicou);
    }

    void Clicou()
    {
        // Primeiro esconde os dados das outras expedições
        foreach (var orelha in GameObject.FindGameObjectsWithTag("Orelha"))
        {
            var outro = orelha.GetComponent<MudarDeExpedição>();
            if (outro != null && outro != this) outro.EsconderDados();
        }

        // Depois mostra os desta
        for (int i = 0; i < Elementos.Length; i++)
            if (Elementos[i] != null)
                Elementos[i].SetActive(i != 0);
    }

    void EsconderDados()
    {
        for (int i = InicioDados; i < Elementos.Length; i++)
            if (Elementos[i] != null)
                Elementos[i].SetActive(false);
    }
}
