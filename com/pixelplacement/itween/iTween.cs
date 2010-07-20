#region Namespaces
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
#endregion

/// <summary>
/// <para>Version: 2.0.0</para>	 
/// <para>Author: Bob Berkebile (http://pixelplacement.com)</para>
/// <para>Contributors: Patrick Corkum (http://insquare.com)</para>
/// <para>Support: http://itween.pixelplacement.com</para>
/// </summary>
public class iTween : MonoBehaviour{
	
	#region Variables
	
	//repository of all living iTweens:
	public static ArrayList tweens = new ArrayList();
	
	//status members (made public for visual troubleshooting in the inspector):
	public string id, type, method;
	public iTween.EaseType easeType;
	public float time, delay;
	public LoopType loopType;
	public bool isRunning,isPaused;
		
	//private members:
 	private float runningTime, percentage;
	protected float delayStarted; //probably not neccesary that this be protected but it shuts Unity's compiler up about this being "never used"
	private bool kinematic, isLocal, loop, reverse;
	private Hashtable tweenArguments;
	private Space space;
	private delegate float EasingFunction(float start, float end, float value);
	private delegate void ApplyTween();
	private EasingFunction ease;
	private ApplyTween apply;
	private AudioSource audioSource;
	private Vector3[] vector3s;
	private Vector2[] vector2s;
	private Color[] colors;
	private float[] floats;
	private int[] ints;
	private Rect[] rects;	
	
	/// <summary>
	/// The type of easing to use based on Robert Penner's open source easing equations (http://www.robertpenner.com/easing_terms_of_use.html).
	/// </summary>
	public enum EaseType{
		easeInQuad,
		easeOutQuad,
		easeInOutQuad,
		easeInCubic,
		easeOutCubic,
		easeInOutCubic,
		easeInQuart,
		easeOutQuart,
		easeInOutQuart,
		easeInQuint,
		easeOutQuint,
		easeInOutQuint,
		easeInSine,
		easeOutSine,
		easeInOutSine,
		easeInExpo,
		easeOutExpo,
		easeInOutExpo,
		easeInCirc,
		easeOutCirc,
		easeInOutCirc,
		linear,
		spring,
		bounce,
		easeInBack,
		easeOutBack,
		easeInOutBack,
		punch
	}
	
	/// <summary>
	/// The type of loop (if any) to use.  
	/// </summary>
	public enum LoopType{
		/// <summary>
		/// Do not loop.
		/// </summary>
		none,
		/// <summary>
		/// Rewind and replay.
		/// </summary>
		loop,
		/// <summary>
		/// Ping pong the animation back and forth.
		/// </summary>
		pingPong
	}

	/// <summary>
	/// Sets the interpolation equation that the curveTo and curveFrom methods use to calculate how they create thier curves. 
	/// </summary>
	public enum CurveType{
		/// <summary>
		/// The path's curves will exaggerate in and out of control points depending on the amount of travel time available.
		/// </summary>
		bezier,
		/// <summary>
		/// The path's curves are set strictly via Hermite Curve interpolation (better description needed).
		/// </summary>
		hermite
	}
			
	#endregion
	
	#region Defaults
	
	/// <summary>
	/// A collection of baseline presets that iTween needs and utilizes if certain parameters are not provided. 
	/// </summary>
	public static class Defaults{
		//general defaults:
		public static float time = 1f;
		public static float delay = 0f;	
		public static LoopType loopType = LoopType.none;
		public static EaseType easeType = iTween.EaseType.easeOutExpo;
		public static float lookSpeed = 3f;
		public static bool isLocal = false;
		public static Space space = Space.Self;
		public static bool orientToPath = false;
		public static CurveType curveType = CurveType.bezier; // clean this up!
		//update defaults:
		public static float smoothTime = .06f;
		//cameraFade defaults:
		public static int cameraFadeDepth = 999999;
	}
	
	#endregion
	
	#region #1 Static Registers
	
	/// <summary>
	/// Returns a value to an 'oncallback' method interpolated between the supplied 'from' and 'to' for application as desired.  Requires an 'oncomplete' callback that accepts the same type as the supplied 'from' and 'to' properties.
	/// </summary>
	/// <param name="from">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/> or <see cref="Vector3"/> or <see cref="Vector2"/> or <see cref="Color"/> or <see cref="Rect"/>
	/// </param> 
	/// <param name="to">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/> or <see cref="Vector3"/> or <see cref="Vector2"/> or <see cref="Color"/> or <see cref="Rect"/>
	/// </param> 
	/// <param name="time">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="delay">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="easetype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>   
	/// <param name="looptype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>
	/// <param name="onstart">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onstarttarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onstartparams">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="onupdate">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onupdatetarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onupdateparams">
	/// A <see cref="System.Object"/>
	/// </param> 
	/// <param name="oncomplete">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="oncompletetarget">
	/// A <see cref="GameObject"/>.
	/// </param>
	/// <param name="oncompleteparams">
	/// A <see cref="System.Object"/>
	/// </param>
	
	public static void ValueTo(GameObject target, Hashtable args){
		//clean args:
		args = iTween.CleanArgs(args);
		
		if (!args.Contains("onupdate") || !args.Contains("from") || !args.Contains("to")) {
			Debug.LogError("iTween Error: ValueTo() requires an 'onupdate' callback function and a 'from' and 'to' property.  The supplied 'onupdate' callback must accept a single argument that is the same type as the supplied 'from' and 'to' properties!");
			return;
		}else{
			//establish iTween:
			args["type"]="value";
			
			if (args["from"].GetType() == typeof(Vector2)) {
				args["method"]="vector2";
			}else if (args["from"].GetType() == typeof(Vector3)) {
				args["method"]="vector3";
			}else if (args["from"].GetType() == typeof(Rect)) {
				args["method"]="rect";
			}else if (args["from"].GetType() == typeof(Single)) {
				args["method"]="float";
			}else if (args["from"].GetType() == typeof(Color)) {
				args["method"]="color";
			}else{
				Debug.LogError("iTween Error: ValueTo() only works with interpolating Vector3s, Vector2s, floats, ints, Rects and Colors!");
				return;	
			}
			
			//set a default easeType of linear if none is supplied since eased color interpolation is nearly unrecognizable:
			if (!args.Contains("easetype")) {
				args.Add("easetype",EaseType.linear);
			}
			
			Launch(target,args);
		}
	}
	
	/// <summary>
	/// Changes a GameObject's alpha value instantly then returns it to the provided alpha over time.  If a GUIText or GUITexture component is attached, it will become the target of the animation. Identical to using ColorFrom and using the "a" parameter.
	/// </summary>
	/// <param name="alpha">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="includechildren">
	/// A <see cref="System.Boolean"/>
	/// </param>
	/// <param name="time">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="delay">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="easetype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>   
	/// <param name="looptype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>
	/// <param name="onstart">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onstarttarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onstartparams">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="onupdate">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onupdatetarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onupdateparams">
	/// A <see cref="System.Object"/>
	/// </param> 
	/// <param name="oncomplete">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="oncompletetarget">
	/// A <see cref="GameObject"/>.
	/// </param>
	/// <param name="oncompleteparams">
	/// A <see cref="System.Object"/>
	/// </param>
	public static void FadeFrom(GameObject target, Hashtable args){	
		args["a"]=args["alpha"];
		ColorFrom(target,args);
	}		

	/// <summary>
	/// Changes a GameObject's alpha value over time.  If a GUIText or GUITexture component is attached, it will become the target of the animation. Identical to using ColorTo and using the "a" parameter.
	/// </summary>
	/// <param name="alpha">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="includechildren">
	/// A <see cref="System.Boolean"/>
	/// </param>
	/// <param name="time">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="delay">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="easetype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>   
	/// <param name="looptype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>
	/// <param name="onstart">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onstarttarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onstartparams">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="onupdate">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onupdatetarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onupdateparams">
	/// A <see cref="System.Object"/>
	/// </param> 
	/// <param name="oncomplete">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="oncompletetarget">
	/// A <see cref="GameObject"/>.
	/// </param>
	/// <param name="oncompleteparams">
	/// A <see cref="System.Object"/>
	/// </param>
	public static void FadeTo(GameObject target, Hashtable args){	
		args["a"]=args["alpha"];
		ColorTo(target,args);
	}		
	
	/// <summary>
	/// Changes a GameObject's color values instantly then returns them to the provided properties over time.  If a GUIText or GUITexture component is attached, it will become the target of the animation.
	/// </summary>
	/// <param name="color">
	/// A <see cref="Color"/>
	/// </param>
	/// <param name="r">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="g">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="b">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="a">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param> 
	/// <param name="includechildren">
	/// A <see cref="System.Boolean"/>
	/// </param>
	/// <param name="time">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="delay">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="easetype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>   
	/// <param name="looptype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>
	/// <param name="onstart">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onstarttarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onstartparams">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="onupdate">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onupdatetarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onupdateparams">
	/// A <see cref="System.Object"/>
	/// </param> 
	/// <param name="oncomplete">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="oncompletetarget">
	/// A <see cref="GameObject"/>.
	/// </param>
	/// <param name="oncompleteparams">
	/// A <see cref="System.Object"/>
	/// </param>
	public static void ColorFrom(GameObject target, Hashtable args){	
		Color fromColor = new Color();
		Color tempColor = new Color();
		
		//clean args:
		args = iTween.CleanArgs(args);
		
		//handle children:
		if(!args.Contains("includechildren") || (bool)args["includechildren"]){
			foreach(Transform child in target.transform){
				Hashtable argsCopy = (Hashtable)args.Clone();
				argsCopy["ischild"]=true;
				ColorFrom(child.gameObject,argsCopy);
			}
		}
		
		//set a default easeType of linear if none is supplied since eased color interpolation is nearly unrecognizable:
		if (!args.Contains("easetype")) {
			args.Add("easetype",EaseType.linear);
		}
		
		//set tempColor and base fromColor:
		if(target.GetComponent(typeof(GUITexture))){
			tempColor=fromColor=target.guiTexture.color;	
		}else if(target.GetComponent(typeof(GUIText))){
			tempColor=fromColor=target.guiText.material.color;
		}else if(target.renderer){
			tempColor=fromColor=target.renderer.material.color;
		}else if(target.light){
			tempColor=fromColor=target.light.color;
		}
		
		//set augmented fromColor:
		if(args.Contains("color")){
			fromColor=(Color)args["color"];
		}else{
			if (args.Contains("r")) {
				fromColor.r=(float)args["r"];
			}
			if (args.Contains("g")) {
				fromColor.g=(float)args["g"];
			}
			if (args.Contains("b")) {
				fromColor.b=(float)args["b"];
			}
			if (args.Contains("a")) {
				fromColor.a=(float)args["a"];
			}
		}
		
		//apply fromColor:
		if(target.GetComponent(typeof(GUITexture))){
			target.guiTexture.color=fromColor;	
		}else if(target.GetComponent(typeof(GUIText))){
			target.guiText.material.color=fromColor;
		}else if(target.renderer){
			target.renderer.material.color=fromColor;
		}else if(target.light){
			target.light.color=fromColor;
		}
		
		//set new color arg:
		args["color"]=tempColor;
		
		//establish iTween:
		args["type"]="color";
		args["method"]="to";
		Launch(target,args);
	}		
	
	/// <summary>
	/// Changes a GameObject's color values over time.  If a GUIText or GUITexture component is attached, they will become the target of the animation.
	/// </summary>
	/// <param name="color">
	/// A <see cref="Color"/>
	/// </param>
	/// <param name="r">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="g">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="b">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="a">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param> 
	/// <param name="includechildren">
	/// A <see cref="System.Boolean"/>
	/// </param>
	/// <param name="time">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="delay">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="easetype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>   
	/// <param name="looptype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>
	/// <param name="onstart">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onstarttarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onstartparams">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="onupdate">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onupdatetarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onupdateparams">
	/// A <see cref="System.Object"/>
	/// </param> 
	/// <param name="oncomplete">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="oncompletetarget">
	/// A <see cref="GameObject"/>.
	/// </param>
	/// <param name="oncompleteparams">
	/// A <see cref="System.Object"/>
	/// </param>
	public static void ColorTo(GameObject target, Hashtable args){	
		//clean args:
		args = iTween.CleanArgs(args);
		
		//handle children:
		if(!args.Contains("includechildren") || (bool)args["includechildren"]){
			foreach(Transform child in target.transform){
				Hashtable argsCopy = (Hashtable)args.Clone();
				argsCopy["ischild"]=true;
				ColorTo(child.gameObject,argsCopy);
			}
		}
		
		//set a default easeType of linear if none is supplied since eased color interpolation is nearly unrecognizable:
		if (!args.Contains("easetype")) {
			args.Add("easetype",EaseType.linear);
		}
		
		//establish iTween:
		args["type"]="color";
		args["method"]="to";
		Launch(target,args);
	}	
	
