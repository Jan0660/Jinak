using Timer = System.Timers.Timer;

namespace Jinak;

public static class DumbTimer
{
    public static List<DumbTimerData> Timers = new();

    public static void Start(string name, double interval, Action callback)
    {
        if (Timers.Any(t => t.Name == name))
            throw new Exception($"Timer with name {name} already exists");

        var data = new DumbTimerData()
        {
            Name = name,
            Callback = callback,
            Timer = new Timer(interval)
        };
        Timers.Add(data);
        data.Timer.Elapsed += (_, _) =>
        {
            try
            {
                data.Callback();
                data.LastException = null;
                Console.Debug($"Timer {name} executed successfully");
            }
            catch (Exception exc)
            {
                Console.Debug($"Timer {name} failed: {exc.Message}");
                data.LastException = exc;
            }

            data.LastRun = DateTimeOffset.Now;
        };
        data.Timer.Start();
    }
}

public class DumbTimerData
{
    public string Name { get; set; }
    public Action Callback { get; set; }
    public DateTimeOffset LastRun { get; set; }
    public Exception? LastException { get; set; }
    public Timer Timer { get; set; }
}