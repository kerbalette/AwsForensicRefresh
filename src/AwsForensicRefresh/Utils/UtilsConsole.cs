﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwsForensicRefresh.Utils
{
    public class UtilsConsole
    {
        public static bool Confirm(string message)
        {
            ConsoleKey response;
            do
            {
                Console.Write($"{message} [y/n] ");
                response = Console.ReadKey(false).Key;
                if (response != ConsoleKey.Enter)
                    Console.WriteLine();
            } while (response != ConsoleKey.Y && response != ConsoleKey.N);

            return (response == ConsoleKey.Y);
        }

        public static string ChooseOption(string message, List<string> allowedKeys)
        {
            Console.WriteLine(message);
            string consoleKey;
            while(true)
            {
                consoleKey = Console.ReadKey(false).Key.ToString().ToLower();

                if (consoleKey.Length == 2 && consoleKey.StartsWith("d"))
                    consoleKey = consoleKey.Substring(1);
                
                if (allowedKeys.Contains(consoleKey.ToString()))
                    break;
            }
            return consoleKey;
        }

    }
}
