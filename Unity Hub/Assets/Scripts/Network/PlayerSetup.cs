using Unity.Netcode;
using UnityEngine;

namespace RazanYamanFarm.Network
{
    /// <summary>
    /// Handles player avatar selection and synchronization across the network.
    /// Avatar selection (Boy=0, Girl=1) is synced using NetworkVariable.
    /// </summary>
    public class PlayerSetup : NetworkBehaviour
    {
        [Header("Avatar Models")]
        [Tooltip("Boy character model (Yaman) - Avatar ID: 0")]
        [SerializeField] private GameObject boyModel;
        
        [Tooltip("Girl character model (Razan) - Avatar ID: 1")]
        [SerializeField] private GameObject girlModel;
        
        [Header("Player Name")]
        [SerializeField] private string boyName = "يمان";
        [SerializeField] private string girlName = "رزان";

        [Header("Visual Feedback")]
        [SerializeField] private GameObject playerNameCanvas;
        [SerializeField] private TMPro.TextMeshProUGUI playerNameText;

        /// <summary>
        /// Network-synced avatar selection.
        /// 0 = Boy (Yaman), 1 = Girl (Razan)
        /// </summary>
        public NetworkVariable<int> AvatarId = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        /// <summary>
        /// Player's display name (synced)
        /// </summary>
        public NetworkVariable<Unity.Collections.FixedString64Bytes> PlayerName = new NetworkVariable<Unity.Collections.FixedString64Bytes>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        private const string PREF_AVATAR_KEY = "SelectedAvatar";
        private const string PREF_PLAYER_NAME_KEY = "PlayerName";

        #region NetworkBehaviour Overrides

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Subscribe to avatar changes
            AvatarId.OnValueChanged += OnAvatarChanged;
            PlayerName.OnValueChanged += OnPlayerNameChanged;

            // Apply current values
            UpdateAvatarVisuals(AvatarId.Value);
            UpdatePlayerName(PlayerName.Value.ToString());

            // Owner-specific setup
            if (IsOwner)
            {
                InitializeOwnerPlayer();
            }

            Debug.Log($"[PlayerSetup] Player spawned - IsOwner: {IsOwner}, AvatarId: {AvatarId.Value}");
        }

        public override void OnNetworkDespawn()
        {
            AvatarId.OnValueChanged -= OnAvatarChanged;
            PlayerName.OnValueChanged -= OnPlayerNameChanged;
            base.OnNetworkDespawn();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Set the avatar for this player (Owner only).
        /// </summary>
        /// <param name="avatarId">0 = Boy, 1 = Girl</param>
        public void SetAvatar(int avatarId)
        {
            if (!IsOwner)
            {
                Debug.LogWarning("[PlayerSetup] Only owner can change avatar");
                return;
            }

            avatarId = Mathf.Clamp(avatarId, 0, 1);
            AvatarId.Value = avatarId;
            
            // Save preference
            PlayerPrefs.SetInt(PREF_AVATAR_KEY, avatarId);
            PlayerPrefs.Save();

            // Update name based on avatar
            string name = avatarId == 0 ? boyName : girlName;
            PlayerName.Value = name;
        }

        /// <summary>
        /// Toggle between Boy and Girl avatars.
        /// </summary>
        public void ToggleAvatar()
        {
            SetAvatar(AvatarId.Value == 0 ? 1 : 0);
        }

        /// <summary>
        /// Get the current avatar name.
        /// </summary>
        public string GetAvatarName()
        {
            return AvatarId.Value == 0 ? boyName : girlName;
        }

        /// <summary>
        /// Check if this is the Boy avatar.
        /// </summary>
        public bool IsBoy => AvatarId.Value == 0;

        /// <summary>
        /// Check if this is the Girl avatar.
        /// </summary>
        public bool IsGirl => AvatarId.Value == 1;

        #endregion

        #region Private Methods

        private void InitializeOwnerPlayer()
        {
            // Load saved avatar preference
            int savedAvatar = PlayerPrefs.GetInt(PREF_AVATAR_KEY, 0);
            SetAvatar(savedAvatar);

            // Setup camera if needed
            SetupPlayerCamera();
        }

        private void OnAvatarChanged(int previousValue, int newValue)
        {
            Debug.Log($"[PlayerSetup] Avatar changed: {previousValue} -> {newValue}");
            UpdateAvatarVisuals(newValue);
        }

        private void OnPlayerNameChanged(Unity.Collections.FixedString64Bytes previousValue, Unity.Collections.FixedString64Bytes newValue)
        {
            UpdatePlayerName(newValue.ToString());
        }

        private void UpdateAvatarVisuals(int avatarId)
        {
            // Activate/deactivate models
            if (boyModel != null)
            {
                boyModel.SetActive(avatarId == 0);
            }

            if (girlModel != null)
            {
                girlModel.SetActive(avatarId == 1);
            }

            // Update player tag for identification
            gameObject.tag = avatarId == 0 ? "Player_Boy" : "Player_Girl";
        }

        private void UpdatePlayerName(string name)
        {
            if (playerNameText != null && !string.IsNullOrEmpty(name))
            {
                playerNameText.text = name;
            }
        }

        private void SetupPlayerCamera()
        {
            // Only setup camera for local player
            if (!IsOwner) return;

            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;

            // Detach camera from any existing parent
            mainCamera.transform.SetParent(null);

            // Add or get camera follower
            CameraFollow follower = mainCamera.GetComponent<CameraFollow>();
            if (follower == null)
            {
                follower = mainCamera.gameObject.AddComponent<CameraFollow>();
            }
            
            follower.SetTarget(transform);
        }

        #endregion
    }

    /// <summary>
    /// Simple camera follow component for player tracking.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new Vector3(0, 10, -8);
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private float rotationX = 45f;

        private Transform _target;

        public void SetTarget(Transform target)
        {
            _target = target;
            
            // Initial position
            if (_target != null)
            {
                transform.position = _target.position + offset;
                transform.rotation = Quaternion.Euler(rotationX, 0, 0);
            }
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            Vector3 desiredPosition = _target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            
            // Keep looking at player
            Vector3 lookTarget = _target.position;
            lookTarget.y = transform.position.y - 5f;
            transform.LookAt(lookTarget);
        }
    }
}
