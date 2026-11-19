
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public void ChangeScene(string Scene_Name)
    {
        LembrarCena.SalvarTodas(); // guarda o estado da cena atual antes de sair
        SceneManager.LoadScene(Scene_Name);
        Debug.Log("gg" + Scene_Name);
    }
}
