using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class FoodAgentScript : Agent
{
    
    public GameObject Food;// Reference to the food GameObject
    public GameObject Poison;
    private Rigidbody rb;
    private int stepCount;
    private float previousFoodDistance;// Distance to food at the previous step
    private float previousPoisonDistance;

    private float currentReward = 0f;// Current accumulated reward for the agent

    public override void Initialize()// Initialization method called when the agent is created
    {
        rb = GetComponent<Rigidbody>();

        if (transform.parent != null)
        {
            Food = transform.parent.Find("Food")?.gameObject;// Find the food GameObject in the parent
            Poison = transform.parent.Find("Poison")?.gameObject;
        }

        if (Food == null || Poison == null)
        {
            Debug.LogError("Missing food or poison references!");
        }
    }

    public override void OnEpisodeBegin()// Called at the start of each episode
    {
        if (Food == null || Poison == null) return;

        rb.velocity = Vector3.zero;// Reset the agent's velocity
        rb.angularVelocity = Vector3.zero;// Reset the agent's angular velocity
        transform.localPosition = new Vector3(0, 1f, 0);    // Reset the agent's position

        Food.transform.localPosition = GetRandomPosition();// Reposition food to a random location
        Poison.transform.localPosition = GetRandomPosition();

        previousFoodDistance = Vector3.Distance(transform.localPosition, Food.transform.localPosition);// Calculate initial distance to food
        previousPoisonDistance = Vector3.Distance(transform.localPosition, Poison.transform.localPosition);

        stepCount = 0;// Reset step count for the new episode

        currentReward = 0f;// Reset current reward for the new episode

        Debug.Log("Episode Started: Agent reset, food and poison repositioned.");
    }

    public override void CollectObservations(VectorSensor sensor)// Collect observations for the agent
    {
        if (Food == null || Poison == null) return;

        sensor.AddObservation(transform.localPosition / 4f);// Normalize agent's position
        sensor.AddObservation(Food.transform.localPosition / 4f);// Normalize food's position
        sensor.AddObservation(Poison.transform.localPosition / 4f);// Normalize poison's position
        sensor.AddObservation(rb.velocity / 5f);// Normalize agent's velocity
        sensor.AddObservation((Food.transform.position - transform.position).normalized);// Add normalized vector to food
        sensor.AddObservation((Poison.transform.position - transform.position).normalized);// Add normalized vector to poison
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (Food == null || Poison == null) return;

        int move = actions.DiscreteActions[0]; // Assuming 0: forward, 1: back, 2: left, 3: right, 4: no movement
        Vector3 direction = GetDirectionFromAction(move);// Convert action to direction vector

        rb.AddForce(direction * 5f, ForceMode.VelocityChange);// Apply force based on action
        rb.velocity = Vector3.ClampMagnitude(rb.velocity, 5f);// Limit speed to prevent excessive movement

        float currentFoodDistance = Vector3.Distance(transform.localPosition, Food.transform.localPosition);// Calculate distance to food
        float currentPoisonDistance = Vector3.Distance(transform.localPosition, Poison.transform.localPosition);

        // Increased reward scaling for debug
        float foodReward = Mathf.Clamp((previousFoodDistance - currentFoodDistance) * 0.3f, -0.03f, 0.3f);// Calculate reward based on food distance change
        float poisonReward = Mathf.Clamp((currentPoisonDistance - previousPoisonDistance) * 0.15f, -0.03f, 0.15f);

        AddReward(foodReward);// Add food reward
        AddReward(poisonReward);// Add poison reward
        currentReward += foodReward + poisonReward;// Update current reward

        previousFoodDistance = currentFoodDistance;// Update previous distances for next step
        previousPoisonDistance = currentPoisonDistance;// Update previous distances for next step

        AddReward(-0.002f); // Slightly higher time penalty to encourage quicker learning
        currentReward -= 0.002f;

        if (++stepCount > 300)// Limit the number of steps per episode
        {
            AddReward(-0.5f);// Penalty for exceeding step limit
            currentReward -= 0.5f;
            Debug.Log($"Episode ended due to step limit. Total reward: {currentReward}");
            EndEpisode();
        }

        Debug.Log($"Step {stepCount}: Move {move}, Reward this step: {foodReward + poisonReward - 0.002f}, Total reward: {currentReward}");
    }

    private Vector3 GetRandomPosition()// Generate a random position within a defined area
    {
        return new Vector3(Random.Range(-4f, 4f), 1f, Random.Range(-4f, 4f));// Adjust the range as needed
    }

    private Vector3 GetDirectionFromAction(int action)// Convert action index to a direction vector
    {
        switch (action)
        {
            case 0: return Vector3.forward;// Forward movement
            case 1: return Vector3.back;    // Backward movement
            case 2: return Vector3.left;
            case 3: return Vector3.right;
            default: return Vector3.zero;
        }
    }

    private void OnTriggerEnter(Collider other)// Handle collisions with food, poison, and walls
    {
        if (other.CompareTag("Food"))
        {
            AddReward(10f); // Increased positive reward for finding food
            currentReward += 10f;// Adjusted reward for food
            Food.transform.localPosition = GetRandomPosition();// Reposition food after collection
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
        else if (other.CompareTag("Wall"))// Handle wall collisions
        {
            AddReward(-0.5f);// Penalty for hitting a wall
            currentReward -= 0.5f;// Adjusted penalty for wall collision
            rb.velocity *= -0.5f;// Reflect the agent's velocity to simulate a bounce effect
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
