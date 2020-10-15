using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;

namespace Lab1
{
    public class ServerObject
    {
        static TcpListener tcpListener; // сервер для прослушивания
        List<ClientObject> clients = new List<ClientObject>(); // все подключения
        public ServerObject()
        {            
            if (!File.Exists("data.json"))
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Technical));
                using (FileStream file = new FileStream("data.json", FileMode.Create))
                {
                    serializer.WriteObject(file, new Technical("None", "None", 0, 0, 0));
                }
            }
        }
        protected internal void AddConnection(ClientObject clientObject)
        {
            clients.Add(clientObject);
        }
        protected internal void RemoveConnection(string id)
        {
            // получаем по id закрытое подключение
            ClientObject client = clients.FirstOrDefault(c => c.Id == id);
            // и удаляем его из списка подключений
            if (client != null)
                clients.Remove(client);
        }
        // прослушивание входящих подключений
        public void Listen()
        {
            try
            {
                tcpListener = new TcpListener(IPAddress.Any, 8888);
                tcpListener.Start();
                Console.WriteLine("Сервер запущен. Ожидание подключений...");

                while (true)
                {
                    TcpClient tcpClient = tcpListener.AcceptTcpClient();
                    ClientObject clientObject = new ClientObject(tcpClient, this);
                    Thread clientThread = new Thread(new ThreadStart(clientObject.Process));
                    clientThread.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Disconnect();
            }
        }

        // трансляция сообщения подключенным клиентам
        public void SendResult(Result result, string id)
        {
            string resultToSend = "";
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Result));
            using (FileStream file = new FileStream("result.json", FileMode.Create))
            {
                serializer.WriteObject(file, result);
            }
            using (StreamReader file = new StreamReader("result.json"))
            {
                resultToSend += file.ReadToEnd();
            }
            byte[] data = Encoding.Unicode.GetBytes(resultToSend.ToString());
            for (int i = 0; i < clients.Count; i++)
            {
                if (clients[i].Id == id) // если id клиента равно id отправляющего
                {
                    clients[i].Stream.Write(data, 0, data.Length); //передача данных
                }
            }
        }
        // отключение всех клиентов
        public void Disconnect()
        {
            tcpListener.Stop(); //остановка сервера

            for (int i = 0; i < clients.Count; i++)
            {
                clients[i].Close(); //отключение клиента
            }
            Environment.Exit(0); //завершение процесса
        }
        public Result MakeCommand(Command command)
        {
            List<Technical> list = new List<Technical>();
            Result result = new Result();
            result.command = command.command;
            Console.WriteLine(command.ToString());
            switch (command.command)
            {
                case Commands.Get:
                    {
                        Console.WriteLine("Выполнение получения данных...");
                        if (command.infoFirst.Equals(command.infoSecond) && command.infoFirst.Equals(""))
                        {
                            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Technical));
                            using (FileStream file = new FileStream("data.json", FileMode.Open))
                            {
                                Technical technical = null;
                                do
                                {
                                    technical = (Technical)serializer.ReadObject(file);
                                    list.Add(technical);
                                } while (technical != null);
                            }
                        }
                        else if (!command.infoFirst.Equals(""))
                        {
                            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Technical));
                            using (FileStream file = new FileStream("data.json", FileMode.Open))
                            {
                                Technical technical = null;
                                do
                                {
                                    technical = (Technical)serializer.ReadObject(file);
                                    if (technical.Cost == Convert.ToDouble(command.infoFirst))
                                    {
                                        list.Add(technical);
                                    }
                                } while (technical != null);
                            }
                        }
                        else
                        {
                            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Technical));
                            using (FileStream file = new FileStream("data.json", FileMode.Open))
                            {
                                Technical technical = null;
                                do
                                {
                                    technical = (Technical)serializer.ReadObject(file);
                                    list.Add(technical);
                                } while (technical != null);
                            }
                        }
                        result.outputInfo = list;
                        Console.WriteLine("Отправка данных.");
                        break;
                    }
                case Commands.Delete:
                    {
                        Console.WriteLine("Выполнение удаления данных...");
                        DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Technical));
                        using (FileStream file = new FileStream("data.json", FileMode.Open))
                        {
                            Technical technical = null;
                            do
                            {
                                technical = (Technical)serializer.ReadObject(file);
                                if (!technical.Name.Equals(command.infoFirst))
                                {
                                    list.Add(technical);
                                }
                            } while (technical != null);
                        }
                        using (FileStream file = new FileStream("data.json", FileMode.Create))
                        {
                            foreach (Technical technical in list)
                            {
                                serializer.WriteObject(file, technical);
                            }
                        }
                        result.outputInfo = "Deleted";
                        Console.WriteLine("Удаление успешно завершено.");
                        break;
                    }
                case Commands.Edit:
                    {
                        Console.WriteLine("Выполнение редактирования данных...");
                        DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Technical));
                        using (FileStream file = new FileStream("data.json", FileMode.Open))
                        {
                            Technical technical = null;
                            do
                            {
                                technical = (Technical)serializer.ReadObject(file);
                                if (technical.Name.Equals(command.infoFirst))
                                {
                                    string[] fields = command.infoSecond.Split(',').ToArray();
                                    technical = new Technical(fields[0], fields[1], Convert.ToDouble(fields[2]), Convert.ToDouble(fields[3]), Convert.ToDouble(fields[4]));
                                    list.Add(technical);
                                }
                            } while (technical != null);
                        }
                        using (FileStream file = new FileStream("data.json", FileMode.Create))
                        {
                            foreach (Technical technical in list)
                            {
                                serializer.WriteObject(file, technical);
                            }
                        }
                        result.outputInfo = "Edited";
                        Console.WriteLine("Редактирование успешно завершено.");
                        break;
                    }
                case Commands.Create:
                    {
                        Console.WriteLine("Выполнение добавления данных...");
                        DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Technical));
                        using (FileStream file = new FileStream("data.json", FileMode.Open))
                        {
                            Technical technical = null;
                            do
                            {
                                technical = (Technical)serializer.ReadObject(file);
                                list.Add(technical);
                            } while (technical != null);
                        }

                        string[] fields = command.infoSecond.Split(',').ToArray();
                        list.Add(new Technical(fields[0], fields[1], Convert.ToDouble(fields[2]), Convert.ToDouble(fields[3]), Convert.ToDouble(fields[4])));
                        using (FileStream file = new FileStream("data.json", FileMode.Create))
                        {
                            foreach (Technical technical in list)
                            {
                                serializer.WriteObject(file, technical);
                            }
                        }
                        result.outputInfo = "Created";
                        Console.WriteLine("Создание успешно завершено.");
                        break;
                    }
                default:
                    break;
            }
            Console.WriteLine(result.command + ";" + result.exitCode + ";" + result.outputInfo);
            return result;
        }
    }
    public class ClientObject
    {
        protected internal string Id { get; private set; }
        protected internal NetworkStream Stream { get; private set; }
        TcpClient client;
        ServerObject server; // объект сервера

        public ClientObject(TcpClient tcpClient, ServerObject serverObject)
        {
            Id = Guid.NewGuid().ToString();
            client = tcpClient;
            server = serverObject;
            serverObject.AddConnection(this);
        }

        public void Process()
        {
            try
            {
                Stream = client.GetStream();
                Console.WriteLine("Появилось новое подключение.");
                bool flag = true;
                Command command = new Command();
                while (flag)
                {
                    try
                    {
                        string line = GetMessage();
                        DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Command));
                        using (StreamWriter file = new StreamWriter("command.json", false))
                        {
                            file.Write(line);
                        }
                        using (FileStream file = new FileStream("command.json", FileMode.Create))
                        {
                            command = (Command)serializer.ReadObject(file);
                        }
                        Console.WriteLine(command.ToString());
                        Result result = server.MakeCommand(command);
                        server.SendResult(result, Id);
                    }
                    catch
                    {
                        
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                // в случае выхода из цикла закрываем ресурсы
                server.RemoveConnection(this.Id);
                Close();
            }
        }

        // чтение входящего сообщения и преобразование в строку
        private string GetMessage()
        {
            byte[] data = new byte[1048576]; // буфер для получаемых данных
            StringBuilder builder = new StringBuilder();
            int bytes = 0;
            do
            {
                bytes = Stream.Read(data, 0, data.Length);
                builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
            }
            while (Stream.DataAvailable);

            return builder.ToString();
            /*byte[] data = new byte[1048576];
            Stream.Read(data, 0, data.Length);
            Result result = new Result();
            string message = Encoding.Unicode.GetString(data);
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Result));
            using (StreamWriter file = new StreamWriter("result.json", false))
            {
                file.Write(message);
            }
            using (FileStream file = new FileStream("result.json", FileMode.Create))
            {
                result = (Result)serializer.ReadObject(file);
            }
            if (result.exitCode != 0)
            {
                Console.WriteLine("Error.");
            }
            else
            {
                if (result.outputInfo is string)
                {
                    Console.WriteLine(result.outputInfo);
                }
                else
                {
                    foreach (Technical technical in (result.outputInfo as List<Technical>))
                    {
                        Console.WriteLine(technical.ToString());
                    }
                }
            }*/
        }

        // закрытие подключения
        protected internal void Close()
        {
            if (Stream != null)
                Stream.Close();
            if (client != null)
                client.Close();
        }
    }
    public enum Commands
    {
        Create,
        Delete,
        Edit,
        Get
    }
    public enum ExitCode
    {
        OK,
        AccessError,
        EditError,
        DeleteError,
        FileNotFoundError
    }
    [Serializable]
    public class Command
    {
        public Commands command = 0;
        public string infoFirst = "";
        public string infoSecond = "";
        public override string ToString()
        {
            return command.ToString() + ";" + infoFirst.ToString() + ";" + infoSecond.ToString();
        }
    }
    [Serializable]
    public class Result
    {
        public Commands command = 0;
        public ExitCode exitCode = 0;
        public object outputInfo = null;
        public override string ToString()
        {
            return command.ToString() + ";" + exitCode.ToString() + ";" + outputInfo.ToString();
        }
    }
    [Serializable]
    public class Technical
    {
        public string Name { get; set; }
        public string ProducerState { get; set; }
        public double Cost { get; set; }
        public double Weight { get; set; }
        public double Objem { get; set; }
        public Technical(string name, string producerState, double cost, double weight, double objem)
        {
            Name = name;
            ProducerState = producerState;
            Cost = cost;
            Weight = weight;
            Objem = objem;
        }
        public override string ToString()
        {
            return Name + "," + ProducerState + "," + Cost.ToString() + "," + Weight.ToString() + "," + Objem.ToString();
        }
        public static void Sort(IEnumerable<Technical> list)
        {
            List<Technical> sortedList = list.ToList();
            sortedList.Sort((x, y) =>
            {
                if (x.Weight > y.Weight)
                {
                    return 1;
                }
                else if (x.Weight < y.Weight)
                {
                    return -1;
                }
                return 0;
            }
            );
            list = sortedList;
        }
        public static IEnumerable<Technical> Find(IEnumerable<Technical> list, double cost)
        {
            List<Technical> findList = new List<Technical>();
            foreach (Technical technical in list)
            {
                if (technical.Cost == cost)
                {
                    findList.Add(technical);
                }
            }
            return findList;
        }
    }
}
