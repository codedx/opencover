﻿// Copyright 2017 Secure Decisions, a division of Applied Visions, Inc. 
// Permission is hereby granted, free of charge, to any person obtaining a copy of 
// this software and associated documentation files (the "Software"), to deal in the 
// Software without restriction, including without limitation the rights to use, copy, 
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the 
// following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies 
// or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using CodePulse.Client.Instrumentation.Id;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodePulse.Client.Test
{
    [TestClass]
    public class MethodIdentifierTests
    {
        [TestMethod]
        public void WhenMethodIdKnownClassIdReturned()
        {
            // arrange
            var methodIdentifier = new MethodIdentifier();
            var methodId = methodIdentifier.Record(1, "A", "B", 2, 3);

            // act
            var retVal = methodIdentifier.Record(1, "A", "B", 2, 3);

            // assert
            Assert.AreEqual(methodId, retVal);
            Assert.AreEqual("A", methodIdentifier.Lookup(methodId).Name);
        }

        [TestMethod]
        public void WhenMethodIdUnknownClassIdReturned()
        {
            // arrange
            var methodIdentifier = new MethodIdentifier();
            var methodId = methodIdentifier.Record(1, "A", "B", 2, 3);

            // act
            var retVal = methodIdentifier.Record(1, "C", "D", 2, 3);

            // assert
            Assert.AreEqual(methodId + 1, retVal);
            Assert.AreEqual("C", methodIdentifier.Lookup(methodId + 1).Name);
        }
    }
}
