using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using ApiCombatGame.Models.DTOs.Strategy;
using Xunit;

namespace ApiCombatGame.Tests.UnitTests;

public class StrategyValidationTests
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void DefaultStrategy_SerializesCorrectly()
    {
        var config = new StrategyConfig();
        var json = JsonSerializer.Serialize(config, Json);
        var deserialized = JsonSerializer.Deserialize<StrategyConfig>(json, Json)!;

        Assert.Equal(Formation.balanced, deserialized.Formation);
        Assert.Single(deserialized.TargetPriority);
        Assert.Equal(TargetPriority.lowest_hp, deserialized.TargetPriority[0]);
        Assert.Empty(deserialized.Abilities);
    }

    [Fact]
    public void InvalidFormation_FailsDeserialization()
    {
        var json = """{"formation":"turtle","targetPriority":["lowest_hp"]}""";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<StrategyConfig>(json, Json));
    }

    [Fact]
    public void ValidEnumValues_AllSerializeAsLowercase()
    {
        foreach (var formation in Enum.GetValues<Formation>())
        {
            var serialized = JsonSerializer.Serialize(formation, Json);
            Assert.Equal($"\"{formation}\"", serialized);
            Assert.DoesNotContain(serialized, c => char.IsUpper(c));
        }

        foreach (var priority in Enum.GetValues<TargetPriority>())
        {
            var serialized = JsonSerializer.Serialize(priority, Json);
            Assert.DoesNotContain(serialized.Trim('"'), c => char.IsUpper(c));
        }
    }

    [Fact]
    public void AbilityCondition_DefaultValues_AreCorrect()
    {
        var condition = new AbilityCondition();
        Assert.Equal(AbilityWhen.always, condition.When);
        Assert.Equal(AbilityTarget.priority, condition.Target);
    }

    [Fact]
    public void StrategyConfig_WithAllFields_RoundTrips()
    {
        var config = new StrategyConfig
        {
            Formation = Formation.aggressive,
            TargetPriority = new List<TargetPriority>
            {
                TargetPriority.healers,
                TargetPriority.lowest_hp,
                TargetPriority.highest_threat
            },
            Abilities = new Dictionary<string, AbilityCondition>
            {
                ["Fireball"] = new() { When = AbilityWhen.always, Target = AbilityTarget.priority },
                ["Heal"] = new() { When = AbilityWhen.ally_hp_below_50, Target = AbilityTarget.lowest_ally_hp },
                ["Meteor"] = new() { When = AbilityWhen.enemy_count_gte_2, Target = AbilityTarget.all_enemies }
            }
        };

        var json = JsonSerializer.Serialize(config, Json);
        var deserialized = JsonSerializer.Deserialize<StrategyConfig>(json, Json)!;

        Assert.Equal(Formation.aggressive, deserialized.Formation);
        Assert.Equal(3, deserialized.TargetPriority.Count);
        Assert.Equal(TargetPriority.healers, deserialized.TargetPriority[0]);
        Assert.Equal(3, deserialized.Abilities.Count);
        Assert.Equal(AbilityWhen.ally_hp_below_50, deserialized.Abilities["Heal"].When);
        Assert.Equal(AbilityTarget.lowest_ally_hp, deserialized.Abilities["Heal"].Target);
        Assert.Equal(AbilityTarget.all_enemies, deserialized.Abilities["Meteor"].Target);
    }

    [Fact]
    public void EnumDescriptions_AllPopulated()
    {
        AssertAllDescriptions<Formation>();
        AssertAllDescriptions<TargetPriority>();
        AssertAllDescriptions<AbilityWhen>();
        AssertAllDescriptions<AbilityTarget>();
    }

    private static void AssertAllDescriptions<T>() where T : struct, Enum
    {
        foreach (var value in Enum.GetValues<T>())
        {
            var member = typeof(T).GetMember(value.ToString());
            Assert.True(member.Length > 0, $"No member found for {typeof(T).Name}.{value}");

            var desc = member[0].GetCustomAttribute<DescriptionAttribute>();
            Assert.NotNull(desc);
            Assert.False(string.IsNullOrWhiteSpace(desc!.Description),
                $"{typeof(T).Name}.{value} has empty [Description]");
        }
    }
}
