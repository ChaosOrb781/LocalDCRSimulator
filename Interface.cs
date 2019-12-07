using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalDCRSimulator
{
    class Interface
    {
        public static int GetIntegerInput(string request = null, ConsoleColor color = ConsoleColor.White)
        {
            int output = -1;
            bool success = false;
            while (!success) {
                if (request != null)
                {
                    Console.Write(request, color);
                }
                string input = Console.ReadLine();
                success = Int32.TryParse(input, out output);
                if (!success)
                    Warning("Expected integer input");
            }
            return output;
        }

        public static string GetStringInput(string request = null, ConsoleColor color = ConsoleColor.White)
        {
            if (request != null)
            {
                Console.Write(request, color);
            }
            return Console.ReadLine();
        }

        public static void Warning(string s)
        {
            ConsoleColor prev = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Warning: " + s);
            Console.ForegroundColor = prev;
        }

        public static void WriteLine(string s, ConsoleColor consoleColor = ConsoleColor.White)
        {
            Console.ForegroundColor = consoleColor;
            Console.WriteLine(s);
        }
        
        public static void Wait()
        {
            Console.Write("Waiting for keypress...");
            Console.ReadKey();
        }
    }
}
