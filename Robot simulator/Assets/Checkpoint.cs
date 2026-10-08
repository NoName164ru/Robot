using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public int checkpointNumber = 1; // Номер чекпоинта (1, 2, 3...)
    public Color activeColor = Color.green;
    public Color passedColor = Color.gray;

    private bool isPassed = false;
    private Renderer[] renderers;

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
        SetColor(activeColor);
    }

    void OnTriggerEnter(Collider other)
    {
        // Проверяем, что это робот и чекпоинт еще не пройден
        if (other.CompareTag("Robot") && !isPassed)
        {
            isPassed = true;
            SetColor(passedColor);
            Debug.Log($"🏁 Чекпоинт {checkpointNumber} пройден!");
            
            // Сообщаем менеджеру чекпоинтов
            CheckpointManager.Instance.CheckpointPassed(checkpointNumber);
        }
    }

    void SetColor(Color color)
    {
        foreach (Renderer r in renderers)
        {
            r.material.color = color;
        }
    }

    public bool IsPassed()
    {
        return isPassed;
    }
}