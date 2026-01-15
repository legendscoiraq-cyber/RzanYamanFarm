using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [Header("إعدادات")]
    public float moveSpeed = 5f;
    [SerializeField] private GameObject yamanModel;
    [SerializeField] private GameObject razanModel;

    public NetworkVariable<int> characterSelection = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
    private Rigidbody rb;
    private VirtualJoystick joystick;

    public override void OnNetworkSpawn()
    {
        UpdateCharacterModel(characterSelection.Value);
        characterSelection.OnValueChanged += (oldVal, newVal) => UpdateCharacterModel(newVal);

        if (IsOwner)
        {
            joystick = FindObjectOfType<VirtualJoystick>();
            characterSelection.Value = PlayerPrefs.GetInt("SelectedCharacter", 0);
            SetupCamera();
        }
    }

    void SetupCamera()
    {
        Camera.main.transform.SetParent(null);
        var follower = Camera.main.gameObject.AddComponent<CameraFollower>();
        follower.target = transform;
    }

    void Start() => rb = GetComponent<Rigidbody>();

    void FixedUpdate()
    {
        if (!IsOwner) return;
        float h = joystick ? joystick.Horizontal : Input.GetAxis("Horizontal");
        float v = joystick ? joystick.Vertical : Input.GetAxis("Vertical");

        Vector3 dir = new Vector3(h, 0, v).normalized;
        
        // Isometric adjust
        Vector3 straight = Camera.main.transform.forward; straight.y=0; straight.Normalize();
        Vector3 right = Camera.main.transform.right; right.y=0; right.Normalize();
        Vector3 finalDir = (straight * v + right * h).normalized;

        if (finalDir.magnitude >= 0.1f)
        {
            rb.MovePosition(rb.position + finalDir * moveSpeed * Time.fixedDeltaTime);
            rb.rotation = Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(finalDir), 10f * Time.fixedDeltaTime);
        }
    }

    void UpdateCharacterModel(int selection)
    {
        if (yamanModel) yamanModel.SetActive(selection == 0);
        if (razanModel) razanModel.SetActive(selection == 1);
    }
}

public class CameraFollower : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 10, -10);
    private void LateUpdate()
    {
        if (target)
        {
            transform.position = Vector3.Lerp(transform.position, target.position + offset, 5f * Time.deltaTime);
            transform.LookAt(target);
        }
    }
}
