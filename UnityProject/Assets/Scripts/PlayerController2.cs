using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/* This structure is used for debug purposes. I store a list of these structures
 * and read from them in OnDrawGizmos. This is how I get away with having a function
 * call called DebugDrawSphere. It basically just adds to the list which is eventually 
 * processed by OnDrawGizmos
 * */
struct Draw_sphere_request
{
    public Vector3 pos;
    public float radius;
    public Color color;
}

public class PlayerController2 : MonoBehaviour
{
    #region InspectorVariables

    [Header("Movement")]
    [SerializeField]
    private float walkingSpeed = 1.4f;
    [SerializeField]
    private float runningSpeed = 3.3f;
    public float jumpHeight = 1.0f;
    public float gravityAcceleration = -9.81f;
    [Tooltip("This factor is applied to the movement of the character when in the air.")]
    public float airMovementFactor = 0.5f;
    [Tooltip("This is the rotation speed of the character when turning.")]
    public float rotationsPerMinute = 80.0f;
    [Tooltip("The maximum velocity of the player while in freefall.")]
    public float terminalVelocity = -53.0f;
    [Tooltip("This value overrides the step offset of the characterController script.")]
    public float stepOffset = 0.3f;
    [Tooltip("Should the player be aloud to jump?")]
    public bool canJump = false;

    [Header("Debug")]
    public bool showDirectionVector = false;
    public bool showGroundedHitPos = false;
    public bool showEdgeCheck = false;
    public bool showGroundDistanceCheck = false;
    public bool showStepCheck = false;
    public bool showSurfaceNomral = false;

    [Header("Restrictions")]
    [Tooltip("The player will not be able to walk up slopes steeper than this angle.")]
    public float slopeLimit = 55.0f;
    [Tooltip("The layers that the player can walk on.")]
    public LayerMask walkingLayerMask;
    [Tooltip("First sphere of the collision capsule.")]
    public Vector3 localCapsuleSphere1 = Vector3.zero;
    [Tooltip("Second sphere of the collision capsule.")]
    public Vector3 localCapsuleSphere2 = Vector3.zero;
    [Tooltip("The radius of the collision capsule.")]
    public float capsuleRadius = 0.5f;

    [Header("Other")]
    [Tooltip("Is the motion of this character driven by animations or script?.")]
    public bool rootMotion = true;
    [Tooltip("When calculating the position of the feet of the player, this value is added as an " +
        "offset to transform.position")]
    public float feetOffset = 0.0f;
    [Tooltip("Step search distance")]
    public float stepSearch = 0.1f;
    #endregion

    #region ConstantVariables
    private const float landingThreshold = 0.1f;
    private const float maintenanceThreshold = 0.5f;
    private const float tinyTolerance = 0.01f;
    private const float tolerance = 0.05f;
    #endregion

    #region PrivateVariables
    private bool ignoreInput = true; // NOTE(Reader): By default, the character is not activated
    private bool gravityLock = false; // When this is true, there are no grounded checks
    private bool grounded = false;
    private float verticalVelocity = 0.0f;
    private Vector3 airInertia = Vector3.zero;
    // NOTE(Reader): The inertial full stop works like this. If whilst in the air and the player hits a wall
    // due to their inertia, we are going to apply a full stop to their intertia!
    private bool inertiaFullStop = false;
    private Transform cameraTransform; // initialized in Start
    private Animator playerAnimator; // initialized in Start
    private CharacterController controller;
    private List<Draw_sphere_request> sphereRequestBuffer = new List<Draw_sphere_request>();
    #endregion

