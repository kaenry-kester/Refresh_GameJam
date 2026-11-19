using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Tela "Expedição concluída": aparece em QUALQUER cena quando os dias da expedição terminam.
// Mostra a semente ganha; o botão "Pegar" coloca ela no inventário e libera uma nova expedição.
// Monte num Canvas próprio e salve como prefab "TelaRecompensa" dentro de uma pasta Resources:
// o jogo cria sozinho e mantém entre as cenas (igual ao PainelVaso).
// Este script fica no objeto RAIZ (sempre ativo); "Painel" é o filho que aparece/some.
public class TelaRecompensa : MonoBehaviour
{
    const string PREFAB = "TelaRecompensa"; // Assets/.../Resources/TelaRecompensa.prefab

    static TelaRecompensa instancia;

    [SerializeField] GameObject painel;   // filho: a tela em si (começa desativado)
    [SerializeField] Image iconeSemente;
    [SerializeField] TMP_Text texto;
    [SerializeField] string formato = "Expedição concluída!\nVocê encontrou: {0}";
    [SerializeField] string textoInventarioCheio = "Inventário cheio! Libere um espaço para pegar a semente.";
    [SerializeField] Button botaoPegar;
    [SerializeField] AudioClip somConcluida;
    [SerializeField, Range(0f, 1f)] float volume = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CriarAoIniciar()
    {
        if (instancia) return;
        var prefab = Resources.Load<GameObject>(PREFAB);
        if (prefab) Instantiate(prefab).name = PREFAB;
        else Debug.LogWarning("TelaRecompensa: prefab 'Resources/TelaRecompensa' não encontrado.");
    }

    void Awake()
    {
        if (instancia && instancia != this) { Destroy(transform.root.gameObject); return; }
        instancia = this;
        DontDestroyOnLoad(transform.root.gameObject);

        if (botaoPegar) botaoPegar.onClick.AddListener(Pegar);
        if (iconeSemente) iconeSemente.preserveAspect = true;
        if (painel) painel.SetActive(false);
    }

    void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }

    void Update()
    {
        // A expedição terminou e a tela ainda não está aberta: mostra
        if (painel && !painel.activeSelf && EstadoExpedicao.Concluida) Mostrar();
    }

    void Mostrar()
    {
        var item = EstadoExpedicao.Recompensa;
        painel.SetActive(true);

        if (iconeSemente)
        {
            iconeSemente.sprite = item ? item.icone : null;
            iconeSemente.enabled = item && item.icone;
        }
        string nome = item ? (string.IsNullOrEmpty(item.nome) ? item.name : item.nome.Trim()) : "nada";
        if (texto) texto.text = string.Format(formato, nome);

        if (somConcluida)
        {
            var go = new GameObject("SomRecompensa");
            DontDestroyOnLoad(go);
            var fonte = go.AddComponent<AudioSource>();
            fonte.spatialBlend = 0f;
            fonte.PlayOneShot(somConcluida, volume);
            Destroy(go, somConcluida.length + 0.1f);
        }
    }

    void Pegar()
    {
        var item = EstadoExpedicao.Recompensa;
        if (!item)
            Debug.LogWarning("TelaRecompensa: esta expedição está sem semente no campo 'Recompensa' do PainelExpedicoes.");
        else if (!Inventario.Adicionar(item)) // a semente vai direto para o inventário
        {
            if (texto) texto.text = textoInventarioCheio; // a tela fica aberta até ter espaço
            return;
        }

        EstadoExpedicao.Finalizar();
        painel.SetActive(false);
    }
}
