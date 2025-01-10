using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gate : MonoBehaviour
{
    public GameObject GateVisual;
    private Collider _gateCollider;
    public float OpenDuration = 2f;
    public float OpenTargetY = -1.5f;

    private void Awake()
    {
        _gateCollider = GetComponent<Collider>();
    }

    IEnumerator OpenGateAnimation()
    {
        float currentOpenDuration = 0;
        Vector3 StartPos = GateVisual.transform.position;
        Vector3 TargetPos = StartPos + Vector3.up * OpenTargetY;

        while (currentOpenDuration < OpenDuration)
        {
            currentOpenDuration += Time.deltaTime;
            GateVisual.transform.position = Vector3.Lerp(StartPos, TargetPos, currentOpenDuration / OpenDuration);
            yield return null;
        }

        _gateCollider.enabled = false;
    }

    public void Open()
    {
        StartCoroutine(OpenGateAnimation());
    }
}
