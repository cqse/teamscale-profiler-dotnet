namespace SampleDebuggingApp
{
    /// <summary>
    /// The debugging app. Does nothing but print out a message once in a while.
    /// </summary>
    public class DebuggingApp
    {
        /// <summary>
        ///  Main entry point for the debugging app.
        /// </summary>
        public static void Main()
        {
            Console.WriteLine("Hello, World!");
            while (true)
            {
                Thread.Sleep(10000);
                TestMethod();
            }
        }

        private static void TestMethod()
        {
            Console.WriteLine($"TestMethod called.");
        }
    }
}