	/// <summary>
	/// Instantly changes an AudioSource's volume and pitch then returns it to it's starting volume and pitch over time. Default AudioSource attached to GameObject will be used (if one exists) if not supplied. 
	/// </summary>
	/// <param name="audiosource">
	/// A <see cref="AudioSource"/>
	/// </param> 
	/// <param name="volume">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="pitch">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="delay">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="easetype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param> 
	/// <param name="onstart">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onstarttarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onstartparams">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="onupdate">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onupdatetarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onupdateparams">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="oncomplete">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="oncompletetarget">
	/// A <see cref="GameObject"/>.
	/// </param>
	/// <param name="oncompleteparams">
	/// A <see cref="System.Object"/>
	/// </param>
	public static void AudioFrom(GameObject target, Hashtable args){
		Vector2 tempAudioProperties;
		Vector2 fromAudioProperties;
		AudioSource tempAudioSource;
		
		//clean args:
		args = iTween.CleanArgs(args);
		
		//set tempAudioSource:
		if(args.Contains("audiosource")){
			tempAudioSource=(AudioSource)args["audiosource"];
		}else{
			if(target.GetComponent(typeof(AudioSource))){
				tempAudioSource=target.audio;
			}else{
				//throw error if no AudioSource is available:
				Debug.LogError("iTween Error: AudioFrom requires an AudioSource.");
				return;
			}
		}			
		
		//set tempAudioProperties:
		tempAudioProperties.x=fromAudioProperties.x=tempAudioSource.volume;
		tempAudioProperties.y=fromAudioProperties.y=tempAudioSource.pitch;
		
		//set augmented fromAudioProperties:
		if(args.Contains("volume")){
			fromAudioProperties.x=(float)args["volume"];
		}
		if(args.Contains("pitch")){
			fromAudioProperties.y=(float)args["pitch"];
		}
		
		//apply fromAudioProperties:
		tempAudioSource.volume=fromAudioProperties.x;
		tempAudioSource.pitch=fromAudioProperties.y;
				
		//set new volume and pitch args:
		args["volume"]=tempAudioProperties.x;
		args["pitch"]=tempAudioProperties.y;
		
		//set a default easeType of linear if none is supplied since eased audio interpolation is nearly unrecognizable:
		if (!args.Contains("easetype")) {
			args.Add("easetype",EaseType.linear);
		}
		
		//establish iTween:
		args["type"]="audio";
		args["method"]="to";
		Launch(target,args);			
	}		

	/// <summary>
	/// Fades volume and pitch of an AudioSource.  Default AudioSource attached to GameObject will be used (if one exists) if not supplied. 
	/// </summary>
	/// <param name="audiosource">
	/// A <see cref="AudioSource"/>
	/// </param> 
	/// <param name="volume">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="pitch">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="delay">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="easetype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param> 
	/// <param name="onstart">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onstarttarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onstartparams">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="onupdate">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onupdatetarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onupdateparams">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="oncomplete">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="oncompletetarget">
	/// A <see cref="GameObject"/>.
	/// </param>
	/// <param name="oncompleteparams">
	/// A <see cref="System.Object"/>
	/// </param>
	public static void AudioTo(GameObject target, Hashtable args){
		//clean args:
		args = iTween.CleanArgs(args);
		
		//set a default easeType of linear if none is supplied since eased audio interpolation is nearly unrecognizable:
		if (!args.Contains("easetype")) {
			args.Add("easetype",EaseType.linear);
		}
		
		//establish iTween:
		args["type"]="audio";
		args["method"]="to";
		Launch(target,args);			
	}	
	
	/// <summary>
	/// Plays an AudioClip once based on supplied volume and pitch and following any delay. AudioSource is optional as iTween will provide one.
	/// </summary>
	/// <param name="audioclip">
	/// A <see cref="AudioClip"/>
	/// </param> 
	/// <param name="audiosource">
	/// A <see cref="AudioSource"/>
	/// </param> 
	/// <param name="volume">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="pitch">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="delay">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="onstart">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onstarttarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onstartparams">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="onupdate">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onupdatetarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onupdateparams">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="oncomplete">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="oncompletetarget">
	/// A <see cref="GameObject"/>.
	/// </param>
	/// <param name="oncompleteparams">
	/// A <see cref="System.Object"/>
	/// </param>
	public static void Stab(GameObject target, Hashtable args){
		//clean args:
		args = iTween.CleanArgs(args);
		
		//establish iTween:
		args["type"]="stab";
		Launch(target,args);			
	}
	
	/// <summary>
	/// Instantly rotates a GameObject to look at a supplied Transform or Vector3 then returns it to it's starting rotation over time (if allowed). 
	/// </summary>
	/// <param name="looktarget">
	/// A <see cref="Transform"/> or <see cref="Vector3"/>
	/// </param>
	/// <param name="axis">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="time">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="delay">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="easetype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>   
	/// <param name="looptype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>
	/// <param name="onstart">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onstarttarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onstartparams">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="onupdate">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onupdatetarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onupdateparams">
	/// A <see cref="System.Object"/>
	/// </param> 
	/// <param name="oncomplete">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="oncompletetarget">
	/// A <see cref="GameObject"/>.
	/// </param>
	/// <param name="oncompleteparams">
	/// A <see cref="System.Object"/>
	/// </param>
	public static void LookFrom(GameObject target, Hashtable args){
		Vector3 tempRotation;
		Vector3 tempRestriction;
		
		//clean args:
		args = iTween.CleanArgs(args);
		
		//set look:
		tempRotation=target.transform.eulerAngles;
		if (args["looktarget"].GetType() == typeof(Transform)) {
			target.transform.LookAt((Transform)args["looktarget"]);
		}else if(args["looktarget"].GetType() == typeof(Vector3)){
			target.transform.LookAt((Vector3)args["looktarget"]);
		}
		
		//axis restriction:
		if(args.Contains("axis")){
			tempRestriction=target.transform.eulerAngles;
			switch((string)args["axis"]){
				case "x":
				 	tempRestriction.y=tempRotation.y;
					tempRestriction.z=tempRotation.z;
				break;
				case "y":
					tempRestriction.x=tempRotation.x;
					tempRestriction.z=tempRotation.z;
				break;
				case "z":
					tempRestriction.x=tempRotation.x;
					tempRestriction.y=tempRotation.y;
				break;
			}
			target.transform.eulerAngles=tempRestriction;
		}		
		
		//set new rotation:
		args["rotation"] = tempRotation;
		
		//establish iTween
		args["type"]="rotate";
		args["method"]="to";
		Launch(target,args);
	}		
	
	/// <summary>
	/// Rotates a GameObject to look at a supplied Transform or Vector3 over time (if allowed). 
	/// </summary>
	/// <param name="looktarget">
	/// A <see cref="Transform"/> or <see cref="Vector3"/>
	/// </param>
	/// <param name="axis">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="time">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="delay">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="easetype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>   
	/// <param name="looptype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>
	/// <param name="onstart">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onstarttarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onstartparams">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="onupdate">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onupdatetarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onupdateparams">
	/// A <see cref="System.Object"/>
	/// </param> 
	/// <param name="oncomplete">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="oncompletetarget">
	/// A <see cref="GameObject"/>.
	/// </param>
	/// <param name="oncompleteparams">
	/// A <see cref="System.Object"/>
	/// </param>
	public static void LookTo(GameObject target, Hashtable args){		
		//clean args:
		args = iTween.CleanArgs(args);			
		
		//additional property to ensure ConflictCheck can work correctly since Transforms are refrences:		
		if(args.Contains("looktarget")){
			if (args["looktarget"].GetType() == typeof(Transform)) {
				Transform transform = (Transform)args["looktarget"];
				args["position"]=new Vector3(transform.position.x,transform.position.y,transform.position.z);
				args["rotation"]=new Vector3(transform.eulerAngles.x,transform.eulerAngles.y,transform.eulerAngles.z);
			}
		}
		
		//establish iTween
		args["type"]="look";
		args["method"]="to";
		Launch(target,args);
	}		
	
	/// <summary>
	/// Changes a GameObject's position over time.
	/// </summary>
	/// <param name="position">
	/// A <see cref="Vector3"/>
	/// </param>
	/// <param name="x">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="y">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="z">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="orienttopath">
	/// A <see cref="System.Boolean"/>
	/// </param>
	/// <param name="looktarget">
	/// A <see cref="Vector3"/> or A <see cref="Transform"/>
	/// </param>
	/// <param name="islocal">
	/// A <see cref="System.Boolean"/>
	/// </param>
	/// <param name="time">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="delay">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="easetype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>   
	/// <param name="looptype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>
	/// <param name="onstart">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onstarttarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onstartparams">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="onupdate">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onupdatetarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onupdateparams">
	/// A <see cref="System.Object"/>
	/// </param> 
	/// <param name="oncomplete">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="oncompletetarget">
	/// A <see cref="GameObject"/>.
	/// </param>
	/// <param name="oncompleteparams">
	/// A <see cref="System.Object"/>
	/// </param>
	public static void MoveTo(GameObject target, Hashtable args){
		//clean args:
		args = iTween.CleanArgs(args);
		
		//establish iTween:
		args["type"]="move";
		args["method"]="to";
		Launch(target,args);
	}
	
	/// <summary>
	/// Instantly changes a GameObject's position then returns it to it's starting position over time.
	/// </summary>
	/// <param name="position">
	/// A <see cref="Vector3"/>
	/// </param>
	/// <param name="x">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="y">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="z">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="orienttopath">
	/// A <see cref="System.Boolean"/>
	/// </param>
	/// <param name="looktarget">
	/// A <see cref="Vector3"/> or A <see cref="Transform"/>
	/// </param>
	/// <param name="islocal">
	/// A <see cref="System.Boolean"/>
	/// </param>
	/// <param name="time">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="delay">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="easetype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>   
	/// <param name="looptype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>
	/// <param name="onstart">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onstarttarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onstartparams">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="onupdate">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onupdatetarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onupdateparams">
	/// A <see cref="System.Object"/>
	/// </param> 
	/// <param name="oncomplete">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="oncompletetarget">
	/// A <see cref="GameObject"/>.
	/// </param>
	/// <param name="oncompleteparams">
	/// A <see cref="System.Object"/>
	/// </param>
	public static void MoveFrom(GameObject target, Hashtable args){
		Vector3 tempPosition;
		Vector3 fromPosition;
		bool tempIsLocal;
	
		//clean args:
		args = iTween.CleanArgs(args);
		
		//set tempIsLocal:
		if(args.Contains("islocal")){
			tempIsLocal = (bool)args["islocal"];
		}else{
			tempIsLocal = Defaults.isLocal;	
		}

		//set tempPosition and base fromPosition:
		if(tempIsLocal){
			tempPosition=fromPosition=target.transform.localPosition;
		}else{
			tempPosition=fromPosition=target.transform.position;	
		}
		
		//set augmented fromPosition:
		if(args.Contains("position")){
			fromPosition=(Vector3)args["position"];
		}else{
			if (args.Contains("x")) {
				fromPosition.x=(float)args["x"];
			}
			if (args.Contains("y")) {
				fromPosition.y=(float)args["y"];
			}
			if (args.Contains("z")) {
				fromPosition.z=(float)args["z"];
			}
		}
		
		//apply fromPosition:
		if(tempIsLocal){
			target.transform.localPosition = fromPosition;
		}else{
			target.transform.position = fromPosition;	
		}
		
		//set new position arg:
		args["position"]=tempPosition;
		
		//establish iTween:
		args["type"]="move";
		args["method"]="to";
		Launch(target,args);
	}
	
	/// <summary>
	/// Translates a GameObject's position over time.
	/// </summary>
	/// <param name="amount">
	/// A <see cref="Vector3"/>
	/// </param>
	/// <param name="x">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="y">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="z">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="orienttopath">
	/// A <see cref="System.Boolean"/>
	/// </param>
	/// <param name="looktarget">
	/// A <see cref="Vector3"/> or A <see cref="Transform"/>
	/// </param>
	/// <param name="space">
	/// A <see cref="Space"/> or <see cref="System.String"/>
	/// </param>
	/// <param name="time">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="delay">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="easetype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>   
	/// <param name="looptype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>
	/// <param name="onstart">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onstarttarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onstartparams">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="onupdate">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onupdatetarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onupdateparams">
	/// A <see cref="System.Object"/>
	/// </param> 
	/// <param name="oncomplete">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="oncompletetarget">
	/// A <see cref="GameObject"/>.
	/// </param>
	/// <param name="oncompleteparams">
	/// A <see cref="System.Object"/>
	/// </param>
	public static void MoveAdd(GameObject target, Hashtable args){
		//clean args:
		args = iTween.CleanArgs(args);
		
		//establish iTween:
		args["type"]="move";
		args["method"]="add";
		Launch(target,args);
	}
	
	/// <summary>
	/// Adds the supplied coordinates to a GameObject's postion.
	/// </summary>
	/// <param name="amount">
	/// A <see cref="Vector3"/>
	/// </param>
	/// <param name="x">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="y">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="z">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="orienttopath">
	/// A <see cref="System.Boolean"/>
	/// </param>
	/// <param name="looktarget">
	/// A <see cref="Vector3"/> or A <see cref="Transform"/>
	/// </param>
	/// <param name="space">
	/// A <see cref="Space"/> or <see cref="System.String"/>
	/// </param>
	/// <param name="time">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="delay">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="easetype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>   
	/// <param name="looptype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>
	/// <param name="onstart">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onstarttarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onstartparams">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="onupdate">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onupdatetarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onupdateparams">
	/// A <see cref="System.Object"/>
	/// </param> 
	/// <param name="oncomplete">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="oncompletetarget">
	/// A <see cref="GameObject"/>.
	/// </param>
	/// <param name="oncompleteparams">
	/// A <see cref="System.Object"/>
	/// </param>
	public static void MoveBy(GameObject target, Hashtable args){
		//clean args:
		args = iTween.CleanArgs(args);
		
		//establish iTween:
		args["type"]="move";
		args["method"]="by";
		Launch(target,args);
	}
	
	/// <summary>
	/// Changes a GameObject's scale over time.
	/// </summary>
	/// <param name="scale">
	/// A <see cref="Vector3"/>
	/// </param>
	/// <param name="x">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="y">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="z">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="time">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="delay">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="easetype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>   
	/// <param name="looptype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>
	/// <param name="onstart">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onstarttarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onstartparams">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="onupdate">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onupdatetarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onupdateparams">
	/// A <see cref="System.Object"/>
	/// </param> 
	/// <param name="oncomplete">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="oncompletetarget">
	/// A <see cref="GameObject"/>.
	/// </param>
	/// <param name="oncompleteparams">
	/// A <see cref="System.Object"/>
	/// </param>
	public static void ScaleTo(GameObject target, Hashtable args){
		//clean args:
		args = iTween.CleanArgs(args);
		
		//establish iTween:
		args["type"]="scale";
		args["method"]="to";
		Launch(target,args);
	}
	
