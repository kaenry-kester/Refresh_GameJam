using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Vaso : MonoBehaviour
{
    public int id; // diferente em cada vaso da cena
    public bool criadoPorBotao; // marque só no prefab que o botão instancia

    [Header("Plantio")]
    [SerializeField] SpriteRenderer plantaRenderer; // filho do vaso que mostra a planta (opcional)
    [SerializeField] float raioPlantio = 1f;        // soltar a semente até essa distância (unidades do mundo) planta
    [SerializeField, Range(0f, 1f)] float escurecer = 0.6f; // cor do vaso com semente (1 = igual, 0 = preto)
    [SerializeField] GameObject iconeSede; // filho do vaso com o ícone "sede" (aparece quando o dia avança)

    [Header("Planta em cima do vaso")]
    [Tooltip("Posição da Planta (filho do vaso), igual para todas as fases")]
    [SerializeField] Vector2 posicaoPlanta = new Vector2(0.3f, 4.2f);
    [Tooltip("Tamanho do desenho em cada fase (unidades do mundo, 1 = um quadrado da grade): Inicial, Broto, Crescendo, Final.\n" +
             "O desenho é ajustado para caber num quadrado desse tamanho, sem distorcer, qualquer que seja a imagem.")]
    [SerializeField] float[] tamanhoPorFase = { 0.5f, 1.0f, 2.0f, 2.3f };
    public ItemData Semente { get; private set; }
    public int Fase { get; private set; }   // 0 = Semente, 1 = Broto, 2 = Crescendo, 3 = Final
    public bool Regado { get; private set; } // já cuidado hoje (vaso escuro)
    public bool ComSede => Semente != null && !Regado;

    SpriteRenderer sr;
    Color corOriginal;

    const string CENA_ARRASTAR = "GMP_Scene_Top";
    const float Z_VASO = -1f; // à frente do holder, evita empate de clique

    static Dictionary<int, Vaso> instancias = new Dictionary<int, Vaso>();
    static readonly List<Vaso> todos = new List<Vaso>(); // todos os vasos vivos (para saber quem ocupa cada slot)

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
        }

        todos.Add(this);

        sr = GetComponent<SpriteRenderer>();
        if (sr) corOriginal = sr.color;

        if (iconeSede) iconeSede.SetActive(false);
        if (plantaRenderer) plantaRenderer.enabled = false; // vaso começa vazio
        GerenciadorTempo.AoNovoDia += AoNovoDia;

        // Vaso que já nasce na cena começa encaixado no slot livre mais próximo
        if (!criadoPorBotao)
            holderAtual = HolderMaisPertoNaCena();

        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += AoCarregarCena;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= AoCarregarCena;
        GerenciadorTempo.AoNovoDia -= AoNovoDia;
        todos.Remove(this);

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

    // Vaso que está encaixado neste slot (null = slot livre)
    static Vaso OcupanteDe(Transform holder)
    {
        foreach (var v in todos)
            if (v != null && v.holderAtual == holder) return v;
        return null;
    }

    // Slot livre mais próximo entre todos os da cena (usado só no início)
    Transform HolderMaisPertoNaCena()
    {
        Transform maisPerto = null;
        float menorDist = float.MaxValue;

        foreach (GameObject h in GameObject.FindGameObjectsWithTag("VasoHolder"))
        {
            if (OcupanteDe(h.transform) != null) continue; // já tem vaso

            float d = Vector2.Distance(transform.position, h.transform.position);
            if (d < menorDist)
            {
                menorDist = d;
                maisPerto = h.transform;
            }
        }

        return maisPerto;
    }

    // Dia novo: planta que ainda não chegou na fase final volta à cor normal e fica com sede
    void AoNovoDia()
    {
        if (Semente == null || Fase >= ItemData.FaseFinal) return; // vazio ou já crescida: não muda
        DefinirRegado(false);
    }

    // Tamanho só da parte desenhada do sprite (sem o espaço transparente em volta).
    // Usa a malha "Tight" que a Unity gera contornando o desenho.
    static Vector2 TamanhoDoDesenho(Sprite sprite)
    {
        var v = sprite.vertices;
        if (v == null || v.Length == 0) return sprite.bounds.size;

        Vector2 min = v[0], max = v[0];
        foreach (var p in v) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
        return max - min;
    }

    // Chamado pelo Regador. Retorna true se regou (planta com sede).
    // Cada rega avança uma fase: Semente -> Broto -> Crescendo -> Final.
    public bool Regar()
    {
        if (!ComSede) return false;

        Fase = Mathf.Min(Fase + 1, ItemData.FaseFinal);
        AtualizarPlanta();
        DefinirRegado(true);
        return true;
    }

    // Regado = vaso escuro e sem sede. Com sede = cor normal e ícone de sede.
    void DefinirRegado(bool regado)
    {
        Regado = regado;
        if (sr) sr.color = regado
            ? new Color(corOriginal.r * escurecer, corOriginal.g * escurecer, corOriginal.b * escurecer, corOriginal.a)
            : corOriginal;
        if (iconeSede) iconeSede.SetActive(!regado);
    }

    void AtualizarPlanta()
    {
        if (!plantaRenderer) return;
        plantaRenderer.sprite = Semente ? Semente.SpriteDaFase(Fase) : null;
        plantaRenderer.enabled = Semente != null;

        // Mesma posição em todas as fases; tamanho padronizado conforme a fase
        var t = plantaRenderer.transform;
        t.localPosition = new Vector3(posicaoPlanta.x, posicaoPlanta.y, t.localPosition.z);
        if (plantaRenderer.sprite && tamanhoPorFase != null && tamanhoPorFase.Length > 0)
        {
            float alvo = tamanhoPorFase[Mathf.Clamp(Fase, 0, tamanhoPorFase.Length - 1)];
            Vector2 desenho = TamanhoDoDesenho(plantaRenderer.sprite);
            float maior = Mathf.Max(desenho.x, desenho.y);
            if (maior > 0f)
            {
                // Escala no mundo para o lado maior do desenho ter "alvo" unidades,
                // compensando a escala do vaso (pai), que não é igual em x e y
                float s = alvo / maior;
                Vector3 pai = t.parent ? t.parent.lossyScale : Vector3.one;
                t.localScale = new Vector3(s / Mathf.Abs(pai.x), s / Mathf.Abs(pai.y), 1f);
            }
        }
    }

    // Vaso visível mais perto do ponto (mundo), dentro do raioPlantio dele. null = nenhum perto.
    public static Vaso MaisPerto(Vector2 ponto)
    {
        Vaso melhor = null;
        float menor = float.MaxValue;
        foreach (var v in todos)
        {
            if (v == null || !v.isActiveAndEnabled) continue;
            float d = Vector2.Distance(ponto, v.transform.position);
            if (d <= v.raioPlantio && d < menor) { menor = d; melhor = v; }
        }
        return melhor;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green; // mostra no editor o alcance do plantio
        Gizmos.DrawWireSphere(transform.position, raioPlantio);
    }

    // Chamado pelo InventorySlot quando uma semente é solta perto do vaso.
    // Retorna true se plantou (aí o inventário consome 1 unidade).
    public bool Plantar(ItemData item)
    {
        if (item == null || !item.ehSemente || Semente != null) return false;

        Semente = item;
        Fase = 0;
        AtualizarPlanta();
        DefinirRegado(true); // dia do plantio já conta como cuidado: vaso escuro, sem sede
        return true;
    }

    void Encaixar()
    {
        Transform novo = HolderMaisPerto();

        // Soltou em um slot: passa a ser o novo. Soltou fora: continua no anterior.
        if (novo != null && novo != holderAtual)
        {
            // Slot ocupado: troca de lugar com o vaso que está lá
            var outro = OcupanteDe(novo);
            if (outro != null)
            {
                if (holderAtual == null) novo = null; // sem slot para trocar: não encaixa
                else outro.holderAtual = holderAtual;
            }
            if (novo != null) holderAtual = novo;
        }

        if (holderAtual != null)
            SeguirHolder();
    }
}