using System;
using UnityEngine;

// 축구공의 골 충돌을 감지하고 환경 컨트롤러에 알려주는 컴포넌트
public class SoccerBallController : MonoBehaviour
{

    public GameObject area; // 이 공이 속한 경기장 오브젝트
    [HideInInspector] public SoccerEnvController envController; // 경기장의 환경 컨트롤러 참조
    public string purpleGoalTag = "purpleGoal";
    public string blueGoalTag = "blueGoal";

    private void Start()
    {
        // 경기장 오브젝트에서 SoccerEnvController 컴포넌트 획득
        envController = area.GetComponent<SoccerEnvController>();
    }

    public void OnCollisionEnter(Collision col)
    {
        // 공이 보라팀 골대에 들어감 → 파란팀 득점
        if (col.gameObject.CompareTag(purpleGoalTag))
        {
            envController.GoalTouched(Team.Blue);
        }
        
        // 공이 파란팀 골대에 들어감 → 보라팀 득점
        if (col.gameObject.CompareTag(blueGoalTag))
        {
            envController.GoalTouched(Team.Purple);
        }
    }
}
