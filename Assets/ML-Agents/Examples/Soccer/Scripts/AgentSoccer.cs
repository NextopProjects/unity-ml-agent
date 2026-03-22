using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using UnityEngine;

public enum Team
{
    Blue = 0,
    Purple = 1
}

// ML-Agents의 Agent 클래스를 상속받은 축구 에이전트
public class AgentSoccer : Agent
{
    public Team team; // 에이전트가 속한 팀

    private float m_KickPower; // 공을 차는 힘 (전진 시 1f, 그 외 0f)

    private float m_BallTouch; // 공과 충돌 시 보상 계수 (커리큘럼 학습으로 설정됨)

    private const float k_Power = 2000f; // 킥 시 가해지는 기본 물리 힘 상수
    // private float m_Existential; // 스텝당 존재 보상/패널티 값
    private float m_LateralSpeed; // 좌우 이동 속도 계수
    private float m_ForwardSpeed; // 전후 이동 속도 계수

    [HideInInspector] public Rigidbody agentRb; // 에이전트의 Rigidbody 컴포넌트

    private SoccerSettings m_SoccerSettings; // 축구 전역 설정값 참조
    // public Vector3 initialPos; // 에피소드 시작 시 기준 초기 위치
    // public float rotSign; // 팀에 따른 회전 방향 부호 (파란팀: +1, 보라팀: -1)

    private EnvironmentParameters m_ResetParams; // 아카데미에서 가져오는 환경 파라미터 (커리큘럼용)

    // 에이전트 초기화: 팀 설정, 포지션별 속도 설정, 컴포넌트 참조 획득
    public override void Initialize()
    {
        // 부모 오브젝트의 환경 컨트롤러를 찾아 존재 보상 단위 계산
        // var envController = GetComponentInParent<SoccerEnvController>();
        // if (envController != null)
        //     // 최대 환경 스텝 수의 역수 → 스텝마다 누적 시 총 보상이 1이 됨
        //     m_Existential = 1f / envController.MaxEnvironmentSteps;
        // else
        //     m_Existential = 1f / MaxStep;

        // BehaviorParameters의 TeamId로 파란팀/보라팀 설정
        gameObject.GetComponent<BehaviorParameters>().TeamId = (int)team;
        // if (team == Team.Blue)
        // {
        //     team = Team.Blue;
        //     // 파란팀: 현재 위치에서 왼쪽(-5)으로 초기 위치 설정
        //     initialPos = new Vector3(transform.position.x - 5f, .5f, transform.position.z);
        //     rotSign = 1f; // 파란팀 회전 방향: 양수
        // }
        // else
        // {
        //     team = Team.Purple;
        //     // 보라팀: 현재 위치에서 오른쪽(+5)으로 초기 위치 설정
        //     initialPos = new Vector3(transform.position.x + 5f, .5f, transform.position.z);
        //     rotSign = -1f; // 보라팀 회전 방향: 음수 (반대 방향)
        // }

        // 일반: 기본 속도
        m_LateralSpeed = 0.3f;
        m_ForwardSpeed = 1.0f;

        m_SoccerSettings = SoccerSettings.Instance; // 전역 설정 오브젝트 참조
        agentRb = GetComponent<Rigidbody>();
        agentRb.maxAngularVelocity = 500; // 최대 각속도 제한 (과도한 회전 방지)

        m_ResetParams = Academy.Instance.EnvironmentParameters; // 커리큘럼 파라미터 참조
    }

    // ActionSegment
    // ML-Agent에서 구성한 기존 배열을 복사하지 않고, 특정 구간(Offset~Length)을 참조해서 사용하는 뷰 구조체
    // 하나의 배열을 여러 부분으로 나눠 효율적으로 처리할 때 사용
    
    // 이산 행동 값을 받아 에이전트를 실제로 이동시키는 메서드
    public void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;   // 이동 방향 벡터
        var rotateDir = Vector3.zero; // 회전 방향 벡터
 
        m_KickPower = 0f; // 기본적으로 킥 파워 없음
 
        var forwardAxis = act[0]; // 전후 이동 액션 (0: 정지, 1: 전진, 2: 후진)
        var rightAxis = act[1];   // 좌우 이동 액션 (0: 정지, 1: 우측, 2: 좌측)
        var rotateAxis = act[2];  // 회전 액션    (0: 정지, 1: 좌회전, 2: 우회전)
 
        // 전후 이동 처리
        switch (forwardAxis)
        {
            case 1:
                dirToGo = transform.forward * m_ForwardSpeed; // 전진
                m_KickPower = 1f; // 전진 중 공 충돌 시 킥 파워 활성화
                break;
            case 2:
                dirToGo = transform.forward * -m_ForwardSpeed; // 후진
                break;
        }
 
        // 좌우 이동 처리
        switch (rightAxis)
        {
            case 1:
                dirToGo = transform.right * m_LateralSpeed;  // 오른쪽 이동
                break;
            case 2:
                dirToGo = transform.right * -m_LateralSpeed; // 왼쪽 이동
                break;
        }
 
        // 회전 처리
        switch (rotateAxis)
        {
            case 1:
                rotateDir = transform.up * -1f; // 왼쪽으로 회전
                break;
            case 2:
                rotateDir = transform.up * 1f;  // 오른쪽으로 회전
                break;
        }
 
        // 실제 물리 적용: 회전 및 이동
        transform.Rotate(rotateDir, Time.deltaTime * 100f);
        agentRb.AddForce(dirToGo * m_SoccerSettings.agentRunSpeed,
            ForceMode.VelocityChange); // 속도 즉시 변경 모드로 힘 적용
    }
    
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        base.Heuristic(in actionsOut);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        MoveAgent(actions.DiscreteActions);// 이산 행동으로 에이전트 이동
    }

    /// <summary>
    /// 공과 충돌했을 때 킥을 가하는 메서드
    /// </summary>
    void OnCollisionEnter(Collision c)
    {
        var force = k_Power * m_KickPower; // 현재 킥 파워에 따른 힘 계산
       
        if (c.gameObject.CompareTag("ball"))
        {
            // 공 터치 보상 (m_BallTouch는 커리큘럼으로 조절됨; 초기엔 0)
            AddReward(.2f * m_BallTouch);
 
            // 에이전트 중심에서 충돌 지점 방향으로 공에 힘 가하기 (킥)
            var dir = c.contacts[0].point - transform.position;
            dir = dir.normalized;
            c.gameObject.GetComponent<Rigidbody>().AddForce(dir * force);
        }
    }
    
    // 에피소드가 시작될 때마다 호출: 커리큘럼에서 ball_touch 값 갱신
    public override void OnEpisodeBegin()
    {
        // "ball_touch" 파라미터로 공 터치 보상 계수 설정 (기본값 0: 초기에는 터치 보상 없음)
        m_BallTouch = m_ResetParams.GetWithDefault("ball_touch", 0);
    }
}