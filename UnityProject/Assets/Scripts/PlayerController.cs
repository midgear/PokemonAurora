using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    //[HideInInspector] public static bool jumping = false;

    public LayerMask walkingLayerMask;
    public float airMovementSpeed = 0.5f;
    public float rotationsPerMinute = 60.0f; // basically one rotation every second
    public float gravityAcceleration = -9.81f;
    public float jumpHeight = 1.0f;
    public float landingThreshold = 0.1f;
    public float maintanenceThreshold = 0.5f;
    //public float minWallSlope = 70.0f; // seems legit

    private const float tinyTolerance =  0.01f;
    private const float tolerance = 0.05f;
    private bool grounded = false;
    private bool directionVectorValid = false;
    private float verticalVelocity = 0.0f; // gavityyyyy
    private Vector3 airInertia = Vector3.zero;
    //private bool running = false; 
    private Transform cameraTransform;
    private Animator playerAnimator;
    private CharacterController controller;

    struct draw_sphere_request
    {
        public Vector3 pos;
        public float radius;
        public Color color;
    }

    List<draw_sphere_request> sphereRequestBuffer = new List<draw_sphere_request>();

    void DebugDrawSphere(Vector3 pos, float radius, Color color)
    {
        draw_sphere_request sphereRequest;
        sphereRequest.pos = pos;
        sphereRequest.radius = radius;
        sphereRequest.color = color;
        sphereRequestBuffer.Add(sphereRequest);
    }

    void OnDrawGizmos()
    {
        if (grounded)
        {
            Gizmos.color = Color.green;
        }
        else
        {
            Gizmos.color = Color.red;
        }

        if (controller != null)
            Gizmos.DrawCube(transform.position + Vector3.up * (controller.height + 0.1f) + transform.right, new Vector3(0.25f, 0.25f, 0.25f));

        if (directionVectorValid)
        {
            Gizmos.color = Color.magenta;
        }
        else
        {
            Gizmos.color = Color.red;
        }

        if (controller != null)
            Gizmos.DrawCube(transform.position + Vector3.up * (controller.height + 0.1f) - transform.right, new Vector3(0.25f, 0.25f, 0.25f));

        for (uint i = 0; i < sphereRequestBuffer.Count; i++)
        {
            draw_sphere_request sphereRequest = sphereRequestBuffer[(int)i];
            Gizmos.color = sphereRequest.color;
            Gizmos.DrawWireSphere(sphereRequest.pos, sphereRequest.radius);
            sphereRequestBuffer.Remove(sphereRequest);
        }
    }

    // NOTE(BluCloos): I also could not be bothered to write this.
    private Vector3 ProjectVectorOnPlane(Vector3 planeNormal, Vector3 vector)
    {
        return vector - (Vector3.Dot(vector, planeNormal) * planeNormal);
    }

    // NOTE(BluCloos): I stole this codes because I could not be bothered to write it myself.
    private bool SimulateSphereCast(Vector3 groundNormal, out RaycastHit hit)
    {
        float groundAngle = Vector3.Angle(groundNormal, Vector3.up) * Mathf.Deg2Rad;

        Vector3 secondaryOrigin = controller.transform.position + Vector3.up * tolerance;

        if (!Mathf.Approximately(groundAngle, 0))
        {
            float horizontal = Mathf.Sin(groundAngle) * controller.radius;
            float vertical = (1.0f - Mathf.Cos(groundAngle)) * controller.radius;

            // Retrieve a vector pointing up the slope
            Vector3 r2 = Vector3.Cross(groundNormal, Vector3.down);
            Vector3 v2 = -Vector3.Cross(r2, groundNormal);

            secondaryOrigin += ProjectVectorOnPlane(Vector3.up, v2).normalized * horizontal + Vector3.up * vertical;
        }

        Debug.DrawRay(secondaryOrigin, Vector3.down, Color.magenta);
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

    private Vector3 ControllerFeetPos()
    {
        return transform.position;
    }

    private float WallAngleFromNormal(Vector3 normal)
    {
        return Mathf.Acos(Vector3.Dot(normal, Vector3.up)) * Mathf.Rad2Deg;
    }

    private bool GroundedAndClamp(float margin)
    {
        // NOTE(Noah): The rayOrigin is calculated assuming that the player's transform is actually on the ground.
        Vector3 rayOrigin = transform.position + Vector3.up * (controller.height / 2.0f);
        Vector3 feetPos = ControllerFeetPos();
        Vector3 feetSpherePos = feetPos + Vector3.up * controller.radius;

        // Debuggggggg
        Debug.DrawLine(rayOrigin, rayOrigin + Vector3.down * controller.height, Color.red);

        RaycastHit hit;
        if (Physics.SphereCast(rayOrigin, controller.radius, Vector3.down, out hit, controller.height, walkingLayerMask))
        {
            // This call will draw the final sphere as a result of the raycast.
            //DebugDrawSphere(rayOrigin + Vector3.down * hit.distance, controller.radius, Color.black);

            // So the first thing we are going to do, before we do anything else,
            // is to determine the normal of the surface that we just hit.
            // we gotta make sure we respect what is a wall and what is not.

            // First things first, we have to calculate the angle between the two vectors.
            // It's time like these that im thankful I got a 95% in Calculus and Vectors :)
            float normalAngleToVertical = WallAngleFromNormal(hit.normal);
            //float deltaFromBottomCenter = 1.0f; // we are going to assume that if the delta is never set, it was a failed raycast
            float deltaDistance = 1.0f; // we are going to assume that if the delta is never set, it was a failed raycast

            if (normalAngleToVertical > controller.slopeLimit)
            {
                // NOTE(Reader): okay so now we need to assume that the wall has blocked the sphere cast 
                // from reaching the ground. We are going to raycast down the wall to find
                // the ground and its normal. Using the normal we are going to run a 
                // simulatedSphereCast to calculate the actual hit point.
                //  Also note that this code was heavily inspired by SuperCharacterCollider made by IronWarriror
                // https://github.com/IronWarrior/SuperCharacterController/

                // Retrieve a vector pointing down the slope
                Vector3 r = Vector3.Cross(hit.normal, Vector3.down);
                Vector3 wallSlopeDirection = Vector3.Cross(r, hit.normal);

                Vector3 flushOrigin = hit.point + hit.normal * tinyTolerance;
                RaycastHit flushHit;

                if (Physics.Raycast(flushOrigin, wallSlopeDirection, out flushHit, controller.height * 2.0f, walkingLayerMask))
                {
                    RaycastHit sphereCastHit;
                    
                    if (SimulateSphereCast(flushHit.normal, out sphereCastHit))
                    {
                        Vector3 contactSpherePos = sphereCastHit.point + Vector3.up * controller.radius;
                        //deltaFromBottomCenter = Vector3.Distance(sphereCastHit.point, feetPos);
                        DebugDrawSphere(sphereCastHit.point, 0.1f, Color.green);
                        deltaDistance = Vector3.Distance(feetSpherePos, contactSpherePos);
                        DebugDrawSphere(contactSpherePos, 0.3f, Color.black);
                        DebugDrawSphere(feetSpherePos, 0.3f, Color.cyan);
                    }
                }
            }
            else 
            {
                // the slope is all good and we can carry on with checking to make sure tha
                // the ground is within reasonable range.
                //deltaFromBottomCenter = Vector3.Distance(hit.point, feetPos);

                Vector3 hitSpherePos = rayOrigin + Vector3.down * hit.distance;
                deltaDistance = Vector3.Distance(feetSpherePos, hitSpherePos);
                DebugDrawSphere(hit.point, 0.1f, Color.red); // draw the hit point
                DebugDrawSphere(hitSpherePos, 0.3f, Color.black); // draw the hitSphere
                DebugDrawSphere(feetSpherePos, 0.3f, Color.cyan); // draw the footSphere
            }

            //Debug.Log(deltaFromBottomCenter);

            //Debug.Log(deltaDistance);

            // This will do the ground clamping
            //if (deltaFromBottomCenter <= margin)
            if (deltaDistance <= margin)
            {
                Vector3 moveVector = Vector3.down * hit.distance;
                controller.Move(moveVector);
                return true;
            }
            else if (normalAngleToVertical > controller.slopeLimit)
            {
                // on a steep wall so do the normal push becaue the default unity character control clips us even on
                // super steep walls. We also have to push the character in the opposite direction they are moving so that we 
                // negate their pushes against the wall.
                // what if the character looks away from the wall? They won't be able to move away from it.
                Vector3 wallPush = hit.normal;
                //Vector3 wallPush = hit.normal + new Vector3(-controller.velocity.x, 0.0f, -controller.velocity.z);
                controller.Move(wallPush * Time.deltaTime);
            }
        }
        else
        {
            //Debug.Log("no hit!");
        }

        return false;
    }

    private bool IsSteepWallFront(float threshold, out Vector3 wallNormal)
    {
        Vector3 feetPos = ControllerFeetPos();
        Vector3 feetSpherePos = feetPos + Vector3.up * controller.radius;

        RaycastHit hit;
        if (Physics.SphereCast(feetSpherePos, controller.radius, transform.forward, out hit, controller.height, walkingLayerMask))
        {
            DebugDrawSphere(hit.point, 0.1f, Color.red);
            Vector3 hitSpherePos = feetSpherePos + transform.forward * hit.distance;
            float deltaDistance = Vector3.Distance(feetSpherePos, hitSpherePos);
            if (deltaDistance <= threshold)
            {
                float normalAngleToVertical = WallAngleFromNormal(hit.normal);
                if (normalAngleToVertical > controller.slopeLimit)
                {
                    wallNormal = hit.normal;
                    return true;
                }
            }
        }

        wallNormal = Vector3.zero;
        return false;
    }

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerAnimator = GetComponent<Animator>();
        cameraTransform = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Transform>();
        //playerAnimator.SetBool("Grounded", true);
    }

    // Update is called once per frame
    void Update()
    {
        if (cameraTransform != null && playerAnimator != null && controller != null)
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            float cameraForwardAngle = cameraTransform.eulerAngles.y * Mathf.Deg2Rad;
            float cameraRightAngle = cameraForwardAngle + Mathf.PI / 2.0f;

            Vector3 forwardDirection = new Vector3(Mathf.Sin(cameraForwardAngle), 0.0f, Mathf.Cos(cameraForwardAngle));
            Vector3 rightDirection = new Vector3(Mathf.Sin(cameraRightAngle), 0.0f, Mathf.Cos(cameraRightAngle));
            Vector3 directionVector = Vector3.Normalize(forwardDirection * vertical + rightDirection * horizontal);
            Vector3 moveVector = Vector3.zero;

            bool moving = (Mathf.Abs(vertical) > Mathf.Epsilon) || Mathf.Abs(horizontal) > Mathf.Epsilon;

            // If we are already grounded, in order to not slip on terrain, we have to make our
            // search radius much larger. Otherwise, we are looking for a pretty small margin
            // if we are already falling. We have to make sure that we are absolutely on the ground

            if (grounded)
                grounded = GroundedAndClamp(maintanenceThreshold);
            else
                grounded = GroundedAndClamp(landingThreshold);

            if (grounded)
                playerAnimator.SetBool("Grounded", true);
            else                
                playerAnimator.SetBool("Grounded", false);

            Vector3 wallNormal;
            bool steepWall;
            if (!directionVectorValid)
                steepWall = IsSteepWallFront(maintanenceThreshold, out wallNormal);
            else
                steepWall = IsSteepWallFront(landingThreshold, out wallNormal);

            directionVectorValid = true;
            if (steepWall)
            {
                float angle = Vector3.Angle(directionVector, new Vector3(wallNormal.x, 0.0f, wallNormal.z));
                //Debug.Log(angle);
                // Determine if the directionVector is valid based on the normal of the wall.
                if (angle > 90.0f)
                {
                    directionVectorValid = false;   
                }
            }

            if (moving)
            {
                if (Input.GetButton("Run"))
                {
                    playerAnimator.SetFloat("V_Move", 1.0f);
                    //running = true;
                }
                else
                {
                    playerAnimator.SetFloat("V_Move", 0.5f);
                    //running = false;
                }
            }
            else
            {
                playerAnimator.SetFloat("V_Move", 0.0f);
            }

            // NOTE(Reader): This will override the previously calculate vertical Velocity. Also note that this is the intended behaviour.
            if (Input.GetButtonDown("Jump") && grounded)
            {
                verticalVelocity = Mathf.Sqrt(-2.0f * gravityAcceleration * jumpHeight);
                //IEnumerator corutine = WindupJump(1.0f);
                //StartCoroutine(corutine);
                grounded = false;
                //playerAnimator.SetBool("Jump", true);
                airInertia = new Vector3(controller.velocity.x, 0.0f, controller.velocity.z);
            }

            // Calculate the vertical velocity!
            verticalVelocity = (grounded) ? 0.0f : verticalVelocity + gravityAcceleration * Time.deltaTime;

            moveVector += Vector3.up * verticalVelocity;
            if (!grounded && directionVectorValid)
                moveVector += directionVector * airMovementSpeed + airInertia;

            // TODO(BluCloos): Document this code so people know what the heck they are reading!
            // btw this stuff is the rotation stuff to get the character to move in a direction relative to the camera rotation.
            if (moving)
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

            // do the movement for the character controller
            controller.Move(moveVector * Time.deltaTime);
        }
    }
}
