using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArmyCommander
{
    public class BootRoot : MonoBehaviour
    {
        [SerializeField] private string _menuSceneName = "Menu";

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
            SceneManager.LoadScene(_menuSceneName);
        }
    }
}
