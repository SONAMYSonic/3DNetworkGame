using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class UI_Room : MonoBehaviourPunCallbacks
{
    private List<UI_RoomItem> _roomItems;
    private Dictionary<string, RoomInfo> _rooms = new();

    private void Awake()
    {
        _roomItems = GetComponentsInChildren<UI_RoomItem>().ToList();

        HideAllRoomUI();

    }

    private void HideAllRoomUI()
    {
        foreach (UI_RoomItem item in _roomItems)
        {
            item.gameObject.SetActive(false);
        }
    }

    // 로비에 입장 후 방 내용(개수, 이름 등등등)이 바뀌면 자동으로 호출되는 함수
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        // 모든 UI를 비활성화 하고,
        HideAllRoomUI();
    
        
        foreach (var room in roomList)
        {
            if (room.RemovedFromList)
            {
                _rooms.Remove(room.Name);
            }
            else
            {
                _rooms[room.Name] = room;
            }
        }

        // 캐싱된 전체 방 목록으로 UI 갱신
        List<RoomInfo> rooms = _rooms.Values.ToList();
        int count = Mathf.Min(rooms.Count, _roomItems.Count);

        for (int i = 0; i < count; i++)
        {
            _roomItems[i].Init(rooms[i]);
            _roomItems[i].gameObject.SetActive(true);
        }
    }
}
