using System;
using System.Net;
using System.Net.Sockets;
using System.Text;




namespace Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            FileServer server = new FileServer();
            //Ввод параметров сервера
            server.InputArgumentsConsole();
            //Запуск сервера
            server.StartServer();
        }



    }
}
