﻿using System;
using System.Collections.Generic;
using System.Linq;
using ILGPU;
using ILGPU.Runtime;
using Qualia;

namespace Tools
{
    public struct NetworkGPU
    {
        private readonly ArrayView<LayerGPU> _layers;

        public NetworkGPU(NetworkDataModel model)
        {
            var layers = GPU.Instance.Accelerator.Allocate<LayerGPU>(model.Layers.Count);
            for (int i = 0; i < model.Layers.Count; ++i)
            {
                layers.CopyFrom(new LayerGPU(model.Layers[i]), i);
            }
            _layers = layers.View;
        }
       /*
        public void FeedForward()
        {
            for (int l = 0; l < Layers.Length - 1; ++l)
            {
                Layers.CopyTo(out LayerGPU layer, l);
                Layers.CopyTo(out LayerGPU layerNext, l + 1);

                for (int nn = 0; nn < layerNext.Neurons.Length; ++nn)
                {
                    layerNext.Neurons.CopyTo(out NeuronGPU nextNeuron, nn);

                    if (nextNeuron.IsBias && nextNeuron.IsBiasConnected)
                    {
                        double sum = 0;
                        for (int n = 0; n < layer.Neurons.Length; ++n)
                        {
                            layer.Neurons.CopyTo(out NeuronGPU bias, n);
                            sum += (bias.IsBias ? bias.AxW(nextNeuron) : 0);
                        }

                        nextNeuron.Activation = nextNeuron.ActivationFunction.Do(sum, nextNeuron.ActivationFuncParamA);
                    }

                    if (!nextNeuron.IsBias)
                    {
                        double sum = 0;
                        for (int n = 0; n < layer.Neurons.Length; ++n)
                        {
                            layer.Neurons.CopyTo(out NeuronGPU neuron, n);
                            sum += (neuron.Activation == 0 ? 0 : neuron.AxW(nextNeuron));
                        }

                        nextNeuron.Activation = nextNeuron.ActivationFunction.Do(sum, nextNeuron.ActivationFuncParamA);
                    }
                }
            }
        }
        */
    }

    public struct LayerGPU
    {
        public int Id;
        public ArrayView<NeuronGPU> Neurons;

        public LayerGPU(LayerDataModel layer)
        {
            Id = layer.Id;
            var neurons = GPU.Instance.Accelerator.Allocate<NeuronGPU>(layer.Neurons.Count);
            for(int i = 0; i < layer.Neurons.Count; ++i)
            {
                neurons.CopyFrom(new NeuronGPU(layer.Neurons[i]), i);
            }

            Neurons = neurons.View;
        }
    }

    public struct NeuronGPU
    {
        public int Id;
        public bool IsBias;
        public bool IsBiasConnected;
        public double Activation;
        public IActivationFunction ActivationFunction;
        public double? ActivationFuncParamA;

        private readonly ArrayView<WeightGPU> _weights;

        public NeuronGPU(NeuronDataModel neuron)
        {
            Id = neuron.Id;
            IsBias = neuron.IsBias;
            IsBiasConnected = neuron.IsBiasConnected;
            Activation = neuron.Activation;
            ActivationFunction = neuron.ActivationFunction;
            ActivationFuncParamA = neuron.ActivationFuncParamA;

            var weights = GPU.Instance.Accelerator.Allocate<WeightGPU>(neuron.Weights.Count);
            for (int i = 0; i < neuron.Weights.Count; ++i)
            {
                weights.CopyFrom(new WeightGPU(neuron.Weights[i]), i);
            }
            _weights = weights.View;
        }

        public double AxW(NeuronGPU nextNeuron)
        {
            var weight = _weights[nextNeuron.Id];
            return Activation * weight.Weight;
        }
    }

    public struct WeightGPU
    {
        public double Weight;

        public WeightGPU(WeightDataModel weight)
        {
            Weight = weight.Weight;
        }
    }

    public struct NeuronGPUEx
    {
        public int GlobalId;
        public int Id;
        public bool IsBias;
        public bool IsBiasConnected;
        public double Activation;
        //public string ActivationFunction;
        public double ActivationFuncParamA;


