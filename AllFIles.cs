ISerializer.cs
using System;

namespace BiologyProject.Serialization
{
    public interface ISerializer<T>
    {
        void Serialize(T data, string filePath);
        T Deserialize(string filePath);
    }
}


ICovariant.cs
namespace BiologyProject.Generics
{
    public interface ICovariant<out T>
    {
        T GetOrganism();
    }
}

IContravariant.cs
namespace BiologyProject.Generics
{
    public interface IContravariant<in T>
    {
        void ProcessOrganism(T organism);
    }
}

Organism.cs
using System;

namespace BiologyProject.Models
{
    public abstract class Organism
    {
        public string Name { get; set; }
        public string Type { get; set; }

        public abstract void Describe();
    }
}
Flora.cs
using System;

namespace BiologyProject.Models
{
    public class Flora : Organism
    {
        public override void Describe() => Console.WriteLine($"Flora: {Name} ({Type})");
    }
}
Fauna.cs
using System;

namespace BiologyProject.Models
{
    public class Fauna : Organism
    {
        public override void Describe() => Console.WriteLine($"Fauna: {Name} ({Type})");
    }
}

Territory.cs
using System;
using System.Collections.Generic;

namespace BiologyProject.Models
{
    public class Territory
    {
        public string Name { get; set; }
        public List<Organism> Organisms { get; set; } = new List<Organism>();

        public void AddOrganism(Organism organism) => Organisms.Add(organism);

        public void Describe()
        {
            Console.WriteLine($"Territory: {Name}");
            foreach (var organism in Organisms)
                organism.Describe();
        }

        public void SimulateCycle()
        {
            foreach (var organism in Organisms)
            {
                if (organism is Fauna fauna)
                    Console.WriteLine($"{fauna.Name} feeds on plants.");
                else if (organism is Flora flora)
                    Console.WriteLine($"{flora.Name} grows in the sunlight.");
            }
        }
    }
}

OrganismCollection.cs
using System;
using System.Collections;
using System.Collections.Generic;

namespace BiologyProject.Collections
{
    public class OrganismCollection<T> : ICollection<T>, ICloneable where T : Organism
    {
        private readonly List<T> _items = new List<T>();

        public int Count => _items.Count;
        public bool IsReadOnly => false;

        public void Add(T item) => _items.Add(item);
        public void Clear() => _items.Clear();
        public bool Contains(T item) => _items.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
        public bool Remove(T item) => _items.Remove(item);
        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public object Clone()
        {
            var clonedCollection = new OrganismCollection<T>();
            foreach (var item in _items)
                clonedCollection.Add(item);
            return clonedCollection;
        }
    }
}

AsyncSorter.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BiologyProject.Sorting
{
    public class AsyncSorter<T>
    {
        public async Task SortAsync(List<T> collection, Func<T, T, int> comparer, Action<string> logger)
        {
            logger("Sorting started...");
            await Task.Run(() =>
            {
                collection.Sort((x, y) => comparer(x, y));
            });
            logger($"Sorting completed. Processed {collection.Count} items.");
        }
    }
}

Logger.cs
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BiologyProject.Logging
{
    public class Logger : IDisposable
    {
        private readonly BlockingCollection<string> _logQueue = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public event Action<string> LogEvent;

        public Logger()
        {
            Task.Run(() => ProcessLogs(_cancellationTokenSource.Token));
        }

        public void Log(string message)
        {
            var logMessage = $"[LOG {DateTime.Now}]: {message}";
            _logQueue.Add(logMessage);
            LogEvent?.Invoke(logMessage);
        }

        private void ProcessLogs(CancellationToken cancellationToken)
        {
            try
            {
                foreach (var log in _logQueue.GetConsumingEnumerable(cancellationToken))
                {
                    Console.WriteLine(log);
                    File.AppendAllText("log.txt", log + Environment.NewLine);
                }
            }
            catch (OperationCanceledException)
            {
                // Logging stop
            }
        }

        public void Dispose()
        {
            _logQueue.CompleteAdding();
            _cancellationTokenSource.Cancel();
        }
    }
}

XmlSerializerAdapter.cs
using System.IO;
using System.Xml.Serialization;

namespace BiologyProject.Serialization
{
    public class XmlSerializerAdapter<T> : ISerializer<T>
    {
        public void Serialize(T data, string filePath)
        {
            var serializer = new XmlSerializer(typeof(T));
            using var writer = new StreamWriter(filePath);
            serializer.Serialize(writer, data);
        }

        public T Deserialize(string filePath)
        {
            var serializer = new XmlSerializer(typeof(T));
            using var reader = new StreamReader(filePath);
            return (T)serializer.Deserialize(reader);
        }
    }
}

JsonSerializerAdapter.cs
using System.IO;
using System.Text.Json;

namespace BiologyProject.Serialization
{
    public class JsonSerializerAdapter<T> : ISerializer<T>
    {
        public void Serialize(T data, string filePath)
        {
            var json = JsonSerializer.Serialize(data);
            File.WriteAllText(filePath, json);
        }

        public T Deserialize(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<T>(json);
        }
    }
}
CustomException.cs
using System;

namespace BiologyProject.Exceptions
{
    public class CustomException : Exception
    {
        public CustomException(string message) : base(message) { }
        public CustomException(string message, Exception innerException) : base(message, innerException) { }
    }
}


MAIN.CS
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BiologyProject.Collections;
using BiologyProject.Exceptions;
using BiologyProject.Logging;
using BiologyProject.Models;
using BiologyProject.Serialization;
using BiologyProject.Sorting;

namespace BiologyProject
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using var logger = new Logger();
            logger.LogEvent += Console.WriteLine;

            try
            {
                var floraCollection = new List<Flora>
                {
                    new Flora { Name = "Oak", Type = "Tree" },
                    new Flora { Name = "Rose", Type = "Flower" },
                    new Flora { Name = "Pine", Type = "Tree" }
                };

                var sorter = new AsyncSorter<Flora>();
                await sorter.SortAsync(
                    floraCollection,
                    (x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal),
                    logger.Log
                );

                var xmlSerializer = new XmlSerializerAdapter<List<Flora>>();
                xmlSerializer.Serialize(floraCollection, "flora.xml");
                logger.Log("Serialized to flora.xml");

                var deserializedFlora = xmlSerializer.Deserialize("flora.xml");
                foreach (var flora in deserializedFlora)
                    flora.Describe();
            }
            catch (Exception ex)
            {
                logger.Log($"Exception: {ex.Message}");
            }
        }
    }
}