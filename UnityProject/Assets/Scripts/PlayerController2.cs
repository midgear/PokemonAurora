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

    [Header("Debug")]
    public bool showDirectionVector = false;
    public bool showGroundedHitPos = false;
    public bool showEdgeCheck = false;
    public bool showGroundDistanceCheck = false;

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
    // NOTE(Reader): I do not check if the playerCollider exists in these functions because
    // at the time of their calling it will always exist as I delete the player if 
    // I was unable to find a capsule collider.
    public float GetHeight() { return Mathf.Abs(localCapsuleSphere2.y - localCapsuleSphere1.y) + capsuleRadius * 2.0f; }
    public float GetRadius() { return capsuleRadius; }
    public void SetWalkingSpeed(float walkingSpeed) { this.walkingSpeed = walkingSpeed; }
    public void DisableGravity() { gravityLock = true; }
    public void EnableGravity() { gravityLock = false; }
    public void SetRunningSpeed(float runningSpeed)
    {
        if (playerAnimator != null && rootMotion)
        {
            playerAnimator.speed = runningSpeed;
        }

        this.runningSpeed = runningSpeed;
    }

    public void TimedLock(float timeToLock)
    {
        IEnumerator corutine = TimedLockCo(timeToLock);
        StartCoroutine(corutine);
    }

    #endregion

    #region PlayerController2Functions
    public IEnumerator TimedLockCo(float timeToWait)
    {
        ignoreInput = true;
        yield return new WaitForSeconds(timeToWait);
        ignoreInput = false;
        yield return null;
    }

    private void DebugDrawSphere(Vector3 pos, float radius, Color color)
    {
        Draw_sphere_request sphereRequest;
        sphereRequest.pos = pos;
        sphereRequest.radius = radius;
        sphereRequest.color = color;
        sphereRequestBuffer.Add(sphereRequest);
    }

    private Vector3 ProjectVectorOnPlane(Vector3 planeNormal, Vector3 vector)
    {
        return vector - (Vector3.Dot(vector, planeNormal) * planeNormal);
    }

    // NOTE(BluCloos): I stole this codes because I could not be bothered to write it myself.
    private bool SimulateSphereCast(Vector3 groundNormal, out RaycastHit hit)
    {
        float groundAngle = Vector3.Angle(groundNormal, Vector3.up) * Mathf.Deg2Rad;

        Vector3 secondaryOrigin = GetFeetPos() + Vector3.up * tolerance;

        if (!Mathf.Approximately(groundAngle, 0))
        {
            float horizontal = Mathf.Sin(groundAngle) * GetRadius();
            float vertical = (1.0f - Mathf.Cos(groundAngle)) * GetRadius();

            // Retrieve a vector pointing up the slope
            Vector3 r2 = Vector3.Cross(groundNormal, Vector3.down);
            Vector3 v2 = -Vector3.Cross(r2, groundNormal);

            secondaryOrigin += ProjectVectorOnPlane(Vector3.up, v2).normalized * horizontal + Vector3.up * vertical;
        }

        //Debug.DrawRay(secondaryOrigin, Vector3.down, Color.magenta);
        if (Physics.Raycast(secondaryOrigin, Vector3.down, out hit, Mathf.Infinity, walkingLayerMask))
        {
            // Remove the tolerance from the distance travelled
            hit.distance -= tolerance + tinyTolerance;
            return true;
        }
        else
        {
            return false;
        }
    }

    private float WallAngleFromNormal(Vector3 normal)
    {
        float wallAngle = Mathf.Acos(Vector3.Dot(normal, Vector3.up)) * Mathf.Rad2Deg;
        return wallAngle;
    }

    private bool IsWallFromNormal(Vector3 normal)
    {
        float wallAngle = WallAngleFromNormal(normal);
        if (wallAngle > slopeLimit)
            return true;
        else
            return false;
    }

   /*private bool SlopeInFrontGround(out Vector3 slopeNormal)
    {
        Vector3 feetPos = GetFeetPos();
        Vector3 feetSpherePos = feetPos + Vector3.up * (GetHeight() / 2.0f) + Vector3.down * (controller.stepOffset + 0.1f);

        RaycastHit hit;
        if (Physics.Raycast(feetSpherePos, transform.forward, out hit, controller.height, walkingLayerMask))
        {
            DebugDrawSphere(hit.point, 0.1f, Color.red);
            float normalAngleToVertical = WallAngleFromNormal(hit.normal);
            
            // TODO(BluCloos): Make this work for inverse walls
            float theta = 90.0f - normalAngleToVertical;
            float distanceThreshold = Mathf.Tan(theta * Mathf.Deg2Rad) * (feetSpherePos.y - feetPos.y) + landingThreshold;
            Debug.DrawLine(feetSpherePos, feetSpherePos + transform.forward * distanceThreshold, Color.green);

            Vector3 hitSpherePos = hit.point - transform.forward * controller.radius;
            //Vector3 hitSpherePos = feetSpherePos + transform.forward * hit.distance;
            float deltaDistance = Vector3.Distance(feetSpherePos, hitSpherePos);
            if (deltaDistance <= distanceThreshold && normalAngleToVertical > controller.slopeLimit)
            {
                slopeNormal = hit.normal;
                return true;
            }
        }

        slopeNormal = Vector3.zero;
        return false;
    }
    */

    private bool SlopeCheck(Vector3 cMoveVector, out Vector3 slopeNormal)
    {
        RaycastHit[] hits = CollisionCheck(cMoveVector);

        // Here is some debug stuff, basically we are just going to draw all those gorgeous hits
        // that we got.
        /*
        for (int i = 0; i < hits.Length; i++)
        {
            DebugDrawSphere(hits[i].point, 0.2f, Color.red);
        }
        */

        slopeNormal = Vector3.zero;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (IsWallFromNormal(hit.normal))
            {
                // we have to make sure that the hit distance is sane
                // so that we know if we are even touching the wall
                if (hit.distance <= landingThreshold)
                {
                    // Okay so the above seems like a working (untested) cheese for now 
                    slopeNormal = hit.normal;
                    return true;
                }
            }
        }

        return false;
    }

    private float GetGroundedThreshold()
    {
        return (grounded) ? maintenanceThreshold : landingThreshold;
    }

    private bool GroundedAndClamp(out List<RaycastHit> hits, out float groundDistance, out bool onLedge)
    {
        // Default the out params in the case they never get set
        groundDistance = Mathf.Infinity;
        hits = new List<RaycastHit>(); // No hits for you!
        onLedge = false;

        Vector3 feetPos = GetFeetPos();
        Vector3 rayOrigin = feetPos + Vector3.up * (GetHeight() / 2.0f);
        Vector3 feetSpherePos = feetPos + Vector3.up * GetRadius();

        RaycastHit hit;
        if (Physics.SphereCast(rayOrigin, GetRadius(), Vector3.down, out hit, Mathf.Infinity, walkingLayerMask))
        {
            hits.Add(hit); // default hit is always at index 1

            // NOTE(Reader): the values below are default unless otherwise set by the various cases below
            float delta = Mathf.Infinity;
            Vector3 hitSpherePos = rayOrigin + Vector3.down * hit.distance;
            Color hitColor = Color.red;
            
            // These adjustment factors are basically only used by the slope pushing!
            bool shouldAdjust = false;
            Vector3 adjustmentVector = Vector3.zero;

            void WriteDelta()
            {
                delta = Vector3.Distance(hitSpherePos, feetSpherePos);
            }

            // NOTE(BluCloos): The job of the cases below is to write the delta, setup the hitSpherePos, and 
            // set the hit if necessary, also the groundDistance.

            // So the first thing I am going to do is to check if we are on perfectly flat ground. If this is the case, 
            // there is no concern for any other case. We are just like, on the ground bro.
            if (hit.normal == new Vector3(0.0f, 1.0f, 0.0f))
            {
                WriteDelta();
                groundDistance = delta;
            }
            else
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
                bool nearHitValid = false;
                bool farHitValid = false;
                {
                    Vector3 newOrigin = hit.point + hit.normal * 0.2f;
                    Vector3 rayDirection = -hit.normal;
                    Vector3 ny = Vector3.Cross(hit.normal, Vector3.up);
                    // NOTE(Reader): the delta direction is the vector who is perpendicular to the normal but 
                    // coplanar to the plane defined by the normal and VEctor3.up
                    Vector3 deltaDirection = Vector3.Cross(ny, hit.normal);
                    Vector3 origin1 = newOrigin + deltaDirection * 0.2f;
                    Vector3 origin2 = newOrigin - deltaDirection * 0.2f;
                    
                    nearHitValid = Physics.Raycast(origin1, rayDirection, out nearHit, Mathf.Infinity, walkingLayerMask);
                    farHitValid = Physics.Raycast(origin2, rayDirection, out farHit, Mathf.Infinity, walkingLayerMask);

                    hits.Add(nearHit);
                    hits.Add(farHit);

                    if (showEdgeCheck)
                    {
                        Debug.DrawLine(origin1, nearHit.point, Color.cyan);
                        Debug.DrawLine(origin2, farHit.point, Color.magenta);
                    }
                }

                // TODO(BluCloos): There is a case here where the ray might not hit the actual surface but instead a completely
                // diff surface. To maintain sanity, I think it might be useful to verfiy the distance of these points to
                // some "expected" distance which we can calcualate.
                if (nearHitValid && farHitValid && !IsWallFromNormal(nearHit.normal) && IsWallFromNormal(farHit.normal))
                {
                    // STANDING ON A LEDGE
                    onLedge = true;

                    // Now that the player is standing on a ledge, we need to know the what the distance to the ground is from said ledge.
                    // We are going to probe with a raycast to find out.
                    Vector3 pushVector = new Vector3(hit.normal.x, 0.0f, hit.normal.z);
                    pushVector = pushVector.normalized;
                    Vector3 originPoint = hit.point + pushVector * tinyTolerance;

                    void ProcessHitGround(RaycastHit _groundHit)
                    {
                        hitSpherePos = _groundHit.point + capsuleRadius * Vector3.up;
                        WriteDelta();
                        hitColor = Color.cyan;

                        if (showEdgeCheck)
                        {
                            Debug.DrawLine(originPoint, _groundHit.point, Color.black);
                        }
                    }

                    // First, prior to raycasting along the surface, we are going to do a straight raycast down to see if we can grab some
                    // ground!
                    RaycastHit groundHit;
                    bool foundGround = false;
                    if (Physics.Raycast(originPoint, Vector3.down, out groundHit, Mathf.Infinity, walkingLayerMask))
                    {
                        if (!IsWallFromNormal(groundHit.normal))
                        {
                            foundGround = true;
                            hits.Add(groundHit);
                            ProcessHitGround(groundHit);
                            groundDistance = Mathf.Abs(hitSpherePos.y - feetSpherePos.y);
                        }
                    }

                    if (!foundGround)
                    {
                        // Calculate the direction vector down the slope so we can do our raycast
                        Vector3 ny = Vector3.Cross(farHit.normal, Vector3.down);
                        Vector3 slopeVec = Vector3.Cross(ny, farHit.normal);

                        if (Physics.Raycast(originPoint, slopeVec, out groundHit, Mathf.Infinity, walkingLayerMask))
                        {
                            hits.Add(groundHit);
                            ProcessHitGround(groundHit);
                            Vector3 deltaVec = groundHit.point - originPoint;
                            groundDistance = Vector3.Dot(deltaVec, Vector3.down);
                        }
                    }
                }
                else if (IsWallFromNormal(hit.normal))
                {
                    // Since the sphere cast has hit a wall we are going to assume that it has simply missed the ground beneath us,
                    // raycast below to find out what's there
                    RaycastHit pushHit = hit;
                    if (farHitValid)
                        pushHit = farHit;

                    Vector3 pushVector = new Vector3(pushHit.normal.x, 0.0f, pushHit.normal.z);
                    pushVector = pushVector.normalized;
                    shouldAdjust = true;
                    adjustmentVector = pushVector * Time.deltaTime * 1.3f;

                    RaycastHit groundHit;
                    if (Physics.Raycast(rayOrigin, Vector3.down, out groundHit, Mathf.Infinity, walkingLayerMask))
                    {
                        hits.Add(groundHit);
                        hitSpherePos = groundHit.point + capsuleRadius * Vector3.up;
                        WriteDelta();
                        hitColor = Color.green;

                        if (showEdgeCheck)
                        {
                            Debug.DrawLine(rayOrigin, groundHit.point, Color.black);
                        }

                        groundDistance = Mathf.Abs(hitSpherePos.y - feetSpherePos.y);
                    }
                }
                else
                {
                    WriteDelta();
                    // NOTE(BluCloos): The ground distance is set as such 
                    // due to the fact that we are not on level ground and we are doing a sphere cast.
                    // we need to take the deltas between the sphere positions, not the 
                    // actual hit and foot pos, because this is not perfectly vertical.
                    groundDistance = Mathf.Abs(hitSpherePos.y - feetSpherePos.y);
                }
            }

            // Do some debugging of the 'new' hit position!
            RaycastHit latestHit = hits[hits.Count - 1];
            if (showGroundedHitPos)
            {
                DebugDrawSphere(hit.point, 0.1f, Color.red);
                if (hits.Count == 4)
                    DebugDrawSphere(latestHit.point, 0.1f, hitColor);
            } 

            // TODO(BluCloos): Is the way I am checking the normal here alright?
            float margin = GetGroundedThreshold();
            if (delta <= margin && !IsWallFromNormal(latestHit.normal))
            {
                // The player is said to be grounded! Clamp that bitch!
                // Also note I do a clamp to make sure we never clip the player up!
                Vector3 moveVec = Mathf.Min((hitSpherePos.y - feetSpherePos.y), 0.0f) * Vector3.up;
                controller.Move(moveVec);
                return true;
            }
            else if (shouldAdjust)
            {
                controller.Move(adjustmentVector);
            }
        }
        else
        {
            Debug.Log("Warning: No hit in ground check! Player must have fallen off the map somehow!");
        }
        
        return false;
    }

    private RaycastHit[] CollisionCheck(Vector3 directionVector)
    {
        Vector3 point1 = transform.position + localCapsuleSphere1;
        Vector3 point2 = transform.position + localCapsuleSphere2;
        RaycastHit[] hits = Physics.CapsuleCastAll(point1, point2, capsuleRadius, directionVector, GetHeight(), walkingLayerMask);
        return hits;
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
        playerAnimator.applyRootMotion = rootMotion;
        controller = GetComponent<CharacterController>();

        if (playerAnimator == null)
            Debug.Log("Warning: No animator component attached!");

        if (cameraTransform == null)
            Debug.Log("Warning: Unable to find the main camera!");

        if (controller == null)
        {
            Debug.Log("Error: No character controller!");
        }

        // Set up the collision capsule and slope limit
        controller.slopeLimit = slopeLimit;
        controller.height = GetHeight();
        controller.center = GetMidsectionPos() - transform.position;
        controller.radius = capsuleRadius;

        // Make sure the animator is up to date!
        SetRunningSpeed(runningSpeed);
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
            {
                // NOTE(BluCloos): The grounded and clamp also applies the normal force
                // of slopes. 
                // NOTE(BluCloos): There are always going to be a static amount of hits,
                // with known indices.
                List<RaycastHit> hits; 
                bool onLedge; // NOTE(Reader): You can be on a ledge and still on the ground
                float groundDistance;
                bool localGrounded = GroundedAndClamp(out hits, out groundDistance, out onLedge);
                //Debug.Log(localGrounded);

                if (showGroundDistanceCheck)
                    Debug.DrawLine(GetFeetPos(), GetFeetPos() + Vector3.down * groundDistance);

                bool shouldPush = true;

                // NOTE(Reader): Only at the transition do we set the ground distance.
                // What is the ground distance you ask? Why it's used by the animator 
                // to determine what type of falling animation to do!
                if (grounded == true && localGrounded == false)
                {
                    playerAnimator.SetFloat("groundDistance", groundDistance);

                    float signedDot = 0.0f; // this default don't even matter
                    if (hits.Count >= 3)
                        signedDot = Vector3.Dot(directionVector, hits[2].normal);

                    if (onLedge && running && !localGrounded && signedDot >= 0.5f)
                    {
                        //Debug.Log("Set the Ledge jump bool to true!");
                        playerAnimator.SetBool("ledgeJump", true);
                        shouldPush = false;
                    }
                }

                grounded = localGrounded; // Actaully commit the local grounded

                if (onLedge && !grounded && shouldPush)
                {
                    //Debug.Log("GET THE FUCK OFF MY LAWN!");
                    // Push that little shit off the ledge RIGHT NOW
                    Vector3 pushVector = new Vector3(hits[2].normal.x, 0.0f, hits[2].normal.z);
                    pushVector = pushVector.normalized * Time.deltaTime * 4.0f;
                    controller.Move(pushVector);
                }
            }

            // Initialize the movement vector with the vertical movement
            Vector3 moveVector = Vector3.zero;
            {
                verticalVelocity = (grounded) ? 0.0f : Mathf.Max(verticalVelocity + gravityAcceleration * Time.deltaTime, terminalVelocity);
                if (gravityLock)
                    verticalVelocity = 0.0f;
                moveVector += Vector3.up * verticalVelocity;
            }

            // Setting up the movementVector
            {
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
                }
                else
                {
                    playerAnimator.SetBool("Grounded", false);
                }

                Vector3 localMoveVector = new Vector3(moveVector.x, 0.0f, moveVector.z) / Time.deltaTime;
                playerAnimator.SetFloat("V_Move", localMoveVector.magnitude / runningSpeed);
            }

            // Actually move the character using the movement vector. Oh and also account for any root motion magic!
            if (rootMotion && grounded)
            {
                Vector3 localMoveVector = new Vector3(0.0f, moveVector.y, 0.0f);
                controller.Move(localMoveVector);
            }
            else
            {
                controller.Move(moveVector);
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