    #region PublicInterface
    public void Activate() { ignoreInput = false; }
    public void Deactivate() { ignoreInput = true; }
    public bool IsActivated() { return (!ignoreInput); }
    public Vector3 GetFeetPos() { return transform.position + Vector3.up * feetOffset; }
    public Vector3 GetMidsectionPos() { return GetFeetPos() + Vector3.up * GetHeight() / 2.0f; }
    public float GetHeight() { return Mathf.Abs(localCapsuleSphere2.y - localCapsuleSphere1.y) + capsuleRadius * 2.0f; }
    public float GetRadius() { return capsuleRadius; }
    public void SetWalkingSpeed(float walkingSpeed) { this.walkingSpeed = walkingSpeed; }
    public void DisableGravity() { gravityLock = true; }
    public void EnableGravity() { gravityLock = false; }
    public void SetRunningSpeed(float runningSpeed) { this.runningSpeed = runningSpeed; }

    public void TimedLock(float timeToLock)
    {
        IEnumerator corutine = TimedLockCo(timeToLock);
        StartCoroutine(corutine);
    }

    public void UpdatePlayer()
    {
        controller.stepOffset = 0.02f;
        controller.slopeLimit = slopeLimit;
        float scale = transform.localScale.x;
        controller.height = GetHeight() / scale;
        controller.radius = capsuleRadius / scale;
        controller.center = (GetMidsectionPos() - transform.position) / scale;
        playerAnimator.applyRootMotion = rootMotion;
    }

    public void DebugDrawSphere(Vector3 pos, float radius, Color color)
    {
        Draw_sphere_request sphereRequest;
        sphereRequest.pos = pos;
        sphereRequest.radius = radius;
        sphereRequest.color = color;
        sphereRequestBuffer.Add(sphereRequest);
    }
    #endregion

    #region PlayerController2Functions
    public void DebugControllerMove(Vector3 movementVector)
    {
        //if (movementVector.y > 0.0f)
            //Debug.Log("Warning: the player was moved upwards!");
        controller.Move(movementVector);
    }

    public IEnumerator TimedLockCo(float timeToWait)
    {
        ignoreInput = true;
        yield return new WaitForSeconds(timeToWait);
        ignoreInput = false;
        yield return null;
    }
   
    private bool IsWallFromNormal(Vector3 normal)
    {
        // NOTE(if the normal is pointing down at all, that means its a wall and you cannot walk on it)
        if (normal.y < 0.0f)
            return true;

        float angleToHorizontal = Mathf.Acos(Vector3.Dot(normal, Vector3.up)) * Mathf.Rad2Deg;
        if (angleToHorizontal >= slopeLimit)
            return true;

        return false;
    }

    private float GetGroundedThreshold()
    {
        return (grounded) ? maintenanceThreshold : landingThreshold;
    }

    // Returns true when the ground is directly below, false in every other case
    private void EdgeProbeGround(RaycastHit hit, Vector3 edgeWallNormal, out RaycastHit newHit)
    {
        newHit = new RaycastHit();
        newHit.distance = Mathf.Infinity;
        
        // Now that the player is standing on a ledge, we need to know the what the distance to the ground is from said ledge.
        // We are going to probe with a raycast to find out.
        Vector3 pushVector = new Vector3(hit.normal.x, 0.0f, hit.normal.z);
        pushVector = pushVector.normalized;
        Vector3 originPoint = hit.point + pushVector * tinyTolerance;

        // First, prior to raycasting along the surface, we are going to do a straight raycast down to see if we can grab some
        // ground!
        RaycastHit groundHit;
        bool foundGround = false;
        if (Physics.Raycast(originPoint, Vector3.down, out groundHit, Mathf.Infinity, walkingLayerMask))
        {
            if (!IsWallFromNormal(groundHit.normal))
            {
                foundGround = true;
                newHit = groundHit;
            }
        }

        if (!foundGround)
        {
            // Calculate the direction vector down the slope so we can do our raycast
            Vector3 ny = Vector3.Cross(edgeWallNormal, Vector3.down);
            Vector3 slopeVec = Vector3.Cross(ny, edgeWallNormal);

            if (Physics.Raycast(originPoint, slopeVec, out groundHit, Mathf.Infinity, walkingLayerMask))
            {
                if (!IsWallFromNormal(groundHit.normal))
                {
                    newHit = groundHit;
                }
            }
        }

        if (newHit.distance != Mathf.Infinity && showEdgeCheck)
            Debug.DrawLine(originPoint, groundHit.point, Color.black);
    }

