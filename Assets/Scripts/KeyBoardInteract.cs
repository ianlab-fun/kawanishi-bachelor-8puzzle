using LitMotion;
using UnityEngine;

public class KeyBoardInteract : MonoBehaviour
{
    [SerializeField] private RectTransform wRect;
    [SerializeField] private RectTransform aRect;
    [SerializeField] private RectTransform sRect;
    [SerializeField] private RectTransform dRect;
    [SerializeField] private float duration = 0.1f;
    [SerializeField] private float scale = 1.2f;
    
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            LMotion.Create(Vector3.one, Vector3.one * scale, duration)
                .Bind(x => wRect.localScale = x)
                .AddTo(this);
        }
        if (Input.GetKeyUp(KeyCode.W))
        {
            LMotion.Create(Vector3.one * scale, Vector3.one, duration)
                .Bind(x => wRect.localScale = x)
                .AddTo(this);
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            LMotion.Create(Vector3.one, Vector3.one * scale, duration)
                .Bind(x => aRect.localScale = x)
                .AddTo(this);
        }
        if (Input.GetKeyUp(KeyCode.A))
        {
            LMotion.Create(Vector3.one * scale, Vector3.one, duration)
                .Bind(x => aRect.localScale = x)
                .AddTo(this);
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            LMotion.Create(Vector3.one, Vector3.one * scale, duration)
                .Bind(x => sRect.localScale = x)
                .AddTo(this);
        }
        if (Input.GetKeyUp(KeyCode.S))
        {
            LMotion.Create(Vector3.one * scale, Vector3.one, duration)
                .Bind(x => sRect.localScale = x)
                .AddTo(this);
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            LMotion.Create(Vector3.one, Vector3.one * scale, duration)
                .Bind(x => dRect.localScale = x)
                .AddTo(this);
        }
        if (Input.GetKeyUp(KeyCode.D))
        {
            LMotion.Create(Vector3.one * scale, Vector3.one, duration)
                .Bind(x => dRect.localScale = x)
                .AddTo(this);
        }
    }
}
