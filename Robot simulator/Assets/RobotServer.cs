using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class RobotServer : MonoBehaviour
{
    [Header("Настройки сети")]
    public int port = 8080;

    [Header("Настройки движения")]
    public float maxSpeed = 5f;
    public float rotateSpeed = 100f;
    public float maxTurnPerFrame = 2f;

    [Header("Состояние")]
    public bool isCodeMode = false;

    private TcpListener server;
    private Thread serverThread;
    private bool isRunning = false;
    private Rigidbody rb;

    // ВАЖНО: храним ПОСЛЕДНЮЮ команду, а не сбрасываем её
    private string currentCommand = "";
    private object lockObject = new object();

    private Vector3 startPosition;
    private Quaternion startRotation;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) Debug.LogError("❌ Нет Rigidbody на объекте!");
        
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.maxLinearVelocity = maxSpeed * 2f; 
        }

        startPosition = transform.position;
        startRotation = transform.rotation;

        isRunning = true;
        serverThread = new Thread(new ThreadStart(StartServer));
        serverThread.IsBackground = true;
        serverThread.Start();
        Debug.Log("✅ Сервер запущен на порту " + port);
    }

    void StartServer()
    {
        try
        {
            server = new TcpListener(IPAddress.Any, port);
            server.Start();

            while (isRunning)
            {
                TcpClient client = server.AcceptTcpClient();
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int bytesRead;

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    string json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    // Сохраняем команду, НЕ очищаем её
                    lock (lockObject) { currentCommand = json; }
                }
                client.Close();
            }
        }
        catch (System.Exception e) { Debug.Log("Ошибка сервера: " + e.Message); }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            isCodeMode = !isCodeMode;
            string mode = isCodeMode ? "КОД (Python)" : "РУЧНОЙ (Клавиатура)";
            Debug.Log($"🔄 Режим переключен на: {mode}");
            
            // При переключении сбрасываем команду
            if (!isCodeMode)
            {
                lock (lockObject) { currentCommand = ""; }
                if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            transform.position = startPosition;
            transform.rotation = startRotation;
            if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            lock (lockObject) { currentCommand = ""; }
            Debug.Log("🔄 Робот сброшен на стартовую позицию!");
        }

        // РУЧНОЕ УПРАВЛЕНИЕ
        if (!isCodeMode && rb != null)
        {
            float moveInput = 0f;
            float turnInput = 0f;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) moveInput = 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) moveInput = -1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) turnInput = -1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) turnInput = 1f;

            Vector3 moveDir = transform.forward * moveInput * maxSpeed;
            rb.linearVelocity = new Vector3(moveDir.x, rb.linearVelocity.y, moveDir.z);

            if (turnInput != 0f)
            {
                float turnAngle = turnInput * rotateSpeed * Time.deltaTime;
                turnAngle = Mathf.Clamp(turnAngle, -maxTurnPerFrame, maxTurnPerFrame);
                Quaternion turn = Quaternion.Euler(0f, turnAngle, 0f);
                rb.MoveRotation(rb.rotation * turn);
            }
        }
    }

    void FixedUpdate()
    {
        if (isCodeMode)
        {
            string command = "";
            lock (lockObject)
            {
                command = currentCommand; // Читаем, но НЕ очищаем
            }

            if (command.Contains("setMotorPower") && rb != null)
            {
                float left = ExtractValue(command, "left");
                float right = ExtractValue(command, "right");

                // Движение
                float forwardSpeed = ((left + right) / 2f) / 100f * maxSpeed;
                Vector3 moveDir = transform.forward * forwardSpeed;
                rb.linearVelocity = new Vector3(moveDir.x, rb.linearVelocity.y, moveDir.z);

                // ПОВОРОТ — выполняется КАЖДЫЙ КАДР, пока команда активна
                float turnAmount = (left - right) / 100f * rotateSpeed * Time.fixedDeltaTime;
                turnAmount = Mathf.Clamp(turnAmount, -maxTurnPerFrame, maxTurnPerFrame);
                
                Quaternion turnRotation = Quaternion.Euler(0f, turnAmount, 0f);
                rb.MoveRotation(rb.rotation * turnRotation);

 //               Debug.Log($"🤖 КОД: L={left}, R={right} | Speed={forwardSpeed:F2} | Turn={turnAmount:F2}");
            }
        }
    }

    float ExtractValue(string json, string key)
    {
        int keyIndex = json.IndexOf(key);
        if (keyIndex == -1) return 0;
        int startIndex = json.IndexOf(':', keyIndex) + 1;
        int endIndex = json.IndexOf(',', startIndex);
        if (endIndex == -1) endIndex = json.IndexOf('}', startIndex);
        string valueStr = json.Substring(startIndex, endIndex - startIndex).Trim();
        float result;
        float.TryParse(valueStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out result);
        return result;
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        if (server != null) server.Stop();
        if (serverThread != null) serverThread.Abort();
    }
}