using Godot;

public enum PlayerState
{
    Standing,
    Jumping,
    Falling,
    Walking,
    Ducking,
    Hurting,
}

public partial class PlatformerPlayer : CharacterBody2D
{
    [Export]
    public AnimatedSprite2D animatedSprite;
    [Export]
    public CollisionShape2D collisionShape2D;
    [Export]
    public EmoBubble emoBubble;
    [Export]
    public Killer mobKiller;

    [Export(PropertyHint.Range, "100.0, 500.0")]
    float ACCELERATION = 280.0f;
    [Export(PropertyHint.Range, "10.0, 100.0")]
    float MAX_SPEED = 72.0f;
    [Export(PropertyHint.Range, "1.0, 20.0")]
    float FRICTION = 10.0f;
    [Export(PropertyHint.Range, "0.0, 5.0")]
    float AIR_RESISTANCE = 0.8f;
    [Export(PropertyHint.Range, "0.0, 500.0")]
    float JUMP_FORCE = 270.0f;
    [Export(PropertyHint.Range, "0.1, 2.0, 0.1")]
    float GRAVITY_SCALE = 1.0f;
    [Export(PropertyHint.Range, "100.0, 200.0")]
    float MAX_FALL_SPEED = 200.0f;

	[Export(PropertyHint.Range, "0.1, 10.0")]
	float INVINCIBLE_TIME = 2.0f;

    float x_input = 0.0f;
    bool jump_pressed = false;
    bool duck_pressed = false;
    bool is_invincible = false;
    Vector2 velocity = Vector2.Zero;

    // Get the gravity from the project settings to be synced with RigidBody nodes.
    float _gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

    private PlayerState currentState = PlayerState.Standing;

    public override void _Ready()
    {
        mobKiller.MobHurt += () => {
            // Stomp attack like mario
            Velocity = Velocity with {Y = -JUMP_FORCE};
            if (GD.Randf() < 0.1f)
            {
                emoBubble.PlayEmo(3);
            }
        };
    }

	public string GetStateString()
	{
		return currentState.ToString();
	}


    public void Hurt(float impactX = 100.0f, float impactY = 200.0f)
    {
        if (is_invincible) return;
        float dir = animatedSprite.FlipH ? 1.0f : -1.0f;
        Vector2 newVelocity = Velocity;
        newVelocity.X = dir * impactX;
        newVelocity.Y = -impactY;
        Velocity = newVelocity;
		
		CreateTween().TweenCallback(Callable.From(() => {
			currentState = PlayerState.Hurting;
		})).SetDelay(0.01);

        // Create invincible animation
		is_invincible = true;
		Tween tween = CreateTween().SetLoops();

        //Disable attack
        mobKiller.GetNode<CollisionShape2D>("CollisionShape2D").SetDeferred("disabled", true);

		tween.TweenCallback(Callable.From(() => {
			Modulate = Modulate with { A = 0.5f };
		})).SetDelay(0.05);

		tween.TweenCallback(Callable.From(() => {
			Modulate = Modulate with { A = 1.0f };
		})).SetDelay(0.05);

		CreateTween().TweenCallback(Callable.From(() => {
			is_invincible = false;
			Modulate = Modulate with { A = 1.0f };
			tween.Kill();
            //Force update collision
            Tween tween2 = CreateTween();
            tween2.TweenCallback(Callable.From(() => collisionShape2D.SetDeferred("disabled", true))).SetDelay(0.01);
            tween2.TweenCallback(Callable.From(() => collisionShape2D.SetDeferred("disabled", false))).SetDelay(0.01);
            //Enable attack
            tween2.TweenCallback(Callable.From(() => mobKiller.GetNode<CollisionShape2D>("CollisionShape2D").SetDeferred("disabled", false))).SetDelay(0.02);
            

		})).SetDelay(INVINCIBLE_TIME);

        emoBubble.PlayEmo(7);
    }

