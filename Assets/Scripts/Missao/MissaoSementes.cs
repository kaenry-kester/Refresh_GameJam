using System;
using TMPro;
using UnityEngine;

// Contador da missão "encontre 6 sementes".
// Coloque no Text (TMP) do MenuMissoes. Ele mostra "coletadas/meta" (ex.: 3/6) e,
// ao completar, o texto fica verde e riscado.
// O contador é static: sobrevive à troca de cenas, então sementes pegas em
// outras cenas (Fase1_Top, Left...) também contam.
[RequireComponent(typeof(TMP_Text))]
public class MissaoSementes : MonoBehaviour
{
    [SerializeField] int meta = 6;
    [SerializeField] Color corConcluido = Color.green;

    [Tooltip("Objetos que aparecem ao completar a meta (ex.: Image, Dialogo3, botão para a Tela Preta 1)")]
    [SerializeField] GameObject[] mostrarAoCompletar;

    [Header("Som ao completar")]
    [SerializeField] AudioClip somSucesso;          // arraste Assets/Som/sucesso
    [SerializeField, Range(0f, 1f)] float volume = 1f;

    static int coletadas;
    static event Action Mudou;

    // Copiados do componente para tocar o som mesmo se a última semente for pega em outra cena
    static int metaGlobal = 6;
    static AudioClip somGlobal;
    static float volumeGlobal = 1f;

    TMP_Text texto;

    // Chamado pelo ColetarItem sempre que um item entra no inventário vindo da tela
    public static void Coletou(ItemData item)
    {
        if (item == null || !item.ehSemente) return;
        coletadas++;
        if (coletadas == metaGlobal) TocarSucesso(); // só na hora que completa
        Mudou?.Invoke();
    }

    static void TocarSucesso()
    {
        if (!somGlobal) return;

        // Objeto temporário que sobrevive à troca de cena até o som acabar
        var go = new GameObject("SomSucesso");
        DontDestroyOnLoad(go);
        var fonte = go.AddComponent<AudioSource>();
        fonte.spatialBlend = 0f;
        fonte.PlayOneShot(somGlobal, volumeGlobal);
        Destroy(go, somGlobal.length + 0.1f);
    }

    void Awake()
    {
        texto = GetComponent<TMP_Text>();
        metaGlobal = meta;
        somGlobal = somSucesso;
        volumeGlobal = volume;
    }

    void OnEnable()  { Mudou += Atualizar; Atualizar(); }
    void OnDisable() { Mudou -= Atualizar; }
    void Start() => Atualizar(); // de novo depois do LembrarCena restaurar a cena

    void Atualizar()
    {
        texto.text = Mathf.Min(coletadas, meta) + "/" + meta;

        if (coletadas >= meta)
        {
            texto.color = corConcluido;
            texto.fontStyle |= FontStyles.Strikethrough;

            // Se completou em outra cena, aparece assim que o jogador voltar para esta
            foreach (var go in mostrarAoCompletar)
                if (go) go.SetActive(true);
        }
    }

    // Começa do zero a cada Play, mesmo sem domain reload.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Resetar()
    {
        coletadas = 0;
        Mudou = null;
        metaGlobal = 6;
        somGlobal = null;
        volumeGlobal = 1f;
    }
}
