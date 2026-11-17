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
    public Sprite spritePlantado; // opcional: o que aparece no vaso depois de plantar
}
