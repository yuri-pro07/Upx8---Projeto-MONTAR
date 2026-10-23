using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;

namespace MontAR
{
    /// <summary>
    /// Navegação do app: apresentação → seleção de peças → início (código do participante e modo)
    /// → montagem → conclusão → histórico de montagens. Também decide qual câmera aparece (RA, bancada
    /// virtual ou só o fundo dos menus), verifica se o aparelho tem RA, pede a permissão da câmera antes
    /// de ligar a RA e oferece retomar uma montagem interrompida.
    /// </summary>
    public class AppController : MonoBehaviour
    {
        [Header("Fluxo")]
        [SerializeField] private StepFlowController flowController;
        [SerializeField] private PartCatalog catalog;

        [Header("Câmeras")]
        [SerializeField] private Camera arCamera;
        [Tooltip("Sessão do ARCore. Fica desligada na cena e só liga depois da permissão da câmera.")]
        [SerializeField] private ARSession arSession;
        [SerializeField] private DemoStage demoStage;

        [Header("Telas")]
        [SerializeField] private WelcomeScreen welcomeScreen;
        [SerializeField] private PartsScreen partsScreen;
        [SerializeField] private StartScreen startScreen;
        [SerializeField] private AssemblyScreen assemblyScreen;
        [SerializeField] private CompletionScreen completionScreen;
        [SerializeField] private HistoryScreen historyScreen;
        [SerializeField] private ConfirmDialog dialog;
        [SerializeField] private Toast toast;

        [Header("Diagnóstico da prova de conceito")]
        [Tooltip("Painel IMGUI da PoC. Fica desligado; o toque longo na versão (tela inicial) liga e desliga.")]
        [SerializeField] private PocDiagnosticsHud diagnosticsHud;
        [SerializeField] private LongPressButton diagnosticsToggle;

        [Header("Disponibilidade da RA")]
        [Tooltip("Tempo máximo esperando o ARCore responder antes de cair na demonstração 3D.")]
        [SerializeField] private float arCheckTimeoutSeconds = 8f;
        [Tooltip("Espera antes da tela de conclusão, para o aviso da última etapa aparecer.")]
        [SerializeField] private float completionDelaySeconds = 0.9f;

        private PartSelection selection;
        private SessionSummary summary;
        private AssemblyHistoryStore historyStore;
        private UiScreen current;
        private UiScreen historyReturn;
        private string lastHistorySessionId;
        private bool askingCamera;
        private GuidanceMode preferredMode = GuidanceMode.AugmentedReality;
        private bool arAvailable;
        private bool arChecked;
        private bool resumeOffered;
        private string lastParticipantCode = string.Empty;

        public UiScreen CurrentScreen => current;
        public bool IsArAvailable => arAvailable;

        /// <summary>A verificação do ARCore já terminou (com ou sem RA).</summary>
        public bool IsArChecked => arChecked;
        public PartSelection Selection => selection;

        /// <summary>Histórico de montagens deste aparelho.</summary>
        public AssemblyHistoryStore History => historyStore;

        private UiScreen[] Screens => new UiScreen[] { welcomeScreen, partsScreen, startScreen, assemblyScreen, completionScreen, historyScreen };

        private void Awake()
        {
            selection = catalog != null ? catalog.CreateDefaultSelection() : new PartSelection();
            historyStore = AssemblyHistoryStore.CreateDefault();
            // A sessão do ARCore pede a câmera ao ligar: só liga quando a permissão já existe.
            if (arSession != null && CameraPermission.IsGranted)
                arSession.enabled = true;

            welcomeScreen.Finished += OnWelcomeFinished;
            partsScreen.Continued += () => ShowStart();
            partsScreen.Back += () => ShowScreen(welcomeScreen);
            partsScreen.HistoryRequested += () => ShowHistory(partsScreen);
            startScreen.Back += () => ShowParts();
            startScreen.StartRequested += OnStartRequested;
            assemblyScreen.ExitRequested += ConfirmExit;
            assemblyScreen.ModeRequested += SetMode;
            completionScreen.NewSessionRequested += () =>
            {
                flowController.LeaveSession();
                ShowParts();
            };
            completionScreen.HistoryRequested += () =>
            {
                flowController.LeaveSession();
                ShowHistory(completionScreen);
            };
            historyScreen.Back += () => ShowScreen(historyReturn != null ? historyReturn : partsScreen);
            historyScreen.NewSessionRequested += ShowParts;

            assemblyScreen.Bind(flowController);
            flowController.FlowEventRaised += OnFlowEvent;

            if (diagnosticsHud != null)
                diagnosticsHud.enabled = false;
            if (diagnosticsToggle != null)
                diagnosticsToggle.OnLongPress.AddListener(ToggleDiagnostics);

            foreach (UiScreen screen in Screens)
                screen.gameObject.SetActive(false);
            dialog.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (flowController != null)
                flowController.FlowEventRaised -= OnFlowEvent;
            ARSession.stateChanged -= OnArSessionStateChanged;
        }

