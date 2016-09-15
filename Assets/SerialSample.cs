using UnityEngine;
using System.Collections;

public class SerialSample : MonoBehaviour {

	public SerialHandler serial;

	// Use this for initialization
	void Start () {
		string[] names = SerialHandler.getPortNames ();
		for (int i = 0; i < names.Length; i++) {
			Debug.Log (names[i]);
		}

		serial.OnDataReceived += Serial_OnDataReceived;
	}

	void Serial_OnDataReceived (string message)
	{
		Debug.Log ("message: "+message);
	}

	// Update is called once per frame
	void Update() {

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

		// inputTest
		Event e = Event.current;

		if (Input.anyKeyDown && e.isKey && e.keyCode != KeyCode.None) {
			Debug.Log ("Detected key code: " + e.keyCode);
			serial.Write (e.keyCode + "\n");
		}

	}
}