    public void UpdateState(float delta)
    {
        // Apply gravity
        velocity.Y = Mathf.Min(velocity.Y + _gravity * GRAVITY_SCALE * delta, MAX_FALL_SPEED);

        // Apply horizontal movement
        if (currentState != PlayerState.Hurting && currentState != PlayerState.Ducking)
        {
            velocity.X += (float)(x_input * ACCELERATION * delta);
            velocity.X = Mathf.Clamp(velocity.X, -MAX_SPEED, MAX_SPEED);
        }

		// Apply air resistance
        if (currentState == PlayerState.Jumping || currentState == PlayerState.Falling)
        {
            if (x_input == 0.0f)
            {
                velocity.X = Mathf.Lerp(velocity.X, 0.0f, (float)(AIR_RESISTANCE * delta));
            }
        }

		// Apply ground friction
		if (currentState == PlayerState.Ducking || currentState == PlayerState.Standing)
		{
			velocity.X = Mathf.Lerp(velocity.X, 0.0f, (float)(FRICTION * delta));
		}

        switch (currentState)
        {
            case PlayerState.Standing:
                if (IsOnFloor())
                {
                    if (x_input != 0.0f)
                    {
                        currentState = PlayerState.Walking;
                    }
                }
                break;
            case PlayerState.Walking:
                if (IsOnFloor())
                {
                    if (x_input == 0.0f)
                    {
                        currentState = PlayerState.Standing;
                    }
                }
                else
                {
                    currentState = PlayerState.Falling;
                }
                break;
            case PlayerState.Jumping:
                if (velocity.Y > 0.0f)
                {
                    currentState = PlayerState.Falling;
                }
                if (!jump_pressed && velocity.Y < -JUMP_FORCE / 2.0f)
                {
                    velocity.Y = -JUMP_FORCE * 0.5f;
					currentState = PlayerState.Falling;
                }
                break;
            case PlayerState.Falling:
                if (IsOnFloor())
                {
                    currentState = PlayerState.Standing;
                }
                break;
            case PlayerState.Hurting:
                if (IsOnFloor())
				{
					currentState = PlayerState.Standing;
				}
				break;

			case PlayerState.Ducking:
				if (!duck_pressed)
				{
					currentState = PlayerState.Standing;
				}
				break;
        }

        if (currentState == PlayerState.Standing || currentState == PlayerState.Walking)
        {
            if (IsOnFloor())
            {
                if (jump_pressed)
                {
                    currentState = PlayerState.Jumping;
                    velocity.Y = -JUMP_FORCE;
                }
				if (duck_pressed)
				{
					currentState = PlayerState.Ducking;
				}
            }
        }
    }

    public void UpdateSprite()
    {
        if (velocity.X > 0.0f) animatedSprite.FlipH = false;
        else if (velocity.X < 0.0f) animatedSprite.FlipH = true;

        switch (currentState)
        {
            case PlayerState.Standing:
                animatedSprite.Play("standing");
                break;
            case PlayerState.Walking:
                animatedSprite.Play("walking");
                break;
            case PlayerState.Jumping:
                animatedSprite.Play("jumping");
                break;
            case PlayerState.Hurting:
                animatedSprite.Play("hurting");
                break;
            case PlayerState.Ducking:
                animatedSprite.Play("ducking");
                break;
			case PlayerState.Falling:
                if (velocity.Y > MAX_FALL_SPEED * 0.9)
				    animatedSprite.Play("falling");
				break;
        }
    }

    public void UpdateInput()
    {
        x_input = Input.GetActionStrength("right") - Input.GetActionStrength("left");
        jump_pressed = Input.IsActionPressed("up");
        duck_pressed = Input.IsActionPressed("down");
    }

    public override void _PhysicsProcess(double delta)
    {
        // Get the input
        UpdateInput();

        velocity = Velocity;

        // Update the state machine
        UpdateState((float)delta);

        // Update the sprite
        UpdateSprite();

        Velocity = velocity;
        MoveAndSlide();
    }
}
