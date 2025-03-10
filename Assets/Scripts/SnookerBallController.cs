using UnityEngine;
using System.Collections;

public class SnookerBallController : MonoBehaviour {
	void Start() {
		GetComponent<Rigidbody>().sleepThreshold = 0.15f;
	}

	void FixedUpdate () {
		var rigidbody = GetComponent<Rigidbody>();
		if (rigidbody.linearVelocity.y > 0) {
			var velocity = rigidbody.linearVelocity;
			velocity.y *= 0.3f;
			rigidbody.linearVelocity = velocity;
		}
	}
}
