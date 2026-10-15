namespace MEDv1.Models;

public class FieldOutcome
{
	public string TestCode { get; init; } = "";
	public string? Raw { get; init; }          // what the user typed
	public string? Error { get; init; }
	public SavedValues? Value { get; init; }    // the parsed value; null when blank or invalid
}

public class ValidationOutcome
{
	public List<FieldOutcome> Fields { get; } = new();

	public bool IsValid => Fields.All(f => f.Error == null);

	public IEnumerable<SavedValues> ToSave =>
		Fields.Where(f => f.Error == null && f.Value != null).Select(f => f.Value!);

	public IEnumerable<string> ToDelete =>
		Fields.Where(f => f.Error == null && f.Value == null).Select(f => f.TestCode);

	public FieldOutcome For(string testCode) => Fields.First(f => f.TestCode == testCode);
}