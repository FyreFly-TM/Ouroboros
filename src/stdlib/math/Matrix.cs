using System;
using System.Text;

namespace Ouro.StdLib.Math
{
    /// <summary>
    /// A dynamic matrix class supporting various matrix operations
    /// </summary>
    public class Matrix
    {
        private double[,] _elements;
        
        public int Rows { get; }
        public int Columns { get; }
        
        // Indexer
        public double this[int row, int col]
        {
            get => _elements[row, col];
            set => _elements[row, col] = value;
        }

        #region Constructors

        public Matrix(int rows, int columns)
        {
            if (rows <= 0 || columns <= 0)
                throw new ArgumentException("Matrix dimensions must be positive");
                
            Rows = rows;
            Columns = columns;
            _elements = new double[rows, columns];
        }

        public Matrix(double[,] elements)
        {
            Rows = elements.GetLength(0);
            Columns = elements.GetLength(1);
            _elements = (double[,])elements.Clone();
        }

        public Matrix(Matrix other)
        {
            Rows = other.Rows;
            Columns = other.Columns;
            _elements = (double[,])other._elements.Clone();
        }

        // Static factory methods
        public static Matrix Zero(int rows, int columns) => new Matrix(rows, columns);
        
        public static Matrix Identity(int size)
        {
            var m = new Matrix(size, size);
            for (int i = 0; i < size; i++)
                m[i, i] = 1;
            return m;
        }

        public static Matrix Diagonal(params double[] diagonal)
        {
            int size = diagonal.Length;
            var m = new Matrix(size, size);
            for (int i = 0; i < size; i++)
                m[i, i] = diagonal[i];
            return m;
        }

        // Common 2x2 matrices
        public static Matrix Identity2 => Identity(2);
        
        public static Matrix Rotation2D(double angle)
        {
            double cos = global::System.Math.Cos(angle);
            double sin = global::System.Math.Sin(angle);
            return new Matrix(new double[,] {
                { cos, -sin },
                { sin, cos }
            });
        }

        public static Matrix Scale2D(double sx, double sy)
        {
            return new Matrix(new double[,] {
                { sx, 0 },
                { 0, sy }
            });
        }

        // Common 3x3 matrices
        public static Matrix Identity3 => Identity(3);
        
        public static Matrix RotationX3D(double angle)
        {
            double cos = global::System.Math.Cos(angle);
            double sin = global::System.Math.Sin(angle);
            return new Matrix(new double[,] {
                { 1, 0, 0 },
                { 0, cos, -sin },
                { 0, sin, cos }
            });
        }

        public static Matrix RotationY3D(double angle)
        {
            double cos = global::System.Math.Cos(angle);
            double sin = global::System.Math.Sin(angle);
            return new Matrix(new double[,] {
                { cos, 0, sin },
                { 0, 1, 0 },
                { -sin, 0, cos }
            });
        }

        public static Matrix RotationZ3D(double angle)
        {
            double cos = global::System.Math.Cos(angle);
            double sin = global::System.Math.Sin(angle);
            return new Matrix(new double[,] {
                { cos, -sin, 0 },
                { sin, cos, 0 },
                { 0, 0, 1 }
            });
        }

        public static Matrix Scale3D(double sx, double sy, double sz)
        {
            return new Matrix(new double[,] {
                { sx, 0, 0 },
                { 0, sy, 0 },
                { 0, 0, sz }
            });
        }

        // Common 4x4 matrices
        public static Matrix Identity4 => Identity(4);
        
        public static Matrix Translation3D(double x, double y, double z)
        {
            return new Matrix(new double[,] {
                { 1, 0, 0, x },
                { 0, 1, 0, y },
                { 0, 0, 1, z },
                { 0, 0, 0, 1 }
            });
        }

