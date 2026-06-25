using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class BallRaceAgent : Agent
{
    [Header("Movement")]
    public float moveForce = 12f;
    public float maxSpeed = 8f;

    [Header("Course")]
    public Transform checkpointParent;
    public Transform startPoint;
    public float fallY = -2f;
    public float distanceRewardScale = 0.01f;

    private Rigidbody rb;
    private Transform[] checkpoints;
    private int currentCheckpointIndex;
    private float previousDistance;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();

        checkpoints = new Transform[checkpointParent.childCount];

        for (int i = 0; i < checkpointParent.childCount; i++)
        {
            checkpoints[i] = checkpointParent.GetChild(i);
        }
    }

    public override void OnEpisodeBegin()
    {
        currentCheckpointIndex = 0;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (startPoint != null)
        {
            transform.position = startPoint.position;
        }

        transform.rotation = Quaternion.identity;

        previousDistance = GetDistanceToCurrentCheckpoint();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 toCheckpoint = checkpoints[currentCheckpointIndex].position - transform.position;

        // 観測数：3 + 3 + 3 + 1 = 10
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(rb.velocity);
        sensor.AddObservation(toCheckpoint.normalized);
        sensor.AddObservation(toCheckpoint.magnitude / 20f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float moveZ = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        Vector3 force = new Vector3(moveX, 0f, moveZ) * moveForce;
        rb.AddForce(force, ForceMode.Force);

        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }

        float currentDistance = GetDistanceToCurrentCheckpoint();

        // チェックポイントに近づいたら少し報酬
        AddReward((previousDistance - currentDistance) * distanceRewardScale);
        previousDistance = currentDistance;

        // 時間がかかるほど少しマイナス
        AddReward(-0.001f);

        // 落下したら終了
        if (transform.position.y < fallY)
        {
            AddReward(-1f);
            EndEpisode();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (checkpoints == null || checkpoints.Length == 0)
        {
            return;
        }

        if (other.transform == checkpoints[currentCheckpointIndex])
        {
            AddReward(1.0f);

            currentCheckpointIndex++;

            if (currentCheckpointIndex >= checkpoints.Length)
            {
                AddReward(5.0f);
                EndEpisode();
            }
            else
            {
                previousDistance = GetDistanceToCurrentCheckpoint();
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.2f);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;

        continuousActions[0] = Input.GetAxis("Horizontal");
        continuousActions[1] = Input.GetAxis("Vertical");
    }

    private float GetDistanceToCurrentCheckpoint()
    {
        return Vector3.Distance(transform.position, checkpoints[currentCheckpointIndex].position);
    }
}