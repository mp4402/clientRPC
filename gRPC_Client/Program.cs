﻿using System;
using System.Drawing;
using System.Threading.Tasks;
using CustomerGrpc;
using Grpc.Core;
using Grpc.Net.Client;

namespace CustomerClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            if (args.Length > 2)
            {
                try
                {
                    var customer = new Customer
                    {
                        ColorInConsole = GetRandomChatColor(),
                        Id = Guid.NewGuid().ToString(),
                        Name = args[2]
                    };
                    string address = "http://" + args[0] + ":" + args[1];
                    address = address.Trim();
                    var channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions { Credentials = ChannelCredentials.Insecure });
                    var client = new CustomerService.CustomerServiceClient(channel);
                    var joinCustomerReply = await client.JoinCustomerChatAsync(new JoinCustomerRequest
                    {
                        Customer = customer
                    });
                    if (joinCustomerReply.RoomId != -1)
                    {
                        using (var streaming = client.SendMessageToChatRoom(new Metadata { new Metadata.Entry("CustomerName", customer.Name) }))
                        {
                            var response = Task.Run(async () =>
                            {
                                while (await streaming.ResponseStream.MoveNext())
                                {
                                    Console.ForegroundColor = Enum.Parse<ConsoleColor>(streaming.ResponseStream.Current.Color);
                                    //Aqui se escribe el mensaje
                                    Console.WriteLine($"{streaming.ResponseStream.Current.MessageTime} - {streaming.ResponseStream.Current.CustomerName}: {streaming.ResponseStream.Current.Message}");
                                    Console.ForegroundColor = Enum.Parse<ConsoleColor>(customer.ColorInConsole);
                                }
                            });

                            await streaming.RequestStream.WriteAsync(new ChatMessage
                            {
                                CustomerId = customer.Id,
                                Color = customer.ColorInConsole,
                                Message = "",
                                RoomId = joinCustomerReply.RoomId,
                                CustomerName = customer.Name,
                            });
                            Console.ForegroundColor = Enum.Parse<ConsoleColor>(customer.ColorInConsole);
                            Console.WriteLine($"Joined the chat as {customer.Name}");
                            string statFunction = "";
                            string file = "";
                            int pos_espacio = 0;
                            var line = "";
                            while (true)
                            {
                                try
                                {
                                    line = Console.ReadLine();
                                    DeletePrevConsoleLine();
                                    break;
                                }
                                catch (Exception)
                                {
                                    Console.WriteLine("Mensaje no valido");
                                    line = "";
                                }
                            }
                            while (!string.Equals(line.ToLower(), "quit", StringComparison.OrdinalIgnoreCase))
                            {
                                try
                                {
                                    if (line.IndexOf("send", 0, 5) != -1)
                                    {
                                        pos_espacio = line.IndexOf(" ", 5); // segundo espacio
                                        statFunction = line.Substring(4, pos_espacio - 4).Trim().ToLower();
                                        file = line.Substring(pos_espacio + 1);
                                        switch (statFunction)
                                        {
                                            case "std":"min":"max":"mean":"count":
                                                    await streaming.RequestStream.WriteAsync(new ChatMessage
                                                    {
                                                        Color = customer.ColorInConsole,
                                                        CustomerId = customer.Id,
                                                        CustomerName = customer.Name,
                                                        Message = file,
                                                        RoomId = joinCustomerReply.RoomId,
                                                        CustomerDest = statFunction
                                                    });
                                                break;
                                            default:
                                                Console.WriteLine("Debe ingresar una funcion estadistica correcta");
                                                break;
                                        }
                                        line = Console.ReadLine();
                                        DeletePrevConsoleLine();
                                    }
                                    else
                                    {
                                        Console.WriteLine("===========");
                                        line = Console.ReadLine();
                                        DeletePrevConsoleLine();
                                    }
                                }
                                catch (Exception)
                                {
                                    Console.WriteLine("===========");
                                    line = Console.ReadLine();
                                    DeletePrevConsoleLine();
                                }

                            }
                            await streaming.RequestStream.WriteAsync(new ChatMessage
                            {
                                Color = customer.ColorInConsole,
                                CustomerId = customer.Id,
                                CustomerName = customer.Name,
                                Message = line,
                                RoomId = joinCustomerReply.RoomId,
                                CustomerDest = statFunction
                            });
                            await streaming.RequestStream.CompleteAsync();
                        }
                    }
                    else
                    {
                        Console.WriteLine("Id invalido, ya existe");
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Direccion IP o puerto invalido");
                }


            }
            else
            {
                Console.WriteLine("Debe de mandar como parámetros: [ipserver port id_maquina]");
            }
            Console.WriteLine("Presione cualquier tecla para salir del programa");
            Console.ReadKey();
            DeletePrevConsoleLine();
        }

        private static string GetRandomChatColor()
        {
            var colors = Enum.GetValues(typeof(ConsoleColor));
            var rnd = new Random();
            return colors.GetValue(rnd.Next(1, colors.Length - 1)).ToString();
        }

        private static void DeletePrevConsoleLine()
        {
            if (Console.CursorTop == 0) return;
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, Console.CursorTop - 1);
        }
    }
}