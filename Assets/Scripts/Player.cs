using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

[RequireComponent (typeof (Controller2D))]
[RequireComponent (typeof (SpriteRenderer))]
public class Player : MonoBehaviour
{

    public float maxJumpHeight = 4;
    public float minJumpHeight = 1;
    public float timeToJumpApex = .4f;
    public Vector2 wallJumpClimb;
    public Vector2 wallJumpOff;
    public Vector2 wallLeap;

    public float wallStickTime = .25f;
    float timeToWallUnstick;

    public float wallSlideSpeedMax = 3f;

    float accelerationTimeAirborne = .2f;
    float accelerationTimeGrounded = .1f;
    float moveSpeed = 6;
    float gravity;
    float maxJumpVelocity;
    float minJumpVelocity;
    Vector2 velocity;
    Sprite facingLeft, facingRight;
    float velocityXSmoothing;

    Controller2D controller;
    SpriteRenderer spriteRenderer;
       
    Vector2 directionalInput;
    bool wallSliding;
    int wallDirX;

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<Controller2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        Sprite[] sprites = Resources.LoadAll<Sprite>("misha-knight-1");
        facingLeft = FindSprite(sprites, "misha-knight-left");
        facingRight = FindSprite(sprites, "misha-knight-right");
        spriteRenderer.sprite = facingRight;

        gravity = - maxJumpHeight * 2 / (timeToJumpApex * timeToJumpApex);

        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);
        maxJumpVelocity = Mathf.Abs(gravity)* timeToJumpApex;
    }

    private Sprite FindSprite(Sprite[] sprites, string name) {
        foreach (Sprite sprite in sprites) {
            if (sprite.name == name) {
                return sprite;
            }
        }

        throw new KeyNotFoundException();
    }


    // Update is called once per frame
    void Update() {

        CalculateVelocity();
        HandleWallSliding();

        controller.Move(velocity * Time.deltaTime, directionalInput);
        if (controller.collisions.above || controller.collisions.below) {
            if (controller.collisions.slidingDownMaxSlope) {
                velocity.y -= controller.collisions.slopeNormal.y * gravity * Time.deltaTime;
            } else {
                velocity.y = 0;
            }
        }

        spriteRenderer.sprite = (velocity.x < 0) ? facingLeft: facingRight;
    }

    public void SetDirectionalInput(Vector2 input) {
        directionalInput = input;
    }

    public void OnJumpButtonDown() {
        if (wallSliding) {
            if (wallDirX == directionalInput.x) {
                velocity.x = -wallDirX * wallJumpClimb.x;
                velocity.y = wallJumpClimb.y;
            }
            else if (directionalInput.x == 0) {
                velocity.x = -wallDirX * wallJumpOff.x;
                velocity.y = wallJumpOff.y;
            }
            else {
                velocity.x = -wallDirX * wallLeap.x;
                velocity.y = wallLeap.y;
            }
        }
        if (controller.collisions.below) {
            if (controller.collisions.slidingDownMaxSlope) {
                if (directionalInput.x != -Mathf.Sign(controller.collisions.slopeNormal.x)) { // not jumping against maxSlope
                    velocity.y = maxJumpVelocity * controller.collisions.slopeNormal.y;
                    velocity.x = maxJumpVelocity * controller.collisions.slopeNormal.x;
                }
            }
            else {
                velocity.y = maxJumpVelocity;
            }
        }
    }

    public void OnJumpButtonUp() {
        if ((velocity.y > minJumpVelocity)) {
            velocity.y = minJumpVelocity;
        }
    }

    private void HandleWallSliding() {
        wallDirX = (controller.collisions.left) ? -1 : 1;
        wallSliding = false;

        if ((controller.collisions.left || controller.collisions.right) && !controller.collisions.below & velocity.y < 0) {

            wallSliding = true;

            if (velocity.y < -wallSlideSpeedMax) {
                velocity.y = -wallSlideSpeedMax;
            }

            if (timeToWallUnstick > 0) {
                velocityXSmoothing = 0;
                velocity.x = 0;

                if (directionalInput.x != wallDirX && directionalInput.x != 0) {
                    timeToWallUnstick -= Time.deltaTime;
                }
                else {
                    timeToWallUnstick = wallStickTime;
                }
            }
            else {
                timeToWallUnstick = wallStickTime;
            }
        }
    }

    private void CalculateVelocity() {
        float targetVelocityX = directionalInput.x * moveSpeed;
        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, controller.collisions.below ? accelerationTimeGrounded : accelerationTimeAirborne);
        velocity.y += gravity * Time.deltaTime;
    }
}
