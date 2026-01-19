using UnityEngine;
using System.Collections;
using System; 

public class TurtleAgentUI : MonoBehaviour
{
    [SerializeField] private TurtleAgent m_Turtle;
    private GUIStyle m_DefaultStyle = new GUIStyle();
    private GUIStyle m_PositiveStyle = new GUIStyle();
    private GUIStyle m_NegativeStyle = new GUIStyle();

    private void Start()
    {
        m_DefaultStyle.fontSize = 18;
        m_DefaultStyle.normal.textColor = Color.yellow;

        m_PositiveStyle.fontSize = 18;
        m_PositiveStyle.normal.textColor = Color.green;

        m_NegativeStyle.fontSize = 18;
        m_NegativeStyle.normal.textColor = Color.red;
    }

    private void OnGUI()
    {
        string episodeAndStep = "Episode: " + m_Turtle.CurrentEpisode + " - Step: " + m_Turtle.StepCount;
        string reward = "Reward: " + m_Turtle.TurtleCumulativeReward.ToString();

        var rewardStyle = m_Turtle.TurtleCumulativeReward < 0 ? m_NegativeStyle : m_PositiveStyle;    

        GUI.Label(new Rect(20, 20, 500, 30), episodeAndStep, m_DefaultStyle);
        GUI.Label(new Rect(20, 60, 500, 30), reward, rewardStyle);
    }

    private void Update()
    {
        
    }
}
