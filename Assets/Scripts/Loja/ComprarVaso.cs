using TMPro;
using UnityEngine;

// Compra de vaso na Loja. Ligue o OnClick do Botao_Comprar em Comprar().
// Cobra o preço e manda uma caixa para a GMP_Scene_Right (EntregaCaixas), onde o jogador abre
// a caixa e o vaso vai para o inventário.
public class ComprarVaso : MonoBehaviour
{
    [SerializeField] int preco = 100;

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

        EntregaCaixas.Pendentes++;
        if (aviso) aviso.text = textoComprado;
        if (fecharAoComprar) fecharAoComprar.SetActive(false);
    }
}
