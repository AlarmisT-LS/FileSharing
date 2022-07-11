using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Server
{
    public class FileServer : DataExchange
    {
        private string ipAddress;
        private int port;
        private string katalog; // Каталог для хранения файлов
        private int remotePortUdp;
        private string fileName;

        //флаг для выхода из цикла получения данных UDP
        private bool flagEnd;
        
        public FileServer() {   }
        public FileServer(string ip, int port, string path)
        {
            ipAddress = ip;
            this.port = port;
            katalog = path;
        }

        public void StartServer()
        {
            TcpListener server = null;
            try
            {

                IPAddress Addr = IPAddress.Parse(ipAddress);
                server = new TcpListener(Addr, port);
                server.Start();

                while (true)
                {
                    Console.WriteLine("Поиск клиента...");
                    // получаем входящее подключение
                    TcpClient client = server.AcceptTcpClient();

                    // получаем сетевой поток
                    NetworkStream stream = client.GetStream();

                    //Принимаем данные через TCP
                    remotePortUdp = Convert.ToInt32(ReceivingMessage(stream));
                    //Отправляем подтверждение через TCP для клиента что данные получены
                    SendingMessage(stream, "Отклик: Порт принят");

                    fileName = ReceivingMessage(stream);
                    SendingMessage(stream, "Отклик: Название принято");


                    Console.Write($"Порт UDP получен:{remotePortUdp}\n" +
                        $"Имя файла получено:{fileName}\n");

                    //UDP
                    UdpClient receiver = new UdpClient(remotePortUdp);
                    IPEndPoint remoteIp = null;

                    //контейнер байтов для записи файла
                    List<byte> fileBytes = new List<byte>();

                    flagEnd = true;//Флаг для цикла принятия данных от клиента, он связан с MessageToEndTcp
                                   //после ответа клиента о конце передачи файла flagEnd = false

                    //Просмотр сообщения через TCP от клиента
                    MessageToEndTcp(stream);

                    //Получение данных через UDP с подтверждением TCP 
                    do
                    {
                        if (receiver.Available > 0)
                        {
                            //package принимает через конструктор массив байт UDP
                            DataPackage package = new DataPackage(receiver.Receive(ref remoteIp));

                            //Добавление данных в контейнер байт файла
                            fileBytes.AddRange(package.Data);

                            //Отправка отклика через TCP о получении UDP датаграммы (Отправление id датаграммы)
                            SendingMessage(stream, package.Id.ToString());

                            Console.WriteLine($"Принято байт:{package.Count} ID:{package.Id}");
                        }
                    } while (flagEnd);

                    Console.WriteLine("Передача данных завершена!");

                    //Создание файла в каталоге
                    File.WriteAllBytes($"{katalog}\\{fileName}", fileBytes.ToArray());

                    Console.WriteLine("Файл создан!");

                    receiver.Close();
                    client.Close();
                    stream.Close();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
            finally
            {
                if (server != null)
                    server.Stop();
            }
        }

        //Инициализация параметров сервера через консоль
        public void InputArgumentsConsole()
        {
            CheckErorrsAndRepeat( () =>
            {
                ipAddress = TextToConsoleIsReadline("Введите ip");
                port = Convert.ToInt32(TextToConsoleIsReadline("Введите port"));
                katalog = TextToConsoleIsReadline("Введите каталог для хранения файлов");
            });
        }

        //Ожидает подтверждение через TCP от клиента об завершении передачи файла 
        private void MessageToEndTcp(NetworkStream stream)
        {
            object locker = new();
            Thread end = new Thread(() => {
                lock (locker)
                {
                    string text = Convert.ToString(ReceivingMessage(stream));
                    if (text == "Передача данных завершена!")
                    {
                        flagEnd = false;
                    }
                }

            });
            end.Start();
        }
    }
}
