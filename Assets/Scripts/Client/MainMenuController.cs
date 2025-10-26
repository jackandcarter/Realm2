using System;
using System.Collections;
using System.Collections.Generic;
using Client.CharacterCreation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Client
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("UI Setup")]
        [SerializeField] private bool autoCreateUi = true;

        [Header("API Settings")]
        [SerializeField] private ApiEnvironmentConfig environmentConfig;
        [SerializeField] private string fallbackBaseApiUrl = "http://localhost:3000";
        [SerializeField] private bool fallbackUseMockServicesInEditor = true;
        [SerializeField] private bool fallbackUseMockServicesInPlayer = false;
        [SerializeField] private string worldSceneName = "SampleScene";

        private AuthService _authService;
        private RealmService _realmService;
        private CharacterService _characterService;

        [SerializeField] private Canvas _loginCanvas;
        private InputField _emailInput;
        private InputField _passwordInput;
        private Button _loginButton;
        private Text _loginMessage;

        [SerializeField] private Canvas _realmCanvas;
        private RectTransform _realmListRoot;
        private Button _reloadRealmsButton;
        private Text _realmMessage;

        [SerializeField] private Canvas _characterCanvas;
        [SerializeField] private CharacterCreationPanel _characterCreationPanelPrefab;
        [SerializeField] private RectTransform _characterCreationPanelMount;
        private RectTransform _characterListRoot;
        private InputField _characterNameInput;
        private Button _createCharacterButton;
        private Text _characterMessage;

        private readonly List<GameObject> _spawnedRealmEntries = new();
        private readonly List<GameObject> _spawnedCharacterEntries = new();
        private CharacterCreationPanel _characterCreationPanelInstance;
        private bool _characterCreationPanelHooked;

        private void Awake()
        {
            EnsureEventSystem();
            EnsureUi();

            if (!Application.isPlaying)
            {
                return;
            }

            var baseUrl = ResolveBaseApiUrl();
            var useMocks = ResolveUseMockServices();

            _authService = new AuthService(baseUrl, useMocks);
            _realmService = new RealmService(baseUrl, useMocks);
            _characterService = new CharacterService(baseUrl, useMocks);

            if (environmentConfig != null)
            {
                Debug.Log($"MainMenuController using environment '{environmentConfig.EnvironmentName}' at {baseUrl} (mocks: {useMocks}).");
            }
        }

        private void Start()
        {
            if (!Application.isPlaying)
            {
                ShowCanvas(_loginCanvas);
                return;
            }

            ShowCanvas(_loginCanvas);
        }

        private void EnsureUi()
        {
            EnsureCanvas(ref _loginCanvas, "LoginCanvas", CreateLoginCanvas);
            EnsureCanvas(ref _realmCanvas, "RealmCanvas", CreateRealmCanvas);
            EnsureCanvas(ref _characterCanvas, "CharacterCanvas", CreateCharacterCanvas);

            if (_characterCreationPanelMount == null && _characterCanvas != null)
            {
                _characterCreationPanelMount = (RectTransform)_characterCanvas.transform;
            }

            if (_realmCanvas != null)
            {
                _realmCanvas.gameObject.SetActive(false);
            }

            if (_characterCanvas != null)
            {
                _characterCanvas.gameObject.SetActive(false);
            }

            if (_loginCanvas != null && !Application.isPlaying)
            {
                _loginCanvas.gameObject.SetActive(true);
            }
        }

        private void EnsureCanvas(ref Canvas canvasField, string canvasName, Func<Canvas> factory)
        {
            if (canvasField == null)
            {
                canvasField = FindCanvas(canvasName);
            }

            if (canvasField != null || !Application.isPlaying && !autoCreateUi)
            {
                return;
            }

            if (!autoCreateUi)
            {
                Debug.LogWarning($"MainMenuController is missing a reference to '{canvasName}' and auto-create is disabled.");
                return;
            }

            canvasField = factory();
        }

        private void EnsureEventSystem()
        {
            var existing = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (existing == null)
            {
                var eventSystemGo = new GameObject("EventSystem");
                eventSystemGo.transform.SetParent(transform, false);
                eventSystemGo.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                eventSystemGo.AddComponent<InputSystemUIInputModule>();
#else
                eventSystemGo.AddComponent<StandaloneInputModule>();
#endif
                return;
            }

#if ENABLE_INPUT_SYSTEM
            if (existing.GetComponent<InputSystemUIInputModule>() == null)
            {
                existing.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            var legacyModule = existing.GetComponent<StandaloneInputModule>();
            if (legacyModule != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(legacyModule);
                }
                else
                {
                    DestroyImmediate(legacyModule);
                }
            }
#endif
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

            _loginMessage = CreateMessageText(panel, "Enter the email tied to your Realm account.");
            _emailInput = CreateInputField(panel, "EmailInput", "Email");
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
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.text = string.Empty;
            textComponent.color = Color.black;
            textComponent.alignment = TextAnchor.MiddleLeft;

            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            placeholderGo.transform.SetParent(inputGo.transform, false);
            ConfigureTextRect((RectTransform)placeholderGo.transform);
            var placeholderText = placeholderGo.GetComponent<Text>();
            placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
                _emailInput.text,
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
            var membershipLabel = realm.isMember
                ? string.IsNullOrWhiteSpace(realm.membershipRole) ? "Member" : $"Member: {realm.membershipRole}"
                : "Joinable";
            var button = CreateListButton(
                _realmListRoot,
                $"Realm_{realm.id}",
                $"{realm.name} • {membershipLabel}"
            );
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
            _createCharacterButton.interactable = false;

            yield return _characterService.GetCharacters(
                realm.id,
                roster =>
                {
                    var characters = roster.characters ?? Array.Empty<CharacterInfo>();
                    if (characters.Length == 0)
                    {
                        _characterMessage.text = "No characters found. Create a new hero.";
                    }
                    else
                    {
                        var roleLabel = string.IsNullOrWhiteSpace(roster.membership?.role)
                            ? ""
                            : $" ({roster.membership.role})";
                        _characterMessage.text = $"Select a character for {realm.name}{roleLabel}:";
                        foreach (var character in characters)
                        {
                            var entry = CreateCharacterButton(character);
                            _spawnedCharacterEntries.Add(entry.gameObject);
                        }
                    }

                    _createCharacterButton.interactable = true;
                },
                error =>
                {
                    _characterMessage.text = error.Message;
                    _createCharacterButton.interactable = true;
                });
        }

        private Button CreateCharacterButton(CharacterInfo character)
        {
            var label = string.IsNullOrWhiteSpace(character.bio)
                ? character.name
                : $"{character.name} • {character.bio}";
            var button = CreateListButton(_characterListRoot, $"Character_{character.id}", label);
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

            if (string.IsNullOrWhiteSpace(_characterNameInput?.text))
            {
                _characterMessage.text = "Enter a character name before creating.";
                return;
            }

            if (_characterCreationPanelPrefab == null)
            {
                BeginImmediateCharacterCreation();
                return;
            }

            EnsureCharacterCreationPanelInstance();

            if (_characterCreationPanelInstance == null)
            {
                BeginImmediateCharacterCreation();
                return;
            }

            if (!_characterCreationPanelInstance.gameObject.activeSelf)
            {
                _characterCreationPanelInstance.gameObject.SetActive(true);
            }

            _characterCreationPanelInstance.Refresh();
            _characterMessage.text = "Choose a race to finalize your hero.";
        }

        private void BeginImmediateCharacterCreation()
        {
            var name = _characterNameInput != null ? _characterNameInput.text : string.Empty;
            _characterMessage.text = "Creating character...";
            _createCharacterButton.interactable = false;
            StartCoroutine(CreateCharacterRoutine(SessionManager.SelectedRealmId, name));
        }

        private IEnumerator CreateCharacterRoutine(string realmId, string name, CharacterCreationSelection? selection = null)
        {
            yield return _characterService.CreateCharacter(
                realmId,
                name,
                string.Empty,
                character =>
                {
                    var raceLabel = selection.HasValue && selection.Value.Race != null
                        ? selection.Value.Race.DisplayName
                        : null;
                    if (string.IsNullOrWhiteSpace(raceLabel))
                    {
                        _characterMessage.text = $"Created {character.name}. Select to enter the world.";
                    }
                    else
                    {
                        _characterMessage.text = $"Created {raceLabel} {character.name}. Select to enter the world.";
                    }
                    _characterNameInput.text = string.Empty;
                    var entry = CreateCharacterButton(character);
                    _spawnedCharacterEntries.Add(entry.gameObject);
                    _createCharacterButton.interactable = true;
                },
                error =>
                {
                    _characterMessage.text = error.Message;
                    _createCharacterButton.interactable = true;
                },
                selection);
        }

        private void EnsureCharacterCreationPanelInstance()
        {
            if (_characterCreationPanelInstance != null)
            {
                HookCharacterCreationPanel(_characterCreationPanelInstance);
                return;
            }

            if (_characterCreationPanelPrefab == null)
            {
                return;
            }

            Transform parentTransform = _characterCreationPanelMount != null
                ? _characterCreationPanelMount
                : _characterCanvas != null ? _characterCanvas.transform : transform;

            var instance = Instantiate(_characterCreationPanelPrefab, parentTransform);
            instance.gameObject.name = _characterCreationPanelPrefab.name + "(Instance)";
            instance.gameObject.SetActive(false);
            instance.transform.localScale = Vector3.one;
            if (instance.transform is RectTransform rect)
            {
                rect.anchoredPosition = Vector2.zero;
            }
            instance.transform.SetAsLastSibling();
            _characterCreationPanelInstance = instance;
            HookCharacterCreationPanel(instance);
        }

        private void HookCharacterCreationPanel(CharacterCreationPanel panel)
        {
            if (panel == null || _characterCreationPanelHooked)
            {
                return;
            }

            panel.RaceSelected += OnCharacterCreationRaceSelected;
            panel.Confirmed += OnCharacterCreationConfirmed;
            panel.Cancelled += OnCharacterCreationCancelled;
            _characterCreationPanelHooked = true;
        }

        private void HideCharacterCreationPanel()
        {
            if (_characterCreationPanelInstance != null)
            {
                _characterCreationPanelInstance.gameObject.SetActive(false);
            }
        }

        private void OnCharacterCreationRaceSelected(RaceViewModel race)
        {
            if (race?.Definition != null)
            {
                _characterMessage.text = $"Customizing {race.Definition.DisplayName}.";
            }
        }

        private void OnCharacterCreationConfirmed(CharacterCreationSelection selection)
        {
            if (string.IsNullOrWhiteSpace(SessionManager.SelectedRealmId))
            {
                _characterMessage.text = "Please select a realm first.";
                return;
            }

            if (string.IsNullOrWhiteSpace(_characterNameInput?.text))
            {
                _characterMessage.text = "Enter a character name before confirming.";
                return;
            }

            if (selection.Race == null)
            {
                _characterMessage.text = "Select a race to continue.";
                return;
            }

            HideCharacterCreationPanel();
            _characterMessage.text = $"Creating {selection.Race.DisplayName}...";
            _createCharacterButton.interactable = false;
            StartCoroutine(CreateCharacterRoutine(SessionManager.SelectedRealmId, _characterNameInput.text, selection));
        }

        private void OnCharacterCreationCancelled()
        {
            HideCharacterCreationPanel();
            _characterMessage.text = "Character creation cancelled.";
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

        private void OnDestroy()
        {
            if (_characterCreationPanelInstance != null)
            {
                _characterCreationPanelInstance.RaceSelected -= OnCharacterCreationRaceSelected;
                _characterCreationPanelInstance.Confirmed -= OnCharacterCreationConfirmed;
                _characterCreationPanelInstance.Cancelled -= OnCharacterCreationCancelled;
            }
        }

        private void ShowCanvas(Canvas canvasToShow)
        {
            if (_loginCanvas != null)
            {
                _loginCanvas.gameObject.SetActive(canvasToShow == _loginCanvas);
            }

            if (_realmCanvas != null)
            {
                _realmCanvas.gameObject.SetActive(canvasToShow == _realmCanvas);
            }

            if (_characterCanvas != null)
            {
                _characterCanvas.gameObject.SetActive(canvasToShow == _characterCanvas);
            }
        }

        private string ResolveBaseApiUrl()
        {
            var url = environmentConfig != null && !string.IsNullOrWhiteSpace(environmentConfig.BaseApiUrl)
                ? environmentConfig.BaseApiUrl
                : fallbackBaseApiUrl;

            if (string.IsNullOrWhiteSpace(url))
            {
                return "http://localhost:3000";
            }

            return url.TrimEnd('/');
        }

        private bool ResolveUseMockServices()
        {
            if (environmentConfig != null)
            {
                return environmentConfig.UseMockServices;
            }

            return Application.isEditor
                ? fallbackUseMockServicesInEditor
                : fallbackUseMockServicesInPlayer;
        }

        private Canvas FindCanvas(string childName)
        {
            var child = transform.Find(childName);
            return child != null ? child.GetComponent<Canvas>() : null;
        }
    }
}
