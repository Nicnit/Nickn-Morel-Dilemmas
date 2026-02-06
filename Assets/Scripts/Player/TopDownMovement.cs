using UnityEngine;

// also contains code on interacting with interactables
public class TopDownMovement : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public float moveSpeed;
    public bool isMoving;
    private Vector2 input;
    private Rigidbody2D rb;

    private Vector3 dir;
    public Vector3 currDirection;
    private bool canMove = true;

    public LayerMask layerMask;
    public SpriteRenderer dish;
    [SerializeField] private Animator animator;

    // Start is called before the first frame update
    void Start()
    {
        // anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        dir = new Vector3(1, 0, 0);
        GameManager.Instance.orderManager.OnHeldOrderChanged += UpdateDish;
    }

    // Update is called once per frame
    private void Update()
    {
        if (canMove)
        {
            float x = Input.GetAxisRaw("Horizontal");
            float y = Input.GetAxisRaw("Vertical");

            dir = new Vector3(x, y).normalized;
            if (dir.magnitude > 0)
            {
                currDirection = dir;
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

    private void UpdateDish()
    {
        if (GameManager.Instance.orderManager.GetHeldOrderIndex() >= 0)
        {
            dish.sprite = GameManager.Instance.orderManager.GetHeldOrder().defaultSprite;
        }
        else
        {
            dish.sprite = null;
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
            rb.MovePosition(transform.position + dir * moveSpeed * Time.fixedDeltaTime);
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
    public Vector3 GetCurrDirection()
    {
        return currDirection;
    }
}