        public static Matrix PerspectiveProjection(double fov, double aspect, double near, double far)
        {
            double f = 1.0 / global::System.Math.Tan(fov / 2.0);
            double nf = 1.0 / (near - far);
            
            return new Matrix(new double[,] {
                { f / aspect, 0, 0, 0 },
                { 0, f, 0, 0 },
                { 0, 0, (far + near) * nf, 2 * far * near * nf },
                { 0, 0, -1, 0 }
            });
        }

        public static Matrix OrthographicProjection(double left, double right, double bottom, double top, double near, double far)
        {
            double rl = 1.0 / (right - left);
            double tb = 1.0 / (top - bottom);
            double fn = 1.0 / (far - near);
            
            return new Matrix(new double[,] {
                { 2 * rl, 0, 0, -(right + left) * rl },
                { 0, 2 * tb, 0, -(top + bottom) * tb },
                { 0, 0, -2 * fn, -(far + near) * fn },
                { 0, 0, 0, 1 }
            });
        }

        public static Matrix LookAt(Vector eye, Vector target, Vector up)
        {
            Vector forward = (target - eye).Normalized;
            Vector right = forward.Cross(up).Normalized;
            Vector newUp = right.Cross(forward);
            
            return new Matrix(new double[,] {
                { right.X, right.Y, right.Z, -right.Dot(eye) },
                { newUp.X, newUp.Y, newUp.Z, -newUp.Dot(eye) },
                { -forward.X, -forward.Y, -forward.Z, forward.Dot(eye) },
                { 0, 0, 0, 1 }
            });
        }

        #endregion

        #region Mathematical Operations

        // Addition
        public static Matrix operator +(Matrix a, Matrix b)
        {
            if (a.Rows != b.Rows || a.Columns != b.Columns)
                throw new ArgumentException("Matrices must have same dimensions for addition");
                
            var result = new Matrix(a.Rows, a.Columns);
            for (int i = 0; i < a.Rows; i++)
                for (int j = 0; j < a.Columns; j++)
                    result[i, j] = a[i, j] + b[i, j];
            return result;
        }

        // Subtraction
        public static Matrix operator -(Matrix a, Matrix b)
        {
            if (a.Rows != b.Rows || a.Columns != b.Columns)
                throw new ArgumentException("Matrices must have same dimensions for subtraction");
                
            var result = new Matrix(a.Rows, a.Columns);
            for (int i = 0; i < a.Rows; i++)
                for (int j = 0; j < a.Columns; j++)
                    result[i, j] = a[i, j] - b[i, j];
            return result;
        }

        // Negation
        public static Matrix operator -(Matrix m)
        {
            var result = new Matrix(m.Rows, m.Columns);
            for (int i = 0; i < m.Rows; i++)
                for (int j = 0; j < m.Columns; j++)
                    result[i, j] = -m[i, j];
            return result;
        }

        // Scalar multiplication
        public static Matrix operator *(Matrix m, double scalar)
        {
            var result = new Matrix(m.Rows, m.Columns);
            for (int i = 0; i < m.Rows; i++)
                for (int j = 0; j < m.Columns; j++)
                    result[i, j] = m[i, j] * scalar;
            return result;
        }

        public static Matrix operator *(double scalar, Matrix m) => m * scalar;

        // Scalar division
        public static Matrix operator /(Matrix m, double scalar)
        {
            if (scalar == 0)
                throw new DivideByZeroException("Cannot divide matrix by zero");
                
            var result = new Matrix(m.Rows, m.Columns);
            for (int i = 0; i < m.Rows; i++)
                for (int j = 0; j < m.Columns; j++)
                    result[i, j] = m[i, j] / scalar;
            return result;
        }

        // Matrix multiplication
        public static Matrix operator *(Matrix a, Matrix b)
        {
            if (a.Columns != b.Rows)
                throw new ArgumentException("Matrix dimensions incompatible for multiplication");
                
            var result = new Matrix(a.Rows, b.Columns);
            for (int i = 0; i < a.Rows; i++)
            {
                for (int j = 0; j < b.Columns; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < a.Columns; k++)
                        sum += a[i, k] * b[k, j];
                    result[i, j] = sum;
                }
            }
            return result;
        }

