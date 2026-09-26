using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class WebhookListener : MonoBehaviour
{
    public static WebhookListener Instance { get; private set; }

    public event Action OnBumpReceived;

    [Header("Server")]
    [SerializeField] private int port = 56789;

    [Header("Debug")]
    [SerializeField] private bool verboseLogging = true;

    private TcpListener listener;
    private Thread listenerThread;

    private volatile bool isRunning;

    private readonly ConcurrentQueue<Action> mainThreadQueue = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        Log("[Webhook] Awake.");
    }

    private void Start()
    {
        StartListener();
    }

    private void Update()
    {
        ProcessMainThreadQueue();
    }

    private void StartListener()
    {
        if (isRunning)
        {
            Log("[Webhook] Listener already running.");
            return;
        }

        isRunning = true;

        listenerThread = new Thread(ListenLoop)
        {
            IsBackground = true,
            Name = "WebhookListener"
        };

        listenerThread.Start();

        Log(
            $"[Webhook] Starting listener on http://localhost:{port}/bump"
        );
    }

    private void ListenLoop()
    {
        try
        {
            listener = new TcpListener(
                IPAddress.Loopback,
                port
            );

            listener.Start();

            QueueLog(
                $"[Webhook] SERVER STARTED successfully on localhost:{port}"
            );

            while (isRunning)
            {
                TcpClient client;

                try
                {
                    client = listener.AcceptTcpClient();
                }
                catch (SocketException)
                {
                    if (!isRunning)
                        break;

                    throw;
                }

                QueueLog(
                    "[Webhook] Client connected."
                );

                ThreadPool.QueueUserWorkItem(
                    _ => HandleClient(client)
                );
            }
        }
        catch (Exception exception)
        {
            QueueLogError(
                $"[Webhook] SERVER ERROR: {exception}"
            );

            isRunning = false;
        }
    }

    private void HandleClient(TcpClient client)
    {
        try
        {
            using (client)
            using (NetworkStream stream = client.GetStream())
            {
                stream.ReadTimeout = 3000;
                stream.WriteTimeout = 3000;

                byte[] buffer = new byte[4096];

                int bytesRead = stream.Read(
                    buffer,
                    0,
                    buffer.Length
                );

                if (bytesRead <= 0)
                {
                    QueueLogWarning(
                        "[Webhook] Client connected but sent no data."
                    );

                    return;
                }

                string request = Encoding.ASCII.GetString(
                    buffer,
                    0,
                    bytesRead
                );

                // Log the first request line.
                string requestLine = GetRequestLine(request);

                QueueLog(
                    $"[Webhook] REQUEST: {requestLine}"
                );

                if (!IsBumpRequest(request))
                {
                    QueueLogWarning(
                        $"[Webhook] Invalid endpoint: {requestLine}"
                    );

                    SendResponse(
                        stream,
                        404,
                        "Not Found"
                    );

                    return;
                }

                QueueLog(
                    "[Webhook] /bump RECEIVED!"
                );

                mainThreadQueue.Enqueue(
                    TriggerBumpOnMainThread
                );

                SendResponse(
                    stream,
                    200,
                    "OK"
                );
            }
        }
        catch (Exception exception)
        {
            QueueLogError(
                $"[Webhook] Request error: {exception}"
            );
        }
    }

    private string GetRequestLine(string request)
    {
        if (string.IsNullOrEmpty(request))
            return "<empty>";

        int lineEnd = request.IndexOf(
            '\n'
        );

        if (lineEnd < 0)
            return request.Trim();

        return request
            .Substring(0, lineEnd)
            .Trim();
    }

    private bool IsBumpRequest(string request)
    {
        if (string.IsNullOrEmpty(request))
            return false;

        string[] lines = request.Split(
            new[] { "\r\n", "\n" },
            StringSplitOptions.None
        );

        if (lines.Length == 0)
            return false;

        string requestLine = lines[0];

        bool isGet =
            requestLine.StartsWith(
                "GET /bump",
                StringComparison.OrdinalIgnoreCase
            );

        bool isPost =
            requestLine.StartsWith(
                "POST /bump",
                StringComparison.OrdinalIgnoreCase
            );

        return isGet || isPost;
    }

    private void SendResponse(
        NetworkStream stream,
        int statusCode,
        string body)
    {
        string responseBody =
            body ?? string.Empty;

        byte[] bodyBytes =
            Encoding.UTF8.GetBytes(responseBody);

        string response =
            $"HTTP/1.1 {statusCode} {GetStatusText(statusCode)}\r\n" +
            "Content-Type: text/plain; charset=UTF-8\r\n" +
            $"Content-Length: {bodyBytes.Length}\r\n" +
            "Connection: close\r\n" +
            "\r\n";

        byte[] headerBytes =
            Encoding.ASCII.GetBytes(response);

        stream.Write(
            headerBytes,
            0,
            headerBytes.Length
        );

        if (bodyBytes.Length > 0)
        {
            stream.Write(
                bodyBytes,
                0,
                bodyBytes.Length
            );
        }

        stream.Flush();
    }

    private string GetStatusText(int statusCode)
    {
        return statusCode switch
        {
            200 => "OK",
            404 => "Not Found",
            _ => "Error"
        };
    }

    private void ProcessMainThreadQueue()
    {
        while (mainThreadQueue.TryDequeue(out Action action))
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[Webhook] Main-thread error: {exception}"
                );
            }
        }
    }

    private void TriggerBumpOnMainThread()
    {
        Debug.Log(
            "[Webhook] BUMP EVENT -> MAIN THREAD"
        );

        if (OnBumpReceived == null)
        {
            Debug.LogError(
                "[Webhook] NO SUBSCRIBERS! " +
                "BumpEventController is not subscribed."
            );

            return;
        }

        Debug.Log(
            $"[Webhook] Subscribers found. " +
            $"Invoking {OnBumpReceived.GetInvocationList().Length} listener(s)."
        );

        OnBumpReceived.Invoke();
    }

    // --------------------------------------------------
    // DEBUG
    // --------------------------------------------------

    public void TestBump()
    {
        Debug.Log(
            "[Webhook] MANUAL TEST BUMP."
        );

        TriggerBumpOnMainThread();
    }

    private void QueueLog(string message)
    {
        mainThreadQueue.Enqueue(
            () => Log(message)
        );
    }

    private void QueueLogWarning(string message)
    {
        mainThreadQueue.Enqueue(
            () => Debug.LogWarning(message)
        );
    }

    private void QueueLogError(string message)
    {
        mainThreadQueue.Enqueue(
            () => Debug.LogError(message)
        );
    }

    private void Log(string message)
    {
        if (verboseLogging)
            Debug.Log(message);
    }

    private void OnDestroy()
    {
        StopListener();

        if (Instance == this)
            Instance = null;
    }

    private void OnApplicationQuit()
    {
        StopListener();
    }

    private void StopListener()
    {
        if (!isRunning)
            return;

        isRunning = false;

        try
        {
            listener?.Stop();
        }
        catch
        {
        }

        listener = null;
        listenerThread = null;
    }
}