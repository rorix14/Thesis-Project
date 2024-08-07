﻿using DL;
using DL.NN;

namespace Algorithms.NE
{
    public class GAModel : NetworkModel
    {
        private readonly GANetworkLayer[] _gaNetworkLayers;

        public GAModel(Layer[] layers) : base(layers,
            new NoLoss(null))
        {
            _gaNetworkLayers = new GANetworkLayer[layers.Length];
            for (int i = 0; i < layers.Length; i++)
            {
                _gaNetworkLayers[i] = (GANetworkLayer)layers[i];
            }
        }

        public void Update(CrossoverInfo[] crossoverInfos, float[] mutationsVolume)
        {
            for (int i = 0; i < _layersCount; i++)
            {
                _gaNetworkLayers[i].UpdateLayer(crossoverInfos, mutationsVolume);
            }
        }
    }
}