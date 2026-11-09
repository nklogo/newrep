using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Makes an electric screwdriver spin its chuck/bit and play a working sound effect
/// while grabbed in VR and its controller trigger (Activate) is engaged.
/// Rotates only the active head transform around its configured local axis (never the tool body).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(XRGrabInteractable))]
public class XRElectricScrewdriver : MonoBehaviour
{
    [Header("Rotation")]
    [Tooltip("Active screwdriver head/bit transform that rotates when the trigger is engaged (defaults to Driver_Bit). Never assign the root screwdriver body.")]
    [SerializeField] private Transform rotatingPart;

    [Tooltip("Built-in native Driver_Bit on Chuck_Nose_Assembly used whenever no external interchangeable head is seated.")]
    [SerializeField] private Transform builtInDriverBit;

    [Tooltip("Optional Chuck_Nose_Assembly transform that co-rotates visually around local +Y alongside the active head.")]
    [SerializeField] private Transform chuckVisual;

    [Tooltip("Local axis around which the active head spins.")]
    [SerializeField] private Vector3 localRotationAxis = Vector3.up;

    [Tooltip("Maximum rotation speed in degrees per second when the trigger is engaged.")]
    [SerializeField] private float rotationSpeed = 1440f;

    [Tooltip("Reverse the rotation direction.")]
    [SerializeField] private bool reverseDirection = false;

    [Tooltip("Scale rotation speed and sound pitch smoothly with analog VR trigger squeeze when available.")]
    [SerializeField] private bool analogTriggerSpeed = true;

    [Header("Trigger & Switch Visuals (Optional)")]
    [Tooltip("Optional Trigger_Switch mesh transform that depresses when the VR trigger is pulled.")]
    [SerializeField] private Transform triggerSwitch;

    [Tooltip("Local position offset applied to Trigger_Switch when fully pressed (calibrated for 1:1 metric screwdriver model).")]
    [SerializeField] private Vector3 triggerPressedLocalOffset = new Vector3(0f, -0.0055f, 0f);

    [Tooltip("Optional Reversing_Slide_Switch mesh transform that shifts when reverse direction is active.")]
    [SerializeField] private Transform reversingSwitch;

    [Tooltip("Local position offset applied to Reversing_Slide_Switch when reverseDirection is true (calibrated for 1:1 metric model).")]
    [SerializeField] private Vector3 reverseSwitchLocalOffset = new Vector3(0.008f, 0f, 0f);

    [Header("Sound Effect")]
    [Tooltip("Looping sound effect played while the screwdriver is working.")]
    [SerializeField] private AudioClip workingSoundClip;

    [Tooltip("Optional one-shot sound played when the trigger is first pressed.")]
    [SerializeField] private AudioClip startSoundClip;

    [Tooltip("Optional one-shot sound played when the trigger is released.")]
    [SerializeField] private AudioClip stopSoundClip;

