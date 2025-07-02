using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using SystemMath = System.Math;

namespace Ouro.StdLib.Math
{
    /// <summary>
    /// Mathematical vector class supporting arbitrary dimensions
    /// Used for physics calculations, 3D graphics, and mathematical operations
    /// </summary>
    public class Vector
    {
        private readonly double[] _components;
        
        #region Constructors

        /// <summary>
        /// Create a vector from an array of components
        /// </summary>
        public Vector(params double[] components)
        {
            if (components == null || components.Length == 0)
                throw new ArgumentException("Vector must have at least one component");
            
            _components = new double[components.Length];
            Array.Copy(components, _components, components.Length);
        }

        /// <summary>
        /// Create a zero vector of specified dimension
        /// </summary>
        public Vector(int dimensions)
        {
            if (dimensions <= 0)
                throw new ArgumentException("Vector dimension must be positive");
                
            _components = new double[dimensions];
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        public Vector(Vector other)
        {
            _components = new double[other._components.Length];
            Array.Copy(other._components, _components, _components.Length);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Number of dimensions in the vector
        /// </summary>
        public int Dimensions => _components.Length;

        /// <summary>
        /// Access vector components by index
        /// </summary>
        public double this[int index]
        {
            get
            {
                if (index < 0 || index >= _components.Length)
                    throw new IndexOutOfRangeException($"Index {index} is out of range for {_components.Length}-dimensional vector");
                return _components[index];
        }
            set
            {
                if (index < 0 || index >= _components.Length)
                    throw new IndexOutOfRangeException($"Index {index} is out of range for {_components.Length}-dimensional vector");
                _components[index] = value;
            }
        }

        /// <summary>
        /// X component (for 2D/3D vectors)
        /// </summary>
        public double X => _components.Length > 0 ? _components[0] : 0;
        
        /// <summary>
        /// Y component (for 2D/3D vectors)
        /// </summary>
        public double Y => _components.Length > 1 ? _components[1] : 0;
        
        /// <summary>
        /// Z component (for 3D vectors)
        /// </summary>
        public double Z => _components.Length > 2 ? _components[2] : 0;
        
        /// <summary>
        /// W component (for 4D vectors)
        /// </summary>
        public double W => _components.Length > 3 ? _components[3] : 0;

        /// <summary>
        /// Magnitude (length) of the vector
        /// </summary>
        public double Magnitude
        {
            get
            {
                double sumOfSquares = _components.Sum(x => x * x);
                return SystemMath.Sqrt(sumOfSquares);
            }
        }

        /// <summary>
        /// Squared magnitude (more efficient than Magnitude when comparing)
        /// </summary>
        public double SqrMagnitude => _components.Sum(x => x * x);

        /// <summary>
        /// Unit vector in the same direction
        /// </summary>
        public Vector Normalized
        {
            get
            {
                double mag = Magnitude;
                if (mag == 0) return new Vector(_components.Length);
                
                var normalized = new double[_components.Length];
                for (int i = 0; i < _components.Length; i++)
                    normalized[i] = _components[i] / mag;
                    
                return new Vector(normalized);
            }
        }
        
        #endregion

        #region Arithmetic Operators

        /// <summary>
        /// Vector addition
        /// </summary>
        public static Vector operator +(Vector a, Vector b)
        {
            if (a.Dimensions != b.Dimensions)
                throw new ArgumentException("Vectors must have the same dimensions for addition");
                
            var result = new double[a.Dimensions];
            for (int i = 0; i < a.Dimensions; i++)
                result[i] = a[i] + b[i];

            return new Vector(result);
        }

        /// <summary>
        /// Vector subtraction
        /// </summary>
        public static Vector operator -(Vector a, Vector b)
        {
            if (a.Dimensions != b.Dimensions)
                throw new ArgumentException("Vectors must have the same dimensions for subtraction");
                
            var result = new double[a.Dimensions];
            for (int i = 0; i < a.Dimensions; i++)
                result[i] = a[i] - b[i];

            return new Vector(result);
        }

        /// <summary>
        /// Scalar multiplication
        /// </summary>
        public static Vector operator *(Vector v, double scalar)
        {
            var result = new double[v.Dimensions];
            for (int i = 0; i < v.Dimensions; i++)
                result[i] = v[i] * scalar;

            return new Vector(result);
        }

        /// <summary>
        /// Scalar multiplication (commutative)
        /// </summary>
        public static Vector operator *(double scalar, Vector v) => v * scalar;

        /// <summary>
        /// Scalar division
        /// </summary>
        public static Vector operator /(Vector v, double scalar)
        {
            if (scalar == 0)
                throw new DivideByZeroException("Cannot divide vector by zero");
                
            var result = new double[v.Dimensions];
            for (int i = 0; i < v.Dimensions; i++)
                result[i] = v[i] / scalar;

            return new Vector(result);
        }

        /// <summary>
        /// Unary negation
        /// </summary>
        public static Vector operator -(Vector v)
        {
            var result = new double[v.Dimensions];
            for (int i = 0; i < v.Dimensions; i++)
                result[i] = -v[i];

            return new Vector(result);
        }

        #endregion

        #region Vector Operations

        /// <summary>
        /// Dot product with another vector
        /// </summary>
        public double Dot(Vector other)
        {
            if (Dimensions != other.Dimensions)
                throw new ArgumentException("Vectors must have the same dimensions for dot product");
                
            double result = 0;
            for (int i = 0; i < Dimensions; i++)
                result += _components[i] * other._components[i];

            return result;
        }

        /// <summary>
        /// Cross product with another 3D vector
        /// </summary>
        public Vector Cross(Vector other)
        {
            if (Dimensions != 3 || other.Dimensions != 3)
                throw new ArgumentException("Cross product requires 3D vectors");
                
            return new Vector(
                _components[1] * other._components[2] - _components[2] * other._components[1],
                _components[2] * other._components[0] - _components[0] * other._components[2],
                _components[0] * other._components[1] - _components[1] * other._components[0]
            );
        }

        /// <summary>
        /// Normalize the vector in place
        /// </summary>
        public void Normalize()
        {
            double mag = Magnitude;
            if (mag == 0) return;
            
            for (int i = 0; i < _components.Length; i++)
                _components[i] /= mag;
        }

        /// <summary>
        /// Distance to another vector
        /// </summary>
        public double DistanceTo(Vector other)
        {
            return (this - other).Magnitude;
        }

        /// <summary>
        /// Angle between this vector and another (in radians)
        /// </summary>
        public double AngleTo(Vector other)
        {
            if (Dimensions != other.Dimensions)
                throw new ArgumentException("Vectors must have the same dimensions");

            double dot = Dot(other);
            double mags = Magnitude * other.Magnitude;
            
            if (mags == 0) return 0;

            double cosAngle = dot / mags;
            // Clamp to avoid numerical errors
            cosAngle = SystemMath.Max(-1, SystemMath.Min(1, cosAngle));
            
            return SystemMath.Acos(cosAngle);
        }

        /// <summary>
        /// Project this vector onto another vector
        /// </summary>
        public Vector ProjectOnto(Vector other)
        {
            if (Dimensions != other.Dimensions)
                throw new ArgumentException("Vectors must have the same dimensions");

            double dot = Dot(other);
            double otherSqrMag = other.SqrMagnitude;
            
            if (otherSqrMag == 0) return new Vector(Dimensions);

            return other * (dot / otherSqrMag);
        }

        /// <summary>
        /// Reflect this vector across a surface with given normal
        /// </summary>
        public Vector Reflect(Vector normal)
        {
            if (Dimensions != normal.Dimensions)
                throw new ArgumentException("Vector and normal must have the same dimensions");

            // R = V - 2 * (V · N) * N
            double dotProduct = Dot(normal);
            return this - 2 * dotProduct * normal;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Check if this vector is approximately equal to another
        /// </summary>
        public bool ApproximatelyEquals(Vector other, double epsilon = 1e-10)
        {
            if (Dimensions != other.Dimensions) return false;

            for (int i = 0; i < Dimensions; i++)
            {
                if (SystemMath.Abs(_components[i] - other._components[i]) > epsilon)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Get a copy of the components array
        /// </summary>
        public double[] ToArray()
        {
            var result = new double[_components.Length];
            Array.Copy(_components, result, _components.Length);
            return result;
        }

        /// <summary>
        /// Convert to string representation
        /// </summary>
        public override string ToString()
        {
            return $"({string.Join(", ", _components.Select(x => x.ToString("F3")))})";
        }

        /// <summary>
        /// Convert to string representation with custom format
        /// </summary>
        public string ToString(string format)
        {
            return $"({string.Join(", ", _components.Select(x => x.ToString(format)))})";
        }

        /// <summary>
        /// Equality comparison
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is Vector other)
                return ApproximatelyEquals(other);
            return false;
        }

        /// <summary>
        /// Hash code for collections
        /// </summary>
        public override int GetHashCode()
        {
            int hash = Dimensions.GetHashCode();
            foreach (double component in _components)
            {
                hash = hash * 31 + component.GetHashCode();
            }
            return hash;
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Create a zero vector
        /// </summary>
        public static Vector Zero(int dimensions) => new Vector(dimensions);

        /// <summary>
        /// Create a unit vector along X axis
        /// </summary>
        public static Vector UnitX => new Vector(1, 0, 0);

        /// <summary>
        /// Create a unit vector along Y axis
        /// </summary>
        public static Vector UnitY => new Vector(0, 1, 0);

        /// <summary>
        /// Create a unit vector along Z axis
        /// </summary>
        public static Vector UnitZ => new Vector(0, 0, 1);

        /// <summary>
        /// Create a vector with all components set to the same value
        /// </summary>
        public static Vector Fill(int dimensions, double value)
        {
            var components = new double[dimensions];
            for (int i = 0; i < dimensions; i++)
                components[i] = value;
            return new Vector(components);
        }

        /// <summary>
        /// Linear interpolation between two vectors
        /// </summary>
        public static Vector Lerp(Vector a, Vector b, double t)
        {
            if (a.Dimensions != b.Dimensions)
                throw new ArgumentException("Vectors must have the same dimensions for interpolation");

            t = SystemMath.Max(0, SystemMath.Min(1, t)); // Clamp t to [0, 1]
            return a * (1 - t) + b * t;
        }

        // Legacy compatibility methods for existing code
        /// <summary>
        /// Create a 2D zero vector
        /// </summary>
        public static Vector Zero2 => new Vector(0, 0);
        
        /// <summary>
        /// Create a 3D zero vector
        /// </summary>
        public static Vector Zero3 => new Vector(0, 0, 0);
        
        /// <summary>
        /// Create a 2D one vector
        /// </summary>
        public static Vector One2 => new Vector(1, 1);
        
        /// <summary>
        /// Create a 3D one vector
        /// </summary>
        public static Vector One3 => new Vector(1, 1, 1);
        
        /// <summary>
        /// Create a 3D unit X vector
        /// </summary>
        public static Vector UnitX3 => new Vector(1, 0, 0);
        
        /// <summary>
        /// Create a 3D unit Y vector
        /// </summary>
        public static Vector UnitY3 => new Vector(0, 1, 0);
        
        /// <summary>
        /// Create a 3D unit Z vector
        /// </summary>
        public static Vector UnitZ3 => new Vector(0, 0, 1);
        
        /// <summary>
        /// Forward direction vector (positive Z)
        /// </summary>
        public static Vector Forward => new Vector(0, 0, 1);
        
        /// <summary>
        /// Right direction vector (positive X)
        /// </summary>
        public static Vector Right => new Vector(1, 0, 0);
        
        /// <summary>
        /// Up direction vector (positive Y)
        /// </summary>
        public static Vector Up => new Vector(0, 1, 0);

        #endregion
    }
} 