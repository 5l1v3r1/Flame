﻿using System;
using System.Collections.Generic;
using Loyc;
using Loyc.Collections;
using Loyc.MiniTest;
using System.Diagnostics;
using Loyc.Syntax;

namespace UnitTests
{
    // Test driver based on Loyc project: https://github.com/qwertie/ecsharp/blob/master/Core/Tests/Program.cs

    public class Program
    {
        public static readonly List<Pair<string, Func<int>>> Menu = new List<Pair<string, Func<int>>>()
        {
            new Pair<string,Func<int>>("Run unit tests of Flame.dll", Flame)
        };

        public static void Main(string[] args)
        {
            // Workaround for MS bug: Assert(false) will not fire in debugger
            Debug.Listeners.Clear();
            Debug.Listeners.Add(new DefaultTraceListener());
            if (RunMenu(Menu, args.Length > 0 ? args[0].GetEnumerator() : null) > 0)
                // Let the outside world know that something went wrong (e.g. Travis CI)
                Environment.ExitCode = 1;
        }

        public static int RunMenu(List<Pair<string, Func<int>>> menu, IEnumerator<char> input)
        {
            int errorCount = 0;
            for (;;)
            {
                Console.WriteLine();
                Console.WriteLine("What do you want to do? (Esc to quit)");
                for (int i = 0; i < menu.Count; i++)
                    Console.WriteLine(PrintHelpers.HexDigitChar(i + 1) + ". " + menu[i].Key);
                Console.WriteLine("Space. Run all tests");

                char c = default(char);
                if (input == null)
                {
                    for (ConsoleKeyInfo k; (k = Console.ReadKey(true)).Key != ConsoleKey.Escape
                        && k.Key != ConsoleKey.Enter;)
                    {
                        c = k.KeyChar;
                        break;
                    }
                }
                else
                {
                    if (!input.MoveNext())
                        break;

                    c = input.Current;
                }

                if (c == ' ')
                {
                    for (int i = 0; i < menu.Count; i++)
                    {
                        Console.WriteLine();
                        ConsoleMessageSink.WriteColoredMessage(ConsoleColor.White, i + 1, menu[i].Key);
                        errorCount += menu[i].Value();
                    }
                }
                else
                {
                    int i = ParseHelpers.HexDigitValue(c);
                    if (i > 0 && i <= menu.Count)
                        errorCount += menu[i - 1].Value();
                }
            }
            return errorCount;
        }

        private static Random globalRng = new Random();

        public static int Flame()
        {
            return RunTests.RunMany(
                new CacheTests(globalRng),
                new IntegerConstantTests(),
                new QualifiedNameTests(),
                new SmallMultiDictionaryTests(),
                new TypeConstructionTests(globalRng),
                new ValueListTests());
        }
    }
}
