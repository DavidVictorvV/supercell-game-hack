using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ObjectTransition : MonoBehaviour
{
    private enum TransitionMode { None, Absolute, Relative }
    private enum ShakeMode { None, Constant, Increasing }

    [BoxGroup("Position")]
    [LabelText("Mode")]
    [SerializeField] private TransitionMode positionMode = TransitionMode.Absolute;

    [BoxGroup("Position")]
    [ShowIf("positionMode", TransitionMode.Absolute)]
    [SerializeField] private Vector3 startPosition;

    [BoxGroup("Position")]
    [ShowIf("positionMode", TransitionMode.Absolute)]
    [SerializeField] private Vector3 endPosition;

    [BoxGroup("Position")]
    [ShowIf("positionMode", TransitionMode.Absolute)]
    [Button("Set Start")] private void SetStartPos() => startPosition = transform.position;

    [BoxGroup("Position")]
    [ShowIf("positionMode", TransitionMode.Relative)]
    [SerializeField] private Vector3 positionOffset;

    [BoxGroup("Rotation")]
    [LabelText("Mode")]
    [SerializeField] private TransitionMode rotationMode = TransitionMode.None;

    [BoxGroup("Rotation")]
    [ShowIf("rotationMode", TransitionMode.Absolute)]
    [SerializeField] private Vector3 startRotation;

    [BoxGroup("Rotation")]
    [ShowIf("rotationMode", TransitionMode.Absolute)]
    [SerializeField] private Vector3 endRotation;

    [BoxGroup("Rotation")]
    [ShowIf("rotationMode", TransitionMode.Absolute)]
    [Button("Set Start")] private void SetStartRot() => startRotation = transform.rotation.eulerAngles;

    [BoxGroup("Rotation")]
    [ShowIf("rotationMode", TransitionMode.Relative)]
    [SerializeField] private Vector3 rotationOffset;

    [BoxGroup("Rotation")]
    [ShowIf("ShowPivot")]
    [Tooltip("Local-space offset from object center to the rotation pivot point. E.g. (0, -0.5, 0) to rotate from the bottom.")]
    [SerializeField] private Vector3 pivotOffset;

    [BoxGroup("Rotation")]
    [ShowIf("ShowPivot")]
    [HorizontalGroup("Rotation/PivotPresets")]
    [Button("Bottom")] private void PivotBottom() => SetPivotFromBounds(Vector3.down);
    [HorizontalGroup("Rotation/PivotPresets")]
    [Button("Top")] private void PivotTop() => SetPivotFromBounds(Vector3.up);
    [HorizontalGroup("Rotation/PivotPresets")]
    [Button("Left")] private void PivotLeft() => SetPivotFromBounds(Vector3.left);
    [HorizontalGroup("Rotation/PivotPresets")]
    [Button("Right")] private void PivotRight() => SetPivotFromBounds(Vector3.right);
    [HorizontalGroup("Rotation/PivotPresets")]
    [Button("Front")] private void PivotFront() => SetPivotFromBounds(Vector3.forward);
    [HorizontalGroup("Rotation/PivotPresets")]
    [Button("Back")] private void PivotBack() => SetPivotFromBounds(Vector3.back);

    [BoxGroup("Shake")]
    [LabelText("Mode")]
    [SerializeField] private ShakeMode shakeMode = ShakeMode.None;

    [BoxGroup("Shake")]
    [ShowIf("HasShake")]
    [MinValue(0.001f)]
    [SerializeField] private float shakeStrength = 0.1f;

    [BoxGroup("Shake")]
    [ShowIf("HasShake")]
    [MinValue(1)]
    [SerializeField] private int shakeVibrato = 10;

    private enum TimeUnit { Seconds, Frames }

    [BoxGroup("Timing")]
    [LabelText("Unit")]
    [SerializeField] private TimeUnit timeUnit = TimeUnit.Seconds;

    [BoxGroup("Timing")]
    [MinValue(0)]
    [SerializeField] private float delay;

    [BoxGroup("Timing")]
    [MinValue(0.01f)]
    [SerializeField] private float duration = 1f;

    private bool ShowPivot => rotationMode != TransitionMode.None;
    private bool HasShake => shakeMode != ShakeMode.None;

    private float FrameRate => Application.targetFrameRate > 0 ? Application.targetFrameRate : 60f;
    private float DelaySeconds => timeUnit == TimeUnit.Frames ? delay / FrameRate : delay;
    private float DurationSeconds => timeUnit == TimeUnit.Frames ? duration / FrameRate : duration;

    private Transform _pivot;
    private Sequence _sequence;
    private Tween _shakeTween;
    private Vector3 _savedPosition;
    private Quaternion _savedRotation;
    private bool _hasSavedState;

    private Vector3 GetFinalPos(Vector3 currentPos) => positionMode switch
    {
        TransitionMode.Absolute => endPosition,
        TransitionMode.Relative => currentPos + positionOffset,
        _ => currentPos
    };

    private Quaternion GetStartRot() => rotationMode == TransitionMode.Absolute
        ? Quaternion.Euler(startRotation)
        : transform.rotation;

    private Quaternion GetFinalRot(Quaternion currentRot) => rotationMode switch
    {
        TransitionMode.Absolute => Quaternion.Euler(endRotation),
        TransitionMode.Relative => currentRot * Quaternion.Euler(rotationOffset),
        _ => currentRot
    };

    private bool HasPivot => rotationMode != TransitionMode.None && pivotOffset != Vector3.zero;

    private void SetPivotFromBounds(Vector3 direction)
    {
        var halfExtents = GetBoundsSize() * 0.5f;
        pivotOffset = Vector3.Scale(direction, halfExtents);
    }

    private void Start()
    {
        Activate();
    }

    public void Activate()
    {
        _sequence?.Kill();
        _shakeTween?.Kill();
        CleanupPivot();

        var currentPos = transform.position;
        var currentRot = transform.rotation;

        if (positionMode == TransitionMode.Absolute)
        {
            currentPos = startPosition;
            transform.position = startPosition;
        }

        if (rotationMode == TransitionMode.Absolute)
        {
            currentRot = Quaternion.Euler(startRotation);
            transform.rotation = currentRot;
        }

        var targetPos = GetFinalPos(currentPos);
        var targetRot = GetFinalRot(currentRot);
        var delayS = DelaySeconds;
        var durationS = DurationSeconds;

        // Shake during delay
        if (HasShake && delayS > 0)
        {
            if (shakeMode == ShakeMode.Constant)
            {
                _shakeTween = transform.DOShakePosition(delayS, shakeStrength, shakeVibrato);
            }
            else // Increasing
            {
                _shakeTween = DOTween.To(
                    () => 0f, strength =>
                    {
                        var offset = Random.insideUnitSphere * strength;
                        transform.position = currentPos + offset;
                    },
                    shakeStrength, delayS
                ).SetEase(Ease.InQuad);
            }
        }

        // Main transition after delay
        _sequence = DOTween.Sequence();
        _sequence.AppendInterval(delayS);
        _sequence.AppendCallback(() => { if (HasShake) transform.position = currentPos; });

        if (HasPivot)
        {
            CreatePivot(currentPos, currentRot);

            if (positionMode != TransitionMode.None)
                _sequence.Append(_pivot.DOMove(targetPos + targetRot * pivotOffset, durationS).SetEase(Ease.InOutSine));

            _sequence.Join(_pivot.DORotateQuaternion(targetRot, durationS).SetEase(Ease.InOutSine));
        }
        else
        {
            if (positionMode != TransitionMode.None)
                _sequence.Append(transform.DOMove(targetPos, durationS).SetEase(Ease.InOutSine));

            if (rotationMode != TransitionMode.None)
                _sequence.Join(transform.DORotateQuaternion(targetRot, durationS).SetEase(Ease.InOutSine));
        }
    }

    private void CreatePivot(Vector3 objPos, Quaternion objRot)
    {
        _pivot = new GameObject($"{name}_Pivot").transform;
        _pivot.position = objPos + objRot * pivotOffset;
        _pivot.rotation = objRot;
        transform.SetParent(_pivot, true);
    }

    private void CleanupPivot()
    {
        if (_pivot == null) return;

        transform.SetParent(_pivot.parent, true);

        if (Application.isPlaying)
            Destroy(_pivot.gameObject);
        else
            DestroyImmediate(_pivot.gameObject);

        _pivot = null;
    }

    private void OnDestroy()
    {
        _sequence?.Kill();
        _shakeTween?.Kill();

        // Don't reparent during destroy - just kill the pivot
        if (_pivot != null)
        {
            if (Application.isPlaying)
                Destroy(_pivot.gameObject);
            else
                DestroyImmediate(_pivot.gameObject);

            _pivot = null;
        }
    }

    private void SaveState()
    {
        if (_hasSavedState) return;
        _savedPosition = transform.position;
        _savedRotation = transform.rotation;
        _hasSavedState = true;
    }

    // ---- Editor Buttons ----

    [BoxGroup("Editor")]
    [HorizontalGroup("Editor/Buttons")]
    [Button("Reset", ButtonSizes.Medium)]
    private void ResetToStart()
    {
        StopPreview();

        if (_hasSavedState)
        {
            transform.position = _savedPosition;
            transform.rotation = _savedRotation;
            _hasSavedState = false;
        }
        else if (positionMode == TransitionMode.Absolute)
        {
            transform.position = startPosition;
            if (rotationMode == TransitionMode.Absolute)
                transform.rotation = Quaternion.Euler(startRotation);
        }
    }

    [HorizontalGroup("Editor/Buttons")]
    [Button("Preview End", ButtonSizes.Medium)]
    private void PreviewEnd()
    {
        SaveState();
        StopPreview();

        var currentPos = positionMode == TransitionMode.Absolute ? startPosition : _savedPosition;
        var currentRot = GetStartRot();

        var targetPos = GetFinalPos(currentPos);
        var targetRot = GetFinalRot(currentRot);

        if (HasPivot)
        {
            var pivotWorld = currentPos + currentRot * pivotOffset;
            transform.position = pivotWorld + targetRot * -pivotOffset;
            transform.rotation = targetRot;
        }
        else
        {
            if (positionMode != TransitionMode.None)
                transform.position = targetPos;

            if (rotationMode != TransitionMode.None)
                transform.rotation = targetRot;
        }
    }

    [HorizontalGroup("Editor/Buttons")]
    [Button("$PlayButtonLabel", ButtonSizes.Medium)]
    private void PlayPreview()
    {
        if (Application.isPlaying)
        {
            Activate();
            return;
        }

#if UNITY_EDITOR
        if (_isPreviewing)
        {
            StopPreview();
            return;
        }

        SaveState();
        ResetToStart();
        StartPreview();
#endif
    }

    private string PlayButtonLabel => _isPreviewing ? "Stop" : "Play";

    // ---- Editor Preview Animation ----

    private bool _isPreviewing;

#if UNITY_EDITOR
    private double _previewStartTime;
    private double _previewShakeEnd;
    private Vector3 _previewStartPos;
    private Vector3 _previewEndPos;
    private Quaternion _previewStartRot;
    private Quaternion _previewEndRot;
    private Vector3 _previewPivotWorld;

    private void StartPreview()
    {
        _previewStartPos = positionMode == TransitionMode.Absolute ? startPosition : transform.position;
        _previewStartRot = GetStartRot();
        _previewEndPos = GetFinalPos(_previewStartPos);
        _previewEndRot = GetFinalRot(_previewStartRot);

        if (HasPivot)
            _previewPivotWorld = _previewStartPos + _previewStartRot * pivotOffset;

        var now = EditorApplication.timeSinceStartup;
        _previewShakeEnd = now + DelaySeconds;
        _previewStartTime = _previewShakeEnd;
        _isPreviewing = true;
        EditorApplication.update += EditorUpdate;
    }

    private void StopPreview()
    {
        if (!_isPreviewing) return;

        _isPreviewing = false;
        EditorApplication.update -= EditorUpdate;

        _sequence?.Kill();
        _shakeTween?.Kill();
        CleanupPivot();

        if (_hasSavedState)
        {
            transform.position = _savedPosition;
            transform.rotation = _savedRotation;
            _hasSavedState = false;
        }
    }

    private void EditorUpdate()
    {
        if (this == null)
        {
            EditorApplication.update -= EditorUpdate;
            _isPreviewing = false;
            return;
        }

        var now = EditorApplication.timeSinceStartup;

        // Shake phase (during delay)
        if (HasShake && DelaySeconds > 0 && now < _previewShakeEnd)
        {
            var shakeProgress = (float)((now - (_previewShakeEnd - DelaySeconds)) / DelaySeconds);
            var strength = shakeMode == ShakeMode.Increasing
                ? shakeStrength * shakeProgress * shakeProgress
                : shakeStrength;

            var offset = Random.insideUnitSphere * strength;
            transform.position = _previewStartPos + offset;

            SceneView.RepaintAll();
            return;
        }

        // Snap back after shake, before transition
        var elapsed = now - _previewStartTime;
        if (elapsed < 0)
        {
            transform.position = _previewStartPos;
            return;
        }

        var t = Mathf.Clamp01((float)(elapsed / DurationSeconds));
        t = EaseInOutSine(t);

        if (HasPivot)
        {
            var pivotRot = Quaternion.Slerp(_previewStartRot, _previewEndRot, t);
            var pivotPos = positionMode != TransitionMode.None
                ? Vector3.Lerp(_previewPivotWorld, _previewEndPos + _previewEndRot * pivotOffset, t)
                : _previewPivotWorld;

            transform.position = pivotPos + pivotRot * -pivotOffset;
            transform.rotation = pivotRot;
        }
        else
        {
            if (positionMode != TransitionMode.None)
                transform.position = Vector3.Lerp(_previewStartPos, _previewEndPos, t);

            if (rotationMode != TransitionMode.None)
                transform.rotation = Quaternion.Slerp(_previewStartRot, _previewEndRot, t);
        }

        SceneView.RepaintAll();

        if (t >= 1f)
            StopPreview();
    }

    private static float EaseInOutSine(float t)
    {
        return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
    }
#else
    private void StopPreview() { }
#endif

    // ---- Scene Handle for Pivot ----

#if UNITY_EDITOR
    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!ShowPivot || this == null) return;

        var worldPivot = transform.position + transform.rotation * pivotOffset;

        EditorGUI.BeginChangeCheck();
        var newWorldPivot = Handles.PositionHandle(worldPivot, transform.rotation);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(this, "Move Pivot");
            pivotOffset = Quaternion.Inverse(transform.rotation) * (newWorldPivot - transform.position);
        }

        Handles.color = Color.cyan;
        Handles.Label(worldPivot + Vector3.up * 0.2f, "Pivot");
    }
