using UnityEngine;
using System.Collections;
using System;

public class Controller2D : RaycastController {

	float maxClimbAngle = 80f;
	float maxDescendAngle = 80f;

	public CollisionsInfo collisions;
	private Vector2 playerInput;

	public override void Start ()
	{
		base.Start();
		collisions.faceDir = 1;
	}

	// overload method, because don't want to have to pass in a 'Vector2 input' to the move meothod in platform controller
	public void Move (Vector3 velocity, bool standingOnPlatform)
	{
		Move(velocity, Vector2.zero, standingOnPlatform);
	}

	public void Move (Vector3 velocity, Vector2 input, bool standingOnPlatform = false)
	{
		UpdateRaycastOrigins ();
		collisions.Reset ();
		collisions.velocityOld = velocity;
		playerInput = input;

		if (velocity.x != 0) {
			collisions.faceDir = (int)Mathf.Sign(velocity.x);// cast MAthf.Sign to an integer, its normally a float
		}

		if (velocity.y < 0 && !collisions.hanging) {
			DescendSlope (ref velocity);
		}

		HorizontalCollisions (ref velocity);	

		if (velocity.y != 0) {
			VerticalCollisions (ref velocity);
		}

		
		transform.Translate (velocity);

		if (standingOnPlatform) {
			collisions.below = true;
		}

	}

	void HorizontalCollisions (ref Vector3 velocity)
	{

		// get direction i.e. +1 or -1
		float directionX = collisions.faceDir;
		float rayLength = Mathf.Abs (velocity.x) + skinWidth;

		if (Mathf.Abs (velocity.x) < skinWidth) {
			rayLength = 2 * skinWidth;
		}



		for (int i = 0; i < horizontalRayCount; i++) {
			// if moving down then rays need to start in bototm left corner
			// if moving up then rays need to start in top left corner
			Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;

			// need to shift the raycast origin with the potentially moving player
			// shifting in the up direction because on the side of the player
			rayOrigin += Vector2.up * (horizontalRaySpacing * i);

			// cast the ray out ato detect for hits
			RaycastHit2D hit = Physics2D.Raycast (rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

			Debug.DrawRay (rayOrigin, Vector2.right * directionX * rayLength, Color.red);

			if (hit) {

				// this is used to fix player x movement when overlapping with a moving platform
				// if its in a collider then the raycast will set hit distance to 0, so rather move on to the next ray, one that's not overlapping
				if (hit.distance == 0) {
					continue;
				}

				float slopeAngle = Vector2.Angle (hit.normal, Vector2.up);
				if (i == 0 && slopeAngle <= maxClimbAngle) {
					if (collisions.descendingSlope) {
						collisions.descendingSlope = false;
						velocity = collisions.velocityOld;
					}
					float distanceToSlopeStart = 0;
					if (slopeAngle != collisions.slopeAngleOld) {
						distanceToSlopeStart = hit.distance - skinWidth;
						velocity.x -= distanceToSlopeStart * directionX;
					}
					ClimbSlope (ref velocity, slopeAngle);
					velocity.x += distanceToSlopeStart * directionX;
				}

				// check if hanging / /climbing overhang
//				if (collisions.hanging && i == (horizontalRayCount - 1)){
//					DescendOverhang(ref velocity);
//				}

				if (!collisions.climbingSlope || slopeAngle > maxClimbAngle) {
					velocity.x = (hit.distance - skinWidth) * directionX;
					rayLength = hit.distance;

					// fix jittery motion when there's an obstacle on a slope
					// velocity.x is being reduced, so need to adjust y velocity to compensate
					if (collisions.climbingSlope) {
						velocity.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x);
					}

					// if something has been hit adn going either left or right set the appropriate collision info.
					collisions.left = directionX == -1;
					collisions.right = directionX == 1;
				}
			}

		}
	}

