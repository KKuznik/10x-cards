namespace _10xCards.Models;

/// <summary>
/// Represents an AI model option for flashcard generation
/// </summary>
public class AiModelOption
{
	/// <summary>
	/// Model identifier/key used in API calls
	/// </summary>
	public required string Value { get; init; }

	/// <summary>
	/// Display name for the model
	/// </summary>
	public required string DisplayName { get; init; }

	/// <summary>
	/// Optional description of the model
	/// </summary>
	public string? Description { get; init; }

	/// <summary>
	/// Whether this is the default/recommended model
	/// </summary>
	public bool IsRecommended { get; init; }
}

/// <summary>
/// Static configuration for available AI models
/// </summary>
public static class AiModelOptions
{
	/// <summary>
	/// Default/recommended AI model
	/// </summary>
	public const string DefaultModel = "openai/gpt-4o-mini";

	/// <summary>
	/// List of available AI models for flashcard generation
	/// </summary>
	public static readonly List<AiModelOption> AvailableModels = new()
	{
		new AiModelOption
		{
			Value = "openai/gpt-4o-mini",
			DisplayName = "GPT-4 Mini (zalecany)",
			Description = "Szybki i ekonomiczny model OpenAI, idealny do generowania fiszek",
			IsRecommended = true
		},
		new AiModelOption
		{
			Value = "openai/gpt-4o",
			DisplayName = "GPT-4",
			Description = "Zaawansowany model OpenAI z najlepszą jakością odpowiedzi",
			IsRecommended = false
		},
		new AiModelOption
		{
			Value = "anthropic/claude-3-haiku",
			DisplayName = "Claude 3 Haiku",
			Description = "Szybki model Anthropic Claude",
			IsRecommended = false
		},
		new AiModelOption
		{
			Value = "anthropic/claude-3-sonnet",
			DisplayName = "Claude 3 Sonnet",
			Description = "Zbalansowany model Anthropic Claude",
			IsRecommended = false
		}
	};

	/// <summary>
	/// Get model display name by value
	/// </summary>
	public static string GetDisplayName(string modelValue)
	{
		var model = AvailableModels.FirstOrDefault(m => m.Value == modelValue);
		return model?.DisplayName ?? modelValue;
	}

	/// <summary>
	/// Check if model value is valid
	/// </summary>
	public static bool IsValidModel(string modelValue)
	{
		return AvailableModels.Any(m => m.Value == modelValue);
	}
}

