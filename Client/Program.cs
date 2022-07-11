using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Client
{
    class Program
    {
 
        static void Main(string[] args)
        {
            FileClient client = new FileClient();
            //Ввод параметров клиента
            client.InputArgumentsConsole();
            //Запуск клиента
            client.StartClient();

        }



    }
}
