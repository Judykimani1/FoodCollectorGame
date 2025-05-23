# 🧠 Unity ML-Agents: Food Collector

This project uses [Unity ML-Agents Toolkit] to train an intelligent agent to **collect food** and **avoid poison** in a 3D environment. The agent learns via **Proximal Policy Optimization (PPO)** reinforcement learning.

---

🎯 Objectives

✅ Train an agent using PPO to navigate a simple environment.
✅ Encourage food collection and penalize poison contact.
✅ Visualize learning progress using TensorBoard.

🛠️ Requirements

- **Unity** 2021.3 LTS or later
- **ML-Agents Toolkit** (tested with `release_20`)
- **Python 3.8+**
- `mlagents` Python package
- `tensorboard` (for training visualization)

## 🚀 How to Run

### 1. Clone the Repository
```bash
git clone https://github.com/yourusername/your-repo-name.git
cd your-repo-name
---
2. Install ML-Agents
pip install mlagents==0.30.0

3. Launch Unity and Open the Project

4. Train the Agent
mlagents-learn config/food_collector.yaml --run-id=food_run_001

5. Visualize Training
tensorboard --logdir results

Open http://localhost:6006 in your browser.
`````
🧠 Agent Behavior
Positive reward for moving closer to food.

Negative reward for moving toward poison.

Episode ends upon poison contact or max steps.







