using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class FoodAgentScript : Agent
{
    public GameObject Food; // Reference to the food GameObject
    public GameObject Poison; // Reference to the poison GameObject
    private Rigidbody rb; // Rigidbody component for physics interactions
    private int stepCount; // Counter for the number of steps taken in the episode
    private float previousFoodDistance; // Distance to food at the previous step
    private float previousPoisonDistance; // Distance to poison at the previous step

    private float currentReward = 0f; // Current accumulated reward for the agent

    public override void Initialize() // Initialize the agent
    {
        rb = GetComponent<Rigidbody>(); // Get the Rigidbody component for physics interactions

        if (transform.parent != null)
        {
            Food = transform.parent.Find("Food")?.gameObject;
            Poison = transform.parent.Find("Poison")?.gameObject;
        } // Find food and poison GameObjects in the parent object

        if (Food == null || Poison == null)
        {
            Debug.LogError("Missing food or poison references!"); // Log an error if food or poison is not found
        } 
    }

    public override void OnEpisodeBegin() // Called at the start of each episode
    {
        if (Food == null || Poison == null) return; // Ensure food and poison are set before starting the episode

        // Reset the agent's position and velocity
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.localPosition = new Vector3(0, 1f, 0);

        Food.transform.localPosition = GetRandomPosition(); // Randomly position food within bounds
        Poison.transform.localPosition = GetRandomPosition(); // Randomly position poison within bounds

        previousFoodDistance = Vector3.Distance(transform.localPosition, Food.transform.localPosition); // Calculate initial distance to food
        previousPoisonDistance = Vector3.Distance(transform.localPosition, Poison.transform.localPosition);

        stepCount = 0; // Reset step count for the new episode

        currentReward = 0f; // Reset current reward for the new episode

        Debug.Log("Episode Started: Agent reset, food and poison repositioned.");
    }

    public override void CollectObservations(VectorSensor sensor) // Collect observations for the agent
    {
        if (Food == null || Poison == null) return;

        sensor.AddObservation(transform.localPosition / 4f); // Normalize agent's position
        sensor.AddObservation(Food.transform.localPosition / 4f);
        sensor.AddObservation(Poison.transform.localPosition / 4f);
        sensor.AddObservation(rb.velocity / 5f);
        sensor.AddObservation((Food.transform.position - transform.position).normalized);
        sensor.AddObservation((Poison.transform.position - transform.position).normalized);
    }

    public override void OnActionReceived(ActionBuffers actions) // Called when the agent receives actions from the neural network
    {
        if (Food == null || Poison == null) return; // Ensure food and poison are set before processing actions

        int move = actions.DiscreteActions[0]; // Get the action for movement
        Vector3 direction = GetDirectionFromAction(move);

        rb.AddForce(direction * 5f, ForceMode.VelocityChange);
        rb.velocity = Vector3.ClampMagnitude(rb.velocity, 5f); // Limit the agent's speed

        float currentFoodDistance = Vector3.Distance(transform.localPosition, Food.transform.localPosition); // Calculate current distance to food
        float currentPoisonDistance = Vector3.Distance(transform.localPosition, Poison.transform.localPosition);

        // Increased reward scaling for debug
        float foodReward = Mathf.Clamp((previousFoodDistance - currentFoodDistance) * 0.3f, -0.03f, 0.3f);
        float poisonReward = Mathf.Clamp((currentPoisonDistance - previousPoisonDistance) * 0.15f, -0.03f, 0.15f);

        AddReward(foodReward); // Add reward based on food distance change
        AddReward(poisonReward);
        currentReward += foodReward + poisonReward;

        previousFoodDistance = currentFoodDistance; // Update previous food distance for next step
        previousPoisonDistance = currentPoisonDistance;

        AddReward(-0.002f); // Slightly higher time penalty to encourage quicker learning
        currentReward -= 0.002f;

        if (++stepCount > 300) // End episode if step limit is reached
        {
            AddReward(-0.5f); // Penalty for ending the episode due to step limit
            currentReward -= 0.5f;
            Debug.Log($"Episode ended due to step limit. Total reward: {currentReward}");
            EndEpisode();
        }

        Debug.Log($"Step {stepCount}: Move {move}, Reward this step: {foodReward + poisonReward - 0.002f}, Total reward: {currentReward}");
    }

    private Vector3 GetRandomPosition() // Generate a random position within specified bounds
    {
        return new Vector3(Random.Range(-4f, 4f), 1f, Random.Range(-4f, 4f));
    }

    private Vector3 GetDirectionFromAction(int action) // Convert action index to movement direction
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

    private void OnTriggerEnter(Collider other) // Handle collisions with food, poison, and walls
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