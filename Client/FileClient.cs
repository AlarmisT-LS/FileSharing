using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Server;

namespace Client
{


    public class FileClient : DataExchange
    {
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

        //Подругзка файла
        private void FileLoading()
        {
            Thread fileLoading = new Thread(() =>
            {
                FileInfo fi = new FileInfo(path);
                fileName = fi.Name;
                bufferfile = File.ReadAllBytes($"{path}").ToList();
                if (bufferfile.Count > 10485760)
                {
                    throw new Exception("Файл больше 10МБ!");
                }
            });
            fileLoading.Start();
        }

        //Ввод аргументов
        public void InputArgumentsConsole()
        {
            CheckErorrsAndRepeat(() => {
                address = TextToConsoleIsReadline("Введите Ip");
                port = Convert.ToInt32(TextToConsoleIsReadline("Введите порт"));
                portUDP = Convert.ToInt32(TextToConsoleIsReadline("Введите порт UDP"));
                path = TextToConsoleIsReadline("Введите путь к файлу");
                timeout = Convert.ToInt32(TextToConsoleIsReadline("Введите timeout"));
            });
        }

        //Отправка сообещния(text) на сервер до получения отклика
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
            int limitedbyte = 60000;//Лимит передачи байтов 

             
            //Через цикл файл делится на части, не больше limitedbyte
            for (int i = 0; i < bufferfile.Count; i += limitedbyte)
            {
                //объект для отправки данных через UDP 
                DataPackage package;

                //в package записывается часть файла bufferfile (размером не больше limitedbyte)
                if ((i + limitedbyte) > bufferfile.Count)
                {
                    package = new DataPackage(indexbytes, bufferfile.GetRange(i, bufferfile.Count - i));
                }
                else
                {
                    package = new DataPackage(indexbytes, bufferfile.GetRange(i, limitedbyte));
                }
                
                long response = -1; //Условие для цикла,

                //Файл отправляется на сервер, пока не будет получен ответ(В ответе id датаграммы) через TCP
                do
                {
                    //Отправление файла
                    int numberOfSentBytes = clientUdp.Send(package.GetPackage().ToArray(), package.Count, address, portUDP);
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
