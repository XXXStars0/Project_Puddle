using UnityEngine;

public class SplashVisual : MonoBehaviour
{
    [Tooltip("Fallback lifetime if no Animator is found.")]
    public float lifetime = 0.3f;

    void Start()
    {
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            Destroy(gameObject, stateInfo.length);
        }
        else
        {
            Destroy(gameObject, lifetime);
        }
    }

    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}
