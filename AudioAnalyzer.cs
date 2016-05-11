using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class AudioAnalyzer : MonoBehaviour {

	[SerializeField]
	protected bool listen, useBakedAudio;
	
	protected AudioSource source;

	[SerializeField]
	protected AudioClip clip;
	
	protected string selectedDevice;
	protected int minFreq, maxFreq;

	//TODO: put this in another thread, see if it improves FPS on mac
	#region Unity Methods
	void Start() {
		source = GetComponent<AudioSource>();

		source.loop = true;
		source.mute = !listen; // if audio is audible through original device (such as sound system with multiple outputs) as well as through unity, there is a noticeable delay. 

		try {
			selectedDevice = Microphone.devices[0].ToString();
			Microphone.GetDeviceCaps(selectedDevice, out minFreq, out maxFreq); // get frequency range of device

			if ((minFreq + maxFreq) == 0)
				maxFreq = 48000;

			Debug.Log("selected input device: " + selectedDevice);
		} catch {
			Debug.Log("NO AUDIO DEVICE CONNECTED\nattempting to use audio from file");
			useBakedAudio = true;
		}

		if (useBakedAudio) {
			checkBakedAudio();
		}

		StartCoroutine(ManageBuffer());
	}

	void Update() {
		if (useBakedAudio) {
			if (clip != source.clip) {
				source.clip = clip;
				source.Play();
			}
		}

		if (source.mute == listen)
			source.mute = !listen;
	}
	#endregion

	#region audio buffer
	protected void SetInputDevice(string device) {
		StopMicrophone();
		selectedDevice = device;
	}

	protected void StopMicrophone() {
		source.Stop();
		Microphone.End(selectedDevice);
	}

	protected bool checkBakedAudio() {
		if (clip != null) {
			source.Stop();
			source.clip = clip;
			source.Play();
		} else {
			useBakedAudio = false; // if there's no valid audio file to play, switch back to live
			Debug.Log("no valid audio clip has been assigned");
		}

		return (clip != null);
	}

	protected IEnumerator ManageBuffer() {
		bool usingLiveAudio = false;

		while (true) {
			if (usingLiveAudio && useBakedAudio) {
				if (checkBakedAudio())
					usingLiveAudio = false;
			}

			while (useBakedAudio)
				yield return null;

			if (!usingLiveAudio) {
				source.Stop();
				source.clip = Microphone.Start(selectedDevice, true, 10, maxFreq);
				while (Microphone.GetPosition(selectedDevice) <= 0)
					yield return Microphone.GetPosition(selectedDevice);
				source.Play();
			}

			usingLiveAudio = true;

			SimpleTimer bufferTimer = new SimpleTimer(5f);
			while (!bufferTimer.isFinished && !useBakedAudio)
				yield return bufferTimer;

			// stop playing audio and halt mic recording
			source.Stop();
			Microphone.End(selectedDevice);

			// set new clip to new recording and wait for recording to being before playing source
			source.clip = Microphone.Start(selectedDevice, true, 10, maxFreq);
			while (Microphone.GetPosition(selectedDevice) <= 0)
				yield return Microphone.GetPosition(selectedDevice);

			source.Play();
		}
	}
	#endregion
}
