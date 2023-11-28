namespace Antelcat.Parameterization.Demo.Models;

public class Container(Image image, string id, string name, bool isRunning)
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
        return $"{Image} {Name} {(IsRunning ? "running" : "stopped")}";
    }

    public Image Image { get; } = image;
    public string Id { get; } = id;
    public string Name { get; init; } = name;
    public bool IsRunning { get; set; } = isRunning;
}