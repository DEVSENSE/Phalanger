using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PHP.Core;

namespace PHP.Library.Curl
{
    /// <summary>
    /// Represents a cURL multi-session.
    /// </summary>
    public class PhpCurlMultiResource : PhpResource
    {
        private readonly List<PhpCurlResource> _resources = new List<PhpCurlResource>();
        private readonly Dictionary<PhpCurlResource, Task<object>> _results = new Dictionary<PhpCurlResource, Task<object>>();
        private readonly List<PhpCurlResource> _returned = new List<PhpCurlResource>();
        private bool _started;

        internal PhpCurlMultiResource() : base("Curl")
        {
        }

        internal int StillRunning
        {
            get { return _results.Values.Count(a => !a.IsCompleted); }
        }

        internal bool SomeResultIsReady
        {
            get { return _results.Values.Any(a => a.IsCompleted); }
        }

        internal void Add(PhpCurlResource res)
        {
            _resources.Add(res);
            res.MultiParent = this;
        }

        internal void StartIfNeeded()
        {
            if (_started)
                return;
            _started = true;
            foreach (PhpCurlResource res in _resources)
            {
                PhpCurlResource res1 = res;
                Func<object> func = () => Curl.Execute(res1);
                var task = new Task<object>(func);
                task.Start();
                _results.Add(res1, task);
            }
        }

        /// <summary>
        /// Closes contained cURL sessions.
        /// </summary>
        public override void Close()
        {
            foreach (PhpCurlResource resource in _resources)
            {
                resource.Close();
            }
            base.Close();
        }

        internal object GetResult(PhpCurlResource handle)
        {
            return _results[handle].Result;
        }

        internal void Remove(PhpCurlResource resource)
        {
            _resources.Remove(resource);
        }

        internal void WaitAny(TimeSpan timeout)
        {
            Task.WaitAny(_results.Values.ToArray());
        }

        internal PhpCurlResource NextCompleted()
        {
            PhpCurlResource result = _results.Where(a => a.Value.IsCompleted && !_returned.Contains(a.Key)).Select(a => a.Key).FirstOrDefault();
            if (result != null)
            {
                _returned.Add(result);
            }
            return result;
        }
    }
}