using System.Diagnostics;
using System.Reflection;

namespace StdLib
{
    // Attribute to mark test-only classes
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class TestingOnlyAttribute : Attribute { }

    // Attribute to specify test data for a test method
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class TestWithAttribute : Attribute
    {
        public Type InputType { get; }
        public TestWithAttribute(Type inputType)
        {
            InputType = inputType;
        }
    }

    // Attribute to specify expected value for a variable
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
    public class ExpectAttribute : Attribute
    {
        public object Expected { get; }
        public ExpectAttribute(object expected)
        {
            Expected = expected;
        }
    }

    // Attribute to specify that a test is expected to throw a specific exception
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class ExpectExceptionAttribute : Attribute
    {
        public Type ExceptionType { get; }
        public ExpectExceptionAttribute(Type exceptionType)
        {
            ExceptionType = exceptionType;
        }
    }

    // Attribute to specify expected return value and arguments for a function call
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class CallAttribute : Attribute
    {
        public object Expected { get; }
        public object[] Args { get; }
        public CallAttribute(object expected, params object[] args)
        {
            Expected = expected;
            Args = args ?? Array.Empty<object>();
        }
    }

    // Attribute to specify a timeout (in milliseconds) for a test method
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class TimeoutAttribute : Attribute
    {
        public int Milliseconds { get; }
        public TimeoutAttribute(int milliseconds)
        {
            Milliseconds = milliseconds;
        }
    }

    // Attribute to categorize/tag tests
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class TestCategoryAttribute : Attribute
    {
        public string Category { get; }
        public TestCategoryAttribute(string category) => Category = category;
    }

    // Attribute to ignore/skip a test
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class IgnoreAttribute : Attribute { }

    // Attribute to mark setup method (run before each test in class)
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class SetupAttribute : Attribute { }

    // Attribute to mark cleanup method (run after each test in class)
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class CleanupAttribute : Attribute { }

    // Attribute to specify a custom comparer for result/field comparison
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
    public class CustomComparerAttribute : Attribute
    {
        public Type ComparerType { get; }
        public CustomComparerAttribute(Type comparerType) => ComparerType = comparerType;
    }

    // Attribute to specify expected console output for a test method
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class TestOutputAttribute : Attribute
    {
        public string ExpectedOutput { get; }
        public TestOutputAttribute(string expectedOutput) => ExpectedOutput = expectedOutput;
    }

    // Simple test runner (skeleton)
    public static class InlineTestRunner
    {
        public static void RunAllTests()
        {
            var mainAssembly = Assembly.GetExecutingAssembly();
            var testClasses = mainAssembly
                .GetTypes()
                .Where(t => t.IsClass && t.IsPublic && !t.IsAbstract && t.Assembly == mainAssembly);

            RunTestsOnClasses(testClasses);
        }

        public static void RunAllTests(Type testClass)
        {
            RunTestsOnClasses(new[] { testClass });
        }

