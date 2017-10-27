﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using OpenCover.Framework.Utility;

namespace OpenCover.Test.Framework.Utility
{
    [TestFixture, Category("AdminOnly")]
    public class PerfCountersTests
    {
        private const string CategoryName = "OpenCover";
        private const string MemoryQueue = "MemoryQueue";
        private const string QueueThroughput = "QueueThroughput";

        private IPerfCounters _counters;

        [SetUp]
        public void Setup()
        {
            if (!IdentityHelper.IsRunningAsWindowsAdmin())
            {
                Assert.Inconclusive("Skipping PerfCountersTests because administrator privileges are missing.");
            }

            _counters = new PerfCounters();
            Assert.IsTrue(PerformanceCounterCategory.CounterExists(MemoryQueue, CategoryName));
            Assert.IsTrue(PerformanceCounterCategory.CounterExists(QueueThroughput, CategoryName));
        }

        [Test]
        public void CanSetAndReset_QueueCounter()
        {
            var counter = new PerformanceCounter(CategoryName, MemoryQueue);

            // arrange
            _counters.CurrentMemoryQueueSize = 10;
            Assert.AreEqual(10, counter.RawValue);

            // act
            _counters.ResetCounters();

            // assert
            Assert.AreEqual(0, counter.RawValue);
        }

        [Test]
        public void CanSetAndReset_QueueThroughput()
        {
            var counter = new PerformanceCounter(CategoryName, QueueThroughput);

            // arrange
            _counters.IncrementBlocksReceived();
            Assert.AreEqual(1, counter.RawValue);

            // act
            _counters.ResetCounters();

            // assert
            Assert.AreEqual(0, counter.RawValue);
        }
    }
}
