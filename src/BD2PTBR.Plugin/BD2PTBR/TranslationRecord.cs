using Newtonsoft.Json;

namespace BD2PTBR;

internal sealed class TranslationRecord
{
	[JsonProperty("key")]
	public string Key { get; set; }

	[JsonProperty("scope")]
	public string Scope { get; set; }

	[JsonProperty("table")]
	public string Table { get; set; }

	[JsonProperty("id")]
	public int Id { get; set; }

	[JsonProperty("source")]
	public string Source { get; set; }

	[JsonProperty("sourceHash")]
	public string SourceHash { get; set; }

	[JsonProperty("translation")]
	public string Translation { get; set; }

	[JsonProperty("status")]
	public string Status { get; set; }
}
