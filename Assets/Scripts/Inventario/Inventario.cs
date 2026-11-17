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

    public class Entrada
    {
        public ItemData item;
        public int qtd;
        public bool noFrigorifico; // controlado pelo Inventario
        public float inicio;       // Time.time em que entrou no frigorífico

        // 0 = acabou de entrar, 1 = pronta (pós). Fora do frigorífico é sempre 0.
        public float Progresso => noFrigorifico ? Mathf.Clamp01((Time.time - inicio) / TempoFrigorifico) : 0f;
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
    public static bool PodeAdicionarNoSlot(ItemData item, int i, Local l = Local.Inventario, int qtd = 1)
    {
        if (item == null || !Valido(l, i)) return false;
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

        a[i].qtd -= qtd;
        if (a[i].qtd == 0) a[i] = null;
        Avisar();
        return true;
    }

    public static bool Mover(int de, int para) => Mover(Local.Inventario, de, Local.Inventario, para);

    // Move o item de um slot para outro (pode ser entre inventário e frigorífico).
    // Destino vazio -> move. Mesmo item -> junta as pilhas. Item diferente -> troca de lugar.
    public static bool Mover(Local lDe, int de, Local lPara, int para)
    {
        if (!Valido(lDe, de) || !Valido(lPara, para) || (lDe == lPara && de == para)) return false;

        var origem = Array(lDe);
        var destino = Array(lPara);
        var a = origem[de];
        var b = destino[para];
        if (a == null) return false;

        int lim = Limite(lPara);

        if (b != null && b.item == a.item)
        {
            // Mesmo item: junta só o que couber no destino
            int n = Mathf.Min(lim - b.qtd, a.qtd);
            if (n <= 0) return false;
            b.qtd += n;
            a.qtd -= n;
            if (a.qtd == 0) origem[de] = null;
        }
        else if (b == null && a.qtd > lim)
        {
            // Destino vazio mas não cabe tudo (ex.: frigorífico): leva só o que cabe
            destino[para] = new Entrada { item = a.item, qtd = lim };
            a.qtd -= lim;
        }
        else
        {
            // Troca de lugar, se os dois couberem no novo lugar
            if (a.qtd > lim || (b != null && b.qtd > Limite(lDe))) return false;
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

    // Marca o que entrou/saiu do frigorífico (inicia ou zera o tempo) e avisa a UI
    static void Avisar()
    {
        foreach (var e in frigorifico)
            if (e != null && !e.noFrigorifico) { e.noFrigorifico = true; e.inicio = Time.time; }
        foreach (var e in slots)
            if (e != null) e.noFrigorifico = false;

        Mudou?.Invoke();
    }

    static int IndiceDe(ItemData item) => System.Array.FindIndex(slots, e => e != null && e.item == item);
    static bool Valido(Local l, int i) => i >= 0 && i < Array(l).Length;

    // Garante inventário e frigorífico vazios a cada Play, mesmo sem domain reload.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Resetar()
    {
        System.Array.Clear(slots, 0, slots.Length);
        System.Array.Clear(frigorifico, 0, frigorifico.Length);
        Mudou = null;
    }
}
