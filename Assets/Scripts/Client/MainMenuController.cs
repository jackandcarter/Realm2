using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Client.BuildState;
using Client.CharacterCreation;
using Client.Equipment;
using Client.Inventory;
using Client.Map;
using Client.Progression;
using Client.UI.Dock;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Client
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("UI Setup")]
        [SerializeField] private Canvas _loginCanvas;
        [SerializeField] private TMP_Text _loginMessage;
        [SerializeField] private TMP_InputField _emailInput;
        [SerializeField] private TMP_InputField _passwordInput;
        [SerializeField] private Toggle _rememberMeToggle;
        [SerializeField] private Button _loginButton;
        [SerializeField] private Button _showCreateAccountButton;

        [SerializeField] private Canvas _createAccountCanvas;
        [SerializeField] private TMP_Text _createAccountMessage;
        [SerializeField] private TMP_InputField _createEmailInput;
        [SerializeField] private TMP_InputField _createUsernameInput;
        [SerializeField] private TMP_InputField _createPasswordInput;
        [SerializeField] private TMP_InputField _createConfirmPasswordInput;
        [SerializeField] private Button _createAccountButton;
        [SerializeField] private Button _backToLoginButton;

        [SerializeField] private Canvas _realmCanvas;
        [SerializeField] private RectTransform _realmListRoot;
        [SerializeField] private Button _reloadRealmsButton;
        [SerializeField] private TMP_Text _realmMessage;
        [SerializeField] private Button _realmEntryTemplate;

        [SerializeField] private Canvas _characterCanvas;
        [SerializeField] private RectTransform _characterListRoot;
        [SerializeField] private TMP_InputField _characterNameInput;
        [SerializeField] private Button _createCharacterButton;
        [SerializeField] private TMP_Text _characterMessage;
        [SerializeField] private TMP_Text _characterCreatedAtLabel;
        [SerializeField] private TMP_Text _characterRaceLabel;
        [SerializeField] private TMP_Text _characterClassLabel;
        [SerializeField] private TMP_Text _characterLocationLabel;
        [SerializeField] private Button _playCharacterButton;
        [SerializeField] private Button _characterEntryTemplate;
        [SerializeField] private CharacterCreationPanel _characterCreationPanelPrefab;
        [SerializeField] private RectTransform _characterCreationPanelMount;

        [Header("API Settings")]
        [SerializeField] private ApiEnvironmentConfig environmentConfig;
        [SerializeField] private string fallbackBaseApiUrl = "http://localhost:3000";
        [SerializeField] private bool fallbackUseMockServicesInEditor = true;
        [SerializeField] private bool fallbackUseMockServicesInPlayer = false;
        [SerializeField] private string worldSceneName = "SampleScene";

        private AuthService _authService;
        private RealmService _realmService;
        private CharacterService _characterService;
        private CharacterProgressionClient _progressionClient;
        private BuildStateClient _buildStateClient;
        private DockLayoutClient _dockLayoutClient;
        private MapPinProgressionClient _mapPinClient;

        private readonly List<GameObject> _spawnedRealmEntries = new();
        private readonly List<GameObject> _spawnedCharacterEntries = new();
        private CharacterCreationPanel _characterCreationPanelInstance;
        private bool _characterCreationPanelHooked;
        private CharacterInfo _selectedCharacter;

        private const string RememberMeKey = "MainMenu.RememberMe";
        private const string RememberEmailKey = "MainMenu.RememberEmail";
        private const string RememberPasswordKey = "MainMenu.RememberPassword";

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
            _progressionClient = new CharacterProgressionClient(baseUrl, useMocks);
            ClassUnlockRepository.SetProgressionClient(_progressionClient);
            InventoryRepository.SetProgressionClient(_progressionClient);
            EquipmentRepository.SetProgressionClient(_progressionClient);
            ApiClientRegistry.Configure(baseUrl, useMocks);

            _buildStateClient = new BuildStateClient(baseUrl, useMocks);
            BuildStateRepository.SetClient(_buildStateClient);

            _dockLayoutClient = new DockLayoutClient(baseUrl, useMocks);
            DockLayoutRepository.SetClient(_dockLayoutClient);

            _mapPinClient = new MapPinProgressionClient(baseUrl, useMocks);
            MapPinProgressionRepository.SetClient(_mapPinClient);

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
            LoadRememberedCredentials();
        }

        private void EnsureUi()
        {
            ResolveCanvas(ref _loginCanvas, "LoginCanvas");
            ResolveCanvas(ref _createAccountCanvas, "CreateAccountCanvas");
            ResolveCanvas(ref _realmCanvas, "RealmCanvas");
            ResolveCanvas(ref _characterCanvas, "CharacterCanvas");

            WireUiEvents();
            ValidateUiReferences();

            if (_characterCreationPanelMount == null && _characterCanvas != null)
            {
                _characterCreationPanelMount = (RectTransform)_characterCanvas.transform;
            }

            if (_realmCanvas != null)
            {
                _realmCanvas.gameObject.SetActive(false);
            }

            if (_createAccountCanvas != null)
            {
                _createAccountCanvas.gameObject.SetActive(false);
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

        private void WireUiEvents()
        {
            if (_loginButton != null)
            {
                _loginButton.onClick.RemoveListener(OnLoginClicked);
                _loginButton.onClick.AddListener(OnLoginClicked);
            }

            if (_rememberMeToggle != null)
            {
                _rememberMeToggle.onValueChanged.RemoveListener(OnRememberMeToggled);
                _rememberMeToggle.onValueChanged.AddListener(OnRememberMeToggled);
            }

            if (_showCreateAccountButton != null)
            {
                _showCreateAccountButton.onClick.RemoveListener(ShowCreateAccount);
                _showCreateAccountButton.onClick.AddListener(ShowCreateAccount);
            }

            if (_createAccountButton != null)
            {
                _createAccountButton.onClick.RemoveListener(OnCreateAccountClicked);
                _createAccountButton.onClick.AddListener(OnCreateAccountClicked);
            }

            if (_backToLoginButton != null)
            {
                _backToLoginButton.onClick.RemoveListener(ShowLoginPanel);
                _backToLoginButton.onClick.AddListener(ShowLoginPanel);
            }

            if (_reloadRealmsButton != null)
            {
                _reloadRealmsButton.onClick.RemoveAllListeners();
                _reloadRealmsButton.onClick.AddListener(() => StartCoroutine(LoadRealms()));
            }

            if (_playCharacterButton != null)
            {
                _playCharacterButton.onClick.RemoveListener(OnPlaySelectedCharacter);
                _playCharacterButton.onClick.AddListener(OnPlaySelectedCharacter);
                _playCharacterButton.interactable = false;
            }

            if (_createCharacterButton != null)
            {
                _createCharacterButton.onClick.RemoveListener(OnCreateCharacterClicked);
                _createCharacterButton.onClick.AddListener(OnCreateCharacterClicked);
            }
        }

        private void ValidateUiReferences()
        {
            if (_loginCanvas == null)
            {
                Debug.LogWarning("MainMenuController is missing LoginCanvas. Assign it in the scene or prefab.", this);
            }

            if (_loginMessage == null)
            {
                Debug.LogWarning("MainMenuController is missing LoginMessage text reference.", this);
            }

            if (_emailInput == null || _passwordInput == null)
            {
                Debug.LogWarning("MainMenuController is missing login input fields.", this);
            }

            if (_createAccountCanvas == null)
            {
                Debug.LogWarning("MainMenuController is missing CreateAccountCanvas.", this);
            }

            if (_createEmailInput == null || _createUsernameInput == null || _createPasswordInput == null || _createConfirmPasswordInput == null)
            {
                Debug.LogWarning("MainMenuController is missing create account input fields.", this);
            }

            if (_loginButton == null)
            {
                Debug.LogWarning("MainMenuController is missing LoginButton.", this);
            }

            if (_realmCanvas == null)
            {
                Debug.LogWarning("MainMenuController is missing RealmCanvas.", this);
            }

            if (_realmListRoot == null || _realmEntryTemplate == null)
            {
                Debug.LogWarning("MainMenuController is missing realm list root or entry template.", this);
            }

            if (_characterCanvas == null)
            {
                Debug.LogWarning("MainMenuController is missing CharacterCanvas.", this);
            }

            if (_characterListRoot == null || _characterEntryTemplate == null)
            {
                Debug.LogWarning("MainMenuController is missing character list root or entry template.", this);
            }
        }

        private void ResolveCanvas(ref Canvas canvasField, string canvasName)
        {
            if (canvasField == null)
            {
                canvasField = FindCanvas(canvasName);
            }
        }

        private void EnsureEventSystem()
        {
            var existing = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (existing == null)
            {
                Debug.LogWarning("MainMenuController requires an EventSystem in the scene. Add one in edit mode.", this);
                return;
            }

#if ENABLE_INPUT_SYSTEM
            if (existing.GetComponent<InputSystemUIInputModule>() == null)
            {
                Debug.LogWarning("EventSystem is missing an InputSystemUIInputModule. Add it in edit mode.", existing);
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

        private void OnLoginClicked()
        {
            if (_loginButton == null || _loginMessage == null || _emailInput == null || _passwordInput == null)
            {
                Debug.LogWarning("MainMenuController login UI is not fully wired. Assign references in the scene or prefab.", this);
                return;
            }

            _loginButton.interactable = false;
            SetMessage(_loginMessage, "Authenticating...");
            StartCoroutine(LoginRoutine());
        }

        private void ShowLoginPanel()
        {
            ShowCanvas(_loginCanvas);
        }

        private void ShowCreateAccount()
        {
            ShowCanvas(_createAccountCanvas);
        }

        private void OnRememberMeToggled(bool isOn)
        {
            if (!isOn)
            {
                PlayerPrefs.DeleteKey(RememberEmailKey);
                PlayerPrefs.DeleteKey(RememberPasswordKey);
                PlayerPrefs.SetInt(RememberMeKey, 0);
                PlayerPrefs.Save();
            }
        }

        private void OnCreateAccountClicked()
        {
            if (_createAccountButton == null || _createAccountMessage == null)
            {
                Debug.LogWarning("MainMenuController create account UI is not fully wired. Assign references in the scene or prefab.", this);
                return;
            }

            var email = _createEmailInput != null ? _createEmailInput.text.Trim() : string.Empty;
            var username = _createUsernameInput != null ? _createUsernameInput.text.Trim() : string.Empty;
            var password = _createPasswordInput != null ? _createPasswordInput.text : string.Empty;
            var confirm = _createConfirmPasswordInput != null ? _createConfirmPasswordInput.text : string.Empty;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                SetMessage(_createAccountMessage, "Email, username, and password are required.");
                return;
            }

            if (!string.Equals(password, confirm, StringComparison.Ordinal))
            {
                SetMessage(_createAccountMessage, "Passwords do not match.");
                return;
            }

            _createAccountButton.interactable = false;
            SetMessage(_createAccountMessage, "Creating account...");
            StartCoroutine(RegisterRoutine(email, username, password));
        }

        private IEnumerator LoginRoutine()
        {
            yield return _authService.Login(
                _emailInput.text,
                _passwordInput.text,
                response =>
                {
                    SetMessage(_loginMessage, "Login successful!");
                    PersistRememberedCredentials();
                    ShowCanvas(_realmCanvas);
                    StartCoroutine(LoadRealms());
                },
                error =>
                {
                    _loginButton.interactable = true;
                    SetMessage(_loginMessage, error.Message);
                });
        }

        private IEnumerator RegisterRoutine(string email, string username, string password)
        {
            yield return _authService.Register(
                email,
                username,
                password,
                response =>
                {
                    if (_emailInput != null)
                    {
                        _emailInput.SetTextWithoutNotify(email);
                    }

                    if (_passwordInput != null)
                    {
                        _passwordInput.SetTextWithoutNotify(password);
                    }

                    PersistRememberedCredentials();
                    SetMessage(_loginMessage, "Account created! Welcome.");
                    ShowCanvas(_realmCanvas);
                    StartCoroutine(LoadRealms());
                    ResetCreateAccountFields();
                    if (_createAccountButton != null)
                    {
                        _createAccountButton.interactable = true;
                    }
                },
                error =>
                {
                    SetMessage(_createAccountMessage, error.Message);
                    if (_createAccountButton != null)
                    {
                        _createAccountButton.interactable = true;
                    }
                });
        }

        private IEnumerator LoadRealms()
        {
            SetMessage(_realmMessage, "Loading realms...");

            if (_reloadRealmsButton != null)
            {
                _reloadRealmsButton.interactable = false;
            }
            ClearList(_spawnedRealmEntries);

            yield return _realmService.GetRealms(
                realms =>
                {
                    if (realms.Count == 0)
                    {
                        SetMessage(_realmMessage, "No realms available.");
                        return;
                    }

                    SetMessage(_realmMessage, "Choose a realm:");
                    foreach (var realm in realms)
                    {
                        var entry = CreateRealmButton(realm);
                        if (entry != null)
                        {
                            _spawnedRealmEntries.Add(entry.gameObject);
                        }
                    }
                },
                error =>
                {
                    SetMessage(_realmMessage, error.Message);
                });

            if (_reloadRealmsButton != null)
            {
                _reloadRealmsButton.interactable = true;
            }
        }

        private Button CreateRealmButton(RealmInfo realm)
        {
            var membershipLabel = realm.isMember
                ? string.IsNullOrWhiteSpace(realm.membershipRole) ? "Member" : $"Member: {realm.membershipRole}"
                : "Joinable";
            var button = CreateListButton(
                _realmListRoot,
                $"Realm_{realm.id}",
                $"{realm.name} • {membershipLabel}",
                _realmEntryTemplate
            );
            if (button == null)
            {
                return null;
            }
            button.onClick.AddListener(() => OnRealmSelected(realm));
            return button;
        }

        private void OnRealmSelected(RealmInfo realm)
        {
            SessionManager.SetRealm(realm.id);
            SetMessage(_characterMessage, $"Loading characters for {realm.name}...");
            ShowCanvas(_characterCanvas);
            StartCoroutine(LoadCharacters(realm));
        }

        private IEnumerator LoadCharacters(RealmInfo realm)
        {
            ClearList(_spawnedCharacterEntries);
            if (_createCharacterButton != null)
            {
                _createCharacterButton.interactable = false;
            }
            DisplayCharacterDetails(null);

            yield return _characterService.GetCharacters(
                realm.id,
                roster =>
                {
                    var characters = roster.characters ?? Array.Empty<CharacterInfo>();
                    if (characters.Length == 0)
                    {
                        SetMessage(_characterMessage, "No characters found. Create a new hero.");
                    }
                    else
                    {
                        var roleLabel = string.IsNullOrWhiteSpace(roster.membership?.role)
                            ? ""
                            : $" ({roster.membership.role})";
                        SetMessage(_characterMessage, $"Select a character for {realm.name}{roleLabel}:");
                        foreach (var character in characters)
                        {
                            ClassUnlockRepository.TrackCharacter(character);
                            if (_progressionClient != null)
                            {
                                StartCoroutine(
                                    ClassUnlockRepository.SyncWithServer(
                                        character.id,
                                        snapshot =>
                                        {
                                            if (snapshot != null && character != null &&
                                                string.Equals(character.id, _selectedCharacter?.id, StringComparison.Ordinal))
                                            {
                                                DisplayCharacterDetails(character);
                                            }
                                        },
                                        error =>
                                        {
                                            if (error != null)
                                            {
                                                Debug.LogWarning($"Failed to sync progression for {character.name}: {error.Message}");
                                            }
                                        }));
                            }
                            var entry = CreateCharacterButton(character);
                            if (entry != null)
                            {
                                _spawnedCharacterEntries.Add(entry.gameObject);
                            }
                        }
                    }

                    if (_createCharacterButton != null)
                    {
                        _createCharacterButton.interactable = true;
                    }
                },
                error =>
                {
                    SetMessage(_characterMessage, error.Message);

                    if (_createCharacterButton != null)
                    {
                        _createCharacterButton.interactable = true;
                    }
                });
        }

        private Button CreateCharacterButton(CharacterInfo character)
        {
            var label = string.IsNullOrWhiteSpace(character.bio)
                ? character.name
                : $"{character.name} • {character.bio}";
            var status = CharacterClassStatusUtility.Evaluate(character);
            if (status.RequiresAttention)
            {
                var statusBadge = string.IsNullOrWhiteSpace(status.StatusLabel) ? "Needs review" : status.StatusLabel;
                label = $"{label} [{statusBadge}]";
            }

            var button = CreateListButton(_characterListRoot, $"Character_{character.id}", label, _characterEntryTemplate);
            if (button == null)
            {
                return null;
            }
            if (status.RequiresAttention)
            {
                var image = button.GetComponent<Image>();
                if (image != null)
                {
                    image.color = new Color(0.9f, 0.75f, 0.75f, 0.95f);
                }
            }
            button.onClick.AddListener(() => OnCharacterEntryClicked(character));
            return button;
        }

        private void BeginPlayForCharacter(CharacterInfo character)
        {
            SetMessage(_characterMessage, "Connecting to realm...");
            if (_createCharacterButton != null)
            {
                _createCharacterButton.interactable = false;
            }
            StartCoroutine(SelectCharacterRoutine(character));
        }

        private void OnCharacterEntryClicked(CharacterInfo character)
        {
            var classStatus = DisplayCharacterDetails(character);

            if (character == null)
            {
                SetMessage(_characterMessage, classStatus.Message);
                return;
            }

            if (!classStatus.CanPlay)
            {
                SetMessage(_characterMessage, classStatus.Message);
                return;
            }

            var builderUnlocked = ClassUnlockRepository.IsClassUnlocked(character.id, ClassUnlockUtility.BuilderClassId);
            SetMessage(_characterMessage,
                builderUnlocked
                ? $"Ready to enter as {character.name}."
                : $"{character.name} must unlock the Builder class before accessing Builder mode.");
        }

        private void OnPlaySelectedCharacter()
        {
            if (_selectedCharacter == null)
            {
                SetMessage(_characterMessage, "Select a character first.");
                return;
            }

            var classStatus = CharacterClassStatusUtility.Evaluate(_selectedCharacter);
            if (!classStatus.CanPlay)
            {
                SetMessage(_characterMessage, classStatus.Message);
                return;
            }

            BeginPlayForCharacter(_selectedCharacter);
        }

        private CharacterClassStatusInfo DisplayCharacterDetails(CharacterInfo character)
        {
            _selectedCharacter = character;

            var statusPreview = CharacterClassStatusUtility.Evaluate(character);
            if (_playCharacterButton != null)
            {
                _playCharacterButton.interactable = character != null && statusPreview.CanPlay;
            }

            if (character == null)
            {
                SetDetailLabel(_characterCreatedAtLabel, "Created: —");
                SetDetailLabel(_characterRaceLabel, "Race: —");
                SetDetailLabel(_characterClassLabel, "Class: —");
                SetDetailLabel(_characterLocationLabel, "Last Known Location: —");
                return statusPreview;
            }

            var createdDisplay = FormatCreatedDate(character.createdAt);
            var raceDisplay = ResolveRaceName(character.raceId);
            var classStatus = statusPreview;
            var unlockedSummary = BuildUnlockedClassesSummary(character.classStates);
            var locationDisplay = string.IsNullOrWhiteSpace(character.lastKnownLocation)
                ? "Unknown"
                : character.lastKnownLocation.Trim();

            SetDetailLabel(_characterCreatedAtLabel, $"Created: {createdDisplay}");
            SetDetailLabel(_characterRaceLabel, $"Race: {raceDisplay}");
            var classLabel = string.IsNullOrWhiteSpace(classStatus.StatusLabel)
                ? $"Class: {classStatus.ClassDisplay}"
                : $"Class: {classStatus.ClassDisplay} ({classStatus.StatusLabel})";
            SetDetailLabel(_characterClassLabel, $"{classLabel} • Unlocked: {unlockedSummary}");
            SetDetailLabel(_characterLocationLabel, $"Last Known Location: {locationDisplay}");
            return classStatus;
        }

        private static void SetDetailLabel(TMP_Text label, string value)
        {
            if (label != null)
            {
                label.text = value;
            }
        }

        private static void SetMessage(TMP_Text label, string message)
        {
            if (label != null)
            {
                label.text = message;
            }
        }

        private static string FormatCreatedDate(string createdAt)
        {
            if (string.IsNullOrWhiteSpace(createdAt))
            {
                return "Unknown";
            }

            if (DateTime.TryParse(
                    createdAt,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                    out var parsedUtc))
            {
                return parsedUtc.ToLocalTime().ToString("MMMM d, yyyy", CultureInfo.CurrentCulture);
            }

            if (DateTime.TryParse(createdAt, out var parsed))
            {
                return parsed.ToLocalTime().ToString("MMMM d, yyyy", CultureInfo.CurrentCulture);
            }

            return createdAt;
        }

        private static string ResolveRaceName(string raceId)
        {
            if (!string.IsNullOrWhiteSpace(raceId) && RaceCatalog.TryGetRace(raceId, out var race))
            {
                return race.DisplayName;
            }

            return string.IsNullOrWhiteSpace(raceId) ? "Unknown" : raceId.Trim();
        }

        private static string BuildUnlockedClassesSummary(ClassUnlockState[] states)
        {
            if (states == null || states.Length == 0)
            {
                return "None";
            }

            var unlockedNames = new List<string>();
            foreach (var state in states)
            {
                if (state == null || !state.Unlocked)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(state.ClassId) && ClassCatalog.TryGetClass(state.ClassId, out var definition))
                {
                    unlockedNames.Add(definition.DisplayName);
                }
                else if (!string.IsNullOrWhiteSpace(state?.ClassId))
                {
                    unlockedNames.Add(state.ClassId.Trim());
                }
            }

            return unlockedNames.Count > 0 ? string.Join(", ", unlockedNames) : "None";
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
                    SetMessage(_characterMessage, error.Message);
                    if (_createCharacterButton != null)
                    {
                        _createCharacterButton.interactable = true;
                    }
                });
        }

        private void OnCreateCharacterClicked()
        {
            if (string.IsNullOrWhiteSpace(SessionManager.SelectedRealmId))
            {
                SetMessage(_characterMessage, "Please select a realm first.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_characterNameInput?.text))
            {
                SetMessage(_characterMessage, "Enter a character name before creating.");
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
            SetMessage(_characterMessage, "Choose a race to finalize your hero.");
        }

        private void BeginImmediateCharacterCreation()
        {
            var name = _characterNameInput != null ? _characterNameInput.text : string.Empty;
            SetMessage(_characterMessage, "Creating character...");
            if (_createCharacterButton != null)
            {
                _createCharacterButton.interactable = false;
            }
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
                    var classLabel = selection.HasValue && selection.Value.Class != null
                        ? selection.Value.Class.DisplayName
                        : null;
                    if (string.IsNullOrWhiteSpace(classLabel) && !string.IsNullOrWhiteSpace(character.classId))
                    {
                        if (ClassCatalog.TryGetClass(character.classId, out var classDefinition))
                        {
                            classLabel = classDefinition.DisplayName;
                        }
                    }

                    var descriptors = new List<string>();
                    if (!string.IsNullOrWhiteSpace(raceLabel))
                    {
                        descriptors.Add(raceLabel);
                    }

                    if (!string.IsNullOrWhiteSpace(classLabel))
                    {
                        descriptors.Add(classLabel);
                    }

                    var descriptorText = descriptors.Count > 0 ? string.Join(" ", descriptors) + " " : string.Empty;
                    SetMessage(_characterMessage, $"Created {descriptorText}{character.name}. Select to enter the world.");
                    if (_characterNameInput != null)
                    {
                        _characterNameInput.text = string.Empty;
                    }
                    ClassUnlockRepository.TrackCharacter(character);
                    var entry = CreateCharacterButton(character);
                    if (entry != null)
                    {
                        _spawnedCharacterEntries.Add(entry.gameObject);
                    }
                    DisplayCharacterDetails(character);
                    if (_createCharacterButton != null)
                    {
                        _createCharacterButton.interactable = true;
                    }
                },
                error =>
                {
                    SetMessage(_characterMessage, error.Message);
                    if (_createCharacterButton != null)
                    {
                        _createCharacterButton.interactable = true;
                    }
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
            panel.ClassSelected += OnCharacterCreationClassSelected;
            panel.Confirmed += OnCharacterCreationConfirmed;
            panel.Cancelled += OnCharacterCreationCancelled;
            panel.BindToCharacter(SessionManager.SelectedCharacterId);
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
                SetMessage(_characterMessage, $"Customizing {race.Definition.DisplayName}. Choose a starting class.");
            }
        }

        private void OnCharacterCreationClassSelected(CharacterClassDefinition classDefinition)
        {
            if (classDefinition != null)
            {
                SetMessage(_characterMessage, $"Starting class set to {classDefinition.DisplayName}.");
            }
        }

        private void OnCharacterCreationConfirmed(CharacterCreationSelection selection)
        {
            if (string.IsNullOrWhiteSpace(SessionManager.SelectedRealmId))
            {
                SetMessage(_characterMessage, "Please select a realm first.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_characterNameInput?.text))
            {
                SetMessage(_characterMessage, "Enter a character name before confirming.");
                return;
            }

            if (selection.Race == null)
            {
                SetMessage(_characterMessage, "Select a race to continue.");
                return;
            }

            if (selection.Class == null)
            {
                SetMessage(_characterMessage, "Select a starting class to continue.");
                return;
            }

            HideCharacterCreationPanel();
            SetMessage(_characterMessage, $"Creating {selection.Race.DisplayName}...");
            if (_createCharacterButton != null)
            {
                _createCharacterButton.interactable = false;
            }
            StartCoroutine(CreateCharacterRoutine(SessionManager.SelectedRealmId, _characterNameInput.text, selection));
        }

        private void OnCharacterCreationCancelled()
        {
            HideCharacterCreationPanel();
            SetMessage(_characterMessage, "Character creation cancelled.");
        }

        private Button CreateListButton(RectTransform parent, string name, string label, Button templateOverride = null)
        {
            var template = templateOverride != null ? templateOverride : _realmEntryTemplate;
            if (parent == null || template == null)
            {
                Debug.LogWarning("MainMenuController list entry template is missing.", this);
                return null;
            }

            var instance = Application.isPlaying ? Instantiate(template, parent) : UnityEngine.Object.Instantiate(template, parent);
            instance.gameObject.name = name;
            instance.gameObject.SetActive(true);

            if (instance.GetComponentInChildren<TMP_Text>(true) is TMP_Text tmpLabel)
            {
                tmpLabel.text = label;
            }
            else if (instance.GetComponentInChildren<Text>(true) is Text legacyLabel)
            {
                legacyLabel.text = label;
            }
            else
            {
                Debug.LogWarning($"List entry '{name}' is missing a Text/TMP_Text component to set the label.", instance);
            }

            return instance;
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
                _characterCreationPanelInstance.ClassSelected -= OnCharacterCreationClassSelected;
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

            if (_createAccountCanvas != null)
            {
                _createAccountCanvas.gameObject.SetActive(canvasToShow == _createAccountCanvas);
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

        private void PersistRememberedCredentials()
        {
            if (_rememberMeToggle == null)
            {
                return;
            }

            var remember = _rememberMeToggle.isOn;
            PlayerPrefs.SetInt(RememberMeKey, remember ? 1 : 0);

            if (remember)
            {
                PlayerPrefs.SetString(RememberEmailKey, _emailInput != null ? _emailInput.text : string.Empty);
                PlayerPrefs.SetString(RememberPasswordKey, _passwordInput != null ? _passwordInput.text : string.Empty);
            }
            else
            {
                PlayerPrefs.DeleteKey(RememberEmailKey);
                PlayerPrefs.DeleteKey(RememberPasswordKey);
            }

            PlayerPrefs.Save();
        }

        private void LoadRememberedCredentials()
        {
            if (_rememberMeToggle == null || _emailInput == null || _passwordInput == null)
            {
                return;
            }

            var remember = PlayerPrefs.GetInt(RememberMeKey, 0) == 1;
            _rememberMeToggle.SetIsOnWithoutNotify(remember);
            if (remember)
            {
                _emailInput.SetTextWithoutNotify(PlayerPrefs.GetString(RememberEmailKey, string.Empty));
                _passwordInput.SetTextWithoutNotify(PlayerPrefs.GetString(RememberPasswordKey, string.Empty));
            }
        }

        private void ResetCreateAccountFields()
        {
            if (_createEmailInput != null)
            {
                _createEmailInput.SetTextWithoutNotify(string.Empty);
            }

            if (_createUsernameInput != null)
            {
                _createUsernameInput.SetTextWithoutNotify(string.Empty);
            }

            if (_createPasswordInput != null)
            {
                _createPasswordInput.SetTextWithoutNotify(string.Empty);
            }

            if (_createConfirmPasswordInput != null)
            {
                _createConfirmPasswordInput.SetTextWithoutNotify(string.Empty);
            }

            SetMessage(_createAccountMessage, "");
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
