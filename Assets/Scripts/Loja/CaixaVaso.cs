using UnityEngine;
using UnityEngine.EventSystems;

// Uma caixa de vaso na GMP_Scene_Right (UI Image com o sprite "caixa 1_0").
// Clicou -> o EntregaCaixas abre: a caixa some, toca o som e o vaso vai para o inventário.
public class CaixaVaso : MonoBehaviour, IPointerClickHandler
{
    [HideInInspector] public EntregaCaixas dono;
    [HideInInspector] public ItemData item; // o que vem dentro (vaso comum, vaso com luz...)

    public void OnPointerClick(PointerEventData ev)
    {
        if (!dono) dono = FindAnyObjectByType<EntregaCaixas>();
        if (dono) dono.Abrir(this);
    }
}
