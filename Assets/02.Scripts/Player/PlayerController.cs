using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

// 플레이어 대표로서 외부와의 소통 또는 어빌리티들을 관리하는 역할
public class PlayerController : MonoBehaviour, IPunObservable, IDamageable
{
    public PhotonView PhotonView;
    public PlayerStat Stat;

    private void Awake()
    {
        PhotonView = GetComponent<PhotonView>();
    }
    
    [PunRPC]
    public void TakeDamage(float damage)
    {
        Stat.CurrentHealth -= damage;
        
        if (Stat.CurrentHealth <= 0)
        {
            // 죽음 처리
            Debug.Log($"{gameObject.name}이(가) 죽었습니다.");
        }
    }

    // 데이터 동기화를 위한 데이터 읽기(전송), 쓰기(수신) 메서드
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // 스트림: '시냇물'처럼 데이터가 멈추지 않고 연속적으로 흐르는 데이터 흐름
        //      : 서버에서 주고받을 데이터가 담겨있는 변수
        
        // 읽기/쓰기 모드
        if (stream.IsWriting)
        {
            Debug.Log("전송중....");
            // 이 PhotonView의 데이터를 보내줘야 하는 상황
            stream.SendNext(Stat.CurrentHealth);
            stream.SendNext(Stat.CurrentStamina);
            
            // 주고받을 데이터가 엄청 많다면 -> JSON으로 변환해서 한 번에 보내는 방법
            // 박싱/언박싱 vs JSON 변환 성능 : 데이터가 커지면 박싱/언박싱이 더 느리다.
        }
        else if (stream.IsReading)
        {
            Debug.Log("수신중....");
            // 이 PhotonView의 데이터를 받아야 하는 상황
            Stat.CurrentHealth = (float)stream.ReceiveNext();
            Stat.CurrentStamina = (float)stream.ReceiveNext();
        }
    }

    private Dictionary<Type, PlayerAbility> _abilitiesCache = new();
    
    public T GetAbility<T>() where T : PlayerAbility
    {
        var type = typeof(T);

        if (_abilitiesCache.TryGetValue(type, out PlayerAbility ability))
        {
            return ability as T;
        }

        // 게으른 초기화/로딩 -> 처음에 곧바로 초기화/로딩을 하는게 아니라
        //                    필요할때만 하는.. 뒤로 미루는 기법
        ability = GetComponent<T>();

        if (ability != null)
        {
            _abilitiesCache[ability.GetType()] = ability;

            return ability as T;
        }
        
        throw new Exception($"어빌리티 {type.Name}을 {gameObject.name}에서 찾을 수 없습니다.");
    }
}