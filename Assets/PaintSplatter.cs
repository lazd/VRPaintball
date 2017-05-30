using UnityEngine;
using System.Collections;

public class PaintSplatter : MonoBehaviour
{
    public float destroyTime = 0f;

    void Awake()
    {
      Destroy(gameObject, destroyTime);
    }
}