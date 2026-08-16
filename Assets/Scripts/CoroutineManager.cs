using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TicTacToe
{
    public class CoroutineManager
    {
        private readonly MonoBehaviour _monoBehaviour;

        private readonly Queue<IEnumerator> _coroutines = new();

        private Coroutine _coroutineWatcher;

        public CoroutineManager(MonoBehaviour monoBehaviour)
        {
            _monoBehaviour = monoBehaviour;
        }

        private IEnumerator watch()
        {
            while (_coroutines.Count > 0)
                yield return _monoBehaviour.StartCoroutine(_coroutines.Dequeue());

            _coroutineWatcher = null;
        }

        public CoroutineManager EnqueueSequentally(params IEnumerator[] coroutines)
        {
            foreach (var coroutine in coroutines)
            {
                _coroutines.Enqueue(coroutine);
            }
            _coroutineWatcher ??= _monoBehaviour.StartCoroutine(watch());
            return this;
        }

        public CoroutineManager EnqueueParallel(params IEnumerator[] coroutines)
        {
            _coroutines.Enqueue(parallel(coroutines));
            _coroutineWatcher ??= _monoBehaviour.StartCoroutine(watch());
            return this;
        }

        public void StopAndClearCoroutines()
        {
            _monoBehaviour.StopAllCoroutines();
            _coroutines.Clear();
            _coroutineWatcher = null;
        }

        public bool AreCoroutinesRunning()
        {
            return _coroutineWatcher != null;
        }

        private IEnumerator parallel(params IEnumerator[] coroutines)
        {
            List<Coroutine> startedCoroutines = new(coroutines.Length);
            foreach (var coroutine in coroutines)
                startedCoroutines.Add(_monoBehaviour.StartCoroutine(coroutine));

            foreach (var startedCorutine in startedCoroutines)
                yield return startedCorutine;
        }
    }
}
