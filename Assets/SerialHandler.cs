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
	public delegate void SerialDataReceivedEventHandler(string message);
	public event SerialDataReceivedEventHandler OnDataReceived;
	
	public string portName = "/dev/cu.usbmodem1421";
	public int baudRate    = 115200;
	
	private SerialPort serialPort_;
	private Thread thread_;
	private bool isRunning_ = false;
	
	private string message_;
	private bool isNewMessageReceived_ = false;

	public bool enableRead = false;

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

	void Start()
	{
		Open();
	}
	
	void Update()
	{
		if (isNewMessageReceived_) {
			OnDataReceived(message_);
			isNewMessageReceived_ = false;
		}
	}
	
	void OnDestroy()
	{
		Close();
	}
	
	private void Open()
	{
		serialPort_ = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);

		serialPort_.ReadTimeout = 20; // win
		serialPort_.NewLine = "\r";// win

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
	}
	
	private void Read()
	{
		while (isRunning_ && serialPort_ != null && serialPort_.IsOpen) {
			// mac
			if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer) {
				try {
					if (serialPort_.BytesToRead > 0) {
//					message_ = serialPort_.ReadLine();
						message_ = serialPort_.ReadExisting ();
						Debug.Log (">> OSX message: " + message_);
						isNewMessageReceived_ = true;
					} else {
						Thread.Sleep (20);
					}
				} catch (System.Exception e) {
					Debug.LogWarning (e.Message);
				}
			}

			// win
			if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer) {
				try {
					// message = serialPort_.ReadTo("\r");
					string message = serialPort_.ReadLine ();
					if (message != "") {;
						message_ = message;
						Debug.Log (">> Win message: " + message);
						isNewMessageReceived_ = true;
					}
//				} catch (System.Exception e) {
				} catch {
					Thread.Sleep (20);
				}
			}
		}
	}
	
	public void Write(string message)
	{
		try {
			if( serialPort_.IsOpen ){
				serialPort_.Write(message);
			}
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