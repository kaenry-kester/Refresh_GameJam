using TMPro;
using UnityEngine;

// Botão de compra de vaso (Loja: vaso comum; Upgrade: vaso com luz, etc.).
// Ligue o OnClick do botão Comprar em Comprar().
// Cobra o preço, toca o som de compra e manda uma caixa para a GMP_Scene_Right (EntregaCaixas),
// onde o jogador abre a caixa e o vaso vai para o inventário.
public class ComprarVaso : MonoBehaviour
{
    [SerializeField] int preco = 100;
    [Tooltip("Item que vem na caixa (ex.: Vaso_Luz_Item). Vazio = o 'Item Vaso' padrão do EntregaCaixas (vaso comum).")]
    [SerializeField] ItemData itemComprado;
    [SerializeField] AudioClip somCompra;
    [SerializeField, Range(0f, 1f)] float volume = 1f;

    [Header("Opcional")]
    [SerializeField] TMP_Text aviso;            // mostra "Comprado!" ou "Dinheiro insuficiente"
    [SerializeField] string textoComprado = "Comprado! A caixa chegará na porta.";
    [SerializeField] string textoSemDinheiro = "Dinheiro insuficiente";
    [SerializeField] GameObject fecharAoComprar; // ex.: a tela Informações_Compra

    public void Comprar()
    {
        if (!Dinheiro.Gastar(preco))
        {
            if (aviso) aviso.text = textoSemDinheiro;
            return;
        }

        EntregaCaixas.Encomendar(itemComprado);
        TocarSom();
        if (aviso) aviso.text = textoComprado;
        if (fecharAoComprar) fecharAoComprar.SetActive(false);
    }

    void TocarSom()
    {
        if (!somCompra) return;
        var go = new GameObject("SomCompra");
        DontDestroyOnLoad(go); // não corta se o jogador sair da tela
        var fonte = go.AddComponent<AudioSource>();
        fonte.spatialBlend = 0f;
        fonte.PlayOneShot(somCompra, volume);
        Destroy(go, somCompra.length + 0.1f);
    }
}
