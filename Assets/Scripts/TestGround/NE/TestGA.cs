using Algorithms.NE;
using DL;
using UnityEngine;

namespace TestGround.NE
{
    public class TestGA : TestES
    {
        [SerializeField] private int eliteNumber;
        [SerializeField] private int tournamentNumber;

        public override string GetDescription()
        {
            return "GA" + (noveltyRelevance > 0 ? "-NS" : "") + ", 3 layers, " + neuronNumber + " neurons, " +
                   activationFunction + ", " + populationSize + " population size, noise std " +
                   noiseStandardDeviation + ", elite number " + eliteNumber + ", tournament number " +
                   tournamentNumber + ", novelty relevance " + noveltyRelevance + ", initialization std " +
                   weightsInitStd;
        }

        protected override void Start()
        {
            //Random.InitState(42);

            _env.CreatePopulation(populationSize);
            _currentSates = _env.DistributedResetEnv();

            var network = new Layer[]
            {
                new GANetworkLayer(populationSize, noiseStandardDeviation, _env.GetObservationSize, neuronNumber,
                    activationFunction, Instantiate(shader), paramsCoefficient: weightsInitStd),
                new GANetworkLayer(populationSize, noiseStandardDeviation, neuronNumber, neuronNumber,
                    activationFunction, Instantiate(shader), paramsCoefficient: weightsInitStd),
                new GANetworkLayer(populationSize, noiseStandardDeviation, neuronNumber, _env.GetNumberOfActions,
                    ActivationFunction.Linear, Instantiate(shader), paramsCoefficient: weightsInitStd)
            };

            var neModel = new GAModel(network);
            _neModel = new GA(neModel, _env.GetNumberOfActions, populationSize, eliteNumber, tournamentNumber,
                noveltyRelevance: noveltyRelevance);

            Time.timeScale = simulationSpeed;
        }
    }
}