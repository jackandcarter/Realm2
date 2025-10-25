using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Client
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("API Settings")]
        [SerializeField] private string baseApiUrl = "http://localhost:5000";
        [SerializeField] private bool useMockServices = true;
        [SerializeField] private string worldSceneName = "SampleScene";

        private AuthService _authService;
        private RealmService _realmService;
        private CharacterService _characterService;

        private Canvas _loginCanvas;
        private InputField _usernameInput;
        private InputField _passwordInput;
        private Button _loginButton;
        private Text _loginMessage;

        private Canvas _realmCanvas;
        private RectTransform _realmListRoot;
        private Button _reloadRealmsButton;
        private Text _realmMessage;

        private Canvas _characterCanvas;
        private RectTransform _characterListRoot;
        private InputField _characterNameInput;
        private Button _createCharacterButton;
        private Text _characterMessage;

        private readonly List<GameObject> _spawnedRealmEntries = new();
        private readonly List<GameObject> _spawnedCharacterEntries = new();

        private void Awake()
        {
            EnsureEventSystem();
            EnsureUi();

            _authService = new AuthService(baseApiUrl, useMockServices);
            _realmService = new RealmService(baseApiUrl, useMockServices);
            _characterService = new CharacterService(baseApiUrl, useMockServices);
        }

        private void Start()
        {
            ShowCanvas(_loginCanvas);
        }

        private void EnsureUi()
        {
            if (_loginCanvas == null)
            {
                _loginCanvas = CreateLoginCanvas();
            }

            if (_realmCanvas == null)
            {
                _realmCanvas = CreateRealmCanvas();
                _realmCanvas.gameObject.SetActive(false);
            }

            if (_characterCanvas == null)
            {
                _characterCanvas = CreateCharacterCanvas();
                _characterCanvas.gameObject.SetActive(false);
            }
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        private Canvas CreateLoginCanvas()
        {
            var canvas = CreateCanvas("LoginCanvas", 0);

            var panel = CreatePanel(canvas.transform, "LoginPanel");
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(32, 32, 32, 32);
            layout.spacing = 12f;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;

            _loginMessage = CreateMessageText(panel, "Enter your credentials to begin.");
            _usernameInput = CreateInputField(panel, "UsernameInput", "Username");
            _passwordInput = CreateInputField(panel, "PasswordInput", "Password", true);
            _loginButton = CreateButton(panel, "LoginButton", "Login");
            _loginButton.onClick.AddListener(OnLoginClicked);

            return canvas;
        }

        private Canvas CreateRealmCanvas()
        {
            var canvas = CreateCanvas("RealmCanvas", 1);

            var panel = CreatePanel(canvas.transform, "RealmPanel");
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(32, 32, 32, 32);
            layout.spacing = 8f;
            layout.childForceExpandHeight = false;

            _realmMessage = CreateMessageText(panel, "Select a realm to continue.");
            _reloadRealmsButton = CreateButton(panel, "RefreshRealmsButton", "Refresh Realms");
            _reloadRealmsButton.onClick.AddListener(() => StartCoroutine(LoadRealms()));

            _realmListRoot = CreateListRoot(panel, "RealmList");

            return canvas;
        }

        private Canvas CreateCharacterCanvas()
        {
            var canvas = CreateCanvas("CharacterCanvas", 2);

            var panel = CreatePanel(canvas.transform, "CharacterPanel");
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(32, 32, 32, 32);
            layout.spacing = 8f;
            layout.childForceExpandHeight = false;

            _characterMessage = CreateMessageText(panel, "Choose your hero or create a new one.");
            _characterListRoot = CreateListRoot(panel, "CharacterList");

            _characterNameInput = CreateInputField(panel, "CharacterNameInput", "Character Name");
            _createCharacterButton = CreateButton(panel, "CreateCharacterButton", "Create Character");
            _createCharacterButton.onClick.AddListener(OnCreateCharacterClicked);

            return canvas;
        }

        private Canvas CreateCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name)
            {
                layer = LayerMask.NameToLayer("UI")
            };
            go.transform.SetParent(transform, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private RectTransform CreatePanel(Transform parent, string name)
        {
            var panelGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelGo.transform.SetParent(parent, false);
            var rect = (RectTransform)panelGo.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(600, 500);
            panelGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            return rect;
        }

        private Text CreateMessageText(Transform parent, string defaultText)
        {
            var textGo = new GameObject("Message", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGo.transform.SetParent(parent, false);
            var rect = (RectTransform)textGo.transform;
            rect.sizeDelta = new Vector2(0, 60);
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.text = defaultText;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            var layout = textGo.AddComponent<LayoutElement>();
            layout.preferredHeight = 60f;
            return text;
        }

        private InputField CreateInputField(Transform parent, string name, string placeholder, bool isPassword = false)
        {
            var inputGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField));
            inputGo.transform.SetParent(parent, false);
            inputGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.9f);
            var layout = inputGo.AddComponent<LayoutElement>();
            layout.preferredHeight = 48f;
            layout.preferredWidth = 400f;

            var rect = (RectTransform)inputGo.transform;
            rect.sizeDelta = new Vector2(400, 48);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGo.transform.SetParent(inputGo.transform, false);
            ConfigureTextRect((RectTransform)textGo.transform);
            var textComponent = textGo.GetComponent<Text>();
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.text = string.Empty;
            textComponent.color = Color.black;
            textComponent.alignment = TextAnchor.MiddleLeft;

            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            placeholderGo.transform.SetParent(inputGo.transform, false);
            ConfigureTextRect((RectTransform)placeholderGo.transform);
            var placeholderText = placeholderGo.GetComponent<Text>();
            placeholderText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            placeholderText.text = placeholder;
            placeholderText.color = new Color(0.6f, 0.6f, 0.6f, 0.75f);
            placeholderText.alignment = TextAnchor.MiddleLeft;

            var input = inputGo.GetComponent<InputField>();
            input.textComponent = textComponent;
            input.placeholder = placeholderText;
            input.contentType = isPassword ? InputField.ContentType.Password : InputField.ContentType.Standard;
            input.lineType = InputField.LineType.SingleLine;

            return input;
        }

        private Button CreateButton(Transform parent, string name, string label)
        {
            var buttonGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(parent, false);
            buttonGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.9f);
            var layout = buttonGo.AddComponent<LayoutElement>();
            layout.preferredHeight = 48f;
            layout.preferredWidth = 400f;

            var rect = (RectTransform)buttonGo.transform;
            rect.sizeDelta = new Vector2(400, 48);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGo.transform.SetParent(buttonGo.transform, false);
            ConfigureTextRect((RectTransform)textGo.transform);
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.text = label;
            text.color = Color.black;
            text.alignment = TextAnchor.MiddleCenter;

            return buttonGo.GetComponent<Button>();
        }

        private RectTransform CreateListRoot(Transform parent, string name)
        {
            var listGo = new GameObject(name, typeof(RectTransform));
            listGo.transform.SetParent(parent, false);
            var rect = (RectTransform)listGo.transform;
            rect.sizeDelta = new Vector2(0, 300);
            var layout = listGo.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            layout.childAlignment = TextAnchor.UpperCenter;

            var fitter = listGo.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.MinSize;

            var layoutElement = listGo.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 300f;
            return rect;
        }

        private void ConfigureTextRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(10, 6);
            rect.offsetMax = new Vector2(-10, -6);
        }

        private void OnLoginClicked()
        {
            _loginButton.interactable = false;
            _loginMessage.text = "Authenticating...";
            StartCoroutine(LoginRoutine());
        }

        private IEnumerator LoginRoutine()
        {
            yield return _authService.Login(
                _usernameInput.text,
                _passwordInput.text,
                response =>
                {
                    _loginMessage.text = "Login successful!";
                    ShowCanvas(_realmCanvas);
                    StartCoroutine(LoadRealms());
                },
                error =>
                {
                    _loginButton.interactable = true;
                    _loginMessage.text = error.Message;
                });
        }

        private IEnumerator LoadRealms()
        {
            _realmMessage.text = "Loading realms...";
            _reloadRealmsButton.interactable = false;
            ClearList(_spawnedRealmEntries);

            yield return _realmService.GetRealms(
                realms =>
                {
                    if (realms.Count == 0)
                    {
                        _realmMessage.text = "No realms available.";
                        return;
                    }

                    _realmMessage.text = "Choose a realm:";
                    foreach (var realm in realms)
                    {
                        var entry = CreateRealmButton(realm);
                        _spawnedRealmEntries.Add(entry.gameObject);
                    }
                },
                error =>
                {
                    _realmMessage.text = error.Message;
                });

            _reloadRealmsButton.interactable = true;
        }

        private Button CreateRealmButton(RealmInfo realm)
        {
            var button = CreateListButton(_realmListRoot, $"Realm_{realm.id}", $"{realm.name} (Pop: {realm.population})");
            button.onClick.AddListener(() => OnRealmSelected(realm));
            return button;
        }

        private void OnRealmSelected(RealmInfo realm)
        {
            SessionManager.SetRealm(realm.id);
            _characterMessage.text = $"Loading characters for {realm.name}...";
            ShowCanvas(_characterCanvas);
            StartCoroutine(LoadCharacters(realm));
        }

        private IEnumerator LoadCharacters(RealmInfo realm)
        {
            ClearList(_spawnedCharacterEntries);

            yield return _characterService.GetCharacters(
                realm.id,
                characters =>
                {
                    if (characters.Count == 0)
                    {
                        _characterMessage.text = "No characters found. Create a new hero.";
                        return;
                    }

                    _characterMessage.text = $"Select a character for {realm.name}:";
                    foreach (var character in characters)
                    {
                        var entry = CreateCharacterButton(character);
                        _spawnedCharacterEntries.Add(entry.gameObject);
                    }
                },
                error => { _characterMessage.text = error.Message; });
        }

        private Button CreateCharacterButton(CharacterInfo character)
        {
            var button = CreateListButton(_characterListRoot, $"Character_{character.id}", $"{character.name} - Lv {character.level} {character.className}");
            button.onClick.AddListener(() => OnCharacterSelected(character));
            return button;
        }

        private void OnCharacterSelected(CharacterInfo character)
        {
            _characterMessage.text = "Connecting to realm...";
            _createCharacterButton.interactable = false;
            StartCoroutine(SelectCharacterRoutine(character));
        }

        private IEnumerator SelectCharacterRoutine(CharacterInfo character)
        {
            yield return _characterService.SelectCharacter(
                character.id,
                () =>
                {
                    SessionManager.SetCharacter(character.id);
                    SceneManager.LoadScene(worldSceneName);
                },
                error =>
                {
                    _characterMessage.text = error.Message;
                    _createCharacterButton.interactable = true;
                });
        }

        private void OnCreateCharacterClicked()
        {
            if (string.IsNullOrWhiteSpace(SessionManager.SelectedRealmId))
            {
                _characterMessage.text = "Please select a realm first.";
                return;
            }

            var name = _characterNameInput.text;
            _characterMessage.text = "Creating character...";
            _createCharacterButton.interactable = false;
            StartCoroutine(CreateCharacterRoutine(SessionManager.SelectedRealmId, name));
        }

        private IEnumerator CreateCharacterRoutine(string realmId, string name)
        {
            yield return _characterService.CreateCharacter(
                realmId,
                name,
                "Adventurer",
                character =>
                {
                    _characterMessage.text = $"Created {character.name}. Select to enter the world.";
                    _characterNameInput.text = string.Empty;
                    var entry = CreateCharacterButton(character);
                    _spawnedCharacterEntries.Add(entry.gameObject);
                    _createCharacterButton.interactable = true;
                },
                error =>
                {
                    _characterMessage.text = error.Message;
                    _createCharacterButton.interactable = true;
                });
        }

        private Button CreateListButton(RectTransform parent, string name, string label)
        {
            var button = CreateButton(parent, name, label);
            var image = button.GetComponent<Image>();
            image.color = new Color(0.9f, 0.9f, 0.9f, 0.95f);
            var layout = button.GetComponent<LayoutElement>();
            layout.preferredWidth = 450f;
            return button;
        }

        private void ClearList(List<GameObject> entries)
        {
            foreach (var entry in entries)
            {
                if (entry != null)
                {
                    Destroy(entry);
                }
            }

            entries.Clear();
        }

        private void ShowCanvas(Canvas canvasToShow)
        {
            _loginCanvas.gameObject.SetActive(canvasToShow == _loginCanvas);
            _realmCanvas.gameObject.SetActive(canvasToShow == _realmCanvas);
            _characterCanvas.gameObject.SetActive(canvasToShow == _characterCanvas);
        }
    }
}
