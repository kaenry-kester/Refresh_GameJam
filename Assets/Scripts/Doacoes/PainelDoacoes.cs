using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Tela de Doações. Coloque num objeto da cena (ex.: no Canvas).
// - Clicou no botão de uma doação -> aparece a imagem com as informações dela.
// - Aceitou -> paga (dinheiro ou sementes) e aparece "Doação realizada! Você recebeu: ...".
// - Doação feita fica travada para sempre (não dá para repetir).
// Tipos:
//   Sementes: abre a janela onde o jogador arrasta N sementes do inventário (SlotDoacao) e confirma.
//   Dinheiro: cobra o valor na hora.
public class PainelDoacoes : MonoBehaviour
{
    public enum Tipo { Sementes, Dinheiro }

    [System.Serializable]
    public class Doacao
    {
        public string nome = "Doação";
        public Button botao;          // botão da lista (Doação 1, 2, 3)
        public GameObject telaInfo;   // imagem com as informações da doação
        public Button botaoAceitar;   // botão "aceitar" dentro da telaInfo
        public Tipo tipo = Tipo.Dinheiro;
        public int custoDinheiro;     // tipo Dinheiro

        [Header("Recompensa")]
        public ItemData recompensaItem;   // ex.: uma semente, ou o Vaso_Luz_Item
        public int recompensaQuantidade = 1;
        public int recompensaDinheiro;
        [Tooltip("Texto da recompensa na tela final (vazio = monta sozinho, ex.: \"2x Vazo com luz\")")]
        public string textoRecompensa;
    }

    [SerializeField] Doacao[] doacoes =
    {
        new Doacao { nome = "Doação 1", tipo = Tipo.Sementes },
        new Doacao { nome = "Doação 2", tipo = Tipo.Dinheiro, custoDinheiro = 350 },
        new Doacao { nome = "Doação 3", tipo = Tipo.Dinheiro, custoDinheiro = 700, recompensaQuantidade = 2 },
    };

    [Header("Janela das sementes (Doação 1)")]
    [SerializeField] GameObject janelaSementes;   // começa desativada
    [SerializeField] SlotDoacao[] slotsSementes;  // os 2 espaços
    [SerializeField] Button botaoConfirmarSementes;
    [SerializeField] Button botaoCancelarSementes; // opcional

    [Header("Tela final")]
    [SerializeField] GameObject telaResultado;     // começa desativada
    [SerializeField] TMP_Text textoResultado;
    [SerializeField] Text textoResultadoLegacy;    // se a tela usar Text (Legacy)
    [SerializeField] string formatoResultado = "Doação realizada!\nVocê recebeu: {0}";
    [SerializeField] Button botaoOkResultado;      // fecha a tela final

    [Header("Avisos (opcional)")]
    [SerializeField] TMP_Text aviso;
    [SerializeField] string textoSemDinheiro = "Dinheiro insuficiente";
    [SerializeField] string textoInventarioCheio = "Inventário cheio";
    [SerializeField] AudioClip somDoacao;
    [SerializeField, Range(0f, 1f)] float volume = 1f;

    // Doações já feitas (índices). Static: sobrevive à troca de cenas.
    static readonly HashSet<int> feitas = new HashSet<int>();
    static PainelDoacoes atual;
    int aberta = -1;

    void Awake()
    {
        atual = this;
        for (int i = 0; i < doacoes.Length; i++)
        {
            int indice = i;
            var d = doacoes[i];
            if (d.botao) d.botao.onClick.AddListener(() => AbrirInfo(indice));
            if (d.botaoAceitar) d.botaoAceitar.onClick.AddListener(() => Aceitar(indice));
            if (d.telaInfo) d.telaInfo.SetActive(false);
        }

        if (janelaSementes) janelaSementes.SetActive(false);
        if (telaResultado) telaResultado.SetActive(false);
        if (botaoConfirmarSementes) botaoConfirmarSementes.onClick.AddListener(ConfirmarSementes);
        if (botaoCancelarSementes) botaoCancelarSementes.onClick.AddListener(FecharJanelaSementes);
        if (botaoOkResultado) botaoOkResultado.onClick.AddListener(() => telaResultado.SetActive(false));
        foreach (var s in slotsSementes) if (s) s.Mudou += AtualizarConfirmar;
    }

    void OnDestroy() { if (atual == this) atual = null; }

    void Update()
    {
        // Doação feita: botão travado
        for (int i = 0; i < doacoes.Length; i++)
            if (doacoes[i].botao) doacoes[i].botao.interactable = !feitas.Contains(i);
    }

