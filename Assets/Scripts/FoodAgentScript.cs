using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class FoodAgentScript : Agent
{
    public GameObject Food;
    public GameObject Poison;
    private Rigidbody rb;
    private int stepCount;
    private float previousFoodDistance;
    private float previousPoisonDistance;

    private float currentReward = 0f;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();

        if (transform.parent != null)
        {
            Food = transform.parent.Find("Food")?.gameObject;
            Poison = transform.parent.Find("Poison")?.gameObject;
        }

        if (Food == null || Poison == null)
        {
            Debug.LogError("Missing food or poison references!");
        }
    }

    public override void OnEpisodeBegin()
    {
        if (Food == null || Poison == null) return;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.localPosition = new Vector3(0, 1f, 0);

        Food.transform.localPosition = GetRandomPosition();
        Poison.transform.localPosition = GetRandomPosition();

        previousFoodDistance = Vector3.Distance(transform.localPosition, Food.transform.localPosition);
        previousPoisonDistance = Vector3.Distance(transform.localPosition, Poison.transform.localPosition);

        stepCount = 0;

        currentReward = 0f;

        Debug.Log("Episode Started: Agent reset, food and poison repositioned.");
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (Food == null || Poison == null) return;

        sensor.AddObservation(transform.localPosition / 4f);
        sensor.AddObservation(Food.transform.localPosition / 4f);
        sensor.AddObservation(Poison.transform.localPosition / 4f);
        sensor.AddObservation(rb.velocity / 5f);
        sensor.AddObservation((Food.transform.position - transform.position).normalized);
        sensor.AddObservation((Poison.transform.position - transform.position).normalized);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (Food == null || Poison == null) return;

        int move = actions.DiscreteActions[0];
        Vector3 direction = GetDirectionFromAction(move);

        rb.AddForce(direction * 5f, ForceMode.VelocityChange);
        rb.velocity = Vector3.ClampMagnitude(rb.velocity, 5f);

        float currentFoodDistance = Vector3.Distance(transform.localPosition, Food.transform.localPosition);
        float currentPoisonDistance = Vector3.Distance(transform.localPosition, Poison.transform.localPosition);

        // Increased reward scaling for debug
        float foodReward = Mathf.Clamp((previousFoodDistance - currentFoodDistance) * 0.3f, -0.03f, 0.3f);
        float poisonReward = Mathf.Clamp((currentPoisonDistance - previousPoisonDistance) * 0.15f, -0.03f, 0.15f);

        AddReward(foodReward);
        AddReward(poisonReward);
        currentReward += foodReward + poisonReward;

        previousFoodDistance = currentFoodDistance;
        previousPoisonDistance = currentPoisonDistance;

        AddReward(-0.002f); // Slightly higher time penalty to encourage quicker learning
        currentReward -= 0.002f;

        if (++stepCount > 300)
        {
            AddReward(-0.5f);
            currentReward -= 0.5f;
            Debug.Log($"Episode ended due to step limit. Total reward: {currentReward}");
            EndEpisode();
        }

        Debug.Log($"Step {stepCount}: Move {move}, Reward this step: {foodReward + poisonReward - 0.002f}, Total reward: {currentReward}");
    }

    private Vector3 GetRandomPosition()
    {
        return new Vector3(Random.Range(-4f, 4f), 1f, Random.Range(-4f, 4f));
    }

    private Vector3 GetDirectionFromAction(int action)
    {
        switch (action)
        {
            case 0: return Vector3.forward;
            case 1: return Vector3.back;
            case 2: return Vector3.left;
            case 3: return Vector3.right;
            default: return Vector3.zero;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Food"))
        {
            AddReward(3f); // Increased positive reward for finding food
            currentReward += 3f;
            Food.transform.localPosition = GetRandomPosition();
            Debug.Log($"Food eaten! Total reward: {currentReward}");
        }
        else if (other.CompareTag("Poison"))
        {
            AddReward(-3f); // Increased penalty for poison
            currentReward -= 3f;
            Poison.transform.localPosition = GetRandomPosition();
            Debug.Log($"Poison hit! Total reward: {currentReward}");
            EndEpisode(); // Optionally end episode on poison contact
        }
        else if (other.CompareTag("Wall"))
        {
            AddReward(-0.5f);
            currentReward -= 0.5f;
            rb.velocity *= -0.5f;
            Debug.Log($"Wall hit! Total reward: {currentReward}");
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = 4; // No movement by default

        if (Input.GetKey(KeyCode.UpArrow)) discreteActions[0] = 0;
        if (Input.GetKey(KeyCode.DownArrow)) discreteActions[0] = 1;
        if (Input.GetKey(KeyCode.LeftArrow)) discreteActions[0] = 2;
        if (Input.GetKey(KeyCode.RightArrow)) discreteActions[0] = 3;
    }

    private void OnDrawGizmos()
    {
        if (Food != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(Food.transform.position, 0.3f);
        }
        if (Poison != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(Poison.transform.position, 0.3f);
        }
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(transform.position, 0.3f);
    }
}