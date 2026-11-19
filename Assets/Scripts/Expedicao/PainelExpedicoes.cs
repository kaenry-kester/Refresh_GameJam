using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Tela de Expedições. Coloque num objeto da cena (ex.: no Canvas).
// - Cada expedição tem: o botão da aba (BotaoExpedicao1..4), custo, duração (dias do jogo) e a semente de recompensa.
// - "Iniciar expedição" (botão comum): cobra o custo e começa a contar os dias.
// - Com uma expedição em andamento, as abas das OUTRAS expedições e o Iniciar ficam trancados.
// - Ao completar os dias, a TelaRecompensa aparece (em qualquer cena) com a semente.
public class PainelExpedicoes : MonoBehaviour
{
    [System.Serializable]
    public class Expedicao
    {
        public string nome = "Expedição";  // só para identificar no Inspector
        public Button aba;                  // BotaoExpedicao1, 2, 3 ou 4
        public int custo = 500;
        public int duracaoDias = 3;
        public ItemData recompensa;         // a semente que o jogador ganha
    }

    [SerializeField] Expedicao[] expedicoes =
    {
        new Expedicao { nome = "Expedição 1", custo = 700, duracaoDias = 3 },
        new Expedicao { nome = "Expedição 2", custo = 500, duracaoDias = 4 },
        new Expedicao { nome = "Expedição 3", custo = 350, duracaoDias = 2 },
        new Expedicao { nome = "Expedição 4", custo = 500, duracaoDias = 4 },
    };

    [SerializeField] Button botaoIniciar;  // "Iniciar expedição"
    [SerializeField] AudioClip somIniciar;
    [SerializeField, Range(0f, 1f)] float volume = 1f;

    [Header("Opcional")]
    [SerializeField] TMP_Text aviso; // "Dinheiro insuficiente", "Em andamento: faltam X dias"...
    [SerializeField] string textoSemDinheiro = "Dinheiro insuficiente";
    [SerializeField] string textoEmAndamento = "Expedição em andamento: faltam {0} dia(s)";
    [SerializeField] string textoIniciou = "Expedição iniciada!";

    int selecionada = -1; // aba aberta agora

    void Awake()
    {
        for (int i = 0; i < expedicoes.Length; i++)
        {
            int indice = i; // cópia para o clique
            if (expedicoes[i].aba) expedicoes[i].aba.onClick.AddListener(() => selecionada = indice);
        }
        if (botaoIniciar) botaoIniciar.onClick.AddListener(Iniciar);

        // Se já tem expedição em andamento, a aba dela é a selecionada
        if (EstadoExpedicao.Ativa) selecionada = EstadoExpedicao.Indice;
    }

    void Update()
    {
        bool ativa = EstadoExpedicao.Ativa;

        // Trava as abas das outras expedições enquanto uma está acontecendo
        for (int i = 0; i < expedicoes.Length; i++)
            if (expedicoes[i].aba) expedicoes[i].aba.interactable = !ativa || i == EstadoExpedicao.Indice;

        if (botaoIniciar) botaoIniciar.interactable = !ativa && selecionada >= 0;

        if (ativa && aviso) aviso.text = string.Format(textoEmAndamento, EstadoExpedicao.DiasRestantes);
    }

    public void Iniciar()
    {
        if (EstadoExpedicao.Ativa || selecionada < 0 || selecionada >= expedicoes.Length) return;

        var e = expedicoes[selecionada];
        if (!Dinheiro.Gastar(e.custo))
        {
            if (aviso) aviso.text = textoSemDinheiro;
            return;
        }

        EstadoExpedicao.Iniciar(selecionada, e.duracaoDias, e.recompensa);
        if (aviso) aviso.text = textoIniciou;
        TocarSom();
    }

    void TocarSom()
    {
        if (!somIniciar) return;
        var go = new GameObject("SomExpedicao");
        DontDestroyOnLoad(go);
        var fonte = go.AddComponent<AudioSource>();
        fonte.spatialBlend = 0f;
        fonte.PlayOneShot(somIniciar, volume);
        Destroy(go, somIniciar.length + 0.1f);
    }
}