    void AbrirInfo(int i)
    {
        if (feitas.Contains(i)) return;
        aberta = i;
        for (int k = 0; k < doacoes.Length; k++)
            if (doacoes[k].telaInfo) doacoes[k].telaInfo.SetActive(k == i);
        if (aviso) aviso.text = "";
    }

    void Aceitar(int i)
    {
        if (feitas.Contains(i)) return;
        var d = doacoes[i];

        if (d.tipo == Tipo.Sementes)
        {
            aberta = i;
            foreach (var s in slotsSementes) if (s) s.Limpar();
            if (janelaSementes) janelaSementes.SetActive(true);
            AtualizarConfirmar();
            return;
        }

        // Dinheiro: confere espaço da recompensa antes de cobrar
        if (!CabeRecompensa(d)) { Avisar(textoInventarioCheio); return; }
        if (!Dinheiro.Gastar(d.custoDinheiro)) { Avisar(textoSemDinheiro); return; }
        Concluir(i);
    }

    // ---- Doação de sementes ----

    void AtualizarConfirmar()
    {
        if (!botaoConfirmarSementes) return;
        bool cheio = slotsSementes.Length > 0;
        foreach (var s in slotsSementes) if (!s || !s.Item) cheio = false;
        botaoConfirmarSementes.interactable = cheio;
    }

    void ConfirmarSementes()
    {
        if (aberta < 0 || feitas.Contains(aberta)) return;
        foreach (var s in slotsSementes) if (!s || !s.Item) return; // falta semente

        var d = doacoes[aberta];
        if (!CabeRecompensa(d)) { Avisar(textoInventarioCheio); return; }

        foreach (var s in slotsSementes) Inventario.Remover(s.Item, 1); // agora sim saem do inventário
        FecharJanelaSementes();
        Concluir(aberta);
    }

    void FecharJanelaSementes()
    {
        foreach (var s in slotsSementes) if (s) s.Limpar();
        if (janelaSementes) janelaSementes.SetActive(false);
    }

    // A mesma semente pode ir em 2 espaços só se o jogador tiver 2 dela no inventário
    public static bool TemSementeSobrando(ItemData item, SlotDoacao ignorar)
    {
        if (!atual) return false;
        int usadas = 0;
        foreach (var s in atual.slotsSementes)
            if (s && s != ignorar && s.Item == item) usadas++;
        return Inventario.Quantidade(item) > usadas;
    }

    // ---- Final ----

    bool CabeRecompensa(Doacao d)
    {
        if (!d.recompensaItem || d.recompensaQuantidade <= 0) return true;
        // Já tem a pilha desse item, ou há algum slot vazio
        if (Inventario.Quantidade(d.recompensaItem) > 0) return true;
        foreach (var e in Inventario.Slots) if (e == null) return true;
        return false;
    }

    void Concluir(int i)
    {
        var d = doacoes[i];
        if (d.recompensaItem && d.recompensaQuantidade > 0) Inventario.Adicionar(d.recompensaItem, d.recompensaQuantidade);
        if (d.recompensaDinheiro > 0) Dinheiro.Ganhar(d.recompensaDinheiro);

        feitas.Add(i);
        if (d.telaInfo) d.telaInfo.SetActive(false);
        aberta = -1;

        string texto = string.Format(formatoResultado, TextoRecompensa(d));
        if (textoResultado) textoResultado.text = texto;
        if (textoResultadoLegacy) textoResultadoLegacy.text = texto;
        if (telaResultado) telaResultado.SetActive(true);
        TocarSom();
    }

    static string TextoRecompensa(Doacao d)
    {
        if (!string.IsNullOrEmpty(d.textoRecompensa)) return d.textoRecompensa;
        var partes = new List<string>();
        if (d.recompensaItem && d.recompensaQuantidade > 0)
        {
            string nome = string.IsNullOrEmpty(d.recompensaItem.nome) ? d.recompensaItem.name : d.recompensaItem.nome.Trim();
            partes.Add(d.recompensaQuantidade > 1 ? $"{d.recompensaQuantidade}x {nome}" : nome);
        }
        if (d.recompensaDinheiro > 0) partes.Add($"R$ {d.recompensaDinheiro},00");
        return partes.Count > 0 ? string.Join(" e ", partes) : "o agradecimento da comunidade";
    }

    void Avisar(string t)
    {
        if (aviso) aviso.text = t;
    }

    void TocarSom()
    {
        if (!somDoacao) return;
        var go = new GameObject("SomDoacao");
        DontDestroyOnLoad(go);
        var fonte = go.AddComponent<AudioSource>();
        fonte.spatialBlend = 0f;
        fonte.PlayOneShot(somDoacao, volume);
        Destroy(go, somDoacao.length + 0.1f);
    }

    // Começa sem doações feitas a cada Play, mesmo sem domain reload.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Resetar() => feitas.Clear();
}
