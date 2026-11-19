using UnityEngine;

// Um tipo de item. Crie em: Create > Inventario > Item
[CreateAssetMenu(menuName = "Inventario/Item")]
public class ItemData : ScriptableObject
{
    public string nome;
    public Sprite icone;
    [UnityEngine.Serialization.FormerlySerializedAs("iconeFrigorifico")]
    public Sprite iconeFrigorificoPre; // versão clara, usada nos slots do frigorífico (vazio = usa o icone)
    public Sprite iconeFrigorificoPos;
    public bool ehSemente;      // só sementes podem ser plantadas em vasos

    // "Pacote" de crescimento: o que aparece em cima do vaso em cada fase.
    // Plantou -> Semente. Cada dia regado avança uma fase até a Final.
    [Header("Crescimento no vaso (só sementes)")]
    [UnityEngine.Serialization.FormerlySerializedAs("spritePlantado")]
    public Sprite spriteSemente;   // logo depois de plantar (vazio = usa o icone)
    public Sprite spriteBroto;
    public Sprite spriteCrescendo;
    public Sprite spriteFinal;

    public const int FaseFinal = 3; // 0 = Semente, 1 = Broto, 2 = Crescendo, 3 = Final

    // Sprite da fase (0..3). Se alguma estiver vazia, usa a fase anterior que tiver.
    public Sprite SpriteDaFase(int fase)
    {
        Sprite[] fases = { spriteSemente ? spriteSemente : icone, spriteBroto, spriteCrescendo, spriteFinal };
        for (int i = Mathf.Clamp(fase, 0, FaseFinal); i >= 0; i--)
            if (fases[i]) return fases[i];
        return icone;
    }
}
