
using UnityEngine;
using System;

public class GerenciadorTempo : MonoBehaviour
{
    [Header("Configuracoes do Dia")]
    public float duracaoDia = 120f;

    public static bool estaDeNoite { get; private set; }

    // Dia atual (o dia inicial é o 0). Sobe quando a noite é encerrada.
    public static int Dia { get; private set; } = 0;

    public static event Action AoMudarPeriodo;
    public static event Action AoNovoDia; // vasos escutam para ficar com sede

    private static float tempoDecorrido = 0f;
    private static bool inicializado = false;

    private static GerenciadorTempo controladorAtivo;

    void Awake()
    {
        if (!inicializado)
        {
            estaDeNoite = false;
            tempoDecorrido = 0f;
            inicializado = true;
        }
    }

    void OnEnable()
    {
        controladorAtivo = this;
    }

    void OnDisable()
    {
        if (controladorAtivo == this)
            controladorAtivo = null;
    }

    void Update()
    {
        // Apenas um gerenciador conta o tempo
        if (controladorAtivo != this)
            return;

        if (estaDeNoite)
            return;

        tempoDecorrido += Time.deltaTime;

        if (tempoDecorrido >= duracaoDia)
        {
            estaDeNoite = true;

            Debug.Log("Anoiteceu no Refresh!");

            AoMudarPeriodo?.Invoke();
        }
    }

    public void EncerrarDia() => AvancarDia();

    // Encerra a noite e começa o próximo dia (pode ser chamado de qualquer script)
    public static void AvancarDia()
    {
        if (!estaDeNoite)
            return;

        estaDeNoite = false;
        tempoDecorrido = 0f;
        Dia++;

        Debug.Log("Novo dia iniciado! Dia " + Dia);

        AoMudarPeriodo?.Invoke();
        AoNovoDia?.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarSistema()
    {
        estaDeNoite = false;
        tempoDecorrido = 0f;
        inicializado = false;
        controladorAtivo = null;
        AoMudarPeriodo = null;
        AoNovoDia = null;
        Dia = 0;
    }
}
