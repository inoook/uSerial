using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System;
using System.IO;
using Microsoft.Win32;

public class SerialHandler : MonoBehaviour
{
	// http://answers.unity3d.com/questions/643078/serialportsgetportnames-error.html
	// https://github.com/mono/mono/blob/master/mcs/class/System/System.IO.Ports/SerialPort.cs
	public static string[] getPortNames ()
	{
		int p = (int)Environment.OSVersion.Platform;
		List<string> serial_ports = new List<string> ();

		// Are we on Unix?
		if (p == 4 || p == 128 || p == 6) {
			string[] ttys = Directory.GetFiles ("/dev/", "tty.*");
			foreach (string dev in ttys) {
				if (dev.StartsWith ("/dev/tty.")) {
					serial_ports.Add (dev);
				}
			}
		}else{
			using (RegistryKey subkey = Registry.LocalMachine.OpenSubKey("HARDWARE\\DEVICEMAP\\SERIALCOMM"))
			{
				if (subkey != null) {
					string[] names = subkey.GetValueNames();
					foreach (string value in names) {
						string port = subkey.GetValue(value, "").ToString();
						if (port != "")
							serial_ports.Add(port);
					}
				}
			}
		}
		return serial_ports.ToArray ();
	}

	public delegate void SerialDataReceivedEventHandler(string message);
	public event SerialDataReceivedEventHandler OnDataReceived;
	
	public string portName = "/dev/cu.usbmodem1421";
	public int baudRate    = 115200;
	
	private SerialPort serialPort_;
	private Thread thread_;
	private bool isRunning_ = false;

	public bool enableRead = false;

	private Queue messageQueue;
	private Queue sendQueue;

	public int maxUnreadMessages = 1;

	void Start()
	{
		Open();
	}
	
	void Update()
	{
		if(messageQueue != null){
			lock(messageQueue.SyncRoot){
				if(messageQueue.Count > 0){

					if(OnDataReceived != null){
						string msg = messageQueue.Dequeue().ToString();
						OnDataReceived( msg );
					}
				}
			}
		}
	}
	
	void OnDestroy()
	{
		Close();
	}
	
	private void Open()
	{
		serialPort_ = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);

		serialPort_.ReadTimeout = 20;
		serialPort_.WriteTimeout = 20;
		serialPort_.NewLine = "\r";

		messageQueue = Queue.Synchronized(new Queue());
		sendQueue = Queue.Synchronized(new Queue());

		try{
			serialPort_.Open();
			
			if(enableRead){
				isRunning_ = true;

				thread_ = new Thread(Read);
				thread_.Start();
			}
		}catch{
			Debug.LogWarning("No serialPort");
		}
	}
	
	private void Close()
	{
		if (thread_ != null && isRunning_)
		{
			isRunning_ = false;
			thread_.Abort();
			thread_ = null;
		}
		
		if (serialPort_ != null && serialPort_.IsOpen) {
			serialPort_.Close();
			serialPort_.Dispose();
			serialPort_ = null;
		}

		if(messageQueue != null){
			messageQueue = null;
		}
		if(sendQueue != null){
			sendQueue = null;
		}
	}
	
	private void Read()
	{
		while (isRunning_ && serialPort_ != null && serialPort_.IsOpen) {

			// send
			if (sendQueue.Count != 0)
			{
				string outputMessage = (string)sendQueue.Dequeue();
				serialPort_.WriteLine(outputMessage);
			}

			// receive
			try {
//					string message = serialPort_.ReadTo("\r");
				string message = serialPort_.ReadLine ();
				if (message != null && message != "") {
					if(messageQueue.Count < maxUnreadMessages){
						messageQueue.Enqueue( message );
					}
				}
			} catch {
//					Thread.Sleep (20);
			}
		}
	}
	
	public void Write(string message)
	{
		try {
			sendQueue.Enqueue(message);
		} catch (System.Exception e) {
			Debug.LogWarning(e.Message);
		}
	}

	public void Write(byte[] buffer, int offset, int count)
	{
		try {
			if( serialPort_.IsOpen ){
				serialPort_.Write(buffer, offset, count);
			}
		} catch (System.Exception e) {
			Debug.LogWarning(e.Message);
		}
	}

	public bool IsOpen()
	{
		if (serialPort_ == null) {
			return false;
		}
		return serialPort_.IsOpen;
	}
}