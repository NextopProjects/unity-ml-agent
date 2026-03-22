using UnityEngine;

// 축구공의 골 충돌을 감지하고 환경 컨트롤러에 알려주는 컴포넌트
public class SoccerBallController : MonoBehaviour
{
    public GameObject area;                         // 이 공이 속한 경기장 오브젝트
    [HideInInspector]
    public SoccerEnvController envController;       // 경기장의 환경 컨트롤러 참조
    public string purpleGoalTag; // 보라팀 골대 태그 (공이 이 골대에 들어가면 파란팀 득점)
    public string blueGoalTag;   // 파란팀 골대 태그 (공이 이 골대에 들어가면 보라팀 득점)

    void Start()
    {
        // 경기장 오브젝트에서 SoccerEnvController 컴포넌트 획득
        envController = area.GetComponent<SoccerEnvController>();
    }

    // 공이 골대와 충돌했을 때 호출되는 메서드
    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag(purpleGoalTag)) // 공이 보라팀 골대에 들어감 → 파란팀 득점
        {
            envController.GoalTouched(Team.Blue);
        }
        if (col.gameObject.CompareTag(blueGoalTag))   // 공이 파란팀 골대에 들어감 → 보라팀 득점
        {
            envController.GoalTouched(Team.Purple);
        }
    }
}