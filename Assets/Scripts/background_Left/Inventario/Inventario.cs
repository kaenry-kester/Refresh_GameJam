using System;
using System.Collections.Generic;
using UnityEngine;

// Dados do inventário e do frigorífico (sem UI). Por ser static, sobrevive à troca de cenas.
// Cada posição do array é um slot fixo (null = vazio), assim dá para arrastar itens entre slots.
// Uso: Inventario.Adicionar(item);  Inventario.Remover(item);  Inventario.Mover(de, para);
public static class Inventario
{
    public enum Local { Inventario, Frigorifico }

    public const int Capacidade = 8;
    public const int CapacidadeFrigorifico = 12;
    public const int MaxPorSlotFrigorifico = 1; // no frigorífico só cabe 1 item por slot

    public const float TempoFrigorifico = 180f; // segundos (3 minutos) para a semente ficar "pós"
    public const float TempoSementeFusao = 8f;  // semente nova da fusão (versão "pré") fica pronta em 8 segundos

    public class Entrada
    {
        public ItemData item;
        public int qtd;
        public bool noFrigorifico; // controlado pelo Inventario
        public float inicio;       // Time.time em que entrou no frigorífico
        public float duracao = TempoFrigorifico; // definido ao entrar no frigorífico

        // 0 = acabou de entrar, 1 = pronta (pós). Fora do frigorífico é sempre 0.
        public float Progresso => noFrigorifico ? Mathf.Clamp01((Time.time - inicio) / Mathf.Max(duracao, 0.01f)) : 0f;
        public bool Pronta => Progresso >= 1f;
    }
    static readonly Entrada[] slots = new Entrada[Capacidade];
    static readonly Entrada[] frigorifico = new Entrada[CapacidadeFrigorifico];

    public static IReadOnlyList<Entrada> Slots => slots; // slots[i] == null -> slot vazio
    public static IReadOnlyList<Entrada> De(Local l) => Array(l);
    public static event Action Mudou;

    static Entrada[] Array(Local l) => l == Local.Frigorifico ? frigorifico : slots;
    static int Limite(Local l) => l == Local.Frigorifico ? MaxPorSlotFrigorifico : int.MaxValue;

    // Adiciona no inventário (empilha se já tiver, senão vai pro primeiro slot vazio)
    public static bool Adicionar(ItemData item, int qtd = 1)
    {
        if (item == null || qtd <= 0) return false;

        int i = IndiceDe(item);
        if (i >= 0) slots[i].qtd += qtd;
        else
        {
            i = System.Array.IndexOf(slots, null);
            if (i < 0) return false; // cheio
            slots[i] = new Entrada { item = item, qtd = qtd };
        }

        Avisar();
        return true;
    }

    // Coloca o item num slot específico (vazio ou com o mesmo item).
    // Na geladeira só entram sementes "pré" (da fusão ou da reconstrução). Sementes normais não.
    public static bool PodeIrPraGeladeira(ItemData item) => item && item.viraNoFrigorifico;

    public static bool PodeAdicionarNoSlot(ItemData item, int i, Local l = Local.Inventario, int qtd = 1)
    {
        if (item == null || !Valido(l, i)) return false;
        if (l == Local.Frigorifico && !PodeIrPraGeladeira(item)) return false;
        var e = Array(l)[i];
        if (e == null) return qtd <= Limite(l);
        return e.item == item && e.qtd + qtd <= Limite(l);
    }

    public static bool AdicionarNoSlot(ItemData item, int i, int qtd = 1, Local l = Local.Inventario)
    {
        if (qtd <= 0 || !PodeAdicionarNoSlot(item, i, l, qtd)) return false;

        var a = Array(l);
        if (a[i] == null) a[i] = new Entrada { item = item, qtd = qtd };
        else a[i].qtd += qtd;

        Avisar();
        return true;
    }

    public static bool Remover(ItemData item, int qtd = 1) => RemoverDoSlot(IndiceDe(item), qtd);

    public static bool RemoverDoSlot(int i, int qtd = 1, Local l = Local.Inventario)
    {
        if (!Valido(l, i)) return false;
        var a = Array(l);
        if (a[i] == null || a[i].qtd < qtd) return false;
        if (Fixo(a[i].item)) return false; // lista de afazeres nunca sai

        a[i].qtd -= qtd;
        if (a[i].qtd == 0) a[i] = null;
        Avisar();
        return true;
    }

    public static bool Mover(int de, int para) => Mover(Local.Inventario, de, Local.Inventario, para);

