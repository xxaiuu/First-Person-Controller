using Godot;
using System;

public partial class player : CharacterBody3D
{
	public const float Speed = 5.0f;
	public const float JumpVelocity = 4.5f;
	public const float Sensitivity = 0.01f;	//look sensitivity

	public const float BobFrequency = 2.0f;	//how often to bob the head
	public const float BobAmp = 0.08f;	//how far up and down the camera goes
	
	private float t_bob = 0.0f;	//how far along the sine wave (bobbing motion)
	private Node3D head;
	private Camera3D camera;
	
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
			RotateY(-mouseMotion.Relative.X * Sensitivity);	//rotates whole
			
			camera.RotateX(-mouseMotion.Relative.Y * Sensitivity);

			if(camera != null){
				float minAngleDegrees = -40f;
				float maxAngleDegrees = 60f;
				float deltaX = -mouseMotion.Relative.Y * Sensitivity; // Invert deltaY to match typical FPS controls

				Vector3 currentRotation = camera.RotationDegrees;
				float newRotationX = currentRotation.X + deltaX;
				float clampedX = Math.Clamp(newRotationX, minAngleDegrees, maxAngleDegrees);
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

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 inputDir = Input.GetVector("left", "right", "up", "down");
		Vector3 direction = (head.Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;
		}
		else
		{
			velocity.X = 0; 
			velocity.Z = 0;
		}

		
						// time 	*	speed
		t_bob += (float)delta * (float)velocity.Length() * (IsOnFloor() ? 1f : 0f);
		UpdateCameraPosition(t_bob);

		Velocity = velocity;
		MoveAndSlide();

	}


	private Vector3 HeadBobOffset(float time)
    {
        Vector3 pos = Vector3.Zero;
        pos.Y = Mathf.Sin(time * BobFrequency) * BobAmp;
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