        private IEnumerator Start()
        {
            ShowScreen(welcomeScreen);
            OfferResume(null);
            yield return CheckArAvailability();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                OnBackPressed();
        }

        // ------------------------------------------------------------ navegação

        private void ShowScreen(UiScreen screen)
        {
            foreach (UiScreen other in Screens)
            {
                if (other != screen && other.IsVisible)
                    other.Hide();
            }
            current = screen;
            screen.Show();
            ApplyView();
        }

        private void ShowParts()
        {
            ShowScreen(partsScreen);
            partsScreen.Bind(catalog, selection, kit => flowController.StepsFor(kit).Count);
        }

        /// <summary>Histórico de montagens. <paramref name="returnTo"/> é para onde o "Voltar" leva.</summary>
        private void ShowHistory(UiScreen returnTo)
        {
            historyReturn = returnTo == completionScreen && flowController.HasSession ? partsScreen : returnTo;
            ShowScreen(historyScreen);
            historyScreen.Bind(historyStore.Load(), lastHistorySessionId);
        }

        private void ShowStart()
        {
            ShowScreen(startScreen);
            startScreen.Bind(flowController.StepsFor(selection), arAvailable, preferredMode, lastParticipantCode);
        }

        private void OnWelcomeFinished()
        {
            OfferResume(ShowParts);
            if (!dialog.IsOpen)
                ShowParts();
        }

        /// <summary>
        /// Se houver uma montagem interrompida, pergunta se é para continuar. Recusando,
        /// segue para <paramref name="otherwise"/> (ou fica na tela atual).
        /// </summary>
        private void OfferResume(System.Action otherwise)
        {
            if (resumeOffered || !flowController.TryGetResumableProgress(out SessionProgress saved))
                return;

            resumeOffered = true;

            int total = flowController.StepsFor(PartSelection.FromEntries(saved.parts)).Count;
            dialog.Show(
                "Continuar a montagem?",
                $"A montagem do participante <b>{saved.participantCode}</b> parou na etapa {saved.completedSteps + 1} de {total}. "
                + "As etapas já validadas ficaram salvas.",
                "Continuar",
                () => Resume(saved),
                "Começar outra",
                otherwise);
        }

        private void OnStartRequested(string participantCode, string label, GuidanceMode mode)
        {
            lastParticipantCode = participantCode;
            preferredMode = mode;
            WithCameraFor(mode, () => BeginAssembly(() => flowController.StartNewSession(participantCode, selection, label)));
        }

        private void Resume(SessionProgress saved)
        {
            selection = PartSelection.FromEntries(saved.parts);
            lastParticipantCode = saved.participantCode;
            WithCameraFor(arAvailable ? preferredMode : GuidanceMode.Demo3D, () => BeginAssembly(() => flowController.ResumeSession(saved)));
        }

