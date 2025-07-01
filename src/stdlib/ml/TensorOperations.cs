using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ouroboros.StdLib.ML
{
    /// <summary>
    /// Core tensor operations for machine learning
    /// </summary>
    public class Tensor
    {
        private readonly double[] data;
        private readonly int[] shape;
        private readonly int[] strides;
        
        public int[] Shape => (int[])shape.Clone();
        public int Rank => shape.Length;
        public int Size { get; }
        
        /// <summary>
        /// Create a tensor from data and shape
        /// </summary>
        public Tensor(double[] data, params int[] shape)
        {
            if (shape == null || shape.Length == 0)
                throw new ArgumentException("Shape must have at least one dimension");
                
            this.shape = (int[])shape.Clone();
            this.Size = shape.Aggregate(1, (a, b) => a * b);
            
            if (data.Length != Size)
                throw new ArgumentException($"Data length {data.Length} doesn't match shape size {Size}");
                
            this.data = (double[])data.Clone();
            this.strides = CalculateStrides(shape);
        }
        
        /// <summary>
        /// Create a tensor filled with zeros
        /// </summary>
        public static Tensor Zeros(params int[] shape)
        {
            var size = shape.Aggregate(1, (a, b) => a * b);
            return new Tensor(new double[size], shape);
        }
        
        /// <summary>
        /// Create a tensor filled with ones
        /// </summary>
        public static Tensor Ones(params int[] shape)
        {
            var size = shape.Aggregate(1, (a, b) => a * b);
            var data = new double[size];
            Array.Fill(data, 1.0);
            return new Tensor(data, shape);
        }
        
        /// <summary>
        /// Create a random tensor with values between 0 and 1
        /// </summary>
        public static Tensor Random(params int[] shape)
        {
            var size = shape.Aggregate(1, (a, b) => a * b);
            var data = new double[size];
            var rand = new Random();
            
            for (int i = 0; i < size; i++)
            {
                data[i] = rand.NextDouble();
            }
            
            return new Tensor(data, shape);
        }
        
        /// <summary>
        /// Create a random tensor with normal distribution
        /// </summary>
        public static Tensor RandomNormal(double mean = 0.0, double stdDev = 1.0, params int[] shape)
        {
            var size = shape.Aggregate(1, (a, b) => a * b);
            var data = new double[size];
            var rand = new Random();
            
            for (int i = 0; i < size; i++)
            {
                // Box-Muller transform
                double u1 = 1.0 - rand.NextDouble();
                double u2 = 1.0 - rand.NextDouble();
                double randStdNormal = global::System.Math.Sqrt(-2.0 * global::System.Math.Log(u1)) * global::System.Math.Sin(2.0 * global::System.Math.PI * u2);
                data[i] = mean + stdDev * randStdNormal;
            }
            
            return new Tensor(data, shape);
        }
        
        /// <summary>
        /// Get element at indices
        /// </summary>
        public double this[params int[] indices]
        {
            get
            {
                if (indices.Length != Rank)
                    throw new ArgumentException($"Expected {Rank} indices, got {indices.Length}");
                    
                int flatIndex = 0;
                for (int i = 0; i < indices.Length; i++)
                {
                    if (indices[i] < 0 || indices[i] >= shape[i])
                        throw new IndexOutOfRangeException($"Index {indices[i]} out of range for dimension {i} with size {shape[i]}");
                    flatIndex += indices[i] * strides[i];
                }
                
                return data[flatIndex];
            }
            set
            {
                if (indices.Length != Rank)
                    throw new ArgumentException($"Expected {Rank} indices, got {indices.Length}");
                    
                int flatIndex = 0;
                for (int i = 0; i < indices.Length; i++)
                {
                    if (indices[i] < 0 || indices[i] >= shape[i])
                        throw new IndexOutOfRangeException($"Index {indices[i]} out of range for dimension {i} with size {shape[i]}");
                    flatIndex += indices[i] * strides[i];
                }
                
                data[flatIndex] = value;
            }
        }
        
        /// <summary>
        /// Reshape tensor to new dimensions
        /// </summary>
        public Tensor Reshape(params int[] newShape)
        {
            var newSize = newShape.Aggregate(1, (a, b) => a * b);
            if (newSize != Size)
                throw new ArgumentException($"Cannot reshape tensor of size {Size} to size {newSize}");
                
            return new Tensor(data, newShape);
        }
        
        /// <summary>
        /// Element-wise addition
        /// </summary>
        public static Tensor operator +(Tensor a, Tensor b)
        {
            if (!a.shape.SequenceEqual(b.shape))
                throw new ArgumentException("Tensors must have same shape for element-wise addition");
                
            var result = new double[a.Size];
            for (int i = 0; i < a.Size; i++)
            {
                result[i] = a.data[i] + b.data[i];
            }
            
            return new Tensor(result, a.shape);
        }
        
        /// <summary>
        /// Element-wise subtraction
        /// </summary>
        public static Tensor operator -(Tensor a, Tensor b)
        {
            if (!a.shape.SequenceEqual(b.shape))
                throw new ArgumentException("Tensors must have same shape for element-wise subtraction");
                
            var result = new double[a.Size];
            for (int i = 0; i < a.Size; i++)
            {
                result[i] = a.data[i] - b.data[i];
            }
            
            return new Tensor(result, a.shape);
        }
        
        /// <summary>
        /// Element-wise multiplication
        /// </summary>
        public static Tensor operator *(Tensor a, Tensor b)
        {
            if (!a.shape.SequenceEqual(b.shape))
                throw new ArgumentException("Tensors must have same shape for element-wise multiplication");
                
            var result = new double[a.Size];
            for (int i = 0; i < a.Size; i++)
            {
                result[i] = a.data[i] * b.data[i];
            }
            
            return new Tensor(result, a.shape);
        }
        
        /// <summary>
        /// Scalar multiplication
        /// </summary>
        public static Tensor operator *(Tensor a, double scalar)
        {
            var result = new double[a.Size];
            for (int i = 0; i < a.Size; i++)
            {
                result[i] = a.data[i] * scalar;
            }
            
            return new Tensor(result, a.shape);
        }
        
        /// <summary>
        /// Scalar multiplication (commutative)
        /// </summary>
        public static Tensor operator *(double scalar, Tensor a)
        {
            return a * scalar;
        }
        
        /// <summary>
        /// Matrix multiplication
        /// </summary>
        public Tensor MatMul(Tensor other)
        {
            if (Rank != 2 || other.Rank != 2)
                throw new ArgumentException("Matrix multiplication requires 2D tensors");
                
            if (shape[1] != other.shape[0])
                throw new ArgumentException($"Incompatible shapes for matrix multiplication: ({shape[0]},{shape[1]}) x ({other.shape[0]},{other.shape[1]})");
                
            var m = shape[0];
            var n = other.shape[1];
            var k = shape[1];
            
            var result = new double[m * n];
            
            // Naive matrix multiplication - can be optimized with BLAS
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    double sum = 0;
                    for (int l = 0; l < k; l++)
                    {
                        sum += this[i, l] * other[l, j];
                    }
                    result[i * n + j] = sum;
                }
            }
            
            return new Tensor(result, m, n);
        }
        
        /// <summary>
        /// Transpose a 2D tensor
        /// </summary>
        public Tensor Transpose()
        {
            if (Rank != 2)
                throw new ArgumentException("Transpose only supported for 2D tensors");
                
            var result = new double[Size];
            var rows = shape[0];
            var cols = shape[1];
            
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    result[j * rows + i] = this[i, j];
                }
            }
            
            return new Tensor(result, cols, rows);
        }
        
        /// <summary>
        /// Apply a function element-wise
        /// </summary>
        public Tensor Apply(Func<double, double> func)
        {
            var result = new double[Size];
            for (int i = 0; i < Size; i++)
            {
                result[i] = func(data[i]);
            }
            
            return new Tensor(result, shape);
        }
        
        /// <summary>
        /// Sum all elements
        /// </summary>
        public double Sum()
        {
            return data.Sum();
        }
        
        /// <summary>
        /// Mean of all elements
        /// </summary>
        public double Mean()
        {
            return data.Average();
        }
        
        /// <summary>
        /// Standard deviation
        /// </summary>
        public double Std()
        {
            var mean = Mean();
            var variance = data.Select(x => global::System.Math.Pow(x - mean, 2)).Average();
            return global::System.Math.Sqrt(variance);
        }
        
        /// <summary>
        /// Get a slice of the tensor
        /// </summary>
        public Tensor Slice(params (int start, int end)[] ranges)
        {
            if (ranges.Length != Rank)
                throw new ArgumentException($"Expected {Rank} ranges, got {ranges.Length}");
                
            var newShape = new int[Rank];
            for (int i = 0; i < Rank; i++)
            {
                var (start, end) = ranges[i];
                if (start < 0 || end > shape[i] || start >= end)
                    throw new ArgumentException($"Invalid range ({start}, {end}) for dimension {i} with size {shape[i]}");
                newShape[i] = end - start;
            }
            
            var newSize = newShape.Aggregate(1, (a, b) => a * b);
            var newData = new double[newSize];
            
            // Copy data using recursive iteration
            CopySlice(data, newData, shape, newShape, strides, ranges, 0, new int[Rank], 0);
            
            return new Tensor(newData, newShape);
        }
        
        private void CopySlice(double[] src, double[] dst, int[] srcShape, int[] dstShape, 
                              int[] srcStrides, (int start, int end)[] ranges, 
                              int dim, int[] indices, int dstOffset)
        {
            if (dim == Rank)
            {
                int srcOffset = 0;
                for (int i = 0; i < Rank; i++)
                {
                    srcOffset += (indices[i] + ranges[i].start) * srcStrides[i];
                }
                dst[dstOffset] = src[srcOffset];
                return;
            }
            
            for (int i = 0; i < dstShape[dim]; i++)
            {
                indices[dim] = i;
                int newDstOffset = dstOffset + i * dstShape.Skip(dim + 1).Aggregate(1, (a, b) => a * b);
                CopySlice(src, dst, srcShape, dstShape, srcStrides, ranges, dim + 1, indices, newDstOffset);
            }
        }
        
        private static int[] CalculateStrides(int[] shape)
        {
            var strides = new int[shape.Length];
            strides[shape.Length - 1] = 1;
            
            for (int i = shape.Length - 2; i >= 0; i--)
            {
                strides[i] = strides[i + 1] * shape[i + 1];
            }
            
            return strides;
        }
        
        public override string ToString()
        {
            return $"Tensor(shape=[{string.Join(",", shape)}])";
        }
    }
} 