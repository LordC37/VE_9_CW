using UnityEngine;
using Valve.VR;

public class CueHandler : MonoBehaviour
{
    public Transform frontControllerTransform;
    public Transform backControllerTransform;
    public Transform cueTip;


    private Rigidbody cueRB;

    private float lockOffset;
    private Vector3 cuePos;
    private Vector3 lockForward;

    void Start()
    {
        cueRB = gameObject.GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (frontControllerTransform != null && backControllerTransform != null)
        {
            UpdateCuePosition();
        }
    }

    void UpdateCuePosition()
    {
        bool triggerPressed = Input.GetButton("Fire1");
        bool triggerDown = Input.GetButtonDown("Fire1");

        Vector3 frontPos = frontControllerTransform.position;
        Vector3 backPos = backControllerTransform.position;

        if (triggerDown)
        {
            //print("first press");
            lockForward = transform.forward;
            lockOffset = (frontPos - backPos).magnitude;
        }
        else if (triggerPressed)
        {
            //print("held");
            float currOffset = (frontPos - backPos).magnitude;
            cueRB.MovePosition(cuePos + lockForward * (lockOffset - currOffset));
        }
        else
        {
            //print("free");
            cuePos = 0.75f * backPos + 0.25f * frontPos;
            cueRB.MovePosition(cuePos);
            cueRB.MoveRotation(Quaternion.LookRotation(frontPos - backPos) * Quaternion.Euler(new Vector3(90f, 0f, 0f)));
        }
    }

    void OnCollisionEnter(Collision col)
    {
        Rigidbody rb = col.gameObject.GetComponent<Rigidbody>();

        if (!rb)
        {
            return;
        }

        Vector3 forceDirection = (col.contacts[0].point - cueTip.position).normalized;
        rb.AddForce(forceDirection * cueRB.linearVelocity.magnitude);

    }   
}