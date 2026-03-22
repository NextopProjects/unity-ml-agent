using System;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using Random = UnityEngine.Random;

// 축구 경기장 전체를 관리하는 환경 컨트롤러
// 에피소드 리셋, 득점 처리, 에이전트 그룹 보상 등을 담당
public class SoccerEnvController : MonoBehaviour
{
    // 경기장에 배치된 각 선수의 정보를 담는 직렬화 가능한 클래스
    [Serializable]
    public class PlayerInfo
    {
        public AgentSoccer Agent; // 해당 에이전트 컴포넌트
        [HideInInspector] public Vector3 StartingPos; // 에피소드 시작 시 초기 위치 (저장용)
        [HideInInspector] public Quaternion StartingRot; // 에피소드 시작 시 초기 회전 (저장용)
        [HideInInspector] public Rigidbody Rb; // 에이전트의 Rigidbody 참조
    }

    /// <summary>
    ///     이 경기장이 리셋되기까지의 최대 Academy 스텝 수
    /// </summary>
    [Tooltip("Max Environment Steps")] public int MaxEnvironmentSteps = 25000;

    public GameObject ball; // 경기에 사용되는 축구공 오브젝트
    [HideInInspector] public Rigidbody ballRb; // 공의 Rigidbody 참조

    private Vector3 m_BallStartingPos; // 공의 초기 위치 (리셋 시 기준점)

    // 경기장에 배치된 모든 선수 정보 리스트 (Inspector에서 직접 할당)
    public List<PlayerInfo> AgentsList = new();

    private SoccerSettings m_SoccerSettings; // 전역 설정값 참조

    // ML-Agents 다중 에이전트 그룹: 팀 단위 보상 및 에피소드 관리
    private SimpleMultiAgentGroup m_BlueAgentGroup;
    private SimpleMultiAgentGroup m_PurpleAgentGroup;

    private int m_ResetTimer; // 현재 에피소드의 경과 스텝 수


    public bool useRandomStartPos = true; // Inspector에서 ON/OFF 가능

    private void Start()
    {
        m_SoccerSettings = SoccerSettings.Instance;

        // 팀별 다중 에이전트 그룹 초기화
        m_BlueAgentGroup = new SimpleMultiAgentGroup();
        m_PurpleAgentGroup = new SimpleMultiAgentGroup();

        // 공의 초기 위치 저장 및 Rigidbody 참조 획득
        ballRb = ball.GetComponent<Rigidbody>();
        m_BallStartingPos =
            new Vector3(ball.transform.position.x, ball.transform.position.y, ball.transform.position.z);

        // 각 선수 정보 초기화 및 팀 그룹에 등록
        foreach (var item in AgentsList)
        {
            item.StartingPos = item.Agent.transform.position;
            item.StartingRot = item.Agent.transform.rotation;
            item.Rb = item.Agent.GetComponent<Rigidbody>();

            // 팀에 따라 해당 그룹에 에이전트 등록
            if (item.Agent.team == Team.Blue)
                m_BlueAgentGroup.RegisterAgent(item.Agent);
            else
                m_PurpleAgentGroup.RegisterAgent(item.Agent);
        }

        ResetScene(); // 게임 시작 시 씬 초기 상태로 설정
    }

    // 매 물리 프레임마다 호출: 타임아웃 감지 및 강제 리셋 처리
    private void FixedUpdate()
    {
        m_ResetTimer += 1;
        // 최대 스텝에 도달하면 에피소드 강제 종료(중단) 후 씬 리셋
        if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            // GroupEpisodeInterrupted: 득점 없이 시간 초과로 끝났음을 ML-Agents에 알림
            m_BlueAgentGroup.GroupEpisodeInterrupted();
            m_PurpleAgentGroup.GroupEpisodeInterrupted();
            ResetScene();
        }
    }

    // 공을 초기 위치 근처 랜덤한 위치에 리스폰하는 메서드
    public void ResetBall()
    {
        Vector3 newBallPos;

        if (useRandomStartPos)
        {
            // 초기 위치 기준 ±2.5 범위 내 랜덤 배치 (매 에피소드마다 다양한 시작 상황)
            var randomPosX = Random.Range(-2.5f, 2.5f);
            var randomPosZ = Random.Range(-2.5f, 2.5f);
            newBallPos = m_BallStartingPos + new Vector3(randomPosX, 0f, randomPosZ);
        }
        else
        {
            // 고정 시작 위치
            newBallPos = m_BallStartingPos;
        }

        ball.transform.position = newBallPos;

        // 공의 속도를 완전히 초기화하여 이전 에피소드 영향 제거
        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;
    }

    // 골이 터졌을 때 호출: 팀 보상 지급, 에피소드 종료, 씬 리셋 처리
    public void GoalTouched(Team scoredTeam)
    {
        if (scoredTeam == Team.Blue)
        {
            // 파란팀 득점: 빠른 득점일수록 보상이 큼 (1에 가까울수록 좋음)
            m_BlueAgentGroup.AddGroupReward(1 - (float)m_ResetTimer / MaxEnvironmentSteps);
            m_PurpleAgentGroup.AddGroupReward(-1); // 보라팀 패배 패널티
        }
        else
        {
            // 보라팀 득점
            m_PurpleAgentGroup.AddGroupReward(1 - (float)m_ResetTimer / MaxEnvironmentSteps);
            m_BlueAgentGroup.AddGroupReward(-1); // 파란팀 패배 패널티
        }

        // 양 팀 에피소드 정상 종료
        m_PurpleAgentGroup.EndGroupEpisode();
        m_BlueAgentGroup.EndGroupEpisode();
        ResetScene();
    }

    // 씬 전체를 초기 상태로 리셋: 타이머, 선수 위치, 공 위치 초기화
    public void ResetScene()
    {
        m_ResetTimer = 0; // 스텝 카운터 초기화

        // 각 선수를 초기 위치 기반 랜덤 위치에 재배치
        foreach (var item in AgentsList)
        {
            Vector3 newStartPos;
            Quaternion newRot;

            if (useRandomStartPos)
            {
                // 랜덤 시작 좌표 및 회전
                var randomPosX = Random.Range(-5f, 5f);
                newStartPos = item.StartingPos + new Vector3(randomPosX, 0f, 0f);

                var rotSign = item.Agent.team switch {
                    Team.Blue => 1,
                    Team.Purple => -1,
                    _ => 0
                };
                
                var rot = rotSign * Random.Range(80f, 100f);
                newRot = Quaternion.Euler(0, rot, 0);
            }
            else
            {
                // 고정 시작 좌표 및 회전
                newStartPos = item.StartingPos;
                newRot = item.StartingRot;
            }

            // 위치와 회전 적용
            item.Agent.transform.SetPositionAndRotation(newStartPos, newRot);

            // 선수의 물리 속도 초기화 (잔여 운동량 제거)
            item.Rb.linearVelocity = Vector3.zero;
            item.Rb.angularVelocity = Vector3.zero;
        }

        // 공 위치 및 속도 초기화
        ResetBall();
    }
}