using System.Globalization;
using MEDv1.Models;
namespace MEDv1.Services;

public static class ResultValidator
{
	private const int MaxTextLength = 1000;
	private const int MaxDecimalPlaces = 4;                       // matches DECIMAL(18,4)
	private const decimal MaxAbsoluteNumber = 99_999_999_999_999m; // 14 integer digits

	public static ValidationOutcome Validate(IEnumerable<Test> tests, IReadOnlyDictionary<string, string?> inputs)
	{
		var outcome = new ValidationOutcome();
		foreach (var test in tests)
		{
			inputs.TryGetValue(test.TestCode, out var raw);
			outcome.Fields.Add(ValidateField(test, raw?.Trim()));
		}
		return outcome;
	}

	private static FieldOutcome ValidateField(Test test, string? raw)
	{
		if (string.IsNullOrEmpty(raw))
			return new FieldOutcome { TestCode = test.TestCode, Raw = raw, Error = test.IsRequired ? "Required" : null };

		return test.DataType switch
		{
			"NUMBER" => ValidateNumber(test, raw),
			"DATE" => ValidateDate(test, raw),
			"TEXT" => ValidateText(test, raw),
			"CHOICE" => ValidateChoice(test, raw),
			"BOOLEAN" => ValidateBoolean(test, raw),
			_ => Fail(test, raw, $"Unsupported field type '{test.DataType}'")
		};
	}

	private static FieldOutcome ValidateNumber(Test test, string raw)
	{
		if (!decimal.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
			return Fail(test, raw, "Enter a number");
		if (Math.Abs(number) > MaxAbsoluteNumber)
			return Fail(test, raw, "Number is too large");
		if (decimal.Round(number, MaxDecimalPlaces) != number)
			return Fail(test, raw, $"Use at most {MaxDecimalPlaces} decimal places");
		if (test.MinValue is { } min && number < min)
			return Fail(test, raw, $"Minimum is {Format(min)}");
		if (test.MaxValue is { } max && number > max)
			return Fail(test, raw, $"Maximum is {Format(max)}");

		return Ok(test, raw, new SavedValues { TestCode = test.TestCode, ValueNumber = number });
	}

	private static FieldOutcome ValidateDate(Test test, string raw)
	{
		if (!DateTime.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
			return Fail(test, raw, "Enter a valid date");

		return Ok(test, raw, new SavedValues { TestCode = test.TestCode, ValueDate = date });
	}

	private static FieldOutcome ValidateText(Test test, string raw) =>
		raw.Length > MaxTextLength
			? Fail(test, raw, $"Too long (maximum {MaxTextLength} characters)")
			: Ok(test, raw, new SavedValues { TestCode = test.TestCode, ValueText = raw });

	private static FieldOutcome Ok(Test test, string raw, SavedValues value) =>
		new() { TestCode = test.TestCode, Raw = raw, Value = value };

	private static FieldOutcome Fail(Test test, string? raw, string error) =>
		new() { TestCode = test.TestCode, Raw = raw, Error = error };

	private static FieldOutcome ValidateChoice(Test test, string raw) =>
	  test.Options.Any(o => o.OptionCode == raw)
		  ? Ok(test, raw, new SavedValues { TestCode = test.TestCode, ValueText = raw })
		  : Fail(test, raw, "Choose one of the listed options");

	private static FieldOutcome ValidateBoolean(Test test, string raw) =>
		raw is "Y" or "N"
			? Ok(test, raw, new SavedValues { TestCode = test.TestCode, ValueText = raw })
			: Fail(test, raw, "Invalid value");

	private static string Format(decimal value) => value.ToString("0.####", CultureInfo.InvariantCulture);
}