        // Matrix-vector multiplication
        public Vector Multiply(Vector v)
        {
            if (Columns != v.Dimensions)
                throw new ArgumentException("Matrix columns must match vector dimensions");
                
            var result = new Vector(Rows);
            for (int i = 0; i < Rows; i++)
            {
                double sum = 0;
                for (int j = 0; j < Columns; j++)
                    sum += this[i, j] * v[j];
                result[i] = sum;
            }
            return result;
        }

        public static Vector operator *(Matrix m, Vector v) => m.Multiply(v);

        // Component-wise multiplication (Hadamard product)
        public Matrix HadamardProduct(Matrix other)
        {
            if (Rows != other.Rows || Columns != other.Columns)
                throw new ArgumentException("Matrices must have same dimensions");
                
            var result = new Matrix(Rows, Columns);
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Columns; j++)
                    result[i, j] = this[i, j] * other[i, j];
            return result;
        }

        #endregion

        #region Matrix Operations

        // Transpose
        public Matrix Transpose()
        {
            var result = new Matrix(Columns, Rows);
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Columns; j++)
                    result[j, i] = this[i, j];
            return result;
        }

        public Matrix T => Transpose();

        // Determinant (only for square matrices)
        public double Determinant()
        {
            if (Rows != Columns)
                throw new InvalidOperationException("Determinant is only defined for square matrices");
                
            if (Rows == 1)
                return this[0, 0];
                
            if (Rows == 2)
                return this[0, 0] * this[1, 1] - this[0, 1] * this[1, 0];
                
            if (Rows == 3)
            {
                return this[0, 0] * (this[1, 1] * this[2, 2] - this[1, 2] * this[2, 1]) -
                       this[0, 1] * (this[1, 0] * this[2, 2] - this[1, 2] * this[2, 0]) +
                       this[0, 2] * (this[1, 0] * this[2, 1] - this[1, 1] * this[2, 0]);
            }
            
            // For larger matrices, use LU decomposition
            var (l, u, _) = LUDecomposition();
            double det = 1;
            for (int i = 0; i < Rows; i++)
                det *= u[i, i];
            return det;
        }

        public double Det => Determinant();

        // Inverse (only for square matrices)
        public Matrix Inverse()
        {
            if (Rows != Columns)
                throw new InvalidOperationException("Inverse is only defined for square matrices");
                
            double det = Determinant();
            if (global::System.Math.Abs(det) < 1e-10)
                throw new InvalidOperationException("Matrix is singular (non-invertible)");
                
            if (Rows == 2)
            {
                return new Matrix(new double[,] {
                    { this[1, 1] / det, -this[0, 1] / det },
                    { -this[1, 0] / det, this[0, 0] / det }
                });
            }
            
            // For larger matrices, use Gauss-Jordan elimination
            var augmented = new Matrix(Rows, Columns * 2);
            
            // Copy original matrix and identity matrix
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Columns; j++)
                {
                    augmented[i, j] = this[i, j];
                    augmented[i, j + Columns] = (i == j) ? 1 : 0;
                }
            }
            
            // Perform Gauss-Jordan elimination
            for (int i = 0; i < Rows; i++)
            {
                // Find pivot
                int maxRow = i;
                for (int k = i + 1; k < Rows; k++)
                    if (global::System.Math.Abs(augmented[k, i]) > global::System.Math.Abs(augmented[maxRow, i]))
                        maxRow = k;
                        
                // Swap rows
                if (maxRow != i)
                {
                    for (int j = 0; j < augmented.Columns; j++)
                    {
                        double temp = augmented[i, j];
                        augmented[i, j] = augmented[maxRow, j];
                        augmented[maxRow, j] = temp;
                    }
                }
                
                // Scale pivot row
                double pivot = augmented[i, i];
                for (int j = 0; j < augmented.Columns; j++)
                    augmented[i, j] /= pivot;
                    
                // Eliminate column
                for (int k = 0; k < Rows; k++)
                {
                    if (k != i)
                    {
                        double factor = augmented[k, i];
                        for (int j = 0; j < augmented.Columns; j++)
                            augmented[k, j] -= factor * augmented[i, j];
                    }
                }
            }
            
            // Extract inverse matrix
            var result = new Matrix(Rows, Columns);
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Columns; j++)
                    result[i, j] = augmented[i, j + Columns];
                    
            return result;
        }

        // Trace (sum of diagonal elements)
        public double Trace()
        {
            if (Rows != Columns)
                throw new InvalidOperationException("Trace is only defined for square matrices");
                
            double sum = 0;
            for (int i = 0; i < Rows; i++)
                sum += this[i, i];
            return sum;
        }

        // Rank
        public int Rank()
        {
            var copy = new Matrix(this);
            int rank = 0;
            
            for (int row = 0; row < global::System.Math.Min(Rows, Columns); row++)
            {
                // Find pivot
                int pivotRow = row;
                for (int i = row + 1; i < Rows; i++)
                    if (global::System.Math.Abs(copy[i, row]) > global::System.Math.Abs(copy[pivotRow, row]))
                        pivotRow = i;
                        
                if (global::System.Math.Abs(copy[pivotRow, row]) < 1e-10)
                    continue;
                    
                // Swap rows
                if (pivotRow != row)
                {
                    for (int j = 0; j < Columns; j++)
                    {
                        double temp = copy[row, j];
                        copy[row, j] = copy[pivotRow, j];
                        copy[pivotRow, j] = temp;
                    }
                }
                
                rank++;
                
                // Eliminate column
                for (int i = row + 1; i < Rows; i++)
                {
                    double factor = copy[i, row] / copy[row, row];
                    for (int j = row; j < Columns; j++)
                        copy[i, j] -= factor * copy[row, j];
                }
            }
            
            return rank;
        }

        // LU Decomposition
        public (Matrix L, Matrix U, int[] permutation) LUDecomposition()
        {
            if (Rows != Columns)
                throw new InvalidOperationException("LU decomposition requires square matrix");
                
            var L = Identity(Rows);
            var U = new Matrix(this);
            var perm = new int[Rows];
            for (int i = 0; i < Rows; i++)
                perm[i] = i;
                
            for (int i = 0; i < Rows - 1; i++)
            {
                // Find pivot
                int pivotRow = i;
                for (int j = i + 1; j < Rows; j++)
                    if (global::System.Math.Abs(U[j, i]) > global::System.Math.Abs(U[pivotRow, i]))
                        pivotRow = j;
                        
                // Swap rows in U and permutation
                if (pivotRow != i)
                {
                    for (int j = 0; j < Columns; j++)
                    {
                        double temp = U[i, j];
                        U[i, j] = U[pivotRow, j];
                        U[pivotRow, j] = temp;
                    }
                    
                    int tempPerm = perm[i];
                    perm[i] = perm[pivotRow];
                    perm[pivotRow] = tempPerm;
                    
                    // Swap rows in L (only the computed part)
                    for (int j = 0; j < i; j++)
                    {
                        double temp = L[i, j];
                        L[i, j] = L[pivotRow, j];
                        L[pivotRow, j] = temp;
                    }
                }
                
                // Compute L and U
                for (int j = i + 1; j < Rows; j++)
                {
                    L[j, i] = U[j, i] / U[i, i];
                    for (int k = i; k < Columns; k++)
                        U[j, k] -= L[j, i] * U[i, k];
                }
            }
            
            return (L, U, perm);
        }

        // QR Decomposition using Gram-Schmidt
        public (Matrix Q, Matrix R) QRDecomposition()
        {
            var Q = new Matrix(Rows, Columns);
            var R = new Matrix(Columns, Columns);
            
            for (int j = 0; j < Columns; j++)
            {
                // Extract column j
                var v = new Vector(Rows);
                for (int i = 0; i < Rows; i++)
                    v[i] = this[i, j];
                    
                // Orthogonalize against previous columns
                for (int k = 0; k < j; k++)
                {
                    var q = new Vector(Rows);
                    for (int i = 0; i < Rows; i++)
                        q[i] = Q[i, k];
                        
                    R[k, j] = v.Dot(q);
                    v = v - R[k, j] * q;
                }
                
                R[j, j] = v.Magnitude;
                
                if (R[j, j] > 1e-10)
                {
                    v = v / R[j, j];
                    for (int i = 0; i < Rows; i++)
                        Q[i, j] = v[i];
                }
            }
            
            return (Q, R);
        }

        // Eigenvalues and eigenvectors
        public (double[] eigenvalues, Matrix eigenvectors) Eigen()
        {
            if (Rows != Columns)
                throw new InvalidOperationException("Eigendecomposition requires square matrix");
                
            // For symmetric matrices, use Jacobi method
            if (IsSymmetric())
            {
                return EigenSymmetric();
            }
            
            // For non-symmetric matrices, use QR algorithm
            return EigenGeneral();
        }
        
        // Eigendecomposition for symmetric matrices using Jacobi method
        private (double[] eigenvalues, Matrix eigenvectors) EigenSymmetric()
        {
            var A = new Matrix(this);
            var V = Identity(Rows);
            
            // Jacobi iterations
            for (int iter = 0; iter < 100; iter++)
            {
                // Find largest off-diagonal element
                int p = 0, q = 1;
                double maxVal = global::System.Math.Abs(A[0, 1]);
                
                for (int i = 0; i < Rows; i++)
                {
                    for (int j = i + 1; j < Columns; j++)
                    {
                        if (global::System.Math.Abs(A[i, j]) > maxVal)
                        {
                            maxVal = global::System.Math.Abs(A[i, j]);
                            p = i;
                            q = j;
                        }
                    }
                }
                
                if (maxVal < 1e-10)
                    break;
                    
                // Calculate rotation angle
                double theta = 0.5 * global::System.Math.Atan2(2 * A[p, q], A[q, q] - A[p, p]);
                double c = global::System.Math.Cos(theta);
                double s = global::System.Math.Sin(theta);
                
                // Apply rotation
                var G = Identity(Rows);
                G[p, p] = c;
                G[q, q] = c;
                G[p, q] = s;
                G[q, p] = -s;
                
                A = G.Transpose() * A * G;
                V = V * G;
            }
            
            // Extract eigenvalues
            var eigenvalues = new double[Rows];
            for (int i = 0; i < Rows; i++)
                eigenvalues[i] = A[i, i];
                
            return (eigenvalues, V);
        }
        
        // Eigendecomposition for general matrices using QR algorithm
        private (double[] eigenvalues, Matrix eigenvectors) EigenGeneral()
        {
            var A = new Matrix(this);
            var V = Identity(Rows);
            
            // QR Algorithm iterations
            for (int iter = 0; iter < 100; iter++)
            {
                var (Q, R) = A.QRDecomposition();
                A = R * Q;
                V = V * Q;
                
                // Check for convergence (off-diagonal elements close to zero)
                bool converged = true;
                for (int i = 1; i < Rows; i++)
                {
                    if (global::System.Math.Abs(A[i, i-1]) > 1e-10)
                    {
                        converged = false;
                        break;
                    }
                }
                
                if (converged)
                    break;
            }
            
            // Extract eigenvalues from diagonal
            var eigenvalues = new double[Rows];
            for (int i = 0; i < Rows; i++)
                eigenvalues[i] = A[i, i];
                
            return (eigenvalues, V);
        }

        // Check if matrix is symmetric
        public bool IsSymmetric(double tolerance = 1e-10)
        {
            if (Rows != Columns) return false;
            
            for (int i = 0; i < Rows; i++)
                for (int j = i + 1; j < Columns; j++)
                    if (global::System.Math.Abs(this[i, j] - this[j, i]) > tolerance)
                        return false;
            return true;
        }

        // Check if matrix is orthogonal
        public bool IsOrthogonal(double tolerance = 1e-10)
        {
            if (Rows != Columns) return false;
            
            var product = this * Transpose();
            var identity = Identity(Rows);
            
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Columns; j++)
                    if (global::System.Math.Abs(product[i, j] - identity[i, j]) > tolerance)
                        return false;
            return true;
        }

        #endregion

        #region Utility Methods

        // Get row as vector
        public Vector GetRow(int row)
        {
            var v = new Vector(Columns);
            for (int j = 0; j < Columns; j++)
                v[j] = this[row, j];
            return v;
        }

        // Get column as vector
        public Vector GetColumn(int col)
        {
            var v = new Vector(Rows);
            for (int i = 0; i < Rows; i++)
                v[i] = this[i, col];
            return v;
        }

        // Set row from vector
        public void SetRow(int row, Vector v)
        {
            if (v.Dimensions != Columns)
                throw new ArgumentException("Vector dimensions must match matrix columns");
                
            for (int j = 0; j < Columns; j++)
                this[row, j] = v[j];
        }

        // Set column from vector
        public void SetColumn(int col, Vector v)
        {
            if (v.Dimensions != Rows)
                throw new ArgumentException("Vector dimensions must match matrix rows");
                
            for (int i = 0; i < Rows; i++)
                this[i, col] = v[i];
        }

        // Extract submatrix
        public Matrix SubMatrix(int startRow, int startCol, int rows, int cols)
        {
            var result = new Matrix(rows, cols);
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    result[i, j] = this[startRow + i, startCol + j];
            return result;
        }

        #endregion

        #region Comparison

        public static bool operator ==(Matrix a, Matrix b)
        {
            if (ReferenceEquals(a, null)) return ReferenceEquals(b, null);
            if (ReferenceEquals(b, null)) return false;
            if (a.Rows != b.Rows || a.Columns != b.Columns) return false;
            
            for (int i = 0; i < a.Rows; i++)
                for (int j = 0; j < a.Columns; j++)
                    if (a[i, j] != b[i, j])
                        return false;
            return true;
        }

        public static bool operator !=(Matrix a, Matrix b) => !(a == b);

        public override bool Equals(object? obj) => obj is Matrix m && this == m;

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + Rows.GetHashCode();
            hash = hash * 31 + Columns.GetHashCode();
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Columns; j++)
                    hash = hash * 31 + this[i, j].GetHashCode();
            return hash;
        }

        public bool ApproximatelyEquals(Matrix other, double epsilon = 1e-6)
        {
            if (Rows != other.Rows || Columns != other.Columns) return false;
            
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Columns; j++)
                    if (global::System.Math.Abs(this[i, j] - other[i, j]) > epsilon)
                        return false;
            return true;
        }

        #endregion

        #region String Representation

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Matrix({Rows}x{Columns}):");
            
            for (int i = 0; i < Rows; i++)
            {
                sb.Append("[ ");
                for (int j = 0; j < Columns; j++)
                {
                    if (j > 0) sb.Append(", ");
                    sb.Append(this[i, j].ToString("F3").PadLeft(8));
                }
                sb.AppendLine(" ]");
            }
            
            return sb.ToString();
        }

        public string ToString(string format)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Matrix({Rows}x{Columns}):");
            
            for (int i = 0; i < Rows; i++)
            {
                sb.Append("[ ");
                for (int j = 0; j < Columns; j++)
                {
                    if (j > 0) sb.Append(", ");
                    sb.Append(this[i, j].ToString(format).PadLeft(8));
                }
                sb.AppendLine(" ]");
            }
            
            return sb.ToString();
        }

        #endregion
    }
} 