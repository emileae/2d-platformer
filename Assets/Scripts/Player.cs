using UnityEngine;
using System.Collections;
using System;

[RequireComponent (typeof (Controller2D))]
public class Player : MonoBehaviour {

	public float maxJumpHeight = 1f;
	public float minJumpHeight = 0.1f;
	public float timeToJumpApex = 0.2f;
	float gravity;
	float maxJumpVelocity;
	float minJumpVelocity;

	float pullVelocity = 0.2f;
	float releaseHangVelocity = 0.5f;

	public float wallSlideSpeedMax = 3;

	// wall jumps
	public Vector2 wallJumpClimb;
	public Vector2 wallJumpOff;
	public Vector2 wallLeap;

	public float wallStickTime = 0.25f;
	float timeToWallUnstick;

	// acceleration times for x direction
	float accelerationTimeAirborne = 0.2f;
	float accelerationTimeGrounded = 0.1f;
	float velocityXSmoothing;

	float moveSpeed = 0.5f;
	float minWalkSpeed = 0.0001f;
	Vector3 velocity;

	Controller2D controller;

	Animator anim;
	SpriteRenderer sprite;

	// Use this for initialization
	void Start () {
		controller = GetComponent<Controller2D>();

		gravity = -(2 * maxJumpHeight)/Mathf.Pow(timeToJumpApex, 2);

		// save a reference to gravity
		controller.collisions.gravity = gravity;

		maxJumpVelocity = Mathf.Abs( gravity * timeToJumpApex );
		minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);

		// animator reference
		anim = GetComponent<Animator>();
		// sprite reference
		sprite = GetComponent<SpriteRenderer>();
	}

	void Update ()
	{
		Vector2 input = new Vector2 (Input.GetAxisRaw ("Horizontal"), Input.GetAxisRaw ("Vertical"));
		int wallDirX = (controller.collisions.left) ? -1 : 1;


		float targetVelocityX = input.x * moveSpeed;

		// damping the x speed... doesn't feel very platformer-ish or fit with animations...
//		velocity.x = Mathf.SmoothDamp (velocity.x, targetVelocityX, ref velocityXSmoothing, ((controller.collisions.below) ? accelerationTimeGrounded : accelerationTimeAirborne));
		velocity.x = targetVelocityX;


		bool wallSliding = false;
		if ((controller.collisions.left || controller.collisions.right) && !controller.collisions.below && velocity.y < 0) {
			wallSliding = true;

			if (velocity.y < -wallSlideSpeedMax) {
				velocity.y = -wallSlideSpeedMax;
			}

			if (timeToWallUnstick > 0) {
				velocityXSmoothing = 0;
				velocity.x = 0;

				if (input.x != wallDirX && input.x != 0) {
					timeToWallUnstick -= Time.deltaTime;
				} else {
					timeToWallUnstick = wallStickTime;
				}
			} else {
				timeToWallUnstick = wallStickTime;
			}

		}

		if (controller.collisions.hanging) {
			// cancel gravity and pull up slightly to keep collisions.above
			velocity.y = pullVelocity - (gravity * Time.deltaTime);

			if (!controller.collisions.above) {
//				Debug.Log ("+++++++++++++++++++++++++++++++++++++++++++ NO ABOVE");
				controller.collisions.hanging = false;
			}

		}

		if (Input.GetButtonDown ("Jump")) {
			// turn off climbing / hanging
			if (controller.collisions.hanging) {
				// can still improve dismount while descending overhang
				if (controller.collisions.descendingOverhang) {
					velocity.x -= 0f;
				}
				velocity.y -= releaseHangVelocity;
				controller.collisions.hanging = false;
			}

			if (wallSliding) {
				if (wallDirX == input.x) {
					velocity.x = -wallDirX * wallJumpClimb.x;
					velocity.y = wallJumpClimb.y;
				} else if (input.x == 0) {
					velocity.x = -wallDirX * wallJumpOff.x;
					velocity.y = wallJumpOff.y;
				} else {
					velocity.x = -wallDirX * wallLeap.x;
					velocity.y = wallLeap.y;
				}
			}
			if (controller.collisions.below) {
				velocity.y = maxJumpVelocity;
			}
		}

		if (Input.GetButtonUp ("Jump")) {
			if (velocity.y > minJumpVelocity) {
				velocity.y = minJumpVelocity;
			}
		}

//		float targetVelocityX = input.x * moveSpeed;
//		velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisions.below ? accelerationTimeGrounded : accelerationTimeAirborne));
		velocity.y += gravity * Time.deltaTime;
		controller.Move (velocity * Time.deltaTime, input);


		if (Math.Sign (velocity.x) < 0) {
			anim.SetBool ("isWalking", true);
			sprite.flipX = false;
		} else if (Math.Sign (velocity.x) > 0) {
			anim.SetBool ("isWalking", true);
			sprite.flipX = true;
		}
		if (Math.Abs (velocity.x) < minWalkSpeed) {
			anim.SetBool ("isWalking", false);
//			velocity.x = 0;
		}

		if (controller.collisions.above || controller.collisions.below) {
			velocity.y = 0;
		}

		if (controller.collisions.above || controller.collisions.below) {
			anim.SetBool ("grounded", true);
		} else {
			anim.SetBool ("grounded", false);
		}

		Debug.Log ("jumping?????????????????------------------------:   " + velocity.y);
		anim.SetFloat("vSpeed", velocity.y);
//		Debug.Log("----------> collisions above..... " + controller.collisions.above);
	}
}