#endif

    // ---- Gizmos ----

    private void OnDrawGizmosSelected()
    {
        if (positionMode == TransitionMode.None && rotationMode == TransitionMode.None)
            return;

        var sPos = positionMode == TransitionMode.Absolute ? startPosition : transform.position;
        var sRot = rotationMode == TransitionMode.Absolute
            ? Quaternion.Euler(startRotation)
            : transform.rotation;

        var ePos = GetFinalPos(sPos);
        var eRot = GetFinalRot(sRot);

        var gizmoSize = GetBoundsSize();

        Gizmos.color = Color.green;
        DrawRotatedCube(sPos, sRot, gizmoSize);

        Gizmos.color = Color.red;
        DrawRotatedCube(ePos, eRot, gizmoSize);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(sPos, ePos);

        if (HasPivot)
        {
            var pivotWorld = sPos + sRot * pivotOffset;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(pivotWorld, 0.15f);
            Gizmos.DrawLine(sPos, pivotWorld);
        }
    }

    private Vector3 GetBoundsSize()
    {
        var mesh = GetComponentInChildren<MeshFilter>();
        if (mesh != null && mesh.sharedMesh != null)
            return Vector3.Scale(mesh.sharedMesh.bounds.size, mesh.transform.lossyScale);

        var collider = GetComponentInChildren<Collider>();
        if (collider != null)
            return collider.bounds.size;

        return Vector3.one * 0.5f;
    }

    private static void DrawRotatedCube(Vector3 position, Quaternion rotation, Vector3 size)
    {
        var prev = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(position, rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, size);
        Gizmos.matrix = prev;
    }
}