        /// <summary>
        /// Antes de usar a RA, explica para que serve a câmera e abre o pedido do Android ("Durante o uso
        /// do app", "Só desta vez", "Não permitir"). Sem permissão, segue na demonstração 3D.
        /// </summary>
        private void WithCameraFor(GuidanceMode mode, System.Action then)
        {
            if (mode != GuidanceMode.AugmentedReality || !arAvailable || CameraPermission.IsGranted)
            {
                EnableArSession(mode == GuidanceMode.AugmentedReality && arAvailable);
                then();
                return;
            }
            if (askingCamera)
                return;

            askingCamera = true;
            dialog.Show(
                "Permitir o uso da câmera?",
                "A RA usa a câmera para achar o marcador A do tapete e mostrar a peça fantasma sobre a placa. "
                + "Nada é gravado nem enviado.\n\nNa próxima tela o Android pergunta: <b>Durante o uso do app</b>, "
                + "<b>Só desta vez</b> ou <b>Não permitir</b>.",
                "Continuar",
                () => CameraPermission.Request(result =>
                {
                    askingCamera = false;
                    if (result != CameraPermissionResult.Granted)
                    {
                        preferredMode = GuidanceMode.Demo3D;
                        flowController.SetGuidanceMode(GuidanceMode.Demo3D);
                        ShowCameraDenied(result);
                    }
                    EnableArSession(result == CameraPermissionResult.Granted);
                    then();
                }),
                "Usar o 3D",
                () =>
                {
                    askingCamera = false;
                    preferredMode = GuidanceMode.Demo3D;
                    flowController.SetGuidanceMode(GuidanceMode.Demo3D);
                    then();
                });
        }

        private void ShowCameraDenied(CameraPermissionResult result)
        {
            string message = result == CameraPermissionResult.DeniedPermanently
                ? "Câmera bloqueada. Para usar a RA, libere em Configurações › Apps › MontAR › Permissões. Seguindo na demonstração 3D."
                : "Sem a câmera, a montagem segue na demonstração 3D. Dá para tentar a RA de novo no botão Câmera.";
            if (current == assemblyScreen)
                assemblyScreen.ShowNotice(message, 4f);
            else
                toast.Show(message, 4f);
        }

        private void EnableArSession(bool enable)
        {
            if (arSession != null && enable && !arSession.enabled)
                arSession.enabled = true;
        }

        private void BeginAssembly(System.Action start)
        {
            if (flowController.HasSession)
                flowController.LeaveSession();

            summary = new SessionSummary(() => Time.realtimeSinceStartupAsDouble);
            bool canUseAr = arAvailable && CameraPermission.IsGranted;
            flowController.SetGuidanceMode(canUseAr ? preferredMode : GuidanceMode.Demo3D);
            assemblyScreen.SetArAvailable(arAvailable);
            start();
            ShowScreen(assemblyScreen);
        }

        private void ConfirmExit()
        {
            dialog.Show(
                "Sair da montagem?",
                "As etapas já validadas ficam salvas: dá para continuar depois de onde parou.",
                "Sair",
                () =>
                {
                    flowController.LeaveSession();
                    resumeOffered = false;
                    ShowScreen(welcomeScreen);
                },
                "Ficar");
        }

        private void OnBackPressed()
        {
            if (dialog.IsOpen)
            {
                dialog.Dismiss();
                return;
            }

            if (current == welcomeScreen)
                welcomeScreen.Previous();
            else if (current == historyScreen)
                ShowScreen(historyReturn != null ? historyReturn : partsScreen);
            else if (current == completionScreen)
                ShowHistory(completionScreen);
            else if (current == partsScreen)
                ShowScreen(welcomeScreen);
            else if (current == startScreen)
                ShowParts();
            else if (current == assemblyScreen)
            {
                if (assemblyScreen.IsValidating)
                    flowController.CancelValidation();
                else
                    ConfirmExit();
            }
        }

        // ------------------------------------------------------------ montagem

        private void OnFlowEvent(FlowEvent flowEvent)
        {
            summary?.Record(flowEvent);
            assemblyScreen.HandleFlowEvent(flowEvent);

            if (flowEvent.Type == FlowEventType.SessionCompleted)
            {
                SaveToHistory();
                StartCoroutine(ShowCompletionSoon());
            }
        }

