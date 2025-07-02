using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Math = System.Math;

namespace Ouro.StdLib.ML
{
    /// <summary>
    /// Layer interface for neural networks
    /// </summary>
    public interface ILayer
    {
        Tensor Forward(Tensor input);
        Tensor Backward(Tensor gradOutput);
        void UpdateWeights(double learningRate);
        List<Tensor> GetParameters();
        List<Tensor> GetGradients();
    }
    
    /// <summary>
    /// Dense (fully connected) layer
    /// </summary>
    public class DenseLayer : ILayer
    {
        private Tensor weights;
        private Tensor bias;
        private Tensor weightGrad;
        private Tensor biasGrad;
        private Tensor lastInput;
        
        public int InputSize { get; }
        public int OutputSize { get; }
        
        public DenseLayer(int inputSize, int outputSize)
        {
            InputSize = inputSize;
            OutputSize = outputSize;
            
            // Xavier initialization
            double scale = global::System.Math.Sqrt(2.0 / inputSize);
            weights = Tensor.RandomNormal(0, scale, outputSize, inputSize);
            bias = Tensor.Zeros(outputSize);
            
            weightGrad = Tensor.Zeros(outputSize, inputSize);
            biasGrad = Tensor.Zeros(outputSize);
        }
        
        public Tensor Forward(Tensor input)
        {
            lastInput = input;
            
            // For simplicity, assuming input is 2D (batch_size, input_size)
            var output = input.MatMul(weights.Transpose());
            
            // Add bias
            var batchSize = input.Shape[0];
            for (int b = 0; b < batchSize; b++)
            {
                for (int i = 0; i < OutputSize; i++)
                {
                    output[b, i] += bias[i];
                }
            }
            
            return output;
        }
        
        public Tensor Backward(Tensor gradOutput)
        {
            var batchSize = gradOutput.Shape[0];
            
            // Compute weight gradients
            weightGrad = gradOutput.Transpose().MatMul(lastInput);
            
            // Compute bias gradients
            for (int i = 0; i < OutputSize; i++)
            {
                double sum = 0;
                for (int b = 0; b < batchSize; b++)
                {
                    sum += gradOutput[b, i];
                }
                biasGrad[i] = sum;
            }
            
            // Compute input gradients
            var gradInput = gradOutput.MatMul(weights);
            
            return gradInput;
        }
        
        public void UpdateWeights(double learningRate)
        {
            // Simple gradient descent
            weights = weights - weightGrad * learningRate;
            bias = bias - biasGrad * learningRate;
            
            // Reset gradients
            weightGrad = Tensor.Zeros(OutputSize, InputSize);
            biasGrad = Tensor.Zeros(OutputSize);
        }
        
        public List<Tensor> GetParameters()
        {
            return new List<Tensor> { weights, bias };
        }
        
        public List<Tensor> GetGradients()
        {
            return new List<Tensor> { weightGrad, biasGrad };
        }
    }
    
    /// <summary>
    /// Activation functions
    /// </summary>
    public static class Activations
    {
        public static ILayer ReLU() => new ReLULayer();
        public static ILayer Sigmoid() => new SigmoidLayer();
        public static ILayer Tanh() => new TanhLayer();
        public static ILayer Softmax() => new SoftmaxLayer();
    }
    
    /// <summary>
    /// ReLU activation layer
    /// </summary>
    public class ReLULayer : ILayer
    {
        private Tensor lastInput;
        
        public Tensor Forward(Tensor input)
        {
            lastInput = input;
            return input.Apply(x => global::System.Math.Max(0, x));
        }
        
        public Tensor Backward(Tensor gradOutput)
        {
            // ReLU derivative: 1 if x > 0, 0 otherwise
            var gradInput = Tensor.Zeros(gradOutput.Shape);
            var flatGrad = new double[gradOutput.Size];
            var flatInput = new double[lastInput.Size];
            var flatGradOutput = new double[gradOutput.Size];
            
            // Flatten tensors for easier element-wise operations
            int idx = 0;
            for (int i = 0; i < lastInput.Shape[0]; i++)
            {
                for (int j = 0; j < (lastInput.Shape.Length > 1 ? lastInput.Shape[1] : 1); j++)
                {
                    if (lastInput.Shape.Length == 1)
                    {
                        flatInput[idx] = lastInput[i];
                        flatGradOutput[idx] = gradOutput[i];
                    }
                    else
                    {
                        flatInput[idx] = lastInput[i, j];
                        flatGradOutput[idx] = gradOutput[i, j];
                    }
                    idx++;
                }
            }
            
            // Apply ReLU gradient
            for (int i = 0; i < flatInput.Length; i++)
            {
                flatGrad[i] = flatInput[i] > 0 ? flatGradOutput[i] : 0;
            }
            
            // Reshape back
            return new Tensor(flatGrad, gradOutput.Shape);
        }
        