        public NeuronGPUEx(NeuronDataModel neuron, int globalId)
        {
            GlobalId = globalId;
            Id = neuron.Id;
            IsBias = neuron.IsBias;
            IsBiasConnected = neuron.IsBiasConnected;
            Activation = neuron.Activation;
           // ActivationFunction = null;// GPU.Instance.Accelerator.CompileKernel(typeof(ActivationFunction).GetMethod("None1"));
            ActivationFuncParamA = 0;// neuron.ActivationFuncParamA;
        }
    }

    public class GPU : IDisposable
    {
        public static GPU Instance = new GPU();
        public Accelerator Accelerator;

        private Context _context;

        private Dictionary<int, List<MemoryBuffer<double>>> _buffers;

        private Action<Index, ArrayView2D<double>, ArrayView2D<double>> _kernelProduct;
        private Action<Index, double[], double[], VariableView<double>> _kernelProduct2;

        public GPU()
        {
            _buffers = new Dictionary<int, List<MemoryBuffer<double>>>();

            _context = new Context();
            var acceleratorId = Accelerator.Accelerators.First(a => a.AcceleratorType == AcceleratorType.Cuda);
            Accelerator = Accelerator.Create(_context, acceleratorId);

            _kernelProduct = Accelerator.LoadAutoGroupedStreamKernel<Index, ArrayView2D<double>, ArrayView2D<double>>(Product);
            //KernelProduct2 = Accelerator.LoadAutoGroupedStreamKernel<Index, double[], double[], VariableView<double>>(KernelProduct2);
        }

        public NetworkGPU GetNetworkGPU(NetworkDataModel model)
        {
            return new NetworkGPU(model);
        }

        public MemoryBuffer<double> GetBuffer(int size)
        {
            if (!_buffers.ContainsKey(size))
            {
                _buffers.Add(size, new List<MemoryBuffer<double>>());
                _buffers[size].Add(Accelerator.Allocate<double>(size));
            }

            var list = _buffers[size];
            MemoryBuffer<double> buffer;
            if (list.Count == 0)
            {
                buffer = Accelerator.Allocate<double>(size);
            }
            else
            {
                buffer = list[0];
                list.RemoveAt(0);
            }
            
            return buffer;
        }

        private void ReleaseBuffer(MemoryBuffer<double> buffer)
        {
            _buffers[buffer.Length].Add(buffer);
        }

        public void Dispose()
        {
            Accelerator.Dispose();
            _context.Dispose();

            foreach (var key in _buffers.Keys)
            {
                var list = _buffers[key];
                foreach (var buff in list)
                {
                    buff.Dispose();
                }
            }
        }

        private static void Product(Index index, ArrayView2D<double> layer, ArrayView2D<double> layerNext)
        {
            //for (int n = 0; n < layerNext.Height; ++n)
            int n = index;
            {
                double sum = 0;
                if (layerNext[1, n] == 1 && layerNext[1, n] == 2) // connected bias
                {
                    for (int p = 0; p < layer.Height; ++p)
                    {
                        sum += layer[1, p] == 1 ? layer[0, p] * layer[3 + n, p] : 0;
                    }
                }

                if (layerNext[1, n] == 0) // not bias
                {
                    for (int p = 0; p < layer.Height; ++p)
                    {
                        sum += layer[0, p] * layer[3 + n, p];
                    }
                }

                layerNext[0, n] = sum;
            }
        }

        private static void Product2(Index index, double[] a, double[] w, VariableView<double> result)
        {

        }

        public void FeedForward(NetworkDataModel model)
        {
            for (int i = 0; i < model.Layers.Count - 1; ++i)
            {
                var neurons = model.Layers[i].GetNeurons();
                var neuronsNext = model.Layers[i + 1].GetNeurons();

                _kernelProduct(neuronsNext.Height, neurons, neuronsNext);

                Accelerator.Synchronize();

                for (int n = 0; n < neuronsNext.Height; ++n)
                {
                    neuronsNext.CopyTo(out double activation, new Index2(0, n));

                    model.Layers[i + 1].Neurons[n].Activation = model.Layers[i + 1].Neurons[n].ActivationFunction.Do(activation, model.Layers[i + 1].Neurons[n].ActivationFuncParamA);
                    neuronsNext.CopyFrom(model.Layers[i + 1].Neurons[n].Activation, new Index2(0, n));
                } 
            }
        }
    }
}
