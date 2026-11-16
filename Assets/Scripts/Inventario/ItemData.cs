using UnityEngine;

// Um tipo de item. Crie em: Create > Inventario > Item
[CreateAssetMenu(menuName = "Inventario/Item")]
public class ItemData : ScriptableObject
{
    public string nome;
    public Sprite icone;
}