    // TODO(BluCloos): There is a case here where the ray might not hit the actual surface but instead a completely
    // diff surface. To maintain sanity, I think it might be useful to verfiy the distance of these points to
    // some "expected" distance which we can calcualate.
    private bool EdgeCheck(RaycastHit hit, out RaycastHit nearHit, out RaycastHit farHit)
    {
        nearHit = new RaycastHit();
        farHit = new RaycastHit();
        nearHit.distance = Mathf.Infinity;
        farHit.distance = Mathf.Infinity;
        bool nearHitValid = false;
        bool farHitValid = false;

        {
            Vector3 newOrigin = hit.point + hit.normal * tolerance;
            Vector3 rayDirection = -hit.normal;
            Vector3 ny = Vector3.Cross(hit.normal, Vector3.up);
            // NOTE(Reader): the delta direction is the vector who is perpendicular to the normal but 
            // coplanar to the plane defined by the normal and VEctor3.up
            Vector3 deltaDirection = Vector3.Cross(ny, hit.normal);
            Vector3 origin1 = newOrigin + deltaDirection * tolerance;
            Vector3 origin2 = newOrigin - deltaDirection * tolerance;

            RaycastHit localNearHit;
            RaycastHit localFarHit;

            nearHitValid = Physics.Raycast(origin1, rayDirection, out localNearHit, Mathf.Infinity, walkingLayerMask);
            farHitValid = Physics.Raycast(origin2, rayDirection, out localFarHit, Mathf.Infinity, walkingLayerMask);

            if (nearHitValid)
                nearHit = localNearHit;

            if (farHitValid)
                farHit = localFarHit;

            if (showEdgeCheck)
            {
                Debug.DrawLine(origin1, nearHit.point, Color.cyan);
                Debug.DrawLine(origin2, farHit.point, Color.magenta);
            }
        }

        return nearHitValid && farHitValid && !IsWallFromNormal(nearHit.normal) && IsWallFromNormal(farHit.normal);
    }

