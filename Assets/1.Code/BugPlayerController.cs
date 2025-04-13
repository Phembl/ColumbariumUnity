using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the player in Bug mode with small-scale movement
/// </summary>
public class BugPlayerController : BasePlayerController
{
    [Header("Bug Settings")]
    [Tooltip("Movement speed in Bug mode")]
    public float walkSpeed = 3f;

    protected override void Start()
    {
        base.Start();
        
        // Initialize bug-specific settings
        InitializeBugSettings();
    }
    
    private void InitializeBugSettings()
    {
        // Configure rigidbody
        rb.useGravity = true;
        rb.linearDamping = 0;
        
    }

    /// <summary>
    /// Handles movement physics for bug mode
    /// </summary>
    protected override void HandleMovement()
    {
        // Calculate movement direction
        Vector3 moveDirection = CalculateMoveDirection();
        
        // Apply movement
        Vector3 targetVelocity = moveDirection * walkSpeed;
        
        // Preserve vertical velocity
        targetVelocity.y = rb.linearVelocity.y;
        
        // Apply velocity
        rb.linearVelocity = targetVelocity;
    }
}