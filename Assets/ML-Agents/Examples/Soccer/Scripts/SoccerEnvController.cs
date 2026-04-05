using System;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using Random = System.Random;

// 축구 경기장 및 게임룰 전체를 관리하는 환경 컨트롤러
// 에피소드 리셋, 득점 처리 등, 보상을 관리
public class SoccerEnvController : MonoBehaviour
{
    [Serializable]
    public class PlayerInfo
    {
        public AgentSoccer Agent; // 에이전트 컴포넌트
        [HideInInspector] public Vector3 StartingPos; // 에피소드 시작 시 초기 위치
        [HideInInspector] public Quaternion StartingRot; // 에피소스 시작 시 초기 회전
        [HideInInspector] public Rigidbody Rb; // 에이전트 rigidbody
    }

    // 애파소드 회당 최대 행동 수
    public int MaxEnvironmentSteps = 25000;
    private int _resetTimer = 0;
    
    // ML-Agents 다중 에이전트 그룹: 팀 단위 보상 및 에피소드 관리
    private SimpleMultiAgentGroup blueAgentGroup;
    private SimpleMultiAgentGroup purpleAgentGroup;

    // 경기장에 배치된 모든 선수 정보 리스트
    public List<PlayerInfo> AgentsList = new();

    public GameObject ball; // 경기에 사용되는 축구공 오브젝트
    [HideInInspector] public Rigidbody ballRb; // 공의 rigidbody 참조
    private Vector3 ballStartPos; // 공 시작 초기 위치 ( 리셋시 기준 좌표 )
    
    public bool useRandomStartPos = true;
    
    private void Start()
    {
        
        // 팀별 다중 에이전트 그룹 초기화
        blueAgentGroup = new SimpleMultiAgentGroup();
        purpleAgentGroup = new SimpleMultiAgentGroup();
        
        // 공의 초기 위치 저장 및 Rigidbody 참조 획득
        ballRb = ball.GetComponent<Rigidbody>();
        ballStartPos = new Vector3(
            ball.transform.position.x,
            ball.transform.position.y, 
            ball.transform.position.z);

        
        // 각 선수 정보 초기화 및 팀 그룹에 등록
        foreach (var player in AgentsList)
        {
            player.StartingPos = player.Agent.transform.position;
            player.StartingRot = player.Agent.transform.rotation;
            player.Rb = player.Agent.GetComponent<Rigidbody>();
            
            if(player.Agent.team == Team.Blue)
                blueAgentGroup.RegisterAgent(player.Agent);
            else
                purpleAgentGroup.RegisterAgent(player.Agent);
        }
        
        ResetScene(); // 게임 시작 시 씬 초기 상태로 설정
    }
    
    private void FixedUpdate()
    {
        _resetTimer++;
        if (_resetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            // GroupEpisodeInterrupted
            // 득점없이 시간 초과로 끝났음을 ML Agent에게 알림
            blueAgentGroup.GroupEpisodeInterrupted();
            purpleAgentGroup.GroupEpisodeInterrupted();
            
            ResetScene();
        }
    }

    // 골이 발생했을 때 호출 : 골을 넣은 팀에게 보상, 에피소드 종료, 씬 리셋
    public void GoalTouched(Team scoredTeam)
    {
        if (scoredTeam == Team.Blue)
        {
            // 빠른 시간내 득점한 경우 더 많은 보상을 줌으로써
            // 최대한 빠르게 공을 넣을 수 있게 유도
            blueAgentGroup.AddGroupReward(1 - (float)_resetTimer / MaxEnvironmentSteps);
            purpleAgentGroup.AddGroupReward(-1); 
        }
        else
        {
            // 빠른 시간내 득점한 경우 더 많은 보상을 줌으로써
            // 최대한 빠르게 공을 넣을 수 있게 유도
            purpleAgentGroup.AddGroupReward(1 - (float)_resetTimer / MaxEnvironmentSteps);
            blueAgentGroup.AddGroupReward(-1); 
        }
        
        // 양팀 에피소드 종료
        blueAgentGroup.EndGroupEpisode();
        purpleAgentGroup.EndGroupEpisode();
        
        // 씬 리셋
        ResetScene();
    }

    // 씬 전체를 초기 상태로 리셋: 타이머, 선수 위치, 공 위치 초기화
    public void ResetScene()
    {
        _resetTimer = 0;

        ResetPlayers();
        ResetBall();
    }

    public void ResetPlayers()
    {
        foreach (var player in AgentsList)
        {
            Vector3 newStartPos;
            Quaternion newRot;
            
            // 고정 시작 좌표 및 회전
            newStartPos = player.StartingPos;
            newRot = player.StartingRot;
            
            // 위치와 회전 적용
            player.Agent.transform.SetPositionAndRotation(newStartPos, newRot);

            // 선수의 물리 속도 초기화 (잔여 운동량 제거)
            player.Rb.linearVelocity = Vector3.zero;
            player.Rb.angularVelocity = Vector3.zero;
        }
    }

    public void ResetBall()
    {
        Vector3 newBallPos;

        if (useRandomStartPos)
        {
            var randomPosX = Random.Range(-2.5f, 2.5f);
            var randomPosZ = Random.Range(-2.5f, 2.5f); 
            newBallPos = new Vector3(randomPosX, 0f, randomPosZ);
        }
        else
        {
            // 고정 시작 위치
            newBallPos = ballStartPos;
        }

        ball.transform.position = newBallPos;
        
        // 공의 속도를 완전히 초기화하여 이전 에피소드 영향 제거
        ballRb.linearVelocity= Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;
    }
}
