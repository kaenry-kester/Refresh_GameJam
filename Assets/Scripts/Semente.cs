using UnityEngine;

public class Semente : MonoBehaviour
{
   public GameObject Informacoes;
   bool Plantada = false;
    GameObject Vaso;

    private void Update()
    {
        if (Plantada)
        {
            transform.position = Vaso.transform.position;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Vaso"))
        {
            Vaso = other.gameObject;
            Plantada = true;
        }
    }
}
