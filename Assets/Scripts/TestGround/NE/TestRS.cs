using Algorithms.NE;
using NN;
using UnityEngine;

namespace TestGround.NE
{
    public class TestRS : TestES
    {
        public override string GetDescription()
        {
            return "RS, 3 layers, " + neuronNumber + " neurons, " + activationFunction +
                   ", " + populationSize + " population size, noise std " + noiseStandardDeviation +
                   ", initialization std " + weightsInitStd;
        }

        protected override void Start()
        {
            _env.CreatePopulation(populationSize);
            _currentSates = _env.DistributedResetEnv();

            var network = new NetworkLayer[]
            {
                new ESNetworkLayer(AlgorithmNE.RS, populationSize, noiseStandardDeviation, _env.GetObservationSize,
                    neuronNumber, activationFunction, Instantiate(shader), paramsCoefficient: weightsInitStd),
                new ESNetworkLayer(AlgorithmNE.RS, populationSize, noiseStandardDeviation, neuronNumber, neuronNumber,
                    activationFunction, Instantiate(shader), paramsCoefficient: weightsInitStd),
                new ESNetworkLayer(AlgorithmNE.RS, populationSize, noiseStandardDeviation, neuronNumber,
                    _env.GetNumberOfActions, ActivationFunction.Linear, Instantiate(shader),
                    paramsCoefficient: weightsInitStd)
            };

            var neModel = new ESModel(network);
            _neModel = new RS(neModel, _env.GetNumberOfActions, populationSize);

            Time.timeScale = simulationSpeed;
        }
    }
}