using UnityEngine;
using System.Collections;

public class SerialSample : MonoBehaviour {

	public SerialHandler serial;

	// Use this for initialization
	void Start () {
		serial.OnDataReceived += Serial_OnDataReceived;
	}

	void Serial_OnDataReceived (string message)
	{
		
	}

	// Update is called once per frame
	void Update () {

	}


	public string str;
	void OnGUI()
	{
		GUILayout.BeginArea(new Rect(10,10,100,100));
		str = GUILayout.TextField(str);
		if(GUILayout.Button("SEND")){
			serial.Write(str+"\r");
		}
		GUILayout.EndArea();
	}
}
