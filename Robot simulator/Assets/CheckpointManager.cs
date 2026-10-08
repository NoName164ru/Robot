using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance;

    public int totalCheckpoints = 6;
    private int passedCheckpoints = 0;

    void Awake()
    {
        // Делаем этот объект единственным (Singleton)
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void CheckpointPassed(int checkpointNumber)
    {
        passedCheckpoints++;
        Debug.Log($"✅ Пройдено чекпоинтов: {passedCheckpoints} / {totalCheckpoints}");

        if (passedCheckpoints >= totalCheckpoints)
        {
            Debug.Log("🏆 ВСЕ ЧЕКПОИНТЫ ПРОЙДЕНЫ! ФИНИШ!");
        }
    }

    public int GetPassedCount()
    {
        return passedCheckpoints;
    }
}