using UnityEngine;
using System.Collections;

public class Scene_PreGameplay : MonoBehaviour
{
    [SerializeField] private float tempoEspera = 5f;
    [SerializeField] private GameObject TextArea;
    [SerializeField] private GameObject BotaoComecar;


    void Start()
    {
        StartCoroutine(Sequencia());
    }

    IEnumerator Sequencia()
    {
        yield return new WaitForSeconds(tempoEspera);
        TextArea.SetActive(true);
        yield return new WaitForSeconds(tempoEspera);
        BotaoComecar.SetActive(true);
    }
}
