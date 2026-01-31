using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneReload : MonoBehaviour
{
    private bool once = false;
    public void ReloadScene()
    {
        if (once) return;
        once = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