	/// <summary>
	/// Instantly changes a GameObject's scale then returns it to it's starting scale over time.
	/// </summary>
	/// <param name="scale">
	/// A <see cref="Vector3"/>
	/// </param>
	/// <param name="x">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="y">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="z">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="time">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="delay">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="easetype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>   
	/// <param name="looptype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>
	/// <param name="onstart">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onstarttarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onstartparams">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="onupdate">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onupdatetarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onupdateparams">
	/// A <see cref="System.Object"/>
	/// </param> 
	/// <param name="oncomplete">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="oncompletetarget">
	/// A <see cref="GameObject"/>.
	/// </param>
	/// <param name="oncompleteparams">
	/// A <see cref="System.Object"/>
	/// </param>
	public static void ScaleFrom(GameObject target, Hashtable args){
		Vector3 tempScale;
		Vector3 fromScale;
	
		//clean args:
		args = iTween.CleanArgs(args);
		
		//set base fromScale:
		tempScale=fromScale=target.transform.localScale;
		
		//set augmented fromScale:
		if(args.Contains("scale")){
			fromScale=(Vector3)args["scale"];
		}else{
			if (args.Contains("x")) {
				fromScale.x=(float)args["x"];
			}
			if (args.Contains("y")) {
				fromScale.y=(float)args["y"];
			}
			if (args.Contains("z")) {
				fromScale.z=(float)args["z"];
			}
		}
		
		//apply fromScale:
		target.transform.localScale = fromScale;	
		
		//set new scale arg:
		args["scale"]=tempScale;
		
		//establish iTween:
		args["type"]="scale";
		args["method"]="to";
		Launch(target,args);
	}
	
	/// <summary>
	/// Adds to a GameObject's scale over time.
	/// </summary>
	/// <param name="amount">
	/// A <see cref="Vector3"/>
	/// </param>
	/// <param name="x">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="y">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="z">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="time">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="delay">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="easetype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>   
	/// <param name="looptype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>
	/// <param name="onstart">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onstarttarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onstartparams">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="onupdate">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onupdatetarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onupdateparams">
	/// A <see cref="System.Object"/>
	/// </param> 
	/// <param name="oncomplete">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="oncompletetarget">
	/// A <see cref="GameObject"/>.
	/// </param>
	/// <param name="oncompleteparams">
	/// A <see cref="System.Object"/>
	/// </param>
	public static void ScaleAdd(GameObject target, Hashtable args){
		//clean args:
		args = iTween.CleanArgs(args);
		
		//establish iTween:
		args["type"]="scale";
		args["method"]="add";
		Launch(target,args);
	}
	
	/// <summary>
	/// Multiplies a GameObject's scale over time.
	/// </summary>
	/// <param name="amount">
	/// A <see cref="Vector3"/>
	/// </param>
	/// <param name="x">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="y">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="z">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="time">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="delay">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="easetype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>   
	/// <param name="looptype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>
	/// <param name="onstart">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onstarttarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onstartparams">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="onupdate">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onupdatetarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onupdateparams">
	/// A <see cref="System.Object"/>
	/// </param> 
	/// <param name="oncomplete">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="oncompletetarget">
	/// A <see cref="GameObject"/>.
	/// </param>
	/// <param name="oncompleteparams">
	/// A <see cref="System.Object"/>
	/// </param>
	public static void ScaleBy(GameObject target, Hashtable args){
		//clean args:
		args = iTween.CleanArgs(args);
		
		//establish iTween:
		args["type"]="scale";
		args["method"]="by";
		Launch(target,args);
	}
	
	/// <summary>
	/// Rotates a GameObject to the supplied angles in euler angles over time (if allowed). 
	/// </summary>
	/// <param name="rotation">
	/// A <see cref="Vector3"/>
	/// </param>
	/// <param name="x">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="y">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="z">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="islocal">
	/// A <see cref="System.Boolean"/>
	/// </param>
	/// <param name="time">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="delay">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="easetype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>   
	/// <param name="looptype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>
	/// <param name="onstart">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onstarttarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onstartparams">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="onupdate">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onupdatetarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onupdateparams">
	/// A <see cref="System.Object"/>
	/// </param> 
	/// <param name="oncomplete">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="oncompletetarget">
	/// A <see cref="GameObject"/>.
	/// </param>
	/// <param name="oncompleteparams">
	/// A <see cref="System.Object"/>
	/// </param>
	public static void RotateTo(GameObject target, Hashtable args){
		//clean args:
		args = iTween.CleanArgs(args);
		
		//establish iTween
		args["type"]="rotate";
		args["method"]="to";
		Launch(target,args);
	}	
	
	/// <summary>
	/// Instantly changes a GameObject's rotation in euler angles then returns it to it's starting rotation over time (if allowed).
	/// </summary>
	/// <param name="rotation">
	/// A <see cref="Vector3"/>
	/// </param>
	/// <param name="x">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="y">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="z">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="islocal">
	/// A <see cref="System.Boolean"/>
	/// </param>
	/// <param name="time">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="delay">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="easetype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>   
	/// <param name="looptype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>
	/// <param name="onstart">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onstarttarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onstartparams">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="onupdate">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onupdatetarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onupdateparams">
	/// A <see cref="System.Object"/>
	/// </param> 
	/// <param name="oncomplete">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="oncompletetarget">
	/// A <see cref="GameObject"/>.
	/// </param>
	/// <param name="oncompleteparams">
	/// A <see cref="System.Object"/>
	/// </param>
	public static void RotateFrom(GameObject target, Hashtable args){
		Vector3 tempRotation;
		Vector3 fromRotation;
		bool tempIsLocal;
	
		//clean args:
		args = iTween.CleanArgs(args);
		
		//set tempIsLocal:
		if(args.Contains("islocal")){
			tempIsLocal = (bool)args["islocal"];
		}else{
			tempIsLocal = Defaults.isLocal;	
		}

		//set tempRotation and base fromRotation:
		if(tempIsLocal){
			tempRotation=fromRotation=target.transform.localEulerAngles;
		}else{
			tempRotation=fromRotation=target.transform.eulerAngles;	
		}
		
		//set augmented fromRotation:
		if(args.Contains("rotation")){
			fromRotation=(Vector3)args["rotation"];
		}else{
			if (args.Contains("x")) {
				fromRotation.x=(float)args["x"];
			}
			if (args.Contains("y")) {
				fromRotation.y=(float)args["y"];
			}
			if (args.Contains("z")) {
				fromRotation.z=(float)args["z"];
			}
		}
		
		//apply fromRotation:
		if(tempIsLocal){
			target.transform.localEulerAngles = fromRotation;
		}else{
			target.transform.eulerAngles = fromRotation;	
		}
		
		//set new rotation arg:
		args["rotation"]=tempRotation;
		
		//establish iTween:
		args["type"]="rotate";
		args["method"]="to";
		Launch(target,args);
	}	
	
	/// <summary>
	/// Adds supplied euler angles to a GameObject's rotation over time (if allowed).
	/// </summary>
	/// <param name="amount">
	/// A <see cref="Vector3"/>
	/// </param>
	/// <param name="x">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="y">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="z">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="space">
	/// A <see cref="Space"/> or <see cref="System.String"/>
	/// </param>
	/// <param name="time">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="delay">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="easetype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>   
	/// <param name="looptype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>
	/// <param name="onstart">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onstarttarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onstartparams">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="onupdate">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onupdatetarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onupdateparams">
	/// A <see cref="System.Object"/>
	/// </param> 
	/// <param name="oncomplete">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="oncompletetarget">
	/// A <see cref="GameObject"/>.
	/// </param>
	/// <param name="oncompleteparams">
	/// A <see cr	
	public static void RotateAdd(GameObject target, Hashtable args){
		//clean args:
		args = iTween.CleanArgs(args);
		
		//establish iTween:
		args["type"]="rotate";
		args["method"]="add";
		Launch(target,args);
	}
	
	/// <summary>
	/// Multiplies supplied values by 360 and rotates a GameObject by calculated amount over time (if allowed). 
	/// </summary>
	/// <param name="rotation">
	/// A <see cref="Vector3"/>
	/// </param>
	/// <param name="x">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="y">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="z">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="space">
	/// A <see cref="Space"/>
	/// </param>
	/// <param name="time">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="delay">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="easetype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>   
	/// <param name="looptype">
	/// A <see cref="EaseType"/> or <see cref="System.String"/>
	/// </param>
	/// <param name="onstart">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onstarttarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onstartparams">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="onupdate">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onupdatetarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onupdateparams">
	/// A <see cref="System.Object"/>
	/// </param> 
	/// <param name="oncomplete">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="oncompletetarget">
	/// A <see cref="GameObject"/>.
	/// </param>
	/// <param name="oncompleteparams">
	/// A <see cref="System.Object"/>
	/// </param>
	public static void RotateBy(GameObject target, Hashtable args){
		//clean args:
		args = iTween.CleanArgs(args);
		
		//establish iTween
		args["type"]="rotate";
		args["method"]="by";
		Launch(target,args);
	}		
	
	/// <summary>
	/// Randomly shakes a GameObject's position by a diminishing amount over time.
	/// </summary>
	/// <param name="amount">
	/// A <see cref="Vector3"/>
	/// </param>
	/// <param name="x">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="y">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="z">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="space">
	/// A <see cref="Space"/>
	/// </param> 
	/// <param name="looktarget">
	/// A <see cref="Vector3"/> or A <see cref="Transform"/>
	/// </param>
	/// <param name="time">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="delay">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="onstart">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onstarttarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onstartparams">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="onupdate">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onupdatetarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onupdateparams">
	/// A <see cref="System.Object"/>
	/// </param> 
	/// <param name="oncomplete">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="oncompletetarget">
	/// A <see cref="GameObject"/>.
	/// </param>
	/// <param name="oncompleteparams">
	/// A <see cref="System.Object"/>
	/// </param>
	public static void ShakePosition(GameObject target, Hashtable args){
		//clean args:
		args = iTween.CleanArgs(args);
		
		//establish iTween
		args["type"]="shake";
		args["method"]="position";
		Launch(target,args);
	}		
	
	/// <summary>
	/// Randomly shakes a GameObject's scale by a diminishing amount over time.
	/// </summary>
	/// <param name="amount">
	/// A <see cref="Vector3"/>
	/// </param>
	/// <param name="x">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="y">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="z">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="time">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="delay">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="onstart">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onstarttarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onstartparams">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="onupdate">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onupdatetarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onupdateparams">
	/// A <see cref="System.Object"/>
	/// </param> 
	/// <param name="oncomplete">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="oncompletetarget">
	/// A <see cref="GameObject"/>.
	/// </param>
	/// <param name="oncompleteparams">
	/// A <see cref="System.Object"/>
	/// </param>
	public static void ShakeScale(GameObject target, Hashtable args){
		//clean args:
		args = iTween.CleanArgs(args);
		
		//establish iTween
		args["type"]="shake";
		args["method"]="scale";
		Launch(target,args);
	}		
	
	/// <summary>
	/// Randomly shakes a GameObject's rotation by a diminishing amount over time.
	/// </summary>
	/// <param name="amount">
	/// A <see cref="Vector3"/>
	/// </param>
	/// <param name="x">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="y">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="z">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="space">
	/// A <see cref="Space"/>
	/// </param>
	/// <param name="time">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="delay">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="onstart">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onstarttarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onstartparams">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="onupdate">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onupdatetarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onupdateparams">
	/// A <see cref="System.Object"/>
	/// </param> 
	/// <param name="oncomplete">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="oncompletetarget">
	/// A <see cref="GameObject"/>.
	/// </param>
	/// <param name="oncompleteparams">
	/// A <see cref="System.Object"/>
	/// </param>
	public static void ShakeRotation(GameObject target, Hashtable args){
		//clean args:
		args = iTween.CleanArgs(args);
		
		//establish iTween
		args["type"]="shake";
		args["method"]="rotation";
		Launch(target,args);
	}			
	
	/// <summary>
	/// Applies a jolt of force to a GameObject's position and wobbles it back to its initial position.
	/// </summary>
	/// <param name="amount">
	/// A <see cref="Vector3"/>
	/// </param>
	/// <param name="x">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="y">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="z">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="space">
	/// A <see cref="Space"/>
	/// </param> 
	/// <param name="looktarget">
	/// A <see cref="Vector3"/> or A <see cref="Transform"/>
	/// </param>
	/// <param name="time">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="delay">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="onstart">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onstarttarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onstartparams">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="onupdate">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onupdatetarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onupdateparams">
	/// A <see cref="System.Object"/>
	/// </param> 
	/// <param name="oncomplete">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="oncompletetarget">
	/// A <see cref="GameObject"/>.
	/// </param>
	/// <param name="oncompleteparams">
	/// A <see cref="System.Object"/>
	/// </param>
	public static void PunchPosition(GameObject target, Hashtable args){
		//clean args:
		args = iTween.CleanArgs(args);
		
		//establish iTween
		args["type"]="punch";
		args["method"]="position";
		args["easetype"]=EaseType.punch;
		Launch(target,args);
	}		
	
