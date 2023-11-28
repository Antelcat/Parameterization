using System;
using System.ComponentModel;
using System.Globalization;
using Antelcat.Parameterization;

namespace Antelcat.Parameterization.Tests
{
    [Parameterization]
    public static partial class Program
    {
        public static void Main()
        {
            while (true)
            {
                try
                {
                    ExecuteInput(Console.ReadLine());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        [Command(ShortName = "e")]
        private static void Exit()
        {
            Environment.Exit(0);
        }

        [Command]
        private static void Command1(
            string arg1,
            [Argument(FullName = "index", ShortName = "i")]
            int arg2,
            DateTime arg3,
            double arg4 = 114.514d,
            [Argument(DefaultValue = "true")] bool arg5 = false,
            [Argument(Converter = typeof(CustomTypeConverter))]
            CustomType? arg6 = null,
            int arg7 = 0,
            [Argument(Converter = typeof(Antelcat.Parameterization.Tests.CustomTypeConverter))]
            CustomType2? arg8 = null)
        {
            Console.WriteLine($"Command1: {arg1}, {arg2}, {arg3}, {arg4}, {arg5}, {arg6}, {arg7}, {arg8}");
        }

        [Command(ShortName = "c")]
        private static void Command2([Argument(FullName = "test", ShortName = "t")] int myArg)
        {
            Console.WriteLine($"Command2: {myArg}");
        }

        private class CustomType
        {
            public string? Name { get; set; }
            public int Age { get; set; }

            public override string ToString()
            {
                return $"{Name} : {Age}";
            }
        }

        public class CustomTypeConverter : StringConverter
        {
            public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
            {
                if (value is not string str) return null;
                var parts = str.Split(',');
                return new CustomType
                {
                    Name = parts[0],
                    Age = int.Parse(parts[1])
                };
            }
        }
    }

    public class CustomType2
    {
        public string? Name { get; set; }
        public int Age { get; set; }

        public override string ToString()
        {
            return $"{Name} : {Age}";
        }
    }

    public class CustomTypeConverter : StringConverter
    {
        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
        {
            if (value is not string str) return null;
            var parts = str.Split(';');
            return new CustomType2
            {
                Name = parts[0],
                Age = int.Parse(parts[1])
            };
        }
    }
}