using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Server
{
    public class FileServer
    {
        private string ipAddress;
        private int port;
        private string katalog; // Каталог для хранения файлов
        private int remotePortUdp;
        private string fileName;

        //флаг для выхода из цикла получения данных UDP
        private bool flagEnd;
        
        public FileServer() {    }
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

                    // получаем сетевой поток для чтения и записи 
                    NetworkStream stream = client.GetStream();

                    //Принимаем данные через TCP
                    remotePortUdp = Convert.ToInt32(ReceivingMessage(stream));

                    //Отправляем подтверждение через TCP для клиента что данные получены
                    SendingMessage(stream, "Отклик: Порт принят");

                    fileName = Convert.ToString(ReceivingMessage(stream));
                    SendingMessage(stream, "Отклик: Название принято");

                    Console.Write($"Порт UDP получен:{remotePortUdp}\n" +
                        $"Имя файла получено:{fileName}\n");

                    //UDP
                    UdpClient receiver = new UdpClient(remotePortUdp);
                    IPEndPoint remoteIp = null;

                    //контейнер байтов для записи данных от клиента
                    List<byte> fileBytes = new List<byte>();

                    // Переменная для индексирования получаемых данных UDP
                    long indexbytes;

                    flagEnd = true;//Флаг для цикла принятия данных от клиента, он связан с MessageToEndTcp
                                   //после ответа клиента о конце передачи файла flagEnd = false

                    //Просмотр сообщения через TCP от клиента
                    MessageToEndTcp(stream);

                    //Получение данных через UDP с подтверждением TCP 
                    do
                    {
                        if (receiver.Available > 0)
                        {
                            List<byte> bufferByte = new List<byte>();
                            //Получение данных
                            bufferByte.AddRange(receiver.Receive(ref remoteIp).ToList());

                            //Id датаграммы
                            indexbytes = BitConverter.ToInt64(bufferByte.GetRange(0, 8).ToArray());

                            //Добавление данных в контейнер(без id датаграммы)
                            fileBytes.AddRange(bufferByte.GetRange(8, bufferByte.Count - 8));

                            //Подтверждение получения через TCP (Отправление id датаграммы)
                            SendingMessage(stream, indexbytes.ToString() );

                            Console.WriteLine($"Принято байт:{bufferByte.Count} ID:{indexbytes}");

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
        public void VariableInitializationConsole()
        {
            while (true)
            {
                try
                {
                    ipAddress = TextToConsoleIsReadline("Введите ip");
                    port = Convert.ToInt32(TextToConsoleIsReadline("Введите port"));
                    katalog = TextToConsoleIsReadline("Введите каталог для хранения файлов");
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        //Вывод текста в консоль + считывание текста и после возврат в string
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
            while (stream.DataAvailable);
            return Convert.ToString(response);
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
