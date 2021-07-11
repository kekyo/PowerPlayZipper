using System.Collections.Generic;
using System.IO;
using System.Threading;

using PowerPlayZipper.Compatibility;

#if NET20 || NET35
using ManualResetEventSlim = System.Threading.ManualResetEvent;
#endif

namespace PowerPlayZipper.Internal.Unzip
{
    /// <summary>
    /// Fast and lightweight directory creator.
    /// </summary>
    internal sealed class DirectoryConstructor
    {
        private readonly Dictionary<string, ManualResetEventSlim?> processings = new();
        
        /// <summary>
        /// Create directory if not exist.
        /// </summary>
        /// <param name="directoryPath">Target directory path</param>
        public void CreateIfNotExist(string directoryPath)
        {
            var firstTime = false;
            ManualResetEventSlim? locker;
            lock (this.processings)
            {
                if (!this.processings.TryGetValue(directoryPath, out locker))
                {
                    firstTime = true;
                    locker = IndependentFactory.CreateManualResetEvent();
                    this.processings.Add(directoryPath, locker);
                }
            }

            if (firstTime)
            {
                try
                {
                    if (!Directory.Exists(directoryPath))
                    {
                        try
                        {
                            Directory.CreateDirectory(directoryPath);
                        }
                        catch
                        {
                        }
                    }
                }
                finally
                {
                    lock (this.processings)
                    {
                        this.processings[directoryPath] = null;
                    }
                    locker!.Set();
                }
            }
            else
            {
                if (locker != null)
                {
                    // Will block short time when ran the first time task.
                    locker.Wait();
                }
            }
        }
    }
}
