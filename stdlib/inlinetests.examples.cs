// using System;
// using System.Collections;

// namespace StdLib.Examples
// {
//     public class IntArrayComparer : IEqualityComparer
//     {
//         public new bool Equals(object x, object y)
//         {
//             var a = x as int[];
//             var b = y as int[];
//             if (a == null || b == null || a.Length != b.Length) return false;
//             for (int i = 0; i < a.Length; i++)
//                 if (a[i] != b[i]) return false;
//             return true;
//         }
//         public int GetHashCode(object obj) => obj.GetHashCode();
//     }

//     public class tests
//     {
//         public int Value;

//         [Setup]
//         public void Setup() { Value = 42; }

//         [Cleanup]
//         public void Cleanup() { Value = 0; }

//         [Call(84)]
//         [TestCategory("Simple")]
//         public int DoubleValue() => Value * 2;

//         [Call(0)]
//         [Ignore]
//         public int IgnoredTest() => 0;

//         [Call(100)]
//         [Timeout(10)]
//         public int FastTest() => 100;

//         [Call(new int[] { 1, 2, 3 }, 1, 2, 3)]
//         [CustomComparer(typeof(IntArrayComparer))]
//         public int[] ReturnArray(int a, int b, int c) => new[] { a, b, c };

//         [Call("Hello, World!")]
//         [TestOutput("Hello, World!")]
//         public string Echo()
//         {
//             Console.WriteLine("Hello, World!");
//             return "Hello, World!";
//         }

//         [TestWith(typeof(TestInput))]
//         [ExpectException(typeof(InvalidOperationException))]
//         public void ThrowsOnNegative(TestInput input)
//         {
//             if (input.Value < 0)
//                 throw new InvalidOperationException("Negative value");
//             Value = input.Value;
//         }
//         public int FieldToCheck;

//         public void SetField(TestInput input)
//         {
//             FieldToCheck = input.Value;
//         }
//     }

//     public class TestInput
//     {
//         public int Value = 123;
//     }
// }
