using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

public class BlockPlacer : NetworkBehaviour
{
    public LayerMask groundLayer;
    public EducationalBlock selectedBlock;

    private void Update()
    {
        if (!IsOwner || Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return;
        if (EventSystem.current.IsPointerOverGameObject()) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                BlockInteract interact = hit.collider.GetComponentInParent<BlockInteract>();
                if (interact)
                {
                    RequestPlaySoundServerRpc(interact.blockData.blockName);
                    return;
                }

                if (((1 << hit.collider.gameObject.layer) & groundLayer) != 0 && selectedBlock != null)
                {
                    RequestSpawnBlockServerRpc(Snap(hit.point), selectedBlock.blockName);
                }
            }
        }
    }

    Vector3 Snap(Vector3 pos) => new Vector3(Mathf.Round(pos.x), 0, Mathf.Round(pos.z));

    [ServerRpc]
    void RequestSpawnBlockServerRpc(Vector3 pos, string name)
    {
        var block = AudioManager.Instance.GetBlockByName(name);
        if (block?.prefab)
        {
            var go = Instantiate(block.prefab, pos, Quaternion.identity);
            go.GetComponent<NetworkObject>().Spawn();
            var bi = go.AddComponent<BlockInteract>();
            bi.blockData = block;
            
            if (LevelController.Instance != null) 
                LevelController.Instance.SubmitActionServerRpc(name);
        }
    }

    [ServerRpc] void RequestPlaySoundServerRpc(string name) 
    {
        PlaySoundClientRpc(name);
        if (LevelController.Instance != null) LevelController.Instance.SubmitActionServerRpc(name);
    }
    [ClientRpc] void PlaySoundClientRpc(string name) => AudioManager.Instance.PlayClip(AudioManager.Instance.GetBlockByName(name)?.audioClip, Vector3.zero);

    public void SelectBlock(EducationalBlock b) => selectedBlock = b;
}
