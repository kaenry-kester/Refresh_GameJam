using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Slot onde um vaso encaixa. Coloque na GMP_Scene_Top com Vaso = 1..6 (um número diferente em cada).
// Fica onde você posicionou no editor na Top; nas outras cenas fica escondido.
// Se tiver SpriteRenderer (desenho do slot), ele só aparece na Top.
// Enquanto o jogador arrasta um vaso novo do inventário, os slots LIVRES ganham um destaque colorido.
public class Vaso_Holder : MonoBehaviour
{
    public int Vaso;

    [Header("Destaque (slot livre enquanto arrasta um vaso)")]
    [SerializeField] Color corDestaque = new Color(0.45f, 1f, 0.45f, 0.45f);
    [Tooltip("Opcional: objeto filho para usar como destaque. Vazio = cria um círculo colorido sozinho.")]
    [SerializeField] SpriteRenderer destaque;
    [SerializeField] float diametroDestaque = 0f; // 0 = usa o tamanho do collider do slot

    static Dictionary<int, Vaso_Holder> instancias = new Dictionary<int, Vaso_Holder>();
    static bool destacando;

    // Liga/desliga o destaque de todos os slots livres (chamado ao arrastar um vaso do inventário)
    public static void Destacar(bool ligar)
    {
        destacando = ligar;
        foreach (var h in instancias.Values)
            if (h) h.AtualizarDestaque();
    }

    void AtualizarDestaque()
    {
        bool mostrar = destacando && gameObject.activeInHierarchy && !global::Vaso.SlotOcupado(transform);
        if (mostrar && !destaque) CriarDestaque();
        if (destaque) destaque.gameObject.SetActive(mostrar);
    }

    void Update()
    {
        // Pisca suave enquanto destacado
        if (destacando && destaque && destaque.gameObject.activeSelf)
        {
            var c = corDestaque;
            c.a *= 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 6f);
            destaque.color = c;
        }
    }

    // Círculo colorido do tamanho do slot, na frente do fundo e atrás dos vasos
    void CriarDestaque()
    {
        var go = new GameObject("Destaque");
        go.transform.SetParent(transform, false);
        destaque = go.AddComponent<SpriteRenderer>();
        destaque.sprite = Circulo();
        destaque.color = corDestaque;
        destaque.sortingOrder = 0; // vasos estão em 1

        float d = diametroDestaque;
        if (d <= 0f && TryGetComponent(out CircleCollider2D col))
            d = col.radius * 2f * Mathf.Abs(transform.lossyScale.x);
        if (d <= 0f) d = 1f;

        Vector3 s = transform.lossyScale;
        go.transform.localScale = new Vector3(d / Mathf.Abs(s.x), d / Mathf.Abs(s.y), 1f);
    }

    static Sprite circulo;
    static Sprite Circulo()
    {
        if (circulo) return circulo;
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        float r = n / 2f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                float a = Mathf.Clamp01(r - dist); // borda suave
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        return circulo = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n); // 1 unidade de diâmetro
    }

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
        if (destaque) destaque.gameObject.SetActive(false);
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