    // Move o item de um slot para outro (pode ser entre inventário e frigorífico).
    // Destino vazio -> move. Mesmo item -> junta as pilhas. Item diferente -> troca de lugar.
    // maxMover limita quantas unidades saem da origem (1 = move só uma semente da pilha).
    // Pilha com mais que maxMover em cima de item diferente -> não troca (volta pro slot).
    public static bool Mover(Local lDe, int de, Local lPara, int para, int maxMover = int.MaxValue)
    {
        Amadurecer(); // semente pré que ficou pronta sai do frigorífico já como semente de verdade
        if (!Valido(lDe, de) || !Valido(lPara, para) || (lDe == lPara && de == para)) return false;

        var origem = Array(lDe);
        var destino = Array(lPara);
        var a = origem[de];
        var b = destino[para];
        if (a == null) return false;
        if (Fixo(a.item) || (b != null && Fixo(b.item))) return false; // lista de afazeres não sai do lugar

        // Entrando na geladeira (vindo do inventário): só sementes "pré".
        // Vale também para a troca de lugar, quando o item do inventário iria para a geladeira.
        if (lPara == Local.Frigorifico && lDe != Local.Frigorifico && !PodeIrPraGeladeira(a.item)) return false;
        if (lDe == Local.Frigorifico && lPara != Local.Frigorifico && b != null && b.item != a.item
            && !PodeIrPraGeladeira(b.item)) return false;

        int lim = Limite(lPara);
        int leva = Mathf.Min(lim, maxMover); // máximo que pode sair da origem

        if (b != null && b.item == a.item)
        {
            // Mesmo item: junta só o que couber no destino
            int n = Mathf.Min(lim - b.qtd, a.qtd, maxMover);
            if (n <= 0) return false;
            b.qtd += n;
            a.qtd -= n;
            if (a.qtd == 0) origem[de] = null;
        }
        else if (b == null && a.qtd > leva)
        {
            // Destino vazio mas não vai tudo (frigorífico ou só 1 da pilha): leva só uma parte
            destino[para] = new Entrada { item = a.item, qtd = leva };
            a.qtd -= leva;
        }
        else
        {
            // Troca de lugar, se os dois couberem no novo lugar
            if (a.qtd > leva || (b != null && b.qtd > Limite(lDe))) return false;
            destino[para] = a;
            origem[de] = b;
        }

        Avisar();
        return true;
    }

    public static int Quantidade(ItemData item)
    {
        int i = IndiceDe(item);
        return i >= 0 ? slots[i].qtd : 0;
    }

    // Sementes "pré" (da fusão) que completaram o tempo no frigorífico viram a semente de verdade
    public static void Amadurecer()
    {
        bool mudou = false;
        foreach (var e in frigorifico)
            if (e != null && e.Pronta && e.item && e.item.viraNoFrigorifico)
            {
                e.item = e.item.viraNoFrigorifico;
                mudou = true;
            }
        if (mudou) Mudou?.Invoke();
    }

    // Marca o que entrou/saiu do frigorífico (inicia ou zera o tempo) e avisa a UI
    static void Avisar()
    {
        foreach (var e in frigorifico)
            if (e != null && !e.noFrigorifico)
            {
                e.noFrigorifico = true;
                e.inicio = Time.time;
                e.duracao = e.item && e.item.viraNoFrigorifico ? TempoSementeFusao : TempoFrigorifico;
            }
        foreach (var e in slots)
            if (e != null) e.noFrigorifico = false;

        Mudou?.Invoke();
    }

    static int IndiceDe(ItemData item) => System.Array.FindIndex(slots, e => e != null && e.item == item);
    static bool Valido(Local l, int i) => i >= 0 && i < Array(l).Length;

    // ---- Lista de afazeres: sempre no último slot do inventário ----
    public static bool Fixo(ItemData item) => item && item.ehListaAfazeres;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ColocarListaDeAfazeres()
    {
        var lista = Resources.Load<ItemData>("ListaAfazeres"); // Assets/Resources/ListaAfazeres.asset
        if (!lista) { Debug.LogWarning("Inventario: Resources/ListaAfazeres não encontrado."); return; }
        slots[Capacidade - 1] = new Entrada { item = lista, qtd = 1 };
    }

    // Garante inventário e frigorífico vazios a cada Play, mesmo sem domain reload.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Resetar()
    {
        System.Array.Clear(slots, 0, slots.Length);
        System.Array.Clear(frigorifico, 0, frigorifico.Length);
        Mudou = null;
    }
}
