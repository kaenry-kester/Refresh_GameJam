using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Vaso : MonoBehaviour
{
    public int id; // diferente em cada vaso da cena
    public bool criadoPorBotao; // marque só no prefab que o botão instancia

    [Header("Plantio")]
    [SerializeField] SpriteRenderer plantaRenderer; // filho do vaso que mostra a planta (opcional)
    public ItemData Semente { get; private set; }

    const string CENA_ARRASTAR = "GMP_Scene_Top";
    const float Z_VASO = -1f; // à frente do holder, evita empate de clique
    const string NOME_PAINEL_SEMENTE = "PainelInfo"; // nome do painel dentro do prefab da semente

    static Dictionary<int, Vaso> instancias = new Dictionary<int, Vaso>();
    static Dictionary<Transform, Vaso> ocupados = new Dictionary<Transform, Vaso>(); // holder -> vaso que está nele

    bool isBeingHeld;
    Transform holderAtual; // holder em que o vaso está encaixado
    List<Transform> holdersPerto = new List<Transform>();
    public GameObject Informacoes; // painel de informações padrão do vaso
    GameObject InfoSemente;        // painel da semente que está em contato

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

            // Vaso que já nasce na cena começa encaixado no holder livre mais próximo
            DefinirHolder(HolderMaisPertoNaCena());
        }

        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += AoCarregarCena;
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name == "GMP_Scene_Center")
        {
            transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        }
        else
        {
            transform.localScale = new Vector3(1f, 1f, 1f);
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= AoCarregarCena;

        if (!criadoPorBotao && instancias.TryGetValue(id, out Vaso atual) && atual == this)
            instancias.Remove(id);

        LiberarHolder();
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
            DefinirHolder(HolderMaisPerto());
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

    void OnMouseOver()
    {
        bool temInfoSemente = InfoSemente != null;

        // Com semente em contato mostra a info dela; senão, a padrão
        Informacoes.SetActive(!temInfoSemente);

        if (temInfoSemente)
            InfoSemente.SetActive(true);
    }

    void OnMouseExit()
    {
        Informacoes.SetActive(false);

        if (InfoSemente != null)
            InfoSemente.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("VasoHolder") && !holdersPerto.Contains(other.transform))
            holdersPerto.Add(other.transform);

        if (other.CompareTag("Semente"))
        {
            Transform painel = AcharPainel(other.transform);

            if (painel != null)
                InfoSemente = painel.gameObject;
            else
                Debug.LogWarning("Não achei '" + NOME_PAINEL_SEMENTE + "' dentro de " + other.name);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        holdersPerto.Remove(other.transform);

        if (other.CompareTag("Semente"))
        {
            if (InfoSemente != null)
                InfoSemente.SetActive(false);

            InfoSemente = null;
        }
    }

    // Procura o painel entre os filhos da semente, mesmo desativado e em qualquer nível
    Transform AcharPainel(Transform raiz)
    {
        foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            if (t.name == "Info_Semente")
                return t;

        return null;
    }

    // O holder está livre se ninguém o ocupa ou se quem o ocupa sou eu
    bool EstaLivre(Transform h)
    {
        return !ocupados.TryGetValue(h, out Vaso dono) || dono == null || dono == this;
    }

    // Libera o holder atual (só se ele estiver registrado como meu)
    void LiberarHolder()
    {
        if (holderAtual != null && ocupados.TryGetValue(holderAtual, out Vaso dono) && dono == this)
            ocupados.Remove(holderAtual);
    }

    // Troca o holder: libera o antigo e ocupa o novo
    void DefinirHolder(Transform novo)
    {
        LiberarHolder();
        holderAtual = novo;

        if (novo != null)
            ocupados[novo] = this;
    }

    // Holder livre mais próximo entre os que o vaso está tocando
    Transform HolderMaisPerto()
    {
        Transform maisPerto = null;
        float menorDist = float.MaxValue;

        foreach (Transform h in holdersPerto)
        {
            if (h == null || !h.gameObject.activeInHierarchy || !EstaLivre(h)) continue;

            float d = Vector2.Distance(transform.position, h.position);
            if (d < menorDist)
            {
                menorDist = d;
                maisPerto = h;
            }
        }

        return maisPerto;
    }

    // Holder livre mais próximo entre todos os da cena (usado só no início)
    Transform HolderMaisPertoNaCena()
    {
        Transform maisPerto = null;
        float menorDist = float.MaxValue;

        foreach (GameObject h in GameObject.FindGameObjectsWithTag("VasoHolder"))
        {
            if (!EstaLivre(h.transform)) continue;

            float d = Vector2.Distance(transform.position, h.transform.position);
            if (d < menorDist)
            {
                menorDist = d;
                maisPerto = h.transform;
            }
        }

        return maisPerto;
    }

    // Chamado pelo InventorySlot quando uma semente é solta em cima do vaso.
    // Retorna true se plantou (aí o inventário consome 1 unidade).
    public bool Plantar(ItemData item)
    {
        if (item == null || !item.ehSemente || Semente != null) return false;

        Semente = item;
        if (plantaRenderer)
        {
            plantaRenderer.sprite = item.spritePlantado ? item.spritePlantado : item.icone;
            plantaRenderer.enabled = true;
        }
        return true;
    }

    void Encaixar()
    {
        Transform novo = HolderMaisPerto();

        // Soltou em um holder livre: passa a ser o novo.
        // Soltou fora ou em um holder ocupado: volta para o anterior.
        if (novo != null && novo != holderAtual)
            DefinirHolder(novo);

        if (holderAtual != null)
            SeguirHolder();
    }
}