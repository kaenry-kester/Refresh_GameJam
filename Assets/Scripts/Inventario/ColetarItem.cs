using UnityEngine;

// Coloque num objeto clicável (ex.: um Button) e ligue Coletar() no OnClick.
public class ColetarItem : MonoBehaviour
{
    [SerializeField] ItemData item;
    [SerializeField] int quantidade = 1;
    [SerializeField] bool sumirAoColetar = true;

    public void Coletar()
    {
        if (Inventario.Adicionar(item, quantidade) && sumirAoColetar)
            gameObject.SetActive(false);
    }
}
