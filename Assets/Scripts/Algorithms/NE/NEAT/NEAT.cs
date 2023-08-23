namespace Algorithms.NE.NEAT
{
    public class NEAT
    {
        private readonly NEATModel _neatModel;
        private readonly int _numberOfActions;
        private readonly int _batchSize;

        private readonly int[] _sampledActions;
        private readonly float[] _episodeRewards;
        private readonly bool[] _completedAgents;

        //Cashed variables
        private float[] _modelPredictions;

        private readonly float[] _episodeRewardUpdate;
        private int _finishedIndividuals;
        private float _episodeRewardMean;
        
        public float EpisodeRewardMean => _episodeRewardMean;
        public float FinishedIndividuals => _finishedIndividuals;

        public NEAT(NEATModel neatModel, int numberOfActions, int batchSize)
        {
            _neatModel = neatModel;
            _numberOfActions = numberOfActions;
            _batchSize = batchSize;

            _sampledActions = new int[batchSize];

            _episodeRewards = new float[batchSize];
            _completedAgents = new bool[batchSize];

            _episodeRewardUpdate = new float[batchSize];
        }

        public int[] SamplePopulationActions(float[,] states)
        {
            _modelPredictions = _neatModel.Predict(states);

            for (int i = 0; i < _batchSize; i++)
            {
                if (_completedAgents[i])
                {
                    _sampledActions[i] = -1;
                    continue;
                }

                var individualStartIndex = i * _batchSize;
                var maxAction = _modelPredictions[individualStartIndex];
                var maxIndex = 0;
                for (int j = 1; j < _numberOfActions; j++)
                {
                    var actionValue = _modelPredictions[individualStartIndex + j];
                    if (maxAction > actionValue) continue;

                    maxAction = actionValue;
                    maxIndex = j;
                }

                _sampledActions[i] = maxIndex;
            }

            return _sampledActions;
        }
        
        public void AddExperience(float[] rewards, bool[] dones)
        {
            for (int i = 0; i < _batchSize; i++)
            {
                if (_completedAgents[i]) continue;

                var done = dones[i];
                _episodeRewards[i] += rewards[i];
                _completedAgents[i] = done;
                _finishedIndividuals += done ? 1 : 0;
            }
        }

        public void Train()
        {
            _episodeRewardMean = 0f;
            for (int i = 0; i < _batchSize; i++)
            {
                var reward = _episodeRewards[i];
                _episodeRewardMean += reward;
                _episodeRewardUpdate[i] = reward;
            
                _episodeRewards[i] = 0f;
                _completedAgents[i] = false;
            }

            _finishedIndividuals = 0;
            _episodeRewardMean /= _batchSize;
            
            _neatModel.Update(_episodeRewardUpdate);
        }

        public void Dispose()
        {
        }
    }
}