
using UnityEngine;
using System;

public class GerenciadorTempo : MonoBehaviour
{
    [Header("Configuracoes do Dia")]
    public float duracaoDia = 120f;

    public static bool estaDeNoite { get; private set; }

    public static event Action AoMudarPeriodo;

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

    public void EncerrarDia()
    {
        if (!estaDeNoite)
            return;

        estaDeNoite = false;
        tempoDecorrido = 0f;

        Debug.Log("Novo dia iniciado!");

        AoMudarPeriodo?.Invoke();
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
    }
}
