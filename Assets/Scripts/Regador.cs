using System.Collections;
using UnityEngine;

// Regador que pode ser arrastado pela cena. Ao soltar, volta para o lugar de origem.
// Segurando em cima de uma planta: a água dela sobe (Agua Por Segundo) até o que ela precisa,
// com o som tocando. Saiu de cima ou encheu -> o som para na hora.
// Coloque no objeto "regador" (SpriteRenderer). O BoxCollider2D é adicionado sozinho
// e é ele que recebe o clique.
[RequireComponent(typeof(BoxCollider2D))]
public class Regador : MonoBehaviour
{
    [SerializeField] float duracaoVoltar = 0.25f; // tempo para voltar ao lugar
    [SerializeField] int ordemArrastando = 100;   // fica na frente de tudo enquanto arrasta

    [Header("Regar")]
    [SerializeField] AudioClip somRegar;              // arraste Assets/Som/regador
    [SerializeField, Range(0f, 1f)] float volume = 1f;

    [Header("Hover")]
    [SerializeField] Color corHover = new Color(0.75f, 0.75f, 0.75f, 1f); // cor com o mouse em cima
    [SerializeField] float escalaHover = 1.08f;                            // cresce um pouco com o mouse em cima

    Vector3 origem;
    Vector3 offset;
    Vector3 escalaOriginal;
    SpriteRenderer sr;
    Color corOriginal;
    int ordemOriginal;
    bool segurando;

    AudioSource fonte;

    void Awake()
    {
        origem = transform.position;
        escalaOriginal = transform.localScale;
        sr = GetComponent<SpriteRenderer>();
        if (sr)
        {
            ordemOriginal = sr.sortingOrder;
            corOriginal = sr.color;

            // Área de clique sempre do tamanho da imagem (não depende de ajustar o collider à mão)
            if (sr.sprite)
            {
                var box = GetComponent<BoxCollider2D>();
                box.size = sr.sprite.bounds.size;
                box.offset = sr.sprite.bounds.center;
            }
        }
    }

    void OnMouseEnter() => Destacar(true);
    void OnMouseExit()  { if (!segurando) Destacar(false); }

    void Destacar(bool ligado)
    {
        if (sr) sr.color = ligado ? corHover : corOriginal;
        transform.localScale = ligado ? escalaOriginal * escalaHover : escalaOriginal;
    }

    void OnMouseDown()
    {
        if (PainelVaso.Aberto) return; // com o painel aberto, usa-se o regador do painel
        StopAllCoroutines();
        segurando = true;
        offset = transform.position - Mouse(); // segura pelo ponto clicado, sem "pular"
        if (sr) sr.sortingOrder = ordemArrastando;
    }

    // Chamado todo frame enquanto o botão do mouse está segurando o regador
    void OnMouseDrag()
    {
        if (!segurando) return;
        transform.position = Mouse() + offset;
        Regando();
    }

    void OnMouseUp()
    {
        if (!segurando) return;
        segurando = false;
        Destacar(false);
        PararRega();
        StartCoroutine(Voltar());
    }

    void OnDisable() => PararRega();

    void Regando()
    {
        // Planta embaixo do regador (dentro do Raio Plantio do vaso)
        Vector2 centro = sr ? (Vector2)sr.bounds.center : (Vector2)transform.position;
        var vaso = Vaso.MaisPerto(centro) ?? Vaso.MaisPerto(Mouse());

        // Som só enquanto a água está subindo
        if (vaso != null && vaso.Regando(Time.deltaTime)) TocarSom();
        else PararSom();
    }

    void PararRega() => PararSom();

    void TocarSom()
    {
        if (!somRegar) return;
        if (!fonte)
        {
            fonte = gameObject.AddComponent<AudioSource>();
            fonte.playOnAwake = false;
            fonte.spatialBlend = 0f;
            fonte.loop = true; // repete enquanto estiver regando
        }
        fonte.clip = somRegar;
        fonte.volume = volume;
        if (!fonte.isPlaying) fonte.Play();
    }

    void PararSom()
    {
        if (fonte && fonte.isPlaying) fonte.Stop(); // para na hora
    }

    IEnumerator Voltar()
    {
        Vector3 p0 = transform.position;
        for (float t = 0f; t < 1f; t += Time.deltaTime / duracaoVoltar)
        {
            transform.position = Vector3.Lerp(p0, origem, 1f - Mathf.Pow(1f - t, 3f)); // ease out
            yield return null;
        }
        transform.position = origem;
        if (sr) sr.sortingOrder = ordemOriginal;
    }

    Vector3 Mouse()
    {
        Vector3 p = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        p.z = origem.z;
        return p;
    }
}
