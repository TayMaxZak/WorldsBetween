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

	[SerializeField]
	private float maxSafeFall = 3.5f;
	[SerializeField]
	private float maxClimbable = 0.6f;
	[SerializeField]
	private float raycastMargin = 0.04f;
	[SerializeField]
	private float strideLength = 0.7f;

	[SerializeField]
	private float mouseSens = 150;
	[SerializeField]
	private float walkSpeed = 2.5f;
	[SerializeField]
	private float peekDistance = 0.4f;
	[SerializeField]
	private float jumpSpeed = 0.24f;

	public float airResistance = 0.05f;

	private float minMouseV = -85;
	private float maxMouseV = 85;
	private float mouseH = 0;
	private float mouseV = 0;

	public bool grounded = false;
	public bool atLedge = false;
	public bool jump = false;
	public bool climbing = false;

	private Timer climbingTimer = new Timer(1);
	[SerializeField]
	private float fastClimbTime = 0.55f;
	[SerializeField]
	private float slowClimbTime = 1.1f;

	private Vector3 initPos;

	protected override void Awake()
	{
		base.Awake();

		mouseH = transform.eulerAngles.y;
		mouseV = head.transform.eulerAngles.x;
	}

	public override void Init()
	{
		base.Init();

		headPosition = head.localPosition;
		prevHeadPosition = new Vector3(headPosition.x, headPosition.y, headPosition.z);

		initPos = position;
	}

	public override void UpdateTick(bool isPhysicsTick, float tickDeltaTime, float tickPartialTime)
	{
		if (!didInit)
			return;

		Cursor.lockState = CursorLockMode.Confined;
		//Cursor.visible = false;

		if (Input.GetButtonDown("Restart"))
		{
			position = initPos;

			velocity = Vector3.zero;
			fallVelocity = Vector3.zero;
			inputVelocity = Vector3.zero;
			climbing = false;
		}

		MouseLookInput();

		if (!climbing)
		{
			DirectionalInput();

			SpacebarInput();

			// Update prev position for lerping
			if (isPhysicsTick)
				prevHeadPosition = new Vector3(headPosition.x, headPosition.y, headPosition.z);

			base.UpdateTick(isPhysicsTick, tickDeltaTime, tickPartialTime);

			// Lerp logic position and visual position
			head.localPosition = Vector3.Lerp(prevHeadPosition, headPosition, 1 - (tickPartialTime / tickDeltaTime));
		}
		else
		{
			if (isPhysicsTick)
				climbingTimer.Increment(tickDeltaTime);

			float climbDeltaTime = 1;
			float climbPartialTime = (climbingTimer.currentTime + tickPartialTime) / climbingTimer.maxTime;
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

				base.UpdateTick(true, tickDeltaTime, 0);

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
		mouseH += Input.GetAxis("Mouse X") * mouseSens * Time.deltaTime;
		mouseV -= Input.GetAxis("Mouse Y") * mouseSens * Time.deltaTime;

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

		inputVelocity = Vector3.Lerp(inputVelocity, dirInput * walkSpeed, Time.deltaTime * 8);
	}

	private void SpacebarInput()
	{
		if (!grounded || climbing)
			return;

		if (Input.GetButtonDown("Jump"))
		{
			jump = true;
		}
	}

	public override void PhysicsTick(float deltaTime)
	{
		// Falling acceleration
		Vector3 fallAccel = PhysicsManager.Instance.gravity * deltaTime;

		fallVelocity += fallAccel;

		// Move player and check if grounded
		FallMove(fallVelocity * deltaTime, deltaTime);

		// Drag
		fallVelocity *= (1 - deltaTime * airResistance);

		// Apply jump
		if (jump)
		{
			jump = false;
			velocity += Vector3.up * jumpSpeed;
		}

		// Move player from jump and other verlocity sources
		Move(velocity);

		// Drag
		velocity *= (1 - deltaTime * airResistance);

		// Walk and run input
		InputMove(inputVelocity * deltaTime, deltaTime);
	}

	protected void FallMove(Vector3 moveVector, float deltaTime)
	{
		bool raycast = Physics.Raycast(new Ray(position + feetOffset + raycastMargin * Vector3.up, Vector3.down), out RaycastHit hit1, moveVector.magnitude, rayMask);
		bool raycast2 = Physics.Raycast(new Ray(position + feetOffset + Utils.SoftSign(inputVelocity.magnitude) * strideLength * inputVelocity.normalized + raycastMargin * Vector3.up, Vector3.down), out RaycastHit hit2, moveVector.magnitude, rayMask);

		// Raycast from center of body
		if (raycast || raycast2)
		{
			float fallPosChange = Mathf.Min(hit1.Equals(default(RaycastHit)) ? moveVector.magnitude : hit1.distance, hit2.Equals(default(RaycastHit)) ? moveVector.magnitude : hit2.distance);

			if (!climbing)
				position += Vector3.down * Mathf.Max(0, fallPosChange - raycastMargin);

			fallVelocity = Vector3.zero;
			velocity.y = 0;
			grounded = true;

			Debug.DrawLine(position, hit1.point, Utils.colorBlue, 3);
			Debug.DrawLine(position + Utils.SoftSign(inputVelocity.magnitude) * strideLength * inputVelocity.normalized, hit2.point, Utils.colorBlue, 3);
		}
		else
		{
			if (!climbing)
				position += moveVector;

			grounded = false;
		}
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

				position = hit.point + Vector3.up * height / 2f;
			}
		}

		// Collision check
		Vector3 adjMoveVector = moveVector;
		if (moveVector != Vector3.zero)
		{
			if (Physics.CapsuleCast(
				position + Vector3.up * ((height / 2) - radius),
				position - Vector3.up * ((height / 2) - radius),
				radius, moveVector.normalized, out RaycastHit capsuleHit, moveVector.magnitude)
			)
			{
				Vector3 reflected = Vector3.Reflect(moveVector, capsuleHit.normal);
				adjMoveVector += reflected;
				adjMoveVector = Vector3.ClampMagnitude(adjMoveVector, moveVector.magnitude);

				//Debug.DrawLine(new Vector3(position.x, capsuleHit.point.y, position.z), capsuleHit.point, Random.value > 0.6f ? Utils.colorCyan : Utils.colorBlue, 12);
			}
		}

		if (adjMoveVector != Vector3.zero)
		{
			if (Physics.CapsuleCast(
				position + Vector3.up * ((height / 2) - radius),
				position - Vector3.up * ((height / 2) - radius),
				radius, adjMoveVector.normalized, out RaycastHit capsuleHit, adjMoveVector.magnitude)
			)
			{
				Vector3 reflected = Vector3.Reflect(adjMoveVector, capsuleHit.normal);
				float mag = adjMoveVector.magnitude;
				adjMoveVector += reflected;
				adjMoveVector = Vector3.ClampMagnitude(adjMoveVector, mag);

				//Debug.DrawLine(new Vector3(position.x, capsuleHit.point.y, position.z), capsuleHit.point, Random.value > 0.6f ? Utils.colorCyan : Utils.colorBlue, 12);
			}
		}

		if (adjMoveVector != Vector3.zero)
		{
			if (Physics.CapsuleCast(
				position + Vector3.up * ((height / 2) - radius),
				position - Vector3.up * ((height / 2) - radius),
				radius, adjMoveVector.normalized, out RaycastHit capsuleHit, adjMoveVector.magnitude)
			)
			{
				Vector3 reflected = Vector3.Reflect(adjMoveVector, capsuleHit.normal);
				float mag = adjMoveVector.magnitude;
				adjMoveVector += reflected;
				adjMoveVector = Vector3.ClampMagnitude(adjMoveVector, mag);

				//Debug.DrawLine(new Vector3(position.x, capsuleHit.point.y, position.z), capsuleHit.point, Random.value > 0.6f ? Utils.colorCyan : Utils.colorBlue, 12);
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
}
