using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace ServerApp
{
    class Program
    {
        static  void Main(string[] args)
        {
            List<User> users = new List<User>();
            TcpListener Listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 3231);
            Listener.Start();
            const string GIVE = "GIVE";
            const string ADD = "ADD";
            const string UPDATE = "UPDATE";
            const string REMOVE = "REMOVE";
            while (true)
            {
                using (var client = Listener.AcceptTcpClient())
                using (var context = new Context())
                {
                    Console.WriteLine("Cоединение открыто");
                    using (var stream = client.GetStream())
                    {
                        var resultText = string.Empty;
                        while (stream.DataAvailable)
                        {
                            var buffer = new byte[1024];
                            stream.Read(buffer, 0, buffer.Length);
                            resultText += System.Text.Encoding.UTF8.GetString(buffer).TrimEnd('\0');
                        }

                        //var us = JsonConvert.SerializeObject(users);
                        //Console.WriteLine(us);

                        XElement xmlElements = new XElement("Branches", users.Select(i => new XElement("branch", i)));
                        Console.WriteLine(xmlElements);

                        var response = JsonConvert.DeserializeObject<Response>(resultText);
                        if (response.Path == "user")
                        {
                            if (response.Action == GIVE)
                            {
                                var data = JsonConvert.SerializeObject(context.Users.ToList());
                                var answer = System.Text.Encoding.UTF8.GetBytes(data);
                                stream.Write(answer, 0, answer.Length);
                            }
                            else if (response.Action == ADD)
                            {
                                var user = new User()
                                {
                                    Name = response.Value,
                                    //Id = users.Count + 1

                                };
                                users.Add(user);
                                context.Users.Add(user);
                                context.SaveChanges();

                            }
                            else if (response.Action == UPDATE)
                            {
                                var user = context.Users.FirstOrDefault(x => x.Id == int.Parse(response.Value));
                                user.Name = response.NewData;
                                context.Update(user);
                                context.SaveChanges();
                            }
                            else if (response.Action == REMOVE)
                            {
                                var user = context.Users.FirstOrDefault(x => x.Id == int.Parse(response.Value));
                                user.IsDeleted = true;
                                context.Update(user);
                                context.SaveChanges();
                            }
                        }
                        //users = users.Where(x => x.IsDeleted == false).ToList();
                    }
                }
            }
        }
    }
}
