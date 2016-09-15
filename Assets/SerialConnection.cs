using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Timers;

public class SerialConnection : MonoBehaviour {

	private SerialPort port;
	
	public string portName = "/dev/cu.usbmodem1421";
	public int baudRate = 9600;
	
	void Start() 
	{
		port = new SerialPort ();
		Open(portName);
	}

	public void init() {
		
	}

	#region Serial Port
	public static string[] GetPortName() 
	{
		return SerialPort.GetPortNames();
	}

	public void Open(string portName) 
	{
		if(port.IsOpen) {
			Debug.LogError("Port(" + port.PortName + ") has Opened yet.");
			
		} else {

			port.PortName = portName;
			port.BaudRate = baudRate;
			//port.BaudRate = 115200;
			//port.BaudRate = 230400;
			port.Parity = Parity.None;
			port.StopBits = StopBits.One;
			port.DataBits = 8;

			try {
				port.Open();
				Debug.Log("Success: Port:" + port.PortName + " Opened.");
				
			} catch(Exception ex) {
				Debug.LogException(ex);
			}
			
		}
	}
	
	public void Close() 
	{
		if(port.IsOpen) {
			try {
				string portName = port.PortName;
				port.Close();
				port.Dispose();
				Debug.Log("Success: Port:" + portName + " Closed.");
				
			} catch(Exception ex) {
				Debug.LogException(ex);
			}
		}
	}
	
	public bool IsOpen 
	{
		get {
			return port.IsOpen;
		}
	}

	#endregion

	void OnApplicationQuit() {
		Close();
	}
	
	#region Commands

	public void SendBuffer(byte[] txBuffer)
	{
		if(IsOpen) {
			try {
				Debug.Log(txBuffer.Length);
				port.Write(txBuffer, 0, txBuffer.Length);
			} catch(Exception ex) {
				Debug.LogException(ex);
			}
		}
	}

	public void SendStrings(string strs)
	{
		if(IsOpen) {
			try {
				port.Write(strs);
			} catch(Exception ex) {
				Debug.LogException(ex);
			}
		}
	}

	#endregion
	
}
