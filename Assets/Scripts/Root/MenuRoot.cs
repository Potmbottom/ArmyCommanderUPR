using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace ArmyCommander
{
    public class MenuRoot : MonoBehaviour, IStartable
    {
        [SerializeField] private Button _startButton;
        [SerializeField] private string _gameSceneName = "Game";

        private bool _isStartButtonBound;

        public void Start()
        {
            if (_startButton == null)
            {
                Debug.LogWarning($"{nameof(MenuRoot)}: Start button is not assigned.");
                return;
            }

            _startButton.onClick.AddListener(OnStartButtonClicked);
            _isStartButtonBound = true;
        }

        private void OnStartButtonClicked()
        {
            SceneManager.LoadScene(_gameSceneName);
        }

        private void OnDestroy()
        {
            if (_isStartButtonBound && _startButton != null)
            {
                _startButton.onClick.RemoveListener(OnStartButtonClicked);
            }
        }
    }
}
