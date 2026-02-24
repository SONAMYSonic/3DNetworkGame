using Photon.Pun;
using UnityEngine;

public class SceneMinimapFollower : MonoBehaviour
{
    [SerializeField] private Camera minimapCamera;
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 30f, 0f);
    [SerializeField] private bool rotateWithPlayer = false;

    private Transform target;

    private void Awake()
    {
        if (minimapCamera == null) minimapCamera = GetComponentInChildren<Camera>(true);
    }

    private void OnEnable()
    {
        // 씬 시작 타이밍에 로컬 플레이어가 아직 없을 수 있으므로 한 번 찾고,
        // 없으면 Update에서 재시도하도록 둠.
        TryBindLocalPlayer();
    }

    private void Update()
    {
        if (target == null)
            TryBindLocalPlayer();
    }

    private void LateUpdate()
    {
        if (target == null || minimapCamera == null) return;

        minimapCamera.transform.position = target.position + followOffset;

        if (rotateWithPlayer)
            minimapCamera.transform.rotation = Quaternion.Euler(90f, target.eulerAngles.y, 0f);
        else
            minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private void TryBindLocalPlayer()
    {
        // 씬에 존재하는 PhotonView들 중 "내 것(IsMine)" 찾기
        foreach (var pv in FindObjectsByType<PhotonView>(FindObjectsSortMode.None))
        {
            if (!pv.IsMine) continue;

            // 여기서 pv가 플레이어의 PhotonView라고 확신할 수 있으면 pv.transform을 타겟으로
            // 또는 특정 컴포넌트(예: PlayerController)가 있는지 검사해서 더 안전하게
            if (pv.GetComponent<PlayerController>() != null) // 본인 프로젝트의 플레이어 스크립트로 교체
            {
                target = pv.transform;
                return;
            }
        }
    }
}