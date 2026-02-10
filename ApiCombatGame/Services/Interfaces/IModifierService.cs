using ApiCombatGame.Models.Domain;
using ApiCombatGame.Services.Modifiers;

namespace ApiCombatGame.Services.Interfaces;

public interface IModifierService
{
    Task<EnvironmentalModifier?> GetCurrentModifier();
    Task<EnvironmentalModifier?> GetUpcomingModifier();
    Task RotateModifier();
    IModifierEffect? GetModifierEffect(string modifierType);
}
