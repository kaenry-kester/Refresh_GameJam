using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Deixa o jogo igual em qualquer tela, SEM distorção. Funciona sozinho (não precisa colocar em objeto nenhum).
// O jogo foi feito em 1920x1080 (16:9):
// - Tela 16:9 (1280x720, 1366x768, 1920x1080, 4K...): tudo é só reduzido/ampliado por igual.
// - Tela de outro formato (4:3, 16:10, 21:9...): o jogo fica num retângulo 16:9 no centro e o resto
//   fica com faixas pretas (como filme). Dentro do retângulo tudo fica idêntico ao 1920x1080.
// Como funciona: a câmera desenha só no retângulo 16:9 e, nessas telas, os Canvas "Overlay" passam
// para "Screen Space - Camera" (assim também ficam presos ao retângulo). Nada na Hierarchy muda.
public class TelaResponsiva : MonoBehaviour
{
    static readonly Vector2 Referencia = new Vector2(1920f, 1080f);
    static float AspectoBase => Referencia.x / Referencia.y;
    const int OrdemAcimaDoMundo = 10000; // UI convertida continua por cima dos sprites do jogo

    Camera fundoPreto;
    readonly Dictionary<Canvas, int> ordemOriginal = new Dictionary<Canvas, int>(); // canvases convertidos
    Vector2Int ultimaTela;
    int framesParaAplicar;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Criar()
    {
        var go = new GameObject("TelaResponsiva");
        DontDestroyOnLoad(go);
        go.AddComponent<TelaResponsiva>();
    }

    void Awake()
    {
        // Câmera que pinta de preto a tela toda (as faixas). Fica atrás de todas as outras.
        var go = new GameObject("FaixasPretas");
        go.transform.SetParent(transform, false);
        fundoPreto = go.AddComponent<Camera>();
        fundoPreto.depth = -100;
        fundoPreto.clearFlags = CameraClearFlags.SolidColor;
        fundoPreto.backgroundColor = Color.black;
        fundoPreto.cullingMask = 0;
        fundoPreto.orthographic = true;
    }

    void OnEnable() => SceneManager.sceneLoaded += AoCarregarCena;
    void OnDisable() => SceneManager.sceneLoaded -= AoCarregarCena;

    void AoCarregarCena(Scene s, LoadSceneMode m)
    {
        // Remove da lista os canvases que foram destruídos com a cena anterior
        var mortos = new List<Canvas>();
        foreach (var c in ordemOriginal.Keys) if (!c) mortos.Add(c);
        foreach (var c in mortos) ordemOriginal.Remove(c);

        framesParaAplicar = 2; // agora e no próximo frame (pega painéis criados logo depois, ex.: PainelVaso)
        Aplicar();
    }

    void LateUpdate()
    {
        var tela = new Vector2Int(Screen.width, Screen.height);
        if (tela != ultimaTela) { ultimaTela = tela; framesParaAplicar = 1; }
        if (framesParaAplicar > 0) { framesParaAplicar--; Aplicar(); }
    }

    void Aplicar()
    {
        float aspecto = (float)Screen.width / Mathf.Max(Screen.height, 1);
        bool outroFormato = Mathf.Abs(aspecto - AspectoBase) > 0.01f;

        // Retângulo 16:9 centralizado
        Rect area = new Rect(0f, 0f, 1f, 1f);
        if (aspecto > AspectoBase) { float w = AspectoBase / aspecto; area = new Rect((1f - w) / 2f, 0f, w, 1f); } // mais larga: faixas nas laterais
        else if (aspecto < AspectoBase) { float h = aspecto / AspectoBase; area = new Rect(0f, (1f - h) / 2f, 1f, h); } // mais alta: faixas em cima/embaixo
        if (!outroFormato) area = new Rect(0f, 0f, 1f, 1f);

        // Câmeras do jogo desenham só dentro do retângulo
        foreach (var cam in Camera.allCameras)
            if (cam != fundoPreto) cam.rect = area;
        fundoPreto.enabled = outroFormato;

        var camPrincipal = Camera.main;

        // Escala da UI: sempre a partir de 1920x1080, por igual (sem esticar)
        foreach (var scaler in FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPhysicalSize) continue;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Referencia;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        }

        // Canvas Overlay: em tela de outro formato vira "Camera" para ficar dentro do retângulo
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!canvas.isRootCanvas) continue;
            bool convertido = ordemOriginal.ContainsKey(canvas);

            if (outroFormato && camPrincipal)
            {
                if (!convertido && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    ordemOriginal[canvas] = canvas.sortingOrder;
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.sortingOrder += OrdemAcimaDoMundo;
                    AjustarFilhos(canvas, OrdemAcimaDoMundo);
                    convertido = true;
                }
                if (convertido)
                {
                    canvas.worldCamera = camPrincipal; // painéis que passam de cena pegam a câmera nova
                    canvas.planeDistance = camPrincipal.nearClipPlane + 0.5f;
                }
            }
            else if (convertido)
            {
                // Voltou para 16:9 (ex.: janela redimensionada): desfaz
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = ordemOriginal[canvas];
                AjustarFilhos(canvas, -OrdemAcimaDoMundo);
                ordemOriginal.Remove(canvas);
            }
        }
    }

    // Canvases filhos com ordem própria (Override Sorting) acompanham o pai
    static void AjustarFilhos(Canvas raiz, int delta)
    {
        foreach (var c in raiz.GetComponentsInChildren<Canvas>(true))
            if (c != raiz && c.overrideSorting) c.sortingOrder += delta;
    }
}
