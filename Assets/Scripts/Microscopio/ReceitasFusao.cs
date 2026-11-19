using System.Collections.Generic;
using UnityEngine;

// Lista de fusões (planta A + planta B = semente nova).
// O arquivo fica em Assets/Resources/ReceitasFusao (o jogo carrega sozinho).
// Use as SEMENTES das espécies nos campos (ex.: Semente_7 Maracujá nativo).
[CreateAssetMenu(menuName = "Inventario/Receitas de Fusao")]
public class ReceitasFusao : ScriptableObject
{
    [System.Serializable]
    public class Receita
    {
        public ItemData plantaA;
        public ItemData plantaB;
        public ItemData resultado; // semente nova (o jogador recebe a versão "pré")
    }

    public Receita[] receitas;

    static ReceitasFusao carregada;
    public static ReceitasFusao Atual => carregada ? carregada : carregada = Resources.Load<ReceitasFusao>("ReceitasFusao");

    // Ordem não importa: A + B = B + A. Plantas iguais nunca fundem.
    public ItemData Resultado(ItemData a, ItemData b)
    {
        if (!a || !b || a == b) return null;
        foreach (var r in receitas)
            if ((r.plantaA == a && r.plantaB == b) || (r.plantaA == b && r.plantaB == a))
                return r.resultado;
        return null;
    }

    // Espécies que podem entrar numa fusão (sem repetir), na ordem das receitas
    public List<ItemData> Plantas()
    {
        var lista = new List<ItemData>();
        foreach (var r in receitas)
        {
            if (r.plantaA && !lista.Contains(r.plantaA)) lista.Add(r.plantaA);
            if (r.plantaB && !lista.Contains(r.plantaB)) lista.Add(r.plantaB);
        }
        return lista;
    }
}
