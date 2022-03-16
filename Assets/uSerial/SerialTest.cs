using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SerialTest : MonoBehaviour
{
    [SerializeField] SerialHandler serialHandler = null;

    // Start is called before the first frame update
    void Start()
    {
        string[] names1 = SerialHandler.GetPortNames1();
        Debug.Log(string.Join(", ", names1));
        string[] names = SerialHandler.GetPortNames();
        Debug.Log(string.Join(", ", names));

        serialHandler.OnDataReceived += SerialHandler_OnDataReceived;
    }

    private void OnDisable()
    {
        serialHandler.OnDataReceived -= SerialHandler_OnDataReceived;
        
        Send();
    }

    public void SetPortName(string name)
    {
        serialHandler.portName = name;
    }

    [SerializeField] float pitch = 0;
    [SerializeField] float roll = 0;
    [SerializeField] float yaw = 0;

    private void SerialHandler_OnDataReceived(string message)
    {
        Debug.LogWarning(message);
        string[] data = message.Split(","[0]);
        if (data.Length == 5)
        {
            float.TryParse(data[1], out pitch);
            float.TryParse(data[2], out roll);
            float.TryParse(data[3], out yaw);
        }
    }

    float time = 0;
    [SerializeField] float sendRate = 30;
    // Update is called once per frame
    void Update()
    {
        float delta = 1 / sendRate;
        time += Time.deltaTime;
        if (time > delta)
        {
            time = time - delta;
            Send();
        }
    }

    [ContextMenu("Send")]
    void Send()
    {

    }

    void SendColorByte(Color32 c)
    {
        serialHandler.Write(new byte[] { c.r, c.g, c.b }, 0, 3);
    }
    void SendBytes(byte v)
    {
        serialHandler.Write(new byte[] {v, 0x00 }, 0, 2);
    }
    void SendBytes(byte[] v)
    {
        serialHandler.Write(v, 0, v.Length);
    }

    void Reconnect()
    {
        serialHandler.Reconnect();
    }

}