	/// <summary>
	/// Applies a jolt of force to a GameObject's rotation and wobbles it back to its initial rotation.
	/// </summary>
	/// <param name="amount">
	/// A <see cref="Vector3"/>
	/// </param>
	/// <param name="x">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="y">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="z">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="space">
	/// A <see cref="Space"/>
	/// </param> 
	/// <param name="time">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="delay">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="onstart">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onstarttarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onstartparams">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="onupdate">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onupdatetarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onupdateparams">
	/// A <see cref="System.Object"/>
	/// </param> 
	/// <param name="oncomplete">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="oncompletetarget">
	/// A <see cref="GameObject"/>.
	/// </param>
	/// <param name="oncompleteparams">
	/// A <see cref="System.Object"/>
	/// </param>
	public static void PunchRotation(GameObject target, Hashtable args){
		//clean args:
		args = iTween.CleanArgs(args);
		
		//establish iTween
		args["type"]="punch";
		args["method"]="rotation";
		args["easetype"]=EaseType.punch;
		Launch(target,args);
	}	
	
	/// <summary>
	/// Applies a jolt of force to a GameObject's scale and wobbles it back to its initial scale.
	/// </summary>
	/// <param name="amount">
	/// A <see cref="Vector3"/>
	/// </param>
	/// <param name="x">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="y">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="z">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="space">
	/// A <see cref="Space"/>
	/// </param> 
	/// <param name="time">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="delay">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="onstart">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onstarttarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onstartparams">
	/// A <see cref="System.Object"/>
	/// </param>
	/// <param name="onupdate">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="onupdatetarget">
	/// A <see cref="GameObject"/>
	/// </param>
	/// <param name="onupdateparams">
	/// A <see cref="System.Object"/>
	/// </param> 
	/// <param name="oncomplete">
	/// A <see cref="System.String"/>
	/// </param>
	/// <param name="oncompletetarget">
	/// A <see cref="GameObject"/>.
	/// </param>
	/// <param name="oncompleteparams">
	/// A <see cref="System.Object"/>
	/// </param>
	public static void PunchScale(GameObject target, Hashtable args){
		//clean args:
		args = iTween.CleanArgs(args);
		
		//establish iTween
		args["type"]="punch";
		args["method"]="scale";
		args["easetype"]=EaseType.punch;
		Launch(target,args);
	}	
	
	#endregion
	
	#region #2 Generate Method Targets
	
	//call correct set target method and set tween application delegate:
	void GenerateTargets(){
		switch (type) {
			case "value":
				switch (method) {
					case "float":
						GenerateFloatTargets();
						apply = new ApplyTween(ApplyFloatTargets);
					break;
				case "vector2":
						GenerateVector2Targets();
						apply = new ApplyTween(ApplyVector2Targets);
					break;
				case "vector3":
						GenerateVector3Targets();
						apply = new ApplyTween(ApplyVector3Targets);
					break;
				case "color":
						GenerateColorTargets();
						apply = new ApplyTween(ApplyColorTargets);
					break;
				case "rect":
						GenerateRectTargets();
						apply = new ApplyTween(ApplyRectTargets);
					break;
				}
			break;
			case "color":
				switch (method) {
					case "to":
						GenerateColorToTargets();
						apply = new ApplyTween(ApplyColorToTargets);
					break;
				}
			break;
			case "audio":
				switch (method) {
					case "to":
						GenerateAudioToTargets();
						apply = new ApplyTween(ApplyAudioToTargets);
					break;
				}
			break;
			case "move":
				switch (method) {
					case "to":
						GenerateMoveToTargets();
						apply = new ApplyTween(ApplyMoveToTargets);
					break;
					case "by":
					case "add":
						GenerateMoveByTargets();
						apply = new ApplyTween(ApplyMoveByTargets);
					break;
				}
			break;
			case "scale":
				switch (method){
					case "to":
						GenerateScaleToTargets();
						apply = new ApplyTween(ApplyScaleToTargets);
					break;
					case "by":
						GenerateScaleByTargets();
						apply = new ApplyTween(ApplyScaleToTargets);
					break;
					case "add":
						GenerateScaleAddTargets();
						apply = new ApplyTween(ApplyScaleToTargets);
					break;
				}
			break;
			case "rotate":
				switch (method) {
					case "to":
						GenerateRotateToTargets();
						apply = new ApplyTween(ApplyRotateToTargets);
					break;
					case "add":
						GenerateRotateAddTargets();
						apply = new ApplyTween(ApplyRotateAddTargets);
					break;
					case "by":
						GenerateRotateByTargets();
						apply = new ApplyTween(ApplyRotateAddTargets);
					break;				
				}
			break;
			case "shake":
				switch (method) {
					case "position":
						GenerateShakePositionTargets();
						apply = new ApplyTween(ApplyShakePositionTargets);
					break;		
					case "scale":
						GenerateShakeScaleTargets();
						apply = new ApplyTween(ApplyShakeScaleTargets);
					break;
					case "rotation":
						GenerateShakeRotationTargets();
						apply = new ApplyTween(ApplyShakeRotationTargets);
					break;
				}
			break;			
			case "punch":
				switch (method) {
					case "position":
						GeneratePunchPositionTargets();
						apply = new ApplyTween(ApplyPunchPositionTargets);
					break;	
					case "rotation":
						GeneratePunchRotationTargets();
						apply = new ApplyTween(ApplyPunchRotationTargets);
					break;	
					case "scale":
						GeneratePunchScaleTargets();
						apply = new ApplyTween(ApplyPunchScaleTargets);
					break;
				}
			break;
			case "look":
				switch (method) {
					case "to":
						GenerateLookToTargets();
						apply = new ApplyTween(ApplyLookToTargets);
					break;	
				}
			break;	
			case "stab":
				GenerateStabTargets();
				apply = new ApplyTween(ApplyStabTargets);
			break;	
		}
	}
	
	#endregion
	
	#region #3 Generate Specific Targets
	
	void GenerateRectTargets(){
		//values holder [0] from, [1] to, [2] calculated value from ease equation:
		rects=new Rect[3];
		
		//from and to values:
		rects[0]=(Rect)tweenArguments["from"];
		rects[1]=(Rect)tweenArguments["to"];
	}		
	
	void GenerateColorTargets(){
		//values holder [0] from, [1] to, [2] calculated value from ease equation:
		colors=new Color[3];
		
		//from and to values:
		colors[0]=(Color)tweenArguments["from"];
		colors[1]=(Color)tweenArguments["to"];
	}	
	
	void GenerateVector3Targets(){
		//values holder [0] from, [1] to, [2] calculated value from ease equation:
		vector3s=new Vector3[3];
		
		//from and to values:
		vector3s[0]=(Vector3)tweenArguments["from"];
		vector3s[1]=(Vector3)tweenArguments["to"];
	}
	
	void GenerateVector2Targets(){
		//values holder [0] from, [1] to, [2] calculated value from ease equation:
		vector2s=new Vector2[3];
		
		//from and to values:
		vector2s[0]=(Vector2)tweenArguments["from"];
		vector2s[1]=(Vector2)tweenArguments["to"];
	}
	
	void GenerateFloatTargets(){
		//values holder [0] from, [1] to, [2] calculated value from ease equation:
		floats=new float[3];
		
		//from and to values:
		floats[0]=(float)tweenArguments["from"];
		floats[1]=(float)tweenArguments["to"];
	}
		
	void GenerateColorToTargets(){
		//values holder [0] from, [1] to, [2] calculated value from ease equation:
		colors = new Color[3];
		
		//from and init to values:
		if(GetComponent(typeof(GUITexture))){
			colors[0] = colors[1] = guiTexture.color;
		}else if(GetComponent(typeof(GUIText))){
			colors[0] = colors[1] = guiText.material.color;
		}else if(renderer){
			colors[0] = colors[1] = renderer.material.color;	
		}else if(light){
			colors[0] = colors[1] = light.color;	
		}
		
		//to values:
		if (tweenArguments.Contains("color")) {
			colors[1]=(Color)tweenArguments["color"];
		}else{
			if (tweenArguments.Contains("r")) {
				colors[1].r=(float)tweenArguments["r"];
			}
			if (tweenArguments.Contains("g")) {
				colors[1].g=(float)tweenArguments["g"];
			}
			if (tweenArguments.Contains("b")) {
				colors[1].b=(float)tweenArguments["b"];
			}
			if (tweenArguments.Contains("a")) {
				colors[1].a=(float)tweenArguments["a"];
			}
		}
	}
	
	void GenerateAudioToTargets(){
		//values holder [0] from, [1] to, [2] calculated value from ease equation:
		vector2s=new Vector2[3];
		
		//set audioSource:
		if(tweenArguments.Contains("audiosource")){
			audioSource=(AudioSource)tweenArguments["audiosource"];
		}else{
			if(GetComponent(typeof(AudioSource))){
				audioSource=audio;
			}else{
				//throw error if no AudioSource is available:
				Debug.LogError("iTween Error: AudioTo requires an AudioSource.");
				Dispose();
			}
		}		
		
		//from values and default to values:
		vector2s[0]=vector2s[1]=new Vector2(audioSource.volume,audioSource.pitch);
				
		//to values:
		if (tweenArguments.Contains("volume")) {
			vector2s[1].x=(float)tweenArguments["volume"];	
		}
		if (tweenArguments.Contains("pitch")) {
			vector2s[1].y=(float)tweenArguments["pitch"];	
		}
	}
	
	void GenerateStabTargets(){
		//set audioSource:
		if(tweenArguments.Contains("audiosource")){
			audioSource=(AudioSource)tweenArguments["audiosource"];
		}else{
			if(GetComponent(typeof(AudioSource))){
				audioSource=audio;
			}else{
				//add and populate AudioSource if one doesn't exist:
				gameObject.AddComponent(typeof(AudioSource));
				audioSource=audio;
				audioSource.playOnAwake=false;
				
			}
		}
		
		//populate audioSource's clip:
		audioSource.clip=(AudioClip)tweenArguments["audioclip"];
		
		//set audio's pitch and volume if requested:
		if(tweenArguments.Contains("pitch")){
			audioSource.pitch=(float)tweenArguments["pitch"];
		}
		if(tweenArguments.Contains("volume")){
			audioSource.volume=(float)tweenArguments["volume"];
		}
			
		//set run time based on length of clip after pitch is augmented
		time=audioSource.clip.length/audioSource.pitch;
	}
	
	void GenerateLookToTargets(){
		//values holder [0] from, [1] to, [2] calculated value from ease equation:
		vector3s=new Vector3[3];
		
		//from values:
		vector3s[0]=transform.eulerAngles;
		
		//set look:
		if(tweenArguments.Contains("looktarget")){
			if (tweenArguments["looktarget"].GetType() == typeof(Transform)) {
				transform.LookAt((Transform)tweenArguments["looktarget"]);
			}else if(tweenArguments["looktarget"].GetType() == typeof(Vector3)){
				transform.LookAt((Vector3)tweenArguments["looktarget"]);
			}
		}else{
			Debug.LogError("iTween Error: LookTo needs a 'looktarget' property!");
			Dispose();
		}

		//to values:
		vector3s[1]=transform.eulerAngles;
		transform.eulerAngles=vector3s[0];
		
		//axis restriction:
		if(tweenArguments.Contains("axis")){
			switch((string)tweenArguments["axis"]){
				case "x":
					vector3s[1].y=vector3s[0].y;
					vector3s[1].z=vector3s[0].z;
				break;
				case "y":
					vector3s[1].x=vector3s[0].x;
					vector3s[1].z=vector3s[0].z;
				break;
				case "z":
					vector3s[1].x=vector3s[0].x;
					vector3s[1].y=vector3s[0].y;
				break;
			}
		}
		
		//shortest distance:
		vector3s[1]=new Vector3(clerp(vector3s[0].x,vector3s[1].x,1),clerp(vector3s[0].y,vector3s[1].y,1),clerp(vector3s[0].z,vector3s[1].z,1));
	}		
	
	void GenerateMoveToTargets(){
		//values holder [0] from, [1] to, [2] calculated value from ease equation:
		vector3s=new Vector3[3];
		
		//from values:
		if (isLocal) {
			vector3s[0]=vector3s[1]=transform.localPosition;				
		}else{
			vector3s[0]=vector3s[1]=transform.position;
		}
		
		//to values:
		if (tweenArguments.Contains("position")) {
			vector3s[1]=(Vector3)tweenArguments["position"];
		}else{
			if (tweenArguments.Contains("x")) {
				vector3s[1].x=(float)tweenArguments["x"];
			}
			if (tweenArguments.Contains("y")) {
				vector3s[1].y=(float)tweenArguments["y"];
			}
			if (tweenArguments.Contains("z")) {
				vector3s[1].z=(float)tweenArguments["z"];
			}
		}
		
		
		//handle orient to path request:
		if(tweenArguments.Contains("orienttopath") && (bool)tweenArguments["orienttopath"]){
			tweenArguments["looktarget"] = vector3s[1];
		}
	}
	
	void GenerateMoveByTargets(){
		//values holder [0] from, [1] to, [2] calculated value from ease equation, [3] previous value for Translate usage to allow Space utilization, [4] original rotation to make sure look requests don't interfere with the direction object should move in:
		vector3s=new Vector3[5];
		
		//grab starting rotation:
		vector3s[4] = transform.eulerAngles;
		
		//from values:
		vector3s[0]=vector3s[1]=vector3s[3]=transform.position;
				
		//to values:
		if (tweenArguments.Contains("amount")) {
			vector3s[1]=vector3s[0] + (Vector3)tweenArguments["amount"];
		}else{
			if (tweenArguments.Contains("x")) {
				vector3s[1].x=vector3s[0].x + (float)tweenArguments["x"];
			}
			if (tweenArguments.Contains("y")) {
				vector3s[1].y=vector3s[0].y +(float)tweenArguments["y"];
			}
			if (tweenArguments.Contains("z")) {
				vector3s[1].z=vector3s[0].z + (float)tweenArguments["z"];
			}
		}	
		
		//handle orient to path request:
		if(tweenArguments.Contains("orienttopath") && (bool)tweenArguments["orienttopath"]){
			tweenArguments["looktarget"] = vector3s[1];
		}
	}
	
