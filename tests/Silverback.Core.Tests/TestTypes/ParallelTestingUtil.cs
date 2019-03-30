﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Tests.Core.Messaging.Publishing
{
    public class ParallelTestingUtil
    {
        private int _lastStep = 0;
        public List<int> Steps { get; } = new List<int>();

        public void DoWork()
        {
            Thread.Sleep(20);
            lock (Steps) Steps.Add(_lastStep + 1);
            Thread.Sleep(20);
            Interlocked.Increment(ref _lastStep);
        }

        public async Task DoWorkAsync()
        {
            await Task.Delay(20);
            lock (Steps) Steps.Add(_lastStep + 1);
            await Task.Delay(20);
            Interlocked.Increment(ref _lastStep);
        }
    }
}
