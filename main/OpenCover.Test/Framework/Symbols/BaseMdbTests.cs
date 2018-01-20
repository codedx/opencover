//
// Modified work Copyright 2017 Secure Decisions, a division of Applied Visions, Inc.
//

using System;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using OpenCover.Framework;

namespace OpenCover.Test.Framework.Symbols
{
    public abstract class BaseMdbTests
    {
        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            var assemblyPath = Path.GetDirectoryName(typeof(Microsoft.Practices.ServiceLocation.ServiceLocator).Assembly.Location);

            var folder = Path.Combine(assemblyPath, "Mdb");
            var source = Path.Combine(assemblyPath, "Microsoft.Practices.ServiceLocation.dll");
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            Directory.CreateDirectory(folder);
            var dest = Path.Combine(folder, "Microsoft.Practices.ServiceLocation.dll");
            File.Copy(source, dest);

            var currentDomainBaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            File.Copy(Path.Combine(currentDomainBaseDirectory, "Microsoft.Practices.ServiceLocation.pdb"), Path.ChangeExtension(dest, "pdb"));

            var process = new ProcessStartInfo
            {
                FileName = Path.Combine(currentDomainBaseDirectory, @"..\..\packages\Mono.pdb2mdb.0.1.0.20130128\tools\pdb2mdb.exe"),
                Arguments = dest,
                WorkingDirectory = folder,
                CreateNoWindow = true,
                UseShellExecute = false,
            };

            var proc = Process.Start(process);
            proc.Do(_ => _.WaitForExit());

            Assert.IsTrue(File.Exists(dest + ".mdb"));
            File.Delete(Path.ChangeExtension(dest, "pdb"));
            Assert.IsFalse(File.Exists(Path.ChangeExtension(dest, "pdb")));
        }
    }
}