	void GenerateScaleToTargets(){
		//values holder [0] from, [1] to, [2] calculated value from ease equation:
		vector3s=new Vector3[3];
		
		//from values:
		vector3s[0]=vector3s[1]=transform.localScale;				

		//to values:
		if (tweenArguments.Contains("scale")) {
			vector3s[1]=(Vector3)tweenArguments["scale"];
		}else{
			if (tweenArguments.Contains("x")) {
				vector3s[1].x=(float)tweenArguments["x"];
			}
			if (tweenArguments.Contains("y")) {
				vector3s[1].y=(float)tweenArguments["y"];
			}
			if (tweenArguments.Contains("z")) {
				vector3s[1].z=(float)tweenArguments["z"];
			}
		} 			
	}
	
	void GenerateScaleByTargets(){
		//values holder [0] from, [1] to, [2] calculated value from ease equation:
		vector3s=new Vector3[3];
		
		//from values:
		vector3s[0]=vector3s[1]=transform.localScale;				

		//to values:
		if (tweenArguments.Contains("amount")) {
			vector3s[1]=Vector3.Scale(vector3s[1],(Vector3)tweenArguments["amount"]);
		}else{
			if (tweenArguments.Contains("x")) {
				vector3s[1].x*=(float)tweenArguments["x"];
			}
			if (tweenArguments.Contains("y")) {
				vector3s[1].y*=(float)tweenArguments["y"];
			}
			if (tweenArguments.Contains("z")) {
				vector3s[1].z*=(float)tweenArguments["z"];
			}
		} 			
	}
	
	void GenerateScaleAddTargets(){
		//values holder [0] from, [1] to, [2] calculated value from ease equation:
		vector3s=new Vector3[3];
		
		//from values:
		vector3s[0]=vector3s[1]=transform.localScale;				

		//to values:
		if (tweenArguments.Contains("amount")) {
			vector3s[1]+=(Vector3)tweenArguments["amount"];
		}else{
			if (tweenArguments.Contains("x")) {
				vector3s[1].x+=(float)tweenArguments["x"];
			}
			if (tweenArguments.Contains("y")) {
				vector3s[1].y+=(float)tweenArguments["y"];
			}
			if (tweenArguments.Contains("z")) {
				vector3s[1].z+=(float)tweenArguments["z"];
			}
		} 			
	}
	
	void GenerateRotateToTargets(){
		//values holder [0] from, [1] to, [2] calculated value from ease equation:
		vector3s=new Vector3[3];
		
		//from values:
		if (isLocal) {
			vector3s[0]=vector3s[1]=transform.localEulerAngles;				
		}else{
			vector3s[0]=vector3s[1]=transform.eulerAngles;
		}
		
		//to values:
		if (tweenArguments.Contains("rotation")) {
			vector3s[1]=(Vector3)tweenArguments["rotation"];
		}else{
			if (tweenArguments.Contains("x")) {
				vector3s[1].x=(float)tweenArguments["x"];
			}
			if (tweenArguments.Contains("y")) {
				vector3s[1].y=(float)tweenArguments["y"];
			}
			if (tweenArguments.Contains("z")) {
				vector3s[1].z=(float)tweenArguments["z"];
			}
		}
		
		//shortest distance:
		vector3s[1]=new Vector3(clerp(vector3s[0].x,vector3s[1].x,1),clerp(vector3s[0].y,vector3s[1].y,1),clerp(vector3s[0].z,vector3s[1].z,1));
	}
	
	void GenerateRotateAddTargets(){
		//values holder [0] from, [1] to, [2] calculated value from ease equation, [3] previous value for Rotate usage to allow Space utilization:
		vector3s=new Vector3[4];
		
		//from values:
		vector3s[0]=vector3s[1]=vector3s[3]=transform.eulerAngles;
		
		//to values:
		if (tweenArguments.Contains("amount")) {
			vector3s[1]=(Vector3)tweenArguments["amount"];
		}else{
			if (tweenArguments.Contains("x")) {
				vector3s[1].x+=(float)tweenArguments["x"];
			}
			if (tweenArguments.Contains("y")) {
				vector3s[1].y+=(float)tweenArguments["y"];
			}
			if (tweenArguments.Contains("z")) {
				vector3s[1].z+=(float)tweenArguments["z"];
			}
		}
	}		
	
	void GenerateRotateByTargets(){
		//values holder [0] from, [1] to, [2] calculated value from ease equation, [3] previous value for Rotate usage to allow Space utilization:
		vector3s=new Vector3[4];
		
		//from values:
		vector3s[0]=vector3s[1]=vector3s[3]=transform.eulerAngles;
		
		//to values:
		if (tweenArguments.Contains("amount")) {
			vector3s[1]+=Vector3.Scale((Vector3)tweenArguments["amount"],new Vector3(360,360,360));
		}else{
			if (tweenArguments.Contains("x")) {
				vector3s[1].x+=360 * (float)tweenArguments["x"];
			}
			if (tweenArguments.Contains("y")) {
				vector3s[1].y+=360 * (float)tweenArguments["y"];
			}
			if (tweenArguments.Contains("z")) {
				vector3s[1].z+=360 * (float)tweenArguments["z"];
			}
		}
	}		
	
	void GenerateShakePositionTargets(){
		//values holder [0] from, [1] to, [2] calculated value from ease equation, [3] original rotation to make sure look requests don't interfere with the direction object should move in:
		vector3s=new Vector3[4];
		
		//grab starting rotation:
		vector3s[3] = transform.eulerAngles;		
		
		//root:
		vector3s[0]=transform.position;
		
		//amount:
		if (tweenArguments.Contains("amount")) {
			vector3s[1]=(Vector3)tweenArguments["amount"];
		}else{
			if (tweenArguments.Contains("x")) {
				vector3s[1].x=(float)tweenArguments["x"];
			}
			if (tweenArguments.Contains("y")) {
				vector3s[1].y=(float)tweenArguments["y"];
			}
			if (tweenArguments.Contains("z")) {
				vector3s[1].z=(float)tweenArguments["z"];
			}
		}
	}		
	
	void GenerateShakeScaleTargets(){
		//values holder [0] root value, [1] amount, [2] generated amount:
		vector3s=new Vector3[3];
		
		//root:
		vector3s[0]=transform.localScale;
		
		//amount:
		if (tweenArguments.Contains("amount")) {
			vector3s[1]=(Vector3)tweenArguments["amount"];
		}else{
			if (tweenArguments.Contains("x")) {
				vector3s[1].x=(float)tweenArguments["x"];
			}
			if (tweenArguments.Contains("y")) {
				vector3s[1].y=(float)tweenArguments["y"];
			}
			if (tweenArguments.Contains("z")) {
				vector3s[1].z=(float)tweenArguments["z"];
			}
		}
	}		
		
	void GenerateShakeRotationTargets(){
		//values holder [0] root value, [1] amount, [2] generated amount:
		vector3s=new Vector3[3];
		
		//root:
		vector3s[0]=transform.eulerAngles;
		
		//amount:
		if (tweenArguments.Contains("amount")) {
			vector3s[1]=(Vector3)tweenArguments["amount"];
		}else{
			if (tweenArguments.Contains("x")) {
				vector3s[1].x=(float)tweenArguments["x"];
			}
			if (tweenArguments.Contains("y")) {
				vector3s[1].y=(float)tweenArguments["y"];
			}
			if (tweenArguments.Contains("z")) {
				vector3s[1].z=(float)tweenArguments["z"];
			}
		}
	}	
	
	void GeneratePunchPositionTargets(){
		//values holder [0] from, [1] to, [2] calculated value from ease equation, [3] previous value for Translate usage to allow Space utilization, [4] original rotation to make sure look requests don't interfere with the direction object should move in:
		vector3s=new Vector3[5];
		
		//grab starting rotation:
		vector3s[4] = transform.eulerAngles;
		
		//from values:
		vector3s[0]=transform.position;
		vector3s[1]=vector3s[3]=Vector3.zero;
				
		//to values:
		if (tweenArguments.Contains("amount")) {
			vector3s[1]=(Vector3)tweenArguments["amount"];
		}else{
			if (tweenArguments.Contains("x")) {
				vector3s[1].x=(float)tweenArguments["x"];
			}
			if (tweenArguments.Contains("y")) {
				vector3s[1].y=(float)tweenArguments["y"];
			}
			if (tweenArguments.Contains("z")) {
				vector3s[1].z=(float)tweenArguments["z"];
			}
		}
	}	
	
	void GeneratePunchRotationTargets(){
		//values holder [0] from, [1] to, [2] calculated value from ease equation, [3] previous value for Translate usage to allow Space utilization:
		vector3s=new Vector3[4];
		
		//from values:
		vector3s[0]=transform.eulerAngles;
		vector3s[1]=vector3s[3]=Vector3.zero;
				
		//to values:
		if (tweenArguments.Contains("amount")) {
			vector3s[1]=(Vector3)tweenArguments["amount"];
		}else{
			if (tweenArguments.Contains("x")) {
				vector3s[1].x=(float)tweenArguments["x"];
			}
			if (tweenArguments.Contains("y")) {
				vector3s[1].y=(float)tweenArguments["y"];
			}
			if (tweenArguments.Contains("z")) {
				vector3s[1].z=(float)tweenArguments["z"];
			}
		}
	}		
	
	void GeneratePunchScaleTargets(){
		//values holder [0] from, [1] to, [2] calculated value from ease equation:
		vector3s=new Vector3[3];
		
		//from values:
		vector3s[0]=transform.localScale;
		vector3s[1]=Vector3.zero;
				
		//to values:
		if (tweenArguments.Contains("amount")) {
			vector3s[1]=(Vector3)tweenArguments["amount"];
		}else{
			if (tweenArguments.Contains("x")) {
				vector3s[1].x=(float)tweenArguments["x"];
			}
			if (tweenArguments.Contains("y")) {
				vector3s[1].y=(float)tweenArguments["y"];
			}
			if (tweenArguments.Contains("z")) {
				vector3s[1].z=(float)tweenArguments["z"];
			}
		}
	}
	
	#endregion
	
	#region #4 Apply Targets
	
	void ApplyRectTargets(){
		//calculate:
		rects[2].x = ease(rects[0].x,rects[1].x,percentage);
		rects[2].y = ease(rects[0].y,rects[1].y,percentage);
		rects[2].width = ease(rects[0].width,rects[1].width,percentage);
		rects[2].height = ease(rects[0].height,rects[1].height,percentage);
		
		//apply:
		tweenArguments["onupdateparams"]=rects[2];
	}		
	
	void ApplyColorTargets(){
		//calculate:
		colors[2].r = ease(colors[0].r,colors[1].r,percentage);
		colors[2].g = ease(colors[0].g,colors[1].g,percentage);
		colors[2].b = ease(colors[0].b,colors[1].b,percentage);
		colors[2].a = ease(colors[0].a,colors[1].a,percentage);
		
		//apply:
		tweenArguments["onupdateparams"]=colors[2];
	}	
	
	void ApplyVector3Targets(){
		//calculate:
		vector3s[2].x = ease(vector3s[0].x,vector3s[1].x,percentage);
		vector3s[2].y = ease(vector3s[0].y,vector3s[1].y,percentage);
		vector3s[2].z = ease(vector3s[0].z,vector3s[1].z,percentage);
		
		//apply:
		tweenArguments["onupdateparams"]=vector3s[2];
	}		
	
	void ApplyVector2Targets(){
		//calculate:
		vector2s[2].x = ease(vector2s[0].x,vector2s[1].x,percentage);
		vector2s[2].y = ease(vector2s[0].y,vector2s[1].y,percentage);
		
		//apply:
		tweenArguments["onupdateparams"]=vector2s[2];
	}	
	
	void ApplyFloatTargets(){
		//calculate:
		floats[2] = ease(floats[0],floats[1],percentage);
		
		//apply:
		tweenArguments["onupdateparams"]=floats[2];
	}	
	
	void ApplyColorToTargets(){
		//calculate:
		colors[2].r = ease(colors[0].r,colors[1].r,percentage);
		colors[2].g = ease(colors[0].g,colors[1].g,percentage);
		colors[2].b = ease(colors[0].b,colors[1].b,percentage);
		colors[2].a = ease(colors[0].a,colors[1].a,percentage);
		
		//apply:
		if(GetComponent(typeof(GUITexture))){
			guiTexture.color=colors[2];
		}else if(GetComponent(typeof(GUIText))){
			guiText.material.color=colors[2];
		}else if(renderer){
			renderer.material.color=colors[2];	
		}else if(light){
			light.color=colors[2];	
		}
	}	
	
	void ApplyAudioToTargets(){
		//calculate:
		vector2s[2].x = ease(vector2s[0].x,vector2s[1].x,percentage);
		vector2s[2].y = ease(vector2s[0].y,vector2s[1].y,percentage);
		
		//apply:
		audioSource.volume=vector2s[2].x;
		audioSource.pitch=vector2s[2].y;
	}	
	
	void ApplyStabTargets(){
		//unnecessary but here just in case
	}
	
	void ApplyMoveToTargets(){
		//calculate:
		vector3s[2].x = ease(vector3s[0].x,vector3s[1].x,percentage);
		vector3s[2].y = ease(vector3s[0].y,vector3s[1].y,percentage);
		vector3s[2].z = ease(vector3s[0].z,vector3s[1].z,percentage);
		
		//apply:
		if (isLocal) {
			transform.localPosition=vector3s[2];		
		}else{
			transform.position=vector3s[2];
		}
	}	
	
