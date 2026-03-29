using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;


public enum Team
{
    Blue = 0,
    Purple = 1
}


public class AgentSoccer : Agent
{
    public Team team;
    
    [HideInInspector] public Rigidbody agentRb; // 에이전트 rigidbody 컴포넌트

    private float _lateralSpeed = 0.3f; // 좌우 이동 속도 계수
    private float _forwardSpeed = 1.0f; // 앞뒤 이동 계수

    private float _kickPower;
    
    // 에이전트 초기화: 팀 설정, 포지션별 속도 설정, 컴포넌트 참조 획득
    public override void Initialize()
    {
        agentRb = GetComponent<Rigidbody>();
        agentRb.maxAngularVelocity = 500; // 최대 회전각 속도 제한
        
        // BehaviorParameters의 TeamId로 파란팀/보라팀 설정
        GetComponent<BehaviorParameters>().TeamId = (int)team;
        
    }
    
    // 사용자 직접 컨트롤 할 수 있게 함수
    // 휴리스틱(수동 조작) 입력 처리
    // 사람이 키보드나 입력 장치로 에이전트를 직접 조작할 때 사용
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        base.Heuristic(in actionsOut);
    }

    // 관측값 수집 함수
    // 에이전트가 환경을 인식하기 위해 필요한 정보를 수집
    public override void CollectObservations(VectorSensor sensor)
    {
        base.CollectObservations(sensor);
    }

    // 행동 수행 함수
    // 신경망이 출력한 행동(Action)을 실제 게임 내 동작으로 변환
    public override void OnActionReceived(ActionBuffers actions)
    {
        MoveAgent(actions.DiscreteActions);
    }
    // ActionSegment
    // ML-Agent에서 구성한 기존 배열을 복사하지 않고, 특정 구간(Offset~Length)을 참조해서 사용하는 뷰 구조체
    // 하나의 배열을 여러 부분으로 나눠 효율적으로 처리할 때 사용
    public void MoveAgent(ActionSegment<int> act)
    {
        // 방향 Vector3 ( 0,0,0 ~ 1,1,1 ) 
        var dirToGo = Vector3.zero; // 이동 방향 벡터
        var rotateDir = Vector3.zero; // 회전 방햑 벡터

        _kickPower = 0f; // 기본적 이동시에는 킥 파워가 없음

        var forwardAxis = act[0]; // 전후 이동 액션 (0: 정지, 1: 전진, 2: 후진)
        var rightAxis = act[1];   // 좌우 이동 액션 (0: 정지, 1: 우측, 2: 좌측)
        var rotateAxis = act[2];  // 회전 액션    (0: 정지, 1: 좌회전, 2: 우회전)

        // 전후 이동 처리
        switch (forwardAxis)
        {
            case 0:
                break;
            case 1: // 전진
                dirToGo = transform.forward * _forwardSpeed;
                _kickPower = 1f; // 전진 중 공 충돌 시 킥 파워 설정
                break;
            case 2: // 후진
                dirToGo = transform.forward * -_forwardSpeed;
                break;
        }

        switch (rightAxis)
        {
            case 0:
                break;
            case 1: // 오른쪽 이동
                dirToGo = transform.right * _lateralSpeed;
                break;
            case 2: // 왼쪽 이동
                dirToGo = transform.right * -_lateralSpeed;
                break;
        }

        switch (rotateAxis)
        {
            case 0:
                break;
            case 1: // 왼쪽 회전
                rotateDir = transform.up * -1f;
                break;
            case 2: // 오른쪽 회전
                rotateDir = transform.up * 1f;
                break;
        }
        // 실제 물리 적용 : 회전 및 이동
        transform.Rotate(rotateDir, Time.fixedDeltaTime * 100f);
        agentRb.AddForce(dirToGo , ForceMode.VelocityChange);
    }

    // 충돌 시작시 발생하는 이벤트 함수
    private void OnCollisionEnter(Collision other)
    {
        // 공과 충돌했을때 킥 처리
        var force = _kickPower * 2000f; // 킥파워를 강제로 키움 2000f;

        if (other.gameObject.CompareTag("ball"))
        {
            AddReward(0.2f ); // 공 터치 보상
            
            // 에이전트 중심에서 충돌 지점 방향으로 공에 힘 가하기
            var dir = other.contacts[0].point - transform.position;
            dir = dir.normalized;
            other.gameObject.GetComponent<Rigidbody>().AddForce(dir * force);
        }
    }

    // 에피소드 시작 시 호출
    // 환경을 초기 상태로 리셋할 때 사용
    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
    }
}
