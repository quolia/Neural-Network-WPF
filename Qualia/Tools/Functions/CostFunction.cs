﻿using Qualia.Model;
using System.Runtime.CompilerServices;

namespace Qualia.Tools
{
    unsafe public class CostFunction : BaseFunction<CostFunction>
    {
        public delegate*<NetworkDataModel, double> Do;
        public delegate*<NetworkDataModel, NeuronDataModel, double> Derivative;

        public CostFunction(delegate*<NetworkDataModel, double> doFunc, delegate*<NetworkDataModel, NeuronDataModel, double> doDerivative)
        {
            Do = doFunc;
            Derivative = doDerivative;
        }

        unsafe sealed public class MSE
        {
            public static readonly CostFunction Instance = new(&Do, &Derivative);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Do(NetworkDataModel networkModel)
            {
                double sum = 0;
                var neuronModel = networkModel.Layers.Last.Neurons.First;

                while (neuronModel != null)
                {
                    var diff = neuronModel.Target - neuronModel.Activation;
                    sum += diff * diff;

                    neuronModel = neuronModel.Next;
                }

                return sum;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static double Derivative(NetworkDataModel networkModel, NeuronDataModel neuronModel)
            {
                return neuronModel.Target - neuronModel.Activation;
            }
        }
    }
}
