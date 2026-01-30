using UnityEditor.UIElements;
using UnityEngine;

// also contains code on interacting with interactables
public class AlexTopDownMovement : MonoBehaviour
{
    public float maxSpeed = 10;
    public float turnSpeed = 5;

    public bool isMoving;
    private Vector2 input;
    private Rigidbody2D rb;

    public SpriteRenderer spriteRenderer;
    private Vector3 dir;
    public Vector3 currDirection;
    private Vector3 tempDirection;
    private bool canMove = true;

    public LayerMask layerMask;
    public Animator animator;

    // Start is called before the first frame update
    void Start()
    {
        // anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        dir = new Vector3(0, 0, 0);
    }

    // Update is called once per frame
    private void Update()
    {
        if (canMove)
        {
            float x = Input.GetAxisRaw("Horizontal");
            float y = Input.GetAxisRaw("Vertical");

            Vector3 rawInput = new Vector3(x, y).normalized;
            dir = Vector3.MoveTowards(dir, rawInput, Time.deltaTime*turnSpeed);
            
            // Use raw input for animator/flip decisions, smoothed dir only for movement
            if (rawInput.magnitude > 0)
            {
                currDirection = rawInput;
                animator.SetBool("IsMoving", true);
                
                if (x > 0)
                {
                    spriteRenderer.flipX = false;
                }
                else if (x < 0)
                {
                    spriteRenderer.flipX = true;
                }
            }
            else
            {
                animator.SetBool("IsMoving", false);
            }
            animator.SetInteger("Vertical", (int) Mathf.Ceil(y));
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            GameObject raycastResult = CheckRaycast();
            if (raycastResult != null)
            {
                Interacting(raycastResult);
            }
        }
    }

    private void Interacting(GameObject interactedObj)
    {
        interactedObj.GetComponent<IInteractable>().OnInteract();
    }


    public GameObject CheckRaycast()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, currDirection, 64f, layerMask);
        if (hit)
        {
            return hit.collider.gameObject;
        }
        return null;
    }

    private void FixedUpdate()
    {
        if (canMove)
        {
            rb.MovePosition(transform.position + dir * maxSpeed * Time.fixedDeltaTime);
            transform.position = new Vector3(transform.position.x, transform.position.y, -2);
        }
    }

    public void SetNotMoving()
    {
        canMove = false;
        rb.linearVelocity = Vector3.zero;
    }

    public void EnableMovement()
    {
        canMove = true;
    }

    public void DisableMovement()
    {
        canMove = false;
    }
    public void SetCurrDirection(Vector3 newDir)
    {
        currDirection = newDir;
    }

    public void SetDirection(Vector3 newDir)
    {
        dir = newDir;
    }
    public Vector3 GetCurrDirection()
    {
        return currDirection;
    }
}