	// ref means that if the variable is changed in this function it will also be changed in the parent function
	void VerticalCollisions (ref Vector3 velocity)
	{

		// get direction i.e. +1 or -1
		float directionY = Mathf.Sign (velocity.y);
		float rayLength = Mathf.Abs (velocity.y) + skinWidth;

		Debug.Log ("DirectionY ---------> > > " + directionY);

		for (int i = 0; i < verticalRayCount; i++) {
			// if moving down then rays need to start in bototm left corner
			// if moving up then rays need to start in top left corner
			Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;

			// need to shift the raycast origin with the potentially moving player
			rayOrigin += Vector2.right * (verticalRaySpacing * i + velocity.x);

			// cast the ray out ato detect for hits
			RaycastHit2D hit = Physics2D.Raycast (rayOrigin, Vector2.up * directionY, rayLength, collisionMask);

			Debug.DrawRay (rayOrigin, Vector2.up * directionY * rayLength, Color.red);

			if (hit) {

				if (hit.collider.tag == "Through") {
					if (directionY == 1 || hit.distance == 0) {
						Debug.Log ("Fall Through... going up and 0 hit distance");
						Debug.Log (collisions.below);
						continue;// skip this case adn don't handle the collision
					}
					if (collisions.fallingThroughPlatform) {
						Debug.Log ("Fall Through... still timing out");
						continue;
					}
					if (playerInput.y == -1) {
						Debug.Log ("Fall Through... pressed down");
						collisions.fallingThroughPlatform = true;
						Invoke ("ResetFallingThroughPlatform", 0.1f);
						continue;
					}
				}

//				if (hit.collider.tag == "Climbable") {
////					Debug.Log ("- - - - - - - - - - Hit a climbable surface - - - - - - - - ");
//					collisions.hanging = true;
//					collisions.above = true;
//				}

				velocity.y = (hit.distance - skinWidth) * directionY;
				rayLength = hit.distance;

				if (collisions.climbingSlope) {
					velocity.x = velocity.y / Mathf.Tan (collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign (velocity.x);
				}

				collisions.below = directionY == -1;
				collisions.above = directionY == 1;

			}

		}

		if (collisions.climbingSlope) {
			float directionX = Mathf.Sign (velocity.x);
			rayLength = Mathf.Abs (velocity.x) + skinWidth;
			Vector2 rayOrigin = ((directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) + Vector2.up * velocity.y;
			RaycastHit2D hit = Physics2D.Raycast (rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

			if (hit) {
				float slopeAngle = Vector2.Angle (hit.normal, Vector2.up);
				if (slopeAngle != collisions.slopeAngle) {
					velocity.x = (hit.distance = skinWidth) * directionX;
					collisions.slopeAngle = slopeAngle;
				}
			}
		}

	}

//// Navigate overhangs like slopes just the other way around
//	void ClimbOverhang (ref Vector3 velocity, float slopeAngle)
//	{
//		// adjusting speed in x direction according to slope angle and slope distance
//		float moveDistance = Mathf.Abs (velocity.x);
//
//		// dont want to directly set velocity.y bc it interferes with jumping
//		float climbVelocityY = Mathf.Sin (slopeAngle * Mathf.Deg2Rad) * moveDistance;
//
//		// assume we're jumping if velocity Y is > climbing velocity
//		if (velocity.y <= climbVelocityY) {
//			velocity.y = climbVelocityY;
//			velocity.x = Mathf.Cos(slopeAngle*Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);
//			// need to set collisions.below to TRUE, to indicate that despite moving up we're still grounded
//			collisions.above = true;
//			collisions.climbingSlope = true;
//			collisions.slopeAngle = slopeAngle;
//		}
//	}
//
//	void DescendOverhang (ref Vector3 velocity)
//	{
//		float directionX = Mathf.Sign (velocity.x);
//		Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.topLeft : raycastOrigins.topRight;
//		RaycastHit2D hit = Physics2D.Raycast (rayOrigin, Vector2.up, Mathf.Infinity, collisionMask);
//
//		Debug.Log("directionX " + directionX);
//
//		if (hit) {
//
//			Debug.Log("hit.normal " + Mathf.Sign(hit.normal.x));
//
//			float slopeAngle = 180 - Vector2.Angle (hit.normal, Vector2.up);
//			if (slopeAngle != 0) {
//				// the inverted normal has opposite sign
//				if ( Mathf.Sign(hit.normal.x) == -directionX ) {
//
//					Debug.Log("##### hit.distance - skinWidth #####" + (hit.distance - skinWidth));
//					Debug.Log(Mathf.Tan (slopeAngle * Mathf.Deg2Rad) * Mathf.Abs (velocity.x));
//
//					if (hit.distance - skinWidth <= Mathf.Tan (slopeAngle * Mathf.Deg2Rad) * Mathf.Abs (velocity.x)) {
//						float moveDistance = Mathf.Abs(velocity.x);
//						float descendVelocityY = Mathf.Sin (slopeAngle * Mathf.Deg2Rad) * moveDistance;
//						velocity.x = Mathf.Cos(slopeAngle*Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);
//						velocity.y -= descendVelocityY;
//
//						Debug.Log("velocity.y - - - - - - - - -. " + velocity.y);
//
////						collisions.slopeAngle = slopeAngle;
//						collisions.descendingOverhang = true;
//						collisions.above = true;
//					}
//				}
//			}
////			collisions.descendingOverhang = true;
//		}
//	}

	void ClimbSlope (ref Vector3 velocity, float slopeAngle)
	{
		// adjusting speed in x direction according to sslope angle adn slope distance
		float moveDistance = Mathf.Abs (velocity.x);

		// dont want to directly set velocity.y bc it interferes with jumping
		float climbVelocityY = Mathf.Sin (slopeAngle * Mathf.Deg2Rad) * moveDistance;

		// assume we're jumping if velocity Y is > climbing velocity
		if (velocity.y <= climbVelocityY) {
			velocity.y = climbVelocityY;
			velocity.x = Mathf.Cos(slopeAngle*Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);
			// need to set collisions.below to TRUE, to indicate that despite moving up we're still grounded
			collisions.below = true;
			collisions.climbingSlope = true;
			collisions.slopeAngle = slopeAngle;
		}
	}

	void DescendSlope (ref Vector3 velocity)
	{
		float directionX = Mathf.Sign (velocity.x);
		Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
		RaycastHit2D hit = Physics2D.Raycast (rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);

		if (hit) {
			float slopeAngle = Vector2.Angle (hit.normal, Vector2.up);
			if (slopeAngle != 0 && slopeAngle <= maxDescendAngle) {
				if ( Mathf.Sign(hit.normal.x) == directionX ) {
					if (hit.distance - skinWidth <= Mathf.Tan (slopeAngle * Mathf.Deg2Rad) * Mathf.Abs (velocity.x)) {
						float moveDistance = Mathf.Abs(velocity.x);
						float descendVelocityY = Mathf.Sin (slopeAngle * Mathf.Deg2Rad) * moveDistance;
						velocity.x = Mathf.Cos(slopeAngle*Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);
						velocity.y -= descendVelocityY;

						collisions.slopeAngle = slopeAngle;
						collisions.descendingSlope = true;
						collisions.below = true;
					}
				}
			}
		}
	}

	void ResetFallingThroughPlatform() {
		collisions.fallingThroughPlatform = false;
	}

	public struct CollisionsInfo {
		public bool above, below;
		public bool left, right;
		public bool climbingSlope;
		public bool descendingSlope;

		public float slopeAngle, slopeAngleOld;
		public Vector3 velocityOld;

		public int faceDir;

		public bool fallingThroughPlatform;

		public bool hanging;
		public float gravity;
		public bool climbingOverhang;
		public bool descendingOverhang;

		public void Reset() {
			above = below = false;
			left = right = false;
			climbingSlope = false;
			descendingSlope = false;

			climbingOverhang = false;
			descendingOverhang = false;

			slopeAngleOld = slopeAngle;
			slopeAngle = 0;

		}
	}
}
