using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Vaso_Holder : MonoBehaviour
{
    public int Vaso;

    static Dictionary<int, Vaso_Holder> instancias = new Dictionary<int, Vaso_Holder>();

    void Awake()
    {
        if (instancias.TryGetValue(Vaso, out Vaso_Holder existente) && existente != null && existente != this)
        {
            Destroy(gameObject);
            return;
        }

        instancias[Vaso] = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += AoCarregarCena;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= AoCarregarCena;

        if (instancias.TryGetValue(Vaso, out Vaso_Holder atual) && atual == this)
            instancias.Remove(Vaso);
    }

    // AoCarregarCena continua igual
    void AoCarregarCena(Scene cena, LoadSceneMode modo)
    {
        if (cena.name == "GMP_Scene_Top")
        {
            gameObject.SetActive(true);
            switch (Vaso)
            {
                case 1:
                    transform.position = new Vector3(-4f, 2f, 0f);
                    break;
                case 2:
                    transform.position = new Vector3(0f, 2f, 0f);
                    break;
                case 3:
                    transform.position = new Vector3(4f, 2f, 0f);
                    break;
                case 4:
                    transform.position = new Vector3(-4f, 0f, 0f);
                    break;
                case 5:
                    transform.position = new Vector3(0f, 0f, 0f);
                    break;
                case 6:
                    transform.position = new Vector3(4f, 0f, 0f);
                    break;
            }

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