    [Tooltip("AudioSource used to play the sound effect (auto-uses AudioSource on this GameObject if left empty).")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Playback volume for the screwdriver sound effects.")]
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 0.85f;

    [Tooltip("Pitch range (min to max) mapped to analog trigger pressure while running.")]
    [SerializeField] private Vector2 pitchRange = new Vector2(0.9f, 1.05f);

    [Header("Reverse Input (VR Controller)")]
    [Tooltip("Optional InputActionReference to toggle reverse rotation while holding the screwdriver. Defaults to VR controller SecondaryButton (B/Y) if left empty.")]
    [SerializeField] private InputActionReference reverseToggleActionRef;

    [Header("XR Simulator Keyboard Switch (Testing)")]
    [Tooltip("Testing shortcut for XR Simulator: first Tab press runs Clockwise (Tighten), next Tab press rotates Anti-Clockwise (Loosen), toggling on each press.")]
    [SerializeField] private bool enableTabKeyboardSwitch = true;

    [Tooltip("Maximum visual chuck/socket spin speed (deg/s) to avoid stroboscopic wagon-wheel reversal on 60Hz/120Hz displays.")]
    [SerializeField] private float visualHeadSpinMaxSpeed = 420f;

    [Header("Inspector Testing (No VR Headset Required)")]
    [Tooltip("Inspector-only test toggle: check during Play Mode to run the motor without needing a VR controller trigger.")]
    [SerializeField] private bool editorTestRunMotor = false;

    private XRGrabInteractable _grabInteractable;
    private IXRSelectInteractor _selectingInteractor;
    private XRBaseInputInteractor _inputInteractor;
    private InputAction _fallbackReverseAction;
    private UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRInteractionSimulator _boundSimulator;
    private XRInputButtonReader.InputSourceMode _savedSimCycleDevicesMode;
    private bool _hasOverriddenSimTabCycle;
    private bool _hasUsedTabSwitch;
    private bool _isTabSpinActive;
    private Vector3 _triggerInitialLocalPos;
    private bool _hasTriggerInitialPos;
    private Vector3 _reverseSwitchInitialLocalPos;
    private bool _hasReverseSwitchInitialPos;
    private bool _isTriggerActivated;
    private bool _lastEditorTestState;
    private bool _lastReverseState;
    private bool _isWorking;

    /// <summary>Raised when the motor starts (true) or stops (false).</summary>
    public event Action<bool> MotorStateChanged;

    /// <summary>Raised when rotation direction changes (true = reverse, false = forward).</summary>
    public event Action<bool> DirectionChanged;

    public bool IsWorking => _isWorking;

    public bool IsHeld => _grabInteractable != null && _grabInteractable.isSelected;

    public bool EditorTestRunMotor => editorTestRunMotor;

    public bool IsTabSpinActive => _isTabSpinActive;

    public Transform ActiveHeadTransform => rotatingPart;

    public Transform BuiltInDriverBit => builtInDriverBit;

    public Vector3 ActiveLocalRotationAxis =>
        localRotationAxis.sqrMagnitude > Mathf.Epsilon ? localRotationAxis.normalized : Vector3.up;

    public float MaxRotationSpeed => rotationSpeed;

    public float CurrentSignedAngularSpeed { get; private set; }

    public float LastFrameSignedDegrees { get; private set; }

    /// <summary>
    /// Set by ScrewTighteningController while engaged with a compatible aligned fastener so
    /// bit spin, fastener spin, and torque-stop stalling stay strictly synchronized.
    /// </summary>
    public bool SuppressAutoHeadSpin { get; set; }

    public bool ReverseDirection
    {
        get => reverseDirection;
        set => SetReverseDirection(value);
    }

    public AudioClip WorkingSoundClip
    {
        get => workingSoundClip;
        set => workingSoundClip = value;
    }

    private void Reset()
    {
        AutoAssignReferences();
    }

    private void Awake()
    {
        _grabInteractable = GetComponent<XRGrabInteractable>();
        AutoAssignReferences();
        SanitizeRotatingPart();
        CacheVisualOrigins();
        UpdateReverseSwitchVisual();
        ConfigureAudioSource();
        _lastReverseState = reverseDirection;
        _lastEditorTestState = editorTestRunMotor;
    }

    private void OnValidate()
    {
        SanitizeRotatingPart();
    }

    private void OnEnable()
    {
        if (_grabInteractable == null)
            _grabInteractable = GetComponent<XRGrabInteractable>();

        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.AddListener(OnSelectEntered);
            _grabInteractable.selectExited.AddListener(OnSelectExited);
            _grabInteractable.activated.AddListener(OnActivated);
            _grabInteractable.deactivated.AddListener(OnDeactivated);

            if (_grabInteractable.isSelected)
            {
                _selectingInteractor = _grabInteractable.firstInteractorSelecting;
                _inputInteractor = _selectingInteractor as XRBaseInputInteractor;
                EnableReverseInputListener(true);
            }
        }
    }

    private void OnDisable()
    {
        EnableReverseInputListener(false);
        RestoreSimulatorTabDeviceCycle();

        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            _grabInteractable.selectExited.RemoveListener(OnSelectExited);
            _grabInteractable.activated.RemoveListener(OnActivated);
            _grabInteractable.deactivated.RemoveListener(OnDeactivated);
        }

