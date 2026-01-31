using UnityEngine;
using UnityEngine.SceneManagement;

namespace UsefulScripts.Core
{
    /// <summary>
    /// Central game manager handling game states and core functionality.
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        public enum GameState
        {
            MainMenu,
            Playing,
            Paused,
            GameOver
        }

        [Header("Game State")]
        [SerializeField] private GameState currentState = GameState.MainMenu;
        
        public GameState CurrentState => currentState;
        public bool IsPaused => currentState == GameState.Paused;
        public bool IsPlaying => currentState == GameState.Playing;

        // Events
        public event System.Action<GameState> OnGameStateChanged;
        public event System.Action OnGamePaused;
        public event System.Action OnGameResumed;

        protected override void OnSingletonAwake()
        {
            Application.targetFrameRate = 60;
        }

        /// <summary>
        /// Changes the current game state
        /// </summary>
        public void SetGameState(GameState newState)
        {
            if (currentState == newState) return;

            GameState previousState = currentState;
            currentState = newState;

            HandleStateChange(previousState, newState);
            OnGameStateChanged?.Invoke(newState);
        }

        private void HandleStateChange(GameState from, GameState to)
        {
            switch (to)
            {
                case GameState.Paused:
                    Time.timeScale = 0f;
                    OnGamePaused?.Invoke();
                    break;
                case GameState.Playing:
                    Time.timeScale = 1f;
                    if (from == GameState.Paused)
                    {
                        OnGameResumed?.Invoke();
                    }
                    break;
                case GameState.GameOver:
                    Time.timeScale = 0f;
                    break;
                case GameState.MainMenu:
                    Time.timeScale = 1f;
                    break;
            }
        }

        /// <summary>
        /// Toggle pause state
        /// </summary>
        public void TogglePause()
        {
            if (currentState == GameState.Playing)
            {
                SetGameState(GameState.Paused);
            }
            else if (currentState == GameState.Paused)
            {
                SetGameState(GameState.Playing);
            }
        }

        /// <summary>
        /// Start playing the game
        /// </summary>
        public void StartGame()
        {
            SetGameState(GameState.Playing);
        }

        /// <summary>
        /// End the game
        /// </summary>
        public void GameOver()
        {
            SetGameState(GameState.GameOver);
        }

        /// <summary>
        /// Return to main menu
        /// </summary>
        public void ReturnToMainMenu()
        {
            SetGameState(GameState.MainMenu);
        }

        /// <summary>
        /// Load a scene by name
        /// </summary>
        public void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// Load a scene by build index
        /// </summary>
        public void LoadScene(int buildIndex)
        {
            SceneManager.LoadScene(buildIndex);
        }

        /// <summary>
        /// Reload the current scene
        /// </summary>
        public void ReloadCurrentScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>
        /// Quit the application
        /// </summary>
        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void Update()
        {
            // Example: Pause with Escape key
            if (Input.GetKeyDown(KeyCode.Escape) && 
                (currentState == GameState.Playing || currentState == GameState.Paused))
            {
                TogglePause();
            }
        }
    }
}
