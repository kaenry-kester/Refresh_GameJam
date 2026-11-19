using UnityEngine;

// Estado da expedição em andamento (só uma por vez). Static: sobrevive à troca de cenas.
// A duração conta em DIAS DO JOGO (cada noite encerrada pela porta = 1 dia).
public static class EstadoExpedicao
{
    public static bool Ativa { get; private set; }
    public static int Indice { get; private set; } = -1; // qual expedição (0 a 3)
    public static int DiaFim { get; private set; }       // dia em que ela volta
    public static ItemData Recompensa { get; private set; }

    public static int DiasRestantes => Ativa ? Mathf.Max(0, DiaFim - GerenciadorTempo.Dia) : 0;
    public static bool Concluida => Ativa && GerenciadorTempo.Dia >= DiaFim;

    public static void Iniciar(int indice, int duracaoDias, ItemData recompensa)
    {
        Ativa = true;
        Indice = indice;
        DiaFim = GerenciadorTempo.Dia + Mathf.Max(1, duracaoDias);
        Recompensa = recompensa;
    }

    // Chamado quando o jogador pega a recompensa: libera uma nova expedição
    public static void Finalizar()
    {
        Ativa = false;
        Indice = -1;
        Recompensa = null;
    }

    // Começa sem expedição a cada Play, mesmo sem domain reload.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Resetar() => Finalizar();
}
