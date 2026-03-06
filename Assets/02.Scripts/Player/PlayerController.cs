using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using Random = UnityEngine.Random;

// 플레이어 대표로서 외부와의 소통 또는 어빌리티들을 관리하는 역할
public class PlayerController : MonoBehaviour, IPunObservable, IDamageable
{
    public PhotonView PhotonView;
    public PlayerStat Stat;
    
    // 죽을 때 점수 오브젝트를 3~5개 드랍한다
    // 점수 오브젝트를 먹으면 하나당 100점이 오른다.
    
    public bool IsDead { get; set; }
    private Animator _animator;
    
    private void Awake()
    {
        PhotonView = GetComponent<PhotonView>();
        _animator = GetComponent<Animator>();
    }
    
    private void Update() 
    {
        if (!PhotonView.IsMine) return;
        
        if (!IsDead && transform.position.y < -20f)
        {
            Die();
        }
    }

    [PunRPC]
    public void TakeDamage(float damage, int attackerActorNumber)
    {
        if (IsDead) return;
        
        Stat.CurrentHealth -= damage;
        
        if (Stat.CurrentHealth <= 0)
        {
            Die();
            // PhotonView.Owner: 이 오브젝트의 소유자 (= 실제 피해자)
            PhotonRoomManager.Instance.OnPlayerDeath(attackerActorNumber, PhotonView.Owner.NickName);
        }
    }

    private void Die()
    {
        IsDead = true;
        _animator.SetBool("IsDead", true);
        ScoreManager.Instance.DeathScore();

        if (PhotonView.IsMine)
        {
            // 리스폰 코루틴을 먼저 시작 (아이템 생성 실패해도 리스폰은 보장)
            StartCoroutine(RespawnAfterDelay(Stat.RespawnTime));

            // 아이템 생성 - ItemObjectFactory를 통해 방장에게 요청
            if (ItemObjectFactory.Instance != null)
            {
                ItemObjectFactory.Instance.RequestMakeScroreItems(transform.position + new Vector3(0, 0.5f, 0));
            }
        }
    }

    private IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // 랜덤 스폰 포인트 선택
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        Transform sp = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)].transform;
        
        // 캐릭터컨트롤러를 끄고, 위치 이동, 다시 켜기
        // 캐컨이 켜진 상태에서는 transform.position을 직접 변경하는게 막혀있음
        CharacterController cc = GetComponent<CharacterController>();
        cc.enabled = false;
        transform.position = sp.position;
        transform.rotation = sp.rotation;
        cc.enabled = true;
        
        // 스탯 복구
        Stat.CurrentHealth = Stat.MaxHealth;
        Stat.CurrentStamina = Stat.MaxStamina;
        
        // 죽음 해제
        IsDead = false;
        _animator.SetBool("IsDead", false);
    }

    // 데이터 동기화를 위한 데이터 읽기(전송), 쓰기(수신) 메서드
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // 스트림: '시냇물'처럼 데이터가 멈추지 않고 연속적으로 흐르는 데이터 흐름
        //      : 서버에서 주고받을 데이터가 담겨있는 변수
        
        // 읽기/쓰기 모드
        if (stream.IsWriting)
        {
            // 이 PhotonView의 데이터를 보내줘야 하는 상황
            stream.SendNext(Stat.CurrentHealth);
            stream.SendNext(Stat.CurrentStamina);
            stream.SendNext(IsDead);

            // 주고받을 데이터가 엄청 많다면 -> JSON으로 변환해서 한 번에 보내는 방법
            // 박싱/언박싱 vs JSON 변환 성능 : 데이터가 커지면 박싱/언박싱이 더 느리다.
        }
        else if (stream.IsReading)
        {
            // 이 PhotonView의 데이터를 받아야 하는 상황
            Stat.CurrentHealth = (float)stream.ReceiveNext();
            Stat.CurrentStamina = (float)stream.ReceiveNext();
            IsDead = (bool)stream.ReceiveNext();
            _animator.SetBool("IsDead", IsDead);
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