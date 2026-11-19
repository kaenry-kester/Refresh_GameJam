using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Faz a cena "lembrar" como estava quando o jogador saiu, em vez de reiniciar.
// Guarda quais objetos estavam ativos/desativados e restaura tudo ao voltar
// (sementes coletadas continuam sumidas, diálogo já visto não aparece de novo, etc.).
// Coloque em UM objeto de cada cena que deve lembrar (ex.: o Canvas de cada Fase1_*).
// O estado é salvo pelo SceneController na hora de trocar de cena.
[DefaultExecutionOrder(-1000)] // restaura antes dos Start() dos outros scripts
public class LembrarCena : MonoBehaviour
{
    // nome da cena -> (caminho do objeto -> estava ativo?)
    static readonly Dictionary<string, Dictionary<string, bool>> salvas = new Dictionary<string, Dictionary<string, bool>>();
    static readonly List<LembrarCena> abertas = new List<LembrarCena>();

    // true se o jogador já saiu desta cena antes (ou seja, ela vai ser restaurada)
    public static bool JaVisitada(Scene cena) => salvas.ContainsKey(cena.name);

    // Chamado antes de trocar de cena
    public static void SalvarTodas()
    {
        foreach (var c in abertas) c.Salvar();
    }

    void Awake()
    {
        abertas.Add(this);
        if (!salvas.TryGetValue(gameObject.scene.name, out var estado)) return;

        // Calcula todos os caminhos antes de mexer em qualquer coisa
        var lista = new List<(GameObject go, bool ativo)>();
        foreach (var t in TodosObjetos())
            if (t != transform && estado.TryGetValue(Caminho(t), out bool ativo))
                lista.Add((t.gameObject, ativo));

        foreach (var (go, ativo) in lista)
            go.SetActive(ativo);
    }

    void OnDestroy() => abertas.Remove(this);

    void Salvar()
    {
        var estado = new Dictionary<string, bool>();
        foreach (var t in TodosObjetos())
            estado[Caminho(t)] = t.gameObject.activeSelf;
        salvas[gameObject.scene.name] = estado;
    }

    // Todos os objetos da cena (inclusive desativados), de cima para baixo
    IEnumerable<Transform> TodosObjetos()
    {
        foreach (var raiz in gameObject.scene.GetRootGameObjects())
            foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
                yield return t;
    }

    // Ex.: "0:Canvas/3:Semente" (o índice diferencia objetos com o mesmo nome)
    static string Caminho(Transform t)
    {
        string c = t.GetSiblingIndex() + ":" + t.name;
        for (var p = t.parent; p != null; p = p.parent)
            c = p.GetSiblingIndex() + ":" + p.name + "/" + c;
        return c;
    }

    // Começa sem nada salvo a cada Play, mesmo sem domain reload.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Resetar()
    {
        salvas.Clear();
        abertas.Clear();
    }
}
