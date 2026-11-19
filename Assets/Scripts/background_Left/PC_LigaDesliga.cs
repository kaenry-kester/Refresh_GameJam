using UnityEngine;

public class PC_LigaDesliga : MonoBehaviour
{

    [SerializeField] private GameObject imagem2;
    [SerializeField] private GameObject[] botoes;


    // Static: sobrevive à troca de cenas. Ligou uma vez, continua ligado ao voltar.
    static bool ligado;

    void Start()
    {
        if (ligado)
        {
            Desligar(); // já estava ligado: mostra os apps direto
            return;
        }

        foreach (GameObject botao in botoes)
        {
            botao.SetActive(false);
        }
    }

    // Apesar do nome, é o que LIGA o PC: some a tela apagada (imagem2) e aparecem os apps
    public void Desligar()
    {
        ligado = true;
        imagem2.SetActive(false);
        foreach (GameObject botao in botoes)
        {
            botao.SetActive(true);
        }
    }

    // Começa desligado a cada Play, mesmo sem domain reload.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Resetar() => ligado = false;

}
