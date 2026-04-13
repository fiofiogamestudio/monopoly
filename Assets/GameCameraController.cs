using UnityEngine;

[DefaultExecutionOrder(120)]
public class GameCameraController : MonoBehaviour
{
    [Header("Refs")]
    public Camera targetCamera;
    public GameManager gameManager;
    public PlayerManager playerManager;
    public Transform mapRoot;

    [Header("Zoom")]
    [Min(1f)] public float maxZoomMultiplier = 4f;
    [Range(0.05f, 1f)] public float zoomStepRatio = 0.18f;
    [Min(1f)] public float zoomSmoothSpeed = 12f;
    [Range(0f, 0.5f)] public float autoFollowZoomThreshold = 0.02f;

    [Header("Move")]
    [Min(0.1f)] public float focusMoveSpeed = 8.5f;
    [Min(0.1f)] public float movingFollowBoost = 1.35f;
    [Min(0.1f)] public float turnSwitchMoveSpeed = 11f;
    [Min(0.1f)] public float manualPanSpeed = 8f;
    [Min(0f)] public float mapClampPadding = 0.75f;
    public bool resetPanOffsetOnTurnSwitch = true;

    private Camera _cachedCamera;
    private Quaternion _initialRotation;
    private Vector3 _initialPosition;
    private float _maxOrthographicSize;
    private float _minOrthographicSize;
    private float _targetOrthographicSize;
    private int _lastFocusedPlayer = -1;
    private int _lastKnownMapSlotCount = -1;
    private Vector2 _manualPanOffset;
    private Vector2 _mapMin;
    private Vector2 _mapMax;
    private float _focusPlaneY;
    private Vector3 _cameraToFocusOffset;
    private bool _boundsReady;

    private void Awake()
    {
        ResolveReferences();
        CacheCameraState();
    }

    private void Start()
    {
        ResolveReferences();
        CacheCameraState();
        RecalculateMapBounds();
        RefreshFocusOffset();
        SnapToCurrentView();
    }

    private void LateUpdate()
    {
        ResolveReferences();
        CacheCameraState();
        if (_cachedCamera == null)
        {
            return;
        }

        if (MapSlotCountChanged())
        {
            RecalculateMapBounds();
            RefreshFocusOffset();
        }

        UpdateZoom();
        UpdateFocusAndPan();
    }

