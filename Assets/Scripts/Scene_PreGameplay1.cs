using UnityEngine;
using System.Collections;

public class Scene_PreGameplay1 : MonoBehaviour
{
    [SerializeField] private float tempoEspera = 5f;
    [SerializeField] private GameObject TextArea;
    [SerializeField] private GameObject Text1;
    [SerializeField] private GameObject Text2;

    [SerializeField] private GameObject BotaoComecar;
    [SerializeField] private GameObject BotaoProximo;

    private bool clicou = false;


    void Start()
    {
        // Voltou para uma cena com LembrarCena: o diálogo já foi visto, a cena é restaurada como estava
        if (LembrarCena.JaVisitada(gameObject.scene)) return;

        StartCoroutine(Sequencia());
    }

    IEnumerator Sequencia()
    {
        yield return new WaitForSeconds(tempoEspera);
        TextArea.SetActive(true);
        Text1.SetActive(true);


        yield return new WaitForSeconds(tempoEspera);
        BotaoProximo.SetActive(true);

        clicou = false;
        yield return new WaitUntil(() => clicou);

        Text1.SetActive(false);
        Text2.SetActive(true);

        clicou = false;

        BotaoProximo.SetActive(false);
        BotaoComecar.SetActive(true);
    }

    public void ClicarBotao()
    {
        clicou = true;
    }

}