    // TODO(BluCloos): I honeslty feel like this boi just needs a better interface tbh.
    // there are just so many out params!
    // NOTE(Reader): This guy also goofs you up the stairs
    private bool GroundedAndClamp(bool shouldLedgePush, out List<RaycastHit> hits, out float groundDistance, out bool wasPushed)
    {
        // Default the out params in the case they never get set
        groundDistance = Mathf.Infinity;
        hits = new List<RaycastHit>(); // No hits for you!
        wasPushed = false;

        Vector3 feetPos = GetFeetPos();
        Vector3 rayOrigin = feetPos + Vector3.up * (GetHeight() / 2.0f);
        Vector3 feetSpherePos = feetPos + Vector3.up * GetRadius();

        RaycastHit hit;
        if (Physics.SphereCast(rayOrigin, GetRadius(), Vector3.down, out hit, Mathf.Infinity, walkingLayerMask))
        {
            hits.Add(hit); // Default hit is always at index 1

            // NOTE(Reader): The values below are default unless otherwise set by the various cases below
            float delta = Mathf.Infinity;
            Vector3 hitSpherePos = rayOrigin + Vector3.down * hit.distance;
            Color hitColor = Color.red;
            float margin = GetGroundedThreshold();
            bool isEdge = false;
            // These adjustment factors are basically only used by the slope pushing!
            Vector3 adjustmentVector = Vector3.zero;

            void SetDelta()
            {
                delta = Mathf.Abs(hitSpherePos.y - feetSpherePos.y);
            }

            // NOTE(BluCloos): The job of the cases below is to write the delta, setup the hitSpherePos, and add hits 

            // So the first thing I am going to do is to check if we are on perfectly flat ground. If this is the case, 
            // there is no concern for any other case. We are just like, on the ground bro.
            if (hit.normal != Vector3.up)
            {
                // In every other scenario, there are 4 cases
                /* 1. Standing on generic sloped ground
                 * 2. Standing on a slope that is too steep
                 * 3. Standing on a ledge where the ground meets a wall
                 * 4. The sphereCast caught a wall before it caught the ground that you are actually on
                 * */

                // EDGE CHECK
                RaycastHit nearHit;
                RaycastHit farHit;
                isEdge = EdgeCheck(hit, out nearHit, out farHit);
                hits.Add(nearHit);
                hits.Add(farHit);

                if (isEdge)
                {
                    if (shouldLedgePush)
                    {
                        wasPushed = true;
                        Vector3 pushVector = new Vector3(farHit.normal.x, 0.0f, farHit.normal.z);
                        pushVector = pushVector.normalized * Time.deltaTime * 4.0f;
                        adjustmentVector = pushVector;
                    }
                    
                    /*
                    RaycastHit groundHit;
                    EdgeProbeGround(hit, farHit.normal, out groundHit);

                    if (groundHit.distance != Mathf.Infinity)
                    {
                        hits.Add(groundHit);
                        hitSpherePos = groundHit.point + capsuleRadius * Vector3.up;
                        hitColor = Color.cyan;
                        SetDelta();
                    }*/

                    SetDelta();
                }
                else if (IsWallFromNormal(hit.normal))
                {
                    // Since the sphere cast has hit a wall we are going to assume that it has simply missed the ground beneath us,
                    // raycast below to find out what's there
                    RaycastHit pushHit = hit;
                    if (farHit.distance != Mathf.Infinity)
                        pushHit = farHit;

                    if (showSurfaceNomral)
                        Debug.DrawLine(hit.point, hit.point + hit.normal, Color.magenta);

                    Vector3 GenPushVector(Vector3 normal)
                    {
                        Vector3 pushVector = new Vector3(normal.x, 0.0f, normal.z);
                        pushVector = pushVector.normalized;
                        return pushVector * Time.deltaTime * 2.0f;
                    }

                    wasPushed = true;
                    adjustmentVector = GenPushVector(pushHit.normal);

                    if (adjustmentVector == Vector3.zero)
                    {
                        // Oh no, this is obviously bad, what the heck do we do!
                        // easy, just use the near hit if its valid
                        if (nearHit.distance != Mathf.Infinity)
                        {
                            adjustmentVector = GenPushVector(nearHit.normal);
                            if (adjustmentVector == Vector3.zero)
                            {
                                // oh fuck shit, what the heck do we do now?
                                // just push em along transform.forward...
                                adjustmentVector = GenPushVector(transform.forward);
                                Debug.LogWarning("Was forced to use tranform.forward whilst " +
                                    "pushing!!");
                            }
                        }
                    }

                    RaycastHit groundHit;
                    if (Physics.Raycast(rayOrigin, Vector3.down, out groundHit, Mathf.Infinity, walkingLayerMask))
                    {
                        if (!IsWallFromNormal(groundHit.normal))
                        {
                            hits.Add(groundHit);
                            hitSpherePos = groundHit.point + capsuleRadius * Vector3.up;
                            hitColor = Color.green;
                            SetDelta();
                        }
                    }
                }
                else
                {
                    SetDelta();
                }
            }
            else
            {
                SetDelta();
            }

            groundDistance = delta;

            // Do some debugging of the 'new' hit position!
            RaycastHit latestHit = hits[hits.Count - 1];
            if (showGroundedHitPos)
            {
                DebugDrawSphere(hit.point, 0.1f, Color.red);
                if (hits.Count == 4)
                    DebugDrawSphere(latestHit.point, 0.1f, hitColor);
            } 

            // NOTE(BluCloos): Now that there is no normal check, these things needs to be done proper prior to this check
            if (delta <= margin)
            {
                // The player is said to be grounded! Clamp that bitch!
                // Also note I do a clamp to make sure we never clip the player up!
                Vector3 moveVec = Mathf.Min(hitSpherePos.y - feetSpherePos.y, 0.0f) * Vector3.up;
                DebugControllerMove(moveVec);
                return true;
            }
            else if (wasPushed)
            {
                wasPushed = false;

                // first things first, we are going to grab the distance to the ACTUAL hit point
                // we only want these pushes to apply if we are within the correct distance to the "real"
                // hit point.
                Vector3 ogHitSphere = hit.point + hit.normal * GetRadius();
                float newDelta = Vector3.Distance(ogHitSphere, feetSpherePos);
                if (newDelta <= landingThreshold)
                {
                    DebugControllerMove(adjustmentVector);
                    wasPushed = true;
                }
            }
        }
        else
        {
            Debug.LogWarning("No hit in ground check! Player must have fallen off the map somehow!");
        }
        
        return false;
    } 

