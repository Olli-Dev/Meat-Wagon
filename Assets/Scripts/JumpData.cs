using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpData : MonoBehaviour
{
    [Header("Jump info")]
    // The scale effects how long the cars will stay airborn when they hit a jump
    public float jumpHeightScale = 1.0f;

    // How much should the cars be pushed forward when they hit the jump
    public float jumpPushScale = 1.0f;
}
