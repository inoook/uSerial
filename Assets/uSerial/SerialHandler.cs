using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System;
using System.IO;
using Microsoft.Win32;
using System.Threading.Tasks;

/// <summary>
/// Unity 2019.4.12f1
/// Serial接続
/// Win <-> Arduino(M5Atomなど)での動作確認済み
/// Macでの動作は未検証
/// </summary>
public class SerialHandler : MonoBehaviour
{
	// http://answers.unity3d.com/questions/643078/serialportsgetportnames-error.html
	// https://github.com/mono/mono/blob/master/mcs/class/System/System.IO.Ports/SerialPort.cs
	// https://nn-hokuson.hatenablog.com/entry/2017/09/12/192024
	// https://nn-hokuson.hatenablog.com/entry/2018/02/01/203114

	public static string[] GetPortNames1()
	{
		return SerialPort.GetPortNames();
	}

	public static string[] GetPortNames()
	{
		int p = (int)Environment.OSVersion.Platform;
		List<string> serial_ports = new List<string>();

		// Are we on Unix?
		if (p == 4 || p == 128 || p == 6)
		{
			string[] ttys = Directory.GetFiles("/dev/", "tty.*");
			foreach (string dev in ttys)
			{
				if (dev.StartsWith("/dev/tty."))
				{
					serial_ports.Add(dev);
				}
			}
		}
		else
		{
			using (RegistryKey subkey = Registry.LocalMachine.OpenSubKey("HARDWARE\\DEVICEMAP\\SERIALCOMM"))
			{
				if (subkey != null)
				{
					string[] names = subkey.GetValueNames();
					foreach (string value in names)
					{
						string port = subkey.GetValue(value, "").ToString();
						if (port != "")
							serial_ports.Add(port);
					}
				}
			}
		}
		return serial_ports.ToArray();
	}

	public delegate void SerialDataReceivedEventHandler(string message);
	public event SerialDataReceivedEventHandler OnDataReceived;

	public string portName = "COM4";
	public int baudRate = 115200;

	private SerialPort serialPort;
	private Thread thread;
	private bool isRunning = false;

	public bool enableRead = false;

	private Queue receiveQueue;
	private Queue sendQueue;

	public int maxUnreadMessages = 10;

	void Start()
	{
		_ = OpenAsync();
	}

	void Update()
	{
		if (receiveQueue != null)
		{
			lock (receiveQueue.SyncRoot)
			{
				if (receiveQueue.Count > 0)
				{
					if (OnDataReceived != null)
					{
						while (receiveQueue.Count > 0)
						{
							string msg = receiveQueue.Dequeue().ToString();
							OnDataReceived(msg);
						}
					}
				}
			}
		}

		if (serialPort != null && IsOpen())
		{
			// 接続が切れた時にfalseになる
			CtsHolding = serialPort.CtsHolding;
			CDHolding = serialPort.CDHolding;
			DsrHolding = serialPort.DsrHolding;

			if (enableReconnect)
			{
				if (!CtsHolding) // M5でBluetooth接続のときは、接続が切れた時にfalseになる
				{
					_ = StartReconnect();
				}
			}
		}
	}

	[SerializeField] bool CDHolding = false; // ポートのキャリア検出ラインの状態を取得します。
	[SerializeField] bool CtsHolding = false;
	[SerializeField] bool DsrHolding = false;
	[SerializeField] bool enableReconnect = true;

	[SerializeField] bool isWriteTimeOut = false;
	public bool IsWriteTimeOut => isWriteTimeOut;

	void OnDestroy()
	{
		Close();
	}

	async Task OpenAsync()
    {
		await Task.Run(() => {
			Open();
		});
    }

	private void Open()
	{
		serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);

		Debug.LogWarning($"Open SerialPort / PortName: {serialPort.PortName} baudRate:{serialPort.BaudRate}");

		serialPort.ReadTimeout = 20;
		serialPort.WriteTimeout = 20;
		serialPort.NewLine = "\r";

		serialPort.WriteBufferSize = 10;
		//
		receiveQueue = Queue.Synchronized(new Queue());
		sendQueue = Queue.Synchronized(new Queue());

		try
		{
			serialPort.Open();

			if (enableRead)
			{
				isRunning = true;

				thread = new Thread(Read);
				thread.Start();
			}
		}
		catch
		{
			Debug.LogError($"No serialPort {serialPort.PortName}");
			//Reconnect();
		}
	}


    public void Reconnect()
    {
		Close();
		Open();
	}

	private void Close()
	{
		if (thread != null && isRunning)
		{
			isRunning = false;
			thread.Abort();
			thread = null;
		}

		if (serialPort != null && serialPort.IsOpen)
		{
			//serialPort_.BaseStream.Flush();
			serialPort.Close();
			serialPort.Dispose();
			serialPort = null;
		}

		if (receiveQueue != null)
		{
			receiveQueue = null;
		}
		if (sendQueue != null)
		{
			sendQueue = null;
		}
	}

	private void Read()
	{
		while (isRunning && serialPort != null && serialPort.IsOpen)
		{
			// send
			if (sendQueue.Count != 0)
			{
				string outputMessage = (string)sendQueue.Dequeue();
				serialPort.WriteLine(outputMessage);
			}

			// receive
			try
			{
				//string message = serialPort_.ReadTo("\r");
				string message = serialPort.ReadLine();
				if (message != null && message != "")
				{
                    if (receiveQueue.Count < maxUnreadMessages)
                    {
                        receiveQueue.Enqueue(message);
                    }
                    else
                    {
						Debug.LogWarning("messageQueue.Count over: "+ receiveQueue.Count);
                    }
                }
			}
			catch
			{
				//Thread.Sleep (20);
			}
		}
	}

	public void Write(string message)
	{
		try
		{
			sendQueue.Enqueue(message);
		}
		catch (System.Exception e)
		{
			Debug.LogWarning(e.Message);
		}
	}

	public void Write(byte[] buffer, int offset, int count)
	{
		try
		{
			isWriteTimeOut = false;

			if (serialPort.IsOpen)
			{
				serialPort.Write(buffer, offset, count);
			}
            else
            {
				//Debug.LogWarning("Not Open");
            }
		}
		catch (Exception e)
		{
			if (e is TimeoutException)
			{
				Debug.LogWarning(e.GetType() + " / " + e.Message);
				isWriteTimeOut = true;
			}
        }
        finally
        {
			
        }
	}

	bool isReconnect = false;
	async Task StartReconnect()
    {
        if (isReconnect) { return; }

		Debug.LogError("Start Reconnect");

		isReconnect = true;

		Debug.LogWarning("Close");

		Close();

		Debug.LogWarning("CloseEnd");

		await Task.Delay(1000);

		Debug.LogWarning("StartOpen_Reconnect");

		await OpenAsync();
		isReconnect = false;

		Debug.LogWarning("End_Recconect");
	}

	public bool IsOpen()
	{
		if (serialPort == null)
		{
			return false;
		}
		return serialPort.IsOpen;
	}

	public void DrawGUI()
    {

    }
}