        private void SaveToHistory()
        {
            SessionProgress progress = flowController.Progress;
            if (progress == null || summary == null)
                return;
            try
            {
                string kit = flowController.Selection != null ? flowController.Selection.Describe() : string.Empty;
                historyStore.Add(AssemblyHistoryStore.CreateEntry(progress, summary, flowController.ActiveSteps, flowController.Mode, kit));
                lastHistorySessionId = progress.sessionId;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[MontAR] Não deu para salvar no histórico: {e.Message}");
            }
        }

        private IEnumerator ShowCompletionSoon()
        {
            yield return new WaitForSecondsRealtime(completionDelaySeconds);
            SessionProgress progress = flowController.Progress;
            completionScreen.Bind(summary, flowController.ActiveSteps, progress != null ? progress.participantCode : "",
                flowController.MetricsFilePath, progress != null ? progress.label : "");
            ShowScreen(completionScreen);
        }

        private void SetMode(GuidanceMode mode)
        {
            if (mode == GuidanceMode.AugmentedReality && !arAvailable)
            {
                assemblyScreen.ShowNotice("A RA não está disponível neste aparelho. Continuando na demonstração 3D.");
                mode = GuidanceMode.Demo3D;
            }

            if (mode == GuidanceMode.AugmentedReality && !CameraPermission.IsGranted)
            {
                WithCameraFor(mode, () =>
                {
                    if (CameraPermission.IsGranted)
                        ApplyMode(GuidanceMode.AugmentedReality);
                });
                return;
            }

            ApplyMode(mode);
        }

        private void ApplyMode(GuidanceMode mode)
        {
            EnableArSession(mode == GuidanceMode.AugmentedReality);
            preferredMode = mode;
            flowController.SetGuidanceMode(mode);
            ApplyView();
            assemblyScreen.Refresh();
        }

        /// <summary>
        /// Câmera da RA só na montagem em modo RA. Na demonstração 3D, a câmera da bancada virtual.
        /// Nos menus, a câmera da bancada só limpa o fundo (bancada escondida).
        /// </summary>
        private void ApplyView()
        {
            bool assembling = current == assemblyScreen;
            bool demo = assembling && flowController.Mode == GuidanceMode.Demo3D;
            bool ar = assembling && !demo;

            if (arCamera != null)
                arCamera.enabled = ar;
            if (demoStage != null)
            {
                demoStage.SetVisible(demo);
                if (demoStage.StageCamera != null)
                    demoStage.StageCamera.enabled = !ar;
            }
        }

        // ------------------------------------------------------------ RA

        private IEnumerator CheckArAvailability()
        {
            if (ARSession.state == ARSessionState.None || ARSession.state == ARSessionState.CheckingAvailability)
                yield return ARSession.CheckAvailability();

            float waited = 0f;
            while (IsPending(ARSession.state) && waited < arCheckTimeoutSeconds)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            UpdateArAvailability(ARSession.state);
            ARSession.stateChanged += OnArSessionStateChanged;
        }

        private static bool IsPending(ARSessionState state)
        {
            return state == ARSessionState.None || state == ARSessionState.CheckingAvailability || state == ARSessionState.Installing;
        }

        private void OnArSessionStateChanged(ARSessionStateChangedEventArgs args)
        {
            UpdateArAvailability(args.state);
        }

        private void UpdateArAvailability(ARSessionState state)
        {
            // NeedsInstall/Installing contam como disponível: o ARCore pede a instalação sozinho.
            bool available = state >= ARSessionState.NeedsInstall;
            if (arChecked && available == arAvailable)
                return;

            arChecked = true;
            arAvailable = available;
            Debug.Log($"[MontAR] RA {(arAvailable ? "disponível" : "indisponível")} (ARSession: {state}).");
            startScreen.SetArAvailable(arAvailable);
            assemblyScreen.SetArAvailable(arAvailable);

            if (!arAvailable && flowController.Mode == GuidanceMode.AugmentedReality && flowController.HasSession)
                SetMode(GuidanceMode.Demo3D);
        }

        private void ToggleDiagnostics()
        {
            if (diagnosticsHud == null)
                return;
            diagnosticsHud.enabled = !diagnosticsHud.enabled;
            toast.Show(diagnosticsHud.enabled ? "Painel de diagnóstico ligado" : "Painel de diagnóstico desligado", 1.6f);
        }
    }
}
