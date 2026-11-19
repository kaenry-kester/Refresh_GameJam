
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Regador : MonoBehaviour
{
    [SerializeField] float duracaoVoltar = 0.25f;
    [SerializeField] int ordemArrastando = 100;

    [Header("Regar")]
    [SerializeField] AudioClip somRegar;
    [SerializeField, Range(0f, 1f)] float volume = 1f;

    [Header("Animacao de Regar")]
    [SerializeField] float anguloRegando = -20f;
    [SerializeField] float velocidadeInclinacao = 8f;

    [Header("Hover")]
    [SerializeField] Color corHover =
        new Color(0.75f, 0.75f, 0.75f, 1f);

    [SerializeField] float escalaHover = 1.08f;

    Vector3 origem;
    Vector3 offset;
    Vector3 escalaOriginal;

    Quaternion rotacaoOriginal;

    SpriteRenderer sr;
    Color corOriginal;

    int ordemOriginal;
    bool segurando;
    bool estaRegando;

    AudioSource fonte;

    void Awake()
    {
        origem = transform.position;
        escalaOriginal = transform.localScale;
        rotacaoOriginal = transform.localRotation;

        sr = GetComponent<SpriteRenderer>();

        if (sr)
        {
            ordemOriginal = sr.sortingOrder;
            corOriginal = sr.color;

            if (sr.sprite)
            {
                var box = GetComponent<BoxCollider2D>();
                box.size = sr.sprite.bounds.size;
                box.offset = sr.sprite.bounds.center;
            }
        }
    }

    void Update()
    {
        // Inclina enquanto estiver regando
        Quaternion rotacaoDestino = estaRegando
            ? rotacaoOriginal *
              Quaternion.Euler(0f, 0f, anguloRegando)
            : rotacaoOriginal;

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            rotacaoDestino,
            Mathf.Clamp01(
                velocidadeInclinacao * Time.deltaTime
            )
        );
    }

    void OnMouseEnter()
    {
        Destacar(true);
    }

    void OnMouseExit()
    {
        if (!segurando)
            Destacar(false);
    }

    void Destacar(bool ligado)
    {
        if (sr)
            sr.color = ligado ? corHover : corOriginal;

        transform.localScale = ligado
            ? escalaOriginal * escalaHover
            : escalaOriginal;
    }

    void OnMouseDown()
    {
        if (PainelVaso.Aberto)
            return;

        StopAllCoroutines();

        segurando = true;

        offset = transform.position - Mouse();

        if (sr)
            sr.sortingOrder = ordemArrastando;
    }

    void OnMouseDrag()
    {
        if (!segurando)
            return;

        transform.position = Mouse() + offset;

        Regando();
    }

    void OnMouseUp()
    {
        if (!segurando)
            return;

        segurando = false;

        Destacar(false);
        PararRega();

        StartCoroutine(Voltar());
    }

    void OnDisable()
    {
        PararRega();
    }

    void Regando()
    {
        Vector2 centro = sr
            ? (Vector2)sr.bounds.center
            : (Vector2)transform.position;

        var vaso = Vaso.MaisPerto(centro)
            ?? Vaso.MaisPerto(Mouse());

        if (vaso != null &&
            vaso.Regando(Time.deltaTime))
        {
            estaRegando = true;
            TocarSom();
        }
        else
        {
            PararRega();
        }
    }

    void PararRega()
    {
        estaRegando = false;
        PararSom();
    }

    void TocarSom()
    {
        if (!somRegar)
            return;

        if (!fonte)
        {
            fonte = gameObject.AddComponent<AudioSource>();

            fonte.playOnAwake = false;
            fonte.spatialBlend = 0f;
            fonte.loop = true;
        }

        fonte.clip = somRegar;
        fonte.volume = volume;

        if (!fonte.isPlaying)
            fonte.Play();
    }

    void PararSom()
    {
        if (fonte && fonte.isPlaying)
            fonte.Stop();
    }

    IEnumerator Voltar()
    {
        Vector3 p0 = transform.position;

        for (float t = 0f; t < 1f;
             t += Time.deltaTime /
             Mathf.Max(duracaoVoltar, 0.01f))
        {
            transform.position = Vector3.Lerp(
                p0,
                origem,
                1f - Mathf.Pow(1f - t, 3f)
            );

            yield return null;
        }

        transform.position = origem;

        if (sr)
            sr.sortingOrder = ordemOriginal;
    }

    Vector3 Mouse()
    {
        Vector3 p = Camera.main.ScreenToWorldPoint(
            Input.mousePosition
        );

        p.z = origem.z;

        return p;
    }
}
