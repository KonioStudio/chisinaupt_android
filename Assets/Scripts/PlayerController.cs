using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

public class PlayerController : MonoBehaviour {

	//transmision
	public List<float> GearRatios;

	public float axleRatio;

	public float minRpm;

	public float maxRpm;

	public float wheelRadius;
	//public GUIText score_text;
	//public GUIText speed;

	public float hp;

	public float converterRatio = 3f;

	private float rpm;
	private int gear;

	// Player Data -> To Save

	public int money = 0;

	//position on road

	private int band = 3;
	private int previousBand = 0;

	private bool pressed = false;
	public bool isPaused = false;
	public bool gameOver = false;

	private float[] positions = new float[]{-5f,-1.8f,1.8f,5f};

	private bool isMoving = false;
	private bool saved = false;

	public float rotationAngle = 20f;

	public float brakeTorque = 4000f;
	public int coinValue = 3;

	private bool touchBrake = false;
	private bool touchAccessMenu = false;
	public bool touchGas = false;
	
	//main controller
	GameObject mainController;
	Loader loader;

	Quaternion initRot;

	void Start() {
		mainController = GameObject.FindGameObjectWithTag("MainController");
		loader = mainController.GetComponent<Loader>();
		Time.timeScale  = 1;
		rpm = minRpm;
		gear = 0;
		money = 0;
		initRot = transform.rotation;
	}

	public void makeTouch(int direction){
		move (direction);
	}

	public void menuTouch(){
		touchAccessMenu = true;
	}

	public void brakeTouch(){
		touchBrake = true;
	}

	public void Save(){
		if(!saved){
			loader.Died(money);
			money = 0;
			saved = true;
		}
	}

	public void Resume(){
		isPaused = false;
		Time.timeScale = 1;
	}

	public void Exit(){
		Application.LoadLevel("menu");
		Save();
	}
	/*
	void OnGUI(){
		if(isPaused){			
			pausePanel.SetActive(true);
			isPaused = false;
		}
		// Game Over
		if(gameOver){
			Time.timeScale = 0;
			isPaused = false;
			//canvas2.alpha = 1f;
			canvasGroups[0].alpha = 1f;
			Save();
		}

	}
	*/
	void Update() {

		if(Input.GetKeyDown(KeyCode.Escape) || touchAccessMenu){
			if(isPaused)
				isPaused = false;
			else
				isPaused = true;
			if(touchAccessMenu)
				touchAccessMenu = false;
		}

		if(isPaused || gameOver)
			return;

		if (isMoving && !isPaused)
			smoothMove();
		else
			if(transform.position.x != positions[band])
				transform.position = new Vector3(positions[band],transform.position.y,transform.position.z);

		float gas = (touchGas || Input.GetKey("up")) ? 1 : 0;
		float brake = -1 * Mathf.Clamp(Input.GetAxis("Vertical"), -1, 0);	

		if(touchBrake)
			brake = 1;

		if(brake != 0){
			rpm -= Time.deltaTime * brakeTorque;
			rpm = Mathf.Max (minRpm,rpm);
			touchBrake = false;
		}
		if(gas == 0 && brake == 0){
			rpm = Mathf.Max(getRpmByVelocity() - (150f*Time.deltaTime),minRpm);
		}
		if(gas == 1 && brake == 0){
			rpm += Time.deltaTime * (10*hp - gear*20) * gas;
			rpm = Mathf.Min (maxRpm,rpm);
		}
		if(rpm == maxRpm && gear < GearRatios.Count-1){
			gear++;
			rpm = getRpmByVelocity() + 0.01f;
			if(audio.enabled){
				audio.Stop();
				audio.PlayDelayed(0.1f);
			}
		}
		if(rpm == minRpm && gear > 0){
			gear--;
			rpm = getRpmByVelocity() - 0.01f;
			if(audio.enabled){
				audio.Stop();
				audio.PlayDelayed(0.1f);
			}
		}
		if(Input.GetKeyDown("left")){
			move (-1);
			pressed = true;

		}
		if(Input.GetKeyDown("right")){
			move (1);
			pressed = true;

		}
		if(Input.GetKeyUp("left") || Input.GetKeyUp("right"))
			pressed = false;
		rigidbody.velocity = new Vector3(0,0,getVelocity());
		audio.pitch = 1 + (rpm/maxRpm);
	}

	public void move(int x) {
		if(gameOver)
			return ;
		if ((band < 3 && x > 0) || (band > 0 && x < 0)) {
			if(!isMoving)
				previousBand = band;
			band = band + x;	
		}
		isMoving = true;
	}

	public void smoothMove(){
		float[] newPositions = new float[]{0f,3.2f,6.8f,10f};

		float x = transform.position.x;
		float coefficient = 0;
		if(getVelocity() < 10)
			coefficient = (getVelocity()/20f);		
		else
			coefficient = (getVelocity()/40f);

		float units = 0.13f * coefficient * 60 * Time.deltaTime;
		/*
		float angle = 20f * coefficient;
		float angle2 = rotationAngle * coefficient;
		float k = (band > previousBand) ? 1 : -1;
		if (band == previousBand){
			transform.rotation = initRot;
		} else {
			float radius = angle / (Mathf.Abs(newPositions[band] - newPositions[previousBand])/units);
			float radius2 = angle2 / (Mathf.Abs(newPositions[band] - newPositions[previousBand])/units);
			if (k > 0) {
				float mediumX = (newPositions[band] + newPositions[previousBand])/2f;
				if (x - positions[0] < mediumX){
					transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x,transform.rotation.eulerAngles.y + radius,transform.rotation.eulerAngles.z + radius2);
				}else{	
					transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x,transform.rotation.eulerAngles.y - radius,transform.rotation.eulerAngles.z - radius2);
				}
			} else {
				float mediumX = (newPositions[previousBand] + newPositions[band])/2f;	
				if (x - positions[0] < mediumX){				
					transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x,transform.rotation.eulerAngles.y + radius,transform.rotation.eulerAngles.z + radius2);
				}else{			
					transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x,transform.rotation.eulerAngles.y - radius,transform.rotation.eulerAngles.z - radius2);
				}
			}
		}
		*/
		x += ((x < positions[band]) ? 1f  : -1f) * units;
		Vector3 newPos = new Vector3 (x, transform.position.y, transform.position.z);
		transform.position = newPos;
		if (Mathf.Abs (x - positions [band]) < 0.2f) {
			Vector3 Pos = new Vector3 (positions[band], transform.position.y, transform.position.z);
			transform.position = Pos;
			transform.rotation = initRot;
			isMoving = false;
		}
	}

	void OnTriggerEnter(Collider other){
		if(other.gameObject.tag == "Coins"){
			money += coinValue;
			other.gameObject.SetActive(false);
		}
		if(other.gameObject.tag == "Car" || other.gameObject.tag == "Police"){
			if(other.transform.position.z < this.transform.position.z && other.transform.position.x == this.transform.position.x)
				Destroy(other.gameObject);
			else
				gameOver = true;
		}
	}

	public float getVelocity() {
		return (0.104f * wheelRadius * rpm)/(axleRatio*GearRatios[gear]);
	}

	public float getRpmByVelocity(){
		return rigidbody.velocity.z * axleRatio * GearRatios[gear] / (0.104f * wheelRadius); 
	}
	
}

