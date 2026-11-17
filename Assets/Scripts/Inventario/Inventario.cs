using System;
using System.Collections.Generic;
using UnityEngine;

// Dados do inventário (sem UI). Por ser static, sobrevive à troca de cenas.
// Uso: Inventario.Adicionar(item);  Inventario.Remover(item);
public static class Inventario
{
    public const int Capacidade = 8;

    public class Entrada { public ItemData item; public int qtd; }
    static readonly List<Entrada> itens = new();

    public static IReadOnlyList<Entrada> Itens => itens;
    public static event Action Mudou;

    public static bool Adicionar(ItemData item, int qtd = 1)
    {
        if (item == null || qtd <= 0) return false;

        var e = itens.Find(x => x.item == item);
        if (e != null) e.qtd += qtd;
        else if (itens.Count < Capacidade) itens.Add(new Entrada { item = item, qtd = qtd });
        else return false; // cheio

        Mudou?.Invoke();
        return true;
    }

    public static bool Remover(ItemData item, int qtd = 1)
    {
        var e = itens.Find(x => x.item == item);
        if (e == null || e.qtd < qtd) return false;

        e.qtd -= qtd;
        if (e.qtd == 0) itens.Remove(e);
        Mudou?.Invoke();
        return true;
    }

    public static int Quantidade(ItemData item) => itens.Find(x => x.item == item)?.qtd ?? 0;

    // Garante inventário vazio a cada Play, mesmo com "Enter Play Mode Options" sem domain reload.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Resetar()
    {
        itens.Clear();
        Mudou = null;
    }
}
