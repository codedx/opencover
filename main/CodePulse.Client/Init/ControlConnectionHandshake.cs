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

using System;
using CodePulse.Client.Config;
using CodePulse.Client.Connect;
using CodePulse.Client.Control;
using CodePulse.Client.Message;
using CodePulse.Client.Util;

namespace CodePulse.Client.Init
{
    public class ControlConnectionHandshake : IControlConnectionHandshake
    {
        private readonly IMessageProtocol _messageProtocol;
        private readonly IConfigurationReader _configurationReader;

        public ControlConnectionHandshake(IMessageProtocol messageProtocol, IConfigurationReader configurationReader)
        {
            _messageProtocol = messageProtocol ?? throw new ArgumentNullException(nameof(messageProtocol));
            _configurationReader = configurationReader ?? throw new ArgumentNullException(nameof(configurationReader));
        }

        public RuntimeAgentConfiguration PerformHandshake(IConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            var outputWriter = connection.OutputWriter;

            _messageProtocol.WriteHello(outputWriter);
            outputWriter.FlushAndLog("WriteHello");

            var inputReader = connection.InputReader;
            var reply = inputReader.ReadByte();
            
            switch (reply)
            {
                case MessageTypes.Configuration:
                    return _configurationReader.ReadConfiguration(inputReader);
                case MessageTypes.Error:
                    throw new HandshakeException(inputReader.ReadUtfBigEndian(), reply);
                default:
                    throw new HandshakeException($"Handshake operation failed with unexpected reply: {reply}", reply);
            }
        }
    }
}