        private static void RunTestsOnClasses(IEnumerable<Type> testClasses)
        {
            int total = 0, passed = 0, failed = 0;
            foreach (var cls in testClasses)
            {
                // Find setup/cleanup methods
                var setupMethod = cls.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .FirstOrDefault(m => m.GetCustomAttributes(typeof(SetupAttribute), false).Any());
                var cleanupMethod = cls.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .FirstOrDefault(m => m.GetCustomAttributes(typeof(CleanupAttribute), false).Any());

                foreach (var method in cls.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                {
                    // Only consider public methods
                    if (!method.IsPublic) continue;

                    // Skip ignored tests
                    if (method.GetCustomAttributes(typeof(IgnoreAttribute), false).Any())
                        continue;

                    // Get timeout attribute if present
                    var timeoutAttr = (TimeoutAttribute)method.GetCustomAttributes(typeof(TimeoutAttribute), false).FirstOrDefault();

                    // Get custom comparer if present
                    var customComparerAttr = (CustomComparerAttribute)method.GetCustomAttributes(typeof(CustomComparerAttribute), false).FirstOrDefault();
                    System.Collections.IEqualityComparer comparer = null;
                    if (customComparerAttr != null)
                        comparer = (System.Collections.IEqualityComparer)Activator.CreateInstance(customComparerAttr.ComparerType);

                    // Handle [Call] attribute
                    var callAttrs = method.GetCustomAttributes(typeof(CallAttribute), false);
                    foreach (CallAttribute callAttr in callAttrs)
                    {
                        total++;
                        var instance = method.IsStatic ? null : Activator.CreateInstance(cls);

                        // Run setup
                        setupMethod?.Invoke(instance, null);

                        // Capture console output if needed
                        var testOutputAttr = (TestOutputAttribute)method.GetCustomAttributes(typeof(TestOutputAttribute), false).FirstOrDefault();
                        var originalOut = Console.Out;
                        StringWriter outputWriter = null;
                        if (testOutputAttr != null)
                        {
                            outputWriter = new StringWriter();
                            Console.SetOut(outputWriter);
                        }

                        var sw = Stopwatch.StartNew();
                        bool testPassed = false;
                        Exception thrown = null!;
                        try
                        {
                            var result = method.Invoke(instance, callAttr.Args);
                            sw.Stop();

                            // Restore console output
                            if (testOutputAttr != null)
                            {
                                Console.SetOut(originalOut);
                                var actualOutput = outputWriter.ToString();
                                if (actualOutput.Trim() != testOutputAttr.ExpectedOutput.Trim())
                                {
                                    PrintFail($"{cls.Name}.{method.Name}: Expected output \"{testOutputAttr.ExpectedOutput}\", but got \"{actualOutput}\"");
                                    continue;
                                }
                            }

                            if (timeoutAttr != null && sw.ElapsedMilliseconds > timeoutAttr.Milliseconds)
                            {
                                PrintFail($"{cls.Name}.{method.Name}: Timeout exceeded ({sw.ElapsedMilliseconds} ms > {timeoutAttr.Milliseconds} ms)");
                            }
                            else if (comparer != null)
                            {
                                if (comparer.Equals(result, callAttr.Expected))
                                {
                                    PrintPass($"{cls.Name}.{method.Name}: Returned expected value (custom comparer)");
                                    testPassed = true;
                                }
                                else
                                {
                                    PrintFail($"{cls.Name}.{method.Name}: Expected {callAttr.Expected}, but got {result} (custom comparer)");
                                }
                            }
                            else if (object.Equals(result, callAttr.Expected))
                            {
                                PrintPass($"{cls.Name}.{method.Name}: Returned expected value {callAttr.Expected}");
                                testPassed = true;
                            }
                            else
                            {
                                PrintFail($"{cls.Name}.{method.Name}: Expected {callAttr.Expected}, but got {result}");
                            }
                        }
                        catch (TargetInvocationException ex)
                        {
                            sw.Stop();
                            thrown = ex.InnerException!;
                            PrintFail($"{cls.Name}.{method.Name}: Exception thrown: {thrown}");
                        }
                        catch (Exception ex)
                        {
                            sw.Stop();
                            PrintFail($"{cls.Name}.{method.Name}: Exception thrown: {ex}");
                        }
                        Console.WriteLine($"    Time: {sw.ElapsedMilliseconds} ms");
                        if (testPassed) passed++; else failed++;

                        // Run cleanup
                        cleanupMethod?.Invoke(instance, null);
                    }

                    // Handle [TestWith] attribute
                    var testWithAttrs = method.GetCustomAttributes(typeof(TestWithAttribute), false);
                    foreach (TestWithAttribute attr in testWithAttrs)
                    {
                        total++;
                        var input = Activator.CreateInstance(attr.InputType);
                        var instance = method.IsStatic ? null : Activator.CreateInstance(cls);

                        // Run setup
                        setupMethod?.Invoke(instance, null);

                        var expectExceptionAttr = (ExpectExceptionAttribute)method.GetCustomAttributes(typeof(ExpectExceptionAttribute), false).FirstOrDefault()!;
                        bool testPassed = false;
                        Exception thrown = null!;
                        var sw = Stopwatch.StartNew();
                        try
                        {
                            method.Invoke(instance, new object[] { input! });
                            sw.Stop();
                            if (timeoutAttr != null && sw.ElapsedMilliseconds > timeoutAttr.Milliseconds)
                            {
                                PrintFail($"{cls.Name}.{method.Name}: Timeout exceeded ({sw.ElapsedMilliseconds} ms > {timeoutAttr.Milliseconds} ms)");
                            }
                            else if (expectExceptionAttr != null)
                            {
                                PrintFail($"{cls.Name}.{method.Name}: Expected exception {expectExceptionAttr.ExceptionType.Name} but none was thrown");
                            }
                            else
                            {
                                var expectFields = cls.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                                    .Where(f => f.GetCustomAttributes(typeof(ExpectAttribute), false).Any());
                                bool allFieldsPassed = true;
                                foreach (var field in expectFields)
                                {
                                    var expectAttr = (ExpectAttribute)field.GetCustomAttributes(typeof(ExpectAttribute), false).FirstOrDefault()!;
                                    var fieldComparerAttr = (CustomComparerAttribute)field.GetCustomAttributes(typeof(CustomComparerAttribute), false).FirstOrDefault();
                                    System.Collections.IEqualityComparer fieldComparer = null;
                                    if (fieldComparerAttr != null)
                                        fieldComparer = (System.Collections.IEqualityComparer)Activator.CreateInstance(fieldComparerAttr.ComparerType);

                                    if (expectAttr != null)
                                    {
                                        var actualValue = field.GetValue(instance);
                                        bool equals = fieldComparer != null
                                            ? fieldComparer.Equals(actualValue, expectAttr.Expected)
                                            : object.Equals(actualValue, expectAttr.Expected);

                                        if (!equals)
                                        {
                                            PrintFail($"Test failed in {cls.Name}.{method.Name}: Expected {expectAttr.Expected}, but got {actualValue} for field {field.Name}");
                                            allFieldsPassed = false;
                                        }
                                        else
                                        {
                                            PrintPass($"Test passed in {cls.Name}.{method.Name}: Field {field.Name} has expected value {expectAttr.Expected}");
                                        }
                                    }
                                }
                                if (allFieldsPassed || !expectFields.Any())
                                {
                                    testPassed = true;
                                }
                            }
                        }
                        catch (TargetInvocationException ex)
                        {
                            sw.Stop();
                            thrown = ex.InnerException!;
                            if (expectExceptionAttr != null && thrown != null && expectExceptionAttr.ExceptionType.IsInstanceOfType(thrown))
                            {
                                PrintPass($"{cls.Name}.{method.Name}: Threw expected exception {thrown.GetType().Name}");
                                testPassed = true;
                            }
                            else
                            {
                                PrintFail($"{cls.Name}.{method.Name}: Unexpected exception: {thrown}");
                            }
                        }
                        catch (Exception ex)
                        {
                            sw.Stop();
                            PrintFail($"{cls.Name}.{method.Name}: Unexpected exception: {ex}");
                        }
                        Console.WriteLine($"    Time: {sw.ElapsedMilliseconds} ms");
                        if (testPassed) passed++; else failed++;

                        // Run cleanup
                        cleanupMethod?.Invoke(instance, null);
                    }
                }
            }
            Console.WriteLine();
            Console.WriteLine("==== Test Summary ====");
            Console.WriteLine($"Total: {total}, Passed: {passed}, Failed: {failed}");
        }

        static void PrintPass(string msg)
        {
            if (ConsoleIsColor())
            {
                var old = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(msg);
                Console.ForegroundColor = old;
            }
            else
            {
                Console.WriteLine(msg);
            }
        }

        static void PrintFail(string msg)
        {
            if (ConsoleIsColor())
            {
                var old = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(msg);
                Console.ForegroundColor = old;
            }
            else
            {
                Console.WriteLine(msg);
            }
        }

        static bool ConsoleIsColor()
        {
            // try { return Console.ForegroundColor! != null!; }
            // catch { return false; }
            return true; // Assume color support for simplicity
        }
    }
}