using System;
using SysMath = System.Math;

namespace Ouroboros.StdLib.Math
{
    /// <summary>
    /// Quaternion class for 3D rotations and orientations
    /// </summary>
    public class Quaternion
    {
        public double W { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        #region Constructors

        public Quaternion(double w, double x, double y, double z)
        {
            W = w;
            X = x;
            Y = y;
            Z = z;
        }

        public Quaternion(Vector axis, double angle)
        {
            if (axis.Dimensions != 3)
                throw new ArgumentException("Axis must be a 3D vector");
                
            axis = axis.Normalized;
            double halfAngle = angle * 0.5;
            double sin = SysMath.Sin(halfAngle);
            
            W = SysMath.Cos(halfAngle);
            X = axis.X * sin;
            Y = axis.Y * sin;
            Z = axis.Z * sin;
        }

        public Quaternion(Quaternion other)
        {
            W = other.W;
            X = other.X;
            Y = other.Y;
            Z = other.Z;
        }

        // Static factory methods
        public static Quaternion Identity => new Quaternion(1, 0, 0, 0);
        
        public static Quaternion FromEulerAngles(double pitch, double yaw, double roll)
        {
            // Convert Euler angles to quaternion
            double cy = SysMath.Cos(yaw * 0.5);
            double sy = SysMath.Sin(yaw * 0.5);
            double cp = SysMath.Cos(pitch * 0.5);
            double sp = SysMath.Sin(pitch * 0.5);
            double cr = SysMath.Cos(roll * 0.5);
            double sr = SysMath.Sin(roll * 0.5);

            return new Quaternion(
                cr * cp * cy + sr * sp * sy,
                sr * cp * cy - cr * sp * sy,
                cr * sp * cy + sr * cp * sy,
                cr * cp * sy - sr * sp * cy
            );
        }

        public static Quaternion FromRotationMatrix(Matrix m)
        {
            if (m.Rows != 3 || m.Columns != 3)
                throw new ArgumentException("Matrix must be 3x3");
                
            double trace = m[0, 0] + m[1, 1] + m[2, 2];
            
            if (trace > 0)
            {
                double s = 0.5 / SysMath.Sqrt(trace + 1.0);
                return new Quaternion(
                    0.25 / s,
                    (m[2, 1] - m[1, 2]) * s,
                    (m[0, 2] - m[2, 0]) * s,
                    (m[1, 0] - m[0, 1]) * s
                );
            }
            else if ((m[0, 0] > m[1, 1]) && (m[0, 0] > m[2, 2]))
            {
                double s = 2.0 * SysMath.Sqrt(1.0 + m[0, 0] - m[1, 1] - m[2, 2]);
                return new Quaternion(
                    (m[2, 1] - m[1, 2]) / s,
                    0.25 * s,
                    (m[0, 1] + m[1, 0]) / s,
                    (m[0, 2] + m[2, 0]) / s
                );
            }
            else if (m[1, 1] > m[2, 2])
            {
                double s = 2.0 * SysMath.Sqrt(1.0 + m[1, 1] - m[0, 0] - m[2, 2]);
                return new Quaternion(
                    (m[0, 2] - m[2, 0]) / s,
                    (m[0, 1] + m[1, 0]) / s,
                    0.25 * s,
                    (m[1, 2] + m[2, 1]) / s
                );
            }
            else
            {
                double s = 2.0 * SysMath.Sqrt(1.0 + m[2, 2] - m[0, 0] - m[1, 1]);
                return new Quaternion(
                    (m[1, 0] - m[0, 1]) / s,
                    (m[0, 2] + m[2, 0]) / s,
                    (m[1, 2] + m[2, 1]) / s,
                    0.25 * s
                );
            }
        }

        public static Quaternion LookRotation(Vector forward, Vector up)
        {
            if (forward.Dimensions != 3 || up.Dimensions != 3)
                throw new ArgumentException("Vectors must be 3D");
                
            forward = forward.Normalized;
            Vector right = up.Cross(forward).Normalized;
            up = forward.Cross(right);
            
            var m = new Matrix(3, 3);
            m[0, 0] = right.X; m[0, 1] = up.X; m[0, 2] = forward.X;
            m[1, 0] = right.Y; m[1, 1] = up.Y; m[1, 2] = forward.Y;
            m[2, 0] = right.Z; m[2, 1] = up.Z; m[2, 2] = forward.Z;
            
            return FromRotationMatrix(m);
        }

        #endregion

        #region Mathematical Operations

        // Addition
        public static Quaternion operator +(Quaternion a, Quaternion b)
        {
            return new Quaternion(a.W + b.W, a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        // Subtraction
        public static Quaternion operator -(Quaternion a, Quaternion b)
        {
            return new Quaternion(a.W - b.W, a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        // Negation
        public static Quaternion operator -(Quaternion q)
        {
            return new Quaternion(-q.W, -q.X, -q.Y, -q.Z);
        }

        // Scalar multiplication
        public static Quaternion operator *(Quaternion q, double scalar)
        {
            return new Quaternion(q.W * scalar, q.X * scalar, q.Y * scalar, q.Z * scalar);
        }

        public static Quaternion operator *(double scalar, Quaternion q) => q * scalar;

        // Scalar division
        public static Quaternion operator /(Quaternion q, double scalar)
        {
            if (scalar == 0)
                throw new DivideByZeroException("Cannot divide quaternion by zero");
            return new Quaternion(q.W / scalar, q.X / scalar, q.Y / scalar, q.Z / scalar);
        }

        // Quaternion multiplication (Hamilton product)
        public static Quaternion operator *(Quaternion a, Quaternion b)
        {
            return new Quaternion(
                a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z,
                a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
                a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
                a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W
            );
        }

        #endregion

        #region Quaternion Operations

        // Magnitude (norm)
        public double Magnitude => SysMath.Sqrt(MagnitudeSquared);
        
        public double MagnitudeSquared => W * W + X * X + Y * Y + Z * Z;

        // Normalize
        public Quaternion Normalized
        {
            get
            {
                double mag = Magnitude;
                if (mag == 0) return new Quaternion(this);
                return this / mag;
            }
        }

        public void Normalize()
        {
            double mag = Magnitude;
            if (mag == 0) return;
            
            W /= mag;
            X /= mag;
            Y /= mag;
            Z /= mag;
        }

        // Conjugate
        public Quaternion Conjugate => new Quaternion(W, -X, -Y, -Z);

        // Inverse
        public Quaternion Inverse
        {
            get
            {
                double magSq = MagnitudeSquared;
                if (magSq == 0)
                    throw new InvalidOperationException("Cannot invert zero quaternion");
                return Conjugate / magSq;
            }
        }

        // Dot product
        public double Dot(Quaternion other)
        {
            return W * other.W + X * other.X + Y * other.Y + Z * other.Z;
        }

        // Angle between quaternions
        public double Angle(Quaternion other)
        {
            double dot = Dot(other);
            dot = SysMath.Max(-1, SysMath.Min(1, dot));
            return 2 * SysMath.Acos(SysMath.Abs(dot));
        }

        // Get axis and angle
        public (Vector axis, double angle) ToAxisAngle()
        {
            if (SysMath.Abs(W) >= 1)
                return (Vector.UnitX3, 0);
                
            double angle = 2 * SysMath.Acos(W);
            double s = SysMath.Sqrt(1 - W * W);
            
            if (s < 0.001)
                return (Vector.UnitX3, angle);
                
            return (new Vector(X / s, Y / s, Z / s), angle);
        }

        // Convert to Euler angles
        public (double pitch, double yaw, double roll) ToEulerAngles()
        {
            // Roll (x-axis rotation)
            double sinr_cosp = 2 * (W * X + Y * Z);
            double cosr_cosp = 1 - 2 * (X * X + Y * Y);
            double roll = SysMath.Atan2(sinr_cosp, cosr_cosp);

            // Pitch (y-axis rotation)
            double sinp = 2 * (W * Y - Z * X);
            double pitch;
            if (SysMath.Abs(sinp) >= 1)
                pitch = SysMath.CopySign(SysMath.PI / 2, sinp);
            else
                pitch = SysMath.Asin(sinp);

            // Yaw (z-axis rotation)
            double siny_cosp = 2 * (W * Z + X * Y);
            double cosy_cosp = 1 - 2 * (Y * Y + Z * Z);
            double yaw = SysMath.Atan2(siny_cosp, cosy_cosp);

            return (pitch, yaw, roll);
        }

        // Convert to rotation matrix
        public Matrix ToRotationMatrix()
        {
            double xx = X * X;
            double xy = X * Y;
            double xz = X * Z;
            double xw = X * W;
            
            double yy = Y * Y;
            double yz = Y * Z;
            double yw = Y * W;
            
            double zz = Z * Z;
            double zw = Z * W;
            
            return new Matrix(new double[,] {
                { 1 - 2 * (yy + zz), 2 * (xy - zw), 2 * (xz + yw) },
                { 2 * (xy + zw), 1 - 2 * (xx + zz), 2 * (yz - xw) },
                { 2 * (xz - yw), 2 * (yz + xw), 1 - 2 * (xx + yy) }
            });
        }

        // Rotate a vector
        public Vector Rotate(Vector v)
        {
            if (v.Dimensions != 3)
                throw new ArgumentException("Vector must be 3D");
                
            // Using the formula: v' = q * v * q^-1
            var qv = new Quaternion(0, v.X, v.Y, v.Z);
            var result = this * qv * Conjugate;
            
            return new Vector(result.X, result.Y, result.Z);
        }

        // Linear interpolation
        public static Quaternion Lerp(Quaternion a, Quaternion b, double t)
        {
            t = SysMath.Max(0, SysMath.Min(1, t));
            
            // Make sure we take the shortest path
            if (a.Dot(b) < 0)
                b = -b;
                
            return (a + (b - a) * t).Normalized;
        }

        // Spherical linear interpolation
        public static Quaternion Slerp(Quaternion a, Quaternion b, double t)
        {
            t = SysMath.Max(0, SysMath.Min(1, t));
            
            double dot = a.Dot(b);
            
            // Make sure we take the shortest path
            if (dot < 0)
            {
                b = -b;
                dot = -dot;
            }
            
            // If quaternions are very close, use linear interpolation
            if (dot > 0.9995)
                return Lerp(a, b, t);
                
            // Calculate angle between quaternions
            double theta = SysMath.Acos(dot);
            double sinTheta = SysMath.Sin(theta);
            
            double wa = SysMath.Sin((1 - t) * theta) / sinTheta;
            double wb = SysMath.Sin(t * theta) / sinTheta;
            
            return a * wa + b * wb;
        }

        // Get forward vector (assuming Z-forward convention)
        public Vector Forward => Rotate(Vector.Forward);
        
        // Get up vector
        public Vector Up => Rotate(Vector.Up);
        
        // Get right vector
        public Vector Right => Rotate(Vector.Right);

        #endregion

        #region Comparison

        public static bool operator ==(Quaternion a, Quaternion b)
        {
            if (ReferenceEquals(a, null)) return ReferenceEquals(b, null);
            if (ReferenceEquals(b, null)) return false;
            return a.W == b.W && a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        }

        public static bool operator !=(Quaternion a, Quaternion b) => !(a == b);

        public override bool Equals(object obj) => obj is Quaternion q && this == q;

        public override int GetHashCode()
        {
            return HashCode.Combine(W, X, Y, Z);
        }

        public bool ApproximatelyEquals(Quaternion other, double epsilon = 1e-6)
        {
            return SysMath.Abs(W - other.W) < epsilon &&
                   SysMath.Abs(X - other.X) < epsilon &&
                   SysMath.Abs(Y - other.Y) < epsilon &&
                   SysMath.Abs(Z - other.Z) < epsilon;
        }

        #endregion

        #region String Representation

        public override string ToString()
        {
            return $"({W:F3}, {X:F3}i, {Y:F3}j, {Z:F3}k)";
        }

        public string ToString(string format)
        {
            return $"({W.ToString(format)}, {X.ToString(format)}i, {Y.ToString(format)}j, {Z.ToString(format)}k)";
        }

        #endregion
    }
} 

