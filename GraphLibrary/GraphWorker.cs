using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GraphLibrary
{
    public class WorkerCompletedEventArgs : EventArgs
    {
        public IReadOnlyList<IReadOnlyList<Stamp>> Result { get; }
        public bool IsEndlessLoop { get; }

        public WorkerCompletedEventArgs(IReadOnlyList<IReadOnlyList<Stamp>> result, bool isEndlessLoop)
        {
            Result = result;
            IsEndlessLoop = isEndlessLoop;
        }
    }

    public class GraphWorker
    {
        private bool _isUsed;
        private readonly Thread _thread;

        private readonly FinishDepartment _finishDepartment;
        private readonly StampList _stampList;
        private IDepartment _department;
        private readonly IDepartment _targetDepartment;

        private readonly List<IReadOnlyList<Stamp>> _result = new List<IReadOnlyList<Stamp>>();
        private bool _isEndlessLoop = false;
        private readonly List<IDepartment> _mapDepartment = new List<IDepartment>();
        private readonly HashSet<string> _states = new HashSet<string>();

        public delegate void WorkerCompletedHandler(object sender, WorkerCompletedEventArgs e);

        public event WorkerCompletedHandler WorkerCompleted;

        public GraphWorker(IDepartment startDepartment, FinishDepartment finishDepartment, IDepartment targetDepartment)
        {
            _finishDepartment = finishDepartment;
            _stampList = new StampList();
            _department = startDepartment;
            _targetDepartment = targetDepartment;
            _thread = new Thread(ThreadFunc) {IsBackground = true};
        }

        private void ThreadFunc()
        {
            _mapDepartment.Add(_department);

            while (_department != _finishDepartment && !_isEndlessLoop)
            {
                var nextDepartment = _department.Perform(_stampList);
                if (_department == _targetDepartment)
                {
                    var state = _stampList.GetState();
                    if (!_result.Any(it => state.Count == it.Count && state.All(it.Contains)))
                    {
                        _result.Add(state);
                    }
                }

                _department = nextDepartment;

                #region Check endless loop

                if (!_mapDepartment.Contains(_department))
                    _mapDepartment.Add(_department);

                var fullState = $"{_stampList.SerializeFullState()}&{_mapDepartment.IndexOf(_department)}";
                if (_states.Contains(fullState))
                    _isEndlessLoop = true;
                _states.Add(fullState);

                #endregion
            }

            if (_department == _targetDepartment)
            {
                var state = _stampList.GetState();
                if (!_result.Any(it => state.Count == it.Count && state.All(it.Contains)))
                {
                    _result.Add(state);
                }
            }

            WorkerCompleted?.Invoke(this, new WorkerCompletedEventArgs(_result, _isEndlessLoop));
        }

        public void Start()
        {
            if (_isUsed)
            {
                throw new Exception("This worker is already used");
            }

            _isUsed = true;

            _thread.Start();
        }

        public void Wait() => _thread.Join();
        public IReadOnlyList<IReadOnlyList<Stamp>> GetResult() => _result;
        public bool IsEndlessLoop() => _isEndlessLoop;
    }
}