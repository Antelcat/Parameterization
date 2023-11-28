using Antelcat.Parameterization.Demo.Models;

namespace Antelcat.Parameterization.Demo;

[Parameterization]
public static partial class Program
{
	public static void Main(string[] args)
	{
		if (args.Length == 0)
		{
			Console.WriteLine("Interactive mode, input 'exit' to exit.");
			while (true)
			{
				try
				{
					ExecuteInput(Console.ReadLine());
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
				}
			}
		}

		try
		{
			ExecuteArguments(args);
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
		[Argument(FullName = "all", ShortName = "a", Description = "Show all containers (default shows just running)", DefaultValue = "true")]
		bool showAll = false)
	{
		Console.WriteLine("CONTAINER ID    IMAGE    NAME    STATUS");
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
	private static void Run(Image image, string? name = null)
	{
		Pull(image);
		name ??= image.Name;
		var container = new Container(image, Guid.NewGuid().ToString("N")[..8], name, true);
		Containers.Add(container);
		Console.WriteLine(container);
	}

	[Command]
	private static void Stop(string id)
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
	}

	[Command(ShortName = "e")]
	private static void Exit()
	{
		Environment.Exit(0);
	}
}