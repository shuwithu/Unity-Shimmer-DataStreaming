using System.Collections;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.Input;

public class PinchFadeDestroy : MonoBehaviour
{
    [Header("Hand Reference (Oculus.Interaction.Input.Hand)")]
    [SerializeField] private Hand _hand;

    [Header("Double Pinch Settings")]
    [SerializeField] private float _doublePinchMaxDelay = 0.4f;

    [Header("Scale Down & Destroy Settings")]
    [SerializeField] private float _scaleDownDuration = 1f;

    [Header("Effect & Audio")]
    [SerializeField] private GameObject _effectPrefab;
    [SerializeField] private GameObject _parentForEffect;
    [SerializeField] private AudioClip _audioClip;

    private bool _wasPinching = false;
    private float _lastPinchTime = -1f;
    private bool _playerInsideTrigger = false;

    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.clip = _audioClip;
    }

    private void Update()
    {
        if (!_playerInsideTrigger || _hand == null || !_hand.IsConnected)
            return;

        bool isPinching = _hand.GetFingerIsPinching(HandFinger.Index);

        if (isPinching && !_wasPinching)
        {
            float currentTime = Time.time;

            if (currentTime - _lastPinchTime <= _doublePinchMaxDelay)
            {
                Debug.Log("Double pinch detected while inside trigger!");
                HandleDoublePinch();
                _lastPinchTime = -1f;
            }
            else
            {
                _lastPinchTime = currentTime;
            }
        }

        _wasPinching = isPinching;
    }

    private void HandleDoublePinch()
    {
        if (_audioSource != null && _audioClip != null)
        {
            _audioSource.Play();
        }

        if (_effectPrefab != null)
        {
            Transform parent = _parentForEffect != null ? _parentForEffect.transform : transform;
            Instantiate(_effectPrefab, transform.position, Quaternion.identity, parent);
        }

        StartCoroutine(ScaleDownAndDestroy());
    }

    private IEnumerator ScaleDownAndDestroy()
    {
        Vector3 originalWorldScale = transform.lossyScale;
        float elapsed = 0f;

        while (elapsed < _scaleDownDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _scaleDownDuration);
            Vector3 newWorldScale = Vector3.Lerp(originalWorldScale, Vector3.zero, t);

            if (transform.parent != null)
            {
                Vector3 parentScale = transform.parent.lossyScale;
                transform.localScale = new Vector3(
                    newWorldScale.x / parentScale.x,
                    newWorldScale.y / parentScale.y,
                    newWorldScale.z / parentScale.z
                );
            }
            else
            {
                transform.localScale = newWorldScale;
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        _playerInsideTrigger = true;
    }

    private void OnTriggerExit(Collider other)
    {
        _playerInsideTrigger = false;
    }
}
