namespace SampleDebuggingApp
{
    /// <summary>
    /// Main entry of the debugging app.
    /// </summary>
    public class DebuggingApp
    {
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
