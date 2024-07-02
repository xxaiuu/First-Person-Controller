using Godot;
using System;

public partial class player : CharacterBody3D
{
	// Movement related Variables
	public const float WalkSpeed = 5.0f;
	public const float SprintSpeed = 8.0f;
	public const float JumpVelocity = 4.5f;
	public const float Sensitivity = 0.01f;	//look sensitivity

	// Bob Variables
	public const float BobFrequency = 2.0f;	//how often to bob the head
	public const float BobAmp = 0.08f;	//how far up and down the camera goes
	private float t_bob = 0.0f;	//how far along the sine wave (bobbing motion)

	// FOV Variables
	public float Base_FOV = 75.0f;
	private float FOV_Change = 1.5f;	//multiple our speed and add to our base FOV

	// Private Variables
	private bool use_bob = true;
	private bool use_fov = true;
	private Node3D head;
	private Camera3D camera;
	private float speed;


	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();


	//Hides mouse once loaded in
	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Hidden;
		head = GetNode<Node3D>("Head"); //$Head
		camera = GetNode<Camera3D>("Head/Camera3D"); //$Head/Camera3D
	}


	public override void _UnhandledInput(InputEvent @event)
	{

		//base._UnhandledInput(@event);

		if(@event is InputEventMouseMotion mouseMotion){
			head.RotateY(-mouseMotion.Relative.X * Sensitivity);	//due to the translation between mouse and camera
			
			if(use_bob)
				RotateY(-mouseMotion.Relative.X * Sensitivity);	//rotates whole	
			
			camera.RotateX(-mouseMotion.Relative.Y * Sensitivity);

			if(camera != null){
				float minAngleDegrees = -40f;
				float maxAngleDegrees = 60f;
				float deltaX = -mouseMotion.Relative.Y * Sensitivity; // Invert deltaY to match typical FPS controls

				Vector3 currentRotation = camera.RotationDegrees;
				float newRotationX = currentRotation.X + deltaX;
				float clampedX = Mathf.Clamp(newRotationX, minAngleDegrees, maxAngleDegrees);
				camera.RotationDegrees = new Vector3(clampedX, currentRotation.Y, currentRotation.Z);
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		if(Input.IsActionJustPressed("EXIT")){
			Input.MouseMode = Input.MouseModeEnum.Visible;	
			
		}

		// Add the gravity.
		if (!IsOnFloor())
			velocity.Y -= gravity * (float)delta;

		// Handle Jump.
		if (Input.IsActionJustPressed("jump") && IsOnFloor())
			velocity.Y = JumpVelocity;

		// Handle speed
		if (Input.IsActionPressed("sprint"))
			speed = SprintSpeed;
		else
			speed = WalkSpeed;

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 inputDir = Input.GetVector("left", "right", "up", "down");
		Vector3 direction = (head.Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

		if (IsOnFloor()){
			if (direction != Vector3.Zero)
			{
				velocity.X = direction.X * speed;
				velocity.Z = direction.Z * speed;
			}
			else
			{
				//You will glide a little, to do a complete stop instantly, change these to 0
				velocity.X = Mathf.Lerp(velocity.X, direction.X * speed, (float)(delta * 7.0));	
				velocity.Z = Mathf.Lerp(velocity.Z, direction.Z * speed, (float)(delta * 7.0));
			}
		}
		else {
			//initial velocity, target velocity, decimal percentage between the two variables we want to cover in each step
			velocity.X = Mathf.Lerp(velocity.X, direction.X * speed, (float)(delta * 2.0));	//delta usually 0.05 so this is actually 10%
			velocity.Z = Mathf.Lerp(velocity.Z, direction.Z * speed, (float)(delta * 2.0));
		}



		// NOTE: looking up and down no longer works when using head bob (which makes sense, can't bob head while looking at the sky can we?)
		//			disable bob code for exploration type first person
		//			Head Bob is more suited for a linear path running - realistic minus not looking up
						// time 	*	speed
		if(use_bob){
			t_bob += (float)delta * (float)velocity.Length() * (IsOnFloor() ? 1f : 0f);
			UpdateCameraPosition(t_bob);
		}

		// FOV
		if (use_fov){
			var velocity_clamp = Mathf.Clamp(velocity.Length(), 0.5, SprintSpeed * 2.0f);
			var target_fov = Base_FOV + FOV_Change * velocity_clamp;	//clamp such that our FOV doesn't go too crazy

			camera.Fov = Mathf.Lerp(camera.Fov, (float)target_fov, (float)delta * 8.0f);
		}


		Velocity = velocity;
		MoveAndSlide();

	}


	private Vector3 HeadBobOffset(float time)
    {
        Vector3 pos = Vector3.Zero;
        pos.Y = Mathf.Sin(time * BobFrequency) * BobAmp;
		pos.X = Mathf.Cos(time * BobFrequency/2) * BobAmp;	//I can't really tell the difference 
        return pos;
    }

	
    private void UpdateCameraPosition(float time)
    {
        Vector3 headBobOffset = HeadBobOffset(time);

        // Set the camera's position relative to the character
        Vector3 characterPosition = head.GlobalTransform.Origin; // Get character's current position
		camera.GlobalTransform = new Transform3D(Basis, characterPosition + headBobOffset);
	}

}
