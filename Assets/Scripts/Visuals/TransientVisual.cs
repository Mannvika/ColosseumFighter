using UnityEngine;
using System.Collections;

public class TransientVisual : MonoBehaviour
{
    [SerializeField] private float _desiredDuration = 0.25f; 
    [SerializeField] private Vector3 _startScale;
    [SerializeField] private Vector3 _endScale;
    [SerializeField] private SpriteRenderer _renderer;

    [SerializeField] private float _tolerance = 0.05f;

    private void Awake()
    {
        if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
    }
    
    public void Play(float cooldownConstraint)
    {
        float maxAllowed = Mathf.Max(0.05f, cooldownConstraint - _tolerance);
        
        float actualDuration = Mathf.Min(_desiredDuration, maxAllowed);
        if (actualDuration <= 0) actualDuration = 0.1f;

        StartCoroutine(AnimateRoutine(actualDuration));
    }

    public IEnumerator AnimateRoutine(float duration)
    {
        float timer = 0f;
        Color startColor = _renderer.color;
        Color endColor = new(startColor.r, startColor.g, startColor.b, 0f);

        transform.localScale = _startScale;
        _renderer.color = startColor;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            transform.localScale = Vector3.Lerp(_startScale, _endScale, t);
            _renderer.color = Color.Lerp(startColor, endColor, t);

            yield return null;
        }

        Destroy(gameObject);
    }

}
