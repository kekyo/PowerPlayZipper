using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace PowerPlayZipper.Internal.Unzip
{
    internal sealed class DirectoryConstructor
    {
        private readonly Dictionary<string, ManualResetEventSlim?> processings = new();

        public void CreateIfNotExist(string directoryPath)
        {
            var firstTime = false;
            ManualResetEventSlim? locker;
            lock (this.processings)
            {
                if (!this.processings.TryGetValue(directoryPath, out locker))
                {
                    firstTime = true;
                    locker = new ManualResetEventSlim(false);
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
