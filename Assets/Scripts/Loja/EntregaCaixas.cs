using UnityEngine;

// Fica na GMP_Scene_Right. Mostra uma caixa para cada vaso comprado na Loja e ainda não aberto.
// Clicar numa caixa: ela some, toca o som e o vaso vai para o inventário.
// Monte uma caixa modelo (UI Image com o sprite "caixa 1_0" + componente CaixaVaso) no Canvas;
// o script esconde o modelo e cria as cópias, uma ao lado da outra.
public class EntregaCaixas : MonoBehaviour
{
    // Caixas compradas e ainda não abertas (o item de cada uma; null = vaso comum).
    // Static: sobrevive à troca de cenas.
    static readonly System.Collections.Generic.List<ItemData> pendentes = new System.Collections.Generic.List<ItemData>();
    public static int Pendentes => pendentes.Count;

    // Chamado pela compra (Loja / Upgrade)
    public static void Encomendar(ItemData item) => pendentes.Add(item);

    [SerializeField] CaixaVaso modelo;      // a caixa de referência (posição da 1ª caixa)
    [SerializeField] Vector2 espacamento = new Vector2(140f, 0f); // distância entre uma caixa e a próxima
    [Tooltip("Item padrão (vaso comum), usado quando a compra não definiu outro")]
    [SerializeField] ItemData itemVaso;
    [SerializeField] AudioClip somAbrir;
    [SerializeField, Range(0f, 1f)] float volume = 1f;

    void Start()
    {
        if (!modelo) return;
        modelo.gameObject.SetActive(false);

        var rt0 = (RectTransform)modelo.transform;
        for (int i = 0; i < pendentes.Count; i++)
        {
            var c = Instantiate(modelo, modelo.transform.parent);
            c.name = "Caixa " + (i + 1);
            ((RectTransform)c.transform).anchoredPosition = rt0.anchoredPosition + espacamento * i;
            c.dono = this;
            c.item = pendentes[i] ? pendentes[i] : itemVaso;
            c.gameObject.SetActive(true);
        }
    }

    public void Abrir(CaixaVaso caixa)
    {
        var item = caixa.item ? caixa.item : itemVaso;
        if (!item || !Inventario.Adicionar(item)) return; // inventário cheio: a caixa fica

        // Tira da lista a encomenda deste item (null conta como vaso comum)
        int i = pendentes.FindIndex(p => (p ? p : itemVaso) == item);
        if (i >= 0) pendentes.RemoveAt(i);

        TocarSom();
        Destroy(caixa.gameObject);
    }

    void TocarSom()
    {
        if (!somAbrir) return;
        var go = new GameObject("SomCaixa");
        DontDestroyOnLoad(go); // não corta se o jogador trocar de cena
        var fonte = go.AddComponent<AudioSource>();
        fonte.spatialBlend = 0f;
        fonte.PlayOneShot(somAbrir, volume);
        Destroy(go, somAbrir.length + 0.1f);
    }

    // Começa sem caixas a cada Play, mesmo sem domain reload.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Resetar() => pendentes.Clear();
}