        public void UpdateWeights(double learningRate) { }
        public List<Tensor> GetParameters() => new List<Tensor>();
        public List<Tensor> GetGradients() => new List<Tensor>();
    }
    
    /// <summary>
    /// Sigmoid activation layer
    /// </summary>
    public class SigmoidLayer : ILayer
    {
        private Tensor lastOutput;
        
        public Tensor Forward(Tensor input)
        {
            lastOutput = input.Apply(x => 1.0 / (1.0 + global::System.Math.Exp(-x)));
            return lastOutput;
        }
        
        public Tensor Backward(Tensor gradOutput)
        {
            // sigmoid' = sigmoid * (1 - sigmoid)
            var sigmoid = lastOutput;
            var gradInput = gradOutput * sigmoid * sigmoid.Apply(x => 1 - x);
            return gradInput;
        }
        
        public void UpdateWeights(double learningRate) { }
        public List<Tensor> GetParameters() => new List<Tensor>();
        public List<Tensor> GetGradients() => new List<Tensor>();
    }
    
    /// <summary>
    /// Tanh activation layer
    /// </summary>
    public class TanhLayer : ILayer
    {
        private Tensor lastOutput;
        
        public Tensor Forward(Tensor input)
        {
            lastOutput = input.Apply(global::System.Math.Tanh);
            return lastOutput;
        }
        
        public Tensor Backward(Tensor gradOutput)
        {
            // tanh' = 1 - tanh^2
            var tanh = lastOutput;
            var gradInput = gradOutput * tanh.Apply(x => 1 - x * x);
            return gradInput;
        }
        
        public void UpdateWeights(double learningRate) { }
        public List<Tensor> GetParameters() => new List<Tensor>();
        public List<Tensor> GetGradients() => new List<Tensor>();
    }
    
    /// <summary>
    /// Softmax activation layer
    /// </summary>
    public class SoftmaxLayer : ILayer
    {
        private Tensor lastOutput;
        
        public Tensor Forward(Tensor input)
        {
            // Assuming 2D input (batch_size, num_classes)
            var batchSize = input.Shape[0];
            var numClasses = input.Shape[1];
            var output = Tensor.Zeros(batchSize, numClasses);
            
            for (int b = 0; b < batchSize; b++)
            {
                // Subtract max for numerical stability
                double maxVal = double.MinValue;
                for (int i = 0; i < numClasses; i++)
                {
                    maxVal = global::System.Math.Max(maxVal, input[b, i]);
                }
                
                double sum = 0;
                for (int i = 0; i < numClasses; i++)
                {
                    output[b, i] = global::System.Math.Exp(input[b, i] - maxVal);
                    sum += output[b, i];
                }
                
                for (int i = 0; i < numClasses; i++)
                {
                    output[b, i] /= sum;
                }
            }
            
            lastOutput = output;
            return output;
        }
        
        public Tensor Backward(Tensor gradOutput)
        {
            // For softmax with cross-entropy, gradient simplifies
            // This is a simplified version
            return gradOutput;
        }
        
        public void UpdateWeights(double learningRate) { }
        public List<Tensor> GetParameters() => new List<Tensor>();
        public List<Tensor> GetGradients() => new List<Tensor>();
    }
    
    /// <summary>
    /// Sequential neural network model
    /// </summary>
    public class Sequential
    {
        private readonly List<ILayer> layers = new();
        
        public Sequential Add(ILayer layer)
        {
            layers.Add(layer);
            return this;
        }
        
        public Tensor Forward(Tensor input)
        {
            var output = input;
            foreach (var layer in layers)
            {
                output = layer.Forward(output);
            }
            return output;
        }
        
        public Tensor Backward(Tensor gradOutput)
        {
            var grad = gradOutput;
            for (int i = layers.Count - 1; i >= 0; i--)
            {
                grad = layers[i].Backward(grad);
            }
            return grad;
        }
        
        public void UpdateWeights(double learningRate)
        {
            foreach (var layer in layers)
            {
                layer.UpdateWeights(learningRate);
            }
        }
        
        public List<Tensor> GetParameters()
        {
            var parameters = new List<Tensor>();
            foreach (var layer in layers)
            {
                parameters.AddRange(layer.GetParameters());
            }
            return parameters;
        }
    }
    
