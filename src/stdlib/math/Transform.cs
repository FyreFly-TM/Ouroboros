using System;

namespace Ouro.StdLib.Math
{
    /// <summary>
    /// Transform class for 3D transformations (position, rotation, scale)
    /// </summary>
    public class Transform
    {
        public Vector? Position { get; set; } = Vector.Zero3;
        public Quaternion? Rotation { get; set; } = Quaternion.Identity;
        public Vector? Scale { get; set; } = Vector.One3;

        // Parent transform for hierarchical transformations
        public Transform? Parent { get; set; }

        #region Constructors

        public Transform()
        {
            Position = Vector.Zero3;
            Rotation = Quaternion.Identity;
            Scale = Vector.One3;
        }

        public Transform(Vector position, Quaternion rotation, Vector scale)
        {
            if (position.Dimensions != 3)
                throw new ArgumentException("Position must be a 3D vector");
            if (scale.Dimensions != 3)
                throw new ArgumentException("Scale must be a 3D vector");
                
            Position = new Vector(position);
            Rotation = new Quaternion(rotation);
            Scale = new Vector(scale);
        }

        public Transform(Transform other)
        {
            Position = new Vector(other.Position!);
            Rotation = new Quaternion(other.Rotation!);
            Scale = new Vector(other.Scale!);
            Parent = other.Parent;
        }

        // Static factory methods
        public static Transform Identity => new Transform();
        
        public static Transform FromPosition(Vector position)
        {
            return new Transform(position, Quaternion.Identity, Vector.One3);
        }

        public static Transform FromRotation(Quaternion rotation)
        {
            return new Transform(Vector.Zero3, rotation, Vector.One3);
        }

        public static Transform FromScale(Vector scale)
        {
            return new Transform(Vector.Zero3, Quaternion.Identity, scale);
        }

        public static Transform FromScale(double uniformScale)
        {
            var scale = new Vector(uniformScale, uniformScale, uniformScale);
            return new Transform(Vector.Zero3, Quaternion.Identity, scale);
        }

        #endregion

        #region Matrix Conversions

        // Convert to 4x4 transformation matrix
        public Matrix ToMatrix()
        {
            // Create rotation matrix from quaternion
            var rotMatrix = Rotation!.ToRotationMatrix();
            
            // Build 4x4 transformation matrix
            var result = new Matrix(4, 4);
            
            // Apply rotation and scale
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    result[i, j] = rotMatrix[i, j] * Scale![j];
                }
            }
            
            // Apply translation
            result[0, 3] = Position!.X;
            result[1, 3] = Position!.Y;
            result[2, 3] = Position!.Z;
            
            // Homogeneous coordinate
            result[3, 3] = 1;
            
