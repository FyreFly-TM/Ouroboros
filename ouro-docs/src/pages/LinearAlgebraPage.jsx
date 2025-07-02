import CodeBlock from '../components/CodeBlock'
import Callout from '../components/Callout'

export default function LinearAlgebraPage() {
  return (
    <div className="max-w-4xl">
      <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">
        Linear Algebra
      </h1>
      <p className="text-xl text-gray-600 dark:text-gray-300 mb-8">
        Built-in support for vectors, matrices, quaternions, and transforms. Perfect for game development, 
        3D graphics, physics simulations, and scientific computing.
      </p>

      <Callout type="info" title="Math Module">
        Import the math module with <code className="bg-gray-100 dark:bg-gray-700 px-2 py-1 rounded">using Ouroboros.StdLib.Math;</code> 
        to access linear algebra types and functions.
      </Callout>

      {/* Vectors */}
      <section className="mb-12">
        <h2 id="vector" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Vectors
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Mathematical vectors for 2D, 3D, and 4D operations with full operator support.
        </p>
        <CodeBlock
          code={`// Creating vectors
var v2d = new Vector2(3, 4);
var v3d = new Vector3(1, 2, 3);
var v4d = new Vector4(1, 0, 0, 1);

// Vector from array
var fromArray = new Vector3(new float[] { 1.0f, 2.0f, 3.0f });

// Common vectors
var zero = Vector3.Zero;      // (0, 0, 0)
var one = Vector3.One;        // (1, 1, 1)
var up = Vector3.Up;          // (0, 1, 0)
var right = Vector3.Right;    // (1, 0, 0)
var forward = Vector3.Forward; // (0, 0, 1)

// Component access
float x = v3d.X;
float y = v3d.Y;
float z = v3d.Z;

// Vector arithmetic
var v1 = new Vector3(1, 2, 3);
var v2 = new Vector3(4, 5, 6);

var sum = v1 + v2;            // (5, 7, 9)
var diff = v1 - v2;           // (-3, -3, -3)
var scaled = v1 * 2.0f;       // (2, 4, 6)
var divided = v1 / 2.0f;      // (0.5, 1, 1.5)
var negated = -v1;            // (-1, -2, -3)

// Dot product
float dot = v1.Dot(v2);       // 1*4 + 2*5 + 3*6 = 32
float dot2 = Vector3.Dot(v1, v2);  // Same result

// Cross product (3D only)
var cross = v1.Cross(v2);     // (-3, 6, -3)
var cross2 = Vector3.Cross(v1, v2);

// Vector length and normalization
float length = v1.Length;     // √14 ≈ 3.74
float lengthSquared = v1.LengthSquared;  // 14 (faster)
var normalized = v1.Normalized;  // Unit vector
v1.Normalize();  // Normalize in place

// Distance between points
var point1 = new Vector3(0, 0, 0);
var point2 = new Vector3(3, 4, 0);
float distance = Vector3.Distance(point1, point2);  // 5
float distSquared = Vector3.DistanceSquared(point1, point2);  // 25

// Angle between vectors
float angle = Vector3.Angle(v1, v2);  // In radians
float degrees = angle * (180 / Math.PI);

// Vector projection
var projection = v1.ProjectOnto(v2);
var rejection = v1 - projection;  // Perpendicular component

// Reflection
var normal = new Vector3(0, 1, 0);
var incident = new Vector3(1, -1, 0);
var reflected = Vector3.Reflect(incident, normal);  // (1, 1, 0)

// Interpolation
var start = new Vector3(0, 0, 0);
var end = new Vector3(10, 10, 10);
var mid = Vector3.Lerp(start, end, 0.5f);  // (5, 5, 5)
var smooth = Vector3.SmoothStep(start, end, 0.5f);

// Component-wise operations
var abs = Vector3.Abs(new Vector3(-1, -2, -3));  // (1, 2, 3)
var min = Vector3.Min(v1, v2);  // Component-wise minimum
var max = Vector3.Max(v1, v2);  // Component-wise maximum
var clamped = Vector3.Clamp(v1, min, max);

// Swizzling (rearranging components)
var v = new Vector3(1, 2, 3);
var swizzled = new Vector3(v.Z, v.X, v.Y);  // (3, 1, 2)

// Practical examples
// Movement with velocity
Vector3 position = new Vector3(0, 0, 0);
Vector3 velocity = new Vector3(1, 0, 0);
float deltaTime = 0.016f;  // 60 FPS
position += velocity * deltaTime;

// Direction to target
Vector3 playerPos = new Vector3(0, 0, 0);
Vector3 enemyPos = new Vector3(10, 0, 5);
Vector3 direction = (enemyPos - playerPos).Normalized;
float distanceToEnemy = Vector3.Distance(playerPos, enemyPos);

// Circular motion
float angle = time * rotationSpeed;
Vector3 circularPos = new Vector3(
    radius * Cos(angle),
    0,
    radius * Sin(angle)
);`}
        />

        <h3 className="text-xl font-semibold text-gray-800 dark:text-gray-200 mb-3 mt-6">
          Advanced Vector Operations
        </h3>
        <CodeBlock
          code={`// Spherical coordinates
public static Vector3 FromSpherical(float radius, float theta, float phi)
{
    return new Vector3(
        radius * Sin(phi) * Cos(theta),
        radius * Cos(phi),
        radius * Sin(phi) * Sin(theta)
    );
}

// Perpendicular vector
public static Vector3 GetPerpendicular(Vector3 v)
{
    // Use cross product with an arbitrary vector
    Vector3 arbitrary = Math.Abs(v.X) < 0.9f ? Vector3.Right : Vector3.Up;
    return v.Cross(arbitrary).Normalized;
}

// Rotation around axis
public static Vector3 RotateAroundAxis(Vector3 v, Vector3 axis, float angle)
{
    float cos = Cos(angle);
    float sin = Sin(angle);
    Vector3 cross = axis.Cross(v);
    float dot = axis.Dot(v);
    
    return v * cos + cross * sin + axis * dot * (1 - cos);
}

// Barycentric coordinates
public static Vector3 Barycentric(Vector3 a, Vector3 b, Vector3 c, float u, float v)
{
    return a + u * (b - a) + v * (c - a);
}

// Catmull-Rom spline interpolation
public static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
{
    float t2 = t * t;
    float t3 = t2 * t;
    
    return 0.5f * (
        2.0f * p1 +
        (-p0 + p2) * t +
        (2.0f * p0 - 5.0f * p1 + 4.0f * p2 - p3) * t2 +
        (-p0 + 3.0f * p1 - 3.0f * p2 + p3) * t3
    );
}

// Vector field sampling
public class VectorField
{
    public Vector3 Sample(Vector3 position)
    {
        // Example: Circular field
        return new Vector3(-position.Z, 0, position.X).Normalized;
    }
    
    public List<Vector3> TracePath(Vector3 start, float stepSize, int steps)
    {
        var path = new List<Vector3> { start };
        Vector3 current = start;
        
        for (int i = 0; i < steps; i++)
        {
            Vector3 direction = Sample(current);
            current += direction * stepSize;
            path.Add(current);
        }
        
        return path;
    }
}`}
        />
      </section>

      {/* Matrices */}
      <section className="mb-12">
        <h2 id="matrix" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Matrices
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Matrix operations for transformations, linear systems, and more.
        </p>
        <CodeBlock
          code={`// Creating matrices
var identity = Matrix4x4.Identity;
var zero = Matrix4x4.Zero;

// Matrix from values (row-major order)
var m = new Matrix4x4(
    1, 0, 0, 0,
    0, 1, 0, 0,
    0, 0, 1, 0,
    0, 0, 0, 1
);

// Matrix from vectors (column vectors)
var mat = new Matrix4x4(
    new Vector4(1, 0, 0, 0),  // Column 0
    new Vector4(0, 1, 0, 0),  // Column 1
    new Vector4(0, 0, 1, 0),  // Column 2
    new Vector4(0, 0, 0, 1)   // Column 3
);

// Element access
float m11 = m[0, 0];  // Row 0, Column 0
float m23 = m[1, 2];  // Row 1, Column 2

// Matrix arithmetic
var m1 = Matrix4x4.Identity;
var m2 = Matrix4x4.CreateScale(2, 2, 2);

var sum = m1 + m2;
var diff = m1 - m2;
var scaled = m1 * 2.0f;
var product = m1 * m2;  // Matrix multiplication

// Vector transformation
var point = new Vector3(1, 0, 0);
var transformed = m * point;  // Apply transformation

// Transformation matrices
var translation = Matrix4x4.CreateTranslation(10, 5, 0);
var rotation = Matrix4x4.CreateRotationY(Math.PI / 4);  // 45 degrees
var scale = Matrix4x4.CreateScale(2, 2, 2);

// Combine transformations (order matters!)
var transform = scale * rotation * translation;  // TRS order

// More rotation options
var rotX = Matrix4x4.CreateRotationX(angle);
var rotZ = Matrix4x4.CreateRotationZ(angle);
var rotAxis = Matrix4x4.CreateFromAxisAngle(axis, angle);
var rotQuat = Matrix4x4.CreateFromQuaternion(quaternion);

// Look-at matrix (view matrix)
var eye = new Vector3(0, 10, 10);
var target = new Vector3(0, 0, 0);
var up = Vector3.Up;
var viewMatrix = Matrix4x4.CreateLookAt(eye, target, up);

// Projection matrices
var perspectiveMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
    Math.PI / 4,  // 45 degree FOV
    aspectRatio,
    0.1f,         // Near plane
    100.0f        // Far plane
);

var orthoMatrix = Matrix4x4.CreateOrthographic(
    width, height,
    0.1f, 100.0f
);

// Matrix operations
var transposed = m.Transpose();
var determinant = m.Determinant;
var inverse = m.Inverse;  // Throws if not invertible
bool success = Matrix4x4.TryInvert(m, out var inverted);

// Decompose transformation
if (transform.Decompose(out Vector3 scale, out Quaternion rotation, out Vector3 translation))
{
    Console.WriteLine($"Scale: {scale}");
    Console.WriteLine($"Rotation: {rotation}");
    Console.WriteLine($"Translation: {translation}");
}

// Matrix interpolation
var start = Matrix4x4.CreateTranslation(0, 0, 0);
var end = Matrix4x4.CreateTranslation(10, 0, 0);
var interpolated = Matrix4x4.Lerp(start, end, 0.5f);

// 3x3 matrices for 2D or normal transformations
var mat3 = new Matrix3x3(
    1, 0, 0,
    0, 1, 0,
    0, 0, 1
);

// Extract 3x3 from 4x4 (remove translation)
var rotationOnly = new Matrix3x3(m);`}
        />

        <h3 className="text-xl font-semibold text-gray-800 dark:text-gray-200 mb-3 mt-6">
          Matrix Applications
        </h3>
        <CodeBlock
          code={`// Transform pipeline
public class TransformPipeline
{
    public Matrix4x4 Model { get; set; }
    public Matrix4x4 View { get; set; }
    public Matrix4x4 Projection { get; set; }
    
    public Matrix4x4 MVP => Model * View * Projection;
    
    public Vector3 WorldToScreen(Vector3 worldPos, int screenWidth, int screenHeight)
    {
        // Transform to clip space
        Vector4 clipPos = MVP * new Vector4(worldPos, 1.0f);
        
        // Perspective divide
        Vector3 ndcPos = new Vector3(clipPos.X, clipPos.Y, clipPos.Z) / clipPos.W;
        
        // Convert to screen coordinates
        float x = (ndcPos.X + 1.0f) * 0.5f * screenWidth;
        float y = (1.0f - ndcPos.Y) * 0.5f * screenHeight;
        float z = ndcPos.Z;  // Depth for z-buffer
        
        return new Vector3(x, y, z);
    }
}

// Solving linear systems
public static Vector3 SolveLinearSystem3x3(Matrix3x3 A, Vector3 b)
{
    // Solve Ax = b using Cramer's rule
    float det = A.Determinant;
    if (Math.Abs(det) < 0.0001f)
        throw new InvalidOperationException("Singular matrix");
    
    // Replace columns with b vector
    var A1 = new Matrix3x3(b, A.Column1, A.Column2);
    var A2 = new Matrix3x3(A.Column0, b, A.Column2);
    var A3 = new Matrix3x3(A.Column0, A.Column1, b);
    
    return new Vector3(
        A1.Determinant / det,
        A2.Determinant / det,
        A3.Determinant / det
    );
}

// Affine transformations
public class Transform2D
{
    private Matrix3x3 matrix = Matrix3x3.Identity;
    
    public void Translate(float x, float y)
    {
        var t = new Matrix3x3(
            1, 0, x,
            0, 1, y,
            0, 0, 1
        );
        matrix = matrix * t;
    }
    
    public void Rotate(float angle)
    {
        float cos = Cos(angle);
        float sin = Sin(angle);
        var r = new Matrix3x3(
            cos, -sin, 0,
            sin,  cos, 0,
            0,    0,   1
        );
        matrix = matrix * r;
    }
    
    public void Scale(float sx, float sy)
    {
        var s = new Matrix3x3(
            sx, 0,  0,
            0,  sy, 0,
            0,  0,  1
        );
        matrix = matrix * s;
    }
    
    public Vector2 Transform(Vector2 point)
    {
        var p = new Vector3(point.X, point.Y, 1);
        var result = matrix * p;
        return new Vector2(result.X, result.Y);
    }
}`}
        />
      </section>

      {/* Quaternions */}
      <section className="mb-12">
        <h2 id="quaternion" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Quaternions
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Efficient representation of 3D rotations without gimbal lock.
        </p>
        <CodeBlock
          code={`// Creating quaternions
var identity = Quaternion.Identity;  // No rotation
var q1 = new Quaternion(0, 0, 0, 1);  // Same as identity (x, y, z, w)

// From Euler angles (pitch, yaw, roll)
var euler = Quaternion.CreateFromYawPitchRoll(
    yaw: Math.PI / 4,      // 45° around Y
    pitch: Math.PI / 6,    // 30° around X
    roll: 0                // 0° around Z
);

// From axis and angle
var axis = new Vector3(0, 1, 0);  // Y-axis
var angle = Math.PI / 2;          // 90 degrees
var q2 = Quaternion.CreateFromAxisAngle(axis, angle);

// From rotation matrix
var rotMatrix = Matrix4x4.CreateRotationY(Math.PI / 4);
var q3 = Quaternion.CreateFromRotationMatrix(rotMatrix);

// From two vectors (shortest rotation)
var from = new Vector3(1, 0, 0);
var to = new Vector3(0, 1, 0);
var q4 = Quaternion.FromToRotation(from, to);

// Quaternion properties
float w = q1.W;  // Scalar part
float x = q1.X;  // Vector part X
float y = q1.Y;  // Vector part Y
float z = q1.Z;  // Vector part Z
float length = q1.Length();
bool isIdentity = q1.IsIdentity;

// Quaternion operations
var q5 = q1 * q2;  // Combine rotations (q2 then q1)
var inverse = q1.Conjugate();  // For unit quaternions
var normalized = q1.Normalized;

// Rotate vectors
var point = new Vector3(1, 0, 0);
var rotated = q2 * point;  // Rotate point by quaternion

// Alternative rotation method
var rotated2 = Vector3.Transform(point, q2);

// Interpolation
var start = Quaternion.Identity;
var end = Quaternion.CreateFromAxisAngle(Vector3.Up, Math.PI);
var mid = Quaternion.Lerp(start, end, 0.5f);      // Linear
var smooth = Quaternion.Slerp(start, end, 0.5f);  // Spherical

// Extract axis and angle
q2.ToAxisAngle(out Vector3 extractedAxis, out float extractedAngle);

// Convert to Euler angles
var eulerAngles = q2.ToEulerAngles();  // Returns Vector3 (pitch, yaw, roll)

// Dot product (measure of similarity)
float dot = Quaternion.Dot(q1, q2);
bool similar = dot > 0.99f;  // Nearly same rotation

// Angle between rotations
float angleBetween = Quaternion.Angle(q1, q2);

// Look rotation (like Unity's Quaternion.LookRotation)
public static Quaternion LookRotation(Vector3 forward, Vector3 up)
{
    forward = forward.Normalized;
    Vector3 right = up.Cross(forward).Normalized;
    up = forward.Cross(right);
    
    var matrix = new Matrix4x4(
        right.X, right.Y, right.Z, 0,
        up.X, up.Y, up.Z, 0,
        forward.X, forward.Y, forward.Z, 0,
        0, 0, 0, 1
    );
    
    return Quaternion.CreateFromRotationMatrix(matrix);
}

// Practical rotation examples
// Smooth rotation towards target
public class SmoothRotation
{
    public Quaternion Current { get; private set; }
    public Quaternion Target { get; set; }
    public float Speed { get; set; } = 5.0f;
    
    public void Update(float deltaTime)
    {
        float t = 1.0f - Math.Exp(-Speed * deltaTime);
        Current = Quaternion.Slerp(Current, Target, t);
    }
}

// Constrained rotation
public static Quaternion ConstrainRotation(Quaternion rotation, float maxAngle)
{
    float angle = Quaternion.Angle(Quaternion.Identity, rotation);
    if (angle > maxAngle)
    {
        float t = maxAngle / angle;
        return Quaternion.Slerp(Quaternion.Identity, rotation, t);
    }
    return rotation;
}

// Swing-twist decomposition
public static void SwingTwistDecomposition(Quaternion rotation, Vector3 twistAxis,
    out Quaternion swing, out Quaternion twist)
{
    Vector3 r = new Vector3(rotation.X, rotation.Y, rotation.Z);
    Vector3 p = Vector3.Project(r, twistAxis);
    twist = new Quaternion(p.X, p.Y, p.Z, rotation.W).Normalized;
    swing = rotation * twist.Conjugate();
}`}
        />
      </section>

      {/* Transforms */}
      <section className="mb-12">
        <h2 id="transform" className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Transform Component
        </h2>
        <p className="text-gray-600 dark:text-gray-300 mb-4">
          Complete transformation representation with position, rotation, and scale.
        </p>
        <CodeBlock
          code={`// Transform class
public class Transform
{
    public Vector3 Position { get; set; } = Vector3.Zero;
    public Quaternion Rotation { get; set; } = Quaternion.Identity;
    public Vector3 Scale { get; set; } = Vector3.One;
    
    // Parent-child hierarchy
    public Transform Parent { get; set; }
    private List<Transform> children = new List<Transform>();
    
    // Local to world matrix
    public Matrix4x4 LocalToWorldMatrix
    {
        get
        {
            var t = Matrix4x4.CreateTranslation(Position);
            var r = Matrix4x4.CreateFromQuaternion(Rotation);
            var s = Matrix4x4.CreateScale(Scale);
            var local = s * r * t;  // Scale, then rotate, then translate
            
            if (Parent != null)
                return local * Parent.LocalToWorldMatrix;
            
            return local;
        }
    }
    
    // World to local matrix
    public Matrix4x4 WorldToLocalMatrix
    {
        get
        {
            Matrix4x4.Invert(LocalToWorldMatrix, out var inverse);
            return inverse;
        }
    }
    
    // Direction vectors
    public Vector3 Forward => Rotation * Vector3.Forward;
    public Vector3 Right => Rotation * Vector3.Right;
    public Vector3 Up => Rotation * Vector3.Up;
    
    // World position (considering parent)
    public Vector3 WorldPosition
    {
        get
        {
            if (Parent != null)
                return Vector3.Transform(Position, Parent.LocalToWorldMatrix);
            return Position;
        }
    }
    
    // Look at target
    public void LookAt(Vector3 target, Vector3 up)
    {
        Vector3 forward = (target - WorldPosition).Normalized;
        Rotation = LookRotation(forward, up);
    }
    
    // Translate
    public void Translate(Vector3 translation, Space space = Space.Self)
    {
        if (space == Space.Self)
        {
            Position += Rotation * translation;
        }
        else
        {
            Position += translation;
        }
    }
    
    // Rotate
    public void Rotate(Vector3 eulerAngles, Space space = Space.Self)
    {
        var rot = Quaternion.CreateFromYawPitchRoll(
            eulerAngles.Y * Deg2Rad,
            eulerAngles.X * Deg2Rad,
            eulerAngles.Z * Deg2Rad
        );
        
        if (space == Space.Self)
        {
            Rotation = Rotation * rot;
        }
        else
        {
            Rotation = rot * Rotation;
        }
    }
    
    // Transform point from local to world space
    public Vector3 TransformPoint(Vector3 point)
    {
        return Vector3.Transform(point, LocalToWorldMatrix);
    }
    
    // Transform direction (ignores translation)
    public Vector3 TransformDirection(Vector3 direction)
    {
        return Vector3.TransformNormal(direction, LocalToWorldMatrix);
    }
    
    // Inverse transform
    public Vector3 InverseTransformPoint(Vector3 point)
    {
        return Vector3.Transform(point, WorldToLocalMatrix);
    }
}

// Usage examples
var player = new Transform();
player.Position = new Vector3(10, 0, 5);
player.Rotation = Quaternion.CreateFromYawPitchRoll(Math.PI / 4, 0, 0);

// Move forward
player.Translate(player.Forward * moveSpeed * deltaTime);

// Look at enemy
var enemy = new Transform { Position = new Vector3(20, 0, 10) };
player.LookAt(enemy.Position, Vector3.Up);

// Parent-child relationship
var weapon = new Transform();
weapon.Parent = player;
weapon.Position = new Vector3(0.5f, 0, 1);  // Relative to player

// Orbit camera
public class OrbitCamera
{
    public Transform Target { get; set; }
    public float Distance { get; set; } = 10.0f;
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    
    public Transform CameraTransform { get; } = new Transform();
    
    public void Update()
    {
        // Calculate position
        var rotation = Quaternion.CreateFromYawPitchRoll(Yaw, Pitch, 0);
        var offset = rotation * new Vector3(0, 0, -Distance);
        
        CameraTransform.Position = Target.Position + offset;
        CameraTransform.LookAt(Target.Position, Vector3.Up);
    }
}`}
        />
      </section>

      {/* Practical Examples */}
      <section className="mb-12">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Practical Examples
        </h2>
        
        <h3 className="text-xl font-semibold text-gray-800 dark:text-gray-200 mb-3">
          Physics Simulation
        </h3>
        <CodeBlock
          code={`public class RigidBody
{
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; } = Quaternion.Identity;
    public Vector3 Velocity { get; set; }
    public Vector3 AngularVelocity { get; set; }
    public float Mass { get; set; } = 1.0f;
    public Matrix3x3 InertiaTensor { get; set; }
    
    public void ApplyForce(Vector3 force, Vector3 point)
    {
        // Linear acceleration
        Vector3 acceleration = force / Mass;
        Velocity += acceleration * Time.DeltaTime;
        
        // Torque = r × F
        Vector3 r = point - Position;
        Vector3 torque = r.Cross(force);
        
        // Angular acceleration
        Vector3 angularAccel = InertiaTensor.Inverse * torque;
        AngularVelocity += angularAccel * Time.DeltaTime;
    }
    
    public void Update(float deltaTime)
    {
        // Update position
        Position += Velocity * deltaTime;
        
        // Update rotation
        Quaternion spin = new Quaternion(
            AngularVelocity.X * deltaTime * 0.5f,
            AngularVelocity.Y * deltaTime * 0.5f,
            AngularVelocity.Z * deltaTime * 0.5f,
            0
        );
        Rotation = (Rotation + spin * Rotation).Normalized;
    }
}`}
        />

        <h3 className="text-xl font-semibold text-gray-800 dark:text-gray-200 mb-3 mt-6">
          3D Camera System
        </h3>
        <CodeBlock
          code={`public class Camera
{
    public Transform Transform { get; } = new Transform();
    public float FieldOfView { get; set; } = 60.0f;
    public float AspectRatio { get; set; } = 16.0f / 9.0f;
    public float NearPlane { get; set; } = 0.1f;
    public float FarPlane { get; set; } = 1000.0f;
    
    public Matrix4x4 ViewMatrix => Transform.WorldToLocalMatrix;
    
    public Matrix4x4 ProjectionMatrix =>
        Matrix4x4.CreatePerspectiveFieldOfView(
            FieldOfView * Deg2Rad,
            AspectRatio,
            NearPlane,
            FarPlane
        );
    
    public Matrix4x4 ViewProjectionMatrix => ViewMatrix * ProjectionMatrix;
    
    // Screen to world ray
    public Ray ScreenPointToRay(Vector2 screenPoint, Vector2 screenSize)
    {
        // Convert to NDC
        float x = (screenPoint.X / screenSize.X) * 2.0f - 1.0f;
        float y = 1.0f - (screenPoint.Y / screenSize.Y) * 2.0f;
        
        // Unproject
        var nearPoint = new Vector4(x, y, -1, 1);
        var farPoint = new Vector4(x, y, 1, 1);
        
        Matrix4x4.Invert(ViewProjectionMatrix, out var invVP);
        
        nearPoint = Vector4.Transform(nearPoint, invVP);
        farPoint = Vector4.Transform(farPoint, invVP);
        
        nearPoint /= nearPoint.W;
        farPoint /= farPoint.W;
        
        Vector3 origin = new Vector3(nearPoint.X, nearPoint.Y, nearPoint.Z);
        Vector3 direction = new Vector3(farPoint.X - nearPoint.X,
                                      farPoint.Y - nearPoint.Y,
                                      farPoint.Z - nearPoint.Z).Normalized;
        
        return new Ray(origin, direction);
    }
    
    // Frustum culling
    public bool IsInFrustum(BoundingSphere sphere)
    {
        var planes = ExtractFrustumPlanes(ViewProjectionMatrix);
        
        foreach (var plane in planes)
        {
            float distance = plane.Normal.Dot(sphere.Center) + plane.D;
            if (distance < -sphere.Radius)
                return false;
        }
        
        return true;
    }
}`}
        />

        <h3 className="text-xl font-semibold text-gray-800 dark:text-gray-200 mb-3 mt-6">
          Bezier Curves
        </h3>
        <CodeBlock
          code={`public class BezierCurve
{
    public Vector3[] ControlPoints { get; set; }
    
    public BezierCurve(params Vector3[] points)
    {
        ControlPoints = points;
    }
    
    // Evaluate curve at parameter t (0 to 1)
    public Vector3 Evaluate(float t)
    {
        return DeCasteljau(ControlPoints, t);
    }
    
    private Vector3 DeCasteljau(Vector3[] points, float t)
    {
        if (points.Length == 1)
            return points[0];
        
        var newPoints = new Vector3[points.Length - 1];
        for (int i = 0; i < newPoints.Length; i++)
        {
            newPoints[i] = Vector3.Lerp(points[i], points[i + 1], t);
        }
        
        return DeCasteljau(newPoints, t);
    }
    
    // Get tangent at parameter t
    public Vector3 GetTangent(float t)
    {
        float delta = 0.0001f;
        Vector3 p1 = Evaluate(t - delta);
        Vector3 p2 = Evaluate(t + delta);
        return (p2 - p1).Normalized;
    }
    
    // Arc length parameterization
    public float GetArcLength(int samples = 100)
    {
        float length = 0;
        Vector3 previousPoint = Evaluate(0);
        
        for (int i = 1; i <= samples; i++)
        {
            float t = i / (float)samples;
            Vector3 point = Evaluate(t);
            length += Vector3.Distance(previousPoint, point);
            previousPoint = point;
        }
        
        return length;
    }
}

// Cubic Bezier (4 control points)
var curve = new BezierCurve(
    new Vector3(0, 0, 0),
    new Vector3(0, 5, 2),
    new Vector3(0, 5, 8),
    new Vector3(0, 0, 10)
);

// Sample along curve
for (float t = 0; t <= 1; t += 0.1f)
{
    Vector3 point = curve.Evaluate(t);
    Vector3 tangent = curve.GetTangent(t);
    // Place object at point, oriented along tangent
}`}
        />
      </section>

      {/* Best Practices */}
      <section className="mb-12">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Best Practices
        </h2>
        
        <Callout type="tip" title="Performance Tips">
          Optimize linear algebra operations:
          <ul className="list-disc list-inside mt-2">
            <li>Use <code>LengthSquared</code> instead of <code>Length</code> for comparisons</li>
            <li>Prefer quaternions over Euler angles for rotations</li>
            <li>Cache frequently used matrices (like MVP)</li>
            <li>Use SIMD-accelerated operations when available</li>
            <li>Avoid normalizing vectors unnecessarily</li>
          </ul>
        </Callout>

        <Callout type="warning" title="Common Pitfalls">
          Avoid these common mistakes:
          <ul className="list-disc list-inside mt-2">
            <li>Matrix multiplication order matters (not commutative)</li>
            <li>Normalize quaternions after multiple operations</li>
            <li>Watch for gimbal lock with Euler angles</li>
            <li>Check for singular matrices before inverting</li>
            <li>Remember row-major vs column-major conventions</li>
          </ul>
        </Callout>

        <CodeBlock
          code={`// Good: Use squared distances for comparison
if (Vector3.DistanceSquared(a, b) < radius * radius)
{
    // Faster than using Distance
}

// Good: Cache matrix calculations
public class Renderer
{
    private Matrix4x4 viewMatrix;
    private Matrix4x4 projMatrix;
    private Matrix4x4 viewProjMatrix;
    private bool viewProjDirty = true;
    
    public Matrix4x4 ViewProjectionMatrix
    {
        get
        {
            if (viewProjDirty)
            {
                viewProjMatrix = viewMatrix * projMatrix;
                viewProjDirty = false;
            }
            return viewProjMatrix;
        }
    }
}

// Good: Validate before operations
public static bool TryNormalize(ref Vector3 vector)
{
    float length = vector.Length;
    if (length > 0.0001f)
    {
        vector /= length;
        return true;
    }
    return false;
}

// Good: Use appropriate precision
const float EPSILON = 0.0001f;
bool isZero = vector.LengthSquared < EPSILON * EPSILON;
bool areEqual = Vector3.DistanceSquared(a, b) < EPSILON * EPSILON;

// Good: Quaternion interpolation for smooth rotation
public void UpdateRotation(float deltaTime)
{
    // Smooth rotation that handles wraparound
    currentRotation = Quaternion.Slerp(
        currentRotation,
        targetRotation,
        1.0f - Math.Exp(-rotationSpeed * deltaTime)
    );
}`}
        />
      </section>
    </div>
  )
} 