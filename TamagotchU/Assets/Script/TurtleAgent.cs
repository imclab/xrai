using System;
using System.Collections;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;

public class TurtleAgent : Agent
{
    [SerializeField] private Transform m_Goal;
    [SerializeField] private float m_MoveSpeed = 1.5f;
    [SerializeField] private float m_RotationSpeed = 180f;
    [SerializeField] private Renderer m_GroundRenderer;

    private Renderer m_Renderer;

    [HideInInspector] public int CurrentEpisode = 0;
    [HideInInspector] public float TurtleCumulativeReward = 0f;

    private Color m_GroundColor;
    private Coroutine m_Coroutine;

    // circle of agent: observation -> action -> reward
    public override void Initialize()
    {
        m_Renderer = GetComponent<Renderer>();
        CurrentEpisode = 0;
        TurtleCumulativeReward = 0f;

        if(m_GroundRenderer) m_GroundColor = m_GroundRenderer.material.color;
    }

    public override void OnEpisodeBegin()
    {
        Debug.Log("OnEpisodeBegin");

        // clean all unfinished previous corountine
        if(m_Coroutine != null) StopCoroutine(m_Coroutine);

        if (m_GroundRenderer && TurtleCumulativeReward != 0f)
        {   
            // new ground win/fail effect
            Color targetColor = TurtleCumulativeReward > 0f ? Color.green : Color.red;
            m_GroundRenderer.material.color = targetColor;
            m_Coroutine = StartCoroutine(GroundEpisodeEffect(targetColor, 3f));
        }

        CurrentEpisode++;
        TurtleCumulativeReward = 0f;
        m_Renderer.material.color = Color.blue;

        SpawnObjects();
    }

    private IEnumerator GroundEpisodeEffect(Color targetColor, float v)
    {
        float timer = 0f;
        while (timer <= v)
        {
            timer += Time.deltaTime;
            m_GroundRenderer.material.color = Color.Lerp(targetColor, m_GroundColor, timer/v);
            yield return null;
        }
    }

    private void SpawnObjects()
    {
        // spawn turble transform
        transform.localRotation = Quaternion.identity;
        transform.localPosition = new Vector3(0, 0.15f, 0);

        // spawn goal transform
        float randomAngle = UnityEngine.Random.Range(0, 360f);
        Vector3 randomDirection = Quaternion.Euler(0, randomAngle, 0) * Vector3.forward; // move forward vector by randomAngle in the direction of Y-axis
        float randomDistance = UnityEngine.Random.Range(1f, 2.5f);

        Vector3 goalPosition = randomDirection * randomDistance;
        m_Goal.localPosition = new Vector3(goalPosition.x, 0.3f, goalPosition.z);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // normalize value to [-1, 1] because of neural network
        // split parameters to float because of neaural network
        float goalPositionXNormalized = m_Goal.localPosition.x / 5.0f;
        float goalPositionZNormalized = m_Goal.localPosition.z / 5.0f;
        float turtlePositionXNormalized = transform.localPosition.x / 5.0f;
        float turtlePositionZNormalized = transform.localPosition.z / 5.0f;
        float turbleRotationNormalized = (transform.localRotation.eulerAngles.y / 360.0f) * 2.0f - 1.0f;

        sensor.AddObservation(goalPositionXNormalized);
        sensor.AddObservation(goalPositionZNormalized);
        sensor.AddObservation(turtlePositionXNormalized);
        sensor.AddObservation(turtlePositionZNormalized);
        sensor.AddObservation(turbleRotationNormalized);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionOut = actionsOut.DiscreteActions;

        discreteActionOut[0] = 0;

        if(Keyboard.current.wKey.isPressed)
            discreteActionOut[0] = 1;
        else if(Keyboard.current.aKey.isPressed)
            discreteActionOut[0] = 2;
        else if(Keyboard.current.dKey.isPressed)
            discreteActionOut[0] = 3;
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        MoveAgent(actions.DiscreteActions);

        // small penalization for every action taken
        AddReward(-2.0f / MaxStep);

        TurtleCumulativeReward = GetCumulativeReward();
    }

    private void MoveAgent(ActionSegment<int> discreteActions)
    {
        int action = discreteActions[0];
        switch (action)
        {
            case 1: transform.localPosition += transform.forward * m_MoveSpeed * Time.deltaTime; // move forward
                break;
            case 2: transform.Rotate(0, m_RotationSpeed * Time.deltaTime, 0); // turn right
                break;
            case 3: transform.Rotate(0, -m_RotationSpeed * Time.deltaTime, 0); // turn left
                break;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Goal"))
        {
            GoalReached();
        }
    }

    private void GoalReached()
    {
        AddReward(1.0f);
        TurtleCumulativeReward = GetCumulativeReward();

        EndEpisode();
    }

    // hit wall penalization
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.005f);
            TurtleCumulativeReward = GetCumulativeReward();

            if(m_Renderer)
            {
                m_Renderer.material.color = Color.red;
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if(collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.01f * Time.fixedDeltaTime);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if(collision.gameObject.CompareTag("Wall"))
        {
            if(m_Renderer)
            {
                m_Renderer.material.color = Color.blue;
            }
        }
    }
}