    private void StepAndClamp(Vector3 directionVector)
    {
        // Okay so here is the plan boys. Shoot the capusle cast forward and make sure we got an edge. Then
        // verify against the step distance. If all is ok, boom! Do the step! Wow, abstraction rlly makes things super simple.
        Vector3 origin = GetMidsectionPos() + directionVector * stepSearch;
        RaycastHit stepHit;
        if (Physics.SphereCast(origin, capsuleRadius, Vector3.down, out stepHit, Mathf.Infinity, walkingLayerMask))
        {
            if (showStepCheck)
            {
                DebugDrawSphere(stepHit.point, 0.1f, Color.blue);
            }

            RaycastHit nearHit;
            RaycastHit farHit;
            if (EdgeCheck(stepHit, out nearHit, out farHit))
            {
                Vector3 hitSpherePos = stepHit.point + Vector3.up * capsuleRadius;
                Vector3 feetSpherePos = GetFeetPos() + Vector3.up * GetRadius();
                float signedDelta = Mathf.Abs(hitSpherePos.y - feetSpherePos.y);
                float unsignedDelta = Mathf.Abs(signedDelta);

                if (unsignedDelta <= stepOffset)
                {
                    Vector3 moveVector = Vector3.up * signedDelta;
                    DebugControllerMove(moveVector);
                }
            } 
        }
    }

    #endregion

    #region UnityCallbacks
    void OnDrawGizmos()
    {
        // Draw the grounded flag gizmo
        {
            if (grounded)
                Gizmos.color = Color.green;
            else
                Gizmos.color = Color.red;
            Gizmos.DrawCube(GetFeetPos() + Vector3.up * (GetHeight() + 0.2f), new Vector3(0.25f, 0.25f, 0.25f));
        }

        // Draw the capsule collider gizmos
        {
            Gizmos.color = Color.green;
            Vector3 point1 = transform.position + localCapsuleSphere1;
            Vector3 point2 = transform.position + localCapsuleSphere2;
            Gizmos.DrawWireSphere(point1, capsuleRadius);
            Gizmos.DrawWireSphere(point2, capsuleRadius);
        }

        // Draw the requested debug spheres
        for (uint i = 0; i < sphereRequestBuffer.Count; i++)
        {
            Draw_sphere_request sphereRequest = sphereRequestBuffer[(int)i];
            Gizmos.color = sphereRequest.color;
            Gizmos.DrawWireSphere(sphereRequest.pos, sphereRequest.radius);
            sphereRequestBuffer.Remove(sphereRequest);
        }
    }

    void Awake()
    {
        playerAnimator = GetComponent<Animator>();
        cameraTransform = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Transform>();
        controller = GetComponent<CharacterController>();

        if (playerAnimator == null)
            Debug.LogWarning("No animator component attached!");

        if (cameraTransform == null)
            Debug.LogWarning("Unable to find the main camera!");

        if (controller == null)
        {
            Debug.LogError("No character controller!");
            Destroy(gameObject);
        }

        // Override specific character controller functionality
        UpdatePlayer();
    }

