using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestNamespace;

namespace Antelcat.Parameterization.Demo
{
	[Parameterization(CaseSensitive = true)]
	public static partial class Program
	{
		public static async Task Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.WriteLine("Interactive mode, input 'exit' to exit.");
				while (true)
				{
					try
					{
						await ExecuteInputAsync(Console.ReadLine());
					}
					catch (Exception e)
					{
						Console.WriteLine(e.Message);
					}
				}
			}

			try
			{
				await ExecuteArgumentsAsync(args);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		private static readonly HashSet<Image> DownloadedImages = new();
		private static readonly HashSet<Container> Containers = new();

		[Command(ShortName = "ps", Description = "Display a list of container(s) resources usage statistics")]
		private static void Stats(
			[Argument(FullName = "all", ShortName = 'a', Description = "Show all containers (default shows just running)", DefaultValue = "true")]
			bool showAll = false)
		{
			Console.WriteLine("CONTAINER_ID    IMAGE    NAME    STATUS    PORTS");
			foreach (var container in Containers.Where(container => showAll || container.IsRunning))
			{
				Console.WriteLine($"{container.Id}    {container.Image}    {container.Name}    {(container.IsRunning ? "running" : "stopped")}");
			}
		}

		[Command]
		private static void Pull(Image image)
		{
			if (DownloadedImages.Contains(image))
			{
				Console.WriteLine($"Image {image} already pulled.");
				return;
			}

			Console.WriteLine($"Pulling image {image}...");
			for (var i = 0; i < 10; i++)
			{
				Console.WriteLine($"{i * 10}%");
				Thread.Sleep(100);
			}

			DownloadedImages.Add(image);
			Console.WriteLine($"Successfully Pulled image {image}");
		}

		[Command]
		private static void Run(
			Image image, 
			string? name = null, 
			[Argument(FullName = "map-type")] MapType mapType = MapType.Forward,
			[Argument(ShortName = 'p', Converter = typeof(PortMappingConverter))] PortMapping[]? portMappings = null)
		{
			Pull(image);
			name ??= image.Name;
			var container = new Container(image, Guid.NewGuid().ToString("N")[..8], name, true, portMappings);
			Containers.Add(container);
			Console.WriteLine(container);
		}

		[Command]
		private static Task Stop(string id)
		{
			var container = Containers.FirstOrDefault(container => container.Id == id);
			if (container == null)
			{
				Console.WriteLine($"Container {id} not found.");
			}
			else
			{
				Console.WriteLine($"Stopping container {id}...");
				container.IsRunning = false;
			}

			return Task.CompletedTask;
		}

		[Command(ShortName = "e")]
		private static void Exit()
		{
			Environment.Exit(0);
		}
	}

	public class Container(Image image, string id, string name, bool isRunning, PortMapping[]? portMappings)
	{
		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}

		public virtual bool Equals(Container? other)
		{
			return other != null && Id == other.Id;
		}

		public override string ToString()
		{
			return $"{Image} {Name} {(IsRunning ? "running" : "stopped")} {(PortMappings == null ? string.Empty : string.Join<PortMapping>(", ", PortMappings))}";
		}

		public Image Image { get; } = image;
		public string Id { get; } = id;
		public string Name { get; init; } = name;
		public bool IsRunning { get; set; } = isRunning;
		public PortMapping[]? PortMappings { get; } = portMappings;
	}

	public class ImageConverter : StringConverter
	{
		public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
		{
			if (value is not string str) throw new ArgumentException("Invalid image string format");
			var args = str.Split(':');
			return args switch
			{
				{ Length: 1 } => new Image(str, null),
				{ Length: 2 } => new Image(args[0], Version.Parse(args[1])),
				_ => throw new ArgumentException("Invalid image string format")
			};
		}
	}

	[TypeConverter(typeof(ImageConverter))]
	public record Image(string Name, Version? Version)
	{
		public override string ToString()
		{
			return $"{Name}:{Version?.ToString() ?? "latest"}";
		}
	}

	public class PortMappingConverter : StringConverter
	{
		public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
		{
			if (value is not string str) throw new ArgumentException("Invalid port mapping string format");
			var args = str.Split(':');
			return args switch
			{
				{ Length: 1 } => new PortMapping(int.Parse(args[0]), int.Parse(args[0])),
				{ Length: 2 } => new PortMapping(int.Parse(args[0]), int.Parse(args[1])),
				_ => throw new ArgumentException("Invalid port mapping string format")
			};
		}
	}

	public record PortMapping(int HostPort, int ContainerPort)
	{
		public override string ToString()
		{
			return $"{HostPort}->{ContainerPort}/tcp";
		}
	}
}

namespace TestNamespace
{
	public enum MapType
	{
		Forward,
		Proxy
	}
}