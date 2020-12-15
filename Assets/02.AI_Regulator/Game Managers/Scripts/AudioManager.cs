using UnityEngine;
using System.Collections;

/* Audio Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

//THIS COMPONENT IS SUBJECT TO REFACTORING/REWRITE IN FUTURE UPDATES.

namespace RTSEngine
{
	public class AudioManager : MonoBehaviour {

		public static void PlayAudio (GameObject SourceObj, AudioClip Clip, bool Loop)
		{
			//First make sure that the source object has an audio source component:
			//Also make there audio clips available to play:
			if (SourceObj != null) {
				if (SourceObj.GetComponent<AudioSource> () && Clip != null) {
					AudioSource AudioSrc = SourceObj.GetComponent<AudioSource> ();
					AudioSrc.Stop (); //Stop the current audio clip from playing.

					//Randomly pick an audio clip from the cosen list
					AudioSrc.clip = Clip;
					AudioSrc.loop = Loop; //Set the loop settings

					AudioSrc.Play (); //Play it.
				}
			}
		}

		public static void StopAudio (GameObject SourceObj)
		{
			//First make sure that the source object has an audio source component:
			if (SourceObj != null) {
				if (SourceObj.GetComponent<AudioSource> ()) {
					//Stop playing audio:
					SourceObj.GetComponent<AudioSource>().Stop();
				}
			}
		}
	}
}