    /// <summary>
    /// Loss functions
    /// </summary>
    public static class Losses
    {
        /// <summary>
        /// Mean Squared Error loss
        /// </summary>
        public static (double loss, Tensor grad) MSE(Tensor predicted, Tensor target)
        {
            if (!predicted.Shape.SequenceEqual(target.Shape))
                throw new ArgumentException("Predicted and target must have same shape");
                
            var diff = predicted - target;
            var loss = diff.Apply(x => x * x).Mean();
            var grad = diff * (2.0 / predicted.Size);
            
            return (loss, grad);
        }
        
        /// <summary>
        /// Cross Entropy loss for classification
        /// </summary>
        public static (double loss, Tensor grad) CrossEntropy(Tensor predicted, Tensor target)
        {
            var batchSize = predicted.Shape[0];
            var numClasses = predicted.Shape[1];
            
            double loss = 0;
            var grad = Tensor.Zeros(predicted.Shape);
            
            for (int b = 0; b < batchSize; b++)
            {
                for (int i = 0; i < numClasses; i++)
                {
                    if (target[b, i] > 0)
                    {
                        loss -= target[b, i] * global::System.Math.Log(global::System.Math.Max(predicted[b, i], 1e-7));
                    }
                    grad[b, i] = predicted[b, i] - target[b, i];
                }
            }
            
            loss /= batchSize;
            grad = grad * (1.0 / batchSize);
            
            return (loss, grad);
        }
    }
    
    /// <summary>
    /// Optimizers
    /// </summary>
    public interface IOptimizer
    {
        void Step(List<Tensor> parameters, List<Tensor> gradients);
    }
    
    /// <summary>
    /// Stochastic Gradient Descent optimizer
    /// </summary>
    public class SGD : IOptimizer
    {
        private readonly double learningRate;
        private readonly double momentum;
        private readonly List<Tensor> velocities = new();
        
        public SGD(double learningRate, double momentum = 0.0)
        {
            this.learningRate = learningRate;
            this.momentum = momentum;
        }
        
        public void Step(List<Tensor> parameters, List<Tensor> gradients)
        {
            if (velocities.Count == 0 && momentum > 0)
            {
                foreach (var param in parameters)
                {
                    velocities.Add(Tensor.Zeros(param.Shape));
                }
            }
            
            for (int i = 0; i < parameters.Count; i++)
            {
                if (momentum > 0)
                {
                    velocities[i] = velocities[i] * momentum - gradients[i] * learningRate;
                    parameters[i] = parameters[i] + velocities[i];
                }
                else
                {
                    parameters[i] = parameters[i] - gradients[i] * learningRate;
                }
            }
        }
    }
    
    /// <summary>
    /// Adam optimizer
    /// </summary>
    public class Adam : IOptimizer
    {
        private readonly double learningRate;
        private readonly double beta1;
        private readonly double beta2;
        private readonly double epsilon;
        private readonly List<Tensor> m = new();
        private readonly List<Tensor> v = new();
        private int t = 0;
        
        public Adam(double learningRate = 0.001, double beta1 = 0.9, double beta2 = 0.999, double epsilon = 1e-8)
        {
            this.learningRate = learningRate;
            this.beta1 = beta1;
            this.beta2 = beta2;
            this.epsilon = epsilon;
        }
        
        public void Step(List<Tensor> parameters, List<Tensor> gradients)
        {
            t++;
            
            if (m.Count == 0)
            {
                foreach (var param in parameters)
                {
                    m.Add(Tensor.Zeros(param.Shape));
                    v.Add(Tensor.Zeros(param.Shape));
                }
            }
            
            for (int i = 0; i < parameters.Count; i++)
            {
                // Update biased first moment estimate
                m[i] = m[i] * beta1 + gradients[i] * (1 - beta1);
                
                // Update biased second raw moment estimate
                v[i] = v[i] * beta2 + gradients[i].Apply(x => x * x) * (1 - beta2);
                
                // Compute bias-corrected first moment estimate
                var mHat = m[i] * (1.0 / (1 - global::System.Math.Pow(beta1, t)));
                
                // Compute bias-corrected second raw moment estimate
                var vHat = v[i] * (1.0 / (1 - global::System.Math.Pow(beta2, t)));
                
                // Update parameters
                parameters[i] = parameters[i] - mHat * learningRate * vHat.Apply(x => 1.0 / (global::System.Math.Sqrt(x) + epsilon));
            }
        }
    }
} 