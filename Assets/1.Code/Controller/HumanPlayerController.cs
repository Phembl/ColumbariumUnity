using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the player in Human mode with basic walking movement
/// </summary>
public class HumanPlayerController : BasePlayerController
{
    [Header("Human Settings")]
    [Tooltip("Movement speed in Human mode")]
    public float walkSpeed = 6f;
    


    protected override void Start()
    {
        base.Start();
        
        // Initialize human-specific settings
        InitializeHumanSettings();
    }
    
    private void InitializeHumanSettings()
    {
        // Configure rigidbody
        rb.useGravity = true;
        rb.linearDamping = 0;

    }

    /// <summary>
    /// Handles movement physics for human mode
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