using UnityEngine;
using Oculus.VR;

public class CueHandler : MonoBehaviour
{
    public OVRInput.Controller frontController = OVRInput.Controller.LTouch;
    public OVRInput.Controller backController = OVRInput.Controller.RTouch;
    public Transform cueTip;
    private Rigidbody cueRB;

    private float lockOffset;
    private Vector3 cuePos;
    private Vector3 lockForward;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cueRB = gameObject.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateCuePosition();
    }

    void UpdateCuePosition()
    {
        Vector3 frontPos = OVRInput.GetLocalControllerPosition(frontController);
        Vector3 backPos = OVRInput.GetLocalControllerPosition(backController);

        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, backController))
        {
            //print("first press");
            lockForward = transform.up;
            lockOffset = (frontPos - backPos).magnitude;
        }
        else if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, backController))
        {
            //print("held");
            float currOffset = (frontPos - backPos).magnitude;
            cueRB.MovePosition(cuePos + lockForward * (lockOffset - currOffset));
        }
        else
        {
            cuePos = 0.75f * backPos + 0.25f * frontPos;
            cueRB.MovePosition(cuePos);
            cueRB.MoveRotation(Quaternion.LookRotation(frontPos - backPos) * Quaternion.Euler(90f, 0f, 0f));
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
        if (!rb)
        {
            return;
        }
        Vector3 forceDirection = (collision.contacts[0].point - cueTip.position).normalized;
        rb.AddForce(forceDirection * cueRB.linearVelocity.magnitude);
    }
}