    void Update()
    {
        if (cameraTransform != null && playerAnimator != null)
        {
            // Retrieve the movement input from the user
            float horizontal;
            float vertical;
            {
                horizontal = Input.GetAxisRaw("Horizontal");
                vertical = Input.GetAxisRaw("Vertical");

                if (ignoreInput)
                {
                    horizontal = 0.0f;
                    vertical = 0.0f;
                }
            }

            // Calculate the direction vector (the direction that the player is trying to move in) 
            // based on the direction that the camera is facing.
            Vector3 directionVector;
            {
                float cameraForwardAngle = cameraTransform.eulerAngles.y * Mathf.Deg2Rad;
                float cameraRightAngle = cameraForwardAngle + Mathf.PI / 2.0f;
                Vector3 forwardDirection = new Vector3(Mathf.Sin(cameraForwardAngle), 0.0f, Mathf.Cos(cameraForwardAngle));
                Vector3 rightDirection = new Vector3(Mathf.Sin(cameraRightAngle), 0.0f, Mathf.Cos(cameraRightAngle));
                directionVector = Vector3.Normalize(forwardDirection * vertical + rightDirection * horizontal);

                if (showDirectionVector)
                    Debug.DrawLine(GetMidsectionPos(), GetMidsectionPos() + directionVector, Color.cyan);
            }

            // Is the playing trying to run?
            bool moving = (Mathf.Abs(vertical) > Mathf.Epsilon) || Mathf.Abs(horizontal) > Mathf.Epsilon;
            bool running = false;
            if (Input.GetButton("Run") && moving)
                running = true;

            // After having moved the last frame, we are going to check if that has changed 
            // the state of our grounding.
            float groundDistance;
            bool wasPushed;
            {
                List<RaycastHit> hits; 
                bool localGrounded = GroundedAndClamp(false, out hits, out groundDistance, out wasPushed);

                if (showGroundDistanceCheck)
                    Debug.DrawLine(GetFeetPos(), GetFeetPos() + Vector3.down * groundDistance);

                // The case below is when we were just grounded, and now we are not
                if (grounded == true && localGrounded == false)
                {
                    airInertia = transform.forward * ((running) ? runningSpeed : 0.0f);
                    playerAnimator.SetFloat("groundDistance", 0.0f);
                }

                // Write the ground distance!
                float localGroundDistance = playerAnimator.GetFloat("groundDistance");
                if (!localGrounded)
                {
                    if (groundDistance > localGroundDistance)
                        playerAnimator.SetFloat("groundDistance", groundDistance);
                }

                grounded = localGrounded; // actually commit the local boi to the global boi
            }

            // NOTE(Reader): Since we are not using the baked in step feature of the character controller (this was found to have
            // some conflicting issues with my ground detection system), we must manually perform the step clamping.
            // of course, we may only do the step clamping provided we are actually on the ground. Although, it may be worth removing the
            // grounded check because it could have some interesting results. For example,  clipping to a ledge if you didn't quite make the jump
            // (although I am quite sure the root motion sorta does the clipping already).
            if (grounded)
                StepAndClamp(directionVector);

            // Set up the movement vector!
            Vector3 moveVector = Vector3.zero;
            {
                // NOTE(Reader): In order to ensure that the characterController
                // respects the slopeLimit we have to make sure that there is a constant downwards
                // velocity being applied to the character.
                float zeroGravity = -0.01f;
                verticalVelocity = (grounded) ? zeroGravity : Mathf.Max(verticalVelocity + gravityAcceleration * Time.deltaTime, terminalVelocity);

                if (gravityLock)
                    verticalVelocity = zeroGravity;

                if (Input.GetButtonDown("Jump") && grounded && !ignoreInput)
                {
                    verticalVelocity = Mathf.Sqrt(-2.0f * gravityAcceleration * jumpHeight);
                    // NOTE(Reader): We set the grounding to false so that during the next frame
                    // the grounding procedure uses a smaller threshold (This is needed so that 
                    // we don't clip back into the ground!)
                    grounded = false;

                    // Also note that we are going to flick the player up for this frame so that you know, they actually leave the ground!
                    DebugControllerMove(Vector3.up * (landingThreshold + tinyTolerance));
                    airInertia = transform.forward * ((running) ? runningSpeed : 0.0f);
                    playerAnimator.SetFloat("groundDistance", 0.0f);
                    //playerAnimator.SetBool("Jump", true);
                }

                // application of the airIntertia!
                if (!grounded)
                {
                    if (wasPushed)
                    {
                        inertiaFullStop = true;
                        airInertia = Vector3.zero;
                    }
                }

                if (!grounded && !inertiaFullStop)
                    moveVector += airInertia;

                moveVector += Vector3.up * verticalVelocity;

                if (grounded)
                    moveVector += directionVector * ((running) ? runningSpeed : walkingSpeed);

                moveVector *= Time.deltaTime;
            }

            // Update the animation states (among other things) based on the character movement
            {
                if (grounded)
                {
                    playerAnimator.SetBool("Grounded", true);
                    airInertia = Vector3.zero;
                    inertiaFullStop = false;
                }
                else
                {
                    playerAnimator.SetBool("Grounded", false);
                }

                if (!grounded)
                {
                    Vector3 localMoveVector = new Vector3(moveVector.x, 0.0f, moveVector.z) / Time.deltaTime;
                    playerAnimator.SetFloat("V_Move", localMoveVector.magnitude / runningSpeed);
                }
                else
                {
                    float V_Move = (moving) ? ( (running) ? 1.0f : 0.5f ) : 0.0f;
                    playerAnimator.SetFloat("V_Move", V_Move);
                }
            }

            // Actually move the character using the movement vector. Oh, and also account for any root motion magic!
            if (rootMotion && grounded)
            {
                Vector3 localMoveVector = new Vector3(0.0f, moveVector.y, 0.0f);
                DebugControllerMove(localMoveVector);
            }
            else
            {
                DebugControllerMove(moveVector);
            }

            // Rotation code for when the character is trying to move in a direction that the character is not facing.
            // NOTE(Reader): The player is only going to do the rotation while they are grounded.
            // TODO(BluCloos): Document this code so people know what the heck they are reading!
            // btw this stuff is the rotation stuff to get the character to move in a direction relative to the camera rotation.
            if (moving && grounded)
            {
                float rotationAngle = (rotationsPerMinute / 60.0f) * 360.0f * Time.deltaTime;
                float safetyAngles = 2;

                float currentRotation = transform.eulerAngles.y;
                if (currentRotation < 0.0f)
                    currentRotation += 360.0f;

                float directionVectorAngle = Mathf.Atan2(directionVector.x, directionVector.z) * Mathf.Rad2Deg;
                float directionVectorAngleOG = directionVectorAngle;

                if (directionVectorAngle < 0.0f)
                    directionVectorAngle += 360.0f;

                float upperBound = directionVectorAngle + rotationAngle + safetyAngles;
                if (upperBound > 360.0f)
                    upperBound -= 360.0f;

                float lowerBound = directionVectorAngle - (rotationAngle + safetyAngles);
                if (lowerBound < 0.0f)
                    lowerBound += 360.0f;

                bool inRange;
                if (upperBound < lowerBound)
                    inRange = (currentRotation <= upperBound) || (currentRotation >= lowerBound);
                else
                    inRange = (currentRotation <= upperBound) && (currentRotation >= lowerBound);

                bool rotateClockwise = true;
                float clockWiseRotation = (directionVectorAngle > currentRotation) ? directionVectorAngle - currentRotation : (360.0f - currentRotation) + directionVectorAngle;
                if (clockWiseRotation >= 360.0f - clockWiseRotation)
                    rotateClockwise = false;

                if (inRange)
                {
                    transform.eulerAngles = new Vector3(0.0f, directionVectorAngleOG, 0.0f);
                }
                else
                {
                    if (rotateClockwise)
                        transform.Rotate(Vector3.up * rotationAngle);
                    else
                        transform.Rotate(Vector3.up * -rotationAngle);
                }
            }
        }
    }
    #endregion
}
