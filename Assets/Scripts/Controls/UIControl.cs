using System.Collections.Generic;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace ArmyCommander
{
    public class UIControl : BaseControl<IUIModel>
    {
        [SerializeField] private GameObject _buildPopup;
        [SerializeField] private List<Button> _buildButtons;
        [SerializeField] private List<GameObject> _buildButtonLocks;
        [SerializeField] private GameObject _nextLevelPopup;
        [SerializeField] private Button _nextLevelButton;
        [SerializeField] private GameObject _endGamePopup;
        [SerializeField] private Button _reloadLevelButton;
        [SerializeField] private TextMeshProUGUI _goldText;
        [SerializeField] private TextMeshProUGUI _silverText;
        [SerializeField] private Slider _enemyProgressBar;
        [SerializeField] private GameObject _allEnemiesDeadIndicator;

        private bool _isNextButtonBound;
        private bool _isReloadButtonBound;
        private bool _isAllEnemiesDead;
        private readonly List<UnityAction> _buildButtonHandlers = new();

        protected override void OnModelBind(IUIModel model)
        {
            if (_buildPopup != null) _buildPopup.SetActive(false);
            if (_nextLevelPopup != null) _nextLevelPopup.SetActive(false);
            if (_endGamePopup != null) _endGamePopup.SetActive(false);
            _isAllEnemiesDead = false;
            if (_allEnemiesDeadIndicator != null) _allEnemiesDeadIndicator.SetActive(false);
            UpdateHudVisibility();

            BindNextLevelButton();
            BindReloadButton();
            RebindBuildButtons(model);

            model.OnShowBuildPopup
                .Subscribe(OnShowBuildPopup)
                .AddTo(Disposables);

            model.OnHideBuildPopup
                .Subscribe(_ =>
                {
                    _buildPopup?.SetActive(false);
                    UpdateHudVisibility();
                })
                .AddTo(Disposables);

            model.OnShowNextLevelPopup
                .Subscribe(_ =>
                {
                    _nextLevelPopup?.SetActive(true);
                    UpdateHudVisibility();
                })
                .AddTo(Disposables);
            
            model.OnShowEndGamePopup
                .Subscribe(_ =>
                {
                    _endGamePopup?.SetActive(true);
                    UpdateHudVisibility();
                })
                .AddTo(Disposables);

            model.Gold
                .Subscribe(g => { if (_goldText != null) _goldText.text = g.ToString(); })
                .AddTo(Disposables);

            model.Silver
                .Subscribe(s => { if (_silverText != null) _silverText.text = s.ToString(); })
                .AddTo(Disposables);

            model.EnemyProgress
                .Subscribe(value =>
                {
                    if (_enemyProgressBar != null)
                        _enemyProgressBar.value = value;

                    _isAllEnemiesDead = value >= 0.999f;
                    UpdateHudVisibility();
                })
                .AddTo(Disposables);
        }

        private void OnShowBuildPopup((List<TroopType> AvailableTypes, List<TroopType> AffordableTypes) popupData)
        {
            if (_buildPopup != null) _buildPopup.SetActive(true);
            UpdateHudVisibility();

            var buttonCount = _buildButtons?.Count ?? 0;
            for (int i = 0; i < buttonCount; i++)
            {
                var type = (TroopType)(i + 1);
                var unlockedByLevel = popupData.AvailableTypes.Contains(type);
                var affordable = popupData.AffordableTypes.Contains(type);
                var isInteractable = unlockedByLevel && affordable;
                var button = _buildButtons[i];
                if (button != null)
                    button.interactable = isInteractable;

                if (_buildButtonLocks != null && _buildButtonLocks.Count > i)
                    _buildButtonLocks[i].SetActive(!isInteractable);
            }
        }

        private void RebindBuildButtons(IUIModel model)
        {
            if (_buildButtons == null) return;

            var boundCount = Mathf.Min(_buildButtonHandlers.Count, _buildButtons.Count);
            for (int i = 0; i < boundCount; i++)
            {
                var button = _buildButtons[i];
                var handler = _buildButtonHandlers[i];
                if (button != null && handler != null)
                    button.onClick.RemoveListener(handler);
            }

            _buildButtonHandlers.Clear();

            for (int i = 0; i < _buildButtons.Count; i++)
            {
                var button = _buildButtons[i];
                if (button == null)
                {
                    _buildButtonHandlers.Add(null);
                    continue;
                }

                var troopType = (TroopType)(i + 1);
                UnityAction handler = () => model.SelectBuild(troopType);
                button.onClick.AddListener(handler);
                _buildButtonHandlers.Add(handler);
            }
        }

        private void BindNextLevelButton()
        {
            if (_nextLevelButton == null && _nextLevelPopup != null)
            {
                _nextLevelButton = _nextLevelPopup.GetComponentInChildren<Button>(true);
            }

            if (_nextLevelButton == null) return;
            if (_isNextButtonBound) return;

            _nextLevelButton.onClick.AddListener(OnNextLevelButtonClicked);
            _isNextButtonBound = true;
        }

        private void OnNextLevelButtonClicked()
        {
            _nextLevelPopup?.SetActive(false);
            UpdateHudVisibility();
            Model?.RequestNextLevel();
        }
        
        private void BindReloadButton()
        {
            if (_reloadLevelButton == null && _endGamePopup != null)
            {
                _reloadLevelButton = _endGamePopup.GetComponentInChildren<Button>(true);
            }

            if (_reloadLevelButton == null) return;
            if (_isReloadButtonBound) return;

            _reloadLevelButton.onClick.AddListener(OnReloadButtonClicked);
            _isReloadButtonBound = true;
        }
        
        private void OnReloadButtonClicked()
        {
            _endGamePopup?.SetActive(false);
            UpdateHudVisibility();
            Model?.RequestReload();
        }

        private void UpdateHudVisibility()
        {
            var isAnyPopupActive = IsPopupActive(_buildPopup)
                                   || IsPopupActive(_nextLevelPopup)
                                   || IsPopupActive(_endGamePopup);
            SetHudVisible(!isAnyPopupActive);
        }

        private static bool IsPopupActive(GameObject popup)
        {
            return popup != null && popup.activeSelf;
        }

        private void SetHudVisible(bool isVisible)
        {
            if (_goldText != null) _goldText.gameObject.SetActive(isVisible);
            if (_silverText != null) _silverText.gameObject.SetActive(isVisible);
            if (_enemyProgressBar != null) _enemyProgressBar.gameObject.SetActive(isVisible);
            if (_allEnemiesDeadIndicator != null) _allEnemiesDeadIndicator.SetActive(isVisible && _isAllEnemiesDead);
        }
    }
}
