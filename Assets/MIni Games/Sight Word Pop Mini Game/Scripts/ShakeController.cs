using System.Collections;
using UnityEngine;

/// <summary>
/// Attached to the VISUAL CHILD of each floating object.
/// Parent handles floating upward. This handles shake/wiggle only.
/// The separation ensures float and shake never conflict.
/// 
/// Architecture:
///   [FloatingObject (parent)] - moves upward
///       └── [VisualChild] - ShakeController lives here
///             └── SpriteRenderer / Text
/// </summary>
public class ShakeController : MonoBehaviour
{
    private Coroutine _shakeRoutine;
    private Vector3 _originalLocalPos;
    private Quaternion _originalLocalRot;
    private Vector3 _originalLocalScale;

    private void Awake()
    {
        _originalLocalPos = transform.localPosition;
        _originalLocalRot = transform.localRotation;
        _originalLocalScale = transform.localScale;
    }

    /// <summary>Call this when the audio for this word starts playing.</summary>
    public void StartShake(ShakeStyle style, float intensity, float speed)
    {
        StopShake();
        _shakeRoutine = style switch
        {
            ShakeStyle.SpinWobble    => StartCoroutine(SpinWobbleRoutine(intensity, speed)),
            ShakeStyle.SideSway      => StartCoroutine(SideSwayRoutine(intensity, speed)),
            ShakeStyle.SquishStretch => StartCoroutine(SquishStretchRoutine(intensity, speed)),
            _                        => StartCoroutine(SideSwayRoutine(intensity, speed))
        };
    }

    /// <summary>Call this when audio stops OR the object is tapped/returned to pool.</summary>
    public void StopShake()
    {
        if (_shakeRoutine != null)
        {
            StopCoroutine(_shakeRoutine);
            _shakeRoutine = null;
        }
        ResetTransform();
    }

    // ─────────────────────────────────────────────
    // Shake Styles
    // ─────────────────────────────────────────────

    /// <summary>
    /// STAR: Rotation oscillation + scale pulse.
    /// Feels excited and sparkly.
    /// </summary>
    private IEnumerator SpinWobbleRoutine(float intensity, float speed)
    {
        float time = 0f;
        while (true)
        {
            time += Time.deltaTime * speed;
            float angle = Mathf.Sin(time) * intensity;
            float scalePulse = 1f + Mathf.Abs(Mathf.Sin(time * 0.5f)) * 0.12f;

            transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            transform.localScale = _originalLocalScale * scalePulse;
            yield return null;
        }
    }

    /// <summary>
    /// CLOUD: Gentle left-right translation.
    /// Soft and floaty.
    /// </summary>
    private IEnumerator SideSwayRoutine(float intensity, float speed)
    {
        float time = 0f;
        while (true)
        {
            time += Time.deltaTime * speed;
            float offsetX = Mathf.Sin(time) * intensity;

            transform.localPosition = _originalLocalPos + new Vector3(offsetX, 0f, 0f);
            yield return null;
        }
    }

    /// <summary>
    /// BUBBLE: Squish on Y, stretch on X — elastic jiggle.
    /// Wobbly and bouncy.
    /// </summary>
    private IEnumerator SquishStretchRoutine(float intensity, float speed)
    {
        float time = 0f;
        float squishAmount = intensity * 0.015f; // Normalized: intensity 8 → 0.12 scale variance

        while (true)
        {
            time += Time.deltaTime * speed;
            float squish = Mathf.Sin(time) * squishAmount;

            transform.localScale = new Vector3(
                _originalLocalScale.x * (1f + squish),
                _originalLocalScale.y * (1f - squish),
                _originalLocalScale.z
            );
            yield return null;
        }
    }

    private void ResetTransform()
    {
        transform.localPosition = _originalLocalPos;
        transform.localRotation = _originalLocalRot;
        transform.localScale = _originalLocalScale;
    }

    private void OnDisable()
    {
        // Always clean up when pooled
        StopShake();
    }
}