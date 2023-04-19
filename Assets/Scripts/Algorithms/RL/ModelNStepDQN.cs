using NN;
using NN.CPU_Single;
using UnityEngine;

namespace Algorithms.RL
{
    public class ModelNStepDQN : ModelDQN
    {
        private readonly int _nStep;
        private readonly float[] _storedNStepGammas;
        private readonly int[] _nStepIndexes;

        public ModelNStepDQN(int nStep, NetworkModel networkModel, NetworkModel targetModel, int numberOfActions,
            int stateSize,
            int maxExperienceSize = 10000, int minExperienceSize = 100, int batchSize = 32, float gamma = 0.99f) : base(
            networkModel, targetModel, numberOfActions, stateSize, maxExperienceSize, minExperienceSize, batchSize,
            gamma)
        {
            _nStep = nStep <= 0 ? 1 : nStep;
            _storedNStepGammas = new float[_nStep + 1];
            for (int i = 0; i < _nStep + 1; i++)
            {
                _storedNStepGammas[i] = Mathf.Pow(_gamma, i);
            }

            _nStepIndexes = new int[nStep - 1];
        }

        public override void AddExperience(float[] currentState, int action, float reward, bool done, float[] nextState)
        {
            var experience = new Experience(currentState, action, reward, done, nextState);
            if (_experiences.Count < _maxExperienceSize)
            {
                _experiences.Add(experience);
            }
            else
            {
                _experiences[_lastExperiencePosition] = experience;
            }

            _lastExperiencePosition = (_lastExperiencePosition + 1) % _maxExperienceSize;

            if (_experiences.Count < _nStep) return;
            
            int startingIndex = (_maxExperienceSize + _lastExperiencePosition - _nStep) % _maxExperienceSize;
            var newExperience = new Experience
            {
                CurrentState = _experiences[startingIndex].CurrentState,
                Action = _experiences[startingIndex].Action
            };
            
            for (int i = 0; i < _nStep; i++)
            {
                var nStepExperience = _experiences[(startingIndex + i) % _maxExperienceSize];
                newExperience.Done = nStepExperience.Done;
                newExperience.NextState = nStepExperience.NextState;
                newExperience.Reward += _storedNStepGammas[i] * nStepExperience.Reward;

                if (nStepExperience.Done) break;
            }

            _experiences[startingIndex] = newExperience;
            
            // Cannot put this in the above loop because that loop breaks if Done is true, but we still can't chose those indexes
            for (int i = 1; i < _nStep; i++)
            {
                _nStepIndexes[i - 1] = (startingIndex + i) % _maxExperienceSize;
            }
        }

        public override void Train()
        {
            if (_experiences.Count < _minExperienceSize)
                return;

            RandomBatch();

            MaxByRow(_targetModel.Predict(_nextStates));
            NnMath.CopyMatrix(_yTarget, _networkModel.Predict(_currentStates));

            for (int i = 0; i < _nextQ.Length; i++)
            {
                var experience = _experiences[_batchIndexes[i]];
                _yTarget[i, experience.Action] =
                    experience.Done
                        ? experience.Reward
                        : experience.Reward + _storedNStepGammas[_nStep] * _nextQ[i].value;
            }

            _networkModel.Update(_yTarget);
        }

        protected override void RandomBatch()
        {
            var nStepIndexesLenght = _nStepIndexes.Length;
            var iteration = 0;
            while (iteration < _batchSize)
            {
                _batchIndexes[iteration] = -1;

                // this works as intended if the AddExperience function is called before 
                var index = Random.Range(0, _experiences.Count);
                var hasIndex = false;

                for (int i = 0; i < nStepIndexesLenght; i++)
                {
                    if (index != _nStepIndexes[i]) continue;

                    hasIndex = true;
                    break;
                }

                for (int i = 0; i < iteration + 1; i++)
                {
                    if (_batchIndexes[i] != index) continue;

                    hasIndex = true;
                    break;
                }

                if (hasIndex) continue;

                _batchIndexes[iteration] = index;

                var experience = _experiences[index];
                for (int i = 0; i < _stateLenght; i++)
                {
                    _nextStates[iteration, i] = experience.NextState[i];
                    _currentStates[iteration, i] = experience.CurrentState[i];
                }

                ++iteration;
            }
        }
    }
}


