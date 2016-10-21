using UnityEngine;
using System.Collections;

public class PlatformTrigger : MonoBehaviour {

	private PlatformController platformController;

	private GameObject[] triggeredPlatforms;

	void OnTriggerEnter2D(Collider2D other) {
        Debug.Log("Player triggered this trigger");
		triggeredPlatforms = GameObject.FindGameObjectsWithTag("TriggerPlatform");

		foreach (GameObject platform in triggeredPlatforms) {
			platformController = platform.GetComponent<PlatformController>();
			Debug.Log("Platform speed: " + platformController.speed);
			platformController.speed = 3;
        }

    }
}
