using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public abstract class ConsoleHelper
    {


        protected void CheckErorrsAndRepeat(Action action)
        {
            while (true)
            {
                try
                {
                    action();
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

        }
        //Вывод текста в консоль + считывание текста и после возврат в string
        protected string TextToConsoleIsReadline(string text)
        {
            string enter;
            Console.Write($"{text}:");
            enter = Console.ReadLine();
            if (enter == null)
            {
                throw new Exception("Введено некорректное значение!");
            }
            return enter;
        }
    }
}