/*
// Old code for reference
private readonly int _nStep;
        private readonly float[] _storedNStepGammas;

        public ModelNStepDQN(int nStep, NetworkModel networkModel, NetworkModel targetModel, int numberOfActions,
            int stateSize,
            int maxExperienceSize = 10000, int minExperienceSize = 100, int batchSize = 32, float gamma = 0.99f) : base(
            networkModel, targetModel, numberOfActions, stateSize, maxExperienceSize, minExperienceSize, batchSize,
            gamma)
        {
            _nStep = nStep <= 0 ? 1 : nStep;
            _storedNStepGammas = new float[_nStep + 1];
            for (int i = 0; i < _nStep + 1; i++)
            {
                _storedNStepGammas[i] = Mathf.Pow(_gamma, i);
            }
        }

        public override void AddExperience(float[] currentState, int action, float reward, bool done, float[] nextState)
        {
            var experience = new Experience(currentState, action, reward, done, nextState);
            int experienceContainerSize = _experiences.Count;

            if (experienceContainerSize < _maxExperienceSize)
            {
                _experiences.Add(experience);
            }
            else
            {
                _experiences[_lastExperiencePosition] = experience;
            }

            // int batchIndex = _batchIndexes[i];
            // var rewardSum = 0.0f;
            // for (int j = 0; j < _nStep; j++)
            // {
            //     var nStepExperience = _experiences[(batchIndex + j) % _experiences.Count];
            //     rewardSum += _storedNStepGammas[j] * nStepExperience.Reward;
            //
            //     if (nStepExperience.Done) break;
            //
            //     if (j == _nStep - 1)
            //     {
            //         rewardSum += _storedNStepGammas[_nStep] * _nextQ[i].value;
            //     }
            // }

            if (experienceContainerSize >= _nStep)
            {
                var newExperience = new Experience();
                
                // Does not work if number is negative
                int startingIndex = (_lastExperiencePosition - _nStep) % _maxExperienceSize;
                for (int i = 0; i < _nStep; i++)
                {
                    var nStepExperience = _experiences[(startingIndex + i) % _maxExperienceSize];
                    newExperience.Done = nStepExperience.Done;
                    
                    if (nStepExperience.Done) break;

                    newExperience.Reward = _storedNStepGammas[i] * nStepExperience.Reward;
                    newExperience.NextState = nStepExperience.CurrentState;
                }

                _experiences[startingIndex] = newExperience;
            }

            _lastExperiencePosition = (_lastExperiencePosition + 1) % _maxExperienceSize;
        }

        public override void Train()
        {
            if (_experiences.Count < _minExperienceSize)
                return;

            RandomBatch();

            MaxByRow(_targetModel.Predict(_nextStates));
            NnMath.CopyMatrix(_yTarget, _networkModel.Predict(_currentStates));

            for (int i = 0; i < _nextQ.Length; i++)
            {
                int batchIndex = _batchIndexes[i];
                var rewardSum = 0.0f;
                for (int j = 0; j < _nStep; j++)
                {
                    var nStepExperience = _experiences[(batchIndex + j) % _experiences.Count];
                    rewardSum += _storedNStepGammas[j] * nStepExperience.Reward;

                    if (nStepExperience.Done) break;

                    if (j == _nStep - 1)
                    {
                        rewardSum += _storedNStepGammas[_nStep] * _nextQ[i].value;
                    }
                }

                _yTarget[i, _experiences[batchIndex].Action] = rewardSum;
            }

            _networkModel.Update(_yTarget);
        }

        protected override void RandomBatch()
        {
            var iteration = 0;
            while (iteration < _batchSize)
            {
                _batchIndexes[iteration] = -1;
                // this works as intended if the AddExperience function is called before 
                var index = Random.Range(0, _experiences.Count - _nStep + _lastExperiencePosition) % _experiences.Count;
                var hasIndex = false;

                for (int i = 0; i < iteration + 1; i++)
                {
                    if (_batchIndexes[i] != index) continue;

                    hasIndex = true;
                    break;
                }

                if (hasIndex) continue;

                _batchIndexes[iteration] = index;

                var experienceNext = _experiences[(index + _nStep) % _experiences.Count];
                var experienceCurrent = _experiences[index];
                for (int i = 0; i < experienceNext.CurrentState.Length; i++)
                {
                    _nextStates[iteration, i] = experienceNext.CurrentState[i];
                    _currentStates[iteration, i] = experienceCurrent.CurrentState[i];
                }

                ++iteration;
            }
        }

*/