        _isTriggerActivated = false;
        _isTabSpinActive = false;
        _hasUsedTabSwitch = false;
        editorTestRunMotor = false;
        _lastEditorTestState = false;
        StopWorking(playStopClip: false);
        ResetTriggerVisual();
        _selectingInteractor = null;
        _inputInteractor = null;
    }

    private void Update()
    {
        if (enableTabKeyboardSwitch && Application.isPlaying)
        {
            SuppressSimulatorTabDeviceCycle();
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                HandleTabKeyboardSwitchPressed();
            }
        }
        else if (_hasOverriddenSimTabCycle)
        {
            RestoreSimulatorTabDeviceCycle();
        }

        if (_lastReverseState != reverseDirection)
        {
            _lastReverseState = reverseDirection;
            UpdateReverseSwitchVisual();
            DirectionChanged?.Invoke(reverseDirection);
        }

        if (_lastEditorTestState != editorTestRunMotor)
        {
            _lastEditorTestState = editorTestRunMotor;
            if (editorTestRunMotor)
                StartWorking();
            else if (!_isTriggerActivated && !_isTabSpinActive)
                StopWorking(playStopClip: true);
        }

        if (!_isWorking)
        {
            CurrentSignedAngularSpeed = 0f;
            LastFrameSignedDegrees = 0f;
            return;
        }

        float throttle = GetTriggerThrottle();
        RotateActiveHead(throttle);
        UpdateTriggerVisual(throttle);
        UpdateWorkingSound(throttle);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        _selectingInteractor = args.interactorObject;
        _inputInteractor = args.interactorObject as XRBaseInputInteractor;
        EnableReverseInputListener(true);
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (!_grabInteractable.isSelected || args.interactorObject == _selectingInteractor)
        {
            EnableReverseInputListener(false);
            _selectingInteractor = null;
            _inputInteractor = null;
            _isTriggerActivated = false;
            if (!editorTestRunMotor && !_isTabSpinActive)
            {
                StopWorking(playStopClip: true);
                ResetTriggerVisual();
            }
        }
    }

    private void OnActivated(ActivateEventArgs args)
    {
        if (!_grabInteractable.isSelected)
            return;

        _isTriggerActivated = true;
        StartWorking();
    }

    private void OnDeactivated(DeactivateEventArgs args)
    {
        _isTriggerActivated = false;
        if (!editorTestRunMotor && !_isTabSpinActive)
        {
            StopWorking(playStopClip: true);
            ResetTriggerVisual();
        }
    }

    /// <summary>
    /// Assigns the currently installed screwdriver head and its local rotation axis.
    /// Never allows the root screwdriver body to become the rotating transform.
    /// </summary>
    public void SetActiveHead(Transform headTransform, Vector3 headLocalAxis)
    {
        if (headTransform == transform)
        {
            Debug.LogWarning("[XRElectricScrewdriver] Refusing to assign root screwdriver body as ActiveHeadTransform.", this);
            rotatingPart = builtInDriverBit;
            return;
        }

        rotatingPart = headTransform != null ? headTransform : builtInDriverBit;
        if (headLocalAxis.sqrMagnitude > Mathf.Epsilon)
            localRotationAxis = headLocalAxis.normalized;

        if (builtInDriverBit != null)
        {
            MeshRenderer mr = builtInDriverBit.GetComponent<MeshRenderer>();
            if (mr != null)
                mr.enabled = (rotatingPart == builtInDriverBit);
        }
    }

    /// <summary>
    /// Clears any external interchangeable head reference and restores the built-in native Impact_Hex_Socket / Driver_Bit.
    /// </summary>
    public void ClearActiveHead()
    {
        if (builtInDriverBit == null)
            builtInDriverBit = FindBuiltInHeadTransform();

        if (builtInDriverBit != null)
        {
            MeshRenderer mr = builtInDriverBit.GetComponent<MeshRenderer>();
            if (mr != null)
                mr.enabled = true;
            rotatingPart = builtInDriverBit;
            localRotationAxis = Vector3.up;
        }
        else
        {
            rotatingPart = null;
        }

        CurrentSignedAngularSpeed = 0f;
        LastFrameSignedDegrees = 0f;
    }

    /// <summary>
    /// Sends a haptic impulse to the VR controller currently holding this impact wrench (no-op if unsupported).
    /// </summary>
    public void SendHapticPulse(float amplitude, float duration)
    {
        if (_inputInteractor != null && amplitude > 0.001f && duration > 0.001f)
            _inputInteractor.SendHapticImpulse(Mathf.Clamp01(amplitude), duration);
    }

    /// <summary>
    /// Synchronously spins the active head (and optional chuck visual) by a specified logical signed degree delta
    /// (+degrees = Clockwise/Tighten, -degrees = Anti-Clockwise/Loosen).
    /// Never rotates the root screwdriver body.
    /// </summary>
    public void RotateHeadExternalStep(float signedDegrees)
    {
        if (Mathf.Abs(signedDegrees) < 1e-5f)
            return;

        ApplyVisualHeadRotation(signedDegrees);
    }

    /// <summary>
    /// Starts rotating the active screwdriver head and playing its working sound effect.
    /// </summary>
    [ContextMenu("Test: Start Motor")]
    public void StartWorking()
    {
        if (_isWorking)
            return;

        _isWorking = true;

        if (audioSource != null && (IsHeld || editorTestRunMotor))
        {
            if (startSoundClip != null)
                audioSource.PlayOneShot(startSoundClip, soundVolume);

            PlayLoopingWorkingSound(GetTriggerThrottle());
        }

        MotorStateChanged?.Invoke(true);
    }

    /// <summary>
    /// Stops rotating the screwdriver head immediately and stops the working sound effect.
    /// </summary>
    public void StopWorking(bool playStopClip = true)
    {
        CurrentSignedAngularSpeed = 0f;
        LastFrameSignedDegrees = 0f;

        if (!_isWorking)
            return;

        _isWorking = false;

        if (audioSource != null)
        {
            if (audioSource.isPlaying)
                audioSource.Stop();

            audioSource.pitch = 1f;

            if (playStopClip && stopSoundClip != null)
                audioSource.PlayOneShot(stopSoundClip, soundVolume);
        }

        MotorStateChanged?.Invoke(false);
    }

    [ContextMenu("Test: Stop Motor")]
    private void ContextMenuStopMotor()
    {
        editorTestRunMotor = false;
        _lastEditorTestState = false;
        _isTabSpinActive = false;
        StopWorking(playStopClip: true);
        ResetTriggerVisual();
    }

    /// <summary>
    /// Sets clockwise (false) or counter-clockwise/reverse (true) rotation direction.
    /// </summary>
    public void SetReverseDirection(bool reverse, bool forceNotify = false)
    {
        if (reverseDirection == reverse && !forceNotify)
            return;

        reverseDirection = reverse;
        _lastReverseState = reverseDirection;
        UpdateReverseSwitchVisual();
        DirectionChanged?.Invoke(reverseDirection);
    }

    /// <summary>
    /// Toggles clockwise / counter-clockwise rotation direction.
    /// </summary>
    [ContextMenu("Test: Toggle Reverse Direction")]
    public void ToggleDirection()
    {
        SetReverseDirection(!reverseDirection);
    }

    private void HandleTabKeyboardSwitchPressed()
    {
        // First Tab press explicitly runs Clockwise (Tighten, reverse = false);
        // next Tab press starts rotating Anti-Clockwise (Loosen, reverse = true), toggling on every press.
        bool targetReverse = !_hasUsedTabSwitch ? false : !reverseDirection;
        _hasUsedTabSwitch = true;
        _isTabSpinActive = true;

        SetReverseDirection(targetReverse, forceNotify: true);
        if (!_isWorking)
            StartWorking();
    }

    private void SuppressSimulatorTabDeviceCycle()
    {
        var sim = UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRInteractionSimulator.instance;
        if (sim == null || sim.cycleDevicesInput == null)
            return;

        if (_boundSimulator != sim || !_hasOverriddenSimTabCycle)
        {
            _boundSimulator = sim;
            _savedSimCycleDevicesMode = sim.cycleDevicesInput.inputSourceMode;
            _hasOverriddenSimTabCycle = true;
        }

        if (sim.cycleDevicesInput.inputSourceMode != XRInputButtonReader.InputSourceMode.Unused)
            sim.cycleDevicesInput.inputSourceMode = XRInputButtonReader.InputSourceMode.Unused;
    }

    private void RestoreSimulatorTabDeviceCycle()
    {
        if (_hasOverriddenSimTabCycle && _boundSimulator != null && _boundSimulator.cycleDevicesInput != null)
            _boundSimulator.cycleDevicesInput.inputSourceMode = _savedSimCycleDevicesMode;

        _hasOverriddenSimTabCycle = false;
        _boundSimulator = null;
    }

    private float GetTriggerThrottle()
    {
        if (_isTabSpinActive && !_isTriggerActivated)
            return 1f;

        if (analogTriggerSpeed && _inputInteractor != null &&
            _inputInteractor.activateInput.TryReadValue(out float rawValue) &&
            rawValue > 0.01f)
        {
            return Mathf.Clamp01(rawValue);
        }

        return 1f;
    }

    /// <summary>
    /// Returns the instantaneous signed angular speed (deg/s) based on current motor state, trigger throttle, and reverse setting.
    /// </summary>
    public float GetLiveSignedAngularSpeed()
    {
        if (!_isWorking || rotatingPart == null || rotatingPart == transform)
            return 0f;

        float throttle = GetTriggerThrottle();
        float directionSign = reverseDirection ? -1f : 1f;
        return rotationSpeed * throttle * directionSign;
    }

    private void RotateActiveHead(float throttle)
    {
        // Strictly rotate ONLY the active head/chuck — never fall back to the root screwdriver body.
        if (rotatingPart == null || rotatingPart == transform)
        {
            CurrentSignedAngularSpeed = 0f;
            LastFrameSignedDegrees = 0f;
            return;
        }

        float directionSign = reverseDirection ? -1f : 1f;
        CurrentSignedAngularSpeed = rotationSpeed * throttle * directionSign;

        if (SuppressAutoHeadSpin)
        {
            LastFrameSignedDegrees = 0f;
            return;
        }

        LastFrameSignedDegrees = CurrentSignedAngularSpeed * Time.deltaTime;
        ApplyVisualHeadRotation(LastFrameSignedDegrees);
    }

    private void ApplyVisualHeadRotation(float logicalSignedDegrees)
    {
        if (rotatingPart == null || rotatingPart == transform || Mathf.Abs(logicalSignedDegrees) < 1e-5f)
            return;

        // Clamp visual per-frame step to avoid 60Hz/120Hz wagon-wheel stroboscopic reversal on ribbed chuck & hex socket.
        float dt = Mathf.Max(Time.deltaTime, 1f / 240f);
        float maxVisualStep = Mathf.Max(120f, visualHeadSpinMaxSpeed) * dt;
        float visualMagnitude = Mathf.Min(Mathf.Abs(logicalSignedDegrees), maxVisualStep);

        // In Unity's Quaternion convention, looking forward from the screwdriver grip along local +Y toward the screw:
        // -degrees around +Y rotates 12 o'clock -> 3 o'clock (CLOCKWISE, Tighten: logicalSignedDegrees > 0),
        // +degrees around +Y rotates 12 o'clock -> 9 o'clock (ANTI-CLOCKWISE, Loosen: logicalSignedDegrees < 0).
        float visualSpinDegrees = logicalSignedDegrees >= 0f ? -visualMagnitude : visualMagnitude;

        if (chuckVisual != null && chuckVisual != transform && rotatingPart.IsChildOf(chuckVisual))
        {
            // Rotate Chuck_Nose_Assembly (ribbed grip, 3 jaws, driver bit, and child Impact_Hex_Socket) as one solid unit.
            chuckVisual.Rotate(Vector3.up, visualSpinDegrees, Space.Self);
        }
        else
        {
            rotatingPart.Rotate(ActiveLocalRotationAxis, visualSpinDegrees, Space.Self);
            if (chuckVisual != null && chuckVisual != transform && chuckVisual != rotatingPart)
                chuckVisual.Rotate(Vector3.up, visualSpinDegrees, Space.Self);
        }
    }

    private void UpdateTriggerVisual(float throttle)
    {
        if (triggerSwitch == null)
            return;

        if (!_hasTriggerInitialPos)
            CacheVisualOrigins();

        triggerSwitch.localPosition = _triggerInitialLocalPos + triggerPressedLocalOffset * throttle;
    }

    private void ResetTriggerVisual()
    {
        if (triggerSwitch != null && _hasTriggerInitialPos)
            triggerSwitch.localPosition = _triggerInitialLocalPos;
    }

    private void UpdateReverseSwitchVisual()
    {
        if (reversingSwitch == null)
            return;

        if (!_hasReverseSwitchInitialPos)
            CacheVisualOrigins();

        reversingSwitch.localPosition = reverseDirection
            ? _reverseSwitchInitialLocalPos + reverseSwitchLocalOffset
            : _reverseSwitchInitialLocalPos;
    }

    private void PlayLoopingWorkingSound(float throttle)
    {
        if (audioSource == null)
            return;

        if (workingSoundClip != null && audioSource.clip != workingSoundClip)
            audioSource.clip = workingSoundClip;

        if (audioSource.clip == null && audioSource.resource == null)
            return;

        audioSource.loop = true;
        audioSource.volume = soundVolume;
        audioSource.pitch = analogTriggerSpeed
            ? Mathf.Lerp(pitchRange.x, pitchRange.y, throttle)
            : 1f;

        if (!audioSource.isPlaying)
            audioSource.Play();
    }

    private void UpdateWorkingSound(float throttle)
    {
        if (audioSource == null)
            return;

        if (_isTabSpinActive && !IsHeld && !editorTestRunMotor)
        {
            if (audioSource.isPlaying)
                audioSource.Stop();
            return;
        }

        if (workingSoundClip != null && audioSource.clip != workingSoundClip)
            audioSource.clip = workingSoundClip;

        if (audioSource.clip == null && audioSource.resource == null)
        {
            if (audioSource.isPlaying)
                audioSource.Stop();
            return;
        }

        audioSource.volume = soundVolume;
        audioSource.pitch = analogTriggerSpeed
            ? Mathf.Lerp(pitchRange.x, pitchRange.y, throttle)
            : 1f;

        if (!audioSource.isPlaying)
        {
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    private void SanitizeRotatingPart()
    {
        if (rotatingPart == transform)
        {
            Debug.LogWarning("[XRElectricScrewdriver] rotatingPart cannot be the root screwdriver body; clearing reference.", this);
            rotatingPart = null;
        }
    }

    private Transform FindChildFlexible(string relativePath)
    {
        Transform found = transform.Find(relativePath);
        if (found != null)
            return found;

        Transform rig = transform.Find("WrenchVisualRig");
        if (rig != null)
        {
            found = rig.Find(relativePath);
            if (found != null)
                return found;
        }
        return null;
    }

    private Transform FindBuiltInHeadTransform()
    {
        Transform impactSocket = FindChildFlexible("Chuck_Nose_Assembly/Impact_Hex_Socket");
        if (impactSocket != null)
            return impactSocket;

        Transform bit = FindChildFlexible("Chuck_Nose_Assembly/Driver_Bit");
        if (bit == null)
            bit = FindChildFlexible("Driver_Bit");

        return bit;
    }

    private void AutoAssignReferences()
    {
        if (builtInDriverBit == null)
            builtInDriverBit = FindBuiltInHeadTransform();

        if (rotatingPart == null || rotatingPart == transform)
            rotatingPart = builtInDriverBit;

        if (chuckVisual == null)
            chuckVisual = FindChildFlexible("Chuck_Nose_Assembly");

        if (triggerSwitch == null)
            triggerSwitch = FindChildFlexible("Trigger_Switch");

        if (reversingSwitch == null)
            reversingSwitch = FindChildFlexible("Reversing_Slide_Switch");

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void CacheVisualOrigins()
    {
        if (triggerSwitch != null && !_hasTriggerInitialPos)
        {
            _triggerInitialLocalPos = triggerSwitch.localPosition;
            _hasTriggerInitialPos = true;
        }

        if (reversingSwitch != null && !_hasReverseSwitchInitialPos)
        {
            _reverseSwitchInitialLocalPos = reversingSwitch.localPosition;
            _hasReverseSwitchInitialPos = true;
        }
    }

    private void ConfigureAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        if (audioSource.spatialBlend < 0.01f)
            audioSource.spatialBlend = 1f;
    }

    private void EnableReverseInputListener(bool enable)
    {
        InputAction action = reverseToggleActionRef != null ? reverseToggleActionRef.action : GetOrCreateFallbackReverseAction();
        if (action == null)
            return;

        action.performed -= OnReverseActionPerformed;
        if (enable)
        {
            action.performed += OnReverseActionPerformed;
            if (!action.enabled)
                action.Enable();
        }
        else if (reverseToggleActionRef == null && _fallbackReverseAction != null && _fallbackReverseAction.enabled)
        {
            _fallbackReverseAction.Disable();
        }
    }

    private InputAction GetOrCreateFallbackReverseAction()
    {
        if (_fallbackReverseAction == null)
        {
            // Binds to VR controller SecondaryButton (B / Y) and PrimaryButton (A / X) while holding the wrench in a real VR headset.
            // (Keyboard Tab for XR Simulator testing is handled directly in Update so Press #1 = CW Tighten and Press #2 = CCW Loosen.)
            _fallbackReverseAction = new InputAction(
                name: "ScrewdriverToggleReverse",
                type: InputActionType.Button);
            _fallbackReverseAction.AddBinding("<XRController>/secondaryButton");
            _fallbackReverseAction.AddBinding("<XRController>/primaryButton");
        }
        return _fallbackReverseAction;
    }

    private void OnReverseActionPerformed(InputAction.CallbackContext ctx)
    {
        if (IsHeld)
            ToggleDirection();
    }

    private void OnDestroy()
    {
        if (_fallbackReverseAction != null)
        {
            _fallbackReverseAction.performed -= OnReverseActionPerformed;
            _fallbackReverseAction.Dispose();
            _fallbackReverseAction = null;
        }
    }
}
