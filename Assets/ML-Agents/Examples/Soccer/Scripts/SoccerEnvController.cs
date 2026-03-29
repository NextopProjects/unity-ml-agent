using System;
using UnityEngine;

// 축구 경기장 및 게임룰 전체를 관리하는 환경 컨트롤러
// 에피소드 리셋, 득점 처리 등, 보상을 관리
public class SoccerEnvController : MonoBehaviour
{
    [Serializable]
    public class PlayerInfo
    {
        public AgentSoccer Agent; // 에이전트 컴포넌트
        [HideInInspector] public Vector3 StartingPos; // 에피소드 시작 시 초기 위치
        [HideInInspector] public Vector3 StartingRot; // 에피소스 시작 시 초기 회전
        [HideInInspector] public Rigidbody Rb; // 에이전트 rigidbody
    }

    // 애파소드 회당 최대 행동 수
    public int MaxEnvironmentSteps = 25000;
    private int _resetTimer = 0;
    
    private void FixedUpdate()
    {
        _resetTimer++;
        if (_resetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            // TODO: 에이전트에게 보상 차감 혹은 시간초과에 대한 알림 처리
            // TODO: 에피소드 종료 (리셋)
        }
    }
}
