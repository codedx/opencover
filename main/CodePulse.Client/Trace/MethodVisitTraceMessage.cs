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

namespace CodePulse.Client.Trace
{
    public class MethodVisitTraceMessage : ITraceMessage
    {
        public string ClassName { get; }
        public string SourceFile { get; }
        public string MethodName { get; }
        public string MethodSignature { get; }
        public int StartLineNumber { get; }
        public int EndLineNumber { get; }

        public MethodVisitTraceMessage(string className, string sourceFile, string methodName, string methodSignature,
            int startLineNumber, int endLineNumber)
        {
            if (string.IsNullOrWhiteSpace(className))
            {
                throw new System.ArgumentException("message", nameof(className));
            }

            if (string.IsNullOrWhiteSpace(methodName))
            {
                throw new System.ArgumentException("message", nameof(methodName));
            }

            if (string.IsNullOrWhiteSpace(methodSignature))
            {
                throw new System.ArgumentException("message", nameof(methodSignature));
            }

            ClassName = className;
            SourceFile = sourceFile;
            MethodName = methodName;
            MethodSignature = methodSignature;
            StartLineNumber = startLineNumber;
            EndLineNumber = endLineNumber;
        }
    }
}
