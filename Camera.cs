using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Keys = OpenTK.Windowing.GraphicsLibraryFramework.Keys;
public class Camera
{
	public Vector3 Position;
	public float Pitch;
	public float Yaw;

	public const float OriginalSpeed = 7f;
	public float SprintSpeed = OriginalSpeed * 3;
	public float Speed = 7f;
	public float Sensitivity = 0.2f;

	private Vector2 _lastMousePos;
	private bool _firstMove = true;

	public Camera(Vector3 position, float yaw = -90f, float pitch = 0f)
	{
		Position = position;
		Yaw = yaw;
		Pitch = pitch;
	}

	public Matrix4 GetViewMatrix()
	{
		Vector3 front = GetFront();
		return Matrix4.LookAt(Position, Position + front, Vector3.UnitY);
	}

	bool lastMouseGrabbedToggle = false;
	public void UpdateInput(KeyboardState input, MouseState mouse, float deltaTime, bool mouseGrabbedToggle)
	{
		Vector3 front = GetFront();
		Vector3 right = Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY));
		Vector3 up = Vector3.Normalize(Vector3.Cross(right, front));

		if (input.IsKeyDown(Keys.LeftShift))
			Speed = SprintSpeed;
		else Speed = OriginalSpeed;

		if (input.IsKeyDown(Keys.W))
			Position += front * Speed * deltaTime;
		if (input.IsKeyDown(Keys.S))
			Position -= front * Speed * deltaTime;
		if (input.IsKeyDown(Keys.A))
			Position -= right * Speed * deltaTime;
		if (input.IsKeyDown(Keys.D))
			Position += right * Speed * deltaTime;
		if (input.IsKeyDown(Keys.Space))
			Position += up * Speed * deltaTime;
		if (input.IsKeyDown(Keys.LeftControl))
			Position -= up * Speed * deltaTime;

		if (lastMouseGrabbedToggle != mouseGrabbedToggle) _lastMousePos = mouse.Position;
		if(mouseGrabbedToggle)
		{
			if (_firstMove)
			{
				_lastMousePos = mouse.Position;
				_firstMove = false;
			}
			else
			{
				var delta = mouse.Position - _lastMousePos;
				_lastMousePos = mouse.Position;

				Yaw += delta.X * Sensitivity;
				Pitch -= delta.Y * Sensitivity;

				Pitch = MathHelper.Clamp(Pitch, -89f, 89f);
			}
		}
		lastMouseGrabbedToggle = mouseGrabbedToggle;
	}

	public Vector3 GetFront()
	{
		float yawRad = MathHelper.DegreesToRadians(Yaw);
		float pitchRad = MathHelper.DegreesToRadians(Pitch);

		return Vector3.Normalize(new Vector3(
			MathF.Cos(pitchRad) * MathF.Cos(yawRad),
			MathF.Sin(pitchRad),
			MathF.Cos(pitchRad) * MathF.Sin(yawRad)
		));
	}
}
