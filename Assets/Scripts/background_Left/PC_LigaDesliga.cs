using UnityEngine;

public class PC_LigaDesliga : MonoBehaviour
{

    [SerializeField] private GameObject imagem2;
    [SerializeField] private GameObject[] botoes;


    void Start()
    {
        foreach (GameObject botao in botoes)
        {
            botao.SetActive(false);
        }
    }

    public void Desligar()
    {
        imagem2.SetActive(false);
        foreach (GameObject botao in botoes)
        {
            botao.SetActive(true);
        }
    }

}
