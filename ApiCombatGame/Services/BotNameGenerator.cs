using ApiCombatGame.Services.Interfaces;

namespace ApiCombatGame.Services;

/// <summary>
/// Generates dev-culture themed names for AI bot players.
/// Names hint at being bots but don't explicitly reveal it.
/// </summary>
public class BotNameGenerator : IBotNameGenerator
{
    private static readonly string[] NamePrefixes = new[]
    {
        // Dev culture classics
        "CodeMonkey", "RubberDuck", "CopyPasta", "BugHunter", "StackOverflow",
        "GitBlame", "NullPointer", "RegexNinja", "SegFault", "CoffeeOverflow",

        // Programming concepts
        "RecursiveLoop", "InfiniteLoop", "AsyncAwait", "NullRef", "DeadlockDan",
        "RaceCondition", "MemoryLeak", "HeapOverflow", "BufferBoy", "PointerPete",

        // Dev tools & tech
        "VSCodeVic", "DockerDave", "KuberneteKid", "JenkinsJoe", "GitHubGary",
        "ElasticEric", "RedisRick", "MongoMike", "PostgresPaul", "MySQLMary",

        // Dev life
        "MergeConflict", "RebaseRebel", "CommitCrusher", "PullRequestPro", "CodeReviewer",
        "HotfixHero", "ProductionPanic", "StagingSteve", "DevOpsDoug", "CICDCarl",

        // Debugging & testing
        "ConsoleLogger", "DebuggerDan", "UnitTester", "IntegrationIvy", "E2EEvan",
        "BreakpointBob", "WatchWindow", "CallStackSam", "TracebackTom", "AssertionAmy",

        // Languages & frameworks
        "JavaJill", "PythonPete", "RustRanger", "GoGopher", "SwiftSam",
        "KotlinKate", "TypeScriptTim", "PHPPhil", "RubyRed", "ElixirEli",

        // Algorithms & data structures
        "BinaryTree", "HashMapHank", "LinkedListLou", "GraphGail", "QueueQuinn",
        "StackSteve", "HeapHarvey", "TrieTravis", "DijkstraDan", "BreadthFirst",

        // Internet culture
        "404NotFound", "500Error", "TeaPot418", "HTTPSHank", "JSONJay",
        "XMLXander", "YAMLYvonne", "TOMLTom", "CSVCarl", "Base64Bob",

        // Refactoring & patterns
        "RefactorRoy", "DesignPattern", "SingletonSue", "FactoryFrank", "ObserverOllie",
        "StrategySteve", "DecoratorDee", "AdapterAlex", "FacadeFaye", "ProxyPete"
    };

    private readonly Random _random = new();
    private readonly HashSet<string> _usedNames = new();

    public string GenerateBotName()
    {
        // Try to generate a unique name (max 100 attempts)
        for (int attempt = 0; attempt < 100; attempt++)
        {
            var prefix = NamePrefixes[_random.Next(NamePrefixes.Length)];
            var number = _random.Next(10, 100); // Two-digit numbers
            var name = $"{prefix}_{number}";

            if (_usedNames.Add(name))
            {
                return name;
            }
        }

        // Fallback: use timestamp to guarantee uniqueness
        var fallbackPrefix = NamePrefixes[_random.Next(NamePrefixes.Length)];
        return $"{fallbackPrefix}_{DateTime.UtcNow.Ticks % 10000}";
    }

    public List<string> GenerateBotNames(int count)
    {
        var names = new List<string>(count);
        for (int i = 0; i < count; i++)
        {
            names.Add(GenerateBotName());
        }
        return names;
    }
}