            return result;
        }

        // Create from 4x4 transformation matrix
        public static Transform FromMatrix(Matrix matrix)
        {
            if (matrix.Rows != 4 || matrix.Columns != 4)
                throw new ArgumentException("Matrix must be 4x4");
                
            // Extract position
            var position = new Vector(matrix[0, 3], matrix[1, 3], matrix[2, 3]);
            
            // Extract scale
            double scaleX = new Vector(matrix[0, 0], matrix[1, 0], matrix[2, 0]).Magnitude;
            double scaleY = new Vector(matrix[0, 1], matrix[1, 1], matrix[2, 1]).Magnitude;
            double scaleZ = new Vector(matrix[0, 2], matrix[1, 2], matrix[2, 2]).Magnitude;
            var scale = new Vector(scaleX, scaleY, scaleZ);
            
            // Extract rotation (normalize to remove scale)
            var rotMatrix = new Matrix(3, 3);
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    rotMatrix[i, j] = matrix[i, j] / scale[j];
                }
            }
            
            var rotation = Quaternion.FromRotationMatrix(rotMatrix);
            
            return new Transform(position, rotation, scale);
        }

        #endregion

        #region Transformation Operations

        // Transform a point (affected by position, rotation, and scale)
        public Vector TransformPoint(Vector point)
        {
            if (point.Dimensions != 3)
                throw new ArgumentException("Point must be a 3D vector");
                
            // Scale, rotate, then translate
            var scaled = new Vector(point.X * Scale!.X, point.Y * Scale!.Y, point.Z * Scale!.Z);
            var rotated = Rotation!.Rotate(scaled);
            return Position! + rotated;
        }

        // Transform a direction (affected by rotation and scale only)
        public Vector TransformDirection(Vector direction)
        {
            if (direction.Dimensions != 3)
                throw new ArgumentException("Direction must be a 3D vector");
                
            // Scale then rotate (no translation for directions)
            var scaled = new Vector(direction.X * Scale!.X, direction.Y * Scale!.Y, direction.Z * Scale!.Z);
            return Rotation!.Rotate(scaled);
        }

        // Transform a vector (affected by rotation only, preserves length)
        public Vector TransformVector(Vector vector)
        {
            if (vector.Dimensions != 3)
                throw new ArgumentException("Vector must be a 3D vector");
                
            return Rotation!.Rotate(vector);
        }

        // Inverse transform operations
        public Vector InverseTransformPoint(Vector point)
        {
            if (point.Dimensions != 3)
                throw new ArgumentException("Point must be a 3D vector");
                
            // Inverse: translate, rotate inverse, then scale inverse
            var translated = point - Position!;
            var rotated = Rotation!.Inverse.Rotate(translated);
            return new Vector(rotated.X / Scale!.X, rotated.Y / Scale!.Y, rotated.Z / Scale!.Z);
        }

        public Vector InverseTransformDirection(Vector direction)
        {
            if (direction.Dimensions != 3)
                throw new ArgumentException("Direction must be a 3D vector");
                
            // Inverse: rotate inverse, then scale inverse
            var rotated = Rotation!.Inverse.Rotate(direction);
            return new Vector(rotated.X / Scale!.X, rotated.Y / Scale!.Y, rotated.Z / Scale!.Z);
        }

        public Vector InverseTransformVector(Vector vector)
        {
            if (vector.Dimensions != 3)
                throw new ArgumentException("Vector must be a 3D vector");
                
            return Rotation!.Inverse.Rotate(vector);
        }

        #endregion

        #region Local/World Space

        // Get world space position (considering parent transforms)
        public Vector WorldPosition
        {
            get
            {
                if (Parent == null)
                    return Position!;
                return Parent.TransformPoint(Position!);
            }
            set
            {
                if (Parent == null)
                    Position = value!;
                else
                    Position = Parent.InverseTransformPoint(value!);
            }
        }

        // Get world space rotation
        public Quaternion WorldRotation
        {
            get
            {
                if (Parent == null)
                    return Rotation!;
                return Parent.WorldRotation * Rotation!;
            }
            set
            {
                if (Parent == null)
                    Rotation = value;
                else
                    Rotation = Parent.WorldRotation.Inverse * value;
            }
        }

        // Get world space scale
        public Vector WorldScale
        {
            get
            {
                if (Parent == null)
                    return Scale!;
                    
                var parentScale = Parent.WorldScale;
                return new Vector(
                    Scale!.X * parentScale.X,
                    Scale!.Y * parentScale.Y,
                    Scale!.Z * parentScale.Z
                );
            }
        }

        // Get world transformation matrix
        public Matrix WorldMatrix
        {
            get
            {
                if (Parent == null)
                    return ToMatrix();
                return Parent.WorldMatrix * ToMatrix();
            }
        }

        #endregion

        #region Direction Vectors

        // Local direction vectors
        public Vector Forward => Rotation!.Forward;
        public Vector Back => -Forward;
        public Vector Right => Rotation!.Right;
        public Vector Left => -Right;
        public Vector Up => Rotation!.Up;
        public Vector Down => -Up;

        // World direction vectors
        public Vector WorldForward => TransformDirection(Vector.Forward);
        public Vector WorldBack => -WorldForward;
        public Vector WorldRight => TransformDirection(Vector.Right);
        public Vector WorldLeft => -WorldRight;
        public Vector WorldUp => TransformDirection(Vector.Up);
        public Vector WorldDown => -WorldUp;

        #endregion

        #region Transformation Methods

        // Translate by a vector
        public void Translate(Vector translation, bool worldSpace = true)
        {
            if (translation.Dimensions != 3)
                throw new ArgumentException("Translation must be a 3D vector");
                
            if (worldSpace)
            {
                WorldPosition = WorldPosition + translation;
            }
            else
            {
                Position = Position! + translation;
            }
        }

        // Rotate by a quaternion
        public void Rotate(Quaternion rotation, bool worldSpace = true)
        {
            if (worldSpace)
            {
                Rotation = rotation * Rotation!;
            }
            else
            {
                Rotation = Rotation! * rotation;
            }
        }

        // Rotate around an axis by an angle
        public void RotateAround(Vector axis, double angle, bool worldSpace = true)
        {
            var q = new Quaternion(axis, angle);
            Rotate(q, worldSpace);
        }

        // Look at a target position
        public void LookAt(Vector target, Vector up)
        {
            if (target.Dimensions != 3 || up.Dimensions != 3)
                throw new ArgumentException("Vectors must be 3D");
                
            var forward = (target - WorldPosition).Normalized;
            WorldRotation = Quaternion.LookRotation(forward, up);
        }

        // Scale uniformly
        public void ScaleUniform(double factor)
        {
            Scale = Scale! * factor;
        }

        #endregion

        #region Interpolation

        // Linear interpolation between transforms
        public static Transform Lerp(Transform a, Transform b, double t)
        {
            t = global::System.Math.Max(0, global::System.Math.Min(1, t));
            
            return new Transform(
                Vector.Lerp(a.Position!, b.Position!, t),
                Quaternion.Slerp(a.Rotation!, b.Rotation!, t),
                Vector.Lerp(a.Scale!, b.Scale!, t)
            );
        }

        #endregion

        #region Utility Methods

        // Get inverse transform
        public Transform Inverse
        {
            get
            {
                var invRotation = Rotation!.Inverse;
                var invScale = new Vector(1.0 / Scale!.X, 1.0 / Scale!.Y, 1.0 / Scale!.Z);
                var invPosition = invRotation.Rotate(new Vector(
                    -Position!.X * invScale.X,
                    -Position!.Y * invScale.Y,
                    -Position!.Z * invScale.Z
                ));
                
                return new Transform(invPosition, invRotation, invScale);
            }
        }

        // Combine two transforms (multiply)
        public static Transform operator *(Transform a, Transform b)
        {
            return new Transform(
                a.TransformPoint(b.Position!),
                a.Rotation! * b.Rotation!,
                new Vector(a.Scale!.X * b.Scale!.X, a.Scale!.Y * b.Scale!.Y, a.Scale!.Z * b.Scale!.Z)
            );
        }

        // Reset to identity
        public void Reset()
        {
            Position = Vector.Zero3;
            Rotation = Quaternion.Identity;
            Scale = Vector.One3;
        }

        #endregion

        #region Comparison

        public bool Equals(Transform other, double epsilon = 1e-6)
        {
            if (other == null) return false;
            
            return Position!.ApproximatelyEquals(other.Position!, epsilon) &&
                   Rotation!.ApproximatelyEquals(other.Rotation!, epsilon) &&
                   Scale!.ApproximatelyEquals(other.Scale!, epsilon);
        }

        public override bool Equals(object? obj) => obj is Transform t && Equals(t);

        public override int GetHashCode()
        {
            return HashCode.Combine(Position!.GetHashCode(), Rotation!.GetHashCode(), Scale!.GetHashCode());
        }

        #endregion

        #region String Representation

        public override string ToString()
        {
            return $"Transform(Pos: {Position}, Rot: {Rotation}, Scale: {Scale})";
        }

        public string ToString(string format)
        {
            return $"Transform(Pos: {Position!.ToString(format)}, Rot: {Rotation!.ToString(format)}, Scale: {Scale!.ToString(format)})";
        }

        #endregion
    }
} 