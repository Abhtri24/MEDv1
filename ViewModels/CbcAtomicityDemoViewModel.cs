
using ChecklistApp.ViewModels;
using MEDv1.Models;

namespace MEDv1.ViewModels;

public class CbcAtomicityDemoViewModel
{
	public EntryForm MedicalForm { get; set; } = new();

	public Dictionary<string, string?> Inputs { get; set; } = new();

	public ChecklistPageViewModel Checklist { get; set; } = new();

	public bool ShowChecklist { get; set; }

	public bool SimulateUploadFailure { get; set; }

	public string? ErrorMessage { get; set; }
}