    private void ResolveReferences()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }

        if (playerManager == null)
        {
            playerManager = FindObjectOfType<PlayerManager>();
        }

        if (mapRoot == null)
        {
            if (playerManager != null && playerManager.mapRoot != null)
            {
                mapRoot = playerManager.mapRoot;
            }
            else if (gameManager != null && gameManager.playerManager != null)
            {
                mapRoot = gameManager.playerManager.mapRoot;
            }
        }
    }

    private void CacheCameraState()
    {
        if (targetCamera == null)
        {
            return;
        }

        if (_cachedCamera == targetCamera)
        {
            return;
        }

        _cachedCamera = targetCamera;
        _initialRotation = transform.rotation;
        _initialPosition = transform.position;

        if (_cachedCamera.orthographic)
        {
            _maxOrthographicSize = Mathf.Max(0.01f, _cachedCamera.orthographicSize);
            _minOrthographicSize = Mathf.Max(0.01f, _maxOrthographicSize / Mathf.Max(1f, maxZoomMultiplier));
            _targetOrthographicSize = Mathf.Clamp(_cachedCamera.orthographicSize, _minOrthographicSize, _maxOrthographicSize);
        }
    }

    private bool MapSlotCountChanged()
    {
        int slotCount = mapRoot != null ? mapRoot.childCount : -1;
        if (slotCount == _lastKnownMapSlotCount)
        {
            return false;
        }

        _lastKnownMapSlotCount = slotCount;
        return true;
    }

    private void RecalculateMapBounds()
    {
        if (mapRoot == null || mapRoot.childCount <= 0)
        {
            _boundsReady = false;
            return;
        }

        Vector3 firstPoint = mapRoot.GetChild(0).position;
        float minX = firstPoint.x;
        float maxX = firstPoint.x;
        float minZ = firstPoint.z;
        float maxZ = firstPoint.z;

        for (int i = 1; i < mapRoot.childCount; i++)
        {
            Vector3 point = mapRoot.GetChild(i).position;
            minX = Mathf.Min(minX, point.x);
            maxX = Mathf.Max(maxX, point.x);
            minZ = Mathf.Min(minZ, point.z);
            maxZ = Mathf.Max(maxZ, point.z);
        }

        _mapMin = new Vector2(minX, minZ);
        _mapMax = new Vector2(maxX, maxZ);
        _boundsReady = true;
        _focusPlaneY = firstPoint.y;
    }

    private void RefreshFocusOffset()
    {
        Vector3 initialFocusPoint = GetInitialFocusPoint();
        _cameraToFocusOffset = _initialPosition - initialFocusPoint;
    }

    private void SnapToCurrentView()
    {
        if (_cachedCamera == null)
        {
            return;
        }

        transform.rotation = _initialRotation;
        if (_cachedCamera.orthographic)
        {
            _cachedCamera.orthographicSize = _targetOrthographicSize;
        }

        Vector3 position = _initialPosition;
        transform.position = ClampWorldPosition(position);
    }

    private void UpdateZoom()
    {
        if (_cachedCamera == null || !_cachedCamera.orthographic)
        {
            return;
        }

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            float zoomStep = _maxOrthographicSize * zoomStepRatio;
            _targetOrthographicSize = Mathf.Clamp(
                _targetOrthographicSize - scroll * zoomStep,
                _minOrthographicSize,
                _maxOrthographicSize);
        }

        _cachedCamera.orthographicSize = Mathf.Lerp(
            _cachedCamera.orthographicSize,
            _targetOrthographicSize,
            1f - Mathf.Exp(-zoomSmoothSpeed * Time.deltaTime));
    }

    private void UpdateFocusAndPan()
    {
        bool autoFollowEnabled = IsAutoFollowEnabled();
        int currentPlayer = gameManager != null ? gameManager.currentPlayerIndex : -1;
        bool turnSwitched = currentPlayer != _lastFocusedPlayer;

        if (turnSwitched)
        {
            if (autoFollowEnabled && resetPanOffsetOnTurnSwitch)
            {
                _manualPanOffset = Vector2.zero;
            }

            _lastFocusedPlayer = currentPlayer;
        }

        UpdateManualPanOffset();

        Vector3 focusPoint = GetDesiredFocusPoint(autoFollowEnabled, currentPlayer);
        focusPoint.x += _manualPanOffset.x;
        focusPoint.z += _manualPanOffset.y;
        focusPoint = ClampFocusPoint(focusPoint, GetCurrentViewPadding());

        Vector3 desiredPosition = focusPoint + _cameraToFocusOffset;
        desiredPosition = ClampWorldPosition(desiredPosition);

        float followSpeed = autoFollowEnabled ? focusMoveSpeed : focusMoveSpeed * 0.85f;
        if (autoFollowEnabled && playerManager != null && playerManager.IsPlayerMoving(currentPlayer))
        {
            followSpeed *= movingFollowBoost;
        }

        if (turnSwitched)
        {
            followSpeed = turnSwitchMoveSpeed;
        }

        transform.rotation = _initialRotation;
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            1f - Mathf.Exp(-followSpeed * Time.deltaTime));
    }

    private void UpdateManualPanOffset()
    {
        Vector3 input = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) input += ProjectDirectionOnGround(transform.forward);
        if (Input.GetKey(KeyCode.S)) input -= ProjectDirectionOnGround(transform.forward);
        if (Input.GetKey(KeyCode.D)) input += ProjectDirectionOnGround(transform.right);
        if (Input.GetKey(KeyCode.A)) input -= ProjectDirectionOnGround(transform.right);

        if (input.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        input.Normalize();

        float zoomRatio = _cachedCamera != null && _cachedCamera.orthographic && _maxOrthographicSize > 0.01f
            ? Mathf.Clamp01(_cachedCamera.orthographicSize / _maxOrthographicSize)
            : 1f;
        float scaledSpeed = manualPanSpeed * Mathf.Lerp(0.65f, 1f, zoomRatio);
        Vector3 delta = input * (scaledSpeed * Time.deltaTime);
        _manualPanOffset += new Vector2(delta.x, delta.z);
    }

    private Vector3 GetDesiredFocusPoint(bool autoFollowEnabled, int currentPlayer)
    {
        if (!autoFollowEnabled || playerManager == null)
        {
            return GetInitialFocusPoint();
        }

        Transform playerTransform = playerManager.GetPlayerTransform(currentPlayer);
        if (playerTransform == null)
        {
            return GetInitialFocusPoint();
        }

        Vector3 focusPoint = playerTransform.position;
        focusPoint.y = _focusPlaneY;
        return focusPoint;
    }

    private bool IsAutoFollowEnabled()
    {
        if (_cachedCamera == null || !_cachedCamera.orthographic)
        {
            return false;
        }

        return _cachedCamera.orthographicSize < (_maxOrthographicSize - autoFollowZoomThreshold);
    }

    private float GetCurrentViewPadding()
    {
        if (_cachedCamera == null || !_cachedCamera.orthographic || _maxOrthographicSize <= 0.01f)
        {
            return mapClampPadding;
        }

        float zoomRatio = Mathf.Clamp01(_cachedCamera.orthographicSize / _maxOrthographicSize);
        return mapClampPadding + Mathf.Lerp(0.15f, 0.95f, zoomRatio);
    }

    private Vector3 ClampFocusPoint(Vector3 focusPoint, float padding)
    {
        if (!_boundsReady)
        {
            return focusPoint;
        }

        focusPoint.x = Mathf.Clamp(focusPoint.x, _mapMin.x - padding, _mapMax.x + padding);
        focusPoint.z = Mathf.Clamp(focusPoint.z, _mapMin.y - padding, _mapMax.y + padding);
        focusPoint.y = _focusPlaneY;
        return focusPoint;
    }

    private Vector3 ClampWorldPosition(Vector3 worldPosition)
    {
        if (!_boundsReady)
        {
            return worldPosition;
        }

        Vector3 focusPoint = worldPosition - _cameraToFocusOffset;
        focusPoint = ClampFocusPoint(focusPoint, GetCurrentViewPadding());
        worldPosition = focusPoint + _cameraToFocusOffset;
        worldPosition.y = _initialPosition.y;
        return worldPosition;
    }

    private Vector3 GetInitialFocusPoint()
    {
        Vector3 forward = _initialRotation * Vector3.forward;
        if (Mathf.Abs(forward.y) < 0.0001f)
        {
            return new Vector3(_initialPosition.x, _focusPlaneY, _initialPosition.z);
        }

        float distance = (_focusPlaneY - _initialPosition.y) / forward.y;
        return _initialPosition + forward * distance;
    }

    private static Vector3 ProjectDirectionOnGround(Vector3 direction)
    {
        direction.y = 0f;
        float magnitude = direction.magnitude;
        return magnitude > 0.0001f ? direction / magnitude : Vector3.zero;
    }
}
