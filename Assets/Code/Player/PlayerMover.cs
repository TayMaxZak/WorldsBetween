using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMover : Actor
{
	[SerializeField]
	private LayerMask rayMask;

	[HideInInspector]
	public Vector3 headPosition;
	private Vector3 prevHeadPosition;

	[Header("Size")]
	[SerializeField]
	private Transform head;
	[SerializeField]
	private Vector3 headOffset = new Vector3(0, 0.6f, 0);
	[SerializeField]
	private Vector3 climbOffset = new Vector3(0, 0.55f, 0.75f);
	[SerializeField]
	private Vector3 feetOffset = new Vector3(0, -0.8f, 0);
	[SerializeField]
	private float height = 1.6f;
	[SerializeField]
	private float radius = 0.3f;

	[Header("Distances")]
	[SerializeField]
	private float maxSafeFall = 3.5f;
	[SerializeField]
	private float maxClimbable = 0.6f;
	[SerializeField]
	private float raycastMargin = 0.04f;
	[SerializeField]
	private float strideLength = 0.7f;
	[SerializeField]
	private float peekDistance = 0.4f;

	[Header("Speeds")]
	[SerializeField]
	private float mouseSens = 150;
	private float curSpeed;
	[SerializeField]
	private float walkSpeed = 3.5f;
	[SerializeField]
	private float sprintSpeed = 7f;
	[SerializeField]
	private float sprintAccel = 4;
	[SerializeField]
	private float sprintDeccel = 2;

	[SerializeField]
	private float jumpSpeed = 0.24f;
	private Timer climbingTimer = new Timer(1);
	[SerializeField]
	private float fastClimbTime = 0.55f;
	[SerializeField]
	private float slowClimbTime = 1.1f;
	public float airResistance = 0.05f;
	public float waterResistance = 0.5f;

	private float minMouseV = -85;
	private float maxMouseV = 85;
	private float mouseH = 0;
	private float mouseV = 0;

	[Header("State")]
	public bool grounded = false;
	public bool atLedge = false;
	public bool jump = false;
	public bool climbing = false;
	public bool sprinting = false;

	[Header("Costs")]
	[SerializeField]
	private float sprintCost = 5;

	private Vector3 initPos;

	protected override void Awake()
	{
		base.Awake();

		mouseH = transform.eulerAngles.y;
		mouseV = head.transform.eulerAngles.x;
	}

	public void Respawn()
	{
		position = initPos;

		velocity = Vector3.zero;
		//fallVelocity = Vector3.zero;
		inputVelocity = Vector3.zero;
		climbing = false;

		mouseV = 0;
	}

	public override void Init()
	{
		base.Init();

		headPosition = head.localPosition;
		prevHeadPosition = new Vector3(headPosition.x, headPosition.y, headPosition.z);

		initPos = position + Vector3.up;
	}

	public override void UpdateTick(bool isPhysicsTick, float physicsDeltaTime, float physicsPartialTime)
	{
		if (!didInit || !GameManager.GetFinishedLoading())
			return;

		Cursor.lockState = CursorLockMode.Confined;
		//Cursor.visible = false;

		MouseLookInput();

		if (Player.Instance.vitals.dead)
			return;

		if (!climbing)
		{
			ButtonInput();

			DirectionalInput();

			// Update prev position for lerping
			if (isPhysicsTick)
				prevHeadPosition = new Vector3(headPosition.x, headPosition.y, headPosition.z);

			base.UpdateTick(isPhysicsTick, physicsDeltaTime, physicsPartialTime);

			// Lerp logic position and visual position
			head.localPosition = Vector3.Lerp(prevHeadPosition, headPosition, 1 - (physicsPartialTime / physicsDeltaTime));
		}
		else
		{
			if (isPhysicsTick)
				climbingTimer.Increment(physicsDeltaTime);

			float climbDeltaTime = 1;
			float climbPartialTime = (climbingTimer.currentTime + physicsPartialTime) / climbingTimer.maxTime;
			if (climbingTimer.maxTime > fastClimbTime)
				climbPartialTime = 0.2f * (climbPartialTime) + 0.8f * (climbPartialTime * climbPartialTime * climbPartialTime);
			else
				climbPartialTime = 0.1f * (climbPartialTime) + 0.9f * (climbPartialTime * climbPartialTime);

			if (climbingTimer.Expired())
			{
				climbing = false;
				climbPartialTime = 0;
				inputVelocity = Vector3.zero;

				prevHeadPosition = new Vector3(headPosition.x, headPosition.y, headPosition.z);

				base.UpdateTick(true, physicsDeltaTime, 0);

				// Lerp logic position and visual position
				head.localPosition = Vector3.Lerp(prevHeadPosition, headPosition, 1 - (climbPartialTime / climbDeltaTime));
			}
			else
			{
				base.UpdateTick(false, climbDeltaTime, climbPartialTime);

				// Lerp logic position and visual position
				head.localPosition = Vector3.Lerp(prevHeadPosition, headPosition, 1 - (climbPartialTime / climbDeltaTime));
			}
		}
	}

	private void MouseLookInput()
	{
		if (!Player.Instance.vitals.dead)
		{
			mouseH += Input.GetAxis("Mouse X") * mouseSens;
			mouseV -= Input.GetAxis("Mouse Y") * mouseSens;
		}
		else
		{
			float dampen = 1 - Mathf.Clamp01(mouseV / minMouseV);
			mouseV -= dampen * 0.15f * Time.deltaTime;
		}

		mouseV = Mathf.Clamp(mouseV, minMouseV, maxMouseV);

		transform.localEulerAngles = new Vector3(0, mouseH, 0);
		head.localEulerAngles = new Vector3(mouseV, 0, 0);
	}

	private void DirectionalInput()
	{
		if (!grounded || climbing)
			return;

		Vector3 dirInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
		dirInput = Vector3.ClampMagnitude(dirInput, 1);
		dirInput = transform.rotation * dirInput;

		if (dirInput == Vector3.zero || inWater)
			sprinting = false;
		if (sprinting && Player.Instance.vitals.vitalsTickTimer.currentTime == Player.Instance.vitals.vitalsTickTimer.maxTime)
			sprinting = Player.Instance.vitals.UseStamina(sprintCost * Player.Instance.vitals.vitalsTickTimer.maxTime, false, false);

		curSpeed = Mathf.Lerp(curSpeed, sprinting ? sprintSpeed : walkSpeed, Time.deltaTime * (sprinting ? sprintAccel : sprintDeccel));

		inputVelocity = Vector3.Lerp(inputVelocity, dirInput * curSpeed, Time.deltaTime * (sprinting ? 2 : 12));
	}

	private void ButtonInput()
	{
		if (!grounded || climbing)
			return;

		if (Input.GetButtonDown("Jump"))
		{
			jump = true;
		}
		if (Input.GetButtonDown("Sprint"))
		{
			sprinting = !sprinting;
		}
	}

	public override void PhysicsTick(float deltaTime)
	{
		// Falling acceleration
		Vector3 fallAccel = PhysicsManager.Instance.gravity * deltaTime;

		velocity += fallAccel;

		// Move player and check if grounded
		FallMove(velocity * deltaTime, deltaTime);

		// Drag
		//fallVelocity *= (1 - deltaTime * airResistance);

		// Apply jump
		if (jump)
		{
			jump = false;
			velocity += Vector3.up * jumpSpeed;
		}

		// Move player from jump and other velocity sources
		Move(velocity * deltaTime);

		// Drag
		velocity *= (1 - deltaTime * (inWater ? waterResistance : airResistance));

		// Walk and run input
		InputMove(inputVelocity * deltaTime, deltaTime);
	}

	protected void FallMove(Vector3 moveVector, float deltaTime)
	{
		// Raycast from center of body
		if (BoxCast(moveVector, out RaycastHit hit))
		{
			//fallVelocity = Vector3.zero;
			velocity.y = 0;
			grounded = true;
		}
		else
		{
			//if (!climbing)
			//	position += moveVector;

			grounded = false;
		}
	}

	protected override void Move(Vector3 moveVector)
	{
		// Collision check
		Vector3 adjMoveVector = moveVector;
		if (moveVector != Vector3.zero)
		{
			if (BoxCast(adjMoveVector, out RaycastHit hit))
			{
				Vector3 reflected = Vector3.Reflect(adjMoveVector, hit.normal);
				adjMoveVector += reflected;
				adjMoveVector = Vector3.ClampMagnitude(adjMoveVector, moveVector.magnitude);
			}
		}
		if (moveVector != Vector3.zero)
		{
			if (BoxCast(adjMoveVector, out RaycastHit hit))
			{
				Vector3 reflected = Vector3.Reflect(adjMoveVector, hit.normal);
				adjMoveVector += reflected;
				adjMoveVector = Vector3.ClampMagnitude(adjMoveVector, moveVector.magnitude);
			}
		}
		if (moveVector != Vector3.zero)
		{
			if (BoxCast(adjMoveVector, out RaycastHit hit))
			{
				Vector3 reflected = Vector3.Reflect(adjMoveVector, hit.normal);
				adjMoveVector += reflected;
				adjMoveVector = Vector3.ClampMagnitude(adjMoveVector, moveVector.magnitude);
			}
		}
		if (moveVector != Vector3.zero)
		{
			if (BoxCast(adjMoveVector, out RaycastHit hit))
			{
				Vector3 reflected = Vector3.Reflect(adjMoveVector, hit.normal);
				adjMoveVector += reflected;
				adjMoveVector = Vector3.ClampMagnitude(adjMoveVector, moveVector.magnitude);
			}
		}

		position += adjMoveVector;
	}

	protected void InputMove(Vector3 moveVector, float deltaTime)
	{
		if (climbing)
			return;

		Vector3 dirInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
		dirInput = Vector3.ClampMagnitude(dirInput, 1);

		if (dirInput.sqrMagnitude > 0.01f)
		{
			// Guess where player will be after this move. Is it over a climbable obstacle?
			if (Physics.Raycast(new Ray(position + transform.rotation * climbOffset + raycastMargin * Vector3.up, Vector3.down), out RaycastHit hit, maxClimbable, rayMask))
			{
				climbing = true;
				climbingTimer.maxTime = (grounded ? fastClimbTime : slowClimbTime);
				climbingTimer.Reset();

				position = hit.point + Vector3.up * (height / 2f + 0.02f);
			}
		}

		// Collision check
		Vector3 adjMoveVector = moveVector;
		if (moveVector != Vector3.zero)
		{
			if (BoxCast(adjMoveVector, out RaycastHit hit))
			{
				Vector3 reflected = Vector3.Reflect(adjMoveVector, hit.normal);
				adjMoveVector += reflected;
				adjMoveVector = Vector3.ClampMagnitude(adjMoveVector, moveVector.magnitude);
			}
		}
		if (moveVector != Vector3.zero)
		{
			if (BoxCast(adjMoveVector, out RaycastHit hit))
			{
				Vector3 reflected = Vector3.Reflect(adjMoveVector, hit.normal);
				adjMoveVector += reflected;
				adjMoveVector = Vector3.ClampMagnitude(adjMoveVector, moveVector.magnitude);
			}
		}
		if (moveVector != Vector3.zero)
		{
			if (BoxCast(adjMoveVector, out RaycastHit hit))
			{
				Vector3 reflected = Vector3.Reflect(adjMoveVector, hit.normal);
				adjMoveVector += reflected;
				adjMoveVector = Vector3.ClampMagnitude(adjMoveVector, moveVector.magnitude);
			}
		}
		if (moveVector != Vector3.zero)
		{
			if (BoxCast(adjMoveVector, out RaycastHit hit))
			{
				Vector3 reflected = Vector3.Reflect(adjMoveVector, hit.normal);
				adjMoveVector += reflected;
				adjMoveVector = Vector3.ClampMagnitude(adjMoveVector, moveVector.magnitude);
			}
		}

		if (grounded)
		{
			// Guess where player will be after this move. Is it over a fall?
			if (Physics.Raycast(new Ray(position + adjMoveVector + feetOffset + raycastMargin * Vector3.up, Vector3.down), maxSafeFall, rayMask) ||
				Physics.Raycast(new Ray(position + adjMoveVector + Utils.SoftSign(inputVelocity.magnitude) * strideLength * inputVelocity.normalized + feetOffset + raycastMargin * Vector3.up, Vector3.down), maxSafeFall, rayMask))
			{
				atLedge = false;
			}
			else
			{
				atLedge = true;
			}
		}
		else
		{
			atLedge = false;
		}

		if (atLedge)
		{
			headPosition = Vector3.Lerp(headPosition, dirInput * peekDistance + headOffset, deltaTime * 2);

			return;
		}
		else
		{
			headPosition = Vector3.Lerp(headPosition, headOffset, deltaTime * 4);
		}

		position += adjMoveVector;
	}

	private bool BoxCast(Vector3 moveVector, out RaycastHit hit)
	{
		Debug.DrawLine(position + Vector3.up * ((height / 2) - radius), position - Vector3.up * ((height / 2) - radius), PhysicsManager.Instance.randomColor, 2);
		Debug.DrawLine(moveVector + position + Vector3.up * ((height / 2) - radius), moveVector + position - Vector3.up * ((height / 2) - radius), PhysicsManager.Instance.randomColor, 2);

		//return Physics.CapsuleCast(
		//	position + Vector3.up * ((height / 2) - radius),
		//	position - Vector3.up * ((height / 2) - radius),
		//	radius, moveVector.normalized, out hit, moveVector.magnitude, rayMask
		//);

		return Physics.BoxCast(
			position,
			new Vector3(radius, height / 2, radius),
			moveVector.normalized, out hit, Quaternion.identity, moveVector.magnitude, rayMask
		);
	}
}
