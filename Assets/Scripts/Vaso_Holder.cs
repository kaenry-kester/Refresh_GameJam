using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Slot onde um vaso encaixa. Coloque na GMP_Scene_Top com Vaso = 1..6 (um número diferente em cada).
// Fica onde você posicionou no editor na Top; nas outras cenas fica escondido.
// Se tiver SpriteRenderer (desenho do slot), ele só aparece na Top.
public class Vaso_Holder : MonoBehaviour
{
    public int Vaso;

    static Dictionary<int, Vaso_Holder> instancias = new Dictionary<int, Vaso_Holder>();

    Vector3 posTop; // posição colocada no editor na GMP_Scene_Top

    void Awake()
    {
        if (instancias.TryGetValue(Vaso, out Vaso_Holder existente) && existente != null && existente != this)
        {
            Destroy(gameObject);
            return;
        }

        instancias[Vaso] = this;
        posTop = transform.position;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += AoCarregarCena;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= AoCarregarCena;

        if (instancias.TryGetValue(Vaso, out Vaso_Holder atual) && atual == this)
            instancias.Remove(Vaso);
    }

    void AoCarregarCena(Scene cena, LoadSceneMode modo)
    {
        // Desenho do slot só aparece na Top
        if (TryGetComponent(out SpriteRenderer sr))
            sr.enabled = cena.name == "GMP_Scene_Top";

        // Slots (e vasos) só existem visíveis na GMP_Scene_Top
        if (cena.name == "GMP_Scene_Top")
        {
            gameObject.SetActive(true);
            transform.position = posTop;
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
