using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Client
{


    public class FileClient
    {
        // адрес и порт сервера, к которому будем подключаться
        private string address;
        private int port; 
        private int portUDP;
        private string path;
        private int timeout;

        private string fileName;
        private List<byte> bufferfile;
        public FileClient()
        {
            bufferfile = new List<byte>();
        }
        public FileClient(string ip, int port, int portUdp, string path, int timeout)
        {
            bufferfile = new List<byte>();
            address = ip;
            this.port = port;
            this.portUDP = portUdp;
            this.path = path;
            this.timeout = timeout;
        }

        public void StartClient()
        {
            try
            {
                TcpClient client = new TcpClient();
                client.Connect(address, port);

                NetworkStream stream = client.GetStream();

                //Загрузка файла в память
                FileLoading();

                //Отправление через TCP информации о порте udp серверу
                SendMessageAndGetResponse(stream, Convert.ToString(portUDP));

                //Отправление через TCP информации о названии файла
                SendMessageAndGetResponse(stream, fileName);

                //Отправка файла через UDP
                SendFileUdp(stream);

                //Отправление через TCP информации о завершении передачи файла
                SendingMessage(stream, "Передача данных завершена!");

                Console.WriteLine("Файл успешно отправлен!");

                // Закрываем потоки
                stream.Close();
                client.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        //Инициализация аргументов
        public void InputArguments()
        {
            while (true)
            {
                try
                {
                    address = TextToConsoleIsReadline("Введите Ip");
                    port = Convert.ToInt32(TextToConsoleIsReadline("Введите порт"));
                    portUDP = Convert.ToInt32(TextToConsoleIsReadline("Введите порт UDP"));
                    path = TextToConsoleIsReadline("Введите путь к файлу");
                    timeout = Convert.ToInt32(TextToConsoleIsReadline("Введите timeout"));
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

        }

        //Принимает данные через TCP от клиента, переводит их в string и возвращает
        private string TextToConsoleIsReadline(string text)
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

        //Подругзка файла
        private void FileLoading()
        {
            Thread file = new Thread(() =>
            {
                FileInfo fi = new FileInfo(path);
                fileName = fi.Name;
                bufferfile = File.ReadAllBytes($"{path}").ToList();
                if (bufferfile.Count > 10485760)
                {
                    throw new Exception("Файл больше 10МБ!");
                }
            });
            file.Start();
        }

        //Кодирует строку и отправляет её через TCP 
        private void SendingMessage(NetworkStream stream, string text)
        {
            byte[] data = Encoding.UTF8.GetBytes(text);
            stream.Write(data, 0, data.Length);
        }

        //Принимает данные через TCP , переводит их в string и возвращает
        private string ReceivingMessage(NetworkStream stream)
        {
            StringBuilder response = new StringBuilder();
            byte[] data = new byte[256];
            do
            {
                int bytes = stream.Read(data, 0, data.Length);
                response.Append(Encoding.UTF8.GetString(data, 0, bytes));
            }
            while (stream.DataAvailable); // пока данные есть в потоке
            return Convert.ToString(response);
        }

        //Отправка сообещния(text) на сервер до получения подтверждения
        private void SendMessageAndGetResponse(NetworkStream stream, string text)
        {
            string response = null; // Переменная для отклика о принятии данных
            do
            {

                SendingMessage(stream, Convert.ToString(text));
                Thread.Sleep(timeout);

                response = ReceivingMessage(stream);
                Console.WriteLine(response);

            } while (response == null);
        }

        //Отправка файла через UDP с подтверждением через TCP
        private void SendFileUdp(NetworkStream stream)
        {
            UdpClient clientUdp = new UdpClient();

            long indexbytes = 0;//id для датаграмм 
            int limitedbyte = 59992;//Лимит передачи байтов 

             
            //Через цикл файл делится на части, не больше limitedbyte
            for (int i = 0; i < bufferfile.Count; i += limitedbyte)
            {
                //Контейнер байтов для отправки данных через UDP 
                List<byte> indexBufferfile = new List<byte>();

                //Добавление indexbytes в начало контейнера (Первые 8 байт)
                indexBufferfile.AddRange(BitConverter.GetBytes(indexbytes).ToList());

                //в indexBufferfile записывается часть файла bufferfile (размером не больше limitedbyte + 8)
                if ((i + limitedbyte) > bufferfile.Count)
                {
                    indexBufferfile.AddRange(bufferfile.GetRange(i, bufferfile.Count - i));
                }
                else
                {
                    indexBufferfile.AddRange(bufferfile.GetRange(i, limitedbyte));
                }


                long response = -1; //отклик с сервера

                //Файл отправляется на сервер, пока не будет получен ответ(В ответе id датаграммы) через TCP
                do
                {
                    //Отправление файла
                    int numberOfSentBytes = clientUdp.Send(indexBufferfile.ToArray(), indexBufferfile.Count, address, portUDP);
                    Console.WriteLine($"Отправлено байт:{numberOfSentBytes} ID:{indexbytes}");

                    //timeout 
                    Thread.Sleep(timeout);

                    //Ответ TCP от сервера
                    response = Convert.ToInt64( ReceivingMessage(stream));
                    Console.WriteLine($"Отклик получен ID:{response}");
                } while (response != indexbytes);
                indexbytes++;
            }
        }
    }
}
