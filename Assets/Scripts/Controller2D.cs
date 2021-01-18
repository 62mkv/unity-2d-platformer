using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller2D : RaycastController
{

    public CollisionInfo collisions;
    
    [HideInInspector]
    public Vector2 playerInput;

    public override void Start()
    {
        base.Start();
        collisions.faceDir = 1;
    }

    public void Move(Vector2 distance, bool standingOnPlatform = false) {
        Move(distance, Vector2.zero, standingOnPlatform);
    }

    public void Move(Vector2 distance, Vector2 input, bool standingOnPlatform = false)
    {
        UpdateRaycastOrigins();
        collisions.Reset();
        collisions.velocityOld = distance;
        playerInput = input;

        if (distance.x != 0) {
            collisions.faceDir = (int)Mathf.Sign(distance.x);

        }

        if (distance.y < 0)
        {
            DescendSlope(ref distance);
        }

        HorizontalCollisions(ref distance);

        if (distance.y != 0)
        {
            VerticalCollisions(ref distance);
        }

        transform.Translate(distance);

        if (standingOnPlatform)
        {
            collisions.below = true;
        }
    }

    protected void HorizontalCollisions(ref Vector2 distance)
    {
        float directionX = collisions.faceDir;
        float rayLength = Mathf.Abs(distance.x) + skinWidth;

        if (Mathf.Abs(distance.x) < skinWidth) {
            rayLength = 2 * skinWidth;
        }

        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayOrigin = directionX == -1 ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.right * directionX, Color.red);

            if (hit)
            {

                if (hit.distance == 0)
                {
                    continue;
                }

                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                if (i == 0 && slopeAngle <= maxClimbAngle)
                {
                    if (collisions.descendingSlope)
                    {
                        collisions.descendingSlope = false;
                        distance = collisions.velocityOld;
                    }

                    float distanceToSlopeStart = 0;
                    if (slopeAngle != collisions.slopeAngleOld)
                    {
                        distanceToSlopeStart = hit.distance - skinWidth;
                        distance.x -= distanceToSlopeStart * directionX;
                    }
                    ClimbSlope(ref distance, slopeAngle);
                    distance.x += distanceToSlopeStart * directionX;
                }

                if (!collisions.climbingSlope || slopeAngle > maxClimbAngle)
                {
                    distance.x = (hit.distance - skinWidth) * directionX;
                    rayLength = hit.distance;

                    if (collisions.climbingSlope)
                    {
                        distance.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(distance.x);
                    }
                    collisions.left = directionX < 0;
                    collisions.right = directionX > 0;
                }
            }
        }
    }

    protected void VerticalCollisions(ref Vector2 distance) {
        float directionY = Mathf.Sign(distance.y);
        float rayLength = Mathf.Abs(distance.y) + skinWidth;

        for (int i = 0; i < verticalRayCount; i++) {
            Vector2 rayOrigin = directionY == -1 ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + distance.x);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.up * directionY, Color.red);

            if (hit) {

                if (hit.collider.tag == "Through") {
                    if (directionY == 1 || hit.distance == 0) {
                        continue; // skip collision detection as if we haven't collided w/anything - because it's a "through" obstacle
                    }

                    if (collisions.fallingThrough) {
                        continue;
                    }

                    if (playerInput.y == -1) {
                        collisions.fallingThrough = true;
                        Invoke("ResetFallingThrough", .5f);
                        continue;
                    }
                }
                distance.y = (hit.distance - skinWidth) * directionY;
                rayLength = hit.distance;

                if (collisions.climbingSlope) {
                    distance.x = distance.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(distance.x);
                }
                collisions.below = directionY < 0;
                collisions.above = directionY > 0;
            }
        }

        if (collisions.climbingSlope) {
            float directionX = Mathf.Sign(distance.x);
            rayLength = Mathf.Abs(distance.x) + skinWidth;
            Vector2 rayOrigin = ((directionX < 0) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) + Vector2.up * distance.y;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            if (hit) {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (slopeAngle != collisions.slopeAngle) {
                    distance.x = (hit.distance - skinWidth) * directionX;
                    collisions.slopeAngle = slopeAngle;
                }
            }
        }
    }

    void ClimbSlope(ref Vector2 distance, float slopeAngle)
    {
        float moveDistance = Mathf.Abs(distance.x);
        float climbVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
        if (distance.y <= climbVelocityY)
        {
            distance.y = climbVelocityY;
            distance.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * distance.x;

            // as long as we're "climbing a slope" we can assume we're already on the ground, so...
            collisions.below = true;
            collisions.climbingSlope = true;
            collisions.slopeAngle = slopeAngle;
        }
    }

    void DescendSlope(ref Vector2 distance)
    {
        float directionX = Mathf.Abs(distance.x);
        Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);
        if (hit)
        {
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            if (slopeAngle != 0 && slopeAngle <= maxDescendAngle)
            {
                if (Mathf.Sign(hit.normal.x) == directionX)
                {
                    if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(distance.x))
                    {
                        float moveDistance = Mathf.Abs(distance.x);
                        float descendVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
                        distance.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * distance.x;
                        distance.y -= descendVelocityY;

                        collisions.slopeAngle = slopeAngle;
                        collisions.descendingSlope = true;
                        collisions.below = true;
                    }
                }
            }
        }
    }

    void ResetFallingThrough() {
        collisions.fallingThrough = false;
    }

    public struct CollisionInfo {
        public bool left, right;
        public bool above, below;
        public bool climbingSlope;
        public bool descendingSlope;
        public int faceDir;
        public bool fallingThrough;

        public Vector2 velocityOld;

        public float slopeAngle, slopeAngleOld;

        public void Reset() {
            left = right = false;
            above = below = false;
            climbingSlope = descendingSlope = false;

            slopeAngleOld = slopeAngle;
            slopeAngle = 0;
        }
    }

}
