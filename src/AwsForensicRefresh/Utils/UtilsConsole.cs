using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AwsForensicRefresh.Utils
{
    public class UtilsConsole
    {
        public static string AskQuestion(string message, string defaultvalue="")
        {
            string outputmessage = "";
            if (defaultvalue.Length > 0)
                outputmessage = $"{message}? [{defaultvalue}]";
            else
                outputmessage = $"{message}? ";
            
            Console.WriteLine(outputmessage);
            Console.WriteLine();
            string result = Console.ReadLine();

            if (result.Length < 1)
                return defaultvalue;
            else
                return result;
        }
        
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

            Console.WriteLine();
            return (response == ConsoleKey.Y);
        }

        public static string ChooseOption(string message, List<string> allowedKeys)
        {
            Console.WriteLine(message);
            string consoleKey;
            while(true)
            {
                consoleKey = Console.ReadKey(false).Key.ToString().ToLower();

                if (consoleKey == "N" || consoleKey == "n")
                    return "N";

                if (consoleKey.Length == 2 && consoleKey.StartsWith("d"))
                    consoleKey = consoleKey.Substring(1);
                
                if (allowedKeys.Contains(consoleKey.ToString()))
                    break;
            }
            Console.WriteLine();
            return consoleKey;
        }

    }
}
