using UnityEngine;

// 축구 환경 전반에서 공유하는 전역 설정값을 보관하는 싱글톤 컴포넌트
// 씬에 하나만 존재하도록 보장
public class SoccerSettings : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static SoccerSettings _instance;
    public static SoccerSettings Instance
    {
        get
        {
            if (_instance == null)
            {
                // 씬에서 SoccerSettings 오브젝트 찾기
                _instance = FindObjectOfType<SoccerSettings>();
                
                if (_instance == null)
                {
                    // 없으면 새 오브젝트 생성
                    GameObject go = new GameObject("SoccerSettings");
                    _instance = go.AddComponent<SoccerSettings>();
                }
            }
            return _instance;
        }
    }
    

    // [Header("팀 머티리얼")]
    // public Material purpleMaterial;  // 보라팀 머티리얼
    // public Material blueMaterial;    // 파란팀 머티리얼
    //
    // [Header("훈련 설정")]
    // public bool randomizePlayersTeamForTraining = true; // 훈련 시 매 에피소드마다 팀 랜덤 배정

    [Header("에이전트 설정")]
    public float agentRunSpeed = 5f; // 에이전트 달리기 속도 기본값

    private void Awake()
    {
        // 싱글톤 인스턴스 설정
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("SoccerSettings 인스턴스가 이미 존재합니다. 중복된 오브젝트를 제거합니다.");
            Destroy(this.gameObject); // 중복된 오브젝트 제거
            return;
        }
        _instance = this;
        DontDestroyOnLoad(this.gameObject); // 씬 전환에도 유지
    }
}