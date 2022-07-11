using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace Server
{
    public abstract class DataExchange : ConsoleHelper
    {

        //кодирует строку text, и отправляет через объект NetworkStream
        public void SendingMessage(NetworkStream stream, string text)
        {
            byte[] data = Encoding.UTF8.GetBytes(text);
            stream.Write(data, 0, data.Length);
        }

        //Принимает данные через объект NetworkStream, переводит в string и возвращает
        public string ReceivingMessage(NetworkStream stream)
        {
            StringBuilder response = new StringBuilder();
            byte[] data = new byte[256];
            do
            {
                int bytes = stream.Read(data, 0, data.Length);
                response.Append(Encoding.UTF8.GetString(data, 0, bytes));
            }
            while (stream.DataAvailable);

            return response.Length == 0 ? null : Convert.ToString(response);
        }

    }

}
