using System.IO;
using Client;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Realm.Editor.UI
{
    public static class MainMenuUiGenerator
    {
        private const string PrefabPath = "Assets/UI/MainMenu/MainMenu.prefab";
        private const string MenuRoot = "Tools/Realm/UI";

        [MenuItem(MenuRoot + "/Generate Main Menu UI", priority = 100)]
        public static void GenerateMainMenu()
        {
            var controller = FindOrCreateMainMenuRoot();
            EnsureEventSystem();

            var root = controller.gameObject;
            ConfigureCanvas(root.GetComponent<Canvas>(), root.GetComponent<RectTransform>(), root.GetComponent<CanvasScaler>());

            var font = TMP_Settings.defaultFontAsset;

            var loginCanvas = FindOrCreateCanvasChild(root.transform, "LoginCanvas");
            var createCanvas = FindOrCreateCanvasChild(root.transform, "CreateAccountCanvas");
            var realmCanvas = FindOrCreateCanvasChild(root.transform, "RealmCanvas");
            var characterCanvas = FindOrCreateCanvasChild(root.transform, "CharacterCanvas");

            var loginPanel = BuildLoginPanel(loginCanvas.transform, font, out var loginMessage, out var emailInput, out var passwordInput, out var rememberToggle, out var loginButton, out var showCreateButton);
            var createPanel = BuildCreateAccountPanel(createCanvas.transform, font, out var createMessage, out var createEmailInput, out var createUsernameInput, out var createPasswordInput, out var createConfirmInput, out var createButton, out var backButton);
            var realmPanel = BuildRealmPanel(realmCanvas.transform, font, out var realmMessage, out var realmListRoot, out var realmEntryTemplate, out var reloadRealmsButton);
            var characterPanel = BuildCharacterPanel(characterCanvas.transform, font, out var characterMessage, out var characterListRoot, out var characterEntryTemplate, out var characterNameInput, out var createCharacterButton, out var playButton, out var createdAtLabel, out var raceLabel, out var classLabel, out var locationLabel, out var creationPanelMount);

            ApplyControllerBindings(controller, loginCanvas, loginMessage, emailInput, passwordInput, rememberToggle, loginButton, showCreateButton, createCanvas, createMessage, createEmailInput, createUsernameInput, createPasswordInput, createConfirmInput, createButton, backButton, realmCanvas, realmMessage, realmListRoot, realmEntryTemplate, reloadRealmsButton, characterCanvas, characterListRoot, characterNameInput, createCharacterButton, characterMessage, createdAtLabel, raceLabel, classLabel, locationLabel, playButton, characterEntryTemplate, creationPanelMount);

            SavePrefab(root);
            Selection.activeGameObject = root;
        }

        private static MainMenuController FindOrCreateMainMenuRoot()
        {
            var existing = Object.FindFirstObjectByType<MainMenuController>();
            if (existing != null)
            {
                return existing;
            }

            var root = new GameObject("MainMenu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MainMenuController));
            Undo.RegisterCreatedObjectUndo(root, "Create Main Menu");
            ApplyUiLayer(root);
            return root.GetComponent<MainMenuController>();
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem != null)
            {
                return;
            }

            var system = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(system, "Create EventSystem");
        }

        private static void ConfigureCanvas(Canvas canvas, RectTransform rectTransform, CanvasScaler scaler)
        {
            if (canvas == null || rectTransform == null || scaler == null)
            {
                return;
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        }

        private static Canvas FindOrCreateCanvasChild(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null && existing.TryGetComponent(out Canvas existingCanvas))
            {
                ConfigureCanvas(existingCanvas, existingCanvas.GetComponent<RectTransform>(), existingCanvas.GetComponent<CanvasScaler>());
                return existingCanvas;
            }

            var canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasObject, "Create Main Menu Canvas");
            canvasObject.transform.SetParent(parent, false);
            ApplyUiLayer(canvasObject);
            var canvas = canvasObject.GetComponent<Canvas>();
            ConfigureCanvas(canvas, canvasObject.GetComponent<RectTransform>(), canvasObject.GetComponent<CanvasScaler>());
            return canvas;
        }

        private static RectTransform BuildLoginPanel(Transform parent, TMP_FontAsset font, out TMP_Text message, out TMP_InputField emailInput, out TMP_InputField passwordInput, out Toggle rememberToggle, out Button loginButton, out Button showCreateButton)
        {
            var panel = CreatePanel(parent, "LoginPanel", new Color(0.08f, 0.09f, 0.12f, 0.95f));
            CreateHeader(panel, "Welcome Back", font);
            emailInput = CreateLabeledInput(panel, "Email", "Email", font, false);
            passwordInput = CreateLabeledInput(panel, "Password", "Password", font, true);
            rememberToggle = CreateToggle(panel, "RememberMeToggle", "Remember Me", font);
            loginButton = CreateButton(panel, "LoginButton", "Login", font);
            showCreateButton = CreateButton(panel, "ShowCreateAccountButton", "Create Account", font);
            message = CreateMessage(panel, "LoginMessage", font);
            return panel;
        }

        private static RectTransform BuildCreateAccountPanel(Transform parent, TMP_FontAsset font, out TMP_Text message, out TMP_InputField emailInput, out TMP_InputField usernameInput, out TMP_InputField passwordInput, out TMP_InputField confirmInput, out Button createButton, out Button backButton)
        {
            var panel = CreatePanel(parent, "CreateAccountPanel", new Color(0.08f, 0.09f, 0.12f, 0.95f));
            CreateHeader(panel, "Create Account", font);
            emailInput = CreateLabeledInput(panel, "CreateEmail", "Email", font, false);
            usernameInput = CreateLabeledInput(panel, "CreateUsername", "Username", font, false);
            passwordInput = CreateLabeledInput(panel, "CreatePassword", "Password", font, true);
            confirmInput = CreateLabeledInput(panel, "CreateConfirmPassword", "Confirm Password", font, true);
            createButton = CreateButton(panel, "CreateAccountButton", "Create Account", font);
            backButton = CreateButton(panel, "BackToLoginButton", "Back to Login", font);
            message = CreateMessage(panel, "CreateAccountMessage", font);
            return panel;
        }

        private static RectTransform BuildRealmPanel(Transform parent, TMP_FontAsset font, out TMP_Text message, out RectTransform listRoot, out Button entryTemplate, out Button reloadButton)
        {
            var panel = CreatePanel(parent, "RealmPanel", new Color(0.07f, 0.08f, 0.12f, 0.95f));
            CreateHeader(panel, "Select Realm", font);
            message = CreateMessage(panel, "RealmMessage", font);
            listRoot = CreateListRoot(panel, "RealmListRoot");
            entryTemplate = CreateListEntryTemplate(listRoot, "RealmEntryTemplate", "Realm Name", font);
            entryTemplate.gameObject.SetActive(false);
            reloadButton = CreateButton(panel, "ReloadRealmsButton", "Reload Realms", font);
            return panel;
        }

        private static RectTransform BuildCharacterPanel(Transform parent, TMP_FontAsset font, out TMP_Text message, out RectTransform listRoot, out Button entryTemplate, out TMP_InputField nameInput, out Button createButton, out Button playButton, out TMP_Text createdAtLabel, out TMP_Text raceLabel, out TMP_Text classLabel, out TMP_Text locationLabel, out RectTransform creationMount)
        {
            var panel = CreatePanel(parent, "CharacterPanel", new Color(0.07f, 0.08f, 0.12f, 0.95f));
            CreateHeader(panel, "Choose Your Hero", font);
            message = CreateMessage(panel, "CharacterMessage", font);
            listRoot = CreateListRoot(panel, "CharacterListRoot");
            entryTemplate = CreateListEntryTemplate(listRoot, "CharacterEntryTemplate", "Character Name", font);
            entryTemplate.gameObject.SetActive(false);
            nameInput = CreateLabeledInput(panel, "CharacterNameInput", "Character Name", font, false);
            createButton = CreateButton(panel, "CreateCharacterButton", "Create Character", font);
            playButton = CreateButton(panel, "PlayCharacterButton", "Enter Realm", font);
            var details = CreateDetailsPanel(panel, "CharacterDetails");
            createdAtLabel = CreateDetailLabel(details, "CharacterCreatedAtLabel", "Created: —", font);
            raceLabel = CreateDetailLabel(details, "CharacterRaceLabel", "Race: —", font);
            classLabel = CreateDetailLabel(details, "CharacterClassLabel", "Class: —", font);
            locationLabel = CreateDetailLabel(details, "CharacterLocationLabel", "Last Known Location: —", font);
            creationMount = CreateMount(panel, "CharacterCreationPanelMount");
            return panel;
        }

        private static RectTransform CreatePanel(Transform parent, string name, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            Undo.RegisterCreatedObjectUndo(panel, "Create Main Menu Panel");
            panel.transform.SetParent(parent, false);
            ApplyUiLayer(panel);

            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(720f, 720f);
            rect.anchoredPosition = Vector2.zero;

            var image = panel.GetComponent<Image>();
            image.color = color;

            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(32, 32, 32, 32);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var fitter = panel.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            return rect;
        }

        private static void CreateHeader(RectTransform parent, string text, TMP_FontAsset font)
        {
            var label = CreateText(parent, "Header", text, font, 34, FontStyles.Bold);
            label.alignment = TextAlignmentOptions.Center;
        }

        private static TMP_Text CreateMessage(RectTransform parent, string name, TMP_FontAsset font)
        {
            var message = CreateText(parent, name, string.Empty, font, 18, FontStyles.Italic);
            message.color = new Color(0.85f, 0.86f, 0.9f, 0.9f);
            return message;
        }

        private static TMP_InputField CreateLabeledInput(RectTransform parent, string name, string labelText, TMP_FontAsset font, bool isPassword)
        {
            var container = new GameObject($"{name}Container", typeof(RectTransform), typeof(VerticalLayoutGroup));
            Undo.RegisterCreatedObjectUndo(container, "Create Main Menu Input");
            container.transform.SetParent(parent, false);
            ApplyUiLayer(container);
            var layout = container.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childAlignment = TextAnchor.UpperLeft;

            CreateText(container.transform as RectTransform, $"{name}Label", labelText, font, 18, FontStyles.Normal);
            var input = CreateInputField(container.transform as RectTransform, name, labelText, font, isPassword);
            return input;
        }

        private static TMP_InputField CreateInputField(RectTransform parent, string name, string placeholderText, TMP_FontAsset font, bool isPassword)
        {
            var fieldObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(fieldObject, "Create Main Menu InputField");
            fieldObject.transform.SetParent(parent, false);
            ApplyUiLayer(fieldObject);

            var rect = fieldObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, 48f);

            var image = fieldObject.GetComponent<Image>();
            image.color = new Color(0.13f, 0.14f, 0.18f, 0.9f);

            var layout = fieldObject.GetComponent<LayoutElement>();
            layout.preferredHeight = 48f;

            var inputField = fieldObject.GetComponent<TMP_InputField>();
            inputField.contentType = isPassword ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
            inputField.lineType = TMP_InputField.LineType.SingleLine;

            var textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            Undo.RegisterCreatedObjectUndo(textArea, "Create Main Menu Text Area");
            textArea.transform.SetParent(fieldObject.transform, false);
            ApplyUiLayer(textArea);

            var textAreaRect = textArea.GetComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(10f, 6f);
            textAreaRect.offsetMax = new Vector2(-10f, -6f);

            var placeholder = CreateText(textAreaRect, "Placeholder", placeholderText, font, 16, FontStyles.Italic);
            placeholder.color = new Color(0.65f, 0.67f, 0.72f, 0.85f);

            var text = CreateText(textAreaRect, "Text", string.Empty, font, 16, FontStyles.Normal);
            text.color = new Color(0.92f, 0.94f, 0.98f, 1f);

            inputField.textViewport = textAreaRect;
            inputField.textComponent = text;
            inputField.placeholder = placeholder;

            return inputField;
        }

        private static Toggle CreateToggle(RectTransform parent, string name, string label, TMP_FontAsset font)
        {
            var toggleObject = new GameObject(name, typeof(RectTransform), typeof(Toggle), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(toggleObject, "Create Main Menu Toggle");
            toggleObject.transform.SetParent(parent, false);
            ApplyUiLayer(toggleObject);

            var rect = toggleObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, 32f);

            var layout = toggleObject.GetComponent<LayoutElement>();
            layout.preferredHeight = 32f;

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(toggleObject.transform, false);
            ApplyUiLayer(background);

            var backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0f, 0.5f);
            backgroundRect.anchorMax = new Vector2(0f, 0.5f);
            backgroundRect.sizeDelta = new Vector2(22f, 22f);
            backgroundRect.anchoredPosition = new Vector2(12f, 0f);

            var backgroundImage = background.GetComponent<Image>();
            backgroundImage.color = new Color(0.13f, 0.14f, 0.18f, 1f);

            var checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkmark.transform.SetParent(background.transform, false);
            ApplyUiLayer(checkmark);
            var checkmarkRect = checkmark.GetComponent<RectTransform>();
            checkmarkRect.anchorMin = Vector2.zero;
            checkmarkRect.anchorMax = Vector2.one;
            checkmarkRect.offsetMin = new Vector2(4f, 4f);
            checkmarkRect.offsetMax = new Vector2(-4f, -4f);
            var checkmarkImage = checkmark.GetComponent<Image>();
            checkmarkImage.color = new Color(0.36f, 0.7f, 1f, 1f);

            var labelText = CreateText(toggleObject.transform as RectTransform, "Label", label, font, 18, FontStyles.Normal);
            var labelRect = labelText.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(40f, 0f);
            labelRect.offsetMax = new Vector2(0f, 0f);

            var toggle = toggleObject.GetComponent<Toggle>();
            toggle.graphic = checkmarkImage;
            toggle.targetGraphic = backgroundImage;
            return toggle;
        }

        private static Button CreateButton(RectTransform parent, string name, string label, TMP_FontAsset font)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(buttonObject, "Create Main Menu Button");
            buttonObject.transform.SetParent(parent, false);
            ApplyUiLayer(buttonObject);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, 44f);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.18f, 0.2f, 0.28f, 1f);

            var layout = buttonObject.GetComponent<LayoutElement>();
            layout.preferredHeight = 44f;

            var text = CreateText(buttonObject.transform as RectTransform, "Label", label, font, 18, FontStyles.Bold);
            text.alignment = TextAlignmentOptions.Center;
            return buttonObject.GetComponent<Button>();
        }

        private static RectTransform CreateListRoot(RectTransform parent, string name)
        {
            var listObject = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            Undo.RegisterCreatedObjectUndo(listObject, "Create Main Menu List Root");
            listObject.transform.SetParent(parent, false);
            ApplyUiLayer(listObject);

            var layout = listObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            var fitter = listObject.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return listObject.GetComponent<RectTransform>();
        }

        private static Button CreateListEntryTemplate(RectTransform parent, string name, string label, TMP_FontAsset font)
        {
            var button = CreateButton(parent, name, label, font);
            return button;
        }

        private static RectTransform CreateDetailsPanel(RectTransform parent, string name)
        {
            var details = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
            Undo.RegisterCreatedObjectUndo(details, "Create Main Menu Details");
            details.transform.SetParent(parent, false);
            ApplyUiLayer(details);

            var layout = details.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            return details.GetComponent<RectTransform>();
        }

        private static TMP_Text CreateDetailLabel(RectTransform parent, string name, string text, TMP_FontAsset font)
        {
            var label = CreateText(parent, name, text, font, 16, FontStyles.Normal);
            label.alignment = TextAlignmentOptions.Left;
            return label;
        }

        private static RectTransform CreateMount(RectTransform parent, string name)
        {
            var mount = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(mount, "Create Character Creation Mount");
            mount.transform.SetParent(parent, false);
            ApplyUiLayer(mount);
            var rect = mount.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.sizeDelta = new Vector2(0f, 0f);
            return rect;
        }

        private static TMP_Text CreateText(RectTransform parent, string name, string text, TMP_FontAsset font, int fontSize, FontStyles style)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(label, "Create Main Menu Text");
            label.transform.SetParent(parent, false);
            ApplyUiLayer(label);

            var textComponent = label.GetComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.fontStyle = style;
            textComponent.color = new Color(0.9f, 0.92f, 0.98f, 1f);
            if (font != null)
            {
                textComponent.font = font;
            }

            var layout = label.GetComponent<LayoutElement>();
            layout.preferredHeight = fontSize + 16f;

            return textComponent;
        }

        private static void ApplyControllerBindings(
            MainMenuController controller,
            Canvas loginCanvas,
            TMP_Text loginMessage,
            TMP_InputField emailInput,
            TMP_InputField passwordInput,
            Toggle rememberToggle,
            Button loginButton,
            Button showCreateButton,
            Canvas createCanvas,
            TMP_Text createMessage,
            TMP_InputField createEmailInput,
            TMP_InputField createUsernameInput,
            TMP_InputField createPasswordInput,
            TMP_InputField createConfirmInput,
            Button createButton,
            Button backButton,
            Canvas realmCanvas,
            TMP_Text realmMessage,
            RectTransform realmListRoot,
            Button realmEntryTemplate,
            Button reloadRealmsButton,
            Canvas characterCanvas,
            RectTransform characterListRoot,
            TMP_InputField characterNameInput,
            Button createCharacterButton,
            TMP_Text characterMessage,
            TMP_Text createdAtLabel,
            TMP_Text raceLabel,
            TMP_Text classLabel,
            TMP_Text locationLabel,
            Button playButton,
            Button characterEntryTemplate,
            RectTransform creationMount)
        {
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("_loginCanvas").objectReferenceValue = loginCanvas;
            serialized.FindProperty("_loginMessage").objectReferenceValue = loginMessage;
            serialized.FindProperty("_emailInput").objectReferenceValue = emailInput;
            serialized.FindProperty("_passwordInput").objectReferenceValue = passwordInput;
            serialized.FindProperty("_rememberMeToggle").objectReferenceValue = rememberToggle;
            serialized.FindProperty("_loginButton").objectReferenceValue = loginButton;
            serialized.FindProperty("_showCreateAccountButton").objectReferenceValue = showCreateButton;
            serialized.FindProperty("_createAccountCanvas").objectReferenceValue = createCanvas;
            serialized.FindProperty("_createAccountMessage").objectReferenceValue = createMessage;
            serialized.FindProperty("_createEmailInput").objectReferenceValue = createEmailInput;
            serialized.FindProperty("_createUsernameInput").objectReferenceValue = createUsernameInput;
            serialized.FindProperty("_createPasswordInput").objectReferenceValue = createPasswordInput;
            serialized.FindProperty("_createConfirmPasswordInput").objectReferenceValue = createConfirmInput;
            serialized.FindProperty("_createAccountButton").objectReferenceValue = createButton;
            serialized.FindProperty("_backToLoginButton").objectReferenceValue = backButton;
            serialized.FindProperty("_realmCanvas").objectReferenceValue = realmCanvas;
            serialized.FindProperty("_realmListRoot").objectReferenceValue = realmListRoot;
            serialized.FindProperty("_reloadRealmsButton").objectReferenceValue = reloadRealmsButton;
            serialized.FindProperty("_realmMessage").objectReferenceValue = realmMessage;
            serialized.FindProperty("_realmEntryTemplate").objectReferenceValue = realmEntryTemplate;
            serialized.FindProperty("_characterCanvas").objectReferenceValue = characterCanvas;
            serialized.FindProperty("_characterListRoot").objectReferenceValue = characterListRoot;
            serialized.FindProperty("_characterNameInput").objectReferenceValue = characterNameInput;
            serialized.FindProperty("_createCharacterButton").objectReferenceValue = createCharacterButton;
            serialized.FindProperty("_characterMessage").objectReferenceValue = characterMessage;
            serialized.FindProperty("_characterCreatedAtLabel").objectReferenceValue = createdAtLabel;
            serialized.FindProperty("_characterRaceLabel").objectReferenceValue = raceLabel;
            serialized.FindProperty("_characterClassLabel").objectReferenceValue = classLabel;
            serialized.FindProperty("_characterLocationLabel").objectReferenceValue = locationLabel;
            serialized.FindProperty("_playCharacterButton").objectReferenceValue = playButton;
            serialized.FindProperty("_characterEntryTemplate").objectReferenceValue = characterEntryTemplate;
            serialized.FindProperty("_characterCreationPanelMount").objectReferenceValue = creationMount;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SavePrefab(GameObject root)
        {
            var directory = Path.GetDirectoryName(PrefabPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }

            PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.AutomatedAction);
        }

        private static void ApplyUiLayer(GameObject target)
        {
            var uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0)
            {
                target.layer = uiLayer;
            }
        }
    }
}
