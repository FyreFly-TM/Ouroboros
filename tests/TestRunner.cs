using System;
using System.Threading.Tasks;
using Ouroboros.Testing;

namespace Ouroboros.Tests
{
    public class TestRunner
    {
        public static async Task<int> Main(string[] args)
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║            OUROBOROS TEST SUITE RUNNER                    ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");

            // Run all tests in the current assembly
            var exitCode = await TestFramework.RunAllTests();

            if (exitCode == 0)
            {
                Console.WriteLine("\n✅ All tests passed!");
            }
            else
            {
                Console.WriteLine("\n❌ Some tests failed.");
            }

            return exitCode;
        }
    }
} 