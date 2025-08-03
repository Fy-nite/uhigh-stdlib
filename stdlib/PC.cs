namespace stdlib
{
    public class PC
    {
        /// <summary>
        /// Prints the line using the specified value
        /// </summary>
        /// <param name="value">The value</param>
        public static void Exit(int exitcode = 0)
        {
            //TODO: replace with a proper language based exit method during conversion
            Environment.Exit(exitcode); // lets hope this works
        }
    }
}