
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public void ChangeScene(string Scene_Name)
    {
        SceneManager.LoadScene(Scene_Name);
        Debug.Log("ggg " + Scene_Name);
    }
}
