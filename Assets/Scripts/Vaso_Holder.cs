using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Slot onde um vaso encaixa. Coloque na GMP_Scene_Top com Vaso = 1..6 (um número diferente em cada).
// Na Top ele fica onde você posicionou no editor; na Center usa as posições da prateleira abaixo.
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

        if (cena.name == "GMP_Scene_Top")
        {
            gameObject.SetActive(true);
            transform.position = posTop;
        }
        else if(cena.name == "GMP_Scene_Center")
        {
            gameObject.SetActive(true);
            switch (Vaso)
            {
                case 1:
                    transform.position = new Vector3(-1.2f, 1.6f, 0f);
                    break;
                case 2:
                    transform.position = new Vector3(-0.1f, 1.6f, 0f);
                    break;
                case 3:
                    transform.position = new Vector3(1f, 1.6f, 0f);
                    break;
                case 4:
                    transform.position = new Vector3(-1.2f, 1.2f, 0f);
                    break;
                case 5:
                    transform.position = new Vector3(-0.1f, 1.2f, 0f);
                    break;
                case 6:
                    transform.position = new Vector3(1f, 1.2f, 0f);
                    break;
            }
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
