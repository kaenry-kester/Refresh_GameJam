using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Mostra a lista de afazeres por cima de tudo. Qualquer clique fecha.
// Abre de novo só clicando no último slot do inventário (o da lista).
// Criada sozinha pelo código (não precisa montar nada na cena).
public class ListaAfazeres : MonoBehaviour, IPointerClickHandler
{
    const float AlturaLista = 950f; // na tela de referência 1920x1080

    static ListaAfazeres instancia;
    Image imagem;

    public static bool Aberta => instancia && instancia.gameObject.activeSelf;

    public static void Abrir(Sprite sprite)
    {
        if (!sprite) { Debug.LogWarning("ListaAfazeres: item sem 'Imagem Lista'."); return; }
        if (!instancia) Criar();
        instancia.imagem.sprite = sprite;
        var rt = instancia.imagem.rectTransform;
        float proporcao = sprite.rect.width / sprite.rect.height;
        rt.sizeDelta = new Vector2(AlturaLista * proporcao, AlturaLista);
        instancia.gameObject.SetActive(true);
    }

    public static void Fechar()
    {
        if (instancia) instancia.gameObject.SetActive(false);
    }

    static void Criar()
    {
        // Canvas próprio, por cima de todos os painéis
        var go = new GameObject("ListaAfazeres", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(go);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        // Fundo escurecido na tela toda: recebe qualquer clique
        var fundo = go.AddComponent<Image>();
        fundo.color = new Color(0f, 0f, 0f, 0.55f);

        // A lista (não bloqueia o clique: clicar nela também fecha)
        var lista = new GameObject("Lista", typeof(RectTransform), typeof(Image));
        lista.transform.SetParent(go.transform, false);
        var img = lista.GetComponent<Image>();
        img.raycastTarget = false;
        img.preserveAspect = true;

        instancia = go.AddComponent<ListaAfazeres>();
        instancia.imagem = img;

        // Canvas só com Image no próprio objeto: estica para cobrir a tela
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
    }

    public void OnPointerClick(PointerEventData ev) => Fechar();

    void Update()
    {
        // Clique com qualquer botão do mouse fecha (inclusive direito/meio)
        if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) Fechar();
    }

    void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }
}