	void ApplyMoveByTargets(){	
		//reset rotation to prevent look interferences as object rotates and attempts to move with translate and record current rotation
		Vector3 currentRotation = new Vector3();
		
		if(tweenArguments.Contains("looktarget")){
			currentRotation = transform.eulerAngles;
			transform.eulerAngles = vector3s[4];	
		}
		
		//calculate:
		vector3s[2].x = ease(vector3s[0].x,vector3s[1].x,percentage);
		vector3s[2].y = ease(vector3s[0].y,vector3s[1].y,percentage);
		vector3s[2].z = ease(vector3s[0].z,vector3s[1].z,percentage);
				
		//apply:
		transform.Translate(vector3s[2]-vector3s[3],space);
		
		//record:
		vector3s[3]=vector3s[2];
		
		//reset rotation:
		if(tweenArguments.Contains("looktarget")){
			transform.eulerAngles = currentRotation;	
		}
	}	
	
	void ApplyScaleToTargets(){
		//calculate:
		vector3s[2].x = ease(vector3s[0].x,vector3s[1].x,percentage);
		vector3s[2].y = ease(vector3s[0].y,vector3s[1].y,percentage);
		vector3s[2].z = ease(vector3s[0].z,vector3s[1].z,percentage);
		
		//apply:
		transform.localScale=vector3s[2];	
	}
	
	void ApplyLookToTargets(){
		//calculate:
		vector3s[2].x = ease(vector3s[0].x,vector3s[1].x,percentage);
		vector3s[2].y = ease(vector3s[0].y,vector3s[1].y,percentage);
		vector3s[2].z = ease(vector3s[0].z,vector3s[1].z,percentage);
		
		//apply:
		if (isLocal) {
			transform.localRotation = Quaternion.Euler(vector3s[2]);
		}else{
			transform.rotation = Quaternion.Euler(vector3s[2]);
		};	
	}	
	
	void ApplyRotateToTargets(){
		//calculate:
		vector3s[2].x = ease(vector3s[0].x,vector3s[1].x,percentage);
		vector3s[2].y = ease(vector3s[0].y,vector3s[1].y,percentage);
		vector3s[2].z = ease(vector3s[0].z,vector3s[1].z,percentage);
		
		//apply:
		if (isLocal) {
			transform.localRotation = Quaternion.Euler(vector3s[2]);
		}else{
			transform.rotation = Quaternion.Euler(vector3s[2]);
		};	
	}
	
	void ApplyRotateAddTargets(){
		//calculate:
		vector3s[2].x = ease(vector3s[0].x,vector3s[1].x,percentage);
		vector3s[2].y = ease(vector3s[0].y,vector3s[1].y,percentage);
		vector3s[2].z = ease(vector3s[0].z,vector3s[1].z,percentage);
		
		//apply:
		transform.Rotate(vector3s[2]-vector3s[3],space);

		//record:
		vector3s[3]=vector3s[2];
	}	
	
	void ApplyShakePositionTargets(){
		//reset rotation to prevent look interferences as object rotates and attempts to move with translate and record current rotation
		Vector3 currentRotation = new Vector3();
		
		if(tweenArguments.Contains("looktarget")){
			currentRotation = transform.eulerAngles;
			transform.eulerAngles = vector3s[3];	
		}
		
		//impact:
		if (percentage==0) {
			transform.Translate(vector3s[1],space);
		}
		
		//reset:
		transform.position=vector3s[0];
		
		//generate:
		float diminishingControl = 1-percentage;
		vector3s[2].x= UnityEngine.Random.Range(-vector3s[1].x*diminishingControl, vector3s[1].x*diminishingControl);
		vector3s[2].y= UnityEngine.Random.Range(-vector3s[1].y*diminishingControl, vector3s[1].y*diminishingControl);
		vector3s[2].z= UnityEngine.Random.Range(-vector3s[1].z*diminishingControl, vector3s[1].z*diminishingControl);

		//apply:
		transform.Translate(vector3s[2],space);	
		
		//reset rotation:
		if(tweenArguments.Contains("looktarget")){
			transform.eulerAngles = currentRotation;	
		}		
	}	
	
	void ApplyShakeScaleTargets(){
		//impact:
		if (percentage==0) {
			transform.localScale=vector3s[1];
		}
		
		//reset:
		transform.localScale=vector3s[0];
		
		//generate:
		float diminishingControl = 1-percentage;
		vector3s[2].x= UnityEngine.Random.Range(-vector3s[1].x*diminishingControl, vector3s[1].x*diminishingControl);
		vector3s[2].y= UnityEngine.Random.Range(-vector3s[1].y*diminishingControl, vector3s[1].y*diminishingControl);
		vector3s[2].z= UnityEngine.Random.Range(-vector3s[1].z*diminishingControl, vector3s[1].z*diminishingControl);

		//apply:
		transform.localScale+=vector3s[2];
	}		
	
	void ApplyShakeRotationTargets(){
		//impact:
		if (percentage==0) {
			transform.Rotate(vector3s[1],space);
		}
		
		//reset:
		transform.eulerAngles=vector3s[0];
		
		//generate:
		float diminishingControl = 1-percentage;
		vector3s[2].x= UnityEngine.Random.Range(-vector3s[1].x*diminishingControl, vector3s[1].x*diminishingControl);
		vector3s[2].y= UnityEngine.Random.Range(-vector3s[1].y*diminishingControl, vector3s[1].y*diminishingControl);
		vector3s[2].z= UnityEngine.Random.Range(-vector3s[1].z*diminishingControl, vector3s[1].z*diminishingControl);

		//apply:
		transform.Rotate(vector3s[2],space);	
	}		
	
	void ApplyPunchPositionTargets(){
		//reset rotation to prevent look interferences as object rotates and attempts to move with translate and record current rotation
		Vector3 currentRotation = new Vector3();
		
		if(tweenArguments.Contains("looktarget")){
			currentRotation = transform.eulerAngles;
			transform.eulerAngles = vector3s[4];	
		}
		
		//calculate:
		if(vector3s[1].x>0){
			vector3s[2].x = punch(vector3s[1].x,percentage);
		}else if(vector3s[1].x<0){
			vector3s[2].x=-punch(Mathf.Abs(vector3s[1].x),percentage); 
		}
		if(vector3s[1].y>0){
			vector3s[2].y=punch(vector3s[1].y,percentage);
		}else if(vector3s[1].y<0){
			vector3s[2].y=-punch(Mathf.Abs(vector3s[1].y),percentage); 
		}
		if(vector3s[1].z>0){
			vector3s[2].z=punch(vector3s[1].z,percentage);
		}else if(vector3s[1].z<0){
			vector3s[2].z=-punch(Mathf.Abs(vector3s[1].z),percentage); 
		}
		
		//apply:
		transform.Translate(vector3s[2]-vector3s[3],space);

		//record:
		vector3s[3]=vector3s[2];
		
		//reset rotation:
		if(tweenArguments.Contains("looktarget")){
			transform.eulerAngles = currentRotation;	
		}
	}		
	
	void ApplyPunchRotationTargets(){
		//calculate:
		if(vector3s[1].x>0){
			vector3s[2].x = punch(vector3s[1].x,percentage);
		}else if(vector3s[1].x<0){
			vector3s[2].x=-punch(Mathf.Abs(vector3s[1].x),percentage); 
		}
		if(vector3s[1].y>0){
			vector3s[2].y=punch(vector3s[1].y,percentage);
		}else if(vector3s[1].y<0){
			vector3s[2].y=-punch(Mathf.Abs(vector3s[1].y),percentage); 
		}
		if(vector3s[1].z>0){
			vector3s[2].z=punch(vector3s[1].z,percentage);
		}else if(vector3s[1].z<0){
			vector3s[2].z=-punch(Mathf.Abs(vector3s[1].z),percentage); 
		}
		
		//apply:
		transform.Rotate(vector3s[2]-vector3s[3],space);

		//record:
		vector3s[3]=vector3s[2];
	}	
	
	void ApplyPunchScaleTargets(){
		//calculate:
		if(vector3s[1].x>0){
			vector3s[2].x = punch(vector3s[1].x,percentage);
		}else if(vector3s[1].x<0){
			vector3s[2].x=-punch(Mathf.Abs(vector3s[1].x),percentage); 
		}
		if(vector3s[1].y>0){
			vector3s[2].y=punch(vector3s[1].y,percentage);
		}else if(vector3s[1].y<0){
			vector3s[2].y=-punch(Mathf.Abs(vector3s[1].y),percentage); 
		}
		if(vector3s[1].z>0){
			vector3s[2].z=punch(vector3s[1].z,percentage);
		}else if(vector3s[1].z<0){
			vector3s[2].z=-punch(Mathf.Abs(vector3s[1].z),percentage); 
		}
		
		//apply:
		transform.localScale=vector3s[0]+vector3s[2];
	}		
	
	#endregion	
	
	#region #5 Tween Steps
	
	IEnumerator TweenDelay(){
		delayStarted = Time.time;
		yield return new WaitForSeconds (delay);
	}	
	
	void TweenStart(){		
		if(!loop){//only if this is not a loop
			ConflictCheck();
			GenerateTargets();
		}
		
		//setup curve crap?
		//		
		
		//run stab:
		if(type == "stab"){
			audioSource.PlayOneShot(audioSource.clip);
		}
		
		//toggle isKinematic for iTweens that may interfere with physics:
		if (type == "move" || type=="scale" || type=="rotate" || type=="punch" || type=="shake" || type=="curve" || type=="look") {
			EnableKinematic();
		}
		
		CallBack("onstart");
		isRunning = true;
	}
	
	IEnumerator TweenRestart(){
		if(delay > 0){
			delayStarted = Time.time;
			yield return new WaitForSeconds (delay);
		}
		loop=true;
		TweenStart();
	}	
	
	void TweenUpdate(){
		apply();
		CallBack("onupdate");
		UpdatePercentage();		
	}
			
	void TweenComplete(){
		CallBack("oncomplete");
		isRunning=false;
		
		//dial in percentage to 1 or 0 for final run:
		if(percentage>.5f){
			percentage=1f;
		}else{
			percentage=0;	
		}
		
		//apply dial in and final run:
		if(type == "value"){
			CallBack("onupdate"); //CallBack run for ValueTo since it only calculates and applies in the update callback
		}
		apply();
		
		//loop or dispose?
		if(loopType==LoopType.none){
			Dispose();
		}else{
			TweenLoop();
		}
	}
	
	void TweenLoop(){
		DisableKinematic(); //give physics control again
		switch(loopType){
			case LoopType.loop:
				//rewind:
				percentage=0;
				runningTime=0;
				apply();
				
				//replay:
				StartCoroutine("TweenRestart");
				break;
			case LoopType.pingPong:
				reverse = !reverse;
				runningTime=0;
			
				//replay:
				StartCoroutine("TweenRestart");
				break;
		}
	}	
	
	#endregion
	
	#region #6 Update Callable
	
	/// <summary>
	/// Similar to MoveTo but incredibly less expensive for usage inside the Update or similar looping situations involving a "live" set of changing values. 
	/// </summary>
	/// <param name="position">
	/// A <see cref="Vector3"/>
	/// </param>
	/// <param name="x">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="y">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="z">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	/// <param name="orienttopath">
	/// A <see cref="System.Boolean"/>
	/// </param>
	/// <param name="looktarget">
	/// A <see cref="Vector3"/> or A <see cref="Transform"/>
	/// </param>
	/// <param name="islocal">
	/// A <see cref="System.Boolean"/>
	/// </param>
	/// <param name="smoothtime">
	/// A <see cref="System.Single"/> or <see cref="System.Double"/>
	/// </param>
	public static void UpdateMove(GameObject target, Hashtable args){
		CleanArgs(args);
		
		float smoothTime=0;
		Vector3[] vector3s = new Vector3[4];
		bool isLocal;
			
		//set smooth time:
		if(args.Contains("smoothtime")){
			smoothTime = (float)args["smoothtime"];
		}else{
			smoothTime=	Defaults.smoothTime;
		}
			
		//set isLocal:
		if(args.Contains("islocal")){
			isLocal = (bool)args["islocal"];
		}else{
			isLocal = Defaults.isLocal;	
		}
		
		//init values:
		if(isLocal){
			vector3s[0] = vector3s[1] = target.transform.localPosition;
		}else{
			vector3s[0] = vector3s[1] = target.transform.position;	
		}
		
		//to values:
		if (args.Contains("position")) {
			vector3s[1]=(Vector3)args["position"];
		}else{
			if (args.Contains("x")) {
				vector3s[1].x=(float)args["x"];
			}
			if (args.Contains("y")) {
				vector3s[1].y=(float)args["y"];
			}
			if (args.Contains("z")) {
				vector3s[1].z=(float)args["z"];
			}
		}
		
		//calculate:
		vector3s[3].x=Mathf.SmoothDamp(vector3s[0].x,vector3s[1].x,ref vector3s[2].x,smoothTime);
		vector3s[3].y=Mathf.SmoothDamp(vector3s[0].y,vector3s[1].y,ref vector3s[2].y,smoothTime);
		vector3s[3].z=Mathf.SmoothDamp(vector3s[0].z,vector3s[1].z,ref vector3s[2].z,smoothTime);
			
		//handle orient to path:
		if(args.Contains("orienttopath") && (bool)args["orienttopath"]){
			args["looktarget"] = vector3s[3];
		}
		
		//look applications:
		if(args.Contains("looktarget")){
			iTween.UpdateLook(target,args);
		}
		
		//apply:
		if(isLocal){
			target.transform.localPosition = vector3s[3];			
		}else{
			target.transform.position=vector3s[3];	
		}		
	}
	
