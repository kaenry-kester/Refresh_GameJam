using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Vaso : MonoBehaviour
{
    public int id; // diferente em cada vaso da cena
    public bool criadoPorBotao; // marque só no prefab que o botão instancia

    const string CENA_ARRASTAR = "GMP_Scene_Top";
    const float Z_VASO = -1f; // à frente do holder, evita empate de clique

    static Dictionary<int, Vaso> instancias = new Dictionary<int, Vaso>();

    bool isBeingHeld;
    Transform holderAtual; // holder em que o vaso está encaixado
    List<Transform> holdersPerto = new List<Transform>();

    void Awake()
    {
        if (!criadoPorBotao)
        {
            if (instancias.TryGetValue(id, out Vaso existente) && existente != null && existente != this)
            {
                Destroy(gameObject);
                return;
            }

            instancias[id] = this;

            // Vaso que já nasce na cena começa encaixado no holder mais próximo
            holderAtual = HolderMaisPertoNaCena();
        }

        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += AoCarregarCena;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= AoCarregarCena;

        if (!criadoPorBotao && instancias.TryGetValue(id, out Vaso atual) && atual == this)
            instancias.Remove(id);
    }

    void AoCarregarCena(Scene cena, LoadSceneMode modo)
    {
        gameObject.SetActive(cena.name == "GMP_Scene_Top" || cena.name == "GMP_Scene_Center");
    }

    void LateUpdate()
    {
        // Acompanha o holder quando não está sendo segurado
        if (!isBeingHeld && holderAtual != null)
            SeguirHolder();
    }

    void SeguirHolder()
    {
        Vector3 p = holderAtual.position;
        p.z = Z_VASO;
        transform.position = p;
    }

    bool PodeArrastar()
    {
        // O vaso fica na cena DontDestroyOnLoad, então usa a cena ativa
        return SceneManager.GetActiveScene().name == CENA_ARRASTAR;
    }

    void OnMouseDown()
    {
        if (!PodeArrastar()) return;

        isBeingHeld = true;

        // Se ainda não tem holder registrado, usa o que está tocando agora
        if (holderAtual == null)
            holderAtual = HolderMaisPerto();
    }

    void OnMouseDrag()
    {
        if (!isBeingHeld) return;

        Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        pos.z = Z_VASO;
        transform.position = pos;
    }

    void OnMouseUp()
    {
        if (!isBeingHeld) return;

        isBeingHeld = false;
        Encaixar();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("VasoHolder") && !holdersPerto.Contains(other.transform))
            holdersPerto.Add(other.transform);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        holdersPerto.Remove(other.transform);
    }

    // Holder mais próximo entre os que o vaso está tocando
    Transform HolderMaisPerto()
    {
        Transform maisPerto = null;
        float menorDist = float.MaxValue;

        foreach (Transform h in holdersPerto)
        {
            if (h == null || !h.gameObject.activeInHierarchy) continue;

            float d = Vector2.Distance(transform.position, h.position);
            if (d < menorDist)
            {
                menorDist = d;
                maisPerto = h;
            }
        }

        return maisPerto;
    }

    // Holder mais próximo entre todos os da cena (usado só no início)
    Transform HolderMaisPertoNaCena()
    {
        Transform maisPerto = null;
        float menorDist = float.MaxValue;

        foreach (GameObject h in GameObject.FindGameObjectsWithTag("VasoHolder"))
        {
            float d = Vector2.Distance(transform.position, h.transform.position);
            if (d < menorDist)
            {
                menorDist = d;
                maisPerto = h.transform;
            }
        }

        return maisPerto;
    }

    void Encaixar()
    {
        Transform novo = HolderMaisPerto();

        // Soltou em um holder: passa a ser o novo. Soltou fora: continua no anterior.
        if (novo != null)
            holderAtual = novo;

        if (holderAtual != null)
            SeguirHolder();
    }
}