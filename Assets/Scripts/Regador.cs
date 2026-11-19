using System.Collections;
using UnityEngine;

// Regador que pode ser arrastado pela cena. Ao soltar, volta para o lugar de origem.
// Segurando em cima de uma planta com sede: toca o som de água e, depois de "Tempo Para Regar"
// segundos, ela é regada (avança uma fase). Saiu de cima antes -> o som para na hora e a rega não conta.
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
    [SerializeField] float tempoParaRegar = 1.5f;     // segundos segurando em cima da planta até regar

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

    Vaso alvo;        // planta sendo regada agora
    float progresso;  // segundos regando o alvo atual
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
        StopAllCoroutines();
        segurando = true;
        offset = transform.position - Mouse(); // segura pelo ponto clicado, sem "pular"
        if (sr) sr.sortingOrder = ordemArrastando;
    }

    // Chamado todo frame enquanto o botão do mouse está segurando o regador
    void OnMouseDrag()
    {
        transform.position = Mouse() + offset;
        Regando();
    }

    void OnMouseUp()
    {
        segurando = false;
        Destacar(false);
        PararRega();
        StartCoroutine(Voltar());
    }

    void OnDisable() => PararRega();

    void Regando()
    {
        // Planta com sede embaixo do regador (dentro do Raio Plantio do vaso)
        Vector2 centro = sr ? (Vector2)sr.bounds.center : (Vector2)transform.position;
        var vaso = Vaso.MaisPerto(centro) ?? Vaso.MaisPerto(Mouse());
        if (vaso != null && !vaso.ComSede) vaso = null;

        if (vaso != alvo) { alvo = vaso; progresso = 0f; } // trocou de planta: recomeça
        if (alvo == null) { PararSom(); return; }

        TocarSom();
        progresso += Time.deltaTime;
        if (progresso >= tempoParaRegar)
        {
            alvo.Regar(); // avança a fase, escurece o vaso, tira a sede
            PararRega();
        }
    }

    void PararRega()
    {
        alvo = null;
        progresso = 0f;
        PararSom();
    }

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