	/// <summary>
	/// Similar to LookTo but incredibly less expensive for usage inside the Update or similar looping situations involving a "live" set of changing values. 
	/// </summary>
	/// <param name="looktarget">
	/// A <see cref="Transform"/> or <see cref="Vector3"/>
	/// </param>
	/// <param name="axis">
	/// A <see cref="System.String"/>
	/// </param>
	public static void UpdateLook(GameObject target, Hashtable args){
		CleanArgs(args);
		
		//markers:
		Vector3 startRotation = target.transform.eulerAngles;
		Quaternion[] quaternions = new Quaternion[2];
		Vector3 axisRestriction = new Vector3();
		
		//from values:
		quaternions[0]=target.transform.rotation;
		
		//set look:
		if(args.Contains("looktarget")){
			if (args["looktarget"].GetType() == typeof(Transform)) {
				target.transform.LookAt((Transform)args["looktarget"]);
			}else if(args["looktarget"].GetType() == typeof(Vector3)){
				target.transform.LookAt((Vector3)args["looktarget"]);
			}
		}else{
			Debug.LogError("iTween Error: UpdateLook needs a 'looktarget' property!");
			return;
		}

		//to values and reset look:
		quaternions[1]=target.transform.rotation;
		target.transform.eulerAngles=startRotation;
		
		//set lookspeed:
		float lookSpeed;
		if(args.Contains("lookspeed")){
			lookSpeed = (float)args["lookspeed"];
		}else{
			lookSpeed = Defaults.smoothTime*60;
			print("BAD!");
		}
				
		//application:
		target.transform.rotation = Quaternion.Slerp(quaternions[0],quaternions[1],Time.deltaTime*lookSpeed);		
	
		//axis restriction:
		if(args.Contains("axis")){
			axisRestriction=target.transform.eulerAngles;
			switch((string)args["axis"]){
				case "x":
					axisRestriction.y=startRotation.y;
					axisRestriction.z=startRotation.z;
				break;
				case "y":
					axisRestriction.x=startRotation.x;
					axisRestriction.z=startRotation.z;
				break;
				case "z":
					axisRestriction.x=startRotation.x;
					axisRestriction.y=startRotation.y;
				break;
			}
			//apply axis restriction:
			target.transform.eulerAngles=axisRestriction;
		}
	}
	
	

	#endregion

	#region Component Segments
	
	void Awake(){
		RetrieveArgs();
	}
	
	IEnumerator Start(){
		if(delay > 0){
			yield return StartCoroutine("TweenDelay");
		}
		TweenStart();
	}	
	
	void Update(){
		if(isRunning){
			if(!reverse){
				if(percentage<1f){
					TweenUpdate();
				}else{
					TweenComplete();	
				}
			}else{
				if(percentage>0){
					TweenUpdate();
				}else{
					TweenComplete();	
				}
			}
		}
	}

	void LateUpdate(){
		//look applications:
		if(tweenArguments.Contains("looktarget") && isRunning){
			UpdateLook(gameObject,tweenArguments);
		}
	}
	
	void OnEnable(){
		if(isRunning){
			EnableKinematic();
		}
		//resume delay:
		if(isPaused && delay>0){
			isPaused=false;
			ResumeDelay();
		}
	}

	void OnDisable(){
		DisableKinematic();
	}
	
	#endregion
	
	#region External Utilities
	public static Hashtable Hash(params object[] args){
		Hashtable hashTable = new Hashtable(args.Length/2);
		if (args.Length %2 != 0) {
			Debug.LogError("Tween Error: Hash requires an even number of arguments!"); 
			return null;
		}else{
			int i = 0;
			while(i < args.Length - 1) {
				hashTable.Add(args[i], args[i+1]);
				i += 2;
			}
			return hashTable;
		}
	}
	
//	stops
//	pauses
//	completes
//	rewinds
//	counts	
	#endregion	
	
	#region Internal Helpers
	
	//catalog new tween and add component phase of iTween:
	static void Launch(GameObject target, Hashtable args){
		if(!args.Contains("id")){
			args["id"] = GenerateID();
		}
		if(!args.Contains("target")){
			args["target"] = target;
		}		
		tweens.Insert(0,args);
		target.AddComponent("iTween");
	}		
	
	//cast any accidentally supplied doubles and ints as floats as iTween only uses floats internally and unify parameter case:
	static Hashtable CleanArgs(Hashtable args){
		Hashtable argsCopy = new Hashtable(args.Count);
		Hashtable argsCaseUnified = new Hashtable(args.Count);
		
		foreach (DictionaryEntry item in args) {
			argsCopy.Add(item.Key, item.Value);
		}
		
		foreach (DictionaryEntry item in argsCopy) {
			if(item.Value.GetType() == typeof(System.Int32)){
				int original = (int)item.Value;
				float casted = (float)original;
				args[item.Key] = casted;
			}
			if(item.Value.GetType() == typeof(System.Double)){
				double original = (double)item.Value;
				float casted = (float)original;
				args[item.Key] = casted;
			}
		}	
		
		//unify parameter case:
		foreach (DictionaryEntry item in args) {
			argsCaseUnified.Add(item.Key.ToString().ToLower(), item.Value);
		}	
		
		//swap back case unification:
		args = argsCaseUnified;
				
		return args;
	}	
	
	//random ID generator:
	static string GenerateID(){
		int strlen = 15;
		char[] chars = {'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z','A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z','0','1','2','3','4','5','6','7','8'};
		int num_chars = chars.Length - 1;
		string randomChar = "";
		for (int i = 0; i < strlen; i++) {
			randomChar += chars[(int)Mathf.Floor(UnityEngine.Random.Range(0,num_chars))];
		}
		return randomChar;
	}	
	
	//grab and set generic, neccesary iTween arguments:
	void RetrieveArgs(){
		foreach (Hashtable item in tweens) {
			if((GameObject)item["target"] == gameObject){
				tweenArguments=item;
				break;
			}
		}
		
		id=(string)tweenArguments["id"];
		type=(string)tweenArguments["type"];
		method=(string)tweenArguments["method"];
               
		if(tweenArguments.Contains("time")){
			time=(float)tweenArguments["time"];
		}else{
			time=Defaults.time;
		}
               
		if(tweenArguments.Contains("delay")){
			delay=(float)tweenArguments["delay"];
		}else{
			delay=Defaults.delay;
		}
				
		if(tweenArguments.Contains("looptype")){
			//allows loopType to be set as either an enum(C# friendly) or a string(JS friendly), string case usage doesn't matter to further increase usability:
			if(tweenArguments["looptype"].GetType() == typeof(LoopType)){
				loopType=(LoopType)tweenArguments["looptype"];
			}else{
				try {
					loopType=(LoopType)Enum.Parse(typeof(LoopType),(string)tweenArguments["looptype"],true); 
				} catch {
					Debug.LogWarning("iTween: Unsupported loopType supplied! Default will be used.");
					loopType = iTween.LoopType.none;	
				}
			}			
		}else{
			loopType = iTween.LoopType.none;	
		}		
         
		if(tweenArguments.Contains("easetype")){
			//allows easeType to be set as either an enum(C# friendly) or a string(JS friendly), string case usage doesn't matter to further increase usability:
			if(tweenArguments["easetype"].GetType() == typeof(EaseType)){
				easeType=(EaseType)tweenArguments["easetype"];
			}else{
				try {
					easeType=(EaseType)Enum.Parse(typeof(EaseType),(string)tweenArguments["easetype"],true); 
				} catch {
					Debug.LogWarning("iTween: Unsupported easeType supplied! Default will be used.");
					easeType=Defaults.easeType;
				}
			}
		}else{
			easeType=Defaults.easeType;
		}
				
		if(tweenArguments.Contains("space")){
			//allows space to be set as either an enum(C# friendly) or a string(JS friendly), string case usage doesn't matter to further increase usability:
			if(tweenArguments["space"].GetType() == typeof(Space)){
				space=(Space)tweenArguments["space"];
			}else{
				try {
					space=(Space)Enum.Parse(typeof(Space),(string)tweenArguments["space"],true); 	
				} catch {
					Debug.LogWarning("iTween: Unsupported space supplied! Default will be used.");
					space = Defaults.space;
				}
			}			
		}else{
			space = Defaults.space;
		}
		
		if(tweenArguments.Contains("islocal")){
			isLocal = (bool)tweenArguments["islocal"];
		}else{
			isLocal = Defaults.isLocal;
		}
		
		//instantiates a cached ease equation reference:
		GetEasingFunction();
	}	
	
	//instantiates a cached ease equation refrence:
	void GetEasingFunction(){
		switch (easeType){
		case EaseType.easeInQuad:
			ease  = new EasingFunction(easeInQuad);
			break;
		case EaseType.easeOutQuad:
			ease = new EasingFunction(easeOutQuad);
			break;
		case EaseType.easeInOutQuad:
			ease = new EasingFunction(easeInOutQuad);
			break;
		case EaseType.easeInCubic:
			ease = new EasingFunction(easeInCubic);
			break;
		case EaseType.easeOutCubic:
			ease = new EasingFunction(easeOutCubic);
			break;
		case EaseType.easeInOutCubic:
			ease = new EasingFunction(easeInOutCubic);
			break;
		case EaseType.easeInQuart:
			ease = new EasingFunction(easeInQuart);
			break;
		case EaseType.easeOutQuart:
			ease = new EasingFunction(easeOutQuart);
			break;
		case EaseType.easeInOutQuart:
			ease = new EasingFunction(easeInOutQuart);
			break;
		case EaseType.easeInQuint:
			ease = new EasingFunction(easeInQuint);
			break;
		case EaseType.easeOutQuint:
			ease = new EasingFunction(easeOutQuint);
			break;
		case EaseType.easeInOutQuint:
			ease = new EasingFunction(easeInOutQuint);
			break;
		case EaseType.easeInSine:
			ease = new EasingFunction(easeInSine);
			break;
		case EaseType.easeOutSine:
			ease = new EasingFunction(easeOutSine);
			break;
		case EaseType.easeInOutSine:
			ease = new EasingFunction(easeInOutSine);
			break;
		case EaseType.easeInExpo:
			ease = new EasingFunction(easeInExpo);
			break;
		case EaseType.easeOutExpo:
			ease = new EasingFunction(easeOutExpo);
			break;
		case EaseType.easeInOutExpo:
			ease = new EasingFunction(easeInOutExpo);
			break;
		case EaseType.easeInCirc:
			ease = new EasingFunction(easeInCirc);
			break;
		case EaseType.easeOutCirc:
			ease = new EasingFunction(easeOutCirc);
			break;
		case EaseType.easeInOutCirc:
			ease = new EasingFunction(easeInOutCirc);
			break;
		case EaseType.linear:
			ease = new EasingFunction(linear);
			break;
		case EaseType.spring:
			ease = new EasingFunction(spring);
			break;
		case EaseType.bounce:
			ease = new EasingFunction(bounce);
			break;
		case EaseType.easeInBack:
			ease = new EasingFunction(easeInBack);
			break;
		case EaseType.easeOutBack:
			ease = new EasingFunction(easeOutBack);
			break;
		case EaseType.easeInOutBack:
			ease = new EasingFunction(easeInOutBack);
			break;
		}
	}
	
	//calculate percentage of tween based on time:
	void UpdatePercentage(){
		runningTime+=Time.deltaTime;
		if(reverse){
			percentage = 1 - runningTime/time;	
		}else{
			percentage = runningTime/time;	
		}
	}
	
	void CallBack(string callbackType){
		if (tweenArguments.Contains(callbackType) && !tweenArguments.Contains("ischild")) {
			//establish target:
			GameObject target;
			if (tweenArguments.Contains(callbackType+"target")) {
				target=(GameObject)tweenArguments[callbackType+"target"];
			}else{
				target=gameObject;	
			}
			
			//throw an error if a string wasn't passed for callback:
			if (tweenArguments[callbackType].GetType() == typeof(System.String)) {
				target.SendMessage((string)tweenArguments[callbackType],(object)tweenArguments[callbackType+"params"],SendMessageOptions.DontRequireReceiver);
			}else{
				Debug.LogError("iTween Error: Callback method references must be passed as a String!");
				Destroy (this);
			}
		}
	}
	
	void Dispose(){
		for (int i = 0; i < tweens.Count; i++) {
			Hashtable tweenEntry = (Hashtable)tweens[i];
			if ((string)tweenEntry["id"] == id){
				tweens.RemoveAt(i);
				break;
			}
		}
		Destroy(this);
	}	
	
	void ConflictCheck(){//if a new iTween is about to run and is of the same type as an in progress iTween this will destroy the previous if the new one is NOT identical in every way or it will destroy the new iTween if they are
		Component[] tweens = GetComponents(typeof(iTween));
		foreach (iTween item in tweens) {
			if(item.isRunning && item.type==type){
				//step 1: check for length first since it's the fastest:
				if(item.tweenArguments.Count != tweenArguments.Count){
					item.Dispose();
					return;
				}
				
				//step 2: side-by-side check to figure out if this is an identical tween scenario to handle Update usages of iTween:
				foreach (DictionaryEntry currentProp in tweenArguments) {

					if(!item.tweenArguments.Contains(currentProp.Key)){
						item.Dispose();
						return;
					}else{
						if(!item.tweenArguments[currentProp.Key].Equals(tweenArguments[currentProp.Key]) && (string)currentProp.Key != "id"){//if we aren't comparing ids and something isn't exactly the same replace the running iTween

							item.Dispose();
							return;
						}
					}
				}
				
				//step 3: prevent a new iTween addition if it is identical to the currently running iTween
				Destroy(this);	
			}
		}
	}
	
	void EnableKinematic(){
		if(gameObject.GetComponent(typeof(Rigidbody))){
			if(!rigidbody.isKinematic){
				kinematic=true;
				rigidbody.isKinematic=true;
			}
		}
	}
	
	void DisableKinematic(){
		if(kinematic && rigidbody.isKinematic==true){
			kinematic=false;
			rigidbody.isKinematic=false;
		}
	}
	
	IEnumerator ResumeDelay(){	
		yield return StartCoroutine("TweenDelay");
		TweenStart();
	}
	
	#endregion	
	
	#region Easing Curves
	
	private float linear(float start, float end, float value){
		return Mathf.Lerp(start, end, value);
	}
	
	private float clerp(float start, float end, float value){
		float min = 0.0f;
		float max = 360.0f;
		float half = Mathf.Abs((max - min) / 2.0f);
		float retval = 0.0f;
		float diff = 0.0f;
		if ((end - start) < -half){
			diff = ((max - start) + end) * value;
			retval = start + diff;
		}else if ((end - start) > half){
			diff = -((max - end) + start) * value;
			retval = start + diff;
		}else retval = start + (end - start) * value;
		return retval;
    }

	private float spring(float start, float end, float value){
		value = Mathf.Clamp01(value);
		value = (Mathf.Sin(value * Mathf.PI * (0.2f + 2.5f * value * value * value)) * Mathf.Pow(1f - value, 2.2f) + value) * (1f + (1.2f * (1f - value)));
		return start + (end - start) * value;
	}

	private float easeInQuad(float start, float end, float value){
		end -= start;
		return end * value * value + start;
	}

	private float easeOutQuad(float start, float end, float value){
		end -= start;
		return -end * value * (value - 2) + start;
	}

	private float easeInOutQuad(float start, float end, float value){
		value /= .5f;
		end -= start;
		if (value < 1) return end / 2 * value * value + start;
		value--;
		return -end / 2 * (value * (value - 2) - 1) + start;
	}

	private float easeInCubic(float start, float end, float value){
		end -= start;
		return end * value * value * value + start;
	}

	private float easeOutCubic(float start, float end, float value){
		value--;
		end -= start;
		return end * (value * value * value + 1) + start;
	}

	private float easeInOutCubic(float start, float end, float value){
		value /= .5f;
		end -= start;
		if (value < 1) return end / 2 * value * value * value + start;
		value -= 2;
		return end / 2 * (value * value * value + 2) + start;
	}

	private float easeInQuart(float start, float end, float value){
		end -= start;
		return end * value * value * value * value + start;
	}

	private float easeOutQuart(float start, float end, float value){
		value--;
		end -= start;
		return -end * (value * value * value * value - 1) + start;
	}

	private float easeInOutQuart(float start, float end, float value){
		value /= .5f;
		end -= start;
		if (value < 1) return end / 2 * value * value * value * value + start;
		value -= 2;
		return -end / 2 * (value * value * value * value - 2) + start;
	}

	private float easeInQuint(float start, float end, float value){
		end -= start;
		return end * value * value * value * value * value + start;
	}

	private float easeOutQuint(float start, float end, float value){
		value--;
		end -= start;
		return end * (value * value * value * value * value + 1) + start;
	}

	private float easeInOutQuint(float start, float end, float value){
		value /= .5f;
		end -= start;
		if (value < 1) return end / 2 * value * value * value * value * value + start;
		value -= 2;
		return end / 2 * (value * value * value * value * value + 2) + start;
	}

	private float easeInSine(float start, float end, float value){
		end -= start;
		return -end * Mathf.Cos(value / 1 * (Mathf.PI / 2)) + end + start;
	}

	private float easeOutSine(float start, float end, float value){
		end -= start;
		return end * Mathf.Sin(value / 1 * (Mathf.PI / 2)) + start;
	}

	private float easeInOutSine(float start, float end, float value){
		end -= start;
		return -end / 2 * (Mathf.Cos(Mathf.PI * value / 1) - 1) + start;
	}

	private float easeInExpo(float start, float end, float value){
		end -= start;
		return end * Mathf.Pow(2, 10 * (value / 1 - 1)) + start;
	}

	private float easeOutExpo(float start, float end, float value){
		end -= start;
		return end * (-Mathf.Pow(2, -10 * value / 1) + 1) + start;
	}

	private float easeInOutExpo(float start, float end, float value){
		value /= .5f;
		end -= start;
		if (value < 1) return end / 2 * Mathf.Pow(2, 10 * (value - 1)) + start;
		value--;
		return end / 2 * (-Mathf.Pow(2, -10 * value) + 2) + start;
	}

	private float easeInCirc(float start, float end, float value){
		end -= start;
		return -end * (Mathf.Sqrt(1 - value * value) - 1) + start;
	}

	private float easeOutCirc(float start, float end, float value){
		value--;
		end -= start;
		return end * Mathf.Sqrt(1 - value * value) + start;
	}

	private float easeInOutCirc(float start, float end, float value){
		value /= .5f;
		end -= start;
		if (value < 1) return -end / 2 * (Mathf.Sqrt(1 - value * value) - 1) + start;
		value -= 2;
		return end / 2 * (Mathf.Sqrt(1 - value * value) + 1) + start;
	}

	private float bounce(float start, float end, float value){
		value /= 1f;
		end -= start;
		if (value < (1 / 2.75f)){
			return end * (7.5625f * value * value) + start;
		}else if (value < (2 / 2.75f)){
			value -= (1.5f / 2.75f);
			return end * (7.5625f * (value) * value + .75f) + start;
		}else if (value < (2.5 / 2.75)){
			value -= (2.25f / 2.75f);
			return end * (7.5625f * (value) * value + .9375f) + start;
		}else{
			value -= (2.625f / 2.75f);
			return end * (7.5625f * (value) * value + .984375f) + start;
		}
	}

	private float easeInBack(float start, float end, float value){
		end -= start;
		value /= 1;
		float s = 1.70158f;
		return end * (value) * value * ((s + 1) * value - s) + start;
	}

	private float easeOutBack(float start, float end, float value){
		float s = 1.70158f;
		end -= start;
		value = (value / 1) - 1;
		return end * ((value) * value * ((s + 1) * value + s) + 1) + start;
	}

	private float easeInOutBack(float start, float end, float value){
		float s = 1.70158f;
		end -= start;
		value /= .5f;
		if ((value) < 1){
			s *= (1.525f);
			return end / 2 * (value * value * (((s) + 1) * value - s)) + start;
		}
		value -= 2;
		s *= (1.525f);
		return end / 2 * ((value) * value * (((s) + 1) * value + s) + 2) + start;
	}

	private float punch(float amplitude, float value){
		float s = 9;
		if (value == 0){
			return 0;
		}
		if (value == 1){
			return 0;
		}
		float period = 1 * 0.3f;
		s = period / (2 * Mathf.PI) * Mathf.Asin(0);
		return (amplitude * Mathf.Pow(2, -10 * value) * Mathf.Sin((value * 1 - s) * (2 * Mathf.PI) / period));
    }
	#endregion	
	
	#region Deprecated and Renamed
	/*
	public static void audioFrom(GameObject target, Hashtable args){Debug.LogError("iTween Error: audioFrom() has been renamed to AudioFrom().");}
	public static void audioTo(GameObject target, Hashtable args){Debug.LogError("iTween Error: audioTo() has been renamed to AudioTo().");}
	public static void colorFrom(GameObject target, Hashtable args){Debug.LogError("iTween Error: colorFrom() has been renamed to ColorFrom().");}
	public static void colorTo(GameObject target, Hashtable args){Debug.LogError("iTween Error: colorTo() has been renamed to ColorTo().");}
	public static void fadeFrom(GameObject target, Hashtable args){Debug.LogError("iTween Error: fadeFrom() has been renamed to FadeFrom().");}
	public static void fadeTo(GameObject target, Hashtable args){Debug.LogError("iTween Error: fadeTo() has been renamed to FadeTo().");}
	public static void lookFrom(GameObject target, Hashtable args){Debug.LogError("iTween Error: lookFrom() has been renamed to LookFrom().");}
	public static void lookFromWorld(GameObject target, Hashtable args){Debug.LogError("iTween Error: lookFromWorld() has been deprecated. Please investigate LookFrom().");}
	public static void lookTo(GameObject target, Hashtable args){Debug.LogError("iTween Error: lookTo() has been renamed to LookTo().");}
	public static void lookToUpdate(GameObject target, Hashtable args){Debug.LogError("iTween Error: lookToUpdate() has been renamed to UpdateLook().");}
	public static void lookToUpdateWorld(GameObject target, Hashtable args){Debug.LogError("iTween Error: lookToUpdateWorld() has been deprecated. Please investigate UpdateLook().");}
	public static void moveAdd(GameObject target, Hashtable args){Debug.LogError("iTween Error: moveAdd() has been renamed to MoveAdd().");}
	public static void moveAddWorld(GameObject target, Hashtable args){Debug.LogError("iTween Error: moveAddWorld() has been deprecated. Please investigate MoveAdd().");}
	public static void moveBy(GameObject target, Hashtable args){Debug.LogError("iTween Error: moveBy() has been renamed to MoveBy().");}
	public static void moveByWorld(GameObject target, Hashtable args){Debug.LogError("iTween Error: moveAddWorld() has been deprecated. Please investigate MoveAdd().");}
	public static void moveFrom(GameObject target, Hashtable args){Debug.LogError("iTween Error: moveFrom() has been renamed to MoveFrom().");}
	public static void moveFromWorld(GameObject target, Hashtable args){Debug.LogError("iTween Error: moveFromWorld() has been deprecated. Please investigate MoveFrom().");}
	public static void moveTo(GameObject target, Hashtable args){Debug.LogError("iTween Error: moveTo() has been renamed to MoveTo().");}
	public static void moveToBezier(GameObject target, Hashtable args){Debug.LogError("iTween Error: moveToBezier() has been deprecated. Please investigate CurveTo().");}
	public static void moveToBezierWorld(GameObject target, Hashtable args){Debug.LogError("iTween Error: moveToBezierWorld() has been deprecated. Please investigate CurveTo().");}
	public static void moveToUpdate(GameObject target, Hashtable args){Debug.LogError("iTween Error: moveToUpdate() has been renamed to UpdateMove().");}
	public static void moveToUpdateWorld(GameObject target, Hashtable args){Debug.LogError("iTween Error: moveToUpdateWorld() has been deprecated. Please investigate UpdateMove().");}
	public static void moveToWorld(GameObject target, Hashtable args){Debug.LogError("iTween Error: moveToWorld() has been deprecated. Please investigate MoveTo().");}
	public static void punchPosition(GameObject target, Hashtable args){Debug.LogError("iTween Error: punchPosition() has been renamed to PunchPosition().");}
	public static void punchPositionWorld(GameObject target, Hashtable args){Debug.LogError("iTween Error: punchPositionWorld() has been deprecated. Please investigate PunchPosition().");}	
	public static void punchRotation(GameObject target, Hashtable args){Debug.LogError("iTween Error: punchPosition() has been renamed to PunchRotation().");}
	public static void punchRotationWorld(GameObject target, Hashtable args){Debug.LogError("iTween Error: punchRotationWorld() has been deprecated. Please investigate PunchRotation().");}	
	public static void punchScale(GameObject target, Hashtable args){Debug.LogError("iTween Error: punchScale() has been renamed to PunchScale().");}
	public static void rotateAdd(GameObject target, Hashtable args){Debug.LogError("iTween Error: rotateAdd() has been renamed to RotateAdd().");}
	public static void rotateBy(GameObject target, Hashtable args){Debug.LogError("iTween Error: rotateBy() has been renamed to RotateBy().");}
	public static void rotateByWorld(GameObject target, Hashtable args){Debug.LogError("iTween Error: rotateByWorld() has been deprecated. Please investigate RotateBy().");}
	public static void rotateFrom(GameObject target, Hashtable args){Debug.LogError("iTween Error: rotateFrom() has been renamed to RotateFrom().");}
	public static void rotateTo(GameObject target, Hashtable args){Debug.LogError("iTween Error: rotateTo() has been renamed to RotateTo().");}
	public static void scaleAdd(GameObject target, Hashtable args){Debug.LogError("iTween Error: scaleAdd() has been renamed to ScaleAdd().");}
	public static void scaleBy(GameObject target, Hashtable args){Debug.LogError("iTween Error: scaleBy() has been renamed to ScaleBy().");}
	public static void scaleFrom(GameObject target, Hashtable args){Debug.LogError("iTween Error: scaleFrom() has been renamed to ScaleFrom().");}
	public static void scaleTo(GameObject target, Hashtable args){Debug.LogError("iTween Error: scaleTo() has been renamed to ScaleTo().");}
	public static void shake(GameObject target, Hashtable args){Debug.LogError("iTween Error: scale() has been deprecated. Please investigate ShakePosition(), ShakeRotation() and ShakeScale().");}
	public static void shakeWorld(GameObject target, Hashtable args){Debug.LogError("iTween Error: shakeWorld() has been deprecated. Please investigate ShakePosition(), ShakeRotation() and ShakeScale().");}
	public static void stab(GameObject target, Hashtable args){Debug.LogError("iTween Error: stab() has been renamed to Stab().");}
	public static void stop(GameObject target, Hashtable args){Debug.LogError("iTween Error: stop() has been renamed to Stop().");}
	public static void stopType(GameObject target, Hashtable args){Debug.LogError("iTween Error: stopType() has been deprecated. Please investigate Stop().");}
	public static void tweenCount(GameObject target, Hashtable args){Debug.LogError("iTween Error: tweenCount() has been deprecated. Please investigate Count().");}
	*/
	#